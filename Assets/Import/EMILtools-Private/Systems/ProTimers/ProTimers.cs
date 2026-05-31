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
        public static Predicate IsLessThan;
        static bool isLessThan(void* ptr) {
            var info = (TimerPredicateInfo*)ptr;
            return info->time < info->triggerTime;
        }

        public static Predicate IsGreaterThan;
        static bool isGreaterThan(void* ptr)
        {
            var info = (TimerPredicateInfo*)ptr;
            return info->time > info->triggerTime;
        }

        public static Predicate IsGreaterThanOrEqualTo;
        static bool isGreaterThanOrEqualTo(void* ptr)
        {
            var info = (TimerPredicateInfo*)ptr;
            return info->time >= info->triggerTime;
        }

        public static Predicate IsLessThanOrEqualTo;
        static bool isLessThanOrEqualTo(void* ptr)
        {
            var info = (TimerPredicateInfo*)ptr;
            return info->time <= info->triggerTime;
        }

        static ProTimersPredicates()
        {
            IsLessThan = new Predicate(&isLessThan);
            IsGreaterThan = new Predicate(&isGreaterThan);
            IsGreaterThanOrEqualTo = new Predicate(&isGreaterThanOrEqualTo);
            IsLessThanOrEqualTo = new Predicate(&isLessThanOrEqualTo);
        }
        
    }

    /// <summary>
    /// Static Procedural Timer System.
    /// Manages a global stack of timers using high-performance batch processing.
    ///
    /// Architecture & Design:
    /// - Data-Oriented: Timers live in a flat, contiguous stack (TimerStack) for cache efficiency.
    /// - Static Execution: Logic is decoupled from game objects; ticked via a central system.
    /// - Pointer-Based: Uses raw pointers for event execution to avoid overhead.
    ///
    /// Features/Configuration:
    /// - TickMath: Supports both incremental (Add) and decremental (Subtract) timing logic.
    /// - Event Types: 
    ///     - OneShotTimerKeepsTicking: Disables the event but keeps the parent timer active.
    ///     - Repeating: Resets elapsed time to zero upon triggering.
    ///     - StopTimer: Deactivates the entire timer on the stack after execution.
    /// - Predicate Integration: Flexible trigger conditions (Less than, Greater than, etc.).
    ///
    /// Memory Safety & Edge Cases:
    /// - [CRITICAL] Stack Reallocation: Adding timers inside a callback can trigger a stack resize. 
    ///   This reallocates the underlying array, potentially dangling pointers used in the current batch.
    /// - [CRITICAL] Data Lifetime: TimerEvents holding pointers to data (WithData) must ensure 
    ///   the target data outlives the timer. Storing pointers to stack-allocated variables is dangerous.
    /// - Capacity Management: The stack grows automatically but does not currently shrink; 
    ///   inactive timers are skipped but still iterated until Reset() is called.
    ///
    /// Usage:
    /// 1. Create a ProTimer with desired TickMath and a Data list of TimerEvents.
    /// 2. Use TimerStack.AddTimer(ref timer) to register it.
    /// 3. Call TimerStack.StartTimer(id) to begin ticking.
    /// 4. Tick the system via TimerStack.TickActives() in a central Update loop.
    /// </summary>
    public struct ProTimer
    {
        public Data<TimerEvent> events;  
        public readonly TickMath math;
        public int removalIndex;
        
        public ProTimer(TickMath _math, ref Data<TimerEvent> _events)
        {
            math = _math;
            events = _events;
            removalIndex = -99;

            events.Active.Set(false);
        }
    }

    public enum TimerEventType
    {
        OneShotTimerKeepsTicking,
        Repeating,
        StopTimer
    }
    
    public unsafe struct TimerEvent
    {
        public TimerEventType type;
        public TimerPredicateInfo info;
        public RefToStatic<Predicate> predicate;
        
        public LogicOperation<IntPtr>* onFinishedOperation; 
        public ref LogicOperation<IntPtr> OnFinishedOperation => ref *onFinishedOperation;
        
        public IntPtr finishedData;

        public static TimerEvent NoData(TimerPredicateInfo _info, ref Predicate _predicate,
            ref LogicOperation<IntPtr> _onFinished, TimerEventType _type)
        {
            return new TimerEvent()
            {
                info = _info,
                predicate = new RefToStatic<Predicate>(ref _predicate),
                onFinishedOperation = (LogicOperation<IntPtr>*)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref _onFinished),
                type = _type,
                finishedData = IntPtr.Zero,
            };
        }

        public static TimerEvent WithData(TimerPredicateInfo _info, ref Predicate _predicate,
            ref LogicOperation<IntPtr> _onFinished, IntPtr dataPtr, TimerEventType _type)
        {
            return new TimerEvent()
            {
                info = _info,
                predicate = new RefToStatic<Predicate>(ref _predicate),
                onFinishedOperation = (LogicOperation<IntPtr>*)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref _onFinished),
                type = _type,
                finishedData = dataPtr,
            };
        }
        
        // Want the callback to do something when it ends
        // Want the callback to inactive itself on the TimerStack
        public bool IsTriggered
        {
            get
            {
                bool ret = predicate.Evaluate(ref info);
                Debug.Log("Predicate Evaluation: " + ret + " time: " + info.time + " triggerTime: " + info.triggerTime + " | time >= triggerTime: " + (info.time >= info.triggerTime) + "");
                return ret;
            }
        }
        public void SetFinishedData(ref IntPtr data) => finishedData = data;
    }

    // Register 
    // Idles dont poll
    // Delegate* wrapped struct timer
    
    public static class TimerStack
    {
        // Timer `Playing` will rely on active state on Data index
        public static Data<ProTimer> timers;
        
        static TimerStack() => Reset();
        public static void Reset()
        {
            if (timers.Active) timers.Dispose();
            timers = new Data<ProTimer>(10000, Allocator.Persistent);
        }

        public static int AddTimer(ref ProTimer tempTimer)
        {
            timers.Allocate(ref tempTimer, out var id);
            timers[id].removalIndex = id; // has to be the indexed timer not the temp timer
            timers[id].events.Active.Set(false);

            Debug.Log($"Timer Added: {id}");
            return id;
        }

        public static void StartTimer(int id)
        {
            timers.GetWrapper(id).Active.Set(true);
            Debug.Log($"Timer Started: {id}");
        }

        public static void StopTimer(int id)
        {
            timers.GetWrapper(id).Active.Set(false);
            Debug.Log($"Timer Stopped: {id}");
        }

        public static void TickActives()
        {
            Batcher.Process(ref timers, TimerStackLogics.TickTimerLogics);
        }
        
        public static void TickActivesDebug(float deltaTime)
        {
            TimerStackLogics.CurrentDeltaTime = deltaTime; 
            Batcher.Process(ref timers, TimerStackLogics.TickTimerLogics);
            Debug.Log($"Ticked: {deltaTime}");
        }
    }

    // This is a nested operation call
    // TickOperation calls the delegate inside of TimerEvent if the timer event is triggered
    // for each `TimerEvent` in the ProTimer
    public static unsafe class TimerStackLogics
    {
        
        
        public static bool isTesting = false;
        public static float CurrentDeltaTime; // Temporary storage for the batch process
        
        public static Logics<ProTimer> TickTimerLogics = new(ref tickTimerOperation);
        public static LogicOperation<ProTimer> tickTimerOperation = new(&TickTimerRun, &TickTimerShouldRun);
        static bool TickTimerShouldRun(ProTimer* timer) => true;
        static void TickTimerRun(ProTimer* timer)
        {
            float dt = isTesting ? CurrentDeltaTime : Time.deltaTime;
            bool defferedDropTimerFromStackThisFrame = false;
            if (timer->math == TickMath.Subtract) dt = -dt;

            for (int i = 0; i < timer->events.currentSize; i++)
            {
                if (!timer->events.GetWrapper(i).Active) continue;
                ref var timerEvent = ref timer->events[i]; 
                timerEvent.info.time += dt;
                
                if(!timerEvent.IsTriggered) continue;

                if (timerEvent.type == TimerEventType.OneShotTimerKeepsTicking)
                    timer->events.GetWrapper(i).Active.Set(false);
                else if (timerEvent.type == TimerEventType.StopTimer)
                    defferedDropTimerFromStackThisFrame = true;
                else if (timerEvent.type == TimerEventType.Repeating)
                    timerEvent.info.time = 0;
                
                timerEvent.OnFinishedOperation.Run(ref timerEvent.finishedData);
            }
            
            if(defferedDropTimerFromStackThisFrame) TimerStack.StopTimer(timer->removalIndex);
        }
        
    }
    
    
    
    

    
}


