using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Services.Action;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;

namespace Olympus.Services.Healing;

/// <summary>
/// Debug information about a candidate spell evaluation.
/// </summary>
public sealed record SpellCandidateDebug
{
    public string SpellName { get; init; } = "";
    public uint ActionId { get; init; }
    public int HealAmount { get; init; }
    public float Efficiency { get; init; }
    public float Score { get; init; }
    public string Bonuses { get; init; } = "";
    public bool WasSelected { get; init; }
    public string? RejectionReason { get; init; }
}

/// <summary>
/// Debug information about the last spell selection decision.
/// </summary>
public sealed class SpellSelectionDebug
{
    public DateTime Timestamp { get; init; } = DateTime.Now;
    public string SelectionType { get; init; } = ""; // "Single" or "AoE"
    public string TargetName { get; init; } = "";
    public int MissingHp { get; init; }
    public float TargetHpPercent { get; init; }
    public bool IsWeaveWindow { get; init; }
    public int LilyCount { get; init; }
    public int BloodLilyCount { get; init; }
    public string LilyStrategy { get; init; } = "";
    public List<SpellCandidateDebug> Candidates { get; init; } = new();
    public string? SelectedSpell { get; init; }
    public string? SelectionReason { get; init; }
    public float SecondsAgo => (float)(DateTime.Now - Timestamp).TotalSeconds;
}

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
/// Intelligent healing spell selector that evaluates all available heals
/// and returns the best option based on predicted healing and efficiency.
/// </summary>
public class HealingSpellSelector : IHealingSpellSelector
{
    private readonly IPlayerStatsService playerStatsService;
    private readonly IHpPredictionService hpPredictionService;
    private readonly ICombatEventService combatEventService;
    private readonly Configuration configuration;
    private readonly IErrorMetricsService? errorMetrics;
    private readonly SpellCandidateEvaluator evaluator;

    // Note: Assize is handled as a DPS oGCD in Apollo, not here
    // Note: Both single-target and AoE heals use tiered priority instead of scoring

    // Debug tracking for last selection
    private SpellSelectionDebug? lastSelection;

    /// <summary>
    /// Gets the last spell selection decision for debugging.
    /// </summary>
    public SpellSelectionDebug? LastSelection => lastSelection;

    public HealingSpellSelector(
        IActionService actionService,
        IPlayerStatsService playerStatsService,
        IHpPredictionService hpPredictionService,
        ICombatEventService combatEventService,
        Configuration configuration,
        IErrorMetricsService? errorMetrics = null)
        : this(actionService, playerStatsService, hpPredictionService, combatEventService, configuration,
               new SpellEnablementService(configuration), errorMetrics)
    {
    }

    public HealingSpellSelector(
        IActionService actionService,
        IPlayerStatsService playerStatsService,
        IHpPredictionService hpPredictionService,
        ICombatEventService combatEventService,
        Configuration configuration,
        ISpellEnablementService enablementService,
        IErrorMetricsService? errorMetrics = null)
    {
        this.playerStatsService = playerStatsService;
        this.hpPredictionService = hpPredictionService;
        this.combatEventService = combatEventService;
        this.configuration = configuration;
        this.errorMetrics = errorMetrics;
        this.evaluator = new SpellCandidateEvaluator(actionService, enablementService);
    }

    /// <summary>
    /// Selects the best single-target heal for a given target.
    /// Uses tiered priority: oGCDs > Lily heals > Regen > GCD heals
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="target">The target to heal.</param>
    /// <param name="isWeaveWindow">Whether we're in a valid oGCD weave window.</param>
    /// <param name="hasFreecure">Whether the player has the Freecure proc (prioritizes Cure II).</param>
    /// <param name="hasRegen">Whether the target already has Regen active.</param>
    /// <param name="regenRemaining">Remaining duration of Regen on target (0 if not present).</param>
    /// <returns>The best action and its heal amount, or null if no valid heal found.</returns>
    public (ActionDefinition? action, int healAmount) SelectBestSingleHeal(
        IPlayerCharacter player,
        IBattleChara target,
        bool isWeaveWindow,
        bool hasFreecure = false,
        bool hasRegen = false,
        float regenRemaining = 0f,
        bool isInMpConservationMode = false)
    {
        evaluator.ClearCandidates();
        var (mind, det, wd) = playerStatsService.GetHealingStats(player.Level);
        var missingHp = (int)(target.MaxHp - hpPredictionService.GetPredictedHp(target.EntityId, target.CurrentHp, target.MaxHp));
        var hpPercent = hpPredictionService.GetPredictedHpPercent(target.EntityId, target.CurrentHp, target.MaxHp);
        var lilyCount = GetLilyCount();
        var bloodLilyCount = GetBloodLilyCount();
        var lilyStrategy = configuration.Healing.LilyStrategy;

        // MP conservation affects spell selection
        var useMpConservation = isInMpConservationMode &&
                                 configuration.Healing.EnableMpAwareSpellSelection;

        // Route to scored selection if enabled
        if (configuration.Healing.EnableScoredHealSelection && missingHp > 0)
        {
            return SelectBestSingleHealScored(
                player, target, isWeaveWindow, hasFreecure, hasRegen, regenRemaining,
                useMpConservation, mind, det, wd, missingHp, hpPercent, lilyCount, bloodLilyCount);
        }

        // Skip if target doesn't need healing
        if (missingHp <= 0)
        {
            lastSelection = new SpellSelectionDebug
            {
                SelectionType = "Single",
                TargetName = target.Name.TextValue,
                MissingHp = missingHp,
                TargetHpPercent = hpPercent,
                IsWeaveWindow = isWeaveWindow,
                LilyCount = lilyCount,
                BloodLilyCount = bloodLilyCount,
                LilyStrategy = lilyStrategy.ToString(),
                Candidates = [],
                SelectedSpell = null,
                SelectionReason = "Target doesn't need healing (missingHp <= 0)"
            };
            return (null, 0);
        }

        ActionDefinition? selectedAction = null;
        int selectedHealAmount = 0;
        string selectionReason = "No valid candidates";

        // Note: Benediction and Tetragrammaton are now handled as oGCDs in Apollo.TryExecuteOgcd()
        // with proper priority ordering (emergency heals first, then regular oGCDs)

        // === TIER 1: Lily GCD (Afflatus Solace) ===
        // Blood Lily optimization: prefer lily heals based on strategy to generate Blood Lilies
        // MP conservation: aggressively prefer lily heals when MP is low
        var shouldUseLily = lilyCount > 0 &&
            (ShouldPreferLilyHeal(lilyCount, bloodLilyCount, hpPercent) ||
             (useMpConservation && configuration.Healing.PreferLiliesInConservationMode));

        if (shouldUseLily)
        {
            var result = evaluator.EvaluateSingleTarget(WHMActions.AfflatusSolace, player.Level, mind, det, wd, target);
            if (result.IsValid && result.Action is not null)
            {
                selectedAction = result.Action;
                selectedHealAmount = result.HealAmount;
                var mpNote = useMpConservation ? ", MP conservation" : "";
                selectionReason = $"Tier 1: Lily heal ({lilyCount} lilies, {bloodLilyCount}/3 Blood, {lilyStrategy}{mpNote})";
            }
        }
        else if (lilyCount == 0)
        {
            evaluator.TrackRejected(WHMActions.AfflatusSolace, 0, "No lilies available");
        }
        else
        {
            evaluator.TrackRejected(WHMActions.AfflatusSolace, 0,
                $"Strategy {lilyStrategy}: Blood {bloodLilyCount}/3, HP {hpPercent:P0}");
        }

        // === TIER 2: Regen (HoT) ===
        // Regen is the only spell with an HP threshold
        // Apply if target is below threshold HP and doesn't have Regen or it's about to expire
        // Also skip if pending heals will top off the target (Regen would overheal)
        if (selectedAction is null)
        {
            var targetBelowThreshold = hpPercent < FFXIVConstants.RegenHpThreshold;
            var needsRegen = (!hasRegen || regenRemaining < FFXIVConstants.RegenRefreshThreshold) && targetBelowThreshold;

            // Check if pending heals will bring target to 95%+ HP (Regen would mostly overheal)
            var pendingHeals = hpPredictionService.GetPendingHealAmount(target.EntityId);
            var projectedHpAfterPending = Math.Min(target.CurrentHp + pendingHeals, target.MaxHp);
            var projectedHpPercent = (float)projectedHpAfterPending / target.MaxHp;
            var regenWouldOverheal = projectedHpPercent >= 0.95f;

            if (needsRegen && regenWouldOverheal)
            {
                evaluator.TrackRejected(WHMActions.Regen, 0,
                    $"Pending heals ({pendingHeals} HP) will bring target to {projectedHpPercent:P0}");
            }
            else if (needsRegen)
            {
                var result = evaluator.EvaluateSingleTarget(WHMActions.Regen, player.Level, mind, det, wd, target);
                if (result.IsValid && result.Action is not null)
                {
                    selectedAction = result.Action;
                    selectedHealAmount = result.HealAmount;
                    selectionReason = hasRegen ? "Tier 2: Regen (refresh)" : "Tier 2: Regen";
                }
            }
            else if (!targetBelowThreshold)
            {
                evaluator.TrackRejected(WHMActions.Regen, 0, $"HP {hpPercent:P0} >= {FFXIVConstants.RegenHpThreshold:P0} threshold");
            }
            else
            {
                evaluator.TrackRejected(WHMActions.Regen, 0, $"Already has Regen ({regenRemaining:F1}s remaining)");
            }
        }

        // === TIER 3: Regular GCD (Cure II with Freecure, or Cure) ===
        // Apply overheal prevention to GCD heals to avoid wasting MP
        // MP conservation: prefer Cure over Cure II to save MP and fish for Freecure procs
        if (selectedAction is null)
        {
            // Prioritize Cure II if we have Freecure proc (always use free spells)
            if (hasFreecure)
            {
                var result = evaluator.EvaluateSingleTarget(WHMActions.CureII, player.Level, mind, det, wd, target, missingHp);
                if (result.IsValid && result.Action is not null)
                {
                    selectedAction = result.Action;
                    selectedHealAmount = result.HealAmount;
                    selectionReason = "Tier 3: Cure II (Freecure proc)";
                }
            }

            // In MP conservation mode, prefer Cure over Cure II (unless Freecure proc)
            var preferCureForConservation = useMpConservation &&
                                             configuration.Healing.PreferCureInConservationMode &&
                                             !hasFreecure;

            if (preferCureForConservation && selectedAction is null)
            {
                // Try Cure first (lower MP cost, may proc Freecure)
                var result = evaluator.EvaluateSingleTarget(WHMActions.Cure, player.Level, mind, det, wd, target, missingHp);
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
                var result = evaluator.EvaluateSingleTarget(WHMActions.CureII, player.Level, mind, det, wd, target, missingHp);
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
                var result = evaluator.EvaluateSingleTarget(fallbackAction, player.Level, mind, det, wd, target, missingHp);
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

        // Mark selected action and build debug info
        if (selectedAction is not null)
        {
            evaluator.MarkAsSelected(selectedAction.ActionId);
        }

        lastSelection = new SpellSelectionDebug
        {
            SelectionType = "Single",
            TargetName = target.Name.TextValue,
            MissingHp = missingHp,
            TargetHpPercent = hpPercent,
            IsWeaveWindow = isWeaveWindow,
            LilyCount = lilyCount,
            BloodLilyCount = bloodLilyCount,
            LilyStrategy = lilyStrategy.ToString(),
            Candidates = evaluator.GetCandidatesCopy(),
            SelectedSpell = selectedAction?.Name,
            SelectionReason = selectionReason
        };

        return (selectedAction, selectedHealAmount);
    }

    /// <summary>
    /// Selects the best AoE heal when multiple party members need healing.
    /// Uses tiered priority: Lily heals > Cure III (stacked) > HoT heals > Basic heals
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="averageMissingHp">Average missing HP across injured targets.</param>
    /// <param name="injuredCount">Number of injured party members (for self-centered AoE).</param>
    /// <param name="anyHaveRegen">Whether any targets already have a Medica regen.</param>
    /// <param name="isWeaveWindow">Whether we're in a valid oGCD weave window.</param>
    /// <param name="cureIIITargetCount">Number of targets within Cure III radius of best target (0 to skip).</param>
    /// <param name="cureIIITarget">The best target for Cure III (party member with most injured allies nearby).</param>
    /// <returns>The best action, heal amount, and target for Cure III (null for self-centered heals).</returns>
    public (ActionDefinition? action, int healAmount, IBattleChara? cureIIITarget) SelectBestAoEHeal(
        IPlayerCharacter player,
        int averageMissingHp,
        int injuredCount,
        bool anyHaveRegen,
        bool isWeaveWindow,
        int cureIIITargetCount = 0,
        IBattleChara? cureIIITarget = null,
        bool isInMpConservationMode = false)
    {
        evaluator.ClearCandidates();
        var (mind, det, wd) = playerStatsService.GetHealingStats(player.Level);
        var lilyCount = GetLilyCount();
        var bloodLilyCount = GetBloodLilyCount();
        var lilyStrategy = configuration.Healing.LilyStrategy;

        // MP conservation affects spell selection
        var useMpConservation = isInMpConservationMode &&
                                 configuration.Healing.EnableMpAwareSpellSelection;

        // Skip if not enough targets (check both self-centered and Cure III targets)
        var hasSelfCenteredTargets = injuredCount >= configuration.Healing.AoEHealMinTargets;
        var hasCureIIITargets = cureIIITargetCount >= configuration.Healing.AoEHealMinTargets;

        if (!hasSelfCenteredTargets && !hasCureIIITargets)
        {
            lastSelection = new SpellSelectionDebug
            {
                SelectionType = "AoE",
                TargetName = $"{injuredCount} injured",
                MissingHp = averageMissingHp,
                TargetHpPercent = 0,
                IsWeaveWindow = isWeaveWindow,
                LilyCount = lilyCount,
                BloodLilyCount = bloodLilyCount,
                LilyStrategy = lilyStrategy.ToString(),
                Candidates = [],
                SelectedSpell = null,
                SelectionReason = $"Not enough targets (self:{injuredCount}, CureIII:{cureIIITargetCount} < {configuration.Healing.AoEHealMinTargets} min)"
            };
            return (null, 0, null);
        }

        // Note: Assize is handled as a DPS oGCD in Apollo.TryExecuteOgcd, not here.
        // It's used on cooldown for damage, with healing as a bonus.

        ActionDefinition? selectedAction = null;
        int selectedHealAmount = 0;
        string selectionReason = "No valid candidates";
        IBattleChara? selectedCureIIITarget = null; // Track if Cure III was selected

        // === TIER 1: Lily AoE (Afflatus Rapture) ===
        // Blood Lily optimization: prefer lily heals based on strategy to generate Blood Lilies
        // MP conservation: aggressively prefer lily heals when MP is low
        // For AoE heals, we use 0 for HP percent since we're healing multiple targets
        var shouldUseLilyAoE = lilyCount > 0 && hasSelfCenteredTargets &&
            (ShouldPreferLilyHeal(lilyCount, bloodLilyCount, 0f) ||
             (useMpConservation && configuration.Healing.PreferLiliesInConservationMode));

        if (shouldUseLilyAoE)
        {
            var result = evaluator.EvaluateAoE(WHMActions.AfflatusRapture, player.Level, mind, det, wd);
            if (result.IsValid && result.Action is not null)
            {
                selectedAction = result.Action;
                selectedHealAmount = result.HealAmount;
                var mpNote = useMpConservation ? ", MP conservation" : "";
                selectionReason = $"Tier 1: Lily AoE heal ({lilyCount} lilies, {bloodLilyCount}/3 Blood, {lilyStrategy}{mpNote})";
            }
        }
        else if (lilyCount == 0)
        {
            evaluator.TrackRejected(WHMActions.AfflatusRapture, 0, "No lilies available");
        }
        else if (!hasSelfCenteredTargets)
        {
            evaluator.TrackRejected(WHMActions.AfflatusRapture, 0, $"Not enough self-centered targets ({injuredCount})");
        }
        else
        {
            evaluator.TrackRejected(WHMActions.AfflatusRapture, 0,
                $"Strategy {lilyStrategy}: Blood {bloodLilyCount}/3");
        }

        // === TIER 1.5: Cure III (targeted AoE, when party is stacked) ===
        // Higher potency (600) than Medica (400), but requires stacked party within 10y
        if (selectedAction is null && hasCureIIITargets && cureIIITarget is not null)
        {
            var result = evaluator.EvaluateAoE(WHMActions.CureIII, player.Level, mind, det, wd);
            if (result.IsValid && result.Action is not null)
            {
                selectedAction = result.Action;
                selectedHealAmount = result.HealAmount;
                selectedCureIIITarget = cureIIITarget;
                selectionReason = $"Tier 1.5: Cure III ({cureIIITargetCount} stacked around {cureIIITarget.Name})";
            }
        }
        else if (selectedAction is null && !hasCureIIITargets)
        {
            evaluator.TrackRejected(WHMActions.CureIII, 0, $"Not enough stacked targets ({cureIIITargetCount} < {configuration.Healing.AoEHealMinTargets})");
        }
        else if (selectedAction is null && cureIIITarget is null)
        {
            evaluator.TrackRejected(WHMActions.CureIII, 0, "No valid Cure III target");
        }

        // === TIER 2: Medica III (HoT, highest potency) ===
        if (selectedAction is null && hasSelfCenteredTargets)
        {
            if (anyHaveRegen)
            {
                evaluator.TrackRejected(WHMActions.MedicaIII, 0, "Would overwrite existing regen");
            }
            else
            {
                var result = evaluator.EvaluateAoE(WHMActions.MedicaIII, player.Level, mind, det, wd);
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
            if (anyHaveRegen)
            {
                evaluator.TrackRejected(WHMActions.MedicaII, 0, "Would overwrite existing regen");
            }
            else
            {
                var result = evaluator.EvaluateAoE(WHMActions.MedicaII, player.Level, mind, det, wd);
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
            var result = evaluator.EvaluateAoE(WHMActions.Medica, player.Level, mind, det, wd);
            if (result.IsValid && result.Action is not null)
            {
                selectedAction = result.Action;
                selectedHealAmount = result.HealAmount;
                selectionReason = "Tier 4: Medica (fallback)";
            }
        }

        // Mark selected action and build debug info
        if (selectedAction is not null)
        {
            evaluator.MarkAsSelected(selectedAction.ActionId);
        }

        lastSelection = new SpellSelectionDebug
        {
            SelectionType = "AoE",
            TargetName = selectedCureIIITarget is not null
                ? $"{cureIIITargetCount} stacked around {selectedCureIIITarget.Name}"
                : $"{injuredCount} injured",
            MissingHp = averageMissingHp,
            TargetHpPercent = 0,
            IsWeaveWindow = isWeaveWindow,
            LilyCount = lilyCount,
            BloodLilyCount = bloodLilyCount,
            LilyStrategy = lilyStrategy.ToString(),
            Candidates = evaluator.GetCandidatesCopy(),
            SelectedSpell = selectedAction?.Name,
            SelectionReason = selectionReason
        };

        return (selectedAction, selectedHealAmount, selectedCureIIITarget);
    }

    /// <summary>
    /// Gets the current Lily count from the WHM job gauge.
    /// Virtual to allow testing with mocked lily counts.
    /// </summary>
    protected virtual int GetLilyCount()
    {
        return SafeGameAccess.GetWhmLilyCount(errorMetrics);
    }

    /// <summary>
    /// Gets the current Blood Lily count from the WHM job gauge.
    /// Virtual to allow testing with mocked Blood Lily counts.
    /// </summary>
    protected virtual int GetBloodLilyCount()
    {
        return SafeGameAccess.GetWhmBloodLilyCount(errorMetrics);
    }

    /// <summary>
    /// Determines whether lily heals (Afflatus Solace/Rapture) should be preferred
    /// over MP-based alternatives based on the configured Blood Lily strategy.
    /// Now considers combat duration for smarter lily flushing.
    /// </summary>
    /// <param name="lilyCount">Current lily count (0-3).</param>
    /// <param name="bloodLilyCount">Current Blood Lily count (0-3).</param>
    /// <param name="targetHpPercent">Target HP percent (for Conservative strategy).</param>
    /// <returns>True if lily heals should be preferred.</returns>
    private bool ShouldPreferLilyHeal(int lilyCount, int bloodLilyCount, float targetHpPercent)
    {
        if (lilyCount == 0)
            return false;

        // Get combat duration for time-aware lily flushing
        var combatDuration = combatEventService.GetCombatDurationSeconds();

        // Time-aware lily flush: If combat has been going for a while and we have
        // blood lilies ready, prefer lily heals to ensure we get Afflatus Misery off.
        // Lily regenerates every 20 seconds, so after ~60s we should be spending freely.
        if (combatDuration > 60f && bloodLilyCount >= 2 && lilyCount > 0)
            return true;

        // If we have full blood lilies (3/3) and lilies available, we should spend
        // lilies even though blood lily won't increase (prevents lily waste)
        if (bloodLilyCount >= 3 && lilyCount > 0)
            return true;

        // Aggressive lily flush: when at 2 blood lilies, always prefer lily heals
        // to build the third blood lily for Afflatus Misery before combat ends
        if (configuration.Healing.EnableAggressiveLilyFlush && bloodLilyCount >= 2)
            return true;

        return configuration.Healing.LilyStrategy switch
        {
            LilyGenerationStrategy.Aggressive => true,
            LilyGenerationStrategy.Balanced => bloodLilyCount < 3,
            LilyGenerationStrategy.Conservative => bloodLilyCount < 3 &&
                targetHpPercent < configuration.Healing.ConservativeLilyHpThreshold,
            LilyGenerationStrategy.Disabled => false,
            _ => bloodLilyCount < 3 // Default to Balanced behavior
        };
    }

    /// <summary>
    /// Calculates a score for a heal based on multiple factors.
    /// Higher scores indicate better heal choices.
    /// </summary>
    /// <param name="context">The context containing all scoring factors.</param>
    /// <returns>A score from 0.0 to 1.0 where higher is better.</returns>
    private float CalculateHealScore(HealScoreContext context)
    {
        var weights = configuration.Healing.ScoreWeights;
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
    /// Selects the best single-target heal using the scored selection system.
    /// Evaluates all valid heals and returns the highest-scoring option.
    /// </summary>
    private (ActionDefinition? action, int healAmount) SelectBestSingleHealScored(
        IPlayerCharacter player,
        IBattleChara target,
        bool isWeaveWindow,
        bool hasFreecure,
        bool hasRegen,
        float regenRemaining,
        bool isInMpConservationMode,
        int mind, int det, int wd,
        int missingHp,
        float hpPercent,
        int lilyCount,
        int bloodLilyCount)
    {
        var candidates = new List<(ActionDefinition action, int healAmount, float score, string reason)>();

        // Evaluate Afflatus Solace (Lily heal)
        if (lilyCount > 0)
        {
            var result = evaluator.EvaluateSingleTarget(WHMActions.AfflatusSolace, player.Level, mind, det, wd, target);
            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateHealScore(new HealScoreContext
                {
                    Action = result.Action,
                    HealAmount = result.HealAmount,
                    MissingHp = missingHp,
                    HasFreecure = hasFreecure,
                    IsWeaveWindow = isWeaveWindow,
                    LilyCount = lilyCount,
                    BloodLilyCount = bloodLilyCount,
                    IsInMpConservationMode = isInMpConservationMode
                });
                candidates.Add((result.Action, result.HealAmount, score, $"Lily heal (score: {score:F2})"));
            }
        }

        // Evaluate Regen (if needed)
        var needsRegen = (!hasRegen || regenRemaining < FFXIVConstants.RegenRefreshThreshold) &&
                         hpPercent < FFXIVConstants.RegenHpThreshold;
        if (needsRegen)
        {
            var result = evaluator.EvaluateSingleTarget(WHMActions.Regen, player.Level, mind, det, wd, target);
            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateHealScore(new HealScoreContext
                {
                    Action = result.Action,
                    HealAmount = result.HealAmount,
                    MissingHp = missingHp,
                    HasFreecure = hasFreecure,
                    IsWeaveWindow = isWeaveWindow,
                    LilyCount = lilyCount,
                    BloodLilyCount = bloodLilyCount,
                    IsInMpConservationMode = isInMpConservationMode
                });
                candidates.Add((result.Action, result.HealAmount, score, $"Regen (score: {score:F2})"));
            }
        }

        // Evaluate Cure II
        {
            var result = evaluator.EvaluateSingleTarget(WHMActions.CureII, player.Level, mind, det, wd, target, missingHp);
            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateHealScore(new HealScoreContext
                {
                    Action = result.Action,
                    HealAmount = result.HealAmount,
                    MissingHp = missingHp,
                    HasFreecure = hasFreecure,
                    IsWeaveWindow = isWeaveWindow,
                    LilyCount = lilyCount,
                    BloodLilyCount = bloodLilyCount,
                    IsInMpConservationMode = isInMpConservationMode
                });
                var freecureNote = hasFreecure ? " (Freecure!)" : "";
                candidates.Add((result.Action, result.HealAmount, score, $"Cure II{freecureNote} (score: {score:F2})"));
            }
        }

        // Evaluate Cure
        {
            var result = evaluator.EvaluateSingleTarget(WHMActions.Cure, player.Level, mind, det, wd, target, missingHp);
            if (result.IsValid && result.Action is not null)
            {
                var score = CalculateHealScore(new HealScoreContext
                {
                    Action = result.Action,
                    HealAmount = result.HealAmount,
                    MissingHp = missingHp,
                    HasFreecure = hasFreecure,
                    IsWeaveWindow = isWeaveWindow,
                    LilyCount = lilyCount,
                    BloodLilyCount = bloodLilyCount,
                    IsInMpConservationMode = isInMpConservationMode
                });
                candidates.Add((result.Action, result.HealAmount, score, $"Cure (score: {score:F2})"));
            }
        }

        // Select highest scoring candidate
        if (candidates.Count == 0)
            return (null, 0);

        var best = candidates.OrderByDescending(c => c.score).First();

        // Track all candidates for debug info
        foreach (var candidate in candidates)
        {
            if (candidate.action.ActionId == best.action.ActionId)
                evaluator.MarkAsSelected(candidate.action.ActionId);
        }

        lastSelection = new SpellSelectionDebug
        {
            SelectionType = "Single (Scored)",
            TargetName = target.Name.TextValue,
            MissingHp = missingHp,
            TargetHpPercent = hpPercent,
            IsWeaveWindow = isWeaveWindow,
            LilyCount = lilyCount,
            BloodLilyCount = bloodLilyCount,
            LilyStrategy = configuration.Healing.LilyStrategy.ToString(),
            Candidates = evaluator.GetCandidatesCopy(),
            SelectedSpell = best.action.Name,
            SelectionReason = best.reason
        };

        return (best.action, best.healAmount);
    }

    /// <summary>
    /// Debug info showing current spell selection state.
    /// </summary>
    public string GetDebugInfo(IPlayerCharacter player)
    {
        var lilyCount = GetLilyCount();
        var bloodLilyCount = GetBloodLilyCount();
        var selectionMode = configuration.Healing.EnableScoredHealSelection ? "Scored" : "Tier-Based";
        return $"Lilies: {lilyCount}/3 | Blood: {bloodLilyCount}/3 | Strategy: {configuration.Healing.LilyStrategy} | Selection: {selectionMode} | BeneThreshold: {configuration.Healing.BenedictionEmergencyThreshold:P0}";
    }
}
