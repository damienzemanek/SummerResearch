using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DataArchitecture;
using LogicArchitecture;
using ProSMLogic;
using StateArchitecture;
using Unity.Collections;
using UnityEngine;

namespace ProceduralStateMachine
{
    
    // Separate Data Layer
    public struct LayerData<TData> where TData : unmanaged // Just the Layer data
    {
        const int ANY_TRANSITIONS_SIZE = 50;
        const int NOT_ENTERED_YET = -1;

        public bool isInitialized;
        public int entryState;
        public int currentState;
        public int previousState;
        public Data<StateData<TData>> states;
        public Data<Transition> anyTransitions;
        
        // time in state... etc..

        public void Init(int statesSize, int _entryState = 0)
        {
            states = new Data<StateData<TData>>(statesSize, Allocator.Persistent);
            anyTransitions = new Data<Transition>(ANY_TRANSITIONS_SIZE, Allocator.Persistent);
            isInitialized = true;
            entryState = _entryState;
            currentState = NOT_ENTERED_YET; // Call Entry() to set this
        }
    }
    
    // Separate Data Layer Handling
    // public static class LayerLogic<TStates> where TStates : Enum // Handles the Layer data
    // {
    //     static LayerLogic()
    //     {
    //         if (Enum.GetUnderlyingType(typeof(TStates)) != typeof(int)) throw new InvalidOperationException($"{typeof(TStates).Name} must have an underlying type of int.");
    //     }
    //     
    //     public static TStates GetState(ref LayerData data) => Unsafe.As<int, TStates>(ref data.currentState);
    //     public static TStates GetEntryState(ref LayerData data) => Unsafe.As<int, TStates>(ref data.entryState);
    //     public static TStates GetPreviousState(ref LayerData data) => Unsafe.As<int, TStates>(ref data.previousState);
    //
    //     public static void Transition(ref LayerData data, TStates to)
    //     {
    //         data.previousState = data.currentState;
    //         data.currentState = Unsafe.As<TStates, int>(ref to);
    //         
    //         //enter/exit logic using StateLogics
    //     }
    // }

    // Basically the logic for ProSM
    public static class ProSMLogic
    {
        // Initalization
        public static void Initialize<TData>(this ref ProSM<TData> fsm, int layerCount)  where TData : unmanaged
        {
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
            // Set the layer data to default values, or maybe take in a struct of values
            var stateCount = Enum.GetValues(typeof(TStates)).Length;
            ref var layerData = ref fsm.layers.GetData(layerIndex);
            if(layerData.isInitialized) throw new InvalidOperationException("Layer already initialized, use another index");
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
            var transition = new Transition(Unsafe.As<TStates, int>(ref to), ref predicate);
            fsm.layers[layerIndex].anyTransitions.Allocate(ref transition, out int _);
        }

        public static void AddDirectTransition<TStates, TData>(this ref ProSM<TData> fsm, int layerIndex, TStates from, TStates to, ref Predicate predicate)    
            where TData : unmanaged
        {
            
            var transition = new Transition(Unsafe.As<TStates, int>(ref to), ref predicate);
            fsm.layers[layerIndex].states[(Unsafe.As<TStates, int>(ref from))].transitions.Allocate(ref transition, out int _);
        }
        
        // PollTransitions
        
        

        /// <summary>
        /// Transition handling
        /// </summary>
        /// <param name="_"></param>
        /// <param name="layerdata"></param>
        /// <param name="data"></param>
        /// <param name="nextState"></param>
        /// <typeparam name="TData"></typeparam>
        /// <returns></returns>
        public static bool TryPollTransitionsOnLayer<TData>(this ref ProSM<TData> fsm, ref LayerData<TData> layerdata, ref TData data, out int nextState)
            where TData : unmanaged
        {
            for(int i = 0; i < layerdata.anyTransitions.currentSize; i++)
            {
                ref var transition = ref layerdata.anyTransitions.GetData(i);
                if (transition.condition.Evaluate(ref data))
                {
                    nextState = transition.to;
                    return true;
                }
            }
            
            ref var currentStateData = ref layerdata.states.GetData(layerdata.currentState);
            for(int i = 0; i < currentStateData.transitions.currentSize; i++)        
            {
                ref var transition = ref currentStateData.transitions.GetData(i);
                if (transition.condition.Evaluate(ref data))
                {
                    nextState = transition.to;
                    return true;
                }
            }
            
            nextState = -1;
            return false;
        }

        public static void TransitionOnLayer<TData>(this ref ProSM<TData> fsm, int layer, int nextState, ref TData data)
            where TData : unmanaged
        {
            //previous
            fsm.layers[layer].previousState = fsm.layers[layer].currentState;
            fsm.layers[layer].states[fsm.layers[layer].currentState].OnExitState.TryRun(ref data);
            
            //next
            fsm.layers[layer].currentState = nextState;
            fsm.layers[layer].states[nextState].OnEnterState.TryRun(ref data);
        }
        
        
        
        // Entry (Awake)
        public static void Entry<TData>(this ref ProSM<TData> fsm)
            where TData : unmanaged
        {
            for(int i = 0; i < fsm.layers.currentSize; i++)
            {
                ref var layerData = ref fsm.layers.GetData(i);
                layerData.currentState = layerData.entryState;
                // enter logic using StateLogics
            }
        }
        
        // State Ticks
        public static void TickCurrentStates<TData>(ref ProSM<TData> fsm, ref TickLogic<TData>.TickData<TData> data) where TData : unmanaged
        {
            for (int i = 0; i < fsm.layers.currentSize; i++)
            {
                var currentState = fsm.layers[i].states[fsm.layers[i].currentState];
                currentState.OnUpdate.TryRun(ref data);
                currentState.OnFixedUpdate.TryRun(ref data);
                currentState.OnLateUpdate.TryRun(ref data);
            }
        }
        
        // Disposal
    }
    
    
    
    
    /// <summary>
    /// Instance Defined Procedural State Machine Handle
    // Layer enums to be declared by the user
    /// </summary>
    public struct ProSM<TData> where TData : unmanaged
    {
        // make internal later (public rn for testing)
        public Data<LayerData<TData>> layers;
        
        public void Dispose()
        {
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
