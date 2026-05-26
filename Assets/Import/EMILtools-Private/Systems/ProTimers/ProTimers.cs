using System;
using ProSM.DataArchitecture;
using ProSM.LogicArchitecture;
using ProSM.StateArchitecture;
using Unity.Collections;


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

    public unsafe struct ProTimer<T> where T : unmanaged
    {
        public float time;
        public T* data;
        public ref T Data => ref *data;
        public TickMath math;
        public NativeArray<TimerEvent<T>> events;
    }

    public struct TimerEvent<T> where T : unmanaged
    {
       // public ByteBool keepTicking;
        
        internal TimerPredicateInfo info;
        internal Predicate predicate;
        internal LogicOperation<T> OnTriggered;
        
        // Want the callback to do something when it ends
        // Want the callback to inactive itself on the TimerStack
        //public delegate*<void*, void*> cb_StopTicking;r
        public bool IsTriggered => predicate.Evaluate(ref info);
    }

    // Register 
    // Idles dont poll
    // Delegate* wrapped struct timer
    
    public static class TimerStack<T> where T : unmanaged 
    {
        // Timer `Playing` will rely on active state on Data index
        static Data<ProTimer<T>> timers;
        
        // PLay
        
        // Stop
        
        // Add

        public static int AddTimer(ref ProTimer<T> _timer)
        {
            timers.Allocate(ref _timer, out var id);
            StopTimer(id);
            return id;
        }

        public static void PlayTimer(int id) => timers.GetWrapper(id).Active.Set(true);
        public static void StopTimer(int id) => timers.GetWrapper(id).Active.Set(false);
    }

    // This is a nested opeartion call
    // TickOperation calls the Operation inside of TimerEvent if the timer event is triggered
    // for each `TimerEvent` in the ProTimer
    public static unsafe class TimerStackLogics<T> where T : unmanaged  
    {
        public static LogicOperation<TickLogic<ProTimer<T>>.TickData<ProTimer<T>>> TickOperation 
            = new(&TickRun, &TickShouldRun); 
        static bool TickShouldRun(TickLogic<ProTimer<T>>.TickData<ProTimer<T>>* tickData) => true;
        static void TickRun(TickLogic<ProTimer<T>>.TickData<ProTimer<T>>* _tickData)
        {
            ref var tickData = ref *_tickData;
            ref var timer = ref tickData.CoreData;
            timer.time += (timer.math == TickMath.Add) ? tickData.deltaTime : -tickData.deltaTime;
            for (int i = 0; i < timer.events.Length; i++)
            {
                var triggered = timer.events[i].IsTriggered;
                if(!triggered || !timer.events[i].OnTriggered.ShouldRun(in timer.Data)) continue;
                timer.events[i].OnTriggered.Run(ref timer.Data);
            }
        }
    }
    
    
    
    

    
}


