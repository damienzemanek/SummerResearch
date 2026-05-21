using LogicArchitecture;

public static class StateLogic<TData> where TData : unmanaged
    {
        public static unsafe delegate*<float, LogicHandle<TData>, TData, void> OnUpdate;
        public static unsafe delegate*<float, LogicHandle<TData>, TData, void> OnFixedUpdate;
        public static unsafe delegate*<float, LogicHandle<TData>, TData, void> OnLateUpdate;
        
        public static unsafe delegate*<LogicHandle<TData>, TData, void> OnEnter;
        public static unsafe delegate*<LogicHandle<TData>, TData, void> OnExit;
    }
    
    
    
    public static unsafe class TickLogic<TData> where TData : unmanaged
    {
        public struct TickData<TData> where TData : unmanaged
        {
            internal float deltaTime;
            internal LogicHandle<TData> coreLogic;
            public TData coreData;
            public TickData(float _deltaTime, LogicHandle<TData> _coreLogic, TData _coreData)
            {
                this.deltaTime = _deltaTime;
                this.coreLogic = _coreLogic;
                this.coreData = _coreData;
            }
            public TickData(float _deltaTime, LogicOperation<TData> operation, TData _coreData)
            {
                this.deltaTime = _deltaTime;
                coreLogic = new LogicHandle<TData>(&operation, 1);
                this.coreData = _coreData;
            }
        }
        
        // concrete impementations
        static void Run(TickData<TData>* data) => data->coreLogic.Run(ref data->coreData);
        static bool ShouldRun(TickData<TData>* data) => true;
        
        // Tick Logic (this specfici implementation) only has 1 operation
        // When used in state logic, it will be added as an operation ITSELF to another LogicHandle
        public static readonly LogicHandle<TickData<TData>> Operation; // Uses implicit operator LogicHandle(LogicOperation* ptr)
        static readonly LogicOperation<TickData<TData>> TickOperation = new(&Run, &ShouldRun); 

        // Use a static constructor to safely capture the pointer to the static field
        static TickLogic()
        {
            fixed (LogicOperation<TickData<TData>>* ptr = &TickOperation)
                Operation = ptr; 
        }
    }
}