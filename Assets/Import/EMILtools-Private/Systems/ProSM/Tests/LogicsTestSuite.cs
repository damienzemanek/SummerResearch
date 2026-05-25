using System;
using NUnit.Framework;
using ProSM.LogicArchitecture;
using ProSM.LogicExamples;
using ProSM;
using ProSM.StateArchitecture;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using static ProSM.LogicExamples.ExampleLogic;

public class LogicsTestSuite
{
    
    [Test]
    public void Test1_Initializes()
    {
        var exampleData = new ExampleData() { x = 2f };
        var logic = ExampleLogic.Operation;
        
        Assert.IsNotNull(exampleData);
        Assert.IsNotNull(logic);
        Assert.AreEqual(2f, exampleData.x);
    }
    
    [Test]
    public void Test2_RunThroughSlotCall()
    {
        var exampleData = new ExampleData() { x = 2f };
        var logic = ExampleLogic.Operation;
        
        logic.Run(ref exampleData);
        
        Assert.AreEqual(3f, exampleData.x);
    }
    
    [Test]
    public void Test3_MultiSlot_Execution()
    {
        var exampleData = new ExampleData() { x = 2f };
    
        var logic = new Logics<ExampleData>(stackalloc LogicOperation<ExampleData>[] 
        { 
            ExampleLogic.Operation, 
            ExampleLogic.Operation 
        });

        // run multi-slot (should run both handles: 1 + 1 + 1)
        logic.TryRunAllSequentially(ref exampleData);

        Assert.AreEqual(4f, exampleData.x);
    }
    

    [Test]
    public unsafe void Test4_TickLogic_Handle_Execution()
    {
        var data = new ExampleData() { x = 10f };
        var tickData = new TickLogic<ExampleData>.TickData<ExampleData>(
            _deltaTime: 0.16f,
            _coreLogics: ExampleLogic.OperationLogics, // This is fine that its not a ref cause the Op is reaodonly
            ref data);
        
        var tickLogic = TickLogic<ExampleData>.Operation;

        // execute the tick logic pipe
        tickLogic.TryRunAllSequentially(ref tickData);

        // check if core data was modified through the pipe
        Assert.AreEqual(11f, tickData.CoreData.x);
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
