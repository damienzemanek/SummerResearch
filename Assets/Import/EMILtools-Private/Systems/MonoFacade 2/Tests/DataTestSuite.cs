using System;
using DataArchitecture;
using UnityEngine;
using NUnit.Framework;
using Unity.Collections;
using static LogicExamples.ExampleLogic;


public class DataTestSuite
{

    [Test]
    public void Test1_Initalizes()
    {
        int capacity = 10;
        Data<ExampleData> data = new Data<ExampleData>(capacity, Allocator.Persistent);
        Assert.IsNotNull(data);
        
        data.Dispose();
    }
    
    [Test]
    public void Test2_Allocates()
    {
        unsafe
        {
            int capacity = 10;
            Data<ExampleData> data = new Data<ExampleData>(capacity, Allocator.Persistent);
        
            data.Allocate(new ExampleData() { x = 1f }, out int id);
            Assert.AreEqual(0, id);

            var allocatedData = data[id];
            Assert.IsNotNull(allocatedData);
        
            var dataInside = data[id];
            Assert.AreEqual(1f, dataInside.x);
        
            data.Dispose();
        }
    }
    
    [Test]
    public void Test3_GetOutsideCapacity_Throws()
    {
        int capacity = 10;
        Data<ExampleData> data = new Data<ExampleData>(capacity, Allocator.Persistent);
        
        data.Allocate(new ExampleData() { x = 1f }, out int id);
        Assert.AreEqual(0, id);

        var allocatedData = data[id];
        Assert.IsNotNull(allocatedData);
        
        Assert.Throws<IndexOutOfRangeException>(() => { var dataInside = data[capacity]; });
        
        data.Dispose();
    }
    
    [Test]
    public void Test4_GetOutsideAllocatedButStillWithinCapacity_Throws()
    {
        int capacity = 10;
        Data<ExampleData> data = new Data<ExampleData>(capacity, Allocator.Persistent);
        
        data.Allocate(new ExampleData() { x = 1f }, out int id);
        Assert.AreEqual(0, id);

        var allocatedData = data[id];
        Assert.IsNotNull(allocatedData);
        
        Assert.Throws<IndexOutOfRangeException>(() => { var dataInside = data[1]; });
        
        data.Dispose();
    }
    
    [Test]
    public void Test5_ExpandsWhenFull()
    {
        int initialCapacity = 2;
        Data<ExampleData> data = new Data<ExampleData>(initialCapacity, Allocator.Persistent);
    
        data.Allocate(new ExampleData { x = 1f }, out _);
        data.Allocate(new ExampleData { x = 2f }, out _);
        // This should trigger SetCapacity(4)
        data.Allocate(new ExampleData { x = 3f }, out int id); 
    
        Assert.AreEqual(2, id);
        Assert.AreEqual(3f, data[2].x);
        Assert.AreEqual(1f, data[0].x); // Verify old data is still there
    
        data.Dispose();
    }
    
    [Test]
    public void Test6_SupportsReferenceMutation()
    {
        Data<ExampleData> data = new Data<ExampleData>(10, Allocator.Persistent);
        data.Allocate(new ExampleData { x = 10f }, out int id);
    
        // Mutate via ref
        ref var storedData = ref data[id];
        storedData.x = 20f;
    
        Assert.AreEqual(20f, data[id].x);
    
        data.Dispose();
    }
    
    [Test]
    public void Test8_MultipleAllocations_TracksSize()
    {
        Data<ExampleData> data = new Data<ExampleData>(1, Allocator.Persistent);
        for(int i = 0; i < 100; i++)
        {
            data.Allocate(new ExampleData { x = (float)i }, out _);
        }
        Assert.AreEqual(100, data.currentSize);
        Assert.AreEqual(99f, data[99].x);
        data.Dispose();
    }
}
