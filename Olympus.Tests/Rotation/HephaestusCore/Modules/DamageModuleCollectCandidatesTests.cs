using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
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

public class DamageModuleCollectCandidatesTests
{
    private readonly DamageModule _module = new();

    // -----------------------------------------------------------------------
    // 1. Gnashing Fang combo: both SavageClaw and WickedTalon always pushed
    //    at priority 1 so the scheduler's ComboStep gate can pick the right one.
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_GnashingFangCombo_BothCandidatesPushed_AtPriority1()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // level 60 = GnashingFang is available; cartridges > 0 triggers StartGnashingFang
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 1, level: 60);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        // Both continuations pushed; scheduler's ComboStep gate picks the active one
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.SavageClaw && c.Priority == 1);
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.WickedTalon && c.Priority == 1);
    }

    // -----------------------------------------------------------------------
    // 2. Gnashing Fang chain starter: GnashingFang pushed at priority 5
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_GnashingFangStart_PushedAtPriority5_WhenCartridgesAvailable()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCooldownRemaining(GNBActions.NoMercy.ActionId)).Returns(30f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 1, level: 60);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.GnashingFang && c.Priority == 5);
    }

    // -----------------------------------------------------------------------
    // 3. Reign chain: NobleBlood and LionHeart both pushed at priority 2
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ReignChain_BothCandidatesPushed_AtPriority2()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 0, level: 100, isReadyToReign: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        // All three Reign candidates pushed; scheduler's ComboStep/ProcBuff gates select
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.ReignOfBeasts && c.Priority == 2);
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.NobleBlood    && c.Priority == 2);
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.LionHeart     && c.Priority == 2);
    }

    // -----------------------------------------------------------------------
    // 4. Continuations: all 5 pushed when config allows; scheduler gates prune
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_Continuations_AllFivePushed_AtPriority1()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableContinuation = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 0, level: 100, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        // All 5 continuation behaviors must be present at priority 1
        Assert.Contains(ogcd, c => c.Behavior == GnbAbilities.JugularRip   && c.Priority == 1);
        Assert.Contains(ogcd, c => c.Behavior == GnbAbilities.AbdomenTear  && c.Priority == 1);
        Assert.Contains(ogcd, c => c.Behavior == GnbAbilities.EyeGouge     && c.Priority == 1);
        Assert.Contains(ogcd, c => c.Behavior == GnbAbilities.Hypervelocity && c.Priority == 1);
        Assert.Contains(ogcd, c => c.Behavior == GnbAbilities.FatedBrand   && c.Priority == 1);
    }

    [Fact]
    public void CollectCandidates_Continuations_NotPushed_WhenDisabled()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableContinuation = false;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 0, level: 100, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == GnbAbilities.JugularRip);
        Assert.DoesNotContain(ogcd, c => c.Behavior == GnbAbilities.AbdomenTear);
        Assert.DoesNotContain(ogcd, c => c.Behavior == GnbAbilities.EyeGouge);
        Assert.DoesNotContain(ogcd, c => c.Behavior == GnbAbilities.Hypervelocity);
        Assert.DoesNotContain(ogcd, c => c.Behavior == GnbAbilities.FatedBrand);
    }

    // -----------------------------------------------------------------------
    // 5. Cartridge overcap: BurstStrike pushed at priority 6 before SolidBarrel
    //    when cartridges == 2 and comboStep == 2 (aboutToOvercap)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_CartridgeOvercap_BurstStrikePushedBeforeSolidBarrel()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 2, comboStep: 2, level: 80);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();

        // BurstStrike pushed at priority 6 (overcap prevention via TryPushCartridgeSpender)
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.BurstStrike && c.Priority == 6);

        // SolidBarrel pushed at priority 7 (basic combo)
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.SolidBarrel && c.Priority == 7);

        // BurstStrike must be before SolidBarrel in priority (lower int wins)
        var bs = gcd.First(c => c.Behavior == GnbAbilities.BurstStrike);
        var sb = gcd.First(c => c.Behavior == GnbAbilities.SolidBarrel);
        Assert.True(bs.Priority < sb.Priority, "BurstStrike should have higher priority (lower int) than SolidBarrel");
    }

    // -----------------------------------------------------------------------
    // 6. AoE transition: FatedCircle pushed instead of BurstStrike at threshold
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_AoE_FatedCirclePushed_AtEnemyThreshold()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy, enemyCount: 3);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableAoEDamage = true;
        config.Tank.AoEMinTargets = 3;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 3, hasMaxCartridges: true, level: 100, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.FatedCircle && c.Priority == 6);
        Assert.DoesNotContain(gcd, c => c.Behavior == GnbAbilities.BurstStrike && c.Priority == 6);
    }

    [Fact]
    public void CollectCandidates_SingleTarget_BurstStrikePushed_BelowAoEThreshold()
    {
        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy, enemyCount: 1);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableAoEDamage = true;
        config.Tank.AoEMinTargets = 3;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 3, hasMaxCartridges: true, level: 80, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.BurstStrike && c.Priority == 6);
        Assert.DoesNotContain(gcd, c => c.Behavior == GnbAbilities.FatedCircle && c.Priority == 6);
    }

    // -----------------------------------------------------------------------
    // 7. Out-of-melee: Trajectory + LightningShot pushed, full rotation not
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_OutOfMelee_PushesTrajectoryAndLightningShot()
    {
        var enemy = CreateMockEnemy(12345UL);
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

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 0, level: 56); // Lv.56 has both Trajectory (Lv.56) and LightningShot (Lv.15)

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        var gcd = scheduler.InspectGcdQueue();

        Assert.Contains(ogcd, c => c.Behavior == GnbAbilities.Trajectory);
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.LightningShot);

        // Melee rotation abilities must NOT be pushed (we returned early)
        Assert.DoesNotContain(gcd, c => c.Behavior == GnbAbilities.KeenEdge);
        Assert.DoesNotContain(gcd, c => c.Behavior == GnbAbilities.GnashingFang);
    }

    [Fact]
    public void CollectCandidates_OutOfMelee_TrajectoryNotPushed_WhenGapCloseBlocked()
    {
        var enemy = CreateMockEnemy(12345UL);
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
            .Returns(true);
        safetyMock.Setup(x => x.LastBlockReason).Returns("Spread marker");
        targeting.Setup(x => x.GapCloserSafety).Returns(safetyMock.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(targeting: targeting, actionService: actionService,
            cartridges: 0, level: 56);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == GnbAbilities.Trajectory);
        Assert.Equal("Trajectory blocked: Spread marker", context.Debug.DamageState);
    }

    // -----------------------------------------------------------------------
    // Early-exit guards
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_NotInCombat_PushesNothing()
    {
        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(inCombat: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    [Fact]
    public void CollectCandidates_DamageDisabled_PushesNothing()
    {
        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableDamage = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(inCombat: true, config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Disabled", context.Debug.DamageState);
    }

    [Fact]
    public void CollectCandidates_NoTarget_PushesNothing()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);

        var safetyMock = new Mock<IGapCloserSafetyService>();
        targeting.Setup(x => x.GapCloserSafety).Returns(safetyMock.Object);

        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(targeting: targeting);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Equal("No target", context.Debug.DamageState);
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

    private static IHephaestusContext CreateContext(
        bool inCombat = true,
        byte level = 100,
        int cartridges = 0,
        bool hasMaxCartridges = false,
        bool hasNoMercy = false,
        bool isReadyToReign = false,
        int comboStep = 0,
        uint lastComboAction = 0,
        Configuration? config = null,
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
        mock.Setup(x => x.CurrentTarget).Returns((Dalamud.Game.ClientState.Objects.Types.IBattleChara?)null);

        mock.Setup(x => x.Debug).Returns(new HephaestusDebugState());

        return mock.Object;
    }
}
