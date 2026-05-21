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
        
        // concrete impementations
        static void Run(ExampleData* data) => data->x += 1f;
        static bool ShouldRun(ExampleData* data) => data->x > 0;
        
        public static readonly LogicHandle<ExampleData> Handle = new(&Run, &ShouldRun);
        
    }
}

