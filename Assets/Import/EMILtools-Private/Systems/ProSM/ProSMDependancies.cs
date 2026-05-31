using System;
using ProArchitecture.Data;
using ProArchitecture.Logic;
using ProArchitecture.Predicates;
using Unity.Collections;

namespace ProSM
{
    
    public struct StateData<TData> where TData : unmanaged
    {
        const int TRANSITIONS_SIZE = 50;
        
        public int state;
        public Data<Transition> transitions;
        
        public Logics<TickLogic<TData>.TickLogicData<TData>> OnUpdate;
        public Logics<TickLogic<TData>.TickLogicData<TData>> OnFixedUpdate;
        public Logics<TickLogic<TData>.TickLogicData<TData>> OnLateUpdate;
    
        public Logics<TData> OnEnterState;  
        public Logics<TData> OnExitState;
        
        public StateData(int _state)
        {
            state = _state;
            transitions = new Data<Transition>(TRANSITIONS_SIZE, Allocator.Persistent);
            
            OnUpdate = default;
            OnFixedUpdate = default;
            OnLateUpdate = default;
            OnEnterState = default;
            OnExitState = default;
        }
    }
    

    public unsafe struct Transition
    {
        public short from;
        public short to;
        public Predicate condition;
        public ByteBool hasDurationCondition;

        public int layer;
        public IntPtr dataFetchLocationOnComplete;
        public IntPtr fsm;

        public Transition(short _to, ref Predicate _condition, bool hasDuration)
        {
            from = -1;
            to = _to;
            condition = _condition;
            hasDurationCondition = new ByteBool(hasDuration);
            layer = -1;
            fsm = IntPtr.Zero;
            dataFetchLocationOnComplete = IntPtr.Zero;
        }
    }

    
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
}

    
