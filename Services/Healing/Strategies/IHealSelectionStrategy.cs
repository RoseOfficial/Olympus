using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Models.Action;
using Olympus.Services.Healing.Models;

namespace Olympus.Services.Healing.Strategies;

/// <summary>
/// Strategy interface for heal selection algorithms.
/// Implementations choose between different approaches (tiered priority vs scored).
/// </summary>
public interface IHealSelectionStrategy
{
    /// <summary>
    /// Gets the name of this selection strategy for debugging.
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Selects the best single-target heal for the given context.
    /// </summary>
    /// <param name="context">The selection context with all relevant factors.</param>
    /// <param name="evaluator">The spell candidate evaluator for validation.</param>
    /// <returns>The best action and its heal amount, or null if no valid heal found.</returns>
    (ActionDefinition? action, int healAmount, string selectionReason) SelectBestSingleHeal(
        HealSelectionContext context,
        SpellCandidateEvaluator evaluator);

    /// <summary>
    /// Selects the best AoE heal for the given context.
    /// </summary>
    /// <param name="context">The AoE selection context with all relevant factors.</param>
    /// <param name="evaluator">The spell candidate evaluator for validation.</param>
    /// <returns>The best action, heal amount, Cure III target (if selected), and selection reason.</returns>
    (ActionDefinition? action, int healAmount, IBattleChara? cureIIITarget, string selectionReason) SelectBestAoEHeal(
        AoEHealSelectionContext context,
        SpellCandidateEvaluator evaluator);
}
