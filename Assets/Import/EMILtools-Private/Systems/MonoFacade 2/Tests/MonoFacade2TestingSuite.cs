using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using LogicArchitecture;
using LogicExamples;

public class MonoFacade2TestingSuite
{
    // A Test behaves as an ordinary method
    [Test]
    public void Test1_Initializes()
    {
        var data = new ExampleLogic.ExampleData() { x = 1f };
        Assert.IsNotNull(data);
        Assert.AreEqual(1f, data.x);

        var handle = ExampleLogic.Handle;
        Assert.IsNotNull(handle);
        
        var shouldRun = handle.ShouldRun(data);
        Assert.IsTrue(shouldRun);
        
        if(shouldRun) handle.Run(ref data);
        Assert.AreEqual(2f, data.x);
    }
}
