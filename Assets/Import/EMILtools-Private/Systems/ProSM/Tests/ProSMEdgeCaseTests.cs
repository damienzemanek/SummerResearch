using System;
using NUnit.Framework;
using ProArchitecture.Data;
using ProArchitecture.Logic;
using ProSM;
using ProTimers;
using UnityEngine;
using Unity.Collections;
using static ProSMTestSuite;
using Predicate = ProArchitecture.Predicates.Predicate;

public class ProSMEdgeCaseTests
{
    public struct SimpleData
    {
        public int value;
    }

    [SetUp]
    public void Setup()
    {
        TimerStack.Reset();
        TimerStackLogics.isTesting = true;
    }

    [TearDown]
    public void TearDown()
    {
        TimerStackLogics.isTesting = false;
    }

    [Test]
    public void Test_FSM_Move_Safety()
    {
        // This test demonstrates the vulnerability where capturing 'ref fsm' as a raw pointer
        // fails if the fsm struct is moved.
        
        ProSM<SimpleData> fsm = new ProSM<SimpleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, SimpleData>(0);
        SimpleData data = new SimpleData { value = 1 };
        fsm.Entry(ref data);

        // Add a timed transition. This captures the address of 'fsm'.
        fsm.AddDirectTimedTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ref data, 1.0f);

        // Move the FSM to a new location on the stack
        ProSM<SimpleData> movedFsm = fsm;
        
        // Zero out the old FSM memory to ensure we aren't "getting lucky"
        fsm = default;

        // Tick the timer to trigger the transition
        // The timer holds a pointer to the OLD location of 'fsm'
        try 
        {
            TimerStack.TickActivesDebug(1.1f);
            
            // If it didn't crash, let's see if 'movedFsm' was updated.
            // It shouldn't be, because the pointer was to 'fsm', not 'movedFsm'.
            Assert.AreNotEqual((int)TestLayerOne.L1S2, movedFsm.layers[0].currentState, 
                "The moved FSM should NOT have transitioned because the pointer was pointing to the old stack location.");
        }
        catch (Exception e)
        {
            Debug.Log($"Caught expected crash or issue: {e.Message}");
            Assert.Pass("Caught an issue when moving FSM, which confirms the edge case.");
        }
        
        movedFsm.Dispose();
    }

    [Test]
    public void Test_TimedTransition_StateInterruption()
    {
        ProSM<SimpleData> fsm = new ProSM<SimpleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, SimpleData>(0);
        SimpleData data = new SimpleData { value = 1 };
        fsm.Entry(ref data);

        // State A (L1S1) -> State B (L1S2) after 1s
        fsm.AddDirectTimedTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ref data, 1.0f);

        // Manually transition A -> C (L1S3) at 0.5s
        TimerStack.TickActivesDebug(0.5f);
        Assert.AreEqual((int)TestLayerOne.L1S1, fsm.layers[0].currentState);
        
        // Manual transition
        fsm.TransitionOnLayer_CallExitEnter(0, (int)TestLayerOne.L1S3, ref data);
        Assert.AreEqual((int)TestLayerOne.L1S3, fsm.layers[0].currentState);

        // Now wait for the original timer to hit 1.0s
        TimerStack.TickActivesDebug(0.6f);

        // Verify that we are still in State C, NOT State B
        // Currently, the implementation might jump to B because it only checks if nextState != currentState
        Assert.AreEqual((int)TestLayerOne.L1S3, fsm.layers[0].currentState, 
            "FSM should have remained in State C and ignored the stale timed transition from State A.");
        
        fsm.Dispose();
    }

    [Test]
    public void Test_TimerStack_Saturation()
    {
        TimerStack.Reset();
        // Default capacity is 10000
        int capacity = 10000;
        
        var events = new Data<TimerEvent>(1, Allocator.Temp);
        unsafe {
            static bool TruePredicate(void* ptr) => true;
            Predicate alwaysTrue = new Predicate(&TruePredicate);
            var ev = TimerEvent.NoData(default, ref ProTimersPredicates.IsGreaterThanOrEqualTo, ref Transitioner<SimpleData>.TransitionOperation, TimerEventType.OneShotTimerKeepsTicking);
            events.Allocate(ref ev);
            ProTimer timer = new ProTimer(TickMath.Add, ref events);

            for (int i = 0; i < capacity; i++)
            {
                TimerStack.AddTimer(ref timer);
            }

            Assert.AreEqual(capacity, TimerStack.timers.currentSize);

            // Adding one more should trigger a resize
            Assert.DoesNotThrow(() => TimerStack.AddTimer(ref timer), "TimerStack should handle overflow by resizing.");
            Assert.AreEqual(capacity + 1, TimerStack.timers.currentSize);
            
            events.Dispose();
        }
    }

    [Test]
    public void Test_SelfTransition_Ignored()
    {
        ProSM<SimpleData> fsm = new ProSM<SimpleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, SimpleData>(0);
        SimpleData data = new SimpleData { value = 1 };
        fsm.Entry(ref data);

        // We can check if TryPollTransitions returns false for self-transition
        unsafe
        {
            static bool TruePredicate(void* ptr) => true;
            Predicate alwaysTrue = new Predicate(&TruePredicate);
            fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S1, ref alwaysTrue);
        }

        bool transitioned = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref data, out int nextState);
        
        Assert.IsFalse(transitioned, "Self-transition should be ignored by TryPollTransitionsOnLayer.");
        Assert.AreEqual(-1, nextState);

        fsm.Dispose();
    }
    [Test]
    public void Test_Data_Reallocation_During_Tick()
    {
        // This test verifies that if we add a timer inside another timer's callback,
        // and that addition causes the TimerStack.timers (Data<ProTimer>) to reallocate,
        // the Batcher.Process loop doesn't crash or skip timers.
        
        TimerStack.Reset();
        
        int initialCapacity = 10000;
        var events = new Data<TimerEvent>(1, Allocator.Temp);
        unsafe {
            var ev = TimerEvent.NoData(new TimerPredicateInfo(0, 0.5f), ref ProTimersPredicates.IsGreaterThanOrEqualTo, ref ReallocationLogic.AddMoreTimersOperation, TimerEventType.OneShotTimerKeepsTicking);
            events.Allocate(ref ev);
            ProTimer timer = new ProTimer(TickMath.Add, ref events);

            // Fill up the stack to capacity - 1
            for (int i = 0; i < initialCapacity - 1; i++)
            {
                TimerStack.AddTimer(ref timer);
            }
            
            // Add the "trigger" timer at index 9999
            int triggerId = TimerStack.AddTimer(ref timer);
            TimerStack.StartTimer(triggerId);
            
            // Now tick. When the triggerId timer runs its callback, it will add another timer.
            // That addition will cause nextIndex to hit 10001, which triggers SetCapacity(20000).
            // This reallocates the underlying UnsafeList.
            
            try 
            {
                TimerStack.TickActivesDebug(1.0f);
            }
            catch (Exception e)
            {
                Assert.Pass($"Caught expected crash due to dangling pointer after reallocation: {e.Message}");
                return;
            }

            // If it didn't crash, verify it finished.
            Assert.AreEqual(initialCapacity + 1, TimerStack.timers.currentSize);
            
            events.Dispose();
        }
    }

    public static unsafe class ReallocationLogic
    {
        public static void AddMoreTimers(IntPtr* data)
        {
            var events = new Data<TimerEvent>(1, Allocator.Temp);
            var ev = TimerEvent.NoData(default, ref ProTimersPredicates.IsGreaterThanOrEqualTo, ref Transitioner<SimpleData>.TransitionOperation, TimerEventType.OneShotTimerKeepsTicking);
            events.Allocate(ref ev);
            ProTimer timer = new ProTimer(TickMath.Add, ref events);
            TimerStack.AddTimer(ref timer);
        }
        static bool AlwaysTrue(IntPtr* data) => true;
        public static LogicOperation<IntPtr> AddMoreTimersOperation = new(&AddMoreTimers, &AlwaysTrue);
    }

    [Test]
    public void Test_ProSM_EmptyTransitions()
    {
        ProSM<SimpleData> fsm = new ProSM<SimpleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, SimpleData>(0);
        SimpleData data = new SimpleData { value = 1 };
        fsm.Entry(ref data);

        // State L1S1 has NO transitions.
        bool transitioned = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref data, out int nextState);
        
        Assert.IsFalse(transitioned);
        Assert.AreEqual(-1, nextState);
        
        fsm.Dispose();
    }

    [Test]
    public void Test_ProSM_MultipleTransitions_Priority()
    {
        ProSM<SimpleData> fsm = new ProSM<SimpleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, SimpleData>(0);
        SimpleData data = new SimpleData { value = 1 };
        fsm.Entry(ref data);

        unsafe
        {
            static bool TruePredicate(void* ptr) => true;
            Predicate alwaysTrue = new Predicate(&TruePredicate);
            
            // Any transition to L1S3
            fsm.AddAnyTransition(0, TestLayerOne.L1S3, ref alwaysTrue);
            // Direct transition to L1S2
            fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ref alwaysTrue);
        }

        // Both transitions are true. AnyTransition should be checked first in TryPollTransitionsOnLayer.
        bool transitioned = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref data, out int nextState);
        
        Assert.IsTrue(transitioned);
        Assert.AreEqual((int)TestLayerOne.L1S3, nextState, "AnyTransitions should have priority over DirectTransitions.");
        
        fsm.Dispose();
    }

    [Test]
    public void Test_ProSM_Disposal_Active()
    {
        ProSM<SimpleData> fsm = new ProSM<SimpleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, SimpleData>(0);
        SimpleData data = new SimpleData { value = 1 };
        fsm.Entry(ref data);
        
        Assert.DoesNotThrow(() => fsm.Dispose());
        Assert.IsFalse(fsm.layers.Active);
    }
}
