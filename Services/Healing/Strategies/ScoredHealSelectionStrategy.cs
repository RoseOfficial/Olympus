using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Services.Healing.Models;

namespace Olympus.Services.Healing.Strategies;

/// <summary>
/// Context for calculating heal scores.
/// Contains all factors that influence the heal score calculation.
/// </summary>
public sealed record HealScoreContext
{
    /// <summary>The action being scored.</summary>
    public required ActionDefinition Action { get; init; }

    /// <summary>Predicted heal amount.</summary>
    public required int HealAmount { get; init; }

    /// <summary>HP the target is missing.</summary>
    public required int MissingHp { get; init; }

    /// <summary>Whether the player has Freecure proc active.</summary>
    public required bool HasFreecure { get; init; }

    /// <summary>Whether we're in a weave window (for oGCD bonus).</summary>
    public required bool IsWeaveWindow { get; init; }

    /// <summary>Current lily count.</summary>
    public required int LilyCount { get; init; }

    /// <summary>Current blood lily count.</summary>
    public required int BloodLilyCount { get; init; }

    /// <summary>Whether in MP conservation mode.</summary>
    public required bool IsInMpConservationMode { get; init; }
}

/// <summary>
/// Scored heal selection strategy.
/// Evaluates all available heals and returns the highest-scoring option
/// based on multiple weighted factors.
/// </summary>
public sealed class ScoredHealSelectionStrategy : IHealSelectionStrategy
{
    public string StrategyName => "Scored";

    /// <inheritdoc/>
    public (ActionDefinition? action, int healAmount, string selectionReason) SelectBestSingleHeal(
        HealSelectionContext context,
        SpellCandidateEvaluator evaluator)
    {
        // Skip if target doesn't need healing
        if (context.MissingHp <= 0)
        {
            return (null, 0, "Target doesn't need healing (missingHp <= 0)");
        }

        var candidates = new List<(ActionDefinition action, int healAmount, float score, string reason)>();

        // Evaluate Afflatus Solace (Lily heal)
        if (context.LilyCount > 0)
        {
            var result = evaluator.EvaluateSingleTarget(
                WHMActions.AfflatusSolace,
                context.Player.Level,
                context.Mind, context.Det, context.Wd,
                context.Target);

            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateHealScore(new HealScoreContext
                {
                    Action = result.Action,
                    HealAmount = result.HealAmount,
                    MissingHp = context.MissingHp,
                    HasFreecure = context.HasFreecure,
                    IsWeaveWindow = context.IsWeaveWindow,
                    LilyCount = context.LilyCount,
                    BloodLilyCount = context.BloodLilyCount,
                    IsInMpConservationMode = context.IsInMpConservationMode
                }, context.Config.ScoreWeights);
                candidates.Add((result.Action, result.HealAmount, score, $"Lily heal (score: {score:F2})"));
            }
        }

        // Evaluate Regen (if needed)
        var needsRegen = (!context.HasRegen || context.RegenRemaining < FFXIVConstants.RegenRefreshThreshold) &&
                         context.HpPercent < FFXIVConstants.RegenHpThreshold;
        if (needsRegen)
        {
            var result = evaluator.EvaluateSingleTarget(
                WHMActions.Regen,
                context.Player.Level,
                context.Mind, context.Det, context.Wd,
                context.Target);

            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateHealScore(new HealScoreContext
                {
                    Action = result.Action,
                    HealAmount = result.HealAmount,
                    MissingHp = context.MissingHp,
                    HasFreecure = context.HasFreecure,
                    IsWeaveWindow = context.IsWeaveWindow,
                    LilyCount = context.LilyCount,
                    BloodLilyCount = context.BloodLilyCount,
                    IsInMpConservationMode = context.IsInMpConservationMode
                }, context.Config.ScoreWeights);
                candidates.Add((result.Action, result.HealAmount, score, $"Regen (score: {score:F2})"));
            }
        }

        // Evaluate Cure II
        {
            var result = evaluator.EvaluateSingleTarget(
                WHMActions.CureII,
                context.Player.Level,
                context.Mind, context.Det, context.Wd,
                context.Target,
                context.MissingHp);

            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateHealScore(new HealScoreContext
                {
                    Action = result.Action,
                    HealAmount = result.HealAmount,
                    MissingHp = context.MissingHp,
                    HasFreecure = context.HasFreecure,
                    IsWeaveWindow = context.IsWeaveWindow,
                    LilyCount = context.LilyCount,
                    BloodLilyCount = context.BloodLilyCount,
                    IsInMpConservationMode = context.IsInMpConservationMode
                }, context.Config.ScoreWeights);
                var freecureNote = context.HasFreecure ? " (Freecure!)" : "";
                candidates.Add((result.Action, result.HealAmount, score, $"Cure II{freecureNote} (score: {score:F2})"));
            }
        }

        // Evaluate Cure
        {
            var result = evaluator.EvaluateSingleTarget(
                WHMActions.Cure,
                context.Player.Level,
                context.Mind, context.Det, context.Wd,
                context.Target,
                context.MissingHp);

            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateHealScore(new HealScoreContext
                {
                    Action = result.Action,
                    HealAmount = result.HealAmount,
                    MissingHp = context.MissingHp,
                    HasFreecure = context.HasFreecure,
                    IsWeaveWindow = context.IsWeaveWindow,
                    LilyCount = context.LilyCount,
                    BloodLilyCount = context.BloodLilyCount,
                    IsInMpConservationMode = context.IsInMpConservationMode
                }, context.Config.ScoreWeights);
                candidates.Add((result.Action, result.HealAmount, score, $"Cure (score: {score:F2})"));
            }
        }

        // Select highest scoring candidate
        if (candidates.Count == 0)
            return (null, 0, "No valid candidates");

        var best = candidates.OrderByDescending(c => c.score).First();

        // Mark selected action
        evaluator.MarkAsSelected(best.action.ActionId);

        return (best.action, best.healAmount, best.reason);
    }

    /// <inheritdoc/>
    public (ActionDefinition? action, int healAmount, IBattleChara? cureIIITarget, string selectionReason) SelectBestAoEHeal(
        AoEHealSelectionContext context,
        SpellCandidateEvaluator evaluator)
    {
        // Check if we have enough targets
        var hasSelfCenteredTargets = context.InjuredCount >= context.Config.AoEHealMinTargets;
        var hasCureIIITargets = context.CureIIITargetCount >= context.Config.AoEHealMinTargets;

        if (!hasSelfCenteredTargets && !hasCureIIITargets)
        {
            return (null, 0, null,
                $"Not enough targets (self:{context.InjuredCount}, CureIII:{context.CureIIITargetCount} < {context.Config.AoEHealMinTargets} min)");
        }

        var candidates = new List<(ActionDefinition action, int healAmount, IBattleChara? target, float score, string reason)>();

        // Evaluate Afflatus Rapture (Lily AoE)
        if (context.LilyCount > 0 && hasSelfCenteredTargets)
        {
            var result = evaluator.EvaluateAoE(
                WHMActions.AfflatusRapture,
                context.Player.Level,
                context.Mind, context.Det, context.Wd);

            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateAoEScore(result.Action, result.HealAmount, context);
                candidates.Add((result.Action, result.HealAmount, null, score, $"Lily AoE (score: {score:F2})"));
            }
        }

        // Evaluate Cure III
        if (hasCureIIITargets && context.CureIIITarget is not null)
        {
            var result = evaluator.EvaluateAoE(
                WHMActions.CureIII,
                context.Player.Level,
                context.Mind, context.Det, context.Wd);

            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateAoEScore(result.Action, result.HealAmount, context);
                // Boost score for Cure III when party is well-stacked
                score *= 1.0f + (context.CureIIITargetCount * 0.1f);
                candidates.Add((result.Action, result.HealAmount, context.CureIIITarget, score,
                    $"Cure III on {context.CureIIITarget.Name} (score: {score:F2})"));
            }
        }

        // Evaluate Medica III
        if (hasSelfCenteredTargets && !context.AnyHaveRegen)
        {
            var result = evaluator.EvaluateAoE(
                WHMActions.MedicaIII,
                context.Player.Level,
                context.Mind, context.Det, context.Wd);

            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateAoEScore(result.Action, result.HealAmount, context);
                candidates.Add((result.Action, result.HealAmount, null, score, $"Medica III (score: {score:F2})"));
            }
        }

        // Evaluate Medica II
        if (hasSelfCenteredTargets && !context.AnyHaveRegen)
        {
            var result = evaluator.EvaluateAoE(
                WHMActions.MedicaII,
                context.Player.Level,
                context.Mind, context.Det, context.Wd);

            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateAoEScore(result.Action, result.HealAmount, context);
                candidates.Add((result.Action, result.HealAmount, null, score, $"Medica II (score: {score:F2})"));
            }
        }

        // Evaluate Medica
        if (hasSelfCenteredTargets)
        {
            var result = evaluator.EvaluateAoE(
                WHMActions.Medica,
                context.Player.Level,
                context.Mind, context.Det, context.Wd);

            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateAoEScore(result.Action, result.HealAmount, context);
                candidates.Add((result.Action, result.HealAmount, null, score, $"Medica (score: {score:F2})"));
            }
        }

        // Select highest scoring candidate
        if (candidates.Count == 0)
            return (null, 0, null, "No valid candidates");

        var best = candidates.OrderByDescending(c => c.score).First();

        // Mark selected action
        evaluator.MarkAsSelected(best.action.ActionId);

        return (best.action, best.healAmount, best.target, best.reason);
    }

    /// <summary>
    /// Calculates a score for a single-target heal based on multiple factors.
    /// Higher scores indicate better heal choices.
    /// </summary>
    private static float CalculateHealScore(HealScoreContext context, HealingScoreWeights weights)
    {
        var action = context.Action;
        var score = 0f;

        // 1. Potency efficiency: heal amount relative to max possible heal
        // Normalize by assuming max heal is ~30000 for a Benediction-class ability
        var potencyEfficiency = Math.Min(context.HealAmount / 30000f, 1f);
        score += potencyEfficiency * weights.Potency;

        // 2. MP efficiency: prefer no-MP-cost heals
        // Lily heals (0 MP) get 1.0, Freecure Cure II gets 1.0, normal heals get scaled by cost
        var mpEfficiency = action.MpCost switch
        {
            0 => 1.0f, // Lily heals, Regen tick
            _ when context.HasFreecure && action.ActionId == WHMActions.CureII.ActionId => 1.0f, // Free Cure II
            _ => Math.Max(0f, 1f - (action.MpCost / 1500f)) // Scale by MP cost (max ~1500 for expensive heals)
        };
        score += mpEfficiency * weights.MpEfficiency;

        // 3. Lily benefit: reward lily heals when building Blood Lilies
        var lilyBenefit = 0f;
        var isLilyHeal = action.ActionId == WHMActions.AfflatusSolace.ActionId ||
                         action.ActionId == WHMActions.AfflatusRapture.ActionId;
        if (isLilyHeal && context.LilyCount > 0)
        {
            // More benefit when blood lilies are low (need to build toward Misery)
            lilyBenefit = context.BloodLilyCount switch
            {
                0 => 1.0f,  // Maximum benefit - need to start building
                1 => 0.85f, // High benefit - close to Misery
                2 => 0.70f, // Good benefit - one more for Misery
                _ => 0.3f   // Minimal benefit - already have Misery ready
            };

            // Bonus if in MP conservation mode
            if (context.IsInMpConservationMode)
                lilyBenefit = Math.Min(1f, lilyBenefit + 0.2f);
        }
        score += lilyBenefit * weights.LilyBenefit;

        // 4. Freecure bonus: strongly prefer Cure II when Freecure is active
        var freecureBonus = 0f;
        if (context.HasFreecure && action.ActionId == WHMActions.CureII.ActionId)
        {
            freecureBonus = 1.0f; // Maximum bonus for using free Cure II
        }
        score += freecureBonus * weights.FreecureBonus;

        // 5. oGCD bonus: prefer oGCDs in weave windows to maintain DPS uptime
        var ogcdBonus = 0f;
        if (context.IsWeaveWindow && action.IsOGCD)
        {
            ogcdBonus = 1.0f;
        }
        score += ogcdBonus * weights.OgcdBonus;

        // 6. Overheal penalty: reduce score for excessive overhealing
        var overhealPenalty = 0f;
        if (context.MissingHp > 0)
        {
            var overhealRatio = (float)context.HealAmount / context.MissingHp;
            if (overhealRatio > 1.5f)
            {
                // Penalize heals that would overheal by more than 50%
                overhealPenalty = Math.Min(1f, (overhealRatio - 1.5f) / 2f);
            }
        }
        else
        {
            // If no HP missing, maximum penalty
            overhealPenalty = 1.0f;
        }
        score -= overhealPenalty * weights.OverhealPenalty;

        return Math.Max(0f, score);
    }

    /// <summary>
    /// Calculates a score for an AoE heal based on multiple factors.
    /// </summary>
    private static float CalculateAoEScore(ActionDefinition action, int healAmount, AoEHealSelectionContext context)
    {
        var score = 0f;

        // Potency efficiency
        var potencyEfficiency = Math.Min(healAmount / 15000f, 1f); // Lower max for AoE
        score += potencyEfficiency * 0.25f;

        // MP efficiency
        var mpEfficiency = action.MpCost switch
        {
            0 => 1.0f,
            _ => Math.Max(0f, 1f - (action.MpCost / 2000f))
        };
        score += mpEfficiency * 0.2f;

        // Lily benefit for AoE
        var isLilyHeal = action.ActionId == WHMActions.AfflatusRapture.ActionId;
        if (isLilyHeal && context.LilyCount > 0)
        {
            var lilyBenefit = context.BloodLilyCount switch
            {
                0 => 1.0f,
                1 => 0.85f,
                2 => 0.70f,
                _ => 0.3f
            };
            if (context.IsInMpConservationMode)
                lilyBenefit = Math.Min(1f, lilyBenefit + 0.2f);
            score += lilyBenefit * 0.3f;
        }

        // Target count bonus
        var targetBonus = Math.Min(context.InjuredCount / 8f, 1f);
        score += targetBonus * 0.15f;

        // HoT bonus if no existing regen
        if (!context.AnyHaveRegen &&
            (action.ActionId == WHMActions.MedicaII.ActionId ||
             action.ActionId == WHMActions.MedicaIII.ActionId))
        {
            score += 0.1f;
        }

        return score;
    }
}
