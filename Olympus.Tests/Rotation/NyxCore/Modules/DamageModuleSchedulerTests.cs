using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.NyxCore.Abilities;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Helpers;
using Olympus.Rotation.NyxCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.NyxCore.Modules;

/// <summary>
/// Scheduler-push tests for Nyx DamageModule (DRK).
/// Verifies that abilities are pushed at the correct priority when their
/// conditions are met, and are absent when config toggles, gauge state,
/// or level gates should suppress them.
/// </summary>
public class DamageModuleSchedulerTests
{
    private readonly DamageModule _module = new();

    // -----------------------------------------------------------------------
    // Early-exit guards
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_DamageDisabled_PushesNothing()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableDamage = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(config: config, inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Disabled", context.Debug.DamageState);
    }

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

    // -----------------------------------------------------------------------
    // Delirium combo (Lv.96+): ScarletDelirium
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_ScarletDelirium_PushedAtPriority2_WhenHasDeliriumAtLevel96()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableDamage = true;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.ScarletDelirium.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            hasDelirium: true,
            inCombat: true,
            level: 96);   // ScarletDelirium.MinLevel == 96

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectGcdQueue(),
            c => c.Behavior == NyxAbilities.ScarletDelirium && c.Priority == 2);
    }

    [Fact]
    public void CollectCandidates_ScarletDelirium_NotPushed_WhenLevelBelowMinLevel()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableDamage = true;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // At level 95 Delirium still grants free Bloodspillers but NOT ScarletDelirium
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            hasDelirium: true,
            bloodGauge: 50,
            inCombat: true,
            level: 95);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == NyxAbilities.ScarletDelirium);
    }

    // -----------------------------------------------------------------------
    // Bloodspiller gauge gating
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Bloodspiller_PushedAtPriority3_WhenGaugeAtFifty()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableDamage = true;
        config.Tank.BloodGaugeCap = 90;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Bloodspiller.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            hasDelirium: false,   // no free Bloodspiller
            bloodGauge: 50,       // exactly 50 — meets the >= 50 threshold
            inCombat: true,
            level: 70);           // below Lv.96 so ScarletDelirium path is inactive

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectGcdQueue(),
            c => c.Behavior == NyxAbilities.Bloodspiller && c.Priority == 3);
    }

    [Fact]
    public void CollectCandidates_Bloodspiller_NotPushed_WhenGaugeLowAndNoFreeDelirium()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableDamage = true;
        config.Tank.BloodGaugeCap = 90;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            hasDelirium: false,
            bloodGauge: 30,   // below 50 and no free Delirium
            inCombat: true,
            level: 70);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == NyxAbilities.Bloodspiller);
    }

    [Fact]
    public void CollectCandidates_Bloodspiller_PushedFree_WhenPreLevel96DeliriumActive()
    {
        // Pre-Lv.96 Delirium grants free Bloodspillers even with low gauge
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableDamage = true;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Bloodspiller.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            hasDelirium: true,
            bloodGauge: 10,   // below 50, but Delirium makes it free at pre-96
            inCombat: true,
            level: 68);       // Delirium.MinLevel == 68; below ScarletDelirium.MinLevel (96)

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectGcdQueue(),
            c => c.Behavior == NyxAbilities.Bloodspiller && c.Priority == 3);
    }

    // -----------------------------------------------------------------------
    // Salted Earth
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_SaltedEarth_PushedAtPriority3_WhenNotActive()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableSaltedEarth = true;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.SaltedEarth.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            hasSaltedEarth: false,  // not active — should push
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.SaltedEarth && c.Priority == 3);
    }

    [Fact]
    public void CollectCandidates_SaltedEarth_NotPushed_WhenAlreadyActive()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableSaltedEarth = true;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.SaltedEarth.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            hasSaltedEarth: true,   // already placed
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.SaltedEarth);
    }

    [Fact]
    public void CollectCandidates_SaltedEarth_NotPushed_WhenDisabled()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableSaltedEarth = false;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.SaltedEarth.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            hasSaltedEarth: false,
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.SaltedEarth);
    }

    // -----------------------------------------------------------------------
    // Dark Arts proc: EdgeOfShadow at priority 1
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_DarkArts_EdgeOfShadow_PushedAtPriority1()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableDamage = true;
        config.Tank.EnableAoEDamage = false; // single-target → EdgeOfShadow

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy, enemyCount: 1);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.EdgeOfShadow.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            hasDarkArts: true,  // TBN shield broke — free Edge of Shadow
            currentMp: 9000,    // enough MP
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.EdgeOfShadow && c.Priority == 1);
    }

    // -----------------------------------------------------------------------
    // HardSlash: default combo starter at priority 7
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_HardSlash_PushedAtPriority7_WhenNoActiveCombo()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableDamage = true;
        config.Tank.EnableAoEDamage = false;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy, enemyCount: 1);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            hasDelirium: false,
            hasDarkArts: false,
            hasSaltedEarth: true,   // already active — so SaltedEarth won't compete
            bloodGauge: 0,
            currentMp: 9000,        // HasEnoughMpForEdge=true but no Darkside → DarksideMaintenance skips
            hasDarkside: false,     // no Darkside → DarksideMaintenance activates (noDarkside=true)
            comboStep: 0,
            inCombat: true,
            level: 30);             // Lv.30: HardSlash available, Souleater available, Bloodspiller not yet

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectGcdQueue(),
            c => c.Behavior == NyxAbilities.HardSlash && c.Priority == 7);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 100ul)
    {
        var m = new Mock<IBattleNpc>();
        m.Setup(x => x.GameObjectId).Returns(objectId);
        m.Setup(x => x.EntityId).Returns((uint)objectId);
        m.Setup(x => x.CurrentHp).Returns(10000u);
        m.Setup(x => x.MaxHp).Returns(10000u);
        return m;
    }

    private static Mock<ITargetingService> BuildTargetingWithMeleeEnemy(
        Mock<IBattleNpc> enemy, int enemyCount = 1)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemyCount);

        var safety = new Mock<IGapCloserSafetyService>();
        safety.Setup(x => x.ShouldBlockGapCloser(
            It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>())).Returns(false);
        safety.Setup(x => x.LastBlockReason).Returns((string?)null);
        targeting.Setup(x => x.GapCloserSafety).Returns(safety.Object);

        return targeting;
    }

    /// <summary>
    /// Builds a Mock&lt;INyxContext&gt; with safe defaults for all properties
    /// read by DamageModule. Individual tests override as needed.
    /// </summary>
    private static INyxContext CreateContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null,
        bool inCombat = true,
        byte level = 100,
        uint currentMp = 10000,
        int bloodGauge = 0,
        int comboStep = 0,
        uint lastComboAction = 0,
        bool hasDelirium = false,
        int deliriumStacks = 0,
        bool hasDarkArts = false,
        bool hasScornfulEdge = false,
        bool hasDarkside = true,
        float darksideRemaining = 20f,
        bool hasSaltedEarth = false,
        bool hasEnoughMpForEdge = true)
    {
        config ??= NyxTestContext.CreateDefaultDarkKnightConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();

        if (targeting == null)
        {
            targeting = MockBuilders.CreateMockTargetingService();
            targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
            var safety = new Mock<IGapCloserSafetyService>();
            safety.Setup(x => x.ShouldBlockGapCloser(
                It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>())).Returns(false);
            safety.Setup(x => x.LastBlockReason).Returns((string?)null);
            targeting.Setup(x => x.GapCloserSafety).Returns(safety.Object);
        }

        var player = MockBuilders.CreateMockPlayerCharacter(
            level: level, currentMp: currentMp);

        var debugState = new NyxDebugState();
        var mock = new Mock<INyxContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((Olympus.Services.Training.ITrainingService?)null);
        mock.Setup(x => x.Debug).Returns(debugState);

        // Gauge and resources
        mock.Setup(x => x.BloodGauge).Returns(bloodGauge);
        mock.Setup(x => x.CurrentMp).Returns((int)currentMp);
        mock.Setup(x => x.HasEnoughMpForEdge).Returns(hasEnoughMpForEdge && currentMp >= DRKActions.EdgeFloodMpCost);
        mock.Setup(x => x.HasEnoughMpForTbn).Returns(currentMp >= DRKActions.TbnMpCost);

        // Buff state
        mock.Setup(x => x.HasDelirium).Returns(hasDelirium);
        mock.Setup(x => x.DeliriumStacks).Returns(deliriumStacks);
        mock.Setup(x => x.HasDarkArts).Returns(hasDarkArts);
        mock.Setup(x => x.HasScornfulEdge).Returns(hasScornfulEdge);
        mock.Setup(x => x.HasDarkside).Returns(hasDarkside);
        mock.Setup(x => x.DarksideRemaining).Returns(darksideRemaining);
        mock.Setup(x => x.HasSaltedEarth).Returns(hasSaltedEarth);

        // Combo tracking
        mock.Setup(x => x.ComboStep).Returns(comboStep);
        mock.Setup(x => x.LastComboAction).Returns(lastComboAction);
        mock.Setup(x => x.ComboTimeRemaining).Returns(30f);

        // Defensives (not tested in DamageModule — safe defaults)
        mock.Setup(x => x.HasLivingDead).Returns(false);
        mock.Setup(x => x.HasWalkingDead).Returns(false);
        mock.Setup(x => x.HasTheBlackestNight).Returns(false);
        mock.Setup(x => x.HasLivingShadow).Returns(false);

        return mock.Object;
    }
}
