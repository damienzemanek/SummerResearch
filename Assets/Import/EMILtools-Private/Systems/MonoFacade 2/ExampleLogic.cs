using LogicArchitecture;

///notes:
/// pointer validity depends on where memory lives and how runtime defines safety, not just whether it compiles
/// static CTOR happens AFTER field init. (weird)
/// Static fields init top-to-bottom before static constructuer body runs


namespace LogicExamples
{
    public static unsafe class ExampleLogic
    {
        // data (needs to be blittable)
        public struct ExampleData
        {
            public float x;
        }
        
        // Pass throughs
        static void Run(ExampleData* data) => data->x += 1f;
        static bool ShouldRun(ExampleData* data) => data->x > 0;
        
        // vtable init (has to be before Handle)
        static readonly LogicFunctionTable<ExampleData> Table = new LogicFunctionTable<ExampleData>
        {
            ShouldRun = &ShouldRun,
            Run = &Run
        };
        
        // local factory (has to be after Table)
        public static readonly LogicHandle<ExampleData> Handle = new(Table);
        
    }
    
}
