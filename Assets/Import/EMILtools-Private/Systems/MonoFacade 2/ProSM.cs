using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DataArchitecture;
using StateArchitecture;
using Unity.Collections;
using UnityEngine;

namespace ProceduralStateMachine
{
    
    // Separate Data Layer
    public struct LayerData // Just the Layer data
    {
        const int ANY_TRANSITIONS_SIZE = 50;

        public bool isInitialized;
        public int entryState;
        public int currentState;
        public int previousState;
        public Data<StateData> states;
        public Data<Transition> anyTransitions;
        // time in state... etc..

        public void Init(int statesSize)
        {
            states = new Data<StateData>(statesSize, Allocator.Persistent);
            anyTransitions = new Data<Transition>(ANY_TRANSITIONS_SIZE, Allocator.Persistent);
            isInitialized = true;
        }
    }
    
    // Separate Data Layer Handling
    public static class LayerLogic<TStates> where TStates : Enum // Handles the Layer data
    {
        static LayerLogic()
        {
            if (Enum.GetUnderlyingType(typeof(TStates)) != typeof(int)) throw new InvalidOperationException($"{typeof(TStates).Name} must have an underlying type of int.");
        }
        
        public static TStates GetState(ref LayerData data) => Unsafe.As<int, TStates>(ref data.currentState);
        public static TStates GetEntryState(ref LayerData data) => Unsafe.As<int, TStates>(ref data.entryState);
        public static TStates GetPreviousState(ref LayerData data) => Unsafe.As<int, TStates>(ref data.previousState);

        public static void Transition(ref LayerData data, TStates to)
        {
            data.previousState = data.currentState;
            data.currentState = Unsafe.As<TStates, int>(ref to);
            
            //enter/exit logic using StateLogic
        }
    }

    // Basically the logic for ProSM
    public static class ProSMLogic
    {
        // Initalization
        public static void Initialize(this ref ProSM fsm, int layerCount) 
        {
            fsm.layers = new Data<LayerData>(layerCount, Allocator.Persistent);
            // Creating the layer data
            for (int i = 0; i < layerCount; i++)
            {
                var layerData = new LayerData() { entryState = 0, currentState = 0, previousState = 0 };
                fsm.layers.Allocate(ref layerData, out int id);
            }
        }
        
        public static void InitLayer<TStates>(this ref ProSM fsm, int layerIndex)
        {
            // Set the layer data to default values, or maybe take in a struct of values
            var stateCount = Enum.GetValues(typeof(TStates)).Length;
            ref var layerData = ref fsm.layers.GetData(layerIndex);
            if(layerData.isInitialized) throw new InvalidOperationException("Layer already initialized, use another index");
            layerData.Init(stateCount);
            for (int i = 0; i < stateCount; i++)
            {
                var stateData = new StateData(i);
                layerData.states.Allocate(ref stateData, out int _);
            }
        }
        
        
        // Add/Remove Layers

        public static void AddAnyTransition<TStates>(this ref ProSM fsm, int layerIndex, TStates to, ref Predicate predicate)
        {
            var transition = new Transition(Unsafe.As<TStates, int>(ref to), ref predicate);
            fsm.layers[layerIndex].anyTransitions.Allocate(ref transition, out int _);
        }

        public static void AddDirectTransition<TStates>(this ref ProSM fsm, int layerIndex, TStates from, TStates to, ref Predicate predicate)    
        {
            var transition = new Transition(Unsafe.As<TStates, int>(ref to), ref predicate);
            fsm.layers[layerIndex].states[(Unsafe.As<TStates, int>(ref from))].transitions.Allocate(ref transition, out int _);
        }
        
        // Enter (Awake)

        
        // PollTransitionAsync
        
        // AddAnyTransition
        
        // AddDirectTransition
        
        // Disposal
    }
    
    
    
    
    /// <summary>
    /// Instance Defined Procedural State Machine Handle
    // Layer enums to be declared by the user
    /// </summary>
    public struct ProSM
    {
        // make internal later (public rn for testing)
        public Data<LayerData> layers;
        
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

    public struct ProSMTestData
    {
        float x;
    }
    public class ProSMUser
    {
        enum TestLayerOne { L1S1, L1S2, L1S3 }
        enum TestLayerTwo { L2S1, L2S2, L2S3 }

        ProSM fsm;

        void UseProSM()
        {
            fsm.Initialize(2);
            fsm.InitLayer<TestLayerOne>(0);
            fsm.InitLayer<TestLayerTwo>(1);
            var TestPredicate = ExamplePredicate.Create();
            fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ref TestPredicate);
        }
        
    }

    
    struct ExampleData
    {
        public int value;
    }

    static unsafe class ExamplePredicate
    {   
        // Little verbose, but oh well
        public static Predicate Create() => new Predicate(&IsZero);
        static bool IsZero(void* ptr)
        {
            ExampleData* data = (ExampleData*)ptr;
            return data->value == 0;
        }
    }
    
}
