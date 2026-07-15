using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.PersephoneCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.PersephoneCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Persephone (SMN) BuffModule.
/// Verifies the SearingLight Toggle gate routes correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void SearingLight_Dispatches_WhenEnabled()
    {
        var config = PersephoneTestContext.CreateDefaultSmnConfiguration();
        config.Summoner.EnableSearingLight = true;
        // Leave FindEnemy returning null (default) to block target-requiring abilities
        // (Enkindle, AstralFlow, MountainBuster, SearingFlash) so SearingLight (priority 3) wins.

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PersephoneTestContext.Create(
            config: config,
            actionService: actionService,
            isDemiSummonActive: true,   // required by SearingLight gate
            searingLightReady: true,    // cooldown ready
            hasSearingLight: false);    // no active buff

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.SearingLight.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void SearingLight_DoesNotDispatch_WhenDisabled()
    {
        var config = PersephoneTestContext.CreateDefaultSmnConfiguration();
        config.Summoner.EnableSearingLight = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = PersephoneTestContext.Create(
            config: config,
            actionService: actionService,
            isDemiSummonActive: true,
            searingLightReady: true,
            hasSearingLight: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SMNActions.SearingLight.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
