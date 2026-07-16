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
/// Tests for ScoredHealSelectionStrategy.
/// Covers the MiseryReady candidate exclusion; deeper scoring interactions
/// require Dalamud runtime.
/// </summary>
public class ScoredHealSelectionStrategyTests
{
    [Fact]
    public void ScoredStrategy_StrategyName_ReturnsScored()
    {
        var strategy = new ScoredHealSelectionStrategy();

        Assert.Equal("Scored", strategy.StrategyName);
    }

    [Fact]
    public void ScoredStrategy_ImplementsInterface()
    {
        var strategy = new ScoredHealSelectionStrategy();

        Assert.IsAssignableFrom<IHealSelectionStrategy>(strategy);
    }

    // -----------------------------------------------------------------------
    // MiseryReady gate: lily candidates are excluded from scoring entirely.
    // Score-zeroing the lily benefit is insufficient — Solace's 0-MP efficiency
    // term alone outscores Cure II, so the exclusion must happen at evaluation.
    // -----------------------------------------------------------------------
    [Fact]
    public void SelectBestSingleHeal_MiseryReady_NeverSelectsLilyHeal()
    {
        var strategy = new ScoredHealSelectionStrategy();
        var (action, _, _) = strategy.SelectBestSingleHeal(
            CreateContext(miseryReady: true), CreateEvaluator());

        Assert.NotNull(action);
        Assert.NotEqual(WHMActions.AfflatusSolace.ActionId, action!.ActionId);
    }

    [Fact]
    public void SelectBestSingleHeal_BloodFullButMiseryNotDispatchable_SelectsLilyHeal()
    {
        var strategy = new ScoredHealSelectionStrategy();
        var (action, _, _) = strategy.SelectBestSingleHeal(
            CreateContext(miseryReady: false), CreateEvaluator());

        Assert.NotNull(action);
        Assert.Equal(WHMActions.AfflatusSolace.ActionId, action!.ActionId);
    }

    private static SpellCandidateEvaluator CreateEvaluator()
    {
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>())).Returns(true);

        var enablement = new Mock<ISpellEnablementService>();
        enablement.Setup(e => e.IsSpellEnabled(It.IsAny<uint>())).Returns(true);

        return new SpellCandidateEvaluator(actionService.Object, enablement.Object);
    }

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
}
