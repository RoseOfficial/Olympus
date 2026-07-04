using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.NyxCore.Abilities;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;

namespace Olympus.Tests.Rotation.NyxCore.Modules;

/// <summary>
/// Verifies Auto*/Enable* split for Shadowstride:
///   - In-melee weave only fires when AutoShadowstride is on.
///   - Out-of-melee gap-close fires regardless of AutoShadowstride.
/// </summary>
public class DamageModuleShadowstrideTests
{
    private readonly DamageModule _module = new();

    // -----------------------------------------------------------------------
    // 1. In-melee: AutoShadowstride off -> weave not pushed
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_InMelee_AutoShadowstride_Off_DoesNotPushWeave()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRKActions.Shadowstride.ActionId)).Returns(true);

        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableShadowstride = true;
        config.Tank.AutoShadowstride = false;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NyxAbilities.Shadowstride && c.Priority == 7);
    }

    // -----------------------------------------------------------------------
    // 2. In-melee: AutoShadowstride on -> weave pushed at priority 7
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_InMelee_AutoShadowstride_On_PushesWeaveAtPriority7()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRKActions.Shadowstride.ActionId)).Returns(true);

        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableShadowstride = true;
        config.Tank.AutoShadowstride = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NyxAbilities.Shadowstride && c.Priority == 7);
    }

    // -----------------------------------------------------------------------
    // 3. Out-of-melee: gap-close pushed regardless of AutoShadowstride
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_OutOfMelee_GapClose_PushedRegardlessOfAutoShadowstride()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithOutOfMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(DRKActions.Shadowstride.ActionId)).Returns(true);

        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableShadowstride = true;
        config.Tank.AutoShadowstride = false; // off, but gap-close path is unconditional

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NyxAbilities.Shadowstride && c.Priority == 4);
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
        // No melee target; engage target found via wide search
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

    private static INyxContext CreateContext(
        bool inCombat = true,
        byte level = 100,
        Olympus.Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null)
    {
        config ??= NyxTestContext.CreateDefaultDarkKnightConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<INyxContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(false);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.Debug).Returns(new NyxDebugState());
        mock.Setup(x => x.CurrentTarget).Returns((IBattleChara?)null);

        // DRK-specific state: defaults that short-circuit other TryPush* methods
        // so only Shadowstride reaches the queue in these focused tests.
        mock.Setup(x => x.HasDarkArts).Returns(false);       // skip TryPushDarkArtsProc
        mock.Setup(x => x.HasSaltedEarth).Returns(true);     // skip TryPushSaltedEarth (already active); SaltAndDarkness IsActionReady=false
        mock.Setup(x => x.HasEnoughMpForEdge).Returns(false);// skip TryPushDarksideMaintenance
        mock.Setup(x => x.HasDelirium).Returns(false);       // skip TryPushDeliriumCombo + blood spender free-path
        mock.Setup(x => x.DeliriumStacks).Returns(0);
        mock.Setup(x => x.HasScornfulEdge).Returns(false);   // skip TryPushDisesteem
        mock.Setup(x => x.BloodGauge).Returns(0);            // skip TryPushBloodSpender (< 50)
        mock.Setup(x => x.HasDarkside).Returns(false);
        mock.Setup(x => x.DarksideRemaining).Returns(0f);
        mock.Setup(x => x.CurrentMp).Returns(0);
        mock.Setup(x => x.MaxMp).Returns(10000);
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(30f);

        return mock.Object;
    }
}
