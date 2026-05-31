using System;
using System.Runtime.CompilerServices;
using ProArchitecture.Data;
using ProArchitecture.Logic;
using ProArchitecture.Predicates;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace ProSM
{
    
    public static partial class ProSMLogic
    {
        // Initalization
        public static void Initialize<TData>(this ref ProSM<TData> fsm, int layerCount)  where TData : unmanaged
        {
            // Ensure TData is blittable (Input Validation)
            if (!UnsafeUtility.IsBlittable<TData>())
                throw new ArgumentException($"Type '{typeof(TData).Name}' is not blittable. TData must be a blittable type to ensure safety in unmanaged memory operations.");
                
            // Prevent memory leaks (Input Validation)
            if (fsm.layers.Active) 
                throw new InvalidOperationException("ProSM is already initialized. Dispose it before initializing again.");
        
            // Valid Layer Count (Input Validation)
            if (layerCount <= 0) 
                throw new ArgumentException("layerCount must be greater than 0.");

                
            fsm.layers = new Data<LayerData<TData>>(layerCount, Allocator.Persistent);
            // Creating the layer data
            for (int i = 0; i < layerCount; i++)
            {
                var layerData = new LayerData<TData>() { entryState = 0, currentState = 0, previousState = 0 };
                fsm.layers.Allocate(ref layerData, out int id);
            }
        }
            
        public static void InitLayer<TStates, TData>(this ref ProSM<TData> fsm, int layerIndex, TStates defaultState = default) 
            where TData : unmanaged
            where TStates : Enum
        {
            // Valid Layer (Input Validation)
            if (layerIndex < 0 || layerIndex >= fsm.layers.currentSize)
                throw new IndexOutOfRangeException($"Layer index {layerIndex} is out of bounds (Size: {fsm.layers.currentSize}).");
                
            ref var layerData = ref fsm.layers.Get(layerIndex);
                
            // Layer not already initialized (Input Validation)
            if(layerData.IsInitialized) 
                throw new InvalidOperationException("Layer already initialized, use another index");
                
            layerData.enumTypeId = typeof(TStates).GetHashCode(); // Store type hash
            var stateCount = Enum.GetValues(typeof(TStates)).Length;

            var entryState = Unsafe.As<TStates, int>(ref defaultState);
            Debug.Log(entryState);
            layerData.Init(stateCount, entryState);
            for (int i = 0; i < stateCount; i++)
            {
                var stateData = new StateData<TData>(i) { transitions = new Data<Transition>(10, Allocator.Persistent) };
                layerData.states.Allocate(ref stateData, out int _);
            }
        }
            
            
        // Add Transitions

        public static void AddAnyTransition<TStates, TData>(this ref ProSM<TData> fsm, int layerIndex, TStates to, ref Predicate predicate)
            where TData : unmanaged
        {
            // Valid Enum (Input Validation)
            if (typeof(TStates).GetHashCode() != fsm.layers[layerIndex].enumTypeId)
                throw new ArgumentException($"Enum type '{typeof(TStates).Name}' does not match the type used to initialize layer {layerIndex}.");
                
            // Valid To (Input Validation)
            int toIndex = Unsafe.As<TStates, int>(ref to);
            if (toIndex < 0 || toIndex >= fsm.layers[layerIndex].states.currentSize)
                throw new ArgumentOutOfRangeException(nameof(to), $"State {to} (index {toIndex}) does not exist in layer {layerIndex}.");
                
            var transition = new Transition(Unsafe.As<TStates, short>(ref to), ref predicate, false);
            fsm.layers[layerIndex].anyTransitions.Allocate(ref transition, out int _);
        }

        public static void AddDirectTransition<TStates, TData>(this ref ProSM<TData> fsm, int layerIndex, TStates from, TStates to, ref Predicate predicate)    
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

            var transition = new Transition((short)toIndex, ref predicate, false);
            fsm.layers[layerIndex].states[fromIndex].transitions.Allocate(ref transition, out int _);
        }
            
            
            

        /// <summary>
        /// Transition handling
        /// </summary>
        /// <param name="fsm"></param>
        /// <param name="data"></param>
        /// <typeparam name="TData"></typeparam>
        /// <returns></returns>
        public static void TryPollTransitions<TData>(this ref ProSM<TData> fsm, ref TData data)
            where TData : unmanaged
        {
            const int NO_NEW_LAYER_FOUND = -1;

            // Poll each layer -> Transition if found a next state
            for (int i = 0; i < fsm.layers.currentSize; i++)
            {
    #if ENABLE_UNITY_COLLECTIONS_CHECKS // Hot path so im using the compiler directive to remove it in builds
                // Check if layer has been entered (Sequential Input Validation)
                if (fsm.layers[i].currentState == -1) 
                    throw new InvalidOperationException($"Layer {i} has not been entered. Call Entry() before polling transitions.");
    #endif 
                fsm.TryPollTransitionsOnLayer(ref fsm.layers[i], ref data, out int nextState);
                if (nextState != NO_NEW_LAYER_FOUND) 
                    fsm.TransitionOnLayer_CallExitEnter(i, nextState, ref data);
            }
        }
            
        // public for testing
        public static bool TryPollTransitionsOnLayer<TData>(this ref ProSM<TData> fsm, ref LayerData<TData> layerdata, ref TData data, out int nextState)
            where TData : unmanaged
        {
            const int NO_NEW_LAYER_FOUND = -1;

                
            for(int i = 0; i < layerdata.anyTransitions.currentSize; i++)
            {
                ref var transition = ref layerdata.anyTransitions.Get(i);
                Debug.Log("[ANY] Eval: " + transition.condition.Evaluate(ref data) + " Time: " + layerdata.timeInState + " Dur: " + transition.hasDurationCondition);
                if (!transition.condition.Evaluate(ref data)) continue;
                if(layerdata.currentState == transition.to) continue;
                nextState = transition.to;
                return true;
            }
                
            ref var currentStateData = ref layerdata.states.Get(layerdata.currentState);
            for(int i = 0; i < currentStateData.transitions.currentSize; i++)        
            {
                ref var transition = ref currentStateData.transitions.Get(i);
                Debug.Log("[DIRECT] Eval: " + transition.condition.Evaluate(ref data) + " Time: " + layerdata.timeInState + " Dur: " + transition.hasDurationCondition);
                if (!transition.condition.Evaluate(ref data)) continue;
                if(layerdata.currentState == transition.to) continue;
                nextState = transition.to;
                return true;
            }
                
            nextState = NO_NEW_LAYER_FOUND;
            return false;
        }
        
        public static bool TryPollDurationTransitionsOnLayer<TData>(this ref ProSM<TData> fsm, ref LayerData<TData> layerdata, out int nextState)
            where TData : unmanaged
        {
            const int NO_NEW_LAYER_FOUND = -1;
            
            for(int i = 0; i < layerdata.anyTransitions.currentSize; i++)
            {
                ref var transition = ref layerdata.anyTransitions.Get(i);
                if(!transition.hasDurationCondition) continue;
                if(layerdata.currentState == transition.to) continue;

                // Stale Transition Check: Ensure the state we are transitioning FROM matches the current state
                // Note: For AnyTransitions, transition.from is -1, so we skip this check
                if (transition.from != -1 && layerdata.currentState != transition.from) continue;

                nextState = transition.to;
                return true;
            }
            
            ref var currentStateData = ref layerdata.states.Get(layerdata.currentState);
            for(int i = 0; i < currentStateData.transitions.currentSize; i++)        
            {
                ref var transition = ref currentStateData.transitions.Get(i);
                Debug.Log($"Transition: {i} hasDurationCondition? {transition.hasDurationCondition}");
                if(!transition.hasDurationCondition) continue;
                Debug.Log("PASS B");
                if(layerdata.currentState == transition.to) continue;

                // Stale Transition Check: Ensure the state we are transitioning FROM matches the current state
                if (layerdata.currentState != transition.from) continue;

                nextState = transition.to;
                Debug.Log("PASS C");
                return true;
            }
                
            nextState = NO_NEW_LAYER_FOUND;
            return false;
        }
        

        public static void TransitionOnLayer_CallExitEnter<TData>(this ref ProSM<TData> fsm, int layer, int nextState, ref TData data)
            where TData : unmanaged
        {
            //previous
            fsm.layers[layer].previousState = fsm.layers[layer].currentState;
            fsm.layers[layer].states[fsm.layers[layer].currentState].OnExitState.TryRunAllSequentially(ref data);
                
            //next
            fsm.layers[layer].currentState = nextState;
            fsm.layers[layer].states[nextState].OnEnterState.TryRunAllSequentially(ref data);
                
            fsm.layers[layer].timeInState = 0;
        }
            
            
            
        // Entry (Awake)
        public static void Entry<TData>(this ref ProSM<TData> fsm, ref TData data)
            where TData : unmanaged
        {
            // (Sequential Input Validation)
            if (!fsm.layers.Active || fsm.layers.currentSize == 0)
                throw new InvalidOperationException("ProSM Entry failed: No layers have been initialized. Call Initialize() and InitLayer() first.");
                
                
            for(int i = 0; i < fsm.layers.currentSize; i++)
            {
                ref var layerData = ref fsm.layers.Get(i);
                if(layerData.IsInitialized == false) 
                    throw new InvalidOperationException($"ProSM Entry failed: Layer {i} has not been initialized. Call InitLayer() before calling Entry().");
                layerData.currentState = layerData.entryState;
                fsm.layers[i].timeInState = 0;
                fsm.layers[i].states[layerData.currentState].OnEnterState.TryRunAllSequentially(ref data);
                // enter logic using StateLogics
            }
        }
            
        // State Ticks with internal TickLogicData creation
        public static void TickUpdate<TData>(this ref ProSM<TData> fsm, float deltaTime, Logics<TData> coreLogics, ref TData data) 
            where TData : unmanaged
        {
            // Create TickLogicData on the stack - pointer is guaranteed stable for the duration of this call
            var tickData = new TickLogic<TData>.TickLogicData<TData>(deltaTime, ref coreLogics, ref data);
        
            for (int i = 0; i < fsm.layers.currentSize; i++)
            {
                fsm.layers[i].timeInState += deltaTime;
                var currentState = fsm.layers[i].states[fsm.layers[i].currentState];
                currentState.OnUpdate.TryRunAllSequentially(ref tickData);
            }
        }

        public static void TickFixedUpdate<TData>(this ref ProSM<TData> fsm, float deltaTime, Logics<TData> coreLogics, ref TData data)
            where TData : unmanaged
        {
            // Create TickLogicData on the stack - pointer is guaranteed stable for the duration of this call
            var tickData = new TickLogic<TData>.TickLogicData<TData>(deltaTime, ref coreLogics, ref data);
        
            for (int i = 0; i < fsm.layers.currentSize; i++)
            {
                var currentState = fsm.layers[i].states[fsm.layers[i].currentState];
                currentState.OnFixedUpdate.TryRunAllSequentially(ref tickData);
            }
        }

        public static void TickLateUpdate<TData>(this ref ProSM<TData> fsm, float deltaTime, Logics<TData> coreLogics, ref TData data)
            where TData : unmanaged
        {
            // Create TickLogicData on the stack - pointer is guaranteed stable for the duration of this call
            var tickData = new TickLogic<TData>.TickLogicData<TData>(deltaTime, ref coreLogics, ref data);
        
            for (int i = 0; i < fsm.layers.currentSize; i++)
            {
                var currentState = fsm.layers[i].states[fsm.layers[i].currentState];
                currentState.OnLateUpdate.TryRunAllSequentially(ref tickData);
            }
        }
    }
}
