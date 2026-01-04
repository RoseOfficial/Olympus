using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using FFXIVClientStructs.FFXIV.Client.Game;
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
    public List<SpellCandidateDebug> Candidates { get; init; } = new();
    public string? SelectedSpell { get; init; }
    public string? SelectionReason { get; init; }
    public float SecondsAgo => (float)(DateTime.Now - Timestamp).TotalSeconds;
}

/// <summary>
/// Intelligent healing spell selector that evaluates all available heals
/// and returns the best option based on predicted healing and efficiency.
/// </summary>
public class HealingSpellSelector
{
    private readonly IActionService actionService;
    private readonly IPlayerStatsService playerStatsService;
    private readonly IHpPredictionService hpPredictionService;
    private readonly Configuration configuration;

    // Note: Assize is handled as a DPS oGCD in Apollo, not here
    // Note: Both single-target and AoE heals use tiered priority instead of scoring

    // Debug tracking for last selection
    private SpellSelectionDebug? lastSelection;
    private readonly List<SpellCandidateDebug> currentCandidates = new();

    /// <summary>
    /// Gets the last spell selection decision for debugging.
    /// </summary>
    public SpellSelectionDebug? LastSelection => lastSelection;

    public HealingSpellSelector(
        IActionService actionService,
        IPlayerStatsService playerStatsService,
        IHpPredictionService hpPredictionService,
        Configuration configuration)
    {
        this.actionService = actionService;
        this.playerStatsService = playerStatsService;
        this.hpPredictionService = hpPredictionService;
        this.configuration = configuration;
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
        float regenRemaining = 0f)
    {
        currentCandidates.Clear();
        var (mind, det, wd) = playerStatsService.GetHealingStats(player.Level);
        var missingHp = (int)(target.MaxHp - hpPredictionService.GetPredictedHp(target.EntityId, target.CurrentHp, target.MaxHp));
        var hpPercent = hpPredictionService.GetPredictedHpPercent(target.EntityId, target.CurrentHp, target.MaxHp);
        var lilyCount = GetLilyCount();

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
                Candidates = new List<SpellCandidateDebug>(),
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
        if (lilyCount > 0)
        {
            var result = TrySelectHeal(WHMActions.AfflatusSolace, player.Level, mind, det, wd, target);
            if (result.action != null)
            {
                selectedAction = result.action;
                selectedHealAmount = result.healAmount;
                selectionReason = $"Tier 1: Lily heal ({lilyCount} lilies)";
            }
        }
        else
        {
            TrackRejectedSpell(WHMActions.AfflatusSolace, 0, 0, "No lilies available");
        }

        // === TIER 2: Regen (HoT) ===
        // Regen is the only spell with an HP threshold (90%)
        // Apply if target is below 90% HP and doesn't have Regen or it's about to expire
        if (selectedAction == null)
        {
            const float RegenHpThreshold = 0.90f;
            var targetBelowThreshold = hpPercent < RegenHpThreshold;
            var needsRegen = (!hasRegen || regenRemaining < 3f) && targetBelowThreshold;

            if (needsRegen)
            {
                var result = TrySelectHeal(WHMActions.Regen, player.Level, mind, det, wd, target);
                if (result.action != null)
                {
                    selectedAction = result.action;
                    selectedHealAmount = result.healAmount;
                    selectionReason = hasRegen ? "Tier 2: Regen (refresh)" : "Tier 2: Regen";
                }
            }
            else if (!targetBelowThreshold)
            {
                TrackRejectedSpell(WHMActions.Regen, 0, 0, $"HP {hpPercent:P0} >= 90% threshold");
            }
            else
            {
                TrackRejectedSpell(WHMActions.Regen, 0, 0, $"Already has Regen ({regenRemaining:F1}s remaining)");
            }
        }

        // === TIER 3: Regular GCD (Cure II with Freecure, or Cure) ===
        // Apply overheal prevention to GCD heals to avoid wasting MP
        if (selectedAction == null)
        {
            // Prioritize Cure II if we have Freecure proc
            if (hasFreecure)
            {
                var result = TrySelectHeal(WHMActions.CureII, player.Level, mind, det, wd, target, missingHp);
                if (result.action != null)
                {
                    selectedAction = result.action;
                    selectedHealAmount = result.healAmount;
                    selectionReason = "Tier 3: Cure II (Freecure proc)";
                }
            }

            // Try Cure II without Freecure
            if (selectedAction == null)
            {
                var result = TrySelectHeal(WHMActions.CureII, player.Level, mind, det, wd, target, missingHp);
                if (result.action != null)
                {
                    selectedAction = result.action;
                    selectedHealAmount = result.healAmount;
                    selectionReason = "Tier 3: Cure II";
                }
            }

            // Fallback to Cure
            if (selectedAction == null)
            {
                var result = TrySelectHeal(WHMActions.Cure, player.Level, mind, det, wd, target, missingHp);
                if (result.action != null)
                {
                    selectedAction = result.action;
                    selectedHealAmount = result.healAmount;
                    selectionReason = "Tier 3: Cure (fallback)";
                }
            }
        }

        // Build debug info
        var debugCandidates = new List<SpellCandidateDebug>(currentCandidates);
        if (selectedAction != null)
        {
            for (int i = 0; i < debugCandidates.Count; i++)
            {
                if (debugCandidates[i].ActionId == selectedAction.ActionId)
                {
                    debugCandidates[i] = debugCandidates[i] with { WasSelected = true };
                    break;
                }
            }
        }

        lastSelection = new SpellSelectionDebug
        {
            SelectionType = "Single",
            TargetName = target.Name.TextValue,
            MissingHp = missingHp,
            TargetHpPercent = hpPercent,
            IsWeaveWindow = isWeaveWindow,
            LilyCount = lilyCount,
            Candidates = debugCandidates,
            SelectedSpell = selectedAction?.Name,
            SelectionReason = selectionReason
        };

        return (selectedAction, selectedHealAmount);
    }

    /// <summary>
    /// Attempts to select a heal spell if it meets all requirements.
    /// Returns the action and heal amount if valid, or (null, 0) if not.
    /// </summary>
    /// <param name="action">The heal action to evaluate.</param>
    /// <param name="playerLevel">The player's current level.</param>
    /// <param name="mind">The player's Mind stat.</param>
    /// <param name="det">The player's Determination stat.</param>
    /// <param name="wd">The player's weapon damage.</param>
    /// <param name="target">The target to heal.</param>
    /// <param name="missingHp">The target's missing HP for overheal prevention. Use 0 to skip overheal check.</param>
    private (ActionDefinition? action, int healAmount) TrySelectHeal(
        ActionDefinition action,
        byte playerLevel,
        int mind, int det, int wd,
        IBattleChara target,
        int missingHp = 0)
    {
        // Check level
        if (playerLevel < action.MinLevel)
        {
            TrackRejectedSpell(action, 0, 0, $"Level too low ({playerLevel} < {action.MinLevel})");
            return (null, 0);
        }

        // Check config enabled
        if (!IsSpellEnabled(action))
        {
            TrackRejectedSpell(action, 0, 0, "Disabled in config");
            return (null, 0);
        }

        // Check cooldown
        if (!actionService.IsActionReady(action.ActionId))
        {
            TrackRejectedSpell(action, 0, 0, "On cooldown");
            return (null, 0);
        }

        // Calculate heal amount
        var healAmount = action.EstimateHealAmount(mind, det, wd, playerLevel);

        // Check for overheal (only for potency-based heals, not Benediction)
        if (missingHp > 0 && action.HealPotency > 0 && healAmount > missingHp)
        {
            TrackRejectedSpell(action, healAmount, 0, $"Would overheal ({healAmount} > {missingHp} missing)");
            return (null, 0);
        }

        // Track as valid candidate
        currentCandidates.Add(new SpellCandidateDebug
        {
            SpellName = action.Name,
            ActionId = action.ActionId,
            HealAmount = healAmount,
            Efficiency = 1.0f,
            Score = 1.0f,
            Bonuses = "Tiered priority",
            WasSelected = false,
            RejectionReason = null
        });

        return (action, healAmount);
    }

    private void TrackRejectedSpell(ActionDefinition action, int healAmount, float efficiency, string reason)
    {
        currentCandidates.Add(new SpellCandidateDebug
        {
            SpellName = action.Name,
            ActionId = action.ActionId,
            HealAmount = healAmount,
            Efficiency = efficiency,
            Score = 0,
            Bonuses = "",
            WasSelected = false,
            RejectionReason = reason
        });
    }

    private bool IsSpellEnabled(ActionDefinition action)
    {
        return action.ActionId switch
        {
            120 => configuration.EnableCure,           // Cure
            135 => configuration.EnableCureII,         // Cure II
            131 => configuration.EnableCureIII,        // Cure III
            137 => configuration.EnableRegen,          // Regen
            16531 => configuration.EnableAfflatusSolace, // Afflatus Solace
            3570 => configuration.EnableTetragrammaton, // Tetragrammaton
            140 => configuration.EnableBenediction,    // Benediction
            124 => configuration.EnableMedica,         // Medica
            133 => configuration.EnableMedicaII,       // Medica II
            37010 => configuration.EnableMedicaIII,    // Medica III
            16534 => configuration.EnableAfflatusRapture, // Afflatus Rapture
            3571 => configuration.EnableAssize,        // Assize
            3569 => configuration.EnableAsylum,        // Asylum
            16535 => configuration.EnableAfflatusMisery, // Afflatus Misery
            7432 => configuration.EnableDivineBenison,   // Divine Benison
            7433 => configuration.EnablePlenaryIndulgence, // Plenary Indulgence
            16536 => configuration.EnableTemperance,     // Temperance
            25861 => configuration.EnableAquaveil,       // Aquaveil
            25862 => configuration.EnableLiturgyOfTheBell, // Liturgy of the Bell
            37011 => configuration.EnableDivineCaress,   // Divine Caress
            _ => true
        };
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
        IBattleChara? cureIIITarget = null)
    {
        currentCandidates.Clear();
        var (mind, det, wd) = playerStatsService.GetHealingStats(player.Level);
        var lilyCount = GetLilyCount();

        // Skip if not enough targets (check both self-centered and Cure III targets)
        var hasSelfCenteredTargets = injuredCount >= configuration.AoEHealMinTargets;
        var hasCureIIITargets = cureIIITargetCount >= configuration.AoEHealMinTargets;

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
                Candidates = new List<SpellCandidateDebug>(),
                SelectedSpell = null,
                SelectionReason = $"Not enough targets (self:{injuredCount}, CureIII:{cureIIITargetCount} < {configuration.AoEHealMinTargets} min)"
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
        if (lilyCount > 0 && hasSelfCenteredTargets)
        {
            var result = TrySelectAoEHeal(WHMActions.AfflatusRapture, player.Level, mind, det, wd);
            if (result.action != null)
            {
                selectedAction = result.action;
                selectedHealAmount = result.healAmount;
                selectionReason = $"Tier 1: Lily AoE heal ({lilyCount} lilies)";
            }
        }
        else if (lilyCount == 0)
        {
            TrackRejectedSpell(WHMActions.AfflatusRapture, 0, 0, "No lilies available");
        }
        else if (!hasSelfCenteredTargets)
        {
            TrackRejectedSpell(WHMActions.AfflatusRapture, 0, 0, $"Not enough self-centered targets ({injuredCount})");
        }

        // === TIER 1.5: Cure III (targeted AoE, when party is stacked) ===
        // Higher potency (600) than Medica (400), but requires stacked party within 10y
        if (selectedAction == null && hasCureIIITargets && cureIIITarget != null)
        {
            var result = TrySelectAoEHeal(WHMActions.CureIII, player.Level, mind, det, wd);
            if (result.action != null)
            {
                selectedAction = result.action;
                selectedHealAmount = result.healAmount;
                selectedCureIIITarget = cureIIITarget;
                selectionReason = $"Tier 1.5: Cure III ({cureIIITargetCount} stacked around {cureIIITarget.Name})";
            }
        }
        else if (selectedAction == null && !hasCureIIITargets)
        {
            TrackRejectedSpell(WHMActions.CureIII, 0, 0, $"Not enough stacked targets ({cureIIITargetCount} < {configuration.AoEHealMinTargets})");
        }
        else if (selectedAction == null && cureIIITarget == null)
        {
            TrackRejectedSpell(WHMActions.CureIII, 0, 0, "No valid Cure III target");
        }

        // === TIER 2: Medica III (HoT, highest potency) ===
        if (selectedAction == null && hasSelfCenteredTargets)
        {
            if (anyHaveRegen)
            {
                TrackRejectedSpell(WHMActions.MedicaIII, 0, 0, "Would overwrite existing regen");
            }
            else
            {
                var result = TrySelectAoEHeal(WHMActions.MedicaIII, player.Level, mind, det, wd);
                if (result.action != null)
                {
                    selectedAction = result.action;
                    selectedHealAmount = result.healAmount;
                    selectionReason = "Tier 2: Medica III (HoT)";
                }
            }
        }

        // === TIER 3: Medica II (HoT) ===
        if (selectedAction == null && hasSelfCenteredTargets)
        {
            if (anyHaveRegen)
            {
                TrackRejectedSpell(WHMActions.MedicaII, 0, 0, "Would overwrite existing regen");
            }
            else
            {
                var result = TrySelectAoEHeal(WHMActions.MedicaII, player.Level, mind, det, wd);
                if (result.action != null)
                {
                    selectedAction = result.action;
                    selectedHealAmount = result.healAmount;
                    selectionReason = "Tier 3: Medica II (HoT)";
                }
            }
        }

        // === TIER 4: Medica (fallback) ===
        if (selectedAction == null && hasSelfCenteredTargets)
        {
            var result = TrySelectAoEHeal(WHMActions.Medica, player.Level, mind, det, wd);
            if (result.action != null)
            {
                selectedAction = result.action;
                selectedHealAmount = result.healAmount;
                selectionReason = "Tier 4: Medica (fallback)";
            }
        }

        // Build debug info
        var debugCandidates = new List<SpellCandidateDebug>(currentCandidates);
        if (selectedAction != null)
        {
            for (int i = 0; i < debugCandidates.Count; i++)
            {
                if (debugCandidates[i].ActionId == selectedAction.ActionId)
                {
                    debugCandidates[i] = debugCandidates[i] with { WasSelected = true };
                    break;
                }
            }
        }

        lastSelection = new SpellSelectionDebug
        {
            SelectionType = "AoE",
            TargetName = selectedCureIIITarget != null
                ? $"{cureIIITargetCount} stacked around {selectedCureIIITarget.Name}"
                : $"{injuredCount} injured",
            MissingHp = averageMissingHp,
            TargetHpPercent = 0,
            IsWeaveWindow = isWeaveWindow,
            LilyCount = lilyCount,
            Candidates = debugCandidates,
            SelectedSpell = selectedAction?.Name,
            SelectionReason = selectionReason
        };

        return (selectedAction, selectedHealAmount, selectedCureIIITarget);
    }

    /// <summary>
    /// Attempts to select an AoE heal spell if it meets all requirements.
    /// Returns the action and heal amount if valid, or (null, 0) if not.
    /// </summary>
    private (ActionDefinition? action, int healAmount) TrySelectAoEHeal(
        ActionDefinition action,
        byte playerLevel,
        int mind, int det, int wd)
    {
        // Check level
        if (playerLevel < action.MinLevel)
        {
            TrackRejectedSpell(action, 0, 0, $"Level too low ({playerLevel} < {action.MinLevel})");
            return (null, 0);
        }

        // Check config enabled
        if (!IsSpellEnabled(action))
        {
            TrackRejectedSpell(action, 0, 0, "Disabled in config");
            return (null, 0);
        }

        // Check cooldown
        if (!actionService.IsActionReady(action.ActionId))
        {
            TrackRejectedSpell(action, 0, 0, "On cooldown");
            return (null, 0);
        }

        // Calculate heal amount
        var healAmount = action.EstimateHealAmount(mind, det, wd, playerLevel);

        // Track as valid candidate
        currentCandidates.Add(new SpellCandidateDebug
        {
            SpellName = action.Name,
            ActionId = action.ActionId,
            HealAmount = healAmount,
            Efficiency = 1.0f,
            Score = 1.0f,
            Bonuses = "Tiered priority",
            WasSelected = false,
            RejectionReason = null
        });

        return (action, healAmount);
    }

    /// <summary>
    /// Gets the current Lily count from the WHM job gauge.
    /// Virtual to allow testing with mocked lily counts.
    /// </summary>
    protected virtual unsafe int GetLilyCount()
    {
        try
        {
            var jobGauge = JobGaugeManager.Instance();
            if (jobGauge == null)
                return 0;

            var whmGauge = jobGauge->WhiteMage;
            return whmGauge.Lily;
        }
        catch
        {
            return 0;
        }
    }

    /// <summary>
    /// Debug info showing current spell selection state.
    /// </summary>
    public string GetDebugInfo(IPlayerCharacter player)
    {
        var lilyCount = GetLilyCount();
        return $"Lilies: {lilyCount}/3 | BeneThreshold: {configuration.BenedictionEmergencyThreshold:P0}";
    }
}
