using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AresCore.Abilities;
using Olympus.Rotation.AresCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AresCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Ares (WAR) BuffModule.
/// Verifies the InnerRelease Toggle constant gates correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void InnerRelease_Dispatches_WhenEnabled()
    {
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.AutoTankStance = false;
        config.Tank.EnableInnerRelease = true;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = AresTestContext.CreateMock(
            hasSurgingTempest: true,
            hasInnerRelease: false,
            beastGauge: 50,   // bypass: if (BeastGauge < 50 && SurgingTempestRemaining > 15f) return;
            config: config,
            actionService: actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.InnerRelease.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void InnerRelease_DoesNotDispatch_WhenDisabled()
    {
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.AutoTankStance = false;
        config.Tank.EnableInnerRelease = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = AresTestContext.CreateMock(
            hasSurgingTempest: true,
            hasInnerRelease: false,
            config: config,
            actionService: actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == WARActions.InnerRelease.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
