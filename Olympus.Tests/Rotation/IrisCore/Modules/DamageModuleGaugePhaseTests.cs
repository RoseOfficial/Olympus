using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.IrisCore.Abilities;
using Olympus.Rotation.IrisCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.IrisCore.Modules;

/// <summary>
/// Scheduler-candidate tests for Iris (PCT) DamageModule covering gauge-phase
/// and palette-state decisions: Subtractive Palette entry, Star Prism / Starstruck
/// finisher, Comet in Black, Holy in White thresholds, Hammer combo step selection,
/// and in-combat / Inspiration motif repainting.
/// </summary>
public class DamageModuleGaugePhaseTests
{
    private readonly DamageModule _module = new();

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

    private static Mock<ITargetingService> CreateTargetingWithEnemy(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        return targeting;
    }

    // -----------------------------------------------------------------------
    // 1-5: Subtractive Palette / subtractive combo entry gates
    // -----------------------------------------------------------------------

    [Fact]
    public void SubtractiveCombo_PushedAtPriority7_WhenHasSubtractivePaletteAndLevel60()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 60,
            hasSubtractivePalette: true,
            baseComboStep: 0);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.BlizzardInCyan && c.Priority == 7);
    }

    [Fact]
    public void SubtractiveCombo_PushedAtPriority7_WhenHasSubtractiveSpectrumAndLevel60()
    {
        // SubtractiveSpectrum is a separate buff that also unlocks the subtractive combo
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 60,
            hasSubtractivePalette: false,
            hasSubtractiveSpectrum: true,
            baseComboStep: 0);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.BlizzardInCyan && c.Priority == 7);
    }

    [Fact]
    public void SubtractiveCombo_NotPushed_WhenNeitherSubtractivePaletteNorSpectrum()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 100,
            hasSubtractivePalette: false,
            hasSubtractiveSpectrum: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c =>
            c.Behavior == IrisAbilities.BlizzardInCyan ||
            c.Behavior == IrisAbilities.StoneInYellow ||
            c.Behavior == IrisAbilities.ThunderInMagenta);
    }

    [Fact]
    public void SubtractiveCombo_NotPushed_WhenLevelBelow60()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 59,
            hasSubtractivePalette: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == IrisAbilities.BlizzardInCyan);
    }

    [Fact]
    public void SubtractiveCombo_NotPushed_WhenToggleDisabled()
    {
        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.EnableSubtractiveCombo = false;

        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            config: config,
            targetingService: targeting,
            level: 100,
            hasSubtractivePalette: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == IrisAbilities.BlizzardInCyan);
    }

    // -----------------------------------------------------------------------
    // 6-8: Star Prism / Starstruck burst finisher
    // -----------------------------------------------------------------------

    [Fact]
    public void StarPrism_PushedAtPriority2_WhenStarstruckActiveAndLevel100()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 100,
            hasStarstruck: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.StarPrism && c.Priority == 2);
    }

    [Fact]
    public void StarPrism_NotPushed_WhenNotStarstruck()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 100,
            hasStarstruck: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == IrisAbilities.StarPrism);
    }

    [Fact]
    public void StarPrism_NotPushed_WhenLevelBelow100()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 99,
            hasStarstruck: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == IrisAbilities.StarPrism);
    }

    // -----------------------------------------------------------------------
    // 9-11: Comet in Black (black paint charges)
    // -----------------------------------------------------------------------

    [Fact]
    public void CometInBlack_PushedAtPriority5_WhenHasBlackPaintAndLevel90()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 90,
            hasBlackPaint: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.CometInBlack && c.Priority == 5);
    }

    [Fact]
    public void CometInBlack_NotPushed_WhenNoBlackPaint()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 100,
            hasBlackPaint: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == IrisAbilities.CometInBlack);
    }

    [Fact]
    public void CometInBlack_NotPushed_WhenLevelBelow90()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 89,
            hasBlackPaint: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == IrisAbilities.CometInBlack);
    }

    // -----------------------------------------------------------------------
    // 12-15: Holy in White (palette gauge and paint-count thresholds)
    // -----------------------------------------------------------------------

    [Fact]
    public void HolyInWhite_PushedAtPriority6_WhenMovingWithWhitePaint()
    {
        // Movement path: both the paint-count and palette-gauge guards are skipped
        // when isMoving is true, so any positive white-paint count triggers Holy.
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 80,
            hasWhitePaint: true,
            whitePaint: 1,
            paletteGauge: 0,          // below HolyMinPalette (50) — bypassed while moving
            isInBurstWindow: false);

        _module.CollectCandidates(context, scheduler, isMoving: true);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.HolyInWhite && c.Priority == 6);
    }

    [Fact]
    public void HolyInWhite_PushedAtPriority6_WhenInBurstWindowWithWhitePaint()
    {
        // Burst-window path: both guards check !IsInBurstWindow, so an active burst
        // window bypasses the paint-count and palette-gauge minimums.
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 80,
            hasWhitePaint: true,
            whitePaint: 1,            // below 4 — bypassed by burst window
            paletteGauge: 0,          // below 50 — bypassed by burst window
            isInBurstWindow: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.HolyInWhite && c.Priority == 6);
    }

    [Fact]
    public void HolyInWhite_NotPushed_WhenNoWhitePaint()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 100,
            hasWhitePaint: false,
            whitePaint: 0);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == IrisAbilities.HolyInWhite);
    }

    [Fact]
    public void HolyInWhite_NotPushed_WhenLevelBelow80()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 79,
            hasWhitePaint: true,
            whitePaint: 5);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == IrisAbilities.HolyInWhite);
    }

    // -----------------------------------------------------------------------
    // 16-19: Hammer combo step routing
    // -----------------------------------------------------------------------

    [Fact]
    public void HammerCombo_Step0_PushesHammerStamp_AtPriority4()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 50,
            hasHammerTime: true,
            hammerComboStep: 0);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.HammerStamp && c.Priority == 4);
    }

    [Fact]
    public void HammerCombo_Step1_PushesHammerBrush_AtPriority4_AtLevel86()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 86,
            hasHammerTime: false,
            isInHammerCombo: true,
            hammerComboStep: 1);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.HammerBrush && c.Priority == 4);
    }

    [Fact]
    public void HammerCombo_Step2_PushesPolishingHammer_AtPriority4_AtLevel86()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 86,
            hasHammerTime: false,
            isInHammerCombo: true,
            hammerComboStep: 2);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.PolishingHammer && c.Priority == 4);
    }

    [Fact]
    public void HammerCombo_NotPushed_WhenNoHammerTimeAndNotInCombo()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 100,
            hasHammerTime: false,
            isInHammerCombo: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c =>
            c.Behavior == IrisAbilities.HammerStamp ||
            c.Behavior == IrisAbilities.HammerBrush ||
            c.Behavior == IrisAbilities.PolishingHammer);
    }

    // -----------------------------------------------------------------------
    // 20-21: In-combat motif repainting and Inspiration-accelerated painting
    // -----------------------------------------------------------------------

    [Fact]
    public void RepaintLandscapeMotif_PushedAtPriority9_WhenNeedsLandscapeAndNotMoving()
    {
        // Without Inspiration, a missing landscape motif is repainted at priority 9
        // (lower priority than burst finishers, so it yields to active burst actions).
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 70,
            hasInspiration: false,
            needsLandscapeMotif: true,
            needsCreatureMotif: false,
            needsWeaponMotif: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.StarrySkyMotif && c.Priority == 9);
    }

    [Fact]
    public void InspirationLandscapeMotif_PushedAtPriority0_DuringInspiration()
    {
        // During Inspiration the cast-time reduction makes motif painting near-free.
        // The module promotes the candidate to priority 0 (highest in the GCD queue)
        // so it fires before burst finishers at priorities 2-8.
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);
        var scheduler = SchedulerFactory.CreateForTest();
        var context = IrisTestContext.Create(
            targetingService: targeting,
            level: 70,
            hasInspiration: true,
            needsLandscapeMotif: true,
            needsCreatureMotif: false,
            needsWeaponMotif: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == IrisAbilities.StarrySkyMotif && c.Priority == 0);
    }
}
