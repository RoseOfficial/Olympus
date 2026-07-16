using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.NikeCore.Abilities;
using Olympus.Rotation.NikeCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.NikeCore.Modules;

/// <summary>
/// Tests for burst-pooling hold gates in BuffModule.
///
/// NEW gates (fail before implementation, pass after):
///   (a) TryPushBurstKenki: Senei/Guren held when burst imminent and Kenki below 90;
///       escape at Kenki >= 90 prevents overcap.
///   (b) TryPushMeikyoShisui: pull-forward hold delays activation until burst arrives when
///       UseMeikyoInBurst is on; escape when both charges are capped (2).
///
/// DOCUMENTATION tests (pass before AND after -- verify intentional design):
///   (c) TryPushShoha: no hold gate is added. Shoha requires exactly 3 Meditation stacks
///       (the cap), so any escape condition based on "at max stacks" is always true, making
///       a hold block a permanent no-op. These tests document that Shoha fires normally even
///       with burst imminent.
/// </summary>
public class BuffModuleBurstPoolingTests
{
    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<IBurstWindowService> MakeBurstService(bool inBurst, bool imminent)
    {
        var m = new Mock<IBurstWindowService>();
        m.Setup(x => x.IsInBurstWindow).Returns(inBurst);
        m.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(imminent);
        return m;
    }

    private static Mock<IBattleNpc> CreateEnemy(ulong id = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(id);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static Mock<ITargetingService> CreateTargetingWithEnemy(int enemyCount = 1)
    {
        var enemy = CreateEnemy();
        var t = MockBuilders.CreateMockTargetingService(countEnemiesInRange: enemyCount);
        t.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        return t;
    }

    /// <summary>
    /// Config with all competing abilities disabled so queue inspection is unambiguous.
    /// Re-enable only the ability under test per case.
    /// </summary>
    private static Configuration BuildConfig(bool enableBurstPooling = true)
    {
        var cfg = NikeTestContext.CreateDefaultSamuraiConfiguration();
        cfg.Samurai.EnableBurstPooling = enableBurstPooling;
        cfg.Samurai.EnableShoha = false;
        cfg.Samurai.EnableZanshin = false;
        cfg.Samurai.EnableIkishoten = false;
        cfg.Samurai.EnableMeikyoShisui = false;
        cfg.Samurai.EnableSenei = false;
        cfg.Samurai.EnableGuren = false;
        cfg.MeleeShared.EnableTrueNorth = false;
        return cfg;
    }

    // -----------------------------------------------------------------------
    // (a) Senei/Guren burst-hold gate -- NEW behavior
    //
    // Senei_NotPushed_WhenBurstImminentAndKenkiBelow90 FAILS before implementation
    // (no hold gate exists so Senei is pushed regardless).
    // The other two Senei tests pass before and after; they pin the baseline and
    // the escape hatch.
    // -----------------------------------------------------------------------

    [Fact]
    public void Senei_PushedAtPriority4_WhenBurstNotImminent()
    {
        // Baseline: burst not imminent, hold gate does not fire, Senei queued normally.
        var burstService = MakeBurstService(inBurst: false, imminent: false);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var cfg = BuildConfig();
        cfg.Samurai.EnableSenei = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: cfg);
        var context = NikeTestContext.Create(
            config: cfg,
            actionService: actionService,
            targetingService: CreateTargetingWithEnemy(enemyCount: 1),
            kenki: 50,
            level: 100);

        new BuffModule(burstService.Object).CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NikeAbilities.Senei && c.Priority == 4);
    }

    [Fact]
    public void Senei_NotPushed_WhenBurstImminentAndKenkiBelow90()
    {
        // NEW gate: burst imminent, Kenki=50 (< 90 escape threshold), hold fires.
        // This test FAILS before implementation (Senei is pushed without the gate).
        var burstService = MakeBurstService(inBurst: false, imminent: true);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var cfg = BuildConfig();
        cfg.Samurai.EnableSenei = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: cfg);
        var context = NikeTestContext.Create(
            config: cfg,
            actionService: actionService,
            targetingService: CreateTargetingWithEnemy(enemyCount: 1),
            kenki: 50,
            level: 100);

        new BuffModule(burstService.Object).CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NikeAbilities.Senei);
    }

    [Fact]
    public void Senei_PushedDespiteBurstImminent_WhenKenkiAt90()
    {
        // Escape hatch: Kenki=90 bypasses hold to prevent overcap.
        var burstService = MakeBurstService(inBurst: false, imminent: true);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var cfg = BuildConfig();
        cfg.Samurai.EnableSenei = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: cfg);
        var context = NikeTestContext.Create(
            config: cfg,
            actionService: actionService,
            targetingService: CreateTargetingWithEnemy(enemyCount: 1),
            kenki: 90,
            level: 100);

        new BuffModule(burstService.Object).CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NikeAbilities.Senei && c.Priority == 4);
    }

    // -----------------------------------------------------------------------
    // (b) Shoha -- DOCUMENTATION tests (pass before and after; no new code added)
    //
    // Shoha requires exactly 3 Meditation stacks (MeditationMaxStacks). The guard
    // `if (context.Meditation < MeditationMaxStacks) return;` ensures Meditation == 3
    // at any point a hold block could execute. A hold condition gating on
    // `Meditation < MeditationMaxStacks` is always false (dead code). A hold block
    // gating on `Meditation >= MeditationMaxStacks` would always fire, permanently
    // suppressing Shoha. Neither is correct. The implementation adds only a comment.
    // These three tests document the resulting intentional behavior.
    // -----------------------------------------------------------------------

    [Fact]
    public void Shoha_PushedAtPriority1_WhenAt3Stacks()
    {
        // Shoha fires at 3 stacks regardless of burst state.
        var burstService = MakeBurstService(inBurst: false, imminent: false);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var cfg = BuildConfig();
        cfg.Samurai.EnableShoha = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: cfg);
        var context = NikeTestContext.Create(
            config: cfg,
            actionService: actionService,
            targetingService: CreateTargetingWithEnemy(),
            meditation: 3,
            level: 100);

        new BuffModule(burstService.Object).CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NikeAbilities.Shoha && c.Priority == 1);
    }

    [Fact]
    public void Shoha_StillPushed_WhenBurstImminentAndAt3Stacks()
    {
        // Documents the intentional no-hold design: even with burst imminent, Shoha fires
        // at 3 stacks. Holding would waste future meditation generation -- meditation cannot
        // exceed 3, so a delayed Shoha cast has nowhere for incoming stacks to go.
        var burstService = MakeBurstService(inBurst: false, imminent: true);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var cfg = BuildConfig();
        cfg.Samurai.EnableShoha = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: cfg);
        var context = NikeTestContext.Create(
            config: cfg,
            actionService: actionService,
            targetingService: CreateTargetingWithEnemy(),
            meditation: 3,
            level: 100);

        new BuffModule(burstService.Object).CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NikeAbilities.Shoha && c.Priority == 1);
    }

    [Fact]
    public void Shoha_NotPushed_WhenDisabled()
    {
        var burstService = MakeBurstService(inBurst: false, imminent: false);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var cfg = BuildConfig();
        cfg.Samurai.EnableShoha = false; // explicitly off (also off via BuildConfig default)

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: cfg);
        var context = NikeTestContext.Create(
            config: cfg,
            actionService: actionService,
            targetingService: CreateTargetingWithEnemy(),
            meditation: 3,
            level: 100);

        new BuffModule(burstService.Object).CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NikeAbilities.Shoha);
    }

    // -----------------------------------------------------------------------
    // (c) MeikyoShisui pull-forward hold gate -- NEW behavior
    //
    // MeikyoShisui_NotPushed_WhenBurstImminentAndOnlyOneCharge FAILS before
    // implementation (no hold gate exists so Meikyo is pushed regardless).
    // The other two Meikyo tests pass before and after; they pin the baseline
    // and the escape hatch.
    // -----------------------------------------------------------------------

    [Fact]
    public void MeikyoShisui_PushedAtPriority3_WhenBurstNotImminent()
    {
        // Baseline: burst not imminent, no hold, Meikyo queued.
        var burstService = MakeBurstService(inBurst: false, imminent: false);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(SAMActions.MeikyoShisui.ActionId)).Returns(1u);

        var cfg = BuildConfig();
        cfg.Samurai.EnableMeikyoShisui = true;
        cfg.Samurai.UseMeikyoInBurst = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: cfg);
        var context = NikeTestContext.Create(
            config: cfg,
            actionService: actionService,
            targetingService: CreateTargetingWithEnemy(),
            hasMeikyoShisui: false,
            hasFugetsu: false, // triggers shouldUseMeikyo = true
            level: 100);

        new BuffModule(burstService.Object).CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NikeAbilities.MeikyoShisui && c.Priority == 3);
    }

    [Fact]
    public void MeikyoShisui_NotPushed_WhenBurstImminentAndOnlyOneCharge()
    {
        // NEW gate: burst imminent, 1 charge (< 2 escape threshold), pull-forward hold fires.
        // This test FAILS before implementation (Meikyo is pushed without the gate).
        var burstService = MakeBurstService(inBurst: false, imminent: true);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(SAMActions.MeikyoShisui.ActionId)).Returns(1u);

        var cfg = BuildConfig();
        cfg.Samurai.EnableMeikyoShisui = true;
        cfg.Samurai.UseMeikyoInBurst = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: cfg);
        var context = NikeTestContext.Create(
            config: cfg,
            actionService: actionService,
            targetingService: CreateTargetingWithEnemy(),
            hasMeikyoShisui: false,
            hasFugetsu: false,
            level: 100);

        new BuffModule(burstService.Object).CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NikeAbilities.MeikyoShisui);
    }

    [Fact]
    public void MeikyoShisui_PushedDespiteBurstImminent_WhenBothChargesCapped()
    {
        // Escape hatch: 2 charges (both capped) bypass hold to prevent losing a charge restock.
        var burstService = MakeBurstService(inBurst: false, imminent: true);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(SAMActions.MeikyoShisui.ActionId)).Returns(2u);

        var cfg = BuildConfig();
        cfg.Samurai.EnableMeikyoShisui = true;
        cfg.Samurai.UseMeikyoInBurst = true;

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: cfg);
        var context = NikeTestContext.Create(
            config: cfg,
            actionService: actionService,
            targetingService: CreateTargetingWithEnemy(),
            hasMeikyoShisui: false,
            hasFugetsu: false,
            level: 100);

        new BuffModule(burstService.Object).CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NikeAbilities.MeikyoShisui && c.Priority == 3);
    }
}
