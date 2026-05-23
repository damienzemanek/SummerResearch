using NUnit.Framework;
using LogicArchitecture;
using LogicExamples;
using ProSMLogic;
using StateArchitecture;
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
    public void Test3_MultiSlot_Execution()
    {
        var exampleData = new ExampleData() { x = 1f };
    
        // We use a static helper to avoid unsafe code in the test method
        var logic = MultiSlotLogic.Create(ExampleLogic.Operation, ExampleLogic.Operation);

        // run multi-slot (should run both handles: 1 + 1 + 1)
        logic.TryRun(ref exampleData);

        Assert.AreEqual(3f, exampleData.x);
    }

    private static unsafe class MultiSlotLogic
    {
        private static readonly LogicOperation<ExampleData>[] Store = new LogicOperation<ExampleData>[10];
        
        public static Logics<ExampleData> Create(LogicOperation<ExampleData> op1, LogicOperation<ExampleData> op2)
        {
            Store[0] = op1;
            Store[1] = op2;
            fixed (LogicOperation<ExampleData>* ptr = Store)
                return new Logics<ExampleData>(ptr, 2);
        }
    }

    [Test]
    public unsafe void Test4_TickLogic_Handle_Execution()
    {
        fixed (LogicOperation<ExampleData>* opPtr = &ExampleLogic.Operation)
        {
            var tickData = new TickLogic<ExampleData>.TickData<ExampleData>(
                _deltaTime: 0.16f,
                _stableOpPtr: opPtr,
                _coreData: new ExampleData() { x = 10f });

            var tickLogic = TickLogic<ExampleData>.Operation;

            // execute the tick logic pipe
            tickLogic.TryRun(ref tickData);

            // check if core data was modified through the pipe
            Assert.AreEqual(11f, tickData.coreData.x);
        }
    }

    [Test]
    public void Test5_StateLogic_Delegates_Assignment()
    {
        // verify logic handles can be assigned to StateData
        // these usually point to LogicHandles that process TickData
        
        var stateData = new StateData<ExampleData>(0);
        stateData.OnUpdate = TickLogic<ExampleData>.Operation;
        stateData.OnEnterState = ExampleLogic.OperationLogics;

        Assert.IsTrue(stateData.OnUpdate.Count > 0);
        Assert.IsTrue(stateData.OnEnterState.Count > 0);
        
        stateData.transitions.Dispose();
    }

    // dummy implementations for delegate pointer testing
    private static unsafe void DummyUpdate(TickLogic<ExampleData>.TickData<ExampleData>* data) { }
    private static unsafe void DummyEnter(TickLogic<ExampleData>.TickData<ExampleData>* data) { }
}
