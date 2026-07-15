using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.IrisCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.IrisCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Iris (PCT) BuffModule.
/// Verifies the StarryMuse Toggle gate routes correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void StarryMuse_Dispatches_WhenEnabled()
    {
        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.EnableStarryMuse = true;
        // Leave FindEnemy returning null (default) so Portrait (priority 1, target-requiring)
        // returns early, letting StarryMuse (priority 2, self-targeted) dispatch.

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = IrisTestContext.Create(
            config: config,
            actionService: actionService,
            starryMuseReady: true,      // cooldown ready
            hasLandscapeCanvas: true,   // landscape canvas painted
            hasStarryMuse: false);      // no active buff

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarryMuse.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void StarryMuse_DoesNotDispatch_WhenDisabled()
    {
        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.EnableStarryMuse = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = IrisTestContext.Create(
            config: config,
            actionService: actionService,
            starryMuseReady: true,
            hasLandscapeCanvas: true,
            hasStarryMuse: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PCTActions.StarryMuse.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
