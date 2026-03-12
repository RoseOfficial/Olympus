using System;
using System.Collections.Generic;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles single-target GCD healing (Cure, Cure II, etc.).
/// Uses triage to select the best target and HealingSpellSelector for optimal spell.
/// </summary>
public sealed class SingleTargetHealingHandler : IHealingHandler
{
    private const byte CureMinLevel = 2;

    public HealingPriority Priority => HealingPriority.SingleHeal;
    public string Name => "SingleTargetHeal";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing)
            return false;

        if (player.Level < CureMinLevel)
            return false;

        // Use damage intake triage if enabled, otherwise fall back to lowest HP
        // Pass ShieldTrackingService for shield-aware triage scoring
        var target = config.Healing.UseDamageIntakeTriage
            ? context.PartyHelper.FindMostEndangeredPartyMember(
                player, context.DamageIntakeService, 0, context.DamageTrendService, context.ShieldTrackingService)
            : context.PartyHelper.FindLowestHpPartyMember(player);

        if (target is null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        // Co-healer awareness: Skip if co-healer has pending heal covering most of missing HP
        if (config.Healing.EnableCoHealerAwareness && context.CoHealerDetectionService?.HasCoHealer == true)
        {
            var coHealerPendingHeals = context.CoHealerDetectionService.CoHealerPendingHeals;
            if (coHealerPendingHeals.TryGetValue(target.EntityId, out var pendingHeal))
            {
                var missingHp = target.MaxHp - target.CurrentHp;
                var pendingHealPercent = missingHp > 0 ? (float)pendingHeal / missingHp : 1f;

                // Skip if co-healer's pending heal covers enough of the missing HP
                if (pendingHealPercent >= config.Healing.CoHealerPendingHealThreshold)
                    return false;
            }
        }

        var hasRegen = StatusHelper.HasRegenActive(target, out var regenRemaining);
        var isInMpConservation = context.MpForecastService.IsInConservationMode;

        var (action, healAmount) = context.HealingSpellSelector.SelectBestSingleHeal(
            player, target, context.CanExecuteOgcd, context.HasFreecure, hasRegen, regenRemaining, isInMpConservation);
        if (action is null)
            return false;

        if (isMoving && action.CastTime > 0)
            return false;

        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
        var healAmountRaw = action.EstimateHealAmountRaw(mind, det, wd, player.Level);
        context.Debug.LastHealAmount = healAmount;
        context.Debug.LastHealStats = $"MND:{mind} DET:{det} WD:{wd} Lv:{player.Level} Pot:{action.HealPotency}";
        context.HpPredictionService.RegisterPendingHeal(target.EntityId, healAmount);

        if (action.HealPotency > 0)
            context.CombatEventService.RegisterPredictionForCalibration(healAmountRaw);

        bool success;
        if (action.Category == ActionCategory.oGCD)
        {
            success = context.ActionService.ExecuteOgcd(action, target.GameObjectId);
        }
        else
        {
            if (action.MpCost >= 1000 && ThinAirHelper.ShouldWaitForThinAir(context))
            {
                context.HpPredictionService.ClearPendingHeals();
                return false;
            }

            success = context.ActionService.ExecuteGcd(action, target.GameObjectId);
        }

        if (success)
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            var castTimeMs = (int)(action.CastTime * 1000);
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, castTimeMs);

            var targetName = target.Name?.TextValue ?? "Unknown";
            var thinAirNote = context.HasThinAir ? " + Thin Air" : "";
            context.Debug.PlannedAction = action.Name + thinAirNote;
            context.Debug.PlanningState = "Single Heal";

            // Log healing decision
            var hpPercent = context.PartyHelper.GetHpPercent(target);
            var conservationNote = isInMpConservation ? ", MP conservation" : "";
            var freecureNote = context.HasFreecure ? ", Freecure proc" : "";
            context.LogHealDecision(
                targetName,
                hpPercent,
                action.Name,
                healAmount,
                $"Single-target{conservationNote}{freecureNote}{thinAirNote}");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var missingHp = target.MaxHp - target.CurrentHp;
                var shortReason = context.HasFreecure
                    ? $"Freecure proc! {action.Name} on {targetName}"
                    : $"{action.Name} on {targetName} at {hpPercent:P0}";

                var factorsList = new List<string>
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Missing HP: {missingHp:N0}",
                    $"Heal amount: {healAmount:N0}",
                    hasRegen ? $"Regen active ({regenRemaining:F1}s remaining)" : "No Regen active",
                };

                if (context.HasFreecure)
                    factorsList.Add("Freecure proc active (free Cure II!)");
                if (isInMpConservation)
                    factorsList.Add("MP conservation mode - using efficient heals");
                if (context.HasThinAir)
                    factorsList.Add("Thin Air active (free cast!)");

                var alternatives = new List<string>();
                if (action.ActionId == WHMActions.CureII.ActionId && !context.HasFreecure)
                    alternatives.Add("Cure (cheaper but weaker)");
                if (action.ActionId == WHMActions.Cure.ActionId && player.Level >= WHMActions.CureII.MinLevel)
                    alternatives.Add("Cure II (stronger but more MP)");
                if (!hasRegen && player.Level >= WHMActions.Regen.MinLevel)
                    alternatives.Add("Regen (if not urgent)");
                if (context.LilyCount > 0)
                    alternatives.Add("Afflatus Solace (builds Blood Lily)");

                var tip = context.HasFreecure
                    ? "Always use Freecure procs! Cure II is free when Freecure is active."
                    : isInMpConservation
                        ? "In MP conservation mode, prioritize efficient heals and let oGCDs do the work."
                        : "Single-target GCD heals should be used when oGCDs are on cooldown and the target needs immediate attention.";

                var isTank = JobRegistry.IsTank(target.ClassJob.RowId);
                var conceptId = isTank ? WhmConcepts.TankPriority : WhmConcepts.HealingPriority;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = action.Name,
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"{action.Name} on {targetName} who was at {hpPercent:P0} HP (missing {missingHp:N0}). {(context.HasFreecure ? "Used Freecure proc for free Cure II. " : "")}{(hasRegen ? $"Target already has Regen ({regenRemaining:F1}s remaining). " : "")}{(isInMpConservation ? "MP conservation mode active - chose efficient heal. " : "")}Heal amount: {healAmount:N0}.",
                    Factors = factorsList.ToArray(),
                    Alternatives = alternatives.ToArray(),
                    Tip = tip,
                    ConceptId = conceptId,
                    Priority = hpPercent < 0.3f ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }
        }
        else
        {
            context.HpPredictionService.ClearPendingHeals();
        }

        return success;
    }
}
