using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.PersephoneCore.Abilities;
using Olympus.Rotation.PersephoneCore.Context;
using Olympus.Rotation.PersephoneCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.PersephoneCore.Modules;

/// <summary>
/// Tests for BuffModule candidate collection covering:
///   - Energy Drain gating: EnableEnergyDrain config, EnergyDrainReady context flag,
///     HasAetherflow guard (drain is skipped when stacks are already held).
///   - Aetherflow spender (Fester / Necrotize) gating: EnableFester config,
///     AetherflowReserve config field (stacks <= reserve → skip),
///     burst-window gate (only fires during demi-summon or Searing Light window, or
///     when Energy Drain is imminently coming off cooldown).
/// Energy Drain and the spender both live in BuffModule, not DamageModule.
/// </summary>
public class BuffModuleGaugeTests
{
    private readonly BuffModule _module = new();

    // ─────────────────────────────────────────────────────────────────────────
    // Energy Drain: pushed at oGCD priority 4 when ready and no stacks held
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void EnergyDrain_PushedAtPriority4_WhenReadyAndNoAetherflow()
    {
        var (scheduler, context) = BuildContext(
            energyDrainReady: true,
            hasAetherflow: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == PersephoneAbilities.EnergyDrain && c.Priority == 4);
    }

    [Fact]
    public void EnergyDrain_NotPushed_WhenDisabled()
    {
        var config = CreateConfig(enableEnergyDrain: false);
        var (scheduler, context) = BuildContext(
            config: config,
            energyDrainReady: true,
            hasAetherflow: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c =>
            c.Behavior == PersephoneAbilities.EnergyDrain ||
            c.Behavior == PersephoneAbilities.EnergySiphon);
    }

    [Fact]
    public void EnergyDrain_NotPushed_WhenHasAetherflow()
    {
        // HasAetherflow=true means stacks are held — don't refresh yet.
        var (scheduler, context) = BuildContext(
            energyDrainReady: true,
            hasAetherflow: true,
            aetherflowStacks: 2);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c =>
            c.Behavior == PersephoneAbilities.EnergyDrain ||
            c.Behavior == PersephoneAbilities.EnergySiphon);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Aetherflow spender (Fester / Necrotize): pushed at oGCD priority 5 during burst
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AetherflowSpender_PushedAtPriority5_DuringDemiPhase()
    {
        // hasAetherflow=true blocks EnergyDrain so only spender appears.
        // isDemiSummonActive=true sets inBurst=true, unblocking the spender path.
        // Level 100 → Necrotize (Lv.92 upgrade over Fester).
        var (scheduler, context) = BuildContext(
            hasAetherflow: true,
            aetherflowStacks: 2,
            aetherflowReserve: 0,
            isDemiSummonActive: true,
            energyDrainReady: false,   // keeps EnergyDrain out of the queue
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == PersephoneAbilities.Necrotize && c.Priority == 5);
    }

    [Fact]
    public void AetherflowSpender_PushedAtPriority5_DuringSearingLightWindow()
    {
        // hasSearingLight=true also sets inBurst=true.
        var (scheduler, context) = BuildContext(
            hasAetherflow: true,
            aetherflowStacks: 2,
            aetherflowReserve: 0,
            hasSearingLight: true,
            energyDrainReady: false,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == PersephoneAbilities.Necrotize && c.Priority == 5);
    }

    [Fact]
    public void AetherflowSpender_NotPushed_WhenStacksAtReserve()
    {
        // AetherflowReserve=1 with stacks=1: stacks <= reserve → skip.
        var (scheduler, context) = BuildContext(
            hasAetherflow: true,
            aetherflowStacks: 1,
            aetherflowReserve: 1,
            isDemiSummonActive: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c =>
            c.Behavior == PersephoneAbilities.Fester ||
            c.Behavior == PersephoneAbilities.Necrotize ||
            c.Behavior == PersephoneAbilities.Painflare);
    }

    [Fact]
    public void AetherflowSpender_NotPushed_WhenNotInBurst_AndEnergyDrainNotSoon()
    {
        // Not in burst (no demi, no searing light).
        // EnergyDrain is ready (energyDrainReady=true) → energyDrainSoon=false
        // because !EnergyDrainReady is false. Both conditions false → hold.
        var (scheduler, context) = BuildContext(
            hasAetherflow: true,
            aetherflowStacks: 2,
            aetherflowReserve: 0,
            isDemiSummonActive: false,
            hasSearingLight: false,
            energyDrainReady: true);   // ready now, not "soon"

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c =>
            c.Behavior == PersephoneAbilities.Fester ||
            c.Behavior == PersephoneAbilities.Necrotize ||
            c.Behavior == PersephoneAbilities.Painflare);
    }

    [Fact]
    public void AetherflowSpender_NotPushed_WhenFesterDisabled()
    {
        var config = CreateConfig(enableFester: false);
        var (scheduler, context) = BuildContext(
            config: config,
            hasAetherflow: true,
            aetherflowStacks: 2,
            aetherflowReserve: 0,
            isDemiSummonActive: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c =>
            c.Behavior == PersephoneAbilities.Fester ||
            c.Behavior == PersephoneAbilities.Necrotize ||
            c.Behavior == PersephoneAbilities.Painflare);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private (RotationScheduler scheduler, IPersephoneContext context) BuildContext(
        Configuration? config = null,
        byte level = 100,
        bool isDemiSummonActive = false,
        bool hasSearingLight = false,
        bool hasAetherflow = false,
        int aetherflowStacks = 0,
        int aetherflowReserve = 0,
        bool energyDrainReady = false,
        bool hasTitansFavor = false)
    {
        config ??= CreateConfig(aetherflowReserve: aetherflowReserve);

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(99999UL);

        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        // GetCooldownRemaining defaults to 0f (from MockBuilders), so energyDrainSoon=true
        // whenever energyDrainReady=false. Tests that need to block energyDrainSoon
        // should pass energyDrainReady=true instead (see AetherflowSpender_NotPushed test).
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = PersephoneTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: level,
            hasPetSummoned: true,
            inCombat: true,
            isDemiSummonActive: isDemiSummonActive,
            isBahamutActive: false,
            isPhoenixActive: false,
            isSolarBahamutActive: false,
            hasSearingLight: hasSearingLight,
            hasAetherflow: hasAetherflow,
            aetherflowStacks: aetherflowStacks,
            energyDrainReady: energyDrainReady,
            hasTitansFavor: hasTitansFavor,
            // Keep phase-tracking flags false so Enkindle/AstralFlow don't push.
            enkindleReady: false,
            astralFlowReady: false,
            hasUsedEnkindleThisPhase: false,
            hasUsedAstralFlowThisPhase: false,
            // SearingLight: not ready, so TryPushSearingLight exits early.
            searingLightReady: false);

        return (scheduler, context);
    }

    private static Configuration CreateConfig(
        bool enableEnergyDrain = true,
        bool enableFester = true,
        int aetherflowReserve = 0)
    {
        var cfg = new Configuration { Enabled = true, EnableDamage = true };
        cfg.Summoner.EnableEnergyDrain = enableEnergyDrain;
        cfg.Summoner.EnableFester = enableFester;
        cfg.Summoner.AetherflowReserve = aetherflowReserve;
        cfg.Summoner.EnableSearingLight = true;
        cfg.Summoner.EnableEnkindle = true;
        cfg.Summoner.EnableAstralFlow = true;
        cfg.Summoner.EnableMountainBuster = true;
        cfg.Summoner.EnableSearingFlash = true;
        cfg.Summoner.EnableBurstPooling = false;  // disable so pooling guard doesn't fire in tests
        cfg.CasterShared.EnableLucidDreaming = false;  // keep Lucid Dreaming out of candidate queue
        return cfg;
    }
}
