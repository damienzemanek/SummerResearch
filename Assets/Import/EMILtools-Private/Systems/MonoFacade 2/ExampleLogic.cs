using LogicArchitecture;

///notes:
/// pointer validity depends on where memory lives and how runtime defines safety, not just whether it compiles
/// static CTOR happens AFTER field init. (weird)
/// Static fields init top-to-bottom before static constructuer body runs


namespace LogicExamples
{
    public static unsafe class ExampleLogic 
    {
        // 1 BLITTABLE DATA
        public struct ExampleData
        {
            public float x;
        }
        
        // 2 CONCRETE IMPLEMENTATIONS
        static void Run(ExampleData* data) => data->x += 1f;
        static bool ShouldRun(ExampleData* data) => data->x > 0;
        
        // 3 CONCRETE OPERATION
        public static readonly LogicOperation<ExampleData> Operation = new(&Run, &ShouldRun);
        
        
        // 4 LOGICS CONTAINING OPERATION(S)
        public static readonly Logics<ExampleData> OperationLogics;

        // 5 STATIC CONSTRUCTOR INITIALIZING LOGICS
        static ExampleLogic()
        {
            fixed (LogicOperation<ExampleData>* ptr = &Operation)
                OperationLogics = ptr;
        }
    }
}

