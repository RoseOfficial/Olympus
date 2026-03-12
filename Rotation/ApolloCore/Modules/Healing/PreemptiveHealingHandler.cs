using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Services.Training;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles preemptive healing when a damage spike is detected.
/// Uses both reactive spike detection and predictive pattern detection
/// to start healing BEFORE the spike lands.
/// </summary>
public sealed class PreemptiveHealingHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.PreemptiveHeal;
    public string Name => "PreemptiveHeal";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        // Check if preemptive healing is enabled
        if (!config.EnableHealing || !config.Healing.EnablePreemptiveHealing)
            return false;

        // Three-pronged spike detection:
        // 1. Timeline: Check if a raidwide is predicted by fight timeline (most accurate)
        // 2. Reactive: Check if a damage spike is currently imminent
        // 3. Predictive: Check if a periodic spike pattern predicts incoming damage

        var isTimelineRaidwide = TimelineHelper.IsRaidwideImminent(
            context.TimelineService,
            context.BossMechanicDetector,
            config.Healing,
            out var raidwideSource);

        var timelineRaidwideInfo = isTimelineRaidwide
            ? TimelineHelper.GetNextRaidwide(context.TimelineService, context.BossMechanicDetector, config.Healing)
            : null;

        var isReactiveSpike = context.DamageTrendService.IsDamageSpikeImminent(0.7f);
        var isPredictedSpike = false;
        var patternConfidence = 0f;
        var secondsUntilPredictedSpike = float.MaxValue;

        // Check pattern prediction for each party member (if no timeline prediction)
        if (!isTimelineRaidwide)
        {
            foreach (var member in context.PartyHelper.GetAllPartyMembers(player))
            {
                if (member.IsDead)
                    continue;

                var (predictedSeconds, confidence) = context.DamageTrendService.PredictNextSpike(member.EntityId);

                // Use prediction if confidence exceeds threshold and spike is within lookahead window
                if (confidence >= config.Healing.SpikePatternConfidenceThreshold &&
                    predictedSeconds <= config.Healing.SpikePredictionLookahead &&
                    confidence > patternConfidence)
                {
                    isPredictedSpike = true;
                    patternConfidence = confidence;
                    secondsUntilPredictedSpike = predictedSeconds;
                }
            }
        }

        // Only proceed if timeline, reactive, or predictive spike is detected
        if (!isTimelineRaidwide && !isReactiveSpike && !isPredictedSpike)
            return false;

        // Get spike severity to determine urgency
        var avgPartyHpPercent = context.PartyHealthMetrics.avgHpPercent;
        var spikeSeverity = context.DamageTrendService.GetSpikeSeverity(avgPartyHpPercent);

        // Timeline predictions get highest severity boost (most reliable source)
        if (isTimelineRaidwide && timelineRaidwideInfo.HasValue)
        {
            var timelineConfidence = timelineRaidwideInfo.Value.confidence;
            // High confidence timeline prediction = high severity regardless of current state
            spikeSeverity = Math.Max(spikeSeverity, 0.6f + (timelineConfidence * 0.3f));
        }
        // Boost severity for high-confidence pattern predictions
        else if (isPredictedSpike && patternConfidence >= 0.8f)
        {
            spikeSeverity = Math.Max(spikeSeverity, 0.5f + (patternConfidence * 0.3f));
        }

        // Only act on significant spikes (severity > 0.4)
        // Timeline predictions bypass this check if we're close to the mechanic
        var bypassSeverityCheck = isTimelineRaidwide &&
                                  timelineRaidwideInfo.HasValue &&
                                  timelineRaidwideInfo.Value.secondsUntil <= 3f;
        if (!bypassSeverityCheck && spikeSeverity < 0.4f)
            return false;

        // Find the most endangered target - use damage trend to identify who's taking the hit
        // Pass ShieldTrackingService for shield-aware triage scoring
        var target = context.PartyHelper.FindMostEndangeredPartyMember(
            player, context.DamageIntakeService, 0, context.DamageTrendService, context.ShieldTrackingService);

        if (target is null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        // Co-healer awareness: Skip if co-healer has pending heal covering most of missing HP
        if (config.Healing.EnableCoHealerAwareness && context.CoHealerDetectionService?.HasCoHealer == true)
        {
            var coHealerPendingHeals = context.CoHealerDetectionService.CoHealerPendingHeals;
            if (coHealerPendingHeals.TryGetValue(target.EntityId, out var coHealerPending))
            {
                var missingHp = target.MaxHp - target.CurrentHp;
                var pendingHealPercent = missingHp > 0 ? (float)coHealerPending / missingHp : 1f;

                // Skip if co-healer's pending heal covers enough of the missing HP
                if (pendingHealPercent >= config.Healing.CoHealerPendingHealThreshold)
                    return false;
            }
        }

        // Check if target is actually at risk
        var targetHpPercent = context.PartyHelper.GetHpPercent(target);
        var targetDamageRate = context.DamageTrendService.GetCurrentDamageRate(target.EntityId, 3f);

        // Calculate lookahead window based on configuration
        // Default to 2 seconds if not using spell cast time
        var defaultLookahead = 2f;

        // For initial risk assessment, use the configured spike prediction lookahead
        var initialLookahead = config.Healing.UseSpellCastTimeForLookahead
            ? Math.Max(config.Healing.MinPreemptiveLookahead, 1.5f) // Use Cure II cast time as initial estimate
            : defaultLookahead;

        // Estimate if target will drop below danger threshold based on current damage rate
        var projectedDamage = targetDamageRate * initialLookahead;
        var projectedHp = target.CurrentHp > projectedDamage ? target.CurrentHp - (uint)projectedDamage : 0;
        var projectedHpPercent = (float)projectedHp / target.MaxHp;

        // Only preemptively heal if projected HP would drop below threshold
        if (projectedHpPercent > config.Healing.PreemptiveHealingThreshold)
            return false;

        // Check if target already has pending heals that would save them
        // Include co-healer pending heals in this calculation
        var pendingHeals = context.HpPredictionService.GetPendingHealAmount(target.EntityId);
        if (config.Healing.EnableCoHealerAwareness && context.CoHealerDetectionService?.HasCoHealer == true)
        {
            var coHealerPendingHeals = context.CoHealerDetectionService.CoHealerPendingHeals;
            if (coHealerPendingHeals.TryGetValue(target.EntityId, out var coHealerPending))
                pendingHeals += coHealerPending;
        }
        if (projectedHp + pendingHeals > target.MaxHp * config.Healing.PreemptiveHealingThreshold)
            return false;

        // Prioritize oGCD heals first (Tetragrammaton, Benediction) for instant response
        if (context.CanExecuteOgcd)
        {
            // Try Tetragrammaton first (lower cooldown, saves Benediction for emergencies)
            if (ActionValidator.CanExecute(player, context.ActionService, WHMActions.Tetragrammaton, config,
                c => c.EnableHealing && c.Healing.EnableTetragrammaton) &&
                DistanceHelper.IsInRange(player, target, WHMActions.Tetragrammaton.Range))
            {
                var (mind, det, wd) = context.PlayerStatsService.GetHealingStats(player.Level);
                var healAmount = WHMActions.Tetragrammaton.EstimateHealAmount(mind, det, wd, player.Level);

                var targetName = target.Name?.TextValue ?? "Unknown";
                if (ActionExecutor.ExecuteHealingOgcd(context, WHMActions.Tetragrammaton, target.GameObjectId,
                    target.EntityId, targetName, target.CurrentHp, healAmount))
                {
                    // Reserve target to prevent other handlers (local or remote) from double-healing
                    context.HealingCoordination.TryReserveTarget(
                        target.EntityId, context.PartyCoordinationService, healAmount, WHMActions.Tetragrammaton.ActionId, 0);

                    var sourceNote = isTimelineRaidwide ? $" via {raidwideSource}" : "";
                    context.Debug.PlannedAction = $"Tetragrammaton (preemptive{sourceNote}, severity {spikeSeverity:F2})";
                    context.LogOgcdDecision(
                        targetName,
                        targetHpPercent,
                        "Tetragrammaton",
                        $"Preemptive{sourceNote} - spike imminent (severity {spikeSeverity:F2}, projected {projectedHpPercent:P0})");

                    // Training mode: capture explanation
                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        var source = isTimelineRaidwide ? $"timeline ({raidwideSource})" :
                                     isPredictedSpike ? $"pattern (confidence {patternConfidence:P0})" :
                                     "reactive spike detection";

                        var shortReason = $"Preemptive heal - {targetName} projected to {projectedHpPercent:P0}";

                        var factors = new[]
                        {
                            $"Current HP: {targetHpPercent:P0}",
                            $"Projected HP: {projectedHpPercent:P0}",
                            $"Damage rate: {targetDamageRate:F0} DPS",
                            $"Spike severity: {spikeSeverity:F2}",
                            $"Detection source: {source}",
                        };

                        var alternatives = new[]
                        {
                            "Wait for HP to drop (risky)",
                            "Use GCD heal (slower response)",
                            "Save for bigger emergency",
                        };

                        context.TrainingService.RecordDecision(new ActionExplanation
                        {
                            Timestamp = DateTime.Now,
                            ActionId = WHMActions.Tetragrammaton.ActionId,
                            ActionName = "Tetragrammaton",
                            Category = "Healing",
                            TargetName = targetName,
                            ShortReason = shortReason,
                            DetailedReason = $"Preemptive Tetragrammaton on {targetName}. Current HP is {targetHpPercent:P0} but projected to drop to {projectedHpPercent:P0} based on {source}. Healing NOW prevents emergency later. Spike severity: {spikeSeverity:F2}.",
                            Factors = factors,
                            Alternatives = alternatives,
                            Tip = "Preemptive healing is key to smooth runs - heal BEFORE damage lands when you can predict it!",
                            ConceptId = WhmConcepts.ProactiveHealing,
                            Priority = ExplanationPriority.High,
                        });
                    }

                    return true;
                }
            }

            // If very high severity and target is in danger, consider Benediction
            if (spikeSeverity >= 0.8f && targetHpPercent < 0.5f &&
                ActionValidator.CanExecute(player, context.ActionService, WHMActions.Benediction, config,
                c => c.EnableHealing && c.Healing.EnableBenediction) &&
                DistanceHelper.IsInRange(player, target, WHMActions.Benediction.Range))
            {
                var missingHp = (int)(target.MaxHp - target.CurrentHp);
                var beneTargetName = target.Name?.TextValue ?? "Unknown";
                if (ActionExecutor.ExecuteHealingOgcd(context, WHMActions.Benediction, target.GameObjectId,
                    target.EntityId, beneTargetName, target.CurrentHp, missingHp))
                {
                    // Reserve target to prevent other handlers (local or remote) from double-healing
                    context.HealingCoordination.TryReserveTarget(
                        target.EntityId, context.PartyCoordinationService, missingHp, WHMActions.Benediction.ActionId, 0);

                    var sourceNote = isTimelineRaidwide ? $" via {raidwideSource}" : "";
                    context.Debug.PlannedAction = $"Benediction (preemptive{sourceNote}, critical severity {spikeSeverity:F2})";
                    context.LogOgcdDecision(
                        beneTargetName,
                        targetHpPercent,
                        "Benediction",
                        $"Preemptive{sourceNote} - critical spike (severity {spikeSeverity:F2}, target at {targetHpPercent:P0})");

                    // Training mode: capture explanation
                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        var source = isTimelineRaidwide ? $"timeline ({raidwideSource})" :
                                     isPredictedSpike ? $"pattern (confidence {patternConfidence:P0})" :
                                     "reactive spike detection";

                        var shortReason = $"Critical preemptive - {beneTargetName} at {targetHpPercent:P0}, severity {spikeSeverity:F2}";

                        var factors = new[]
                        {
                            $"Current HP: {targetHpPercent:P0}",
                            $"Missing HP: {missingHp:N0}",
                            $"Spike severity: {spikeSeverity:F2} (critical!)",
                            $"Detection source: {source}",
                            "Target below 50% with severe spike incoming",
                        };

                        context.TrainingService.RecordDecision(new ActionExplanation
                        {
                            Timestamp = DateTime.Now,
                            ActionId = WHMActions.Benediction.ActionId,
                            ActionName = "Benediction",
                            Category = "Emergency Healing",
                            TargetName = beneTargetName,
                            ShortReason = shortReason,
                            DetailedReason = $"Critical preemptive Benediction on {beneTargetName}. HP is already at {targetHpPercent:P0} with a severity {spikeSeverity:F2} spike incoming via {source}. Using Benediction now to ensure survival.",
                            Factors = factors,
                            Alternatives = new[] { "Tetragrammaton (insufficient for critical spike)", "GCD heal (too slow)" },
                            Tip = "When spike severity is very high (>0.8) and HP is low, don't hesitate to use Benediction preemptively!",
                            ConceptId = WhmConcepts.ProactiveHealing,
                            Priority = ExplanationPriority.Critical,
                        });
                    }

                    return true;
                }
            }
        }

        // Fall back to GCD heals if no oGCD available and not moving
        if (context.CanExecuteGcd && !isMoving)
        {
            var hasRegen = StatusHelper.HasRegenActive(target, out var regenRemaining);
            var isInMpConservation = context.MpForecastService.IsInConservationMode;

            var (action, healAmount) = context.HealingSpellSelector.SelectBestSingleHeal(
                player, target, false, context.HasFreecure, hasRegen, regenRemaining, isInMpConservation);

            if (action is not null)
            {
                context.HpPredictionService.RegisterPendingHeal(target.EntityId, healAmount);

                var gcdTargetName = target.Name?.TextValue ?? "Unknown";
                if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
                {
                    // Reserve target to prevent other handlers (local or remote) from double-healing
                    var castTimeMs = (int)(action.CastTime * 1000);
                    context.HealingCoordination.TryReserveTarget(
                        target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, castTimeMs);

                    var thinAirNote = context.HasThinAir ? " + Thin Air" : "";
                    var sourceNote = isTimelineRaidwide ? $" via {raidwideSource}" : "";
                    context.Debug.PlannedAction = $"{action.Name} (preemptive{sourceNote}){thinAirNote}";
                    context.Debug.PlanningState = "Preemptive Heal";

                    context.LogHealDecision(
                        gcdTargetName,
                        targetHpPercent,
                        action.Name,
                        healAmount,
                        $"Preemptive{sourceNote} - spike severity {spikeSeverity:F2}, projected {projectedHpPercent:P0}{thinAirNote}");

                    // Training mode: capture explanation
                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        var source = isTimelineRaidwide ? $"timeline ({raidwideSource})" :
                                     isPredictedSpike ? $"pattern (confidence {patternConfidence:P0})" :
                                     "reactive spike detection";

                        var shortReason = $"Preemptive {action.Name} - {gcdTargetName} projected to {projectedHpPercent:P0}";

                        var factors = new[]
                        {
                            $"Current HP: {targetHpPercent:P0}",
                            $"Projected HP: {projectedHpPercent:P0}",
                            $"Heal amount: {healAmount:N0}",
                            $"Spike severity: {spikeSeverity:F2}",
                            $"Detection source: {source}",
                            context.HasThinAir ? "Thin Air active (free cast!)" : $"MP cost: {action.MpCost}",
                        };

                        var alternatives = new[]
                        {
                            "Wait for oGCD cooldowns",
                            "Wait for HP to drop further (risky)",
                            hasRegen ? "Rely on existing Regen" : "Apply Regen first",
                        };

                        context.TrainingService.RecordDecision(new ActionExplanation
                        {
                            Timestamp = DateTime.Now,
                            ActionId = action.ActionId,
                            ActionName = action.Name,
                            Category = "Healing",
                            TargetName = gcdTargetName,
                            ShortReason = shortReason,
                            DetailedReason = $"Preemptive GCD heal on {gcdTargetName}. No oGCD available, so using {action.Name} to heal before the predicted spike. Current HP {targetHpPercent:P0}, projected to drop to {projectedHpPercent:P0} based on {source}.",
                            Factors = factors,
                            Alternatives = alternatives,
                            Tip = "When oGCDs are on cooldown, don't hesitate to use GCD heals preemptively - preventing deaths is worth the DPS loss!",
                            ConceptId = WhmConcepts.ProactiveHealing,
                            Priority = ExplanationPriority.Normal,
                        });
                    }

                    return true;
                }
                else
                {
                    context.HpPredictionService.ClearPendingHeals();
                }
            }
        }

        return false;
    }
}
