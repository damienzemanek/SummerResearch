using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using DataArchitecture;
using UnityEngine;

namespace FunctionalStateMachine
{
    public struct LayerData // Just the Layer data
    {
        public int entryState;
        public int currentState;
        public int previousState;
        // time in state... etc..
    }
    
    public static class LayerLogic<TStates> where TStates : Enum // Handles the Layer data
    {
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
    
    // =================================================== //
    //   FIRST GO AN MAKE TEST RUNNER TESTS FOR DATA<T>    //
    // =================================================== //

    public static class FuSMLogic
    {
        // Enter (Awake)
        
        // Add/Remove Layers
        
        // PollTransitionAsync
        
        // AddAnyTransition
        
        // AddDirectTransition
        
        // Disposal
    }
    
    
    
    /// <summary>
    /// Functional State Machine
    /// </summary>
    public class FuSM
    {
        Data<LayerData> layers;
        
        
        // Get Data Returns the LayerData
    }

}
