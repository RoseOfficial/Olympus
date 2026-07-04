using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.ThemisCore.Abilities;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Rotation.ThemisCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;

namespace Olympus.Tests.Rotation.ThemisCore.Modules;

/// <summary>
/// Verifies Auto*/Enable* split for Intervene:
///   - In-melee weave only fires when AutoIntervene is on.
///   - Out-of-melee gap-close fires regardless of AutoIntervene.
/// </summary>
public class DamageModuleInterveneTests
{
    private readonly DamageModule _module = new();

    // -----------------------------------------------------------------------
    // 1. In-melee: AutoIntervene off -> weave not pushed
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_InMelee_AutoIntervene_Off_DoesNotPushWeave()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(PLDActions.Intervene.ActionId)).Returns(true);

        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableIntervene = true;
        config.Tank.AutoIntervene = false;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == ThemisAbilities.Intervene);
    }

    // -----------------------------------------------------------------------
    // 2. In-melee: AutoIntervene on -> weave pushed at priority 4
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_InMelee_AutoIntervene_On_PushesWeaveAtPriority4()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(PLDActions.Intervene.ActionId)).Returns(true);

        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableIntervene = true;
        config.Tank.AutoIntervene = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == ThemisAbilities.Intervene && c.Priority == 4);
    }

    // -----------------------------------------------------------------------
    // 3. Out-of-melee: gap-close pushed regardless of AutoIntervene
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_OutOfMelee_GapClose_PushedRegardlessOfAutoIntervene()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithOutOfMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(PLDActions.Intervene.ActionId)).Returns(true);

        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableIntervene = true;
        config.Tank.AutoIntervene = false; // off, but gap-close path is unconditional

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == ThemisAbilities.Intervene && c.Priority == 4);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static Mock<ITargetingService> BuildTargetingWithMeleeEnemy(
        Mock<IBattleNpc> enemy, int enemyCount = 1)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemyCount);

        var safetyMock = new Mock<IGapCloserSafetyService>();
        safetyMock.Setup(x => x.ShouldBlockGapCloser(It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>()))
            .Returns(false);
        safetyMock.Setup(x => x.LastBlockReason).Returns((string?)null);
        targeting.Setup(x => x.GapCloserSafety).Returns(safetyMock.Object);

        return targeting;
    }

    private static Mock<ITargetingService> BuildTargetingWithOutOfMeleeEnemy(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(0);

        var safetyMock = new Mock<IGapCloserSafetyService>();
        safetyMock.Setup(x => x.ShouldBlockGapCloser(It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>()))
            .Returns(false);
        safetyMock.Setup(x => x.LastBlockReason).Returns((string?)null);
        targeting.Setup(x => x.GapCloserSafety).Returns(safetyMock.Object);

        return targeting;
    }

    private static IThemisContext CreateContext(
        bool inCombat = true,
        byte level = 100,
        Olympus.Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null)
    {
        config ??= ThemisTestContext.CreateDefaultPaladinConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IThemisContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(false);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.Debug).Returns(new ThemisDebugState());
        mock.Setup(x => x.CurrentTarget).Returns((IBattleChara?)null);

        // PLD-specific state: all defaults that short-circuit other TryPush* methods
        // so only Intervene candidates reach the oGCD queue in these focused tests.
        mock.Setup(x => x.HasRequiescat).Returns(false);      // skip Confiteor chain + Holy phase spenders
        mock.Setup(x => x.RequiescatStacks).Returns(0);
        mock.Setup(x => x.ConfiteorStep).Returns(0);          // double-guard on Confiteor chain
        mock.Setup(x => x.HasSwordOath).Returns(false);       // skip Atonement chain
        mock.Setup(x => x.SwordOathStacks).Returns(0);
        mock.Setup(x => x.AtonementStep).Returns(0);
        mock.Setup(x => x.HasFightOrFlight).Returns(false);   // skip Atonement chain (both conditions false)
        mock.Setup(x => x.FightOrFlightRemaining).Returns(0f);
        mock.Setup(x => x.GoringBladeRemaining).Returns(15f); // skip GoringBlade (15f >= 5f threshold)
        mock.Setup(x => x.HasBladeOfHonor).Returns(false);    // skip BladeOfHonor fast-path
        mock.Setup(x => x.HasDivineMight).Returns(false);
        mock.Setup(x => x.HasActiveMitigation).Returns(false);
        mock.Setup(x => x.HasHallowedGround).Returns(false);
        mock.Setup(x => x.OathGauge).Returns(0);
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(30f);

        return mock.Object;
    }
}
