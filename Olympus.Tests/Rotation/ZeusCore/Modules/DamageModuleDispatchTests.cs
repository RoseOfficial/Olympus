using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ZeusCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ZeusCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Zeus (DRG) DamageModule.
/// Verifies the MirageDive ProcBuff gate (DiveReady) routes correctly through a
/// real DispatchOgcd pass. Catches wrong status-ID constants or missing proc wiring.
/// </summary>
public class DamageModuleDispatchTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void MirageDive_Dispatches_WhenDiveReadyProcActive()
    {
        var config = ZeusTestContext.CreateDefaultDragoonConfiguration();
        config.Dragoon.EnableMirageDive = true;

        var enemy = new Mock<IBattleNpc>();
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        actionService.Setup(x => x.PlayerHasStatus(DRGActions.StatusIds.DiveReady)).Returns(true);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(),
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = ZeusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            hasDiveReady: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.MirageDive.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void MirageDive_DoesNotDispatch_WhenDiveReadyProcAbsent()
    {
        var config = ZeusTestContext.CreateDefaultDragoonConfiguration();
        config.Dragoon.EnableMirageDive = true;

        var enemy = new Mock<IBattleNpc>();
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);
        // PlayerHasStatus defaults to false -- proc gate must reject MirageDive.

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(),
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        // hasDiveReady=true so the module guard passes and the candidate IS pushed.
        // PlayerHasStatus defaults to false, so the scheduler's ProcBuff gate rejects it.
        // This ensures the ProcBuff gate is the sole reason for the negative outcome.
        var context = ZeusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            hasDiveReady: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DRGActions.MirageDive.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
