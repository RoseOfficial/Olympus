using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.ThemisCore.Abilities;
using Olympus.Rotation.ThemisCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.ThemisCore;
using Xunit;

namespace Olympus.Tests.Rotation.ThemisCore.Modules;

/// <summary>
/// Scheduler-push tests for Paladin DamageModule.
/// Verifies that candidates are pushed to the scheduler queue with correct
/// behaviors and priorities, given specific combo step, level, and config state.
/// All tests call CollectCandidates and inspect queues without dispatching.
/// </summary>
public class DamageModuleSchedulerTests
{
    private readonly DamageModule _module = new();

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<IBattleNpc> CreateMockEnemy(ulong gameObjectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(gameObjectId);
        mock.Setup(x => x.EntityId).Returns((uint)gameObjectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return mock;
    }

    /// <summary>
    /// Sets up the targeting service to return a melee-range enemy from FindEnemyForAction.
    /// This satisfies both the ThemisContext constructor (CurrentTarget) and DamageModule
    /// (its own FindEnemyForAction call), since both use the same mock.
    /// </summary>
    private static Mock<ITargetingService> BuildTargetingWithMeleeEnemy(
        Mock<IBattleNpc> enemy,
        int enemyCount = 1)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.CountEnemiesInRange(
                It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemyCount);
        return targeting;
    }

    // -----------------------------------------------------------------------
    // Early-exit guards
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_DamageDisabled_PushesNothing()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableDamage = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(config: config, inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Disabled", context.Debug.DamageState);
    }

    [Fact]
    public void CollectCandidates_NotInCombat_PushesNothing()
    {
        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(inCombat: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    [Fact]
    public void CollectCandidates_TargetingPaused_PushesNothing()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(targetingService: targeting, inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Paused (no target)", context.Debug.DamageState);
    }

    [Fact]
    public void CollectCandidates_NoTarget_PushesNothing()
    {
        // Default targeting service leaves FindEnemyForAction and FindEnemy both returning null.
        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Equal("No target", context.Debug.DamageState);
    }

    // -----------------------------------------------------------------------
    // Basic single-target combo
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_BasicCombo_Step0_PushesFastBladeAtPriority7()
    {
        var enemy = CreateMockEnemy();
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        // Suppress BladeOfHonor so it does not shadow the combo test — HasBladeOfHonor
        // = (level >= 100 && IsActionReady(BladeOfHonor)), and default level is 100.
        actionService.Setup(x => x.IsActionReady(PLDActions.BladeOfHonor.ActionId)).Returns(false);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            targetingService: targeting,
            actionService: actionService,
            level: 100,
            comboStep: 0,
            lastComboAction: 0,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == ThemisAbilities.FastBlade && c.Priority == 7);
    }

    [Fact]
    public void CollectCandidates_BasicCombo_Step2_PushesRiotBladeAtPriority7()
    {
        var enemy = CreateMockEnemy();
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(PLDActions.BladeOfHonor.ActionId)).Returns(false);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            targetingService: targeting,
            actionService: actionService,
            level: 60,
            comboStep: 2,
            lastComboAction: PLDActions.FastBlade.ActionId,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == ThemisAbilities.RiotBlade && c.Priority == 7);
    }

    [Fact]
    public void CollectCandidates_BasicCombo_Step3_PushesRoyalAuthorityAtPriority6()
    {
        var enemy = CreateMockEnemy();
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        // BladeOfHonor not available at level 60
        // (no suppression needed since 60 < 100, HasBladeOfHonor = false at level 60)

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            targetingService: targeting,
            actionService: actionService,
            level: 60,
            comboStep: 3,
            lastComboAction: PLDActions.RiotBlade.ActionId,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == ThemisAbilities.RoyalAuthority && c.Priority == 6);
    }

    // -----------------------------------------------------------------------
    // DoT / Goring Blade chain
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_GoringBlade_PushedAtPriority3_WhenDotExpiring()
    {
        // Level 70: GoringBlade (Lv.54) available, BladeOfHonor (Lv.100) not yet.
        // GoringBladeRemaining = 0 (no status on null-StatusList enemy) → shouldRefresh = true.
        var enemy = CreateMockEnemy();
        var targeting = BuildTargetingWithMeleeEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService();

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            targetingService: targeting,
            actionService: actionService,
            level: 70,
            comboStep: 0,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == ThemisAbilities.GoringBlade && c.Priority == 3);
        Assert.DoesNotContain(gcd, c => c.Behavior == ThemisAbilities.BladeOfHonor);
    }

    [Fact]
    public void CollectCandidates_BladeOfHonor_PushedAtPriority2_WhenAvailableAtLevel100()
    {
        // At level 100 with all actions ready, HasBladeOfHonor = true.
        // BladeOfHonor takes priority 2 — higher than GoringBlade (priority 3).
        var enemy = CreateMockEnemy();
        var targeting = BuildTargetingWithMeleeEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService();
        // Default: IsActionReady returns true for everything, including BladeOfHonor.

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            targetingService: targeting,
            actionService: actionService,
            level: 100,
            comboStep: 0,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == ThemisAbilities.BladeOfHonor && c.Priority == 2);
    }

    // -----------------------------------------------------------------------
    // oGCD damage
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_CircleOfScorn_PushedAtPriority3_WhenEnabled()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableCircleOfScorn = true;

        var enemy = CreateMockEnemy();
        var targeting = BuildTargetingWithMeleeEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService();

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            config: config,
            targetingService: targeting,
            actionService: actionService,
            level: 50, // Lv.50 = CircleOfScorn MinLevel
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == ThemisAbilities.CircleOfScorn && c.Priority == 3);
    }

    [Fact]
    public void CollectCandidates_CircleOfScorn_NotPushed_WhenDisabled()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableCircleOfScorn = false;

        var enemy = CreateMockEnemy();
        var targeting = BuildTargetingWithMeleeEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService();

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            config: config,
            targetingService: targeting,
            actionService: actionService,
            level: 50,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == ThemisAbilities.CircleOfScorn);
    }

    [Fact]
    public void CollectCandidates_Expiacion_PushedAtPriority3_AtHighLevel()
    {
        // Lv.86+: Expiacion replaces Spirits Within.
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableSpiritsWithin = true;

        var enemy = CreateMockEnemy();
        var targeting = BuildTargetingWithMeleeEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService();

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            config: config,
            targetingService: targeting,
            actionService: actionService,
            level: 86,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == ThemisAbilities.Expiacion && c.Priority == 3);
        Assert.DoesNotContain(ogcd, c => c.Behavior == ThemisAbilities.SpiritsWithin);
    }

    [Fact]
    public void CollectCandidates_SpiritsWithin_PushedAtPriority3_BelowExpiacionLevel()
    {
        // Lv.30–85: Spirits Within is the oGCD single-target (Expiacion not yet available).
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableSpiritsWithin = true;

        var enemy = CreateMockEnemy();
        var targeting = BuildTargetingWithMeleeEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService();

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            config: config,
            targetingService: targeting,
            actionService: actionService,
            level: 30,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == ThemisAbilities.SpiritsWithin && c.Priority == 3);
        Assert.DoesNotContain(ogcd, c => c.Behavior == ThemisAbilities.Expiacion);
    }
}
