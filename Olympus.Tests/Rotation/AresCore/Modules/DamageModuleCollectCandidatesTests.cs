using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AresCore.Abilities;
using Olympus.Rotation.AresCore.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AresCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AresCore.Modules;

/// <summary>
/// Scheduler-push tests for <see cref="DamageModule.CollectCandidates"/>.
/// Tests inspect the GCD and oGCD queues rather than calling
/// <see cref="RotationScheduler.DispatchGcd"/>/<see cref="RotationScheduler.DispatchOgcd"/>,
/// so <see cref="IActionService"/> dispatch readiness is irrelevant to these assertions.
/// </summary>
public class DamageModuleCollectCandidatesTests
{
    private readonly DamageModule _module = new();

    // -----------------------------------------------------------------------
    // Early-exit guards
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Disabled_PushesNothing()
    {
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.EnableDamage = false;

        var context = AresTestContext.CreateMock(config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Disabled", context.Debug.DamageState);
    }

    [Fact]
    public void CollectCandidates_NotInCombat_PushesNothing()
    {
        var context = AresTestContext.CreateMock(inCombat: false);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.DamageState);
    }

    [Fact]
    public void CollectCandidates_NoTarget_SetsDebugAndPushesNothing()
    {
        // Both FindEnemyForAction and FindEnemy return null →
        // module writes "No target" and returns without pushing.
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        targeting.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = AresTestContext.CreateMock(targetingService: targeting);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("No target", context.Debug.DamageState);
    }

    // -----------------------------------------------------------------------
    // Inner Release window — Fell Cleave
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_FellCleave_PushedAtPriority4_DuringInnerRelease()
    {
        // HasInnerRelease=true → canSpend=true → FellCleave pushed at priority 4
        var (targeting, _) = BuildTargetingWithEnemy();
        var context = AresTestContext.CreateMock(
            hasInnerRelease: true,
            innerReleaseStacks: 3,
            enemyCount: 1,
            targetingService: targeting);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == AresAbilities.FellCleave && c.Priority == 4);
    }

    [Fact]
    public void CollectCandidates_FellCleave_PushedAtPriority4_WhenGaugeAtCap()
    {
        // BeastGauge >= BeastGaugeCap (90) → atCap=true → canSpend=true → FellCleave at priority 4
        var (targeting, _) = BuildTargetingWithEnemy();
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        // BeastGaugeCap defaults to 90 in TankConfig

        var context = AresTestContext.CreateMock(
            beastGauge: 90,
            hasSurgingTempest: true,
            surgingTempestRemaining: 30f, // healthy — does not trigger StormsEye push
            enemyCount: 1,
            targetingService: targeting,
            config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == AresAbilities.FellCleave && c.Priority == 4);
    }

    // -----------------------------------------------------------------------
    // Primal Rend — AutoPrimalRend toggle
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_PrimalRend_PushedAtPriority2_WhenAutoPrimalRendEnabled()
    {
        // AutoPrimalRend=true (default in CreateDefaultWarriorConfiguration), HasPrimalRendReady=true
        var (targeting, _) = BuildTargetingWithEnemy();
        var context = AresTestContext.CreateMock(
            hasPrimalRendReady: true,
            hasSurgingTempest: true,
            surgingTempestRemaining: 30f,
            enemyCount: 1,
            targetingService: targeting);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == AresAbilities.PrimalRend && c.Priority == 2);
    }

    [Fact]
    public void CollectCandidates_PrimalRend_NotPushed_WhenAutoPrimalRendDisabled()
    {
        // Override AutoPrimalRend to false — toggle is checked before HasPrimalRendReady
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.AutoPrimalRend = false;

        var (targeting, _) = BuildTargetingWithEnemy();
        var context = AresTestContext.CreateMock(
            hasPrimalRendReady: true,
            hasSurgingTempest: true,
            surgingTempestRemaining: 30f,
            enemyCount: 1,
            targetingService: targeting,
            config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == AresAbilities.PrimalRend);
    }

    // -----------------------------------------------------------------------
    // Surging Tempest refresh (StormsEye) at priority 5
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_StormsEye_PushedAtPriority5_WhenSurgingTempestExpiring()
    {
        // SurgingTempestRemaining=5f < 10f → TryPushSurgingTempestRefresh fires.
        // ComboStep=2 + LastComboAction=Maim.ActionId → falls through to ST path → StormsEye at 5.
        var (targeting, _) = BuildTargetingWithEnemy();
        var context = AresTestContext.CreateMock(
            hasSurgingTempest: true,
            surgingTempestRemaining: 5f,
            comboStep: 2,
            lastComboAction: WARActions.Maim.ActionId,
            enemyCount: 1,
            targetingService: targeting);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == AresAbilities.StormsEye && c.Priority == 5);
    }

    [Fact]
    public void CollectCandidates_StormsEye_NotPushedAtRefreshPriority_WhenSurgingTempestHealthy()
    {
        // SurgingTempestRemaining=30f > 10f → TryPushSurgingTempestRefresh returns early (no priority-5 push).
        // TryPushCombo with ComboStep=2 + LastComboAction=Maim: needsSurgingTempest=false (30f >= 10f)
        // → selects StormsPath (not StormsEye) at priority 7. StormsEye does not appear in the queue at all.
        var (targeting, _) = BuildTargetingWithEnemy();
        var context = AresTestContext.CreateMock(
            hasSurgingTempest: true,
            surgingTempestRemaining: 30f,
            comboStep: 2,
            lastComboAction: WARActions.Maim.ActionId,
            enemyCount: 1,
            targetingService: targeting);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == AresAbilities.StormsEye && c.Priority == 5);
    }

    // -----------------------------------------------------------------------
    // Base combo — Heavy Swing at priority 7
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_HeavySwing_PushedAtPriority7_AsBaseComboStarter()
    {
        // No IR, no NascentChaos, gauge=0 (no gauge spend), healthy ST (no refresh).
        // ComboStep=0 → falls through all combo guards → HeavySwing at priority 7.
        var (targeting, _) = BuildTargetingWithEnemy();
        var context = AresTestContext.CreateMock(
            hasSurgingTempest: true,
            surgingTempestRemaining: 30f,
            comboStep: 0,
            lastComboAction: 0,
            enemyCount: 1,
            targetingService: targeting);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == AresAbilities.HeavySwing && c.Priority == 7);
    }

    // -----------------------------------------------------------------------
    // Upheaval vs Orogeny routing
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Upheaval_PushedAtPriority2_InSingleTarget()
    {
        // enemyCount=1 < AoEMinTargets(3) → TryPushUpheaval proceeds past the AoE-prefer check
        var (targeting, _) = BuildTargetingWithEnemy();
        var context = AresTestContext.CreateMock(
            hasSurgingTempest: true,
            surgingTempestRemaining: 30f,
            enemyCount: 1,
            targetingService: targeting);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == AresAbilities.Upheaval && c.Priority == 2);
    }

    [Fact]
    public void CollectCandidates_Orogeny_PushedAtPriority2_InAoE_AndUpheavalSuppressed()
    {
        // enemyCount=3 >= AoEMinTargets(3) → TryPushUpheaval returns early, TryPushOrogeny fires
        var (targeting, _) = BuildTargetingWithEnemy();
        var context = AresTestContext.CreateMock(
            hasSurgingTempest: true,
            surgingTempestRemaining: 30f,
            enemyCount: 3, // AoE path
            targetingService: targeting);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == AresAbilities.Orogeny && c.Priority == 2);
        Assert.DoesNotContain(ogcd, c => c.Behavior == AresAbilities.Upheaval);
    }

    // -----------------------------------------------------------------------
    // Inner Chaos — Nascent Chaos proc
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_InnerChaos_PushedAtPriority3_WhenNascentChaosActive()
    {
        // HasNascentChaos=true, single target (enemyCount=1) → InnerChaos at priority 3
        var (targeting, _) = BuildTargetingWithEnemy();
        var context = AresTestContext.CreateMock(
            hasNascentChaos: true,
            hasSurgingTempest: true,
            surgingTempestRemaining: 30f,
            enemyCount: 1,
            targetingService: targeting);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == AresAbilities.InnerChaos && c.Priority == 3);
    }

    [Fact]
    public void CollectCandidates_InnerChaos_NotPushed_WhenNascentChaosInactive()
    {
        // HasNascentChaos=false → TryPushInnerChaos exits at first gate
        var (targeting, _) = BuildTargetingWithEnemy();
        var context = AresTestContext.CreateMock(
            hasNascentChaos: false,
            hasSurgingTempest: true,
            surgingTempestRemaining: 30f,
            enemyCount: 1,
            targetingService: targeting);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == AresAbilities.InnerChaos);
    }

    // -----------------------------------------------------------------------
    // Helper
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns a targeting service mock with a single in-melee-range enemy.
    /// <see cref="ITargetingService.FindEnemyForAction"/> returns the enemy, routing the module
    /// down the in-melee path and skipping the out-of-range Onslaught/Tomahawk branch.
    /// <para>
    /// Do NOT set up <c>CountEnemiesInRange</c> here: <see cref="AresTestContext.CreateMock"/>
    /// overwrites it via its own <c>enemyCount</c> parameter. Pass <c>enemyCount</c> to
    /// <see cref="AresTestContext.CreateMock"/> instead.
    /// </para>
    /// </summary>
    private static (Mock<ITargetingService> targeting, Mock<IBattleNpc> enemy) BuildTargetingWithEnemy(
        ulong enemyId = 99999UL)
    {
        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(enemyId);
        enemy.Setup(x => x.EntityId).Returns((uint)enemyId);
        enemy.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        // CountEnemiesInRange is intentionally NOT set up here:
        // AresTestContext.CreateMock always sets it via the enemyCount parameter,
        // so any setup here would be silently overwritten. Pass enemyCount to CreateMock instead.

        return (targeting, enemy);
    }
}
