using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.HephaestusCore.Abilities;
using Olympus.Rotation.HephaestusCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.HephaestusCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Hephaestus (GNB) DamageModule.
/// Verifies the production AbilityBehavior's ProcBuff constant (ReadyToRip) routes
/// through a real DispatchOgcd pass. Catches wrong status-ID constants.
/// </summary>
public class DamageModuleDispatchTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void JugularRip_Dispatches_WhenReadyToRipActive()
    {
        var enemy = new Mock<IBattleNpc>();
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        actionService.Setup(x => x.PlayerHasStatus(GNBActions.StatusIds.ReadyToRip)).Returns(true);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = HephaestusTestContext.Create(
            actionService: actionService,
            targetingService: targetingService,
            cartridges: 1,
            level: 70);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == GNBActions.JugularRip.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void JugularRip_DoesNotDispatch_WithoutProc()
    {
        var enemy = new Mock<IBattleNpc>();
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        // PlayerHasStatus defaults to false — proc gate must reject JugularRip.

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = HephaestusTestContext.Create(
            actionService: actionService,
            targetingService: targetingService,
            cartridges: 1,
            level: 70);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == GNBActions.JugularRip.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
