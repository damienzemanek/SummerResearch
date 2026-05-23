using System;
using LogicArchitecture;
using LogicExamples;
using NUnit.Framework;
using ProceduralStateMachine;
using ProSMLogic;
using StateArchitecture;
using UnityEngine;

public class ProSMTestSuite : MonoBehaviour
{
    public struct ExampleData
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
    
    enum TestLayerOne { L1S1, L1S2, L1S3 }
    enum TestLayerTwo { L2S1, L2S2, L2S3 }
    
    [Test]
    public void Test1_Initalizes()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        var exampleData = new ExampleData() { value = 0 };
        var TestPredicate = ExamplePredicates.IsZero();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.InitLayer<TestLayerTwo, ExampleData>(1);

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
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.InitLayer<TestLayerTwo, ExampleData>(1);
        var exampleData = new ExampleData() { value = 0 };
        var TestPredicate = ExamplePredicates.IsZero();
        fsm.AddAnyTransition(0, TestLayerOne.L1S1, ref TestPredicate);
        
        Assert.IsTrue(fsm.layers[0].anyTransitions.active);
        Assert.AreEqual(1, fsm.layers[0].anyTransitions.currentSize);

        fsm.Dispose();
    }
    
    [Test]
    public void Test3_DirectTransitionsInitialize()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.InitLayer<TestLayerTwo, ExampleData>(1);
        var exampleData = new ExampleData() { value = 0 };
        var TestPredicate = ExamplePredicates.IsZero();
        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ref TestPredicate);
        
        Assert.IsTrue(fsm.layers[0].states[0].transitions.active);
        Assert.AreEqual(1, fsm.layers[0].states[0].transitions.currentSize);

        fsm.Dispose();
    }
    
    [Test]
    public void Test4_EntersIntoCorrectState()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer(0, TestLayerOne.L1S2);
        fsm.InitLayer(1, TestLayerTwo.L2S3);
        var exampleData = new ExampleData() { value = 0 };
        var TestPredicate = ExamplePredicates.IsZero();
        
        fsm.Entry();

        Assert.AreEqual(1, fsm.layers[0].currentState);
        Assert.AreEqual(2, fsm.layers[1].currentState);

        fsm.Dispose();
    }

    
    [Test] 
    public void Test5_AnyTransitions_TryPollTransitions_AnyTransition_ReturnsBool()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.Entry();
        var exampleData1 = new ExampleData() { value = 0 };
        var exampleData2 = new ExampleData() { value = 1 };

        var TestPredicate = ExamplePredicates.IsZero();
        fsm.AddAnyTransition(0, TestLayerOne.L1S1, ref TestPredicate);
        
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData1, out int _);
        var resultFalse = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData2, out int _);

        
        Assert.IsTrue(resultTrue);
        Assert.IsFalse(resultFalse);

        fsm.Dispose();
        
    }
    
    [Test] 
    public void Test6_AnyTransitions_TryPollTransitions_DirectTransition_ReturnsBool()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.Entry();
        var exampleData1 = new ExampleData() { value = 0 };
        var exampleData2 = new ExampleData() { value = 1 };

        var TestPredicate = ExamplePredicates.IsZero();
        fsm.AddDirectTransition(0, TestLayerOne.L1S1,TestLayerOne.L1S2, ref TestPredicate);
        
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData1, out int _);
        var resultFalse = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData2, out int _);

        
        Assert.IsTrue(resultTrue);
        Assert.IsFalse(resultFalse);

        fsm.Dispose();
        
    }
    
    
    
    
    [Test] 
    public void Test7_AnyTransitions_TryPollTransitions_AnyTransition_Transitions()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.Entry();
        var exampleData1 = new ExampleData() { value = 0 };
        var exampleData2 = new ExampleData() { value = 1 };

        var TestPredicate = ExamplePredicates.IsZero();
        fsm.AddAnyTransition(0, TestLayerOne.L1S2, ref TestPredicate);
        
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData1, out int nextState);
        var resultFalse = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData2, out int nextState2);

        if(resultTrue) fsm.TransitionOnLayer(0, nextState, ref exampleData1);
        
        Assert.AreEqual(1, nextState);
        Assert.AreEqual(-1, nextState2);
        
        Assert.AreEqual(1, fsm.layers[0].currentState);

        fsm.Dispose();
        
    }
    
    [Test] 
    public void Test8_AnyTransitions_TryPollTransitions_DirectTransition_Transitions()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.Entry();
        var exampleData1 = new ExampleData() { value = 0 };
        var exampleData2 = new ExampleData() { value = 1 };

        var TestPredicate = ExamplePredicates.IsZero();
        fsm.AddDirectTransition(0, TestLayerOne.L1S1,TestLayerOne.L1S2, ref TestPredicate);
        
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData1, out int nextState);
        var resultFalse = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData2, out int nextState2);

        if(resultTrue) fsm.TransitionOnLayer(0, nextState, ref exampleData1);
        
        Assert.AreEqual(1, nextState);
        Assert.AreEqual(-1, nextState2);
        
        Assert.AreEqual(1, fsm.layers[0].currentState);

        fsm.Dispose();
        
    }
    
    
    
    [Test] 
    public void Test9_StateLogic_EnterExitExecutes()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.Entry();
        
        var exampleData = new ExampleData() { value = 0 };
        var TestPredicate = ExamplePredicates.IsZero();
        
        Test9Logic.Reset();
        
        fsm.layers[0].states[0].OnExitState = Test9Logic.ExitLogics;
        fsm.layers[0].states[1].OnEnterState = Test9Logic.EnterLogics;
        
        fsm.AddAnyTransition(0, TestLayerOne.L1S2, ref TestPredicate);
        
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData, out int nextState);

        if(resultTrue) fsm.TransitionOnLayer(0, nextState, ref exampleData);
        
        Assert.AreEqual(1, nextState);
        Assert.AreEqual(1, fsm.layers[0].currentState);
        Assert.IsTrue(Test9Logic.Exited, "OnExitState should have executed");
        Assert.IsTrue(Test9Logic.Entered, "OnEnterState should have executed");

        fsm.Dispose();
    }

    public static unsafe class Test9Logic
    {
        public static bool Exited;
        public static bool Entered;

        public static void Reset()
        {
            Exited = false;
            Entered = false;
        }

        static void Exit(ExampleData* data) => Exited = true;
        static void Enter(ExampleData* data) => Entered = true;
        static bool ShouldRun(ExampleData* data) => true;

        public static readonly LogicOperation<ExampleData> ExitOp = new(&Exit, &ShouldRun);
        public static readonly LogicOperation<ExampleData> EnterOp = new(&Enter, &ShouldRun);

        public static readonly Logics<ExampleData> ExitLogics;
        public static readonly Logics<ExampleData> EnterLogics;

        static Test9Logic()
        {
            fixed (LogicOperation<ExampleData>* ptr = &ExitOp) ExitLogics = ptr;
            fixed (LogicOperation<ExampleData>* ptr = &EnterOp) EnterLogics = ptr;
        }
    }
    
    
    
    
    [Test] 
    public unsafe void Test10_StateLogic_TicksExecute()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.Entry();
        
        var exampleData = new ExampleData() { value = 0 };
        // Test10Logics already has the Logics handles pre-initialized with stable pointers.
        // We can just use any stable pointer for the test TickData.
        fixed (LogicOperation<ExampleData>* opPtr = &Test9Logic.EnterOp)
        {
            var tickData = new TickLogic<ExampleData>.TickData<ExampleData>(0.1f, opPtr, exampleData);

            Test10Logics.Reset();

            fsm.layers[0].states[0].OnUpdate = Test10Logics.UpdateLogics;
            fsm.layers[0].states[0].OnFixedUpdate = Test10Logics.FixedUpdateLogics;
            fsm.layers[0].states[0].OnLateUpdate = Test10Logics.LateUpdateLogics;

            // Execute the logics
            fsm.layers[0].states[0].OnUpdate.TryRun(ref tickData);
            fsm.layers[0].states[0].OnFixedUpdate.TryRun(ref tickData);
            fsm.layers[0].states[0].OnLateUpdate.TryRun(ref tickData);

            Assert.IsTrue(Test10Logics.update, "Update logic should have executed");
            Assert.IsTrue(Test10Logics.fixedUpdate, "FixedUpdate logic should have executed");
            Assert.IsTrue(Test10Logics.lateUpdate, "LateUpdate logic should have executed");
        }

        fsm.Dispose();
    }
    
    public static unsafe class Test10Logics
    {
        public static bool update;
        public static bool lateUpdate;
        public static bool fixedUpdate;

        public static void Reset()
        {
            update = false;
            lateUpdate = false;
            fixedUpdate = false;
        }
        
        static void FixedUpdate(TickLogic<ExampleData>.TickData<ExampleData>* data) => fixedUpdate = true;
        static void LateUpdate(TickLogic<ExampleData>.TickData<ExampleData>* data) => lateUpdate = true;
        static void Update(TickLogic<ExampleData>.TickData<ExampleData>* data) => update = true;
        static bool ShouldRun(TickLogic<ExampleData>.TickData<ExampleData>* data) => true;

        public static readonly LogicOperation<TickLogic<ExampleData>.TickData<ExampleData>> UpdateOp = new(&Update, &ShouldRun);
        public static readonly LogicOperation<TickLogic<ExampleData>.TickData<ExampleData>> LateUpdateOp = new(&LateUpdate, &ShouldRun);
        public static readonly LogicOperation<TickLogic<ExampleData>.TickData<ExampleData>> FixedUpdateOp = new(&FixedUpdate, &ShouldRun);

        public static readonly Logics<TickLogic<ExampleData>.TickData<ExampleData>> UpdateLogics;
        public static readonly Logics<TickLogic<ExampleData>.TickData<ExampleData>> FixedUpdateLogics;
        public static readonly Logics<TickLogic<ExampleData>.TickData<ExampleData>> LateUpdateLogics;

        static Test10Logics()
        {
            fixed (LogicOperation<TickLogic<ExampleData>.TickData<ExampleData>>* ptr = &UpdateOp) UpdateLogics = ptr;
            fixed (LogicOperation<TickLogic<ExampleData>.TickData<ExampleData>>* ptr = &FixedUpdateOp) FixedUpdateLogics = ptr;
            fixed (LogicOperation<TickLogic<ExampleData>.TickData<ExampleData>>* ptr = &LateUpdateOp) LateUpdateLogics = ptr;
        }
    }
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    

}
