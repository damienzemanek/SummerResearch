using System;
using ProSM.DataArchitecture;
using ProSM.LogicArchitecture;
using Unity.Collections;

namespace ProSM.StateArchitecture
{
    
    public struct StateData<TData> where TData : unmanaged
    {
        const int TRANSITIONS_SIZE = 50;
        
        public int state;
        public Data<Transition> transitions;
        
        public Logics<TickLogic<TData>.TickData<TData>> OnUpdate;
        public Logics<TickLogic<TData>.TickData<TData>> OnFixedUpdate;
        public Logics<TickLogic<TData>.TickData<TData>> OnLateUpdate;
    
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
    
    public static unsafe class TickLogic<TData> where TData : unmanaged
    {
        public struct TickData<TData> where TData : unmanaged
        {
            public float deltaTime;
            public Logics<TData> coreLogics;
            readonly TData* coreData;
            public ref TData CoreData => ref *coreData;
            public TickData(float _deltaTime, Logics<TData> _coreLogics, ref TData _coreData)
            {
                deltaTime = _deltaTime;
                coreLogics = _coreLogics;
                coreData = (TData*)Unity.Collections.LowLevel.Unsafe.UnsafeUtility.AddressOf(ref _coreData);
            }
        }
    
        // concrete impementations
        // Deref pointer satisfies ref T param on TryRun
        static void Run(TickData<TData>* data) => data->coreLogics.TryRunAllSequentially(ref data->CoreData);
        static bool ShouldRun(TickData<TData>* data) => true;
    
        // Tick Logic (this specfici implementation) only has 1 operation
        // When used in state logic, it will be added as an operation ITSELF to another Logics
        static readonly LogicOperation<TickData<TData>> TickOperation = new(&Run, &ShouldRun); 
        public static readonly Logics<TickData<TData>> Operation = new (ref TickOperation);
    }
    

    public struct Transition
    {
        public short to;
        public float duration;
        public Predicate condition;
        // mabye in the future make this a logic that does not have to pass in the predicate, but creates it here
        public Transition(short _to, ref Predicate _condition, float _duration = 0f) 
        {
            to = _to;
            condition = _condition;
            duration = _duration;
        }
    }



    /// <summary>
    /// Delegate* are blittable
    /// </summary>
    public unsafe struct Predicate
    {
        //      Subscribe using this delegate signature:
        //      bool IsSomething(ref T data);

        // 1st: void* is the delegate
        // 2nd: bool is the result
        internal delegate*<void*, bool> evaluate;
        public bool IsCreated => evaluate != null;
        public Predicate(delegate*<void*, bool> _evaluate) => evaluate = _evaluate;
    }

    public static unsafe class PredicateExtensions
    {
        public static bool Evaluate<T>(this ref Predicate predicate, ref T data) where T : unmanaged
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            if (predicate.evaluate == null) throw new NullReferenceException("Predicate not initialized");
#endif
            fixed(void* ptr = &data) return predicate.evaluate(ptr);
        }
    }
    
    




}