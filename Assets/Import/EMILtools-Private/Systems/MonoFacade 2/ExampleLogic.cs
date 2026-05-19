using LogicArchitecture;

///notes:
/// pointer validity depends on where memory lives and how runtime defines safety, not just whether it compiles
/// static CTOR happens AFTER field init. (weird)


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
        static bool ShouldRun(ExampleData* data) => data->x > 0;
        static void Run(ExampleData* data) => data->x += 1f;
        
        // local factory
        public static readonly LogicHandle<ExampleData> Handle = new(Table);
        
        // vtable init
        static readonly LogicFunctionTable<ExampleData> Table = new LogicFunctionTable<ExampleData>
        {
            ShouldRun = &ShouldRun,
            Run = &Run
        };
    }
    
}
