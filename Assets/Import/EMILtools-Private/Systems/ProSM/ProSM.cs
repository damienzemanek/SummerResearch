using ProArchitecture.Data;
using UnityEngine;

namespace ProSM
{
    

    /// <summary>
    /// Instance Defined Procedural State Machine Handle.
    /// Manages multi-layered, parallel state logic using unmanaged data structures.
    ///
    /// Architecture & Design:
    /// - Procedural-First: Logic is separated from data; state transitions are handled via systems.
    /// - High Performance: Blittable and unmanaged compatible; uses raw pointers for speed.
    /// - Multi-Layered: Supports parallel state machines running on the same data.
    /// - Deterministic: Transitions follow a strict evaluation order.
    ///
    /// Features/Configuration:
    /// - Transition Priority: AnyTransitions are evaluated first, followed by DirectTransitions.
    /// - Self-Transitions: Explicitly ignored; transitioning to the current state does nothing.
    /// - Timed Transitions: Integrated with ProTimers for duration-based logic.
    /// - Idempotent Disposal: Safe to call Dispose multiple times; handles nested cleanup.
    ///
    /// Memory Safety & Edge Cases:
    /// - [CRITICAL] Struct Mobility: Capturing 'ref fsm' (e.g., in AddDirectTimedTransition) creates a raw pointer. 
    ///   Moving the FSM struct (passing by value, list resize) will dangle this pointer.
    /// - [CRITICAL] State Interruption: Manual transitions do NOT cancel pending timed transitions. 
    ///   The FSM may jump unexpectedly if a stale timer triggers after a manual state change.
    /// - TData Constraint: Must be an unmanaged/blittable struct.
    ///
    /// Usage:
    /// 1. Initialize(layerCount) to allocate internal buffers.
    /// 2. InitLayer<TEnum, TData>(index) for each layer with the default state.
    /// 3. Add_AnyTransition / Add_DirectTransition to define the graph.
    /// 4. Entry(ref TData) to trigger initial state entry logic.
    /// 5. TickUpdate / TryPollTransitions within the game loop.
    /// 6. Dispose() when the FSM is no longer needed.
    ///
    /// Validation:
    /// - Throws if TData is not unmanaged.
    /// - Throws if Entry/Tick is called on uninitialized layers.
    /// - Throws if enum types mismatch the layer's initialized type.
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
