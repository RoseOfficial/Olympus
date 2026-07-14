using System;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Unit tests for RotationManager factory-failure caching (finding #21).
/// A throwing factory must be attempted exactly once per job session;
/// subsequent frames with the same failing job must not re-invoke the factory.
/// </summary>
public class RotationManagerTests
{
    private static Mock<IRotation> MakeRotation(uint jobId)
    {
        var rotation = new Mock<IRotation>();
        rotation.SetupGet(r => r.SupportedJobIds).Returns(new[] { jobId });
        return rotation;
    }

    // -------------------------------------------------------------------------
    // Happy path: successful factory caches the rotation
    // -------------------------------------------------------------------------

    [Fact]
    public void UpdateActiveRotation_SuccessfulFactory_ReturnsTrue()
    {
        var manager = new RotationManager();
        var rotation = MakeRotation(24U);
        var callCount = 0;
        manager.RegisterFactory(24U, () => { callCount++; return rotation.Object; });

        var result = manager.UpdateActiveRotation(24U);

        Assert.True(result);
        Assert.Equal(1, callCount);
    }

    [Fact]
    public void UpdateActiveRotation_SameJobSecondCall_DoesNotReinvokeFactory()
    {
        var manager = new RotationManager();
        var rotation = MakeRotation(24U);
        var callCount = 0;
        manager.RegisterFactory(24U, () => { callCount++; return rotation.Object; });

        manager.UpdateActiveRotation(24U);
        manager.UpdateActiveRotation(24U);

        Assert.Equal(1, callCount);
    }

    // -------------------------------------------------------------------------
    // Finding #21: factory failure caching
    // -------------------------------------------------------------------------

    [Fact]
    public void UpdateActiveRotation_FailingFactory_ReturnsFalseOnFirstCall()
    {
        var log = new Mock<IPluginLog>();
        var manager = new RotationManager(log.Object);
        manager.RegisterFactory(24U, () => throw new InvalidOperationException("rotation init failed"));

        var result = manager.UpdateActiveRotation(24U);

        Assert.False(result);
    }

    [Fact]
    public void UpdateActiveRotation_FailingFactory_SecondCallDoesNotReinvokeFactory()
    {
        var log = new Mock<IPluginLog>();
        var manager = new RotationManager(log.Object);
        var callCount = 0;
        manager.RegisterFactory(24U, () =>
        {
            callCount++;
            throw new InvalidOperationException("rotation init failed");
        });

        manager.UpdateActiveRotation(24U);  // first call: factory invoked, fails
        manager.UpdateActiveRotation(24U);  // second call: must NOT invoke factory again

        Assert.Equal(1, callCount);
    }

    [Fact]
    public void UpdateActiveRotation_FailingFactory_ReturnsFalseOnSubsequentCalls()
    {
        var manager = new RotationManager();
        manager.RegisterFactory(24U, () => throw new InvalidOperationException("rotation init failed"));

        manager.UpdateActiveRotation(24U);
        var result = manager.UpdateActiveRotation(24U);

        Assert.False(result);
    }

    [Fact]
    public void UpdateActiveRotation_AfterJobChange_FailedJobIsRetried()
    {
        var log = new Mock<IPluginLog>();
        var manager = new RotationManager(log.Object);
        var failCount = 0;
        var callCount = 0;
        // First call throws; second call succeeds (simulating a fix after job switch).
        var rotation = MakeRotation(24U);
        manager.RegisterFactory(24U, () =>
        {
            callCount++;
            if (failCount++ < 1)
                throw new InvalidOperationException("transient failure");
            return rotation.Object;
        });

        manager.UpdateActiveRotation(24U);  // job 24, fails
        manager.UpdateActiveRotation(30U);  // switch to job 30 — clears failure cache
        var result = manager.UpdateActiveRotation(24U);  // back to job 24 — should retry

        Assert.True(result);
        Assert.Equal(2, callCount);  // called once on first attempt, once after retry
    }

    [Fact]
    public void UpdateActiveRotation_JobWithNoFactory_ReturnsFalse()
    {
        var manager = new RotationManager();

        var result = manager.UpdateActiveRotation(99U);

        Assert.False(result);
    }
}
