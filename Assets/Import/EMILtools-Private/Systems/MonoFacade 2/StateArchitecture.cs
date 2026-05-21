using LogicArchitecture;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace StateArchitecture
{
    public static class StateLogic<TData> where TData : unmanaged
    {
        public static unsafe delegate*<float, InstanceInjectableLogicSlot<TData>, TData, void> OnUpdate;
        public static unsafe delegate*<float, InstanceInjectableLogicSlot<TData>, TData, void> OnFixedUpdate;
        public static unsafe delegate*<float, InstanceInjectableLogicSlot<TData>, TData, void> OnLateUpdate;
        
        public static unsafe delegate*<MultiInstanceInjectableLogicSlot<TData>, TData, void> OnEnter;
        public static unsafe delegate*<MultiInstanceInjectableLogicSlot<TData>, TData, void> OnExit;
    }
    
    
    
    public static unsafe class TickLogic<TData> where TData : unmanaged
    {
        public struct TickData<TData> where TData : unmanaged
        {
            public float deltaTime;
            public InstanceInjectableLogicSlot<TData> tick;
            public TData coreData;
            public TickData(float deltaTime, InstanceInjectableLogicSlot<TData> tick, TData coreData)
            {
                this.deltaTime = deltaTime;
                this.tick = tick;
                this.coreData = coreData;
            }
        }
        
        // concrete impementations
        static void Run(TickData<TData>* data) => data->tick.Run(ref data->coreData);
        static bool ShouldRun(TickData<TData>* data) => true;
        
        // local factory (has to be after Table)
        public static readonly LogicHandle<TickData<TData>> Handle = new(&Run, &ShouldRun);
    }
    
    
    public readonly struct InstanceInjectableLogicSlot<TData> where TData : unmanaged
    {
        readonly LogicHandle<TData> _logicHandle;
        
        public InstanceInjectableLogicSlot(LogicHandle<TData> handle) => _logicHandle = handle;
        public void Run(ref TData data) => _logicHandle.Run(ref data);
    }
    
    public readonly unsafe struct MultiInstanceInjectableLogicSlot<TData> where TData : unmanaged
    {
        readonly LogicHandle<TData>* _logicHandles;
        readonly int count;

        public MultiInstanceInjectableLogicSlot(LogicHandle<TData>* handles, int count)
        {
            _logicHandles = handles;
            this.count = count;
        }

        public void Run(ref TData data)
        {
            for (int i = 0; i < count; i++)
                _logicHandles[i].Run(ref data);

        }
    }

}