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
/// Handles AoE healing (Medica, Cure III, etc.).
/// </summary>
public sealed class AoEHealingHandler : IHealingHandler
{
    private const byte MedicaMinLevel = 10;

    public HealingPriority Priority => HealingPriority.AoEHeal;
    public string Name => "AoEHeal";

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing) { context.Debug.AoEStatus = "Healing disabled"; return; }
        if (player.Level < MedicaMinLevel) { context.Debug.AoEStatus = $"Level {player.Level} < {MedicaMinLevel}"; return; }

        var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
        var medicaHealAmount = WHMActions.Medica.EstimateHealAmount(mind, det, wd, player.Level);
        var cureIIIHealAmount = WHMActions.CureIII.EstimateHealAmount(mind, det, wd, player.Level);

        var (injuredCount, anyHaveRegen, allTargets, averageMissingHp) =
            context.PartyHelper.CountPartyMembersNeedingAoEHeal(player, medicaHealAmount);
        context.Debug.AoEInjuredCount = injuredCount;

        var (cureIIITarget, cureIIITargetCount, cureIIITargetIds) =
            context.PartyHelper.FindBestCureIIITarget(player, cureIIIHealAmount);

        var raidwideImminent = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            config,
            out var raidwideSource);

        var effectiveMinTargets = config.Healing.AoEHealMinTargets;
        if (raidwideImminent && effectiveMinTargets > 2)
            effectiveMinTargets = config.Healing.AoEHealMinTargets - 1;

        var hasEnoughSelfCenteredTargets = injuredCount >= effectiveMinTargets;
        var hasEnoughCureIIITargets = cureIIITargetCount >= effectiveMinTargets;

        if (!hasEnoughSelfCenteredTargets && !hasEnoughCureIIITargets)
        {
            context.Debug.AoEStatus = $"Injured {injuredCount} (self) / {cureIIITargetCount} (CureIII) < min {effectiveMinTargets}" +
                (raidwideImminent ? $" (raidwide via {raidwideSource})" : "");
            return;
        }

        var isInMpConservation = context.MpForecastService.IsInConservationMode;
        var (action, healAmount, selectedCureIIITarget) = context.HealingSpellSelector.SelectBestAoEHeal(
            player, averageMissingHp, injuredCount, anyHaveRegen, context.CanExecuteOgcd,
            cureIIITargetCount, cureIIITarget, isInMpConservation);

        if (action is null) { context.Debug.AoEStatus = "No AoE heal available"; return; }
        if (isMoving && action.CastTime > 0) { context.Debug.AoEStatus = "Moving"; return; }

        var castTimeMs = (int)(action.CastTime * 1000);
        if (!context.HealingCoordination.TryReserveAoEHeal(
            context.PartyCoordinationService, action.ActionId, healAmount, castTimeMs))
        {
            context.Debug.AoEStatus = "Skipped (remote AOE reserved)";
            return;
        }

        if (action.MpCost >= 1000 && action.Category == ActionCategory.GCD && ThinAirHelper.ShouldWaitForThinAir(context))
        {
            context.Debug.AoEStatus = "Waiting for Thin Air";
            return;
        }

        context.Debug.AoESelectedSpell = action.ActionId;
        context.Debug.AoEStatus = $"Executing {action.Name}" +
            (selectedCureIIITarget is not null ? $" on {selectedCureIIITarget.Name}" : "");

        var capturedAction = action;
        var capturedHealAmount = healAmount;
        var capturedSelectedTarget = selectedCureIIITarget;
        var capturedAllTargets = allTargets;
        var capturedCureIIITargetIds = cureIIITargetIds;
        var capturedInjuredCount = injuredCount;
        var capturedRaidwideImminent = raidwideImminent;
        var capturedRaidwideSource = raidwideSource;
        var capturedIsInMpConservation = isInMpConservation;
        var capturedAverageMissingHp = averageMissingHp;
        var capturedCureIIITargetCount = cureIIITargetCount;
        var capturedEffectiveMinTargets = effectiveMinTargets;

        var executionTarget = selectedCureIIITarget?.GameObjectId ?? player.GameObjectId;
        var behavior = new AbilityBehavior { Action = action };

        Action<IRotationContext> onDispatched = _ =>
        {
            List<uint> targetIds;
            if (capturedSelectedTarget is not null)
            {
                targetIds = capturedCureIIITargetIds;
            }
            else
            {
                targetIds = new List<uint>();
                foreach (var (entityId, _) in capturedAllTargets)
                    targetIds.Add(entityId);
            }
            context.HpPredictionService.RegisterPendingAoEHeal(targetIds, capturedHealAmount);

            var thinAirNote = context.HasThinAir ? " + Thin Air" : "";
            context.Debug.PlannedAction = capturedAction.Name + thinAirNote;
            context.Debug.PlanningState = "AoE Heal";
            var targetName = capturedSelectedTarget?.Name?.TextValue ?? context.Player.Name?.TextValue ?? "Unknown";

            var avgHpPct = context.PartyHealthMetrics.avgHpPercent;
            var conservationNote = capturedIsInMpConservation ? ", MP conservation" : "";
            var timelineNote = capturedRaidwideImminent ? $", raidwide via {capturedRaidwideSource}" : "";
            context.LogHealDecision(
                $"{capturedInjuredCount} injured", avgHpPct, capturedAction.Name, capturedHealAmount,
                $"AoE heal (avg missing {capturedAverageMissingHp} HP){conservationNote}{timelineNote}{thinAirNote}");

            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var isCureIII = capturedSelectedTarget is not null;
                var shortReason = isCureIII
                    ? $"Cure III on {targetName} - {capturedCureIIITargetCount} stacked"
                    : $"AoE heal - {capturedInjuredCount} injured, {avgHpPct:P0} avg HP";

                var factorsList = new List<string>
                {
                    $"Injured count: {capturedInjuredCount}",
                    $"Average party HP: {avgHpPct:P0}",
                    $"Average missing HP: {capturedAverageMissingHp:N0}",
                    $"Heal amount: {capturedHealAmount:N0}",
                    $"Min targets threshold: {capturedEffectiveMinTargets}",
                };

                if (capturedRaidwideImminent) factorsList.Add($"Raidwide incoming via {capturedRaidwideSource}");
                if (capturedIsInMpConservation) factorsList.Add("MP conservation mode active");
                if (context.HasThinAir) factorsList.Add("Thin Air active (free cast!)");
                if (isCureIII) factorsList.Add($"Cure III optimal: {capturedCureIIITargetCount} targets stacked");

                var alternatives = new List<string>();
                if (capturedAction.ActionId != WHMActions.Medica.ActionId)
                    alternatives.Add("Medica (cheaper but weaker)");
                if (capturedAction.ActionId != WHMActions.MedicaII.ActionId && context.Player.Level >= WHMActions.MedicaII.MinLevel)
                    alternatives.Add("Medica II (adds HoT)");
                if (capturedAction.ActionId != WHMActions.CureIII.ActionId && context.Player.Level >= WHMActions.CureIII.MinLevel)
                    alternatives.Add("Cure III (if stacked)");
                if (context.LilyCount > 0 && context.Player.Level >= 76)
                    alternatives.Add("Afflatus Rapture (builds Blood Lily)");

                var tip = capturedRaidwideImminent
                    ? "Healing before raidwides ensures the party survives - don't wait for HP to drop!"
                    : isCureIII
                        ? "Cure III is most efficient when the party is stacked - perfect for stack mechanics!"
                        : "AoE heals are efficient when multiple targets are injured - single-target heals for just one.";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.UtcNow,
                    ActionId = capturedAction.ActionId,
                    ActionName = capturedAction.Name,
                    Category = "Healing",
                    TargetName = isCureIII ? targetName : "Party",
                    ShortReason = shortReason,
                    DetailedReason = $"{capturedAction.Name} heals {(isCureIII ? $"{capturedCureIIITargetCount} stacked targets around {targetName}" : $"all {capturedInjuredCount} injured party members")}. {(capturedRaidwideImminent ? $"Used proactively due to incoming raidwide ({capturedRaidwideSource}). " : "")}Average party HP was {avgHpPct:P0} with {capturedAverageMissingHp:N0} average missing HP.",
                    Factors = factorsList.ToArray(),
                    Alternatives = alternatives.ToArray(),
                    Tip = tip,
                    ConceptId = capturedRaidwideImminent ? WhmConcepts.PartyWideDamage : WhmConcepts.HealingPriority,
                    Priority = capturedRaidwideImminent ? ExplanationPriority.High : ExplanationPriority.Normal,
                });
            }
        };

        if (action.Category == ActionCategory.oGCD)
        {
            scheduler.PushOgcd(behavior, executionTarget, priority: (int)Priority, onDispatched: onDispatched);
        }
        else
        {
            scheduler.PushGcd(behavior, executionTarget, priority: (int)Priority, onDispatched: onDispatched);
        }
    }
}
