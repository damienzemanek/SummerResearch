using System;
using NUnit.Framework;
using ProArchitecture.Logic;
using static ProArchitecture.Logic.ExampleLogic;

public class LogicsTestSuite
{
    
    [Test]
    public void Test1_Initializes()
    {
        var exampleData = new ExampleData() { x = 2f };
        var logic = Operation;
        
        Assert.IsNotNull(exampleData);
        Assert.IsNotNull(logic);
        Assert.AreEqual(2f, exampleData.x);
    }
    
    [Test]
    public void Test2_RunThroughSlotCall()
    {
        var exampleData = new ExampleData() { x = 2f };
        var logic = Operation;
        
        logic.Run(ref exampleData);
        
        Assert.AreEqual(3f, exampleData.x);
    }
    
    [Test]
    public void Test3_MultiSlot_Execution()
    {
        var exampleData = new ExampleData() { x = 2f };
    
        var logic = new Logics<ExampleData>(stackalloc LogicOperation<ExampleData>[] 
        { 
            Operation, 
            Operation 
        });

        // run multi-slot (should run both handles: 1 + 1 + 1)
        logic.TryRunAllSequentially(ref exampleData);

        Assert.AreEqual(4f, exampleData.x);
    }
    

    [Test]
    public unsafe void Test4_TickLogic_Handle_Execution()
    {
        var data = new ExampleData() { x = 10f };
        var tickData = new TickLogic<ExampleData>.TickLogicData<ExampleData>(
            _deltaTime: 0.16f,
            _coreLogics: ref OperationLogics, // This is fine that its not a ref cause the Op is reaodonly
            ref data);
        
        var tickLogic = TickLogic<ExampleData>.TickLogics;

        // execute the tick logic pipe
        tickLogic.TryRunAllSequentially(ref tickData);

        // check if core data was modified through the pipe
        Assert.AreEqual(11f, tickData.CoreData.x);
    }
    
}
