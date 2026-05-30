using System;
using NUnit.Framework;
using ProArchitecture.Data;
using ProArchitecture.Logic;
using Unity.Collections;
using UnityEngine;
using ProTimers;
using Unity.Collections.LowLevel.Unsafe;
using Predicate = ProArchitecture.Predicates.Predicate;

public class ProTimerTestSuite
{
    static unsafe class SomeTriggerLogic
    {
        public static bool _eventTriggered = false;
        public static bool _removeTriggered = false;
        public static int _triggerCount = 0;
        public static int _removeCount = 0;
        public static int _testDataMutated = 0;
        public static IntPtr dummyPtr = IntPtr.Zero;
        
        static void SomeLogicRun(IntPtr* ptr)
        {
            _eventTriggered = true;
            _triggerCount++;
        }
        static bool SomeLogicShouldRun(IntPtr* ptr) => true;
        public static LogicOperation<IntPtr> TriggeredOperation = new(&SomeLogicRun, &SomeLogicShouldRun);
        

        static void MutateDataLogicRun(IntPtr* ptr)
        {
            Debug.Log("[DEBUG_LOG] Inside MutateDataLogicRun");

            _eventTriggered = true;
            _triggerCount++;
            ref int value = ref IntPtrPtrTo<int>.GetRef(ptr);
            value += 100;
        }
        public static LogicOperation<IntPtr> MutateDataOperation = new(&MutateDataLogicRun, &SomeLogicShouldRun);
    }

    [SetUp]
    public unsafe void Setup()
    {
        TimerStack.Reset();
        TimerStackLogics.isTesting = true;
        SomeTriggerLogic._eventTriggered = false;
        SomeTriggerLogic._removeTriggered = false;
        SomeTriggerLogic._triggerCount = 0;
        SomeTriggerLogic._removeCount = 0;
        fixed (IntPtr* p = &SomeTriggerLogic.dummyPtr)
            SomeTriggerLogic.dummyPtr = (IntPtr)p;
    }
    
    [TearDown]
    public void TearDown()
    {
        TimerStackLogics.isTesting = false;
        SomeTriggerLogic._triggerCount = 0;
        SomeTriggerLogic._removeCount = 0;
        SomeTriggerLogic._eventTriggered = false;
        SomeTriggerLogic._removeTriggered = false;
    }


    [Test]
    public void Test1_TimerInitialization()
    {
        // 1. Manually allocate memory for events
        int eventCount = 1;
        
        var events = new Data<TimerEvent>(eventCount, Allocator.Persistent);
        
        var timerEvent = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 5.0f }, 
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
        );
        
        events.Allocate(ref timerEvent);
        
        // 2. Initialize Timer with pointer
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);

        // 3. Assert
        Assert.AreEqual(0, timer.events[0].info.time);
        Assert.AreEqual(1, timer.events.currentSize);
        Assert.IsFalse(events.Active, "Since Timer is not active yet, the events should not be active.");
        Assert.IsFalse(timer.events.Active);
        Assert.IsTrue(Mathf.Approximately(timer.events[0].info.triggerTime, 5.0f));
        Assert.AreEqual(TimerEventType.OneShotTimerKeepsTicking, timer.events[0].type);
    
        // 4. Manual Cleanup
        events.Dispose();
    }

    [Test]
    public void Test2_AddAndPlayTimer()
    {
        Debug.Log("[DEBUG_LOG] Starting Test_AddAndPlayTimer");
        int eventCount = 0;
        int timerId = -1;
        var events = new Data<TimerEvent>(eventCount, Allocator.Temp);
        
        ProTimer timer = new ProTimer(TickMath.Add, ref events);

        // Test Add
        timerId = TimerStack.AddTimer(ref timer);
        Debug.Log($"[DEBUG_LOG] Added timer id: {timerId}");
        Assert.GreaterOrEqual(timerId, 0);

        // Test Play (Verify it doesn't crash and sets active state)
        TimerStack.StartTimer(timerId);
        Debug.Log("[DEBUG_LOG] Played timer");
        
        events.Dispose();
    }

    [Test]
    public void Test3_TickDeltaTimeAndTrigger()
    {
        int eventCount = 1;
        var events = new Data<TimerEvent>(eventCount, Allocator.Persistent);
            
        var timerEvent = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f }, 
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
        );
            
        events.Allocate(ref timerEvent);

        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.StartTimer(id);

        TimerStack.TickActivesDebug(0.5f);
        Assert.IsFalse(SomeTriggerLogic._eventTriggered, "Event should not trigger at 0.5s");
        Assert.IsFalse(SomeTriggerLogic._removeTriggered, "Remove should not trigger at 0.5s");

        TimerStack.TickActivesDebug(0.6f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "Event should trigger at 1.1s");

        events.Dispose();
    }
    [Test]
    public void Test4_MultiEventSequentialTriggering()
    {
        // Add a timer with two events: Event A at 1.0s and Event B at 2.0s
        int eventCount = 2;
        var events = new Data<TimerEvent>(eventCount, Allocator.Persistent);
    
        var eventA = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f }, 
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
            );
        
        var eventB = TimerEvent.NoData(   
            new TimerPredicateInfo { time = 0, triggerTime = 2.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
            );
    
        events.Allocate(ref eventA);
        events.Allocate(ref eventB);
    
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.StartTimer(id);
    
        TimerStack.TickActivesDebug(1.1f);
        Assert.AreEqual(1, SomeTriggerLogic._triggerCount);
    
        TimerStack.TickActivesDebug(1.0f);
        Assert.AreEqual(2, SomeTriggerLogic._triggerCount);
    
        events.Dispose();
    }
    
    [Test]
    public void Test5_CountdownLogic()
    {
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        
        var timerEvent = TimerEvent.NoData(
            new TimerPredicateInfo { time = 5.0f, triggerTime = 0.0f },
            ref ProTimersPredicates.IsLessThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
        );
        events.Allocate(ref timerEvent);
    
        ProTimer timer = new ProTimer(TickMath.Subtract, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.StartTimer(id);
    
        TimerStack.TickActivesDebug(4.0f); // Time becomes 1.0
        Assert.IsFalse(SomeTriggerLogic._eventTriggered, "Event should not trigger yet (time=1.0)");
    
        TimerStack.TickActivesDebug(2.0f); // Time becomes -1.0
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "Event should trigger (time=-1.0)");
    
        events.Dispose();
    }
    
    [Test]
    public void Test6_PausingAndResuming()
    {
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        
        var timerEvent = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
        );
        events.Allocate(ref timerEvent);
    
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        
        TimerStack.StartTimer(id);
        TimerStack.TickActivesDebug(0.5f);
        Assert.IsFalse(SomeTriggerLogic._eventTriggered);
    
        TimerStack.StopTimer(id);
        TimerStack.TickActivesDebug(1.0f); 
        Assert.IsFalse(SomeTriggerLogic._eventTriggered, "Timer should not have progressed while stopped");
        
        TimerStack.StartTimer(id);
        TimerStack.TickActivesDebug(0.6f); // Total 1.1
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "Timer should have resumed and triggered");
    
        events.Dispose();
    }
    
    [Test]
    public void Test7_ImmediateTrigger()
    {
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        
        var timerEvent = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 0.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
        );
        events.Allocate(ref timerEvent);
    
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.StartTimer(id);
    
        TimerStack.TickActivesDebug(0.001f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "Zero duration timer should trigger immediately");
    
        events.Dispose();
    }
    
    [Test]
    public void Test8_LargeDeltaTimeOvershoot()
    {
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        
        var timerEvent = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
        );
        
        events.Allocate(ref timerEvent);
    
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.StartTimer(id);
    
        TimerStack.TickActivesDebug(100.0f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "Large delta time should trigger the event");
    
        events.Dispose();
    }
    
    [Test]
    public void Test9_DataStabilityAndIDReuse()
    {
        for (int i = 0; i < 10; i++)
        {
            var events = new Data<TimerEvent>(1, Allocator.Temp);
            
            var timerEvent = TimerEvent.NoData(
                new TimerPredicateInfo { time = 0, triggerTime = 0.1f },
                ref ProTimersPredicates.IsGreaterThanOrEqualTo,
                ref SomeTriggerLogic.TriggeredOperation,
                TimerEventType.OneShotTimerKeepsTicking
            );
            events.Allocate(ref timerEvent);
            ProTimer timer = new ProTimer(TickMath.Add, ref events);
            int id = TimerStack.AddTimer(ref timer);
            Assert.GreaterOrEqual(id, 0);
            events.Dispose();
        }
    }
    
    [Test]
    public void Test10_MultipleTimersIndependentTicking()
    {
        var events1 = new Data<TimerEvent>(1, Allocator.Persistent);
        var timerEvent1 = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
            );
        events1.Allocate(ref timerEvent1);
        ProTimer timer1 = new ProTimer(TickMath.Add, ref events1);
        int id1 = TimerStack.AddTimer(ref timer1);
    
        var events2 = new Data<TimerEvent>(1, Allocator.Persistent);
        var timerEvent2 = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 2.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
        );
        events2.Allocate(ref timerEvent2);
        ProTimer timer2 = new ProTimer(TickMath.Add, ref events2);
        int id2 = TimerStack.AddTimer(ref timer2);
    
        TimerStack.StartTimer(id1);
        TimerStack.StartTimer(id2);
    
        TimerStack.TickActivesDebug(1.1f);
        Assert.AreEqual(1, SomeTriggerLogic._triggerCount, "Only first timer should have triggered");
    
        TimerStack.TickActivesDebug(1.0f);
        Assert.AreEqual(2, SomeTriggerLogic._triggerCount, "Both timers should have triggered");
    
        events1.Dispose();
        events2.Dispose();
    }
    
    [Test]
    public void Test11_TimerWithNoEvents()
    {
        var events = new Data<TimerEvent>(0, Allocator.Persistent);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.StartTimer(id);
    
        Assert.DoesNotThrow(() => TimerStack.TickActivesDebug(1.0f), "Ticking a timer with no events should not throw");
        
        events.Dispose();
    }
    
    [Test]
    public void Test12_MixedOneShotAndRepeatingEvents()
    {
        var events = new Data<TimerEvent>(2, Allocator.Persistent);
        
        // One-shot
        var oneShot = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
        );
        
        // Continuous
        var continuous = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.Repeating
            );
    
        events.Allocate(ref oneShot);
        events.Allocate(ref continuous);
    
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.StartTimer(id);
    
        TimerStack.TickActivesDebug(1.1f);
        Assert.AreEqual(2, SomeTriggerLogic._triggerCount, "Both events should trigger first time");
    
        TimerStack.TickActivesDebug(1.1f);
        Assert.AreEqual(3, SomeTriggerLogic._triggerCount, "Timer Continues evaluating, second event (Repeating) should trigger");
    
        events.Dispose();
    }
    
    
    [Test]
    public unsafe void Test13_TriggeredDataPtrMutation()
    {
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        int testData = 10;
        
        var timerEvent = TimerEvent.WithData(
            new TimerPredicateInfo { time = 0, triggerTime = 0.5f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.MutateDataOperation, 
            (IntPtr)(&testData),
            TimerEventType.OneShotTimerKeepsTicking
        );
        
        Debug.Log("IntPtr is : " + timerEvent.finishedData);
        
        events.Allocate(ref timerEvent);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.StartTimer(id);
    
        TimerStack.TickActivesDebug(1.0f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "Callback should have been triggered");
        Assert.AreEqual(110, testData, "Data should have been mutated by the callback (10 + 100)");
    
        events.Dispose();
    }
    
    [Test]
    public void Test15_StopTimerStopsEntireTimer_MultiEventOrdering()
    {
        var events = new Data<TimerEvent>(3, Allocator.Persistent);

        // Fires at t = 1.0
        var sameTimeEvent = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.StopTimer
        );

        // Also fires at t = 1.0 (same frame as first)
        var secondEvent = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
        );

        // Should NOT fire because timer stops at same time
        var lateEvent = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 2.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
        );

        events.Allocate(ref sameTimeEvent);
        events.Allocate(ref secondEvent);
        events.Allocate(ref lateEvent);

        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.StartTimer(id);

        TimerStack.TickActivesDebug(1.0f);

        // First event fires
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "First event should fire at t=1.0");

        // StopTimer event should still be evaluated in same tick pass
        // (depends on ordering, but should NOT affect already executed events)

        Assert.AreEqual(1.0f, TimerStack.timers[id].events[0].info.time, 0.001f);

        // Second tick should not progress timer anymore
        TimerStack.TickActivesDebug(1.0f);

        Assert.IsFalse(
            TimerStack.timers.GetWrapper(id).Active.active,
            "Timer should be stopped and no longer active"
        );

        events.Dispose();
    }
    
    [Test]
    public void Test16_RepeatingRepeats()
    {
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        var timerEvent = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.Repeating
        );
        events.Allocate(ref timerEvent);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.StartTimer(id);
    
        TimerStack.TickActivesDebug(1.1f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered);
        Assert.AreEqual(1, SomeTriggerLogic._triggerCount);
        Assert.IsTrue(TimerStack.timers[id].events.GetWrapper(0).Active, "Event should remain active because keepEvaluatingEvents is true");
    
        TimerStack.TickActivesDebug(1.1f);
        Assert.AreEqual(2, SomeTriggerLogic._triggerCount, "Event should trigger again if it keeps ticking");
    
        TimerStack.TickActivesDebug(0.2f);
        Assert.AreEqual(2, SomeTriggerLogic._triggerCount, "Repeat Not There Yet");
        
        TimerStack.TickActivesDebug(1.1f);
        Assert.AreEqual(3, SomeTriggerLogic._triggerCount, "Event should trigger again if it keeps ticking");


        events.Dispose();
    }
    
    [Test]
    public void Test17_OneShotsDeactivateEventAfterTrigger()
    {
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        var timerEvent = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.OneShotTimerKeepsTicking
        );
        events.Allocate(ref timerEvent);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.StartTimer(id);
    
        TimerStack.TickActivesDebug(1.1f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered);
        Assert.AreEqual(1, SomeTriggerLogic._triggerCount);
        Assert.IsFalse(TimerStack.timers[id].events.GetWrapper(0).Active.active, "Event should be deactivated after trigger when keepEvaluatingEvents is false");
    
        TimerStack.TickActivesDebug(1.0f);
        Assert.AreEqual(1, SomeTriggerLogic._triggerCount, "Event should NOT trigger again");
    
        events.Dispose();
    }
    
    [Test]
    public unsafe void Test18_TimerSelfRemoval()
    {
        // This test simulates how a timer can stop itself using a logic operation
        // Since we now use a static override, we override it for the test
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
            
        var timerEvent = TimerEvent.NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f },
            ref ProTimersPredicates.IsGreaterThanOrEqualTo,
            ref SomeTriggerLogic.TriggeredOperation,
            TimerEventType.StopTimer
        );
            
        events.Allocate(ref timerEvent);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
            
        TimerStack.StartTimer(id);
            
        TimerStack.TickActivesDebug(1.1f);
            
        Assert.IsFalse(TimerStack.timers.GetWrapper(id).Active.active, "Timer should be stopped in the stack");
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "OnFinishedOperation should still run");
    
        events.Dispose();
    }
}