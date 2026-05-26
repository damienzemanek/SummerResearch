using System;
using System.Runtime.CompilerServices;
using ProArchitecture.Data;
using ProArchitecture.Logic;
using ProArchitecture.Predicates;
using ProSM;
using UnityEngine;
using ProTimers;
using Unity.Collections;

namespace ProSM
{
    

    public static unsafe class ProSMxProTimersIntegrationLogic<TData> where TData : unmanaged
    {
        
        public static LogicOperation<IntPtr> TransitionOperation = 
            new LogicOperation<IntPtr>(&TransitionRun, &TransitionShouldRun);
        
        static void TransitionRun(IntPtr* data)
        {
            IntPtr intptr = *data;
            Transition* transitionPtr = (Transition*)intptr;
            ref Transition transition = ref *transitionPtr;
            transition.durationConditionOverride.Set(true);
        }
        
        static bool TransitionShouldRun(IntPtr* data) => true;
    }
    
    public static partial class ProSMLogic
    {
        static unsafe bool TrueCondition(void* ptr) => true;

        public static void AddDirectTimedTransition<TStates, TData>(this ref ProSM<TData> fsm, int layerIndex, TStates from, TStates to, float duration)    
            where TData : unmanaged
        {
            // Valid Enum (Input Validation)
            if (typeof(TStates).GetHashCode() != fsm.layers[layerIndex].enumTypeId)
                throw new ArgumentException($"Enum type '{typeof(TStates).Name}' does not match the type used to initialize layer {layerIndex}.");
        
            // Valid From (Input Validation)
            int fromIndex = Unsafe.As<TStates, int>(ref from);
            if (fromIndex < 0 || fromIndex >= fsm.layers[layerIndex].states.currentSize)
                throw new ArgumentOutOfRangeException(nameof(from), $"State {from} (index {fromIndex}) does not exist in layer {layerIndex}.");
        
            // Valid To (Input Validation)
            int toIndex = Unsafe.As<TStates, int>(ref to);
            if (toIndex < 0 || toIndex >= fsm.layers[layerIndex].states.currentSize)
                throw new ArgumentOutOfRangeException(nameof(to), $"State {to} (index {toIndex}) does not exist in layer {layerIndex}.");
        
            //var transition = new Transition((short)toIndex, ref predicate);
            unsafe
            {
                 Predicate AlwaysTrue = new Predicate(&TrueCondition);
                 var transition = new Transition((short)toIndex, ref AlwaysTrue, false);
                 fsm.layers[layerIndex].states[fromIndex].transitions.Allocate(ref transition, out int _);

                 var events = new Data<TimerEvent>(1, Allocator.Temp);
                 var timerFinishEvent = TimerEvent.NoData(
                     new TimerPredicateInfo(0, duration),
                     ProTimersPredicates.IsGreaterThanOrEqualTo(),
                     ref ProSMxProTimersIntegrationLogic<Transition>.TransitionOperation,
                     false
                 );
                 events.Allocate(ref timerFinishEvent);
                 var timer = new ProTimer(TickMath.Add, ref events);
                 int id = TimerStack.AddTimer(ref timer);
                 TimerStack.StartTimer(id);
            }
        }
    }

}
