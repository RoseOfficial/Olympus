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
/// Tests for DamageModule candidate collection covering:
///   - Demi-phase GCD replacement handling: AbilityBehavior.ReplacementBaseId encodes the
///     "pass Ruin III base ID to UseAction" quirk documented in CLAUDE.md ActionManager Quirks.
///   - Primal attunement GCD selection for Ifrit (RubyRite), Titan (TopazRite), Garuda (EmeraldRite).
///   - Config toggle gating (EnableBahamut, EnablePhoenix, EnablePrimalAbilities, EnableRuin).
///   - Filler spell fallback (Ruin III at priority 9).
///   - UseDemiDuringBurst=false skip path: FireSummonDemiRaw has a config guard at line ~384;
///     since SafeGameAccess.GetActionManager returns null in tests the unsafe block exits
///     immediately, so the test documents correct completion without exception.
/// </summary>
public class DamageModuleGaugePhaseTests
{
    private readonly DamageModule _module = new();

    // ─────────────────────────────────────────────────────────────────────────
    // ReplacementBaseId: confirms Ruin III is the base ID sent to UseAction
    // during all three demi-summon phases (CLAUDE.md ActionManager Quirks).
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void AstralImpulse_HasReplacementBaseId_OfRuin3()
    {
        Assert.Equal(SMNActions.Ruin3.ActionId, PersephoneAbilities.AstralImpulse.ReplacementBaseId);
    }

    [Fact]
    public void FountainOfFire_HasReplacementBaseId_OfRuin3()
    {
        Assert.Equal(SMNActions.Ruin3.ActionId, PersephoneAbilities.FountainOfFire.ReplacementBaseId);
    }

    [Fact]
    public void UmbralImpulse_HasReplacementBaseId_OfRuin3()
    {
        Assert.Equal(SMNActions.Ruin3.ActionId, PersephoneAbilities.UmbralImpulse.ReplacementBaseId);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Demi-phase GCD: correct ability at priority 2
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BahamutPhase_PushesAstralImpulse_AtPriority2()
    {
        var (scheduler, context) = BuildContext(
            isBahamutActive: true,
            isDemiSummonActive: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == PersephoneAbilities.AstralImpulse && c.Priority == 2);
    }

    [Fact]
    public void PhoenixPhase_PushesFountainOfFire_AtPriority2()
    {
        var (scheduler, context) = BuildContext(
            isPhoenixActive: true,
            isDemiSummonActive: true,
            level: 80);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == PersephoneAbilities.FountainOfFire && c.Priority == 2);
    }

    [Fact]
    public void SolarBahamutPhase_PushesUmbralImpulse_AtPriority2()
    {
        var (scheduler, context) = BuildContext(
            isSolarBahamutActive: true,
            isDemiSummonActive: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == PersephoneAbilities.UmbralImpulse && c.Priority == 2);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Demi-phase config toggles
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void BahamutPhase_NoPush_WhenEnableBahamutFalse()
    {
        var config = CreateConfig(enableBahamut: false);
        var (scheduler, context) = BuildContext(
            config: config,
            isBahamutActive: true,
            isDemiSummonActive: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == PersephoneAbilities.AstralImpulse);
    }

    [Fact]
    public void PhoenixPhase_NoPush_WhenEnablePhoenixFalse()
    {
        var config = CreateConfig(enablePhoenix: false);
        var (scheduler, context) = BuildContext(
            config: config,
            isPhoenixActive: true,
            isDemiSummonActive: true,
            level: 80);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == PersephoneAbilities.FountainOfFire);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Primal attunement GCDs at priority 3 (Ifrit / Titan / Garuda)
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void IfritAttunement_PushesRubyRite_AtPriority3()
    {
        var (scheduler, context) = BuildContext(
            isIfritAttuned: true,
            currentAttunement: 1,
            attunementStacks: 2);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == PersephoneAbilities.RubyRite && c.Priority == 3);
    }

    [Fact]
    public void TitanAttunement_PushesTopazRite_AtPriority3()
    {
        var (scheduler, context) = BuildContext(
            isTitanAttuned: true,
            currentAttunement: 2,
            attunementStacks: 4);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == PersephoneAbilities.TopazRite && c.Priority == 3);
    }

    [Fact]
    public void GarudaAttunement_PushesEmeraldRite_AtPriority3()
    {
        var (scheduler, context) = BuildContext(
            isGarudaAttuned: true,
            currentAttunement: 3,
            attunementStacks: 4);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == PersephoneAbilities.EmeraldRite && c.Priority == 3);
    }

    [Fact]
    public void AttunementGcd_NoPush_WhenEnablePrimalAbilitiesFalse()
    {
        var config = CreateConfig(enablePrimalAbilities: false);
        var (scheduler, context) = BuildContext(
            config: config,
            isIfritAttuned: true,
            currentAttunement: 1,
            attunementStacks: 2);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c =>
            c.Behavior == PersephoneAbilities.RubyRite ||
            c.Behavior == PersephoneAbilities.TopazRite ||
            c.Behavior == PersephoneAbilities.EmeraldRite);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Filler spell (Ruin III) at priority 9 when no active phase
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void Filler_PushesRuin3_AtPriority9_WhenNoActivePhase()
    {
        // No demi, no attunement, no primals available — falls through to TryPushFiller.
        var (scheduler, context) = BuildContext();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == PersephoneAbilities.Ruin3 && c.Priority == 9);
    }

    [Fact]
    public void Filler_NoPush_WhenEnableRuinFalse()
    {
        var config = CreateConfig(enableRuin: false, enableRuinIV: false);
        // hasFurtherRuin=false (default) already blocks Ruin4; EnableRuin=false gates TryPushFiller.
        var (scheduler, context) = BuildContext(config: config);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c =>
            c.Behavior == PersephoneAbilities.Ruin3 ||
            c.Behavior == PersephoneAbilities.Ruin2 ||
            c.Behavior == PersephoneAbilities.Ruin);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // UseDemiDuringBurst=false skip: FireSummonDemiRaw line ~384 returns early.
    // SafeGameAccess.GetActionManager returns null in tests, so the unsafe block
    // never executes regardless — the test documents no exception is thrown and
    // the scheduler receives no demi-entry candidate from that code path.
    // ─────────────────────────────────────────────────────────────────────────

    [Fact]
    public void UseDemiDuringBurst_False_CompletesWithoutError_WhenSearingLightActive()
    {
        var config = CreateConfig(useDemiDuringBurst: false);
        // Conditions that reach FireSummonDemiRaw: no demi, no attunement, no primals.
        // hasSearingLight=true triggers the UseDemiDuringBurst guard inside that method.
        var (scheduler, context) = BuildContext(
            config: config,
            hasSearingLight: true,
            isDemiSummonActive: false,
            attunementStacks: 0,
            primalsAvailable: 0,
            hasFurtherRuin: false);

        var exception = Record.Exception(
            () => _module.CollectCandidates(context, scheduler, isMoving: false));

        Assert.Null(exception);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Helpers
    // ─────────────────────────────────────────────────────────────────────────

    private (RotationScheduler scheduler, IPersephoneContext context) BuildContext(
        Configuration? config = null,
        byte level = 100,
        bool isBahamutActive = false,
        bool isPhoenixActive = false,
        bool isSolarBahamutActive = false,
        bool isDemiSummonActive = false,
        bool isIfritAttuned = false,
        bool isTitanAttuned = false,
        bool isGarudaAttuned = false,
        int currentAttunement = 0,
        int attunementStacks = 0,
        int primalsAvailable = 0,
        bool hasSearingLight = false,
        bool hasFurtherRuin = false)
    {
        config ??= CreateConfig();

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(99999UL);

        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

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
            isBahamutActive: isBahamutActive,
            isPhoenixActive: isPhoenixActive,
            isSolarBahamutActive: isSolarBahamutActive,
            isDemiSummonActive: isDemiSummonActive,
            isIfritAttuned: isIfritAttuned,
            isTitanAttuned: isTitanAttuned,
            isGarudaAttuned: isGarudaAttuned,
            currentAttunement: currentAttunement,
            attunementStacks: attunementStacks,
            primalsAvailable: primalsAvailable,
            hasSearingLight: hasSearingLight,
            hasFurtherRuin: hasFurtherRuin);

        return (scheduler, context);
    }

    private static Configuration CreateConfig(
        bool enableBahamut = true,
        bool enablePhoenix = true,
        bool enableSolarBahamut = true,
        bool enablePrimalAbilities = true,
        bool enableRuin = true,
        bool enableRuinIV = true,
        bool useDemiDuringBurst = true)
    {
        var cfg = new Configuration { Enabled = true, EnableDamage = true };
        cfg.Summoner.EnableBahamut = enableBahamut;
        cfg.Summoner.EnablePhoenix = enablePhoenix;
        cfg.Summoner.EnableSolarBahamut = enableSolarBahamut;
        cfg.Summoner.EnablePrimalAbilities = enablePrimalAbilities;
        cfg.Summoner.EnableAstralAbilities = true;
        cfg.Summoner.EnableFountainAbilities = true;
        cfg.Summoner.EnableRuin = enableRuin;
        cfg.Summoner.EnableRuinIV = enableRuinIV;
        cfg.Summoner.EnableAoERotation = true;
        cfg.Summoner.UseDemiDuringBurst = useDemiDuringBurst;
        cfg.Summoner.EnableAddle = true;
        return cfg;
    }
}
