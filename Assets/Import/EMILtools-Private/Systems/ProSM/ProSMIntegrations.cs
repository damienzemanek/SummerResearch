using System;
using System.Runtime.CompilerServices;
using System.Timers;
using ProArchitecture.Data;
using ProArchitecture.Logic;
using ProArchitecture.Predicates;
using ProSM;
using UnityEngine;
using ProTimers;
using Unity.Collections;

namespace ProSM
{

    public static unsafe class Transitioner<TData> where TData : unmanaged
    {
        public static LogicOperation<IntPtr> TransitionOperation = new (&Transition, &AlwaysRuns);
        static bool AlwaysRuns(IntPtr* data) => true;
        static void Transition(IntPtr* data)
        {
            ref Transition transition = ref IntPtrPtrTo<Transition>.GetRef(data);
            ref ProSM<TData> fsm = ref IntPtrTo<ProSM<TData>>.GetRef(ref transition.fsm);
            bool didTransition = fsm.TryPollDurationTransitionsOnLayer(ref fsm.layers[transition.layer], out int nextState);
            ref TData fetchDataRef = ref IntPtrTo<TData>.GetRef(ref transition.dataFetchLocationOnComplete);
            if(didTransition) fsm.TransitionOnLayer_CallExitEnter(transition.layer, nextState, ref fetchDataRef);
            else Debug.LogWarning("No Transition Found when Timer transitioned");
            
            Debug.Log("TRANSITIONING");
        }
    }
    
    public static unsafe class ProSMxProTimersIntegrationLogic
    {
        
        public static LogicOperation<IntPtr> RemoveSelfFromTimerStackOperation = new (&RemoveSelfFromTimerStackRun, &AlwaysRuns);
        static void RemoveSelfFromTimerStackRun(IntPtr* data)
        {
            ref Transition transition = ref IntPtrPtrTo<Transition>.GetRef(data);
            transition.durationMet.Set(true);
            TimerStack.StopTimer(transition.timerStackRemovalIndex);
        }
        static bool AlwaysRuns(IntPtr* data) => true;

    }
    
    public static partial class ProSMLogic
    {
        static unsafe bool TrueCondition(void* ptr) => true;

        public static void AddDirectTimedTransition<TStates, TData>(
            this ref ProSM<TData> fsm, 
            int layerIndex,
            TStates from, 
            TStates to,
            ref TData dataFetchLocationOnComplete,
            float duration)    
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
        
            unsafe
            {
                 Predicate AlwaysTrue = new Predicate(&TrueCondition);
                 var tempTransition = new Transition((short)toIndex, ref AlwaysTrue, true);
                 tempTransition.dataFetchLocationOnComplete = (IntPtr)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref dataFetchLocationOnComplete);
                 fsm.layers[layerIndex].states[fromIndex].transitions.Allocate(ref tempTransition, out int transitionIndex);
                 
                 ref var storedTransition = ref fsm.layers[layerIndex].states[fromIndex].transitions.Get(transitionIndex);
                 var events = new Data<TimerEvent>(1, Allocator.Temp);
                 var removeSelfFromStackEvent = TimerEvent.WithData(
                     new TimerPredicateInfo(0, duration),
                     ProTimersPredicates.IsGreaterThanOrEqualTo(),
                     ref ProSMxProTimersIntegrationLogic.RemoveSelfFromTimerStackOperation,
                     ref Transitioner<TData>.TransitionOperation,
                     false,
                     (IntPtr)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref storedTransition)
                 );
                 
                 events.Allocate(ref removeSelfFromStackEvent);
                 var timer = new ProTimer(TickMath.Add, ref events);
                 int id = TimerStack.AddTimer(ref timer);
                 storedTransition.timerStackRemovalIndex = id;
                
                 storedTransition.layer = layerIndex;
                 storedTransition.fsm = (IntPtr)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref fsm);
                
                 TimerStack.StartTimer(id);
            }
        }
    }

}
