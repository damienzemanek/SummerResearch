using NUnit.Framework;
using LogicArchitecture;
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
        var logic = Handle;
        
        // slot to put logic into
        var slot = new InstanceInjectableLogicSlot<ExampleData>(logic);
        
        Assert.IsNotNull(exampleData);
        Assert.IsNotNull(slot);
        Assert.IsNotNull(logic);
        Assert.AreEqual(1f, exampleData.x);
    }
    
    [Test]
    public void Test2_RunThroughSlotCall()
    {
        var exampleData = new ExampleData() { x = 1f };
        var logic = Handle;
        
        // slot to put logic into
        var slot = new InstanceInjectableLogicSlot<ExampleData>(logic);
        
        slot.Run(ref exampleData);
        
        Assert.AreEqual(2f, exampleData.x);
    }
    
    [Test]
    public unsafe void Test3_MultiSlot_Execution()
    {
        var exampleData = new ExampleData() { x = 1f };
    
        // Use stackalloc instead of NativeArray to bypass the unmanaged check
        // LogicHandle is small, so this is safe for a test
        LogicHandle<ExampleData>* handles = stackalloc LogicHandle<ExampleData>[2];
        handles[0] = Handle;
        handles[1] = Handle;

        // multi-slot init using the stack pointer
        var multiSlot = new MultiInstanceInjectableLogicSlot<ExampleData>(handles, 2);

        // run multi-slot (should run both handles: 1 + 1 + 1)
        multiSlot.Run(ref exampleData);

        Assert.AreEqual(3f, exampleData.x);
        // No Dispose needed for stackalloc
    }

    [Test]
    public unsafe void Test4_TickLogic_Handle_Execution()
    {
        var exampleData = new ExampleData() { x = 10f };
        var slot = new InstanceInjectableLogicSlot<ExampleData>(Handle);
        
        // package data into the tick packet
        var tickData = new TickLogic<ExampleData>.TickData<ExampleData>(0.016f, slot, exampleData);

        // execute the tick logic pipe
        TickLogic<ExampleData>.Handle.Run(ref tickData);

        // check if core data was modified through the pipe
        Assert.AreEqual(11f, tickData.coreData.x);
    }

    [Test]
    public unsafe void Test5_StateLogic_StaticDelegates_Assignment()
    {
        // verify static delegate pointers can be assigned and held
        // these usually point to static methods in your state implementations
        StateLogic<ExampleData>.OnUpdate = &DummyUpdate;
        StateLogic<ExampleData>.OnEnter = &DummyEnter;

        Assert.IsTrue(StateLogic<ExampleData>.OnUpdate != null);
        Assert.IsTrue(StateLogic<ExampleData>.OnEnter != null);
    }

    // dummy implementations for delegate pointer testing
    private static unsafe void DummyUpdate(float dt, InstanceInjectableLogicSlot<ExampleData> slot, ExampleData data) { }
    private static unsafe void DummyEnter(MultiInstanceInjectableLogicSlot<ExampleData> slot, ExampleData data) { }
}
