using System;
using NUnit.Framework;
using StateArchitecture;
using UnityEngine;

public class PredicateTestSuite : MonoBehaviour
{
    struct ExampleData
    {
        public int value;
    }

    static unsafe class ExamplePredicate
    {   
        // Little verbose, but oh well
        public static Predicate Create() => new Predicate(&IsZero);
        static bool IsZero(void* ptr)
        {
            ExampleData* data = (ExampleData*)ptr;
            return data->value == 0;
        }
    }
    // originally looked like
    // had to create Factory funciton Create() in the wrapper
    // internal static unsafe class ExamplePredicate
    // {
    //     public static readonly delegate*<void*, bool> IsZero = &isZero;
    //
    //     static bool isZero(void* ptr)
    //     {
    //         ExampleData* data = (ExampleData*)ptr;
    //         return data->value == 0;
    //     } 
    // }
    // meant that the original initialization had to be unsafe
    // [Test]
    // public unsafe void Test1_Initalizes()
    // {
    //     ExampleData data = new ExampleData() { value = 0 };
    //     Assert.IsNotNull(data);
    //
    //     unsafe
    //     {
    //         Predicate predicate = new Predicate(&ExamplePredicate.IsZero);
    //     }
    //     Assert.IsNotNull(predicate);
    // }


    [Test]
    public void Test1_Initalizes()
    {
        ExampleData data = new ExampleData() { value = 0 };
        Assert.IsNotNull(data);
        
        Predicate predicate = ExamplePredicate.Create();
        Assert.IsNotNull(predicate);
    }
    

    [Test]
    public void Test2_Evaluates()
    {
        ExampleData data = new ExampleData() { value = 0 };
        Predicate predicate = ExamplePredicate.Create();
        bool result = predicate.Evaluate(ref data);
        Assert.IsTrue(result);
    }
    // originally had to use PredicateLogic<T>.Evaluate(ref predicate, ref data)
    // but that was unsafe and required the caller to know about the internal unsafe implementation,
    // so I changed it to Predicate.Evaluate(ref data) using an extension method using `this ref Predicate`
    
    
    [Test]
    public void Test3_EvaluatesFalse()
    {
        ExampleData data = new ExampleData() { value = 10 }; // Not zero
        Predicate predicate = ExamplePredicate.Create();
        bool result = predicate.Evaluate(ref data);
        Assert.IsFalse(result, "Predicate should return false for non-zero value");
    }
    
    struct HealthData { public float hp; }
    static unsafe class HealthPredicate {
        public static Predicate Create() => new Predicate(&IsDead);
        static bool IsDead(void* ptr) => ((HealthData*)ptr)->hp <= 0;
    }

    [Test]
    public void Test4_MultipleDataTypes()
    {
        HealthData hData = new HealthData() { hp = -1f };
        Predicate hPredicate = HealthPredicate.Create();
        Assert.IsTrue(hPredicate.Evaluate(ref hData));
    }
    
    [Test]
    public void Test5_ConsistentEvaluation()
    {
        ExampleData data = new ExampleData() { value = 0 };
        Predicate predicate = ExamplePredicate.Create();
    
        Assert.IsTrue(predicate.Evaluate(ref data));
        Assert.IsTrue(predicate.Evaluate(ref data)); // Second call
        Assert.AreEqual(0, data.value, "Data should not be modified by evaluation");
    }
    
    [Test]
    public void Test6_UninitializedPredicateBehavior()
    {
        Predicate emptyPredicate = default;
        var data = new ExampleData() { value = 0 };
        // This is expected to be null. 
        // In production, we might want a 'predicate.IsCreated' property.
        Assert.Throws<NullReferenceException>(() => emptyPredicate.Evaluate(ref data), "Evaluating an uninitialized predicate should throw an exception");
    }
    
}
