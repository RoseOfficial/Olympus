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
/// Handles AoE healing (Medica, Cure III, etc.).
/// Evaluates injured party members and selects optimal AoE heal.
/// </summary>
public sealed class AoEHealingHandler : IHealingHandler
{
    private const byte MedicaMinLevel = 10;

    public HealingPriority Priority => HealingPriority.AoEHeal;
    public string Name => "AoEHeal";

    public bool TryExecute(IApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing)
        {
            context.Debug.AoEStatus = "Healing disabled";
            return false;
        }

        if (player.Level < MedicaMinLevel)
        {
            context.Debug.AoEStatus = $"Level {player.Level} < {MedicaMinLevel}";
            return false;
        }

        // Calculate heal amounts for overheal prevention
        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
        var medicaHealAmount = WHMActions.Medica.EstimateHealAmount(mind, det, wd, player.Level);
        var cureIIIHealAmount = WHMActions.CureIII.EstimateHealAmount(mind, det, wd, player.Level);

        // Count injured for self-centered AoE heals
        var (injuredCount, anyHaveRegen, allTargets, averageMissingHp) =
            context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, medicaHealAmount);
        context.Debug.AoEInjuredCount = injuredCount;

        // Find best Cure III target
        var (cureIIITarget, cureIIITargetCount, cureIIITargetIds) =
            context.PartyHelper.FindBestCureIIITarget(player, cureIIIHealAmount);

        // Check if a raidwide is imminent (timeline-aware)
        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            config.Healing,
            out var raidwideSource);

        // Lower threshold when raidwide is imminent to ensure party is healthy before damage
        var effectiveMinTargets = config.Healing.AoEHealMinTargets;
        if (raidwideImminent && effectiveMinTargets > 2)
        {
            // Lower threshold by 1 (but not below 2) when raidwide is coming
            effectiveMinTargets = config.Healing.AoEHealMinTargets - 1;
        }

        // Check if we have enough targets
        var hasEnoughSelfCenteredTargets = injuredCount >= effectiveMinTargets;
        var hasEnoughCureIIITargets = cureIIITargetCount >= effectiveMinTargets;

        if (!hasEnoughSelfCenteredTargets && !hasEnoughCureIIITargets)
        {
            context.Debug.AoEStatus = $"Injured {injuredCount} (self) / {cureIIITargetCount} (CureIII) < min {effectiveMinTargets}" +
                (raidwideImminent ? $" (raidwide via {raidwideSource})" : "");
            return false;
        }

        // Get best AoE heal from selector
        var isInMpConservation = context.MpForecastService.IsInConservationMode;
        var (action, healAmount, selectedCureIIITarget) = context.HealingSpellSelector.SelectBestAoEHeal(
            player, averageMissingHp, injuredCount, anyHaveRegen, context.CanExecuteOgcd,
            cureIIITargetCount, cureIIITarget, isInMpConservation);

        if (action is null)
        {
            context.Debug.AoEStatus = "No AoE heal available";
            return false;
        }

        if (isMoving && action.CastTime > 0)
        {
            context.Debug.AoEStatus = "Moving";
            return false;
        }

        // Check AoE coordination - prevent multiple healers from casting AoE heals simultaneously
        var castTimeMs = (int)(action.CastTime * 1000);
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, healAmount, castTimeMs))
        {
            context.Debug.AoEStatus = "Skipped (remote AOE reserved)";
            return false;
        }

        context.Debug.AoESelectedSpell = action.ActionId;
        context.Debug.AoEStatus = $"Executing {action.Name}" +
            (selectedCureIIITarget is not null ? $" on {selectedCureIIITarget.Name}" : "");

        // Register pending heals
        List<uint> targetIds;
        if (selectedCureIIITarget is not null)
        {
            targetIds = cureIIITargetIds;
        }
        else
        {
            targetIds = new List<uint>();
            foreach (var (entityId, _) in allTargets)
            {
                targetIds.Add(entityId);
            }
        }
        context.HpPredictionService.RegisterPendingAoEHeal(targetIds, healAmount);

        // Execute
        bool success;
        var executionTarget = selectedCureIIITarget?.GameObjectId ?? player.GameObjectId;

        if (action.Category == ActionCategory.oGCD)
        {
            success = context.ActionService.ExecuteOgcd(action, executionTarget);
        }
        else
        {
            if (action.MpCost >= 1000 && ThinAirHelper.ShouldWaitForThinAir(context))
            {
                context.Debug.AoEStatus = "Waiting for Thin Air";
                context.HpPredictionService.ClearPendingHeals();
                return false;
            }

            success = context.ActionService.ExecuteGcd(action, executionTarget);
        }

        if (success)
        {
            var thinAirNote = context.HasThinAir ? " + Thin Air" : "";
            context.Debug.PlannedAction = action.Name + thinAirNote;
            context.Debug.PlanningState = "AoE Heal";
            var targetName = selectedCureIIITarget?.Name?.TextValue ?? player.Name?.TextValue ?? "Unknown";

            // Log AoE healing decision
            var avgHpPct = context.PartyHealthMetrics.avgHpPercent;
            var conservationNote = isInMpConservation ? ", MP conservation" : "";
            var timelineNote = raidwideImminent ? $", raidwide via {raidwideSource}" : "";
            context.LogHealDecision(
                $"{injuredCount} injured",
                avgHpPct,
                action.Name,
                healAmount,
                $"AoE heal (avg missing {averageMissingHp} HP){conservationNote}{timelineNote}{thinAirNote}");

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var isCureIII = selectedCureIIITarget is not null;
                var shortReason = isCureIII
                    ? $"Cure III on {targetName} - {cureIIITargetCount} stacked"
                    : $"AoE heal - {injuredCount} injured, {avgHpPct:P0} avg HP";

                var factorsList = new List<string>
                {
                    $"Injured count: {injuredCount}",
                    $"Average party HP: {avgHpPct:P0}",
                    $"Average missing HP: {averageMissingHp:N0}",
                    $"Heal amount: {healAmount:N0}",
                    $"Min targets threshold: {effectiveMinTargets}",
                };

                if (raidwideImminent)
                    factorsList.Add($"Raidwide incoming via {raidwideSource}");
                if (isInMpConservation)
                    factorsList.Add("MP conservation mode active");
                if (context.HasThinAir)
                    factorsList.Add("Thin Air active (free cast!)");
                if (isCureIII)
                    factorsList.Add($"Cure III optimal: {cureIIITargetCount} targets stacked");

                var alternatives = new List<string>();
                if (action.ActionId != WHMActions.Medica.ActionId)
                    alternatives.Add("Medica (cheaper but weaker)");
                if (action.ActionId != WHMActions.MedicaII.ActionId && player.Level >= WHMActions.MedicaII.MinLevel)
                    alternatives.Add("Medica II (adds HoT)");
                if (action.ActionId != WHMActions.CureIII.ActionId && player.Level >= WHMActions.CureIII.MinLevel)
                    alternatives.Add("Cure III (if stacked)");
                if (context.LilyCount > 0 && player.Level >= 76)
                    alternatives.Add("Afflatus Rapture (builds Blood Lily)");

                var tip = raidwideImminent
                    ? "Healing before raidwides ensures the party survives - don't wait for HP to drop!"
                    : isCureIII
                        ? "Cure III is most efficient when the party is stacked - perfect for stack mechanics!"
                        : "AoE heals are efficient when multiple targets are injured - single-target heals for just one.";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = action.ActionId,
                    ActionName = action.Name,
                    Category = "Healing",
                    TargetName = isCureIII ? targetName : "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"{action.Name} heals {(isCureIII ? $"{cureIIITargetCount} stacked targets around {targetName}" : $"all {injuredCount} injured party members")}. {(raidwideImminent ? $"Used proactively due to incoming raidwide ({raidwideSource}). " : "")}Average party HP was {avgHpPct:P0} with {averageMissingHp:N0} average missing HP.",
                    Factors = factorsList.ToArray(),
                    Alternatives = alternatives.ToArray(),
                    Tip = tip,
                    ConceptId = raidwideImminent ? WhmConcepts.PartyWideDamage : WhmConcepts.HealingPriority,
                    Priority = raidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
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
