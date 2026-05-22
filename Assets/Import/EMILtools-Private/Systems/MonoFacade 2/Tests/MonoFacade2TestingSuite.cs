using NUnit.Framework;
using LogicArchitecture;
using LogicExamples;

public class MonoFacade2TestingSuite
{
    /// <summary>
    /// This test has to pass because data is initialized and handle is assigned
    /// </summary>
    [Test]
    public void Test1_InitializeDataAndHandle()
    {
        var data = new ExampleLogic.ExampleData { x = 1f };
        var handle = ExampleLogic.Operation;

        Assert.AreEqual(1f, data.x);
        Assert.IsNotNull(handle);
    }

    /// <summary>
    /// This test has to pass because ShouldRun() returns true
    /// </summary>
    [Test]
    public void Test2_ShouldRun()
    {
        var data = new ExampleLogic.ExampleData { x = 1f };
        var handle = ExampleLogic.Operation;

        var shouldRun = handle.ShouldRun(in data);
        Assert.IsTrue(shouldRun);
    }

    /// <summary>
    /// This test has to pass because TryRun() increments data from 1 to 2
    /// </summary>
    [Test]
    public void Test3_Runs()
    {
        var data = new ExampleLogic.ExampleData { x = 1f };
        var handle = ExampleLogic.Operation;

        handle.Run(ref data);

        Assert.AreEqual(2f, data.x);
    }

    /// <summary>
    /// This test has to pass because ShouldRun() returns true,
    /// so TryRun() is executed and increments data from 1 to 2
    /// </summary>
    [Test]
    public void Test4_ShouldRunPassesRun()
    {
        var data = new ExampleLogic.ExampleData { x = 1f };
        var handle = ExampleLogic.Operation;

        var shouldRun = handle.ShouldRun(in data);
        Assert.IsTrue(shouldRun);

        handle.Run(ref data);

        Assert.AreEqual(2f, data.x);
    }

    /// <summary>
    /// This test has to pass because TryRun() should NOT execute,
    /// since ShouldRun() returns false when x == 0
    /// </summary>
    [Test]
    public void Test5_ShouldRunFailsRun()
    {
        var data = new ExampleLogic.ExampleData { x = 0f };
        var handle = ExampleLogic.Operation;

        var shouldRun = handle.ShouldRun(in data);

        if (shouldRun)
            handle.Run(ref data);

        Assert.AreEqual(0f, data.x);
    }
}