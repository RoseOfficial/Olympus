using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.IrisCore.Abilities;
using Olympus.Rotation.IrisCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.IrisCore;
using Xunit;

namespace Olympus.Tests.Rotation.IrisCore.Modules;

/// <summary>
/// Tests for DamageModule.CollectCandidates during-downtime behavior:
/// when InCombat and no valid enemy exists, PCT paints missing motifs so three
/// instant Muse GCDs are available immediately on re-engage.
///
/// Trigger: InCombat &amp;&amp; IsDamageTargetingPaused().
/// Reuses TryPushPrepaintMotif (Landscape &gt; Creature &gt; Weapon priority order,
/// per-motif config toggles, existing PrepaintMotifs master toggle).
/// No new config gate: PrepaintMotifs already expresses "auto-paint motifs."
///
/// Holy in White is intentionally NOT pushed during downtime: PCTActions.HolyInWhite
/// requires an enemy target (SingleEnemy action type) which does not exist during
/// downtime. The pre-downtime dump (Phase B3) handles white-paint overflow before
/// the boss jumps.
/// </summary>
public class DamageModuleDowntimeTests
{
    private readonly DamageModule _module = new();

    private static Mock<ITargetingService> CreatePausedTargeting()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(true);
        return targeting;
    }

    private static Mock<ITargetingService> CreateActiveTargeting(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        // IsDamageTargetingPaused defaults to false in MockBuilders.
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        return targeting;
    }

    // -----------------------------------------------------------------------
    // Positive: landscape motif pushed at priority 1 during downtime
    // -----------------------------------------------------------------------

    [Fact]
    public void LandscapeMotif_PushedAtPriority1_WhenInCombatNoEnemy_NeedsLandscape()
    {
        // InCombat=true + IsDamageTargetingPaused=true + NeedsLandscapeMotif=true +
        // PrepaintMotifs=true (default), PrepaintOption=All (default), EnableLandscapeMotif=true (default)
        // → downtime branch calls TryPushPrepaintMotif → StarrySkyMotif at priority 1.
        var targeting = CreatePausedTargeting();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            needsLandscapeMotif: true,
            needsCreatureMotif: false,
            needsWeaponMotif: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.StarrySkyMotif && c.Priority == 1);
    }

    // -----------------------------------------------------------------------
    // Negative 1: Enemy present — normal rotation runs, motif NOT at priority 1
    // Discriminating variable: IsDamageTargetingPaused=false (same NeedsLandscapeMotif)
    // TryPushRepaintMotif still pushes at priority 9 via the in-combat path, not 1.
    // -----------------------------------------------------------------------

    [Fact]
    public void LandscapeMotif_NotAtPriority1_WhenEnemyPresent()
    {
        // Same NeedsLandscapeMotif=true as positive but enemy is present.
        // IsDamageTargetingPaused=false → downtime branch skipped.
        // TryPushRepaintMotif fires at priority 9 instead of prepaint priority 1.
        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(99999UL);
        enemy.Setup(x => x.CurrentHp).Returns(10000u);
        enemy.Setup(x => x.MaxHp).Returns(10000u);
        var targeting = CreateActiveTargeting(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 100,
            inCombat: true,
            needsLandscapeMotif: true,
            needsCreatureMotif: false,
            needsWeaponMotif: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        // Must NOT be at priority 1 (prepaint/downtime slot).
        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == IrisAbilities.StarrySkyMotif && c.Priority == 1);
    }

    // -----------------------------------------------------------------------
    // Negative 2: PrepaintMotifs toggle off — TryPushPrepaintMotif returns early
    // Discriminating variable: PrepaintMotifs=false (same InCombat, same pause, same need)
    // -----------------------------------------------------------------------

    [Fact]
    public void LandscapeMotif_NotPushed_WhenPrepaintMotifsDisabled()
    {
        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.PrepaintMotifs = false;

        var targeting = CreatePausedTargeting();
        var scheduler = SchedulerFactory.CreateForTest(config: config);
        var context = IrisTestContext.Create(
            config: config,
            targetingService: targeting,
            level: 100,
            inCombat: true,
            needsLandscapeMotif: true,
            needsCreatureMotif: false,
            needsWeaponMotif: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == IrisAbilities.StarrySkyMotif);
    }
}
