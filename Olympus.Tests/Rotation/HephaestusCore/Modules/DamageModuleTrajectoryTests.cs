using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HephaestusCore.Abilities;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.HephaestusCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;

namespace Olympus.Tests.Rotation.HephaestusCore.Modules;

/// <summary>
/// Verifies Auto*/Enable* split for Trajectory:
///   - In-melee weave only fires when AutoTrajectory is on.
///   - Out-of-melee gap-close fires regardless of AutoTrajectory.
/// </summary>
public class DamageModuleTrajectoryTests
{
    private readonly DamageModule _module = new();

    // -----------------------------------------------------------------------
    // 1. In-melee: AutoTrajectory off -> weave not pushed
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_InMelee_AutoTrajectory_Off_DoesNotPushWeave()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(GNBActions.Trajectory.ActionId)).Returns(true);

        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableTrajectory = true;
        config.Tank.AutoTrajectory = false;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService, config: config, level: 56);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == GnbAbilities.Trajectory);
    }

    // -----------------------------------------------------------------------
    // 2. In-melee: AutoTrajectory on -> weave pushed at priority 4
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_InMelee_AutoTrajectory_On_PushesWeaveAtPriority4()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(GNBActions.Trajectory.ActionId)).Returns(true);

        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableTrajectory = true;
        config.Tank.AutoTrajectory = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService, config: config, level: 56);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == GnbAbilities.Trajectory && c.Priority == 4);
    }

    // -----------------------------------------------------------------------
    // 3. Out-of-melee: gap-close pushed regardless of AutoTrajectory
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_OutOfMelee_GapClose_PushedRegardlessOfAutoTrajectory()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithOutOfMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(false);
        actionService.Setup(x => x.IsActionReady(GNBActions.Trajectory.ActionId)).Returns(true);

        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableTrajectory = true;
        config.Tank.AutoTrajectory = false; // off, but gap-close path is unconditional

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService, config: config, level: 56);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == GnbAbilities.Trajectory && c.Priority == 4);
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

    private static IHephaestusContext CreateContext(
        bool inCombat = true,
        byte level = 100,
        int cartridges = 0,
        bool hasMaxCartridges = false,
        bool hasNoMercy = false,
        bool isReadyToReign = false,
        int comboStep = 0,
        uint lastComboAction = 0,
        Olympus.Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null)
    {
        config ??= HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IHephaestusContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(false);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);

        // GNB gauge/combo state
        mock.Setup(x => x.Cartridges).Returns(cartridges);
        mock.Setup(x => x.HasMaxCartridges).Returns(hasMaxCartridges || cartridges >= 3);
        mock.Setup(x => x.CanUseGnashingFang).Returns(cartridges >= 1);
        mock.Setup(x => x.CanUseDoubleDown).Returns(cartridges >= 2);
        mock.Setup(x => x.HasNoMercy).Returns(hasNoMercy);
        mock.Setup(x => x.NoMercyRemaining).Returns(0f);
        mock.Setup(x => x.HasSonicBreakDot).Returns(false);
        mock.Setup(x => x.HasBowShockDot).Returns(false);
        mock.Setup(x => x.IsInGnashingFangCombo).Returns(false);
        mock.Setup(x => x.GnashingFangStep).Returns(0);
        mock.Setup(x => x.IsReadyToRip).Returns(false);
        mock.Setup(x => x.IsReadyToTear).Returns(false);
        mock.Setup(x => x.IsReadyToGouge).Returns(false);
        mock.Setup(x => x.IsReadyToBlast).Returns(false);
        mock.Setup(x => x.IsReadyToBrand).Returns(false);
        mock.Setup(x => x.IsReadyToReign).Returns(isReadyToReign);
        mock.Setup(x => x.HasAnyContinuationReady).Returns(false);
        mock.Setup(x => x.IsInReignCombo).Returns(false);
        mock.Setup(x => x.ReignComboStep).Returns(0);
        mock.Setup(x => x.ComboStep).Returns(comboStep);
        mock.Setup(x => x.LastComboAction).Returns(lastComboAction);
        mock.Setup(x => x.ComboTimeRemaining).Returns(30f);
        mock.Setup(x => x.CurrentTarget).Returns((IBattleChara?)null);

        mock.Setup(x => x.Debug).Returns(new HephaestusDebugState());

        return mock.Object;
    }
}
