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
        // - Cache friendly data that exerts no gc pressure
        public struct ExampleData
        {
            public float x;
        }
        
        // 2 CONCRETE IMPLEMENTATIONS
        // - Implement your `Operations` statically, auto-validated using `ShouldRun`
        static void Run(ExampleData* data) => data->x += 1f;
        static bool ShouldRun(ExampleData* data) => data->x > 1;
        
        // 3 CONCRETE OPERATION
        // - Compose `Logics` with your implementation using `Operations`
        public static LogicOperation<ExampleData> Operation = new(&Run, &ShouldRun);
        
        // 4 LOGICS CONTAINING OPERATION(S)
        // - Add `Logics` to your ProSM, composed of `LogicOperation`s
        // (!) Not all `Logics` classes will have an OperationLogics handle (!)
        //   - Logics will be composed of many different operatiuons from seperate static Logic classes
        public static readonly Logics<ExampleData> OperationLogics = new(ref Operation);
    }
}

