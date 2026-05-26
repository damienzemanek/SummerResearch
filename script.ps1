$content = @"
using System;
using NUnit.Framework;
using ProSM.DataArchitecture;
using Unity.Collections;
using UnityEngine;
using ProTimers;
using Unity.Collections.LowLevel.Unsafe;
using ProSM.LogicArchitecture;

public class ProTimerTestSuite
{
    static unsafe class SomeTriggerLogic
    {
        public static bool _eventTriggered = false;
        public static int _triggerCount = 0;
        public static IntPtr dummyPtr = IntPtr.Zero;
        public static int lastReceivedValue = 0;
        
        static void SomeLogicRun(IntPtr* ptr)
        {
            _eventTriggered = true;
            _triggerCount++;
            if (ptr != null && *ptr != IntPtr.Zero)
            {
                lastReceivedValue = *(int*)*ptr;
            }
        }
        static bool SomeLogicShouldRun(IntPtr* ptr) => true;
        public static LogicOperation<IntPtr> TriggeredOperation = new(&SomeLogicRun, &SomeLogicShouldRun);
    }

    [SetUp]
    public unsafe void Setup()
    {
        TimerStack.Reset();
        TimerStackLogics.isTesting = true;
        SomeTriggerLogic._eventTriggered = false;
        SomeTriggerLogic._triggerCount = 0;
        SomeTriggerLogic.lastReceivedValue = 0;
        fixed (IntPtr* p = &SomeTriggerLogic.dummyPtr)
        {
            SomeTriggerLogic.dummyPtr = (IntPtr)p;
        }
    }

    [Test]
    public void Test1_TimerInitialization()
    {
        int eventCount = 1;
        var events = new Data<TimerEvent>(eventCount, Allocator.Persistent);
        var timerEvent = TimerEvent.FireAndForget_NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 5.0f }, 
            ProTimersPredicates.IsGreaterThanOrEqualTo(),
            ref SomeTriggerLogic.TriggeredOperation,
            false
        );
        events.Allocate(ref timerEvent);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        Assert.AreEqual(0, timer.events[0].info.time);
        Assert.AreEqual(1, timer.events.currentSize);
        Assert.IsTrue(events.Active);
        Assert.IsTrue(timer.events.Active);
        Assert.IsTrue(Mathf.Approximately(timer.events[0].info.triggerTime, 5.0f));
        Assert.IsTrue(timer.events[0].predicate.IsCreated);
        events.Dispose();
    }

    [Test]
    public void Test2_AddAndPlayTimer()
    {
        int eventCount = 0;
        var events = new Data<TimerEvent>(eventCount, Allocator.Temp);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        Assert.GreaterOrEqual(id, 0);
        TimerStack.PlayTimer(id);
        events.Dispose();
    }

    [Test]
    public void Test3_TickDeltaTimeAndTrigger()
    {
        int eventCount = 1;
        var events = new Data<TimerEvent>(eventCount, Allocator.Persistent);
        var timerEvent = TimerEvent.FireAndForget_NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f }, 
            ProTimersPredicates.IsGreaterThanOrEqualTo(),
            ref SomeTriggerLogic.TriggeredOperation,
            false
        );
        events.Allocate(ref timerEvent);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.PlayTimer(id);

        TimerStack.TickActives(0.5f);
        Assert.IsFalse(SomeTriggerLogic._eventTriggered, "Event should not trigger at 0.5s");

        TimerStack.TickActives(0.6f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "Event should trigger at 1.1s");
        events.Dispose();
    }

    [TearDown]
    public void TearDown()
    {
        TimerStackLogics.isTesting = false;
        SomeTriggerLogic._triggerCount = 0;
        SomeTriggerLogic._eventTriggered = false;
    }

    [Test]
    public void Test4_MultiEventSequentialTriggering()
    {
        int eventCount = 2;
        var events = new Data<TimerEvent>(eventCount, Allocator.Persistent);
        var eventA = TimerEvent.FireAndForget_NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f }, 
            ProTimersPredicates.IsGreaterThanOrEqualTo(),
            ref SomeTriggerLogic.TriggeredOperation,
            false
            );
        var eventB = TimerEvent.FireAndForget_NoData(   
            new TimerPredicateInfo { time = 0, triggerTime = 2.0f },
            ProTimersPredicates.IsGreaterThanOrEqualTo(),
            ref SomeTriggerLogic.TriggeredOperation,
            false
            );
        events.Allocate(ref eventA);
        events.Allocate(ref eventB);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.PlayTimer(id);

        TimerStack.TickActives(1.1f);
        Assert.AreEqual(1, SomeTriggerLogic._triggerCount);

        TimerStack.TickActives(1.0f);
        Assert.AreEqual(2, SomeTriggerLogic._triggerCount);
        events.Dispose();
    }

    [Test]
    public void Test5_CountdownLogic()
    {
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        var timerEvent = TimerEvent.FireAndForget_NoData(
            new TimerPredicateInfo { time = 5.0f, triggerTime = 0.0f },
            ProTimersPredicates.IsLessThanOrEqualTo(),
            ref SomeTriggerLogic.TriggeredOperation,
            false
        );
        events.Allocate(ref timerEvent);
        ProTimer timer = new ProTimer(TickMath.Subtract, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.PlayTimer(id);

        TimerStack.TickActives(4.0f);
        Assert.IsFalse(SomeTriggerLogic._eventTriggered, "Event should not trigger yet (time=1.0)");

        TimerStack.TickActives(2.0f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "Event should trigger (time=-1.0)");
        events.Dispose();
    }

    [Test]
    public void Test6_PausingAndResuming()
    {
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        var timerEvent = TimerEvent.FireAndForget_NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f },
            ProTimersPredicates.IsGreaterThanOrEqualTo(),
            ref SomeTriggerLogic.TriggeredOperation,
            false
        );
        events.Allocate(ref timerEvent);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.PlayTimer(id);
        TimerStack.TickActives(0.5f);
        Assert.IsFalse(SomeTriggerLogic._eventTriggered);
        TimerStack.StopTimer(id);
        TimerStack.TickActives(1.0f); 
        Assert.IsFalse(SomeTriggerLogic._eventTriggered, "Timer should not have progressed while stopped");
        TimerStack.PlayTimer(id);
        TimerStack.TickActives(0.6f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "Timer should have resumed and triggered");
        events.Dispose();
    }

    [Test]
    public void Test7_ImmediateTrigger()
    {
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        var timerEvent = TimerEvent.FireAndForget_NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 0.0f },
            ProTimersPredicates.IsGreaterThanOrEqualTo(),
            ref SomeTriggerLogic.TriggeredOperation,
            false
        );
        events.Allocate(ref timerEvent);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.PlayTimer(id);
        TimerStack.TickActives(0.001f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "Zero duration timer should trigger immediately");
        events.Dispose();
    }

    [Test]
    public void Test8_LargeDeltaTimeOvershoot()
    {
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        var timerEvent = TimerEvent.FireAndForget_NoData(
            new TimerPredicateInfo { time = 0, triggerTime = 1.0f },
            ProTimersPredicates.IsGreaterThanOrEqualTo(),
            ref SomeTriggerLogic.TriggeredOperation,
            false
        );
        events.Allocate(ref timerEvent);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.PlayTimer(id);
        TimerStack.TickActives(100.0f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered, "Large delta time should trigger the event");
        events.Dispose();
    }

    [Test]
    public void Test9_DataStabilityAndIDReuse()
    {
        for (int i = 0; i < 10; i++)
        {
            var events = new Data<TimerEvent>(1, Allocator.Temp);
            var timerEvent = TimerEvent.FireAndForget_NoData(
                new TimerPredicateInfo { time = 0, triggerTime = 0.1f },
                ProTimersPredicates.IsGreaterThanOrEqualTo(),
                ref SomeTriggerLogic.TriggeredOperation,
                false
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
        var event1 = TimerEvent.FireAndForget_NoData(new TimerPredicateInfo { time = 0, triggerTime = 1.0f }, ProTimersPredicates.IsGreaterThanOrEqualTo(), ref SomeTriggerLogic.TriggeredOperation, false);
        events1.Allocate(ref event1);
        ProTimer timer1 = new ProTimer(TickMath.Add, ref events1);
        int id1 = TimerStack.AddTimer(ref timer1);
        TimerStack.PlayTimer(id1);

        var events2 = new Data<TimerEvent>(1, Allocator.Persistent);
        var event2 = TimerEvent.FireAndForget_NoData(new TimerPredicateInfo { time = 0, triggerTime = 2.0f }, ProTimersPredicates.IsGreaterThanOrEqualTo(), ref SomeTriggerLogic.TriggeredOperation, false);
        events2.Allocate(ref event2);
        ProTimer timer2 = new ProTimer(TickMath.Add, ref events2);
        int id2 = TimerStack.AddTimer(ref timer2);
        TimerStack.PlayTimer(id2);

        TimerStack.TickActives(1.1f);
        Assert.AreEqual(1, SomeTriggerLogic._triggerCount, "Only first timer should trigger");

        TimerStack.TickActives(1.0f);
        Assert.AreEqual(2, SomeTriggerLogic._triggerCount, "Second timer should trigger");

        events1.Dispose();
        events2.Dispose();
    }

    [Test]
    public void Test11_TimerWithNoEvents()
    {
        var events = new Data<TimerEvent>(0, Allocator.Persistent);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.PlayTimer(id);
        Assert.DoesNotThrow(() => TimerStack.TickActives(1.0f));
        events.Dispose();
    }

    [Test]
    public void Test12_MixedOneShotAndContinuousEvents()
    {
        var events = new Data<TimerEvent>(2, Allocator.Persistent);
        var eventOneShot = TimerEvent.FireAndForget_NoData(new TimerPredicateInfo { time = 0, triggerTime = 1.0f }, ProTimersPredicates.IsGreaterThanOrEqualTo(), ref SomeTriggerLogic.TriggeredOperation, false);
        var eventContinuous = TimerEvent.FireAndForget_NoData(new TimerPredicateInfo { time = 0, triggerTime = 1.0f }, ProTimersPredicates.IsGreaterThanOrEqualTo(), ref SomeTriggerLogic.TriggeredOperation, true);
        
        events.Allocate(ref eventOneShot);
        events.Allocate(ref eventContinuous);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.PlayTimer(id);

        TimerStack.TickActives(1.1f);
        Assert.AreEqual(2, SomeTriggerLogic._triggerCount, "Both events should trigger first time");

        TimerStack.TickActives(1.0f);
        Assert.AreEqual(3, SomeTriggerLogic._triggerCount, "Only continuous event should trigger second time");
        
        events.Dispose();
    }

    [Test]
    public unsafe void Test13_TriggeredDataPtrUsage()
    {
        int myValue = 42;
        int* valPtr = &myValue;
        
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        var timerEvent = new TimerEvent()
        {
            info = new TimerPredicateInfo { time = 0, triggerTime = 0.5f },
            predicate = ProTimersPredicates.IsGreaterThanOrEqualTo(),
            onFinished = (LogicOperation<IntPtr>*)UnsafeUtility.AddressOf(ref SomeTriggerLogic.TriggeredOperation),
            keepTicking = false,
            triggeredDataPtr = (IntPtr)UnsafeUtility.AddressOf(ref valPtr)
        };
        
        events.Allocate(ref timerEvent);
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.PlayTimer(id);

        TimerStack.TickActives(1.0f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered);
        Assert.AreEqual(42, SomeTriggerLogic.lastReceivedValue);
        
        events.Dispose();
    }

    [Test]
    public void Test14_NegativeDeltaTimeHandling()
    {
        var events = new Data<TimerEvent>(1, Allocator.Persistent);
        var timerEvent = TimerEvent.FireAndForget_NoData(new TimerPredicateInfo { time = 2.0f, triggerTime = 1.0f }, ProTimersPredicates.IsLessThanOrEqualTo(), ref SomeTriggerLogic.TriggeredOperation, false);
        events.Allocate(ref timerEvent);
        
        ProTimer timer = new ProTimer(TickMath.Add, ref events);
        int id = TimerStack.AddTimer(ref timer);
        TimerStack.PlayTimer(id);

        TimerStack.TickActives(0.5f);
        Assert.IsFalse(SomeTriggerLogic._eventTriggered);

        TimerStack.TickActives(-2.0f);
        Assert.IsTrue(SomeTriggerLogic._eventTriggered);
        
        events.Dispose();
    }
}
"
Set-Content -Path Assets\Import\EMILtools-Private\Systems\ProTimers\Tests\ProTimerTestSuite.cs -Value $content -Encoding utf8
