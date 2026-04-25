using System;
using System.Collections.Generic;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
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

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing) return;
        if (player.Level < CureMinLevel) return;

        var target = config.Healing.UseDamageIntakeTriage
            ? context.PartyHelper.FindMostEndangeredPartyMember(
                player, context.DamageIntakeService, 0, context.DamageTrendService, context.ShieldTrackingService)
            : context.PartyHelper.FindLowestHpPartyMember(player);

        if (target is null) return;
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) return;

        if (CoHealerAwarenessHelper.CoHealerWillCover(
                config.Healing.EnableCoHealerAwareness,
                context.CoHealerDetectionService,
                target,
                config.Healing.CoHealerPendingHealThreshold))
            return;

        var hasRegen = StatusHelper.HasRegenActive(target, out var regenRemaining);
        var isInMpConservation = context.MpForecastService.IsInConservationMode;

        var (action, healAmount) = context.HealingSpellSelector.SelectBestSingleHeal(
            player, target, context.CanExecuteOgcd, context.HasFreecure, hasRegen, regenRemaining, isInMpConservation);
        if (action is null) return;
        if (isMoving && action.CastTime > 0) return;

        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
        var healAmountRaw = action.EstimateHealAmountRaw(mind, det, wd, player.Level);
        context.Debug.LastHealAmount = healAmount;
        context.Debug.LastHealStats = $"MND:{mind} DET:{det} WD:{wd} Lv:{player.Level} Pot:{action.HealPotency}";

        if (action.MpCost >= 1000 && action.Category == ActionCategory.GCD && ThinAirHelper.ShouldWaitForThinAir(context))
            return;

        var capturedTarget = target;
        var capturedAction = action;
        var capturedHealAmount = healAmount;
        var capturedHealAmountRaw = healAmountRaw;
        var capturedHasRegen = hasRegen;
        var capturedRegenRemaining = regenRemaining;
        var capturedIsInMpConservation = isInMpConservation;

        var behavior = new AbilityBehavior { Action = action };

        Action<IRotationContext> onDispatched = _ =>
        {
            var castTimeMs = (int)(capturedAction.CastTime * 1000);
            context.HealingCoordination.TryReserveTarget(
                capturedTarget.EntityId, context.PartyCoordinationService, capturedHealAmount, capturedAction.ActionId, castTimeMs);
            context.HpPredictionService.RegisterPendingHeal(capturedTarget.EntityId, capturedHealAmount);
            if (capturedAction.HealPotency > 0)
                context.CombatEventService.RegisterPredictionForCalibration(capturedHealAmountRaw);

            var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
            var thinAirNote = context.HasThinAir ? " + Thin Air" : "";
            context.Debug.PlannedAction = capturedAction.Name + thinAirNote;
            context.Debug.PlanningState = "Single Heal";

            var hpPercent = context.PartyHelper.GetHpPercent(capturedTarget);
            var conservationNote = capturedIsInMpConservation ? ", MP conservation" : "";
            var freecureNote = context.HasFreecure ? ", Freecure proc" : "";
            context.LogHealDecision(targetName, hpPercent, capturedAction.Name, capturedHealAmount,
                $"Single-target{conservationNote}{freecureNote}{thinAirNote}");

            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var missingHp = capturedTarget.MaxHp - capturedTarget.CurrentHp;
                var shortReason = context.HasFreecure
                    ? $"Freecure proc! {capturedAction.Name} on {targetName}"
                    : $"{capturedAction.Name} on {targetName} at {hpPercent:P0}";

                var factorsList = new List<string>
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Missing HP: {missingHp:N0}",
                    $"Heal amount: {capturedHealAmount:N0}",
                    capturedHasRegen ? $"Regen active ({capturedRegenRemaining:F1}s remaining)" : "No Regen active",
                };

                if (context.HasFreecure) factorsList.Add("Freecure proc active (free Cure II!)");
                if (capturedIsInMpConservation) factorsList.Add("MP conservation mode - using efficient heals");
                if (context.HasThinAir) factorsList.Add("Thin Air active (free cast!)");

                var alternatives = new List<string>();
                if (capturedAction.ActionId == WHMActions.CureII.ActionId && !context.HasFreecure)
                    alternatives.Add("Cure (cheaper but weaker)");
                if (capturedAction.ActionId == WHMActions.Cure.ActionId && context.Player.Level >= WHMActions.CureII.MinLevel)
                    alternatives.Add("Cure II (stronger but more MP)");
                if (!capturedHasRegen && context.Player.Level >= WHMActions.Regen.MinLevel)
                    alternatives.Add("Regen (if not urgent)");
                if (context.LilyCount > 0)
                    alternatives.Add("Afflatus Solace (builds Blood Lily)");

                var tip = context.HasFreecure
                    ? "Always use Freecure procs! Cure II is free when Freecure is active."
                    : capturedIsInMpConservation
                        ? "In MP conservation mode, prioritize efficient heals and let oGCDs do the work."
                        : "Single-target GCD heals should be used when oGCDs are on cooldown and the target needs immediate attention.";

                var isTank = JobRegistry.IsTank(capturedTarget.ClassJob.RowId);
                var conceptId = isTank ? WhmConcepts.TankPriority : WhmConcepts.HealingPriority;

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = capturedAction.ActionId,
                    ActionName = capturedAction.Name,
                    Category = "Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"{capturedAction.Name} on {targetName} who was at {hpPercent:P0} HP (missing {missingHp:N0}). {(context.HasFreecure ? "Used Freecure proc for free Cure II. " : "")}{(capturedHasRegen ? $"Target already has Regen ({capturedRegenRemaining:F1}s remaining). " : "")}{(capturedIsInMpConservation ? "MP conservation mode active - chose efficient heal. " : "")}Heal amount: {capturedHealAmount:N0}.",
                    Factors = factorsList.ToArray(),
                    Alternatives = alternatives.ToArray(),
                    Tip = tip,
                    ConceptId = conceptId,
                    Priority = hpPercent < 0.3f ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }
        };

        if (action.Category == ActionCategory.oGCD)
        {
            scheduler.PushOgcd(behavior, target.GameObjectId, priority: (int)Priority, onDispatched: onDispatched);
        }
        else
        {
            scheduler.PushGcd(behavior, target.GameObjectId, priority: (int)Priority, onDispatched: onDispatched);
        }
    }
}
