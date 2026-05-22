using System;
using NUnit.Framework;
using ProceduralStateMachine;
using StateArchitecture;
using UnityEngine;

public class ProSMTestSuite : MonoBehaviour
{
    struct ExampleData
    {
        public int value;
    }

    static unsafe class ExamplePredicates
    {   
        // Little verbose, but oh well
        public static Predicate IsZero() => new Predicate(&isZero);
        static bool isZero(void* ptr)
        {
            ExampleData* data = (ExampleData*)ptr;
            return data->value == 0;
        }
    }
    
    public class ProSMUser
    {
        enum TestLayerOne { L1S1, L1S2, L1S3 }
        enum TestLayerTwo { L2S1, L2S2, L2S3 }

        ProSM fsm;

        // fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ref TestPredicate);

        
    }

    enum TestLayerOne { L1S1, L1S2, L1S3 }
    enum TestLayerTwo { L2S1, L2S2, L2S3 }
    
    [Test]
    public void Test1_Initalizes()
    {
        ProSM fsm = new ProSM();
        var exampleData = new ExampleData() { value = 0 };
        var TestPredicate = ExamplePredicates.IsZero();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne>(0);
        fsm.InitLayer<TestLayerTwo>(1);

        Assert.IsTrue(TestPredicate.IsCreated);
        Assert.AreEqual(0, exampleData.value);
        Assert.IsTrue(fsm.layers.active);
        Assert.IsTrue(fsm.layers[0].isInitialized);
        Assert.IsTrue(fsm.layers[1].isInitialized);
        Assert.AreEqual(3, fsm.layers[0].states.currentSize);
        Assert.AreEqual(3, fsm.layers[1].states.currentSize);
        
        Assert.IsTrue(fsm.layers[0].states[0].transitions.active);
        Assert.IsTrue(fsm.layers[1].states[0].transitions.active);
        
        fsm.Dispose();
    }
    
    [Test]
    public void Test2_AnyTransitionsInitialize()
    {
        ProSM fsm = new ProSM();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne>(0);
        fsm.InitLayer<TestLayerTwo>(1);
        var exampleData = new ExampleData() { value = 0 };
        var TestPredicate = ExamplePredicates.IsZero();
        fsm.AddAnyTransition(0, TestLayerOne.L1S1, ref TestPredicate);
        
        Assert.IsTrue(fsm.layers[0].anyTransitions.active);
        Assert.AreEqual(1, fsm.layers[0].anyTransitions.currentSize);

        fsm.Dispose();
    }
    

}
