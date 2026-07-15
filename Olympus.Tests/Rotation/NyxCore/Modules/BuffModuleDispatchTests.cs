using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.NyxCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.NyxCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Nyx (DRK) BuffModule.
/// Verifies the BloodWeapon Toggle constant gates correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void BloodWeapon_Dispatches_WhenEnabled()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.AutoTankStance = false;
        config.Tank.EnableBloodWeapon = true;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        // HasBloodWeapon is false by default (NyxStatusHelper reads null StatusList → no status found)
        var context = NyxTestContext.Create(
            config: config,
            actionService: actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRKActions.BloodWeapon.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void BloodWeapon_DoesNotDispatch_WhenDisabled()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.AutoTankStance = false;
        config.Tank.EnableBloodWeapon = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = NyxTestContext.Create(
            config: config,
            actionService: actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRKActions.BloodWeapon.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
