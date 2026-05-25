using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DataArchitecture;
using LogicArchitecture;
using ProSMLogic;
using StateArchitecture;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;

namespace ProceduralStateMachine
{
    
    
    // Separate Data Layer
    public struct LayerData<TData> where TData : unmanaged // Just the Layer data
    {
        const int ANY_TRANSITIONS_SIZE = 50;
        const int NOT_ENTERED_YET = -1;

        public byte isInitialized;
        public bool IsInitialized => isInitialized != 0;
        public int entryState;
        public int currentState;
        public int previousState;
        
        public Data<StateData<TData>> states;
        public Data<Transition> anyTransitions;

        public long enumTypeId; // hash of enum

        public float timeInState;

        public byte transitionEventFlagged;
        public bool IsTransitionEventFlagged => transitionEventFlagged != 0;

        public void Init(int statesSize, int _entryState = 0)
        {
            states = new Data<StateData<TData>>(statesSize, Allocator.Persistent);
            anyTransitions = new Data<Transition>(ANY_TRANSITIONS_SIZE, Allocator.Persistent);
            isInitialized = 1;
            entryState = _entryState;
            currentState = NOT_ENTERED_YET; // Call Entry() to set this
            timeInState = 0;
        }
    }

    // Basically the logic for ProSM
    public static class ProSMLogic
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
            
            var transition = new Transition(Unsafe.As<TStates, short>(ref to), ref predicate);
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

            var transition = new Transition((short)toIndex, ref predicate);
            fsm.layers[layerIndex].states[fromIndex].transitions.Allocate(ref transition, out int _);
        }
        
        // public static void AddDirectTimedTransition<TStates, TData>(this ref ProSM<TData> fsm, int layerIndex, TStates from, TStates to, float duration)    
        //     where TData : unmanaged
        // {
        //     // Valid Enum (Input Validation)
        //     if (typeof(TStates).GetHashCode() != fsm.layers[layerIndex].enumTypeId)
        //         throw new ArgumentException($"Enum type '{typeof(TStates).Name}' does not match the type used to initialize layer {layerIndex}.");
        //
        //     // Valid From (Input Validation)
        //     int fromIndex = Unsafe.As<TStates, int>(ref from);
        //     if (fromIndex < 0 || fromIndex >= fsm.layers[layerIndex].states.currentSize)
        //         throw new ArgumentOutOfRangeException(nameof(from), $"State {from} (index {fromIndex}) does not exist in layer {layerIndex}.");
        //
        //     // Valid To (Input Validation)
        //     int toIndex = Unsafe.As<TStates, int>(ref to);
        //     if (toIndex < 0 || toIndex >= fsm.layers[layerIndex].states.currentSize)
        //         throw new ArgumentOutOfRangeException(nameof(to), $"State {to} (index {toIndex}) does not exist in layer {layerIndex}.");
        //
        //     //var transition = new Transition((short)toIndex, ref predicate);
        //     unsafe
        //     {
        //         Predicate alwaysTrue = new Predicate(&TrueCondition);
        //         var transition = new Transition((short)toIndex, ref alwaysTrue, duration);
        //         fsm.layers[layerIndex].states[fromIndex].transitions.Allocate(ref transition, out int _);
        //         static bool TrueCondition(void* ptr) => true;
        //     }
        // }
        
        
        

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
                Debug.Log("[ANY] Eval: " + transition.condition.Evaluate(ref data) + " Time: " + layerdata.timeInState + " Dur: " + transition.duration);
                if (transition.condition.Evaluate(ref data) && layerdata.timeInState >= transition.duration)
                {
                    if(layerdata.currentState == transition.to) continue;
                    nextState = transition.to;
                    return true;
                }
            }
            
            ref var currentStateData = ref layerdata.states.Get(layerdata.currentState);
            for(int i = 0; i < currentStateData.transitions.currentSize; i++)        
            {
                ref var transition = ref currentStateData.transitions.Get(i);
                Debug.Log("[DIRECT] Eval: " + transition.condition.Evaluate(ref data) + " Time: " + layerdata.timeInState + " Dur: " + transition.duration);
                if (transition.condition.Evaluate(ref data) && layerdata.timeInState >= transition.duration)
                {
                    if(layerdata.currentState == transition.to) continue;
                    nextState = transition.to;
                    return true;
                }
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
        
        // State Ticks with internal TickData creation
        public static void TickUpdate<TData>(this ref ProSM<TData> fsm, float deltaTime, Logics<TData> coreLogics, ref TData data) 
            where TData : unmanaged
        {
            // Create TickData on the stack - pointer is guaranteed stable for the duration of this call
            var tickData = new TickLogic<TData>.TickData<TData>(deltaTime, coreLogics, ref data);
    
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
            // Create TickData on the stack - pointer is guaranteed stable for the duration of this call
            var tickData = new TickLogic<TData>.TickData<TData>(deltaTime, coreLogics, ref data);
    
            for (int i = 0; i < fsm.layers.currentSize; i++)
            {
                var currentState = fsm.layers[i].states[fsm.layers[i].currentState];
                currentState.OnFixedUpdate.TryRunAllSequentially(ref tickData);
            }
        }

        public static void TickLateUpdate<TData>(this ref ProSM<TData> fsm, float deltaTime, Logics<TData> coreLogics, ref TData data)
            where TData : unmanaged
        {
            // Create TickData on the stack - pointer is guaranteed stable for the duration of this call
            var tickData = new TickLogic<TData>.TickData<TData>(deltaTime, coreLogics, ref data);
    
            for (int i = 0; i < fsm.layers.currentSize; i++)
            {
                var currentState = fsm.layers[i].states[fsm.layers[i].currentState];
                currentState.OnLateUpdate.TryRunAllSequentially(ref tickData);
            }
        }
    }
    
    
    // To Implement: Make Predicate static on the static implementation level
    

    /// <summary>
    /// Instance Defined Procedural State Machine Handle
    /// Layer enums to be declared by the user
    ///
    /// Features/Configuration:
    /// - Any Transitions are evaluated first, followed by direct transitions
    /// - Self Transitions are explicitly ignored (skipped)
    /// - Disposal is idempotent and safe to call multiple times
    /// - Blittable and unmanaged compatible for high performance
    /// - Multi-layered support for parallel state logic
    ///
    /// Usage:
    /// - Initialize() with the number of layers
    /// - InitLayer() for each layer with the default state
    /// - Add_AnyTransition() for any transitions
    /// - Add_DirectTransition() for direct transitions
    /// - Entry() to start the FSM
    /// - TickUpdate(), TickFixedUpdate(), TickLateUpdate() to call tick logic
    /// - TryPollTransitions() to check for transitions and update the FSM
    /// - Dispose() to clean up resources
    ///
    /// Validation / Exception Handling:
    /// - Throws ArgumentException if TData is not a blittable type
    /// - Throws InvalidOperationException if Entry() is called without initialized layers
    /// - Throws InvalidOperationException if an allocated layer was never set up via InitLayer()
    /// - Throws InvalidOperationException if TryPollTransitions() is called before Entry() (Unity checks only)
    /// - Throws ArgumentException if enum types do not match the layer's initialized type
    /// - Throws ArgumentOutOfRangeException if state indices are invalid for the layer
    /// - Throws InvalidOperationException if Initialize() is called on an already active FSM
    /// </summary>
    /// <typeparam name="TData">Unmanaged data structure passed through all state logic</typeparam>
    public struct ProSM<TData> where TData : unmanaged
    {
        // make internal later (public rn for testing)
        public Data<LayerData<TData>> layers;
        
        public void Dispose()
        {
            if (!layers.Active)
            {
                Debug.LogWarning("(IDEMPOTENT ACTION) ProSM Dispose called, but ProSM was not initialized or was already disposed. No action taken.");
                return;
            }
            
            for (int i = 0; i < layers.currentSize; i++)
            {
                for(int z = 0; z < layers[i].states.currentSize; z++)
                    layers[i].states[z].transitions.Dispose();
                layers[i].states.Dispose();
                layers[i].anyTransitions.Dispose();
            }
            layers.Dispose();
        }
    }
}
