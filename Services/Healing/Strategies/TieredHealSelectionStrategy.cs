using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Services.Healing.Models;

namespace Olympus.Services.Healing.Strategies;

/// <summary>
/// Tiered priority heal selection strategy.
/// Uses a fixed tier order: oGCDs > Lily heals > Regen > GCD heals
/// This is the default, battle-tested approach.
/// </summary>
public sealed class TieredHealSelectionStrategy : IHealSelectionStrategy
{
    public string StrategyName => "Tier-Based";

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

        ActionDefinition? selectedAction = null;
        int selectedHealAmount = 0;
        string selectionReason = "No valid candidates";

        // Note: Benediction and Tetragrammaton are now handled as oGCDs in Apollo.TryExecuteOgcd()
        // with proper priority ordering (emergency heals first, then regular oGCDs)

        // === TIER 1: Lily GCD (Afflatus Solace) ===
        // Blood Lily optimization: prefer lily heals based on strategy to generate Blood Lilies
        // MP conservation: aggressively prefer lily heals when MP is low
        var shouldUseLily = context.LilyCount > 0 &&
            (ShouldPreferLilyHeal(context) ||
             (context.IsInMpConservationMode && context.Config.PreferLiliesInConservationMode));

        if (shouldUseLily)
        {
            var result = evaluator.EvaluateSingleTarget(
                WHMActions.AfflatusSolace,
                context.Player.Level,
                context.Mind, context.Det, context.Wd,
                context.Target);

            if (result.IsValid && result.Action is not null)
            {
                selectedAction = result.Action;
                selectedHealAmount = result.HealAmount;
                var mpNote = context.IsInMpConservationMode ? ", MP conservation" : "";
                selectionReason = $"Tier 1: Lily heal ({context.LilyCount} lilies, {context.BloodLilyCount}/3 Blood, {context.LilyStrategy}{mpNote})";
            }
        }
        else if (context.LilyCount == 0)
        {
            evaluator.TrackRejected(WHMActions.AfflatusSolace, 0, "No lilies available");
        }
        else
        {
            evaluator.TrackRejected(WHMActions.AfflatusSolace, 0,
                $"Strategy {context.LilyStrategy}: Blood {context.BloodLilyCount}/3, HP {context.HpPercent:P0}");
        }

        // === TIER 2: Regen (HoT) ===
        // Regen is the only spell with an HP threshold
        // Apply if target is below threshold HP and doesn't have Regen or it's about to expire
        // Threshold is dynamic based on damage rate: higher damage = apply earlier
        if (selectedAction is null)
        {
            // Dynamic Regen threshold based on damage rate
            // High damage (>300 DPS): Apply earlier at 75% HP
            // Low damage (<100 DPS): Defer to 45% HP
            // Normal: Use default 60%
            var dynamicRegenThreshold = context.DamageRate switch
            {
                > 300f => 0.75f,  // High damage - apply early
                < 100f => 0.45f,  // Low damage - defer
                _ => FFXIVConstants.RegenHpThreshold  // Normal (0.60f)
            };
            var targetBelowThreshold = context.HpPercent < dynamicRegenThreshold;
            var needsRegen = (!context.HasRegen || context.RegenRemaining < FFXIVConstants.RegenRefreshThreshold)
                && targetBelowThreshold;

            if (needsRegen)
            {
                var result = evaluator.EvaluateSingleTarget(
                    WHMActions.Regen,
                    context.Player.Level,
                    context.Mind, context.Det, context.Wd,
                    context.Target);

                if (result.IsValid && result.Action is not null)
                {
                    selectedAction = result.Action;
                    selectedHealAmount = result.HealAmount;
                    selectionReason = context.HasRegen ? "Tier 2: Regen (refresh)" : "Tier 2: Regen";
                }
            }
            else if (!targetBelowThreshold)
            {
                evaluator.TrackRejected(WHMActions.Regen, 0,
                    $"HP {context.HpPercent:P0} >= {dynamicRegenThreshold:P0} threshold (DPS: {context.DamageRate:F0})");
            }
            else
            {
                evaluator.TrackRejected(WHMActions.Regen, 0,
                    $"Already has Regen ({context.RegenRemaining:F1}s remaining)");
            }
        }

        // === TIER 3: Regular GCD (Cure II with Freecure, or Cure) ===
        // Apply overheal prevention to GCD heals to avoid wasting MP
        // MP conservation: prefer Cure over Cure II to save MP and fish for Freecure procs
        if (selectedAction is null)
        {
            // Prioritize Cure II if we have Freecure proc (always use free spells)
            if (context.HasFreecure)
            {
                var result = evaluator.EvaluateSingleTarget(
                    WHMActions.CureII,
                    context.Player.Level,
                    context.Mind, context.Det, context.Wd,
                    context.Target,
                    context.MissingHp);

                if (result.IsValid && result.Action is not null)
                {
                    selectedAction = result.Action;
                    selectedHealAmount = result.HealAmount;
                    selectionReason = "Tier 3: Cure II (Freecure proc)";
                }
            }

            // In MP conservation mode, prefer Cure over Cure II (unless Freecure proc)
            var preferCureForConservation = context.IsInMpConservationMode &&
                                            context.Config.PreferCureInConservationMode &&
                                            !context.HasFreecure;

            if (preferCureForConservation && selectedAction is null)
            {
                // Try Cure first (lower MP cost, may proc Freecure)
                var result = evaluator.EvaluateSingleTarget(
                    WHMActions.Cure,
                    context.Player.Level,
                    context.Mind, context.Det, context.Wd,
                    context.Target,
                    context.MissingHp);

                if (result.IsValid && result.Action is not null)
                {
                    selectedAction = result.Action;
                    selectedHealAmount = result.HealAmount;
                    selectionReason = "Tier 3: Cure (MP conservation)";
                }
            }

            // Try Cure II without Freecure (normal mode or Cure didn't work)
            if (selectedAction is null && !preferCureForConservation)
            {
                var result = evaluator.EvaluateSingleTarget(
                    WHMActions.CureII,
                    context.Player.Level,
                    context.Mind, context.Det, context.Wd,
                    context.Target,
                    context.MissingHp);

                if (result.IsValid && result.Action is not null)
                {
                    selectedAction = result.Action;
                    selectedHealAmount = result.HealAmount;
                    selectionReason = "Tier 3: Cure II";
                }
            }

            // Fallback to Cure (or Cure II in conservation mode if Cure didn't work)
            if (selectedAction is null)
            {
                var fallbackAction = preferCureForConservation ? WHMActions.CureII : WHMActions.Cure;
                var result = evaluator.EvaluateSingleTarget(
                    fallbackAction,
                    context.Player.Level,
                    context.Mind, context.Det, context.Wd,
                    context.Target,
                    context.MissingHp);

                if (result.IsValid && result.Action is not null)
                {
                    selectedAction = result.Action;
                    selectedHealAmount = result.HealAmount;
                    selectionReason = preferCureForConservation
                        ? "Tier 3: Cure II (fallback, MP conservation)"
                        : "Tier 3: Cure (fallback)";
                }
            }
        }

        // Mark selected action
        if (selectedAction is not null)
        {
            evaluator.MarkAsSelected(selectedAction.ActionId);
        }

        return (selectedAction, selectedHealAmount, selectionReason);
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

        // Note: Assize is handled as a DPS oGCD in Apollo.TryExecuteOgcd, not here.
        // It's used on cooldown for damage, with healing as a bonus.

        ActionDefinition? selectedAction = null;
        int selectedHealAmount = 0;
        string selectionReason = "No valid candidates";
        IBattleChara? selectedCureIIITarget = null;

        // === TIER 1: Lily AoE (Afflatus Rapture) ===
        // Blood Lily optimization: prefer lily heals based on strategy to generate Blood Lilies
        // MP conservation: aggressively prefer lily heals when MP is low
        // For AoE heals, we use 0 for HP percent since we're healing multiple targets
        var shouldUseLilyAoE = context.LilyCount > 0 && hasSelfCenteredTargets &&
            (ShouldPreferLilyHealForAoE(context) ||
             (context.IsInMpConservationMode && context.Config.PreferLiliesInConservationMode));

        if (shouldUseLilyAoE)
        {
            var result = evaluator.EvaluateAoE(
                WHMActions.AfflatusRapture,
                context.Player.Level,
                context.Mind, context.Det, context.Wd);

            if (result.IsValid && result.Action is not null)
            {
                selectedAction = result.Action;
                selectedHealAmount = result.HealAmount;
                var mpNote = context.IsInMpConservationMode ? ", MP conservation" : "";
                selectionReason = $"Tier 1: Lily AoE heal ({context.LilyCount} lilies, {context.BloodLilyCount}/3 Blood, {context.LilyStrategy}{mpNote})";
            }
        }
        else if (context.LilyCount == 0)
        {
            evaluator.TrackRejected(WHMActions.AfflatusRapture, 0, "No lilies available");
        }
        else if (!hasSelfCenteredTargets)
        {
            evaluator.TrackRejected(WHMActions.AfflatusRapture, 0,
                $"Not enough self-centered targets ({context.InjuredCount})");
        }
        else
        {
            evaluator.TrackRejected(WHMActions.AfflatusRapture, 0,
                $"Strategy {context.LilyStrategy}: Blood {context.BloodLilyCount}/3");
        }

        // === TIER 1.5: Cure III (targeted AoE, when party is stacked) ===
        // Higher potency (600) than Medica (400), but requires stacked party within 10y
        if (selectedAction is null && hasCureIIITargets && context.CureIIITarget is not null)
        {
            var result = evaluator.EvaluateAoE(
                WHMActions.CureIII,
                context.Player.Level,
                context.Mind, context.Det, context.Wd);

            if (result.IsValid && result.Action is not null)
            {
                selectedAction = result.Action;
                selectedHealAmount = result.HealAmount;
                selectedCureIIITarget = context.CureIIITarget;
                selectionReason = $"Tier 1.5: Cure III ({context.CureIIITargetCount} stacked around {context.CureIIITarget.Name})";
            }
        }
        else if (selectedAction is null && !hasCureIIITargets)
        {
            evaluator.TrackRejected(WHMActions.CureIII, 0,
                $"Not enough stacked targets ({context.CureIIITargetCount} < {context.Config.AoEHealMinTargets})");
        }
        else if (selectedAction is null && context.CureIIITarget is null)
        {
            evaluator.TrackRejected(WHMActions.CureIII, 0, "No valid Cure III target");
        }

        // === TIER 2: Medica III (HoT, highest potency) ===
        if (selectedAction is null && hasSelfCenteredTargets)
        {
            if (context.AnyHaveRegen)
            {
                evaluator.TrackRejected(WHMActions.MedicaIII, 0, "Would overwrite existing regen");
            }
            else
            {
                var result = evaluator.EvaluateAoE(
                    WHMActions.MedicaIII,
                    context.Player.Level,
                    context.Mind, context.Det, context.Wd);

                if (result.IsValid && result.Action is not null)
                {
                    selectedAction = result.Action;
                    selectedHealAmount = result.HealAmount;
                    selectionReason = "Tier 2: Medica III (HoT)";
                }
            }
        }

        // === TIER 3: Medica II (HoT) ===
        if (selectedAction is null && hasSelfCenteredTargets)
        {
            if (context.AnyHaveRegen)
            {
                evaluator.TrackRejected(WHMActions.MedicaII, 0, "Would overwrite existing regen");
            }
            else
            {
                var result = evaluator.EvaluateAoE(
                    WHMActions.MedicaII,
                    context.Player.Level,
                    context.Mind, context.Det, context.Wd);

                if (result.IsValid && result.Action is not null)
                {
                    selectedAction = result.Action;
                    selectedHealAmount = result.HealAmount;
                    selectionReason = "Tier 3: Medica II (HoT)";
                }
            }
        }

        // === TIER 4: Medica (fallback) ===
        if (selectedAction is null && hasSelfCenteredTargets)
        {
            var result = evaluator.EvaluateAoE(
                WHMActions.Medica,
                context.Player.Level,
                context.Mind, context.Det, context.Wd);

            if (result.IsValid && result.Action is not null)
            {
                selectedAction = result.Action;
                selectedHealAmount = result.HealAmount;
                selectionReason = "Tier 4: Medica (fallback)";
            }
        }

        // Mark selected action
        if (selectedAction is not null)
        {
            evaluator.MarkAsSelected(selectedAction.ActionId);
        }

        return (selectedAction, selectedHealAmount, selectedCureIIITarget, selectionReason);
    }

    /// <summary>
    /// Determines whether lily heals (Afflatus Solace) should be preferred
    /// over MP-based alternatives based on the configured Blood Lily strategy.
    /// Now considers combat duration for smarter lily flushing.
    /// </summary>
    private static bool ShouldPreferLilyHeal(HealSelectionContext context)
    {
        if (context.LilyCount == 0)
            return false;

        // Time-aware lily flush: If combat has been going for a while and we have
        // blood lilies ready, prefer lily heals to ensure we get Afflatus Misery off.
        // Lily regenerates every 20 seconds, so after ~60s we should be spending freely.
        if (context.CombatDuration > 60f && context.BloodLilyCount >= 2 && context.LilyCount > 0)
            return true;

        // If we have full blood lilies (3/3) and lilies available, we should spend
        // lilies even though blood lily won't increase (prevents lily waste)
        if (context.BloodLilyCount >= 3 && context.LilyCount > 0)
            return true;

        // Aggressive lily flush: when at 2 blood lilies, always prefer lily heals
        // to build the third blood lily for Afflatus Misery before combat ends
        if (context.Config.EnableAggressiveLilyFlush && context.BloodLilyCount >= 2)
            return true;

        return context.LilyStrategy switch
        {
            LilyGenerationStrategy.Aggressive => true,
            LilyGenerationStrategy.Balanced => context.BloodLilyCount < 3,
            LilyGenerationStrategy.Conservative => context.BloodLilyCount < 3 &&
                context.HpPercent < context.Config.ConservativeLilyHpThreshold,
            LilyGenerationStrategy.Disabled => false,
            _ => context.BloodLilyCount < 3 // Default to Balanced behavior
        };
    }

    /// <summary>
    /// Determines whether lily heals (Afflatus Rapture) should be preferred for AoE.
    /// For AoE heals, HP percent is not relevant since we're healing multiple targets.
    /// </summary>
    private static bool ShouldPreferLilyHealForAoE(AoEHealSelectionContext context)
    {
        if (context.LilyCount == 0)
            return false;

        // Time-aware lily flush
        if (context.CombatDuration > 60f && context.BloodLilyCount >= 2 && context.LilyCount > 0)
            return true;

        // Full blood lilies
        if (context.BloodLilyCount >= 3 && context.LilyCount > 0)
            return true;

        // Aggressive lily flush
        if (context.Config.EnableAggressiveLilyFlush && context.BloodLilyCount >= 2)
            return true;

        return context.LilyStrategy switch
        {
            LilyGenerationStrategy.Aggressive => true,
            LilyGenerationStrategy.Balanced => context.BloodLilyCount < 3,
            LilyGenerationStrategy.Conservative => context.BloodLilyCount < 3,
            LilyGenerationStrategy.Disabled => false,
            _ => context.BloodLilyCount < 3
        };
    }
}
