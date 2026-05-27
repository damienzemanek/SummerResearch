using System;
using ProArchitecture.Logic;
using ProArchitecture.Predicates;
using NUnit.Framework;
using ProSM;
using ProTimers;
using UnityEngine;
using static ProArchitecture.Logic.ExampleLogic;
using static ProSMTestSuite;
using static ProSMTestSuite.SomeInstanceLogic;

public class ProSMIntegrationTestSuite : MonoBehaviour
{

    [Test]
    public void Test1_TimedDirectTransition_Initalizes()
    {
        TimerStack.Reset();
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.InitLayer<TestLayerTwo, ExampleData>(1);
        var exampleData = new ExampleData() { x = 2 };
        var TestPredicate = ExamplePredicates.IsGreaterThanOne();
        fsm.AddDirectTimedTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, 1);
        
        Assert.IsTrue(fsm.layers[0].states[0].transitions.Active);
        Assert.AreEqual(1, fsm.layers[0].states[0].transitions.currentSize);

        fsm.Dispose();
    }
    
    [Test]
    public void Test2_TimedDirectTransition_Executes()
    {
        TimerStack.Reset();
        TimerStackLogics.isTesting = true;
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var data = new ExampleData() { x = 1 };
        fsm.Entry(ref data);

        fsm.AddDirectTimedTransition(0, TestLayerOne.L1S1,TestLayerOne.L1S2, 1);
        TimerStack.TickActivesDebug(0.5f);

        Assert.IsFalse(fsm.layers[0].states[0].transitions[0].hasDurationCondition);
        
        TimerStack.TickActivesDebug(0.6f);
        
        Assert.IsTrue(fsm.layers[0].states[0].transitions[0].hasDurationCondition);


        fsm.Dispose();
        TimerStackLogics.isTesting = false;
    }
}
