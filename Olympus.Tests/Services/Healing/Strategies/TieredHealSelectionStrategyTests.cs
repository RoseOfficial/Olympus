using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Services.Healing;
using Olympus.Services.Healing.Models;
using Olympus.Services.Healing.Strategies;
using Olympus.Tests.Mocks;
using Xunit;

namespace Olympus.Tests.Services.Healing.Strategies;

/// <summary>
/// Tests for TieredHealSelectionStrategy.
/// Covers the MiseryReady lily gate end-to-end through SelectBestSingleHeal;
/// deeper tier interactions require Dalamud runtime.
/// </summary>
public class TieredHealSelectionStrategyTests
{
    [Fact]
    public void TieredStrategy_StrategyName_ReturnsTierBased()
    {
        var strategy = new TieredHealSelectionStrategy();

        Assert.Equal("Tier-Based", strategy.StrategyName);
    }

    [Fact]
    public void TieredStrategy_ImplementsInterface()
    {
        var strategy = new TieredHealSelectionStrategy();

        Assert.IsAssignableFrom<IHealSelectionStrategy>(strategy);
    }

    // -----------------------------------------------------------------------
    // MiseryReady gate: with blood lily capped and Misery dispatchable, Tier 1
    // must not select Afflatus Solace — a lily spend returns no gauge and its
    // scheduler candidate would outrank the pending Misery.
    // -----------------------------------------------------------------------
    [Fact]
    public void SelectBestSingleHeal_MiseryReady_DoesNotSelectLilyHeal()
    {
        var strategy = new TieredHealSelectionStrategy();
        var (action, _, _) = strategy.SelectBestSingleHeal(
            CreateContext(miseryReady: true), CreateEvaluator());

        Assert.NotNull(action);
        Assert.NotEqual(WHMActions.AfflatusSolace.ActionId, action!.ActionId);
    }

    // -----------------------------------------------------------------------
    // Heal-only regression guard: blood lily capped but Misery NOT dispatchable
    // (damage disabled / below 74) — lily heals stay preferred so free heals and
    // overcap prevention keep working.
    // -----------------------------------------------------------------------
    [Fact]
    public void SelectBestSingleHeal_BloodFullButMiseryNotDispatchable_SelectsLilyHeal()
    {
        var strategy = new TieredHealSelectionStrategy();
        var (action, _, reason) = strategy.SelectBestSingleHeal(
            CreateContext(miseryReady: false), CreateEvaluator());

        Assert.NotNull(action);
        Assert.Equal(WHMActions.AfflatusSolace.ActionId, action!.ActionId);
        Assert.StartsWith("Tier 1", reason);
    }

    // -----------------------------------------------------------------------
    // Tier 5 lily AoE fallback: when Medicas are blocked (regen up + overheal)
    // the fallback Rapture must also respect MiseryReady — returning nothing
    // lets the pending Misery win the GCD instead.
    // -----------------------------------------------------------------------
    [Fact]
    public void SelectBestAoEHeal_MiseryReady_Tier5FallbackDoesNotSelectRapture()
    {
        var strategy = new TieredHealSelectionStrategy();
        var (action, _, _, _) = strategy.SelectBestAoEHeal(
            CreateAoEContext(miseryReady: true), CreateEvaluator(medicaDisabled: true));

        Assert.Null(action);
    }

    [Fact]
    public void SelectBestAoEHeal_MiseryNotDispatchable_Tier5FallbackSelectsRapture()
    {
        var strategy = new TieredHealSelectionStrategy();
        var (action, _, _, _) = strategy.SelectBestAoEHeal(
            CreateAoEContext(miseryReady: false), CreateEvaluator(medicaDisabled: true));

        Assert.NotNull(action);
        Assert.Equal(WHMActions.AfflatusRapture.ActionId, action!.ActionId);
    }

    private static SpellCandidateEvaluator CreateEvaluator(bool medicaDisabled = false)
    {
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>())).Returns(true);

        var enablement = new Mock<ISpellEnablementService>();
        enablement.Setup(e => e.IsSpellEnabled(It.IsAny<uint>()))
            .Returns((uint actionId) => !(medicaDisabled && actionId == WHMActions.Medica.ActionId));

        return new SpellCandidateEvaluator(actionService.Object, enablement.Object);
    }

    /// <summary>
    /// Blood lily 3/3, lilies available, target injured. Regen is active so
    /// Tier 2 is blocked; with Tier 1 gated the selection falls to Cure II.
    /// CombatDuration > 60s makes ShouldPreferLilyHeal return true in the
    /// non-gated case, so the gate is the only variable under test.
    /// </summary>
    private static HealSelectionContext CreateContext(bool miseryReady)
    {
        var player = MockBuilders.CreateMockPlayerCharacter(level: 100);

        var target = new Mock<IBattleChara>();
        target.Setup(x => x.CurrentHp).Returns(20000u);
        target.Setup(x => x.MaxHp).Returns(220000u);

        return new HealSelectionContext
        {
            Player = player.Object,
            Target = target.Object,
            Mind = 3000,
            Det = 2000,
            Wd = 130,
            MissingHp = 200000,
            HpPercent = 0.5f,
            LilyCount = 3,
            BloodLilyCount = 3,
            IsWeaveWindow = false,
            HasFreecure = false,
            HasRegen = true,
            RegenRemaining = 15f,
            IsInMpConservationMode = false,
            LilyStrategy = LilyGenerationStrategy.Balanced,
            CombatDuration = 120f,
            Config = new HealingConfig(),
            MiseryReady = miseryReady,
        };
    }

    /// <summary>
    /// AoE context that forces the Tier 5 fallback: enough injured for
    /// self-centered AoE, no Cure III stack, regen active (blocks Medica II/III),
    /// and AverageMissingHp = 1 so Medica is overheal-rejected. Only the lily
    /// fallback remains, gated by MiseryReady.
    /// </summary>
    private static AoEHealSelectionContext CreateAoEContext(bool miseryReady)
    {
        var player = MockBuilders.CreateMockPlayerCharacter(level: 100);

        return new AoEHealSelectionContext
        {
            Player = player.Object,
            Mind = 3000,
            Det = 2000,
            Wd = 130,
            AverageMissingHp = 1,
            InjuredCount = 8,
            AnyHaveRegen = true,
            IsWeaveWindow = false,
            CureIIITargetCount = 0,
            IsInMpConservationMode = false,
            LilyCount = 3,
            BloodLilyCount = 3,
            LilyStrategy = LilyGenerationStrategy.Balanced,
            CombatDuration = 120f,
            Config = new HealingConfig(),
            MiseryReady = miseryReady,
        };
    }
}
