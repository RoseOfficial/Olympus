using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ThemisCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ThemisCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Themis (PLD) BuffModule.
/// Verifies the FightOrFlight Toggle constant gates correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void FightOrFlight_Dispatches_WhenEnabled()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.AutoTankStance = false;
        config.Tank.EnableFightOrFlight = true;

        var enemy = new Mock<IBattleNpc>();
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = ThemisTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            comboStep: 0);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.FightOrFlight.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void FightOrFlight_DoesNotDispatch_WhenDisabled()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.AutoTankStance = false;
        config.Tank.EnableFightOrFlight = false;

        var enemy = new Mock<IBattleNpc>();
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = ThemisTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            comboStep: 0);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == PLDActions.FightOrFlight.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
