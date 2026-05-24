using DataArchitecture;
using LogicArchitecture;
using StateArchitecture;
using Unity.Collections;

namespace ProSMLogic
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
        static void Run(TickData<TData>* data) => data->coreLogics.TryRun(ref data->CoreData);
        static bool ShouldRun(TickData<TData>* data) => true;
    
        // Tick Logic (this specfici implementation) only has 1 operation
        // When used in state logic, it will be added as an operation ITSELF to another Logics
        static readonly LogicOperation<TickData<TData>> TickOperation = new(&Run, &ShouldRun); 
        public static readonly Logics<TickData<TData>> Operation = new (ref TickOperation);
    }
}
