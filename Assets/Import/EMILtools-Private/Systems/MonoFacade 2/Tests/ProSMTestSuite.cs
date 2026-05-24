using System;
using LogicArchitecture;
using NUnit.Framework;
using ProceduralStateMachine;
using ProSMLogic;
using StateArchitecture;
using UnityEngine;
using static LogicExamples.ExampleLogic;

public class ProSMTestSuite : MonoBehaviour
{
    static unsafe class ExamplePredicates
    {   
        // Little verbose, but oh well
        public static Predicate IsGreaterThanOne() => new Predicate(&isGreaterThanOne);
        static bool isGreaterThanOne(void* ptr)
        {
            ExampleData* data = (ExampleData*)ptr;
            return data->x > 1;
        }
    }
    
    enum TestLayerOne { L1S1, L1S2, L1S3 }
    enum TestLayerTwo { L2S1, L2S2, L2S3 }
    
    [Test]
    public void Test1_Initalizes()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        var exampleData = new ExampleData() { x = 2 };
        var TestPredicate = ExamplePredicates.IsGreaterThanOne();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.InitLayer<TestLayerTwo, ExampleData>(1);

        Assert.IsTrue(TestPredicate.IsCreated);
        Assert.AreEqual(2, exampleData.x);
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
        var exampleData = new ExampleData() { x = 2 };
        var TestPredicate = ExamplePredicates.IsGreaterThanOne();
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
        var exampleData = new ExampleData() { x = 2 };
        var TestPredicate = ExamplePredicates.IsGreaterThanOne();
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
        var exampleData = new ExampleData() { x = 2 };
        var TestPredicate = ExamplePredicates.IsGreaterThanOne();
        
        fsm.Entry(ref exampleData);

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
        var exampleData1 = new ExampleData() { x = 1 };
        var exampleData2 = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData1);

        var TestPredicate = ExamplePredicates.IsGreaterThanOne();
        fsm.AddAnyTransition(0, TestLayerOne.L1S2, ref TestPredicate);
        
        var resultFalse = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData1, out int _);
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData2, out int _);

        Assert.IsFalse(resultFalse);
        Assert.IsTrue(resultTrue);

        fsm.Dispose();
        
    }
    
    [Test] 
    public void Test6_AnyTransitions_TryPollTransitions_DirectTransition_ReturnsBool()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var exampleData1 = new ExampleData() { x = 1 };
        var exampleData2 = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData1);

        var TestPredicate = ExamplePredicates.IsGreaterThanOne();
        fsm.AddDirectTransition(0, TestLayerOne.L1S1,TestLayerOne.L1S2, ref TestPredicate);
        
        var resultFalse = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData1, out int _);
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData2, out int _);
        
        Assert.IsFalse(resultFalse);
        Assert.IsTrue(resultTrue);

        fsm.Dispose();
        
    }
    
    
    
    
    [Test] 
    public void Test7_AnyTransitions_TryPollTransitions_AnyTransition_Transitions()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var exampleData1 = new ExampleData() { x = 1 };
        var exampleData2 = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData1);

        var TestPredicate = ExamplePredicates.IsGreaterThanOne();
        fsm.AddAnyTransition(0, TestLayerOne.L1S2, ref TestPredicate);
        
        var resultFalse = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData1, out int nextState_doesNOTtransition);
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData2, out int nextState2_doesTransition);

        if(resultTrue) fsm.TransitionOnLayer_CallExitEnter(0, nextState2_doesTransition, ref exampleData1);
        
        Assert.AreEqual(-1, nextState_doesNOTtransition);
        Assert.AreEqual(1, nextState2_doesTransition);
        
        Assert.AreEqual(1, fsm.layers[0].currentState);

        fsm.Dispose();
        
    }
    
    [Test] 
    public void Test8_AnyTransitions_TryPollTransitions_DirectTransition_Transitions()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var exampleData1 = new ExampleData() { x = 1 };
        var exampleData2 = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData1);

        var TestPredicate = ExamplePredicates.IsGreaterThanOne();
        fsm.AddDirectTransition(0, TestLayerOne.L1S1,TestLayerOne.L1S2, ref TestPredicate);
        
        var resultFalse = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData1, out int nextState_doesNOTtransition);
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData2, out int nextState2_doesTransition);

        if(resultTrue) fsm.TransitionOnLayer_CallExitEnter(0, nextState2_doesTransition, ref exampleData1);
        
        Assert.AreEqual(-1, nextState_doesNOTtransition);
        Assert.AreEqual(1, nextState2_doesTransition);
        
        Assert.AreEqual(1, fsm.layers[0].currentState);

        fsm.Dispose();
        
    }
    
    
    
    [Test] 
    public void Test9_StateLogic_EnterExitTransitionEventsExecutes()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        
        var exampleData = new ExampleData() { x = 2 };
        var TestPredicate = ExamplePredicates.IsGreaterThanOne();
        fsm.Entry(ref exampleData);

        Test9Logic.Reset();
        
        fsm.layers[0].states[0].OnExitState = Test9Logic.ExitLogics;
        fsm.layers[0].states[1].OnEnterState = Test9Logic.EnterLogics;
        
        fsm.AddAnyTransition(0, TestLayerOne.L1S2, ref TestPredicate);
        
        var resultTrue = fsm.TryPollTransitionsOnLayer(ref fsm.layers[0], ref exampleData, out int nextState);

        if(resultTrue) fsm.TransitionOnLayer_CallExitEnter(0, nextState, ref exampleData);
        
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

        public static readonly Logics<ExampleData> ExitLogics = new(ref ExitOp);
        public static readonly Logics<ExampleData> EnterLogics = new(ref EnterOp);
        
    }
    
    
    
    
    [Test] 
    public unsafe void Test10_StateLogic_TicksExecute()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var exampleData = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData);

        // Test10Logics already has the Logics handles pre-initialized with stable pointers.
        // We can just use any stable pointer for the test TickData.

        var tickData = new TickLogic<ExampleData>.TickData<ExampleData>(
            _deltaTime: 0.1f,
            op: Operation,
            _coreData: exampleData);
        
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
            UpdateLogics = new Logics<TickLogic<ExampleData>.TickData<ExampleData>>(ref UpdateOp);
            FixedUpdateLogics = new Logics<TickLogic<ExampleData>.TickData<ExampleData>>(ref FixedUpdateOp);
            LateUpdateLogics = new Logics<TickLogic<ExampleData>.TickData<ExampleData>>(ref LateUpdateOp);
        }
        
    }
    
    
    [Test] 
    public unsafe void Test11_PollTransitionsAllLayers()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();

        fsm.Initialize(2);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.InitLayer<TestLayerTwo, ExampleData>(1);

        var exampleData1 = new ExampleData() { x = 1 };
        var exampleData2 = new ExampleData() { x = 2 };
        fsm.Entry(ref exampleData1);
        
        var TestPredicate = ExamplePredicates.IsGreaterThanOne();
        fsm.AddAnyTransition(0, TestLayerOne.L1S2, ref TestPredicate);
        fsm.AddAnyTransition(1, TestLayerTwo.L2S2, ref TestPredicate);

        
        // Should not Transition
        fsm.TryPollTransitions(ref exampleData1);
        
        Debug.Log("CurrentStates: layer1: " + fsm.layers[0].currentState + " layer2: " + fsm.layers[1].currentState + "");
        Assert.AreEqual(0, fsm.layers[0].currentState);
        Assert.AreEqual(0, fsm.layers[1].currentState);
        
        // Should Transition
        fsm.TryPollTransitions(ref exampleData2);
        
        Debug.Log("CurrentStates: layer1: " + fsm.layers[0].currentState + " layer2: " + fsm.layers[1].currentState + "");
        Assert.AreEqual(1, fsm.layers[0].currentState);
        Assert.AreEqual(1, fsm.layers[1].currentState);
        
        fsm.Dispose();
    }
    
    [Test]
    public void Test12_TransitionPriority_AnyOverDirect()
    {
        // Tests that AnyTransitions are evaluated before DirectTransitions
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        var exampleData = new ExampleData() { x = 2 };

        fsm.Entry(ref exampleData);
        var truePredicate = ExamplePredicates.IsGreaterThanOne(); // Always true for x=2
        
        // Add direct transition to S2
        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ref truePredicate);
        // Add any transition to S3
        fsm.AddAnyTransition(0, TestLayerOne.L1S3, ref truePredicate);

        fsm.TryPollTransitions(ref exampleData);

        // Should be S3 because AnyTransitions are polled first in TryPollTransitionsOnLayer
        Assert.AreEqual((int)TestLayerOne.L1S3, fsm.layers[0].currentState);
        
        fsm.Dispose();
    }
    
    // Helper logic class for the new tests
    public static unsafe class PipelineLogic
    {
        static void AddOne(ExampleData* data) => data->x += 1;
        static void Double(ExampleData* data) => data->x *= 2;
        static bool ShouldRun(ExampleData* data) => true;

        public static readonly LogicOperation<ExampleData> AddOneOp = new(&AddOne, &ShouldRun);
        public static readonly LogicOperation<ExampleData> DoubleOp = new(&Double, &ShouldRun);

        public static readonly Logics<ExampleData> AddOneLogics = new(ref AddOneOp);
        public static readonly Logics<ExampleData> DoubleLogics = new(ref DoubleOp);

        public static void Reset() { } 
    }
    
    [Test]
    public void Test13_DataPipeline_ExitToEnter_IsSequential()
    {
        // Verifies that data mutated in OnExit is correctly seen by OnEnter of the next state (Operated Sequentially)
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        
        var data = new ExampleData() { x = 10 };
        fsm.Entry(ref data);

        var predicate = ExamplePredicates.IsGreaterThanOne();

        // S1 Exit: x += 1 (11)
        // S2 Enter: x *= 2 (22)
        fsm.layers[0].states[(int)TestLayerOne.L1S1].OnExitState = PipelineLogic.AddOneLogics;
        fsm.layers[0].states[(int)TestLayerOne.L1S2].OnEnterState = PipelineLogic.DoubleLogics;

        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S2, ref predicate);
        fsm.TryPollTransitions(ref data);

        Assert.AreEqual(22f, data.x, "Data should be modified by Exit then Enter in sequence");
        fsm.Dispose();
    }
    
    // Helper class for counting executions
    public static unsafe class TransitionCounter
    {
        public static int ExitCount;
        public static int EnterCount;

        public static void Reset() { ExitCount = 0; EnterCount = 0; }

        static void OnExit(ExampleData* data) => ExitCount++;
        static void OnEnter(ExampleData* data) => EnterCount++;
        static bool ShouldRun(ExampleData* data) => true;

        public static readonly LogicOperation<ExampleData> ExitOp = new(&OnExit, &ShouldRun);
        public static readonly LogicOperation<ExampleData> EnterOp = new(&OnEnter, &ShouldRun);

        public static readonly Logics<ExampleData> ExitLogics = new(ref ExitOp);
        public static readonly Logics<ExampleData> EnterLogics = new(ref EnterOp);
    }
    
    [Test]
    public void Test14_SelfTransition_IsIgnored()
    {
        // Verifies that transitioning to the same state is forbidden and triggers no logic
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        
        var data = new ExampleData() { x = 10 };
        fsm.Entry(ref data);

        var predicate = ExamplePredicates.IsGreaterThanOne();

        // Setup counters in logic
        TransitionCounter.Reset();
        fsm.layers[0].states[(int)TestLayerOne.L1S1].OnExitState = TransitionCounter.ExitLogics;
        fsm.layers[0].states[(int)TestLayerOne.L1S1].OnEnterState = TransitionCounter.EnterLogics;

        // Add transition S1 -> S1 (Self)
        fsm.AddDirectTransition(0, TestLayerOne.L1S1, TestLayerOne.L1S1, ref predicate);
        
        // Poll transitions
        fsm.TryPollTransitions(ref data);

        // Assertions
        Assert.AreEqual((int)TestLayerOne.L1S1, fsm.layers[0].currentState, "State should not have changed");
        Assert.AreEqual(0, TransitionCounter.ExitCount, "OnExit should NOT have run for self-transition");
        Assert.AreEqual(0, TransitionCounter.EnterCount, "OnEnter should NOT have run for self-transition");

        fsm.Dispose();
    }
    
    [Test]
    public void Test15_Stress_MultipleLayersPerformance()
    {
        // Stress test: 100 layers with transitions on every layer
        const int LAYER_COUNT = 100;
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(LAYER_COUNT);
        
        var data = new ExampleData() { x = 2 };
        var predicate = ExamplePredicates.IsGreaterThanOne();

        for (int i = 0; i < LAYER_COUNT; i++)
        {
            fsm.InitLayer<TestLayerOne, ExampleData>(i);
            fsm.AddAnyTransition(i, TestLayerOne.L1S2, ref predicate);
        }

        fsm.Entry(ref data);
        
        // Measure or just verify correctness across large volume
        fsm.TryPollTransitions(ref data);

        for (int i = 0; i < LAYER_COUNT; i++)
        {
            Assert.AreEqual((int)TestLayerOne.L1S2, fsm.layers[i].currentState, $"Layer {i} failed to transition");
        }

        fsm.Dispose();
    }
    
    [Test]
    public void Test16_InvalidEnum_ThrowsException()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        fsm.Initialize(1);
        var data = new ExampleData() { x = 2 };

        // Init layer 0 with TestLayerOne
        fsm.InitLayer<TestLayerOne, ExampleData>(0);
        fsm.Entry(ref data);

        var predicate = ExamplePredicates.IsGreaterThanOne();

        // Attempting to add a transition using TestLayerTwo on a layer initialized with TestLayerOne
        Assert.Throws<ArgumentException>(() => {
            fsm.AddAnyTransition(0, TestLayerTwo.L2S1, ref predicate);
        }, "Should throw when adding Any transition with wrong enum type");

        Assert.Throws<ArgumentException>(() => {
            fsm.AddDirectTransition(0, TestLayerTwo.L2S1, TestLayerTwo.L2S2, ref predicate);
        }, "Should throw when adding Direct transition with wrong enum type");

        fsm.Dispose();
    }
    
    [Test]
    public void Test17_EntryWithoutLayers_ThrowsException()
    {
        ProSM<ExampleData> fsm = new ProSM<ExampleData>();
        // fsm.Initialize(0); // Or skip initialization entirely
    
        var data = new ExampleData() { x = 0 };

        Assert.Throws<InvalidOperationException>(() => {
            fsm.Entry(ref data);
        }, "Should throw if Entry is called on an uninitialized FSM");

        fsm.Dispose();
    }

}
