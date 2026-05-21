using NUnit.Framework;
using LogicArchitecture;
using LogicExamples;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using static LogicExamples.ExampleLogic;

public class StateArchitectureTestSuite
{
    
    [Test]
    public void Test1_Initializes()
    {
        var exampleData = new ExampleData() { x = 1f };
        var logic = ExampleLogic.Operation;
        
        Assert.IsNotNull(exampleData);
        Assert.IsNotNull(logic);
        Assert.AreEqual(1f, exampleData.x);
    }
    
    [Test]
    public void Test2_RunThroughSlotCall()
    {
        var exampleData = new ExampleData() { x = 1f };
        var logic = ExampleLogic.Operation;
        
        logic.Run(ref exampleData);
        
        Assert.AreEqual(2f, exampleData.x);
    }
    
    [Test]
    public unsafe void Test3_MultiSlot_Execution()
    {
        var exampleData = new ExampleData() { x = 1f };
    
        // Use stackalloc instead of NativeArray to bypass the unmanaged check
        // LogicHandle is small, so this is safe for a test
        LogicOperation<ExampleData>* handles = stackalloc LogicOperation<ExampleData>[2];
        handles[0] = ExampleLogic.Operation;
        handles[1] = ExampleLogic.Operation;

        // multi-slot init using the stack pointer
        var logic = new LogicHandle<ExampleData>(handles, 2);

        // run multi-slot (should run both handles: 1 + 1 + 1)
        logic.Run(ref exampleData);

        Assert.AreEqual(3f, exampleData.x);
        // No Dispose needed for stackalloc
    }

    [Test]
    public unsafe void Test4_TickLogic_Handle_Execution()
    {
        var tickData = new TickLogic<ExampleData>.TickData<ExampleData>(
            _deltaTime: 0.16f,
            operation: ExampleLogic.Operation,
            _coreData: new ExampleData() { x = 10f } );

        var tickLogic = TickLogic<ExampleData>.Operation;
        
        
        // execute the tick logic pipe
        tickLogic.Run(ref tickData);

        // check if core data was modified through the pipe
        Assert.AreEqual(11f, tickData.coreData.x);
    }

    [Test]
    public unsafe void Test5_StateLogic_StaticDelegates_Assignment()
    {
        // verify static logic handles can be assigned
        // these usually point to LogicHandles that process TickData
        
        static void Run(TickLogic<ExampleData>.TickData<ExampleData>* data) { }
        static bool ShouldRun(TickLogic<ExampleData>.TickData<ExampleData>* data) => true;
        
        LogicOperation<TickLogic<ExampleData>.TickData<ExampleData>> op = new(&Run, &ShouldRun);
        
        StateLogic<ExampleData>.OnUpdate = &op;
        StateLogic<ExampleData>.OnEnterState = &op;

        Assert.IsTrue(StateLogic<ExampleData>.OnUpdate.Count > 0);
        Assert.IsTrue(StateLogic<ExampleData>.OnEnterState.Count > 0);
    }

    // dummy implementations for delegate pointer testing
    private static unsafe void DummyUpdate(TickLogic<ExampleData>.TickData<ExampleData>* data) { }
    private static unsafe void DummyEnter(TickLogic<ExampleData>.TickData<ExampleData>* data) { }
}
