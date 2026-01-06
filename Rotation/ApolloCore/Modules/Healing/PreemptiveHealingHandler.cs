using System;
using Olympus.Data;
using Olympus.Models;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;

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

        // Two-pronged spike detection:
        // 1. Reactive: Check if a damage spike is currently imminent
        // 2. Predictive: Check if a periodic spike pattern predicts incoming damage

        var isReactiveSpike = context.DamageTrendService.IsDamageSpikeImminent(0.7f);
        var isPredictedSpike = false;
        var patternConfidence = 0f;
        var secondsUntilPredictedSpike = float.MaxValue;

        // Check pattern prediction for each party member
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

        // Only proceed if either reactive or predictive spike is detected
        if (!isReactiveSpike && !isPredictedSpike)
            return false;

        // Get spike severity to determine urgency
        var avgPartyHpPercent = context.PartyHealthMetrics.avgHpPercent;
        var spikeSeverity = context.DamageTrendService.GetSpikeSeverity(avgPartyHpPercent);

        // Boost severity for high-confidence predictions
        if (isPredictedSpike && patternConfidence >= 0.8f)
        {
            spikeSeverity = Math.Max(spikeSeverity, 0.5f + (patternConfidence * 0.3f));
        }

        // Only act on significant spikes (severity > 0.4)
        if (spikeSeverity < 0.4f)
            return false;

        // Find the most endangered target - use damage trend to identify who's taking the hit
        var target = context.PartyHelper.FindMostEndangeredPartyMember(
            player, context.DamageIntakeService, 0, context.DamageTrendService);

        if (target is null)
            return false;

        // Skip if another handler is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId))
            return false;

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
        var pendingHeals = context.HpPredictionService.GetPendingHealAmount(target.EntityId);
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

                if (ActionExecutor.ExecuteHealingOgcd(context, WHMActions.Tetragrammaton, target.GameObjectId,
                    target.EntityId, target.Name?.TextValue ?? "Unknown", target.CurrentHp, healAmount))
                {
                    // Reserve target to prevent other handlers from double-healing
                    context.HealingCoordination.TryReserveTarget(target.EntityId);

                    context.Debug.PlannedAction = $"Tetragrammaton (preemptive, spike severity {spikeSeverity:F2})";
                    context.LogOgcdDecision(
                        target.Name?.TextValue ?? "Unknown",
                        targetHpPercent,
                        "Tetragrammaton",
                        $"Preemptive - spike imminent (severity {spikeSeverity:F2}, projected {projectedHpPercent:P0})");
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
                if (ActionExecutor.ExecuteHealingOgcd(context, WHMActions.Benediction, target.GameObjectId,
                    target.EntityId, target.Name?.TextValue ?? "Unknown", target.CurrentHp, missingHp))
                {
                    // Reserve target to prevent other handlers from double-healing
                    context.HealingCoordination.TryReserveTarget(target.EntityId);

                    context.Debug.PlannedAction = $"Benediction (preemptive, critical spike severity {spikeSeverity:F2})";
                    context.LogOgcdDecision(
                        target.Name?.TextValue ?? "Unknown",
                        targetHpPercent,
                        "Benediction",
                        $"Preemptive - critical spike (severity {spikeSeverity:F2}, target at {targetHpPercent:P0})");
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

                if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
                {
                    // Reserve target to prevent other handlers from double-healing
                    context.HealingCoordination.TryReserveTarget(target.EntityId);

                    var thinAirNote = context.HasThinAir ? " + Thin Air" : "";
                    context.Debug.PlannedAction = $"{action.Name} (preemptive){thinAirNote}";
                    context.Debug.PlanningState = "Preemptive Heal";
                    context.ActionTracker.LogAttempt(action.ActionId, target.Name?.TextValue ?? "Unknown",
                        target.CurrentHp, ActionResult.Success, player.Level);

                    context.LogHealDecision(
                        target.Name?.TextValue ?? "Unknown",
                        targetHpPercent,
                        action.Name,
                        healAmount,
                        $"Preemptive - spike severity {spikeSeverity:F2}, projected {projectedHpPercent:P0}{thinAirNote}");
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
