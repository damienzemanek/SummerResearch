using System;
using ProArchitecture.Data;
using ProArchitecture.Logic;
using ProArchitecture.Predicates;
using Unity.Collections;
using UnityEngine;


namespace ProTimers
{
    public enum TickMath
    {
        Add,
        Subtract
    }
    
    public struct TimerPredicateInfo
    {
        public float time;
        public float triggerTime;
        public TimerPredicateInfo(float _time, float _triggerTime) : this()
        {
            time = _time;
            triggerTime = _triggerTime;
        }
    }
    
    public static unsafe class ProTimersPredicates
    {
        public static Predicate IsLessThan() => new Predicate(&isLessThan);
        static bool isLessThan(void* ptr) {
            var info = (TimerPredicateInfo*)ptr;
            return info->time < info->triggerTime;
        }
        
        public static Predicate IsGreaterThan() => new Predicate(&isGreaterThan);
        static bool isGreaterThan(void* ptr)
        {
            var info = (TimerPredicateInfo*)ptr;
            return info->time > info->triggerTime;
        }
        
        public static Predicate IsGreaterThanOrEqualTo() => new Predicate(&isGreaterThanOrEqualTo);
        static bool isGreaterThanOrEqualTo(void* ptr)
        {
            var info = (TimerPredicateInfo*)ptr;
            return info->time >= info->triggerTime;
        }
        
        public static Predicate IsLessThanOrEqualTo() => new Predicate(&isLessThanOrEqualTo);
        static bool isLessThanOrEqualTo(void* ptr)
        {
            var info = (TimerPredicateInfo*)ptr;
            return info->time <= info->triggerTime;
        }
        
    }

    /// <summary>
    /// DataEvents live on the ProTimer, which lives in the static TimerStack
    /// Ensure that you do not allocated Persistent for your temporary Data<TimerEvent> pass through
    /// </summary>
    public struct ProTimer
    {
        public Data<TimerEvent> events;  
        public readonly TickMath math;
        
        public ProTimer(TickMath _math, ref Data<TimerEvent> _events)
        {
            math = _math;
            events = _events;
        }
    }

    public unsafe struct TimerEvent
    {
        public ByteBool keepTicking;

        public TimerPredicateInfo info;
        public Predicate predicate;
        public LogicOperation<IntPtr>* onFinished; 
        public ref LogicOperation<IntPtr> OnFinished => ref *onFinished;
        public IntPtr triggeredDataPtr;

        public static TimerEvent NoData(TimerPredicateInfo _info, Predicate _predicate,
            ref LogicOperation<IntPtr> _onFinished, bool keepTickingAfterEventTriggered)
        {
            return new TimerEvent()
            {
                info = _info,
                predicate = _predicate,
                onFinished = (LogicOperation<IntPtr>*)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref _onFinished),
                keepTicking = new ByteBool(keepTickingAfterEventTriggered),
                triggeredDataPtr = IntPtr.Zero
            };
        }

        public static TimerEvent WithData(TimerPredicateInfo _info, Predicate _predicate,
            ref LogicOperation<IntPtr> _onFinished, bool keepTickingAfterEventTriggered, IntPtr dataPtr)
        {
            return new TimerEvent()
            {
                info = _info,
                predicate = _predicate,
                onFinished = (LogicOperation<IntPtr>*)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref _onFinished),
                keepTicking = new ByteBool(keepTickingAfterEventTriggered),
                triggeredDataPtr = dataPtr
            };
        }
        
        // Want the callback to do something when it ends
        // Want the callback to inactive itself on the TimerStack
        public bool IsTriggered => predicate.Evaluate(ref info);
    }

    // Register 
    // Idles dont poll
    // Delegate* wrapped struct timer
    
    public static class TimerStack
    {
        // Timer `Playing` will rely on active state on Data index
        static Data<ProTimer> timers;

        static TimerStack() => Reset();
        public static void Reset()
        {
            if (timers.Active) timers.Dispose();
            timers = new Data<ProTimer>(10000, Allocator.Persistent);
        }

        public static int AddTimer(ref ProTimer _timer)
        {
            timers.Allocate(ref _timer, out var id);
            StopTimer(id);
            return id;
        }

        public static void StartTimer(int id)
        {
            timers.GetWrapper(id).Active.Set(true);
        }

        public static void StopTimer(int id)
        {
            timers.GetWrapper(id).Active.Set(false);
        }

        public static void TickActives()
        {
            Batcher.Process(ref timers, TimerStackLogics.TickTimerLogics);
        }
        
        public static void TickActivesDebug(float deltaTime)
        {
            TimerStackLogics.CurrentDeltaTime = deltaTime; 
            Batcher.Process(ref timers, TimerStackLogics.TickTimerLogics);
        }
    }

    // This is a nested operation call
    // TickOperation calls the delegate inside of TimerEvent if the timer event is triggered
    // for each `TimerEvent` in the ProTimer
    public static unsafe class TimerStackLogics
    {
        public static bool isTesting = false;
        public static float CurrentDeltaTime; // Temporary storage for the batch process
        
        public static LogicOperation<ProTimer> tickTimerOperation = new(&TickTimerRun, &TickTimerShouldRun);
        static bool TickTimerShouldRun(ProTimer* timer) => true;
        static void TickTimerRun(ProTimer* timer)
        {
            float dt = isTesting ? CurrentDeltaTime : Time.deltaTime;
            if (timer->math == TickMath.Subtract) dt = -dt;

            for (int i = 0; i < timer->events.currentSize; i++)
            {
                if (!timer->events.GetWrapper(i).Active) continue;
                
                ref var timerEvent = ref timer->events[i]; 
                timerEvent.info.time += dt;
                
                if(!timerEvent.IsTriggered) continue;
                if (!timerEvent.OnFinished.ShouldRun(in timerEvent.triggeredDataPtr)) continue;
                if (timerEvent.keepTicking == false) timer->events.GetWrapper(i).Active.Set(false);
                timerEvent.OnFinished.Run(ref timerEvent.triggeredDataPtr);
                
            }
        }
        public static Logics<ProTimer> TickTimerLogics = new(ref tickTimerOperation);
    }
    
    
    
    

    
}


