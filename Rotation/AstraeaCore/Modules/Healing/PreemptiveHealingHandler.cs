using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

/// <summary>
/// AST preemptive heal — fires before a detected spike lands. Uses the
/// shared <see cref="PreemptiveSpikeDetectionHelper"/> so timeline,
/// reactive, and predictive spike signals match WHM behavior exactly.
/// Prefers Celestial Intersection (instant oGCD, 2 charges) and falls
/// back to Aspected Benefic (instant GCD with regen) when no oGCD is
/// available. Runs at oGCD priority 5 so it precedes Essential Dignity.
/// </summary>
public sealed class PreemptiveHealingHandler : IHealingHandler
{
    public int Priority => 5;
    public string Name => "PreemptiveHeal";

    private static readonly string[] _intersectionAlternatives =
    {
        "Wait for HP to drop (risky — spike incoming)",
        "Essential Dignity (save for emergency)",
        "Aspected Benefic (GCD, slower)",
    };

    private static readonly string[] _aspectedBeneficAlternatives =
    {
        "Wait for oGCD cooldowns",
        "Wait for HP to drop further (risky)",
        "Rely on existing Aspected Benefic regen",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing)
            return false;

        var detection = PreemptiveSpikeDetectionHelper.Detect(
            player,
            config.Healing,
            context.PartyHelper,
            context.DamageIntakeService,
            context.DamageTrendService,
            context.HpPredictionService,
            context.ShieldTrackingService,
            context.CoHealerDetectionService,
            context.TimelineService,
            context.BossMechanicDetector,
            context.PartyHealthMetrics.avgHpPercent);

        if (detection is null)
            return false;

        var target = detection.Value.Target;
        var spikeSeverity = detection.Value.Severity;
        var raidwideSource = detection.Value.Source;
        var isTimelineRaidwide = detection.Value.IsTimelineRaidwide;
        var isPredictedSpike = !isTimelineRaidwide && detection.Value.PatternConfidence > 0;
        var patternConfidence = detection.Value.PatternConfidence;
        var targetHpPercent = detection.Value.TargetHpPercent;
        var projectedHpPercent = detection.Value.ProjectedHpPercent;

        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        if (CoHealerAwarenessHelper.CoHealerWillCover(
                config.Healing.EnableCoHealerAwareness,
                context.CoHealerDetectionService,
                target,
                config.Healing.CoHealerPendingHealThreshold))
            return false;

        // oGCD: Celestial Intersection (instant, tank-shield aware, 2 charges)
        if (context.CanExecuteOgcd &&
            config.Astrologian.EnableCelestialIntersection &&
            player.Level >= ASTActions.CelestialIntersection.MinLevel &&
            context.ActionService.IsActionReady(ASTActions.CelestialIntersection.ActionId))
        {
            var action = ASTActions.CelestialIntersection;
            var targetName = target.Name?.TextValue ?? "Unknown";
            var healAmount = action.HealPotency * 10;

            if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
            {
                context.HealingCoordination.TryReserveTarget(
                    target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, 0);

                var sourceNote = isTimelineRaidwide ? $" via {raidwideSource}" : "";
                context.Debug.PlannedAction = $"Celestial Intersection (preemptive{sourceNote}, severity {spikeSeverity:F2})";
                context.Debug.CelestialIntersectionState = "Used (preemptive)";
                context.LogHealDecision(
                    targetName,
                    targetHpPercent,
                    action.Name,
                    healAmount,
                    $"Preemptive{sourceNote} - spike imminent (severity {spikeSeverity:F2}, projected {projectedHpPercent:P0})");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var source = isTimelineRaidwide ? $"timeline ({raidwideSource})" :
                                 isPredictedSpike ? $"pattern (confidence {patternConfidence:P0})" :
                                 "reactive spike detection";

                    var isTank = JobRegistry.IsTank(target.ClassJob.RowId);

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Celestial Intersection",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = $"Preemptive Intersection - {targetName} projected to {projectedHpPercent:P0}",
                        DetailedReason = $"Celestial Intersection on {targetName} ahead of a predicted spike. HP is {targetHpPercent:P0} but projected to drop to {projectedHpPercent:P0} based on {source}. {(isTank ? "Tank receives 400 potency shield — great against incoming tankbusters." : "Non-tank receives 200 potency heal plus regen.")} Instant oGCD means zero GCD cost.",
                        Factors = new[]
                        {
                            $"Current HP: {targetHpPercent:P0}",
                            $"Projected HP: {projectedHpPercent:P0}",
                            $"Spike severity: {spikeSeverity:F2}",
                            $"Detection source: {source}",
                            isTank ? "Tank target (400 potency shield)" : "Non-tank (heal + regen)",
                        },
                        Alternatives = _intersectionAlternatives,
                        Tip = "Celestial Intersection is the ideal preemptive oGCD - instant, two charges, and the shield on tanks mitigates the spike directly.",
                        ConceptId = AstConcepts.ProactiveHealing,
                        Priority = ExplanationPriority.High,
                    });
                }

                return true;
            }
        }

        // GCD fallback: Aspected Benefic (instant cast, heal + regen)
        if (context.CanExecuteGcd &&
            config.Astrologian.EnableAspectedBenefic &&
            player.Level >= ASTActions.AspectedBenefic.MinLevel)
        {
            var action = ASTActions.AspectedBenefic;
            var targetName = target.Name?.TextValue ?? "Unknown";
            var healAmount = action.HealPotency * 10;

            if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
            {
                var castTimeMs = (int)(action.CastTime * 1000);
                context.HealingCoordination.TryReserveTarget(
                    target.EntityId, context.PartyCoordinationService, healAmount, action.ActionId, castTimeMs);

                var sourceNote = isTimelineRaidwide ? $" via {raidwideSource}" : "";
                context.Debug.PlannedAction = $"Aspected Benefic (preemptive{sourceNote})";
                context.Debug.SingleHealState = "Preemptive Aspected Benefic";
                context.LogHealDecision(
                    targetName,
                    targetHpPercent,
                    action.Name,
                    healAmount,
                    $"Preemptive{sourceNote} - spike severity {spikeSeverity:F2}, projected {projectedHpPercent:P0}");

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var source = isTimelineRaidwide ? $"timeline ({raidwideSource})" :
                                 isPredictedSpike ? $"pattern (confidence {patternConfidence:P0})" :
                                 "reactive spike detection";

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = action.ActionId,
                        ActionName = "Aspected Benefic",
                        Category = "Healing",
                        TargetName = targetName,
                        ShortReason = $"Preemptive Aspected Benefic - {targetName} projected to {projectedHpPercent:P0}",
                        DetailedReason = $"Preemptive GCD heal on {targetName}. Celestial Intersection unavailable, so Aspected Benefic covers the spike instead — instant cast with a 15s regen tail. Current HP {targetHpPercent:P0}, projected to drop to {projectedHpPercent:P0} based on {source}.",
                        Factors = new[]
                        {
                            $"Current HP: {targetHpPercent:P0}",
                            $"Projected HP: {projectedHpPercent:P0}",
                            $"Spike severity: {spikeSeverity:F2}",
                            $"Detection source: {source}",
                            "Instant cast (movement-safe)",
                        },
                        Alternatives = _aspectedBeneficAlternatives,
                        Tip = "When Celestial Intersection is on cooldown, Aspected Benefic's instant cast makes it the next-best preemptive tool.",
                        ConceptId = AstConcepts.ProactiveHealing,
                        Priority = ExplanationPriority.Normal,
                    });
                }

                return true;
            }
        }

        return false;
    }
}
