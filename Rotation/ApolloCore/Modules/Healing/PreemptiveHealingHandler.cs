using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles preemptive healing when a damage spike is detected.
/// </summary>
public sealed class PreemptiveHealingHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.PreemptiveHeal;
    public string Name => "PreemptiveHeal";

    private static readonly string[] _preemptiveTetragrammatonAlternatives =
    {
        "Wait for HP to drop (risky)",
        "Use GCD heal (slower response)",
        "Save for bigger emergency",
    };

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing) return;

        var detection = PreemptiveSpikeDetectionHelper.Detect(
            player, config, context.PartyHelper,
            context.DamageIntakeService, context.DamageTrendService, context.HpPredictionService,
            context.ShieldTrackingService, context.CoHealerDetectionService,
            context.TimelineService, context.BossMechanicDetector,
            context.PartyHealthMetrics.avgHpPercent);

        if (detection is null) return;

        var target = detection.Value.Target;
        var spikeSeverity = detection.Value.Severity;
        var raidwideSource = detection.Value.Source;
        var isTimelineRaidwide = detection.Value.IsTimelineRaidwide;
        var isPredictedSpike = !isTimelineRaidwide && detection.Value.PatternConfidence > 0;
        var patternConfidence = detection.Value.PatternConfidence;
        var targetHpPercent = detection.Value.TargetHpPercent;
        var projectedHpPercent = detection.Value.ProjectedHpPercent;

        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) return;
        if (CoHealerAwarenessHelper.CoHealerWillCover(
                config.Healing.EnableCoHealerAwareness,
                context.CoHealerDetectionService,
                target,
                config.Healing.CoHealerPendingHealThreshold))
            return;

        // Try Tetragrammaton (oGCD) first
        if (context.CanExecuteOgcd &&
            ActionValidator.CanExecute(player, context.ActionService, WHMActions.Tetragrammaton, config,
                c => c.EnableHealing && c.Healing.EnableTetragrammaton) &&
            DistanceHelper.IsInRange(player, target, WHMActions.Tetragrammaton.Range))
        {
            var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
            var healAmount = WHMActions.Tetragrammaton.EstimateHealAmount(mind, det, wd, player.Level);

            var capturedTarget = target;
            var capturedHealAmount = healAmount;
            var capturedSpikeSeverity = spikeSeverity;
            var capturedIsTimelineRaidwide = isTimelineRaidwide;
            var capturedIsPredicted = isPredictedSpike;
            var capturedPatternConfidence = patternConfidence;
            var capturedRaidwideSource = raidwideSource;
            var capturedTargetHpPercent = targetHpPercent;
            var capturedProjectedHpPercent = projectedHpPercent;

            scheduler.PushOgcd(ApolloAbilities.Tetragrammaton, target.GameObjectId, priority: (int)Priority,
                onDispatched: _ =>
                {
                    context.HealingCoordination.TryReserveTarget(
                        capturedTarget.EntityId, context.PartyCoordinationService, capturedHealAmount, WHMActions.Tetragrammaton.ActionId, 0);
                    context.HpPredictionService.RegisterPendingHeal(capturedTarget.EntityId, capturedHealAmount);

                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var sourceNote = capturedIsTimelineRaidwide ? $" via {capturedRaidwideSource}" : "";
                    context.Debug.PlannedAction = $"Tetragrammaton (preemptive{sourceNote}, severity {capturedSpikeSeverity:F2})";
                    context.LogOgcdDecision(targetName, capturedTargetHpPercent, "Tetragrammaton",
                        $"Preemptive{sourceNote} - spike imminent (severity {capturedSpikeSeverity:F2}, projected {capturedProjectedHpPercent:P0})");

                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        var source = capturedIsTimelineRaidwide ? $"timeline ({capturedRaidwideSource})" :
                                     capturedIsPredicted ? $"pattern (confidence {capturedPatternConfidence:P0})" :
                                     "reactive spike detection";

                        var shortReason = $"Preemptive heal - {targetName} projected to {capturedProjectedHpPercent:P0}";
                        var factors = new[]
                        {
                            $"Current HP: {capturedTargetHpPercent:P0}",
                            $"Projected HP: {capturedProjectedHpPercent:P0}",
                            $"Spike severity: {capturedSpikeSeverity:F2}",
                            $"Detection source: {source}",
                        };

                        context.TrainingService.RecordDecision(new ActionExplanation
                        {
                            Timestamp = DateTime.UtcNow,
                            ActionId = WHMActions.Tetragrammaton.ActionId,
                            ActionName = "Tetragrammaton",
                            Category = "Healing",
                            TargetName = targetName,
                            ShortReason = shortReason,
                            DetailedReason = $"Preemptive Tetragrammaton on {targetName}. Current HP is {capturedTargetHpPercent:P0} but projected to drop to {capturedProjectedHpPercent:P0} based on {source}. Healing NOW prevents emergency later. Spike severity: {capturedSpikeSeverity:F2}.",
                            Factors = factors,
                            Alternatives = _preemptiveTetragrammatonAlternatives,
                            Tip = "Preemptive healing is key to smooth runs - heal BEFORE damage lands when you can predict it!",
                            ConceptId = WhmConcepts.ProactiveHealing,
                            Priority = ExplanationPriority.High,
                        });
                    }
                });
            return;
        }

        // Try Benediction for very high severity + low HP
        if (context.CanExecuteOgcd && spikeSeverity >= 0.8f && targetHpPercent < 0.5f &&
            ActionValidator.CanExecute(player, context.ActionService, WHMActions.Benediction, config,
                c => c.EnableHealing && c.Healing.EnableBenediction) &&
            DistanceHelper.IsInRange(player, target, WHMActions.Benediction.Range))
        {
            var missingHp = (int)(target.MaxHp - target.CurrentHp);
            var capturedTarget = target;
            var capturedMissingHp = missingHp;
            var capturedSpikeSeverity = spikeSeverity;
            var capturedIsTimelineRaidwide = isTimelineRaidwide;
            var capturedIsPredicted = isPredictedSpike;
            var capturedPatternConfidence = patternConfidence;
            var capturedRaidwideSource = raidwideSource;
            var capturedTargetHpPercent = targetHpPercent;

            scheduler.PushOgcd(ApolloAbilities.Benediction, target.GameObjectId, priority: (int)Priority,
                onDispatched: _ =>
                {
                    context.HealingCoordination.TryReserveTarget(
                        capturedTarget.EntityId, context.PartyCoordinationService, capturedMissingHp, WHMActions.Benediction.ActionId, 0);
                    context.HpPredictionService.RegisterPendingHeal(capturedTarget.EntityId, capturedMissingHp);

                    var beneTargetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var sourceNote = capturedIsTimelineRaidwide ? $" via {capturedRaidwideSource}" : "";
                    context.Debug.PlannedAction = $"Benediction (preemptive{sourceNote}, critical severity {capturedSpikeSeverity:F2})";
                    context.LogOgcdDecision(beneTargetName, capturedTargetHpPercent, "Benediction",
                        $"Preemptive{sourceNote} - critical spike (severity {capturedSpikeSeverity:F2}, target at {capturedTargetHpPercent:P0})");

                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        var source = capturedIsTimelineRaidwide ? $"timeline ({capturedRaidwideSource})" :
                                     capturedIsPredicted ? $"pattern (confidence {capturedPatternConfidence:P0})" :
                                     "reactive spike detection";

                        var shortReason = $"Critical preemptive - {beneTargetName} at {capturedTargetHpPercent:P0}, severity {capturedSpikeSeverity:F2}";
                        var factors = new[]
                        {
                            $"Current HP: {capturedTargetHpPercent:P0}",
                            $"Missing HP: {capturedMissingHp:N0}",
                            $"Spike severity: {capturedSpikeSeverity:F2} (critical!)",
                            $"Detection source: {source}",
                            "Target below 50% with severe spike incoming",
                        };

                        context.TrainingService.RecordDecision(new ActionExplanation
                        {
                            Timestamp = DateTime.UtcNow,
                            ActionId = WHMActions.Benediction.ActionId,
                            ActionName = "Benediction",
                            Category = "Emergency Healing",
                            TargetName = beneTargetName,
                            ShortReason = shortReason,
                            DetailedReason = $"Critical preemptive Benediction on {beneTargetName}. HP is already at {capturedTargetHpPercent:P0} with a severity {capturedSpikeSeverity:F2} spike incoming via {source}. Using Benediction now to ensure survival.",
                            Factors = factors,
                            Alternatives = new[] { "Tetragrammaton (insufficient for critical spike)", "GCD heal (too slow)" },
                            Tip = "When spike severity is very high (>0.8) and HP is low, don't hesitate to use Benediction preemptively!",
                            ConceptId = WhmConcepts.ProactiveHealing,
                            Priority = ExplanationPriority.Critical,
                        });
                    }
                });
            return;
        }

        // GCD fallback
        if (context.CanExecuteGcd && !isMoving)
        {
            var hasRegen = StatusHelper.HasRegenActive(target, out var regenRemaining);
            var isInMpConservation = context.MpForecastService.IsInConservationMode;

            var (action, healAmount) = context.HealingSpellSelector.SelectBestSingleHeal(
                player, target, false, context.HasFreecure, hasRegen, regenRemaining, isInMpConservation);
            if (action is null) return;

            var capturedTarget = target;
            var capturedAction = action;
            var capturedHealAmount = healAmount;
            var capturedHasRegen = hasRegen;
            var capturedIsTimelineRaidwide = isTimelineRaidwide;
            var capturedIsPredicted = isPredictedSpike;
            var capturedPatternConfidence = patternConfidence;
            var capturedRaidwideSource = raidwideSource;
            var capturedTargetHpPercent = targetHpPercent;
            var capturedProjectedHpPercent = projectedHpPercent;
            var capturedSpikeSeverity = spikeSeverity;

            var behavior = new AbilityBehavior { Action = action };

            scheduler.PushGcd(behavior, target.GameObjectId, priority: (int)Priority,
                onDispatched: _ =>
                {
                    var castTimeMs = (int)(capturedAction.CastTime * 1000);
                    context.HealingCoordination.TryReserveTarget(
                        capturedTarget.EntityId, context.PartyCoordinationService, capturedHealAmount, capturedAction.ActionId, castTimeMs);
                    context.HpPredictionService.RegisterPendingHeal(capturedTarget.EntityId, capturedHealAmount);

                    var gcdTargetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var thinAirNote = context.HasThinAir ? " + Thin Air" : "";
                    var sourceNote = capturedIsTimelineRaidwide ? $" via {capturedRaidwideSource}" : "";
                    context.Debug.PlannedAction = $"{capturedAction.Name} (preemptive{sourceNote}){thinAirNote}";
                    context.Debug.PlanningState = "Preemptive Heal";

                    context.LogHealDecision(gcdTargetName, capturedTargetHpPercent, capturedAction.Name, capturedHealAmount,
                        $"Preemptive{sourceNote} - spike severity {capturedSpikeSeverity:F2}, projected {capturedProjectedHpPercent:P0}{thinAirNote}");

                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        var source = capturedIsTimelineRaidwide ? $"timeline ({capturedRaidwideSource})" :
                                     capturedIsPredicted ? $"pattern (confidence {capturedPatternConfidence:P0})" :
                                     "reactive spike detection";

                        var shortReason = $"Preemptive {capturedAction.Name} - {gcdTargetName} projected to {capturedProjectedHpPercent:P0}";
                        var factors = new[]
                        {
                            $"Current HP: {capturedTargetHpPercent:P0}",
                            $"Projected HP: {capturedProjectedHpPercent:P0}",
                            $"Heal amount: {capturedHealAmount:N0}",
                            $"Spike severity: {capturedSpikeSeverity:F2}",
                            $"Detection source: {source}",
                            context.HasThinAir ? "Thin Air active (free cast!)" : $"MP cost: {capturedAction.MpCost}",
                        };

                        var alternatives = new[]
                        {
                            "Wait for oGCD cooldowns",
                            "Wait for HP to drop further (risky)",
                            capturedHasRegen ? "Rely on existing Regen" : "Apply Regen first",
                        };

                        context.TrainingService.RecordDecision(new ActionExplanation
                        {
                            Timestamp = DateTime.UtcNow,
                            ActionId = capturedAction.ActionId,
                            ActionName = capturedAction.Name,
                            Category = "Healing",
                            TargetName = gcdTargetName,
                            ShortReason = shortReason,
                            DetailedReason = $"Preemptive GCD heal on {gcdTargetName}. No oGCD available, so using {capturedAction.Name} to heal before the predicted spike. Current HP {capturedTargetHpPercent:P0}, projected to drop to {capturedProjectedHpPercent:P0} based on {source}.",
                            Factors = factors,
                            Alternatives = alternatives,
                            Tip = "When oGCDs are on cooldown, don't hesitate to use GCD heals preemptively - preventing deaths is worth the DPS loss!",
                            ConceptId = WhmConcepts.ProactiveHealing,
                            Priority = ExplanationPriority.Normal,
                        });
                    }
                });
        }
    }
}
