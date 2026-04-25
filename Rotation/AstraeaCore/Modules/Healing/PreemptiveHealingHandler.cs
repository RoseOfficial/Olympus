using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.AstraeaCore.Modules.Healing;

public sealed class PreemptiveHealingHandler : IHealingHandler
{
    public int Priority => 5;
    public string Name => "PreemptiveHeal";

    private static readonly string[] _intersectionAlternatives =
    {
        "Wait for HP to drop (risky - spike incoming)",
        "Essential Dignity (save for emergency)",
        "Aspected Benefic (GCD, slower)",
    };

    private static readonly string[] _aspectedBeneficAlternatives =
    {
        "Wait for oGCD cooldowns",
        "Wait for HP to drop further (risky)",
        "Rely on existing Aspected Benefic regen",
    };

    public bool TryExecute(IAstraeaContext context, bool isMoving) => false;

    public void CollectCandidates(IAstraeaContext context, RotationScheduler scheduler, bool isMoving)
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

        // oGCD: Celestial Intersection
        if (config.Astrologian.EnableCelestialIntersection &&
            player.Level >= ASTActions.CelestialIntersection.MinLevel &&
            context.ActionService.IsActionReady(ASTActions.CelestialIntersection.ActionId))
        {
            var ciAction = ASTActions.CelestialIntersection;
            var ciHealAmount = ciAction.HealPotency * 10;
            var capturedTarget = target;
            var capturedHealAmount = ciHealAmount;
            var capturedSpikeSeverity = spikeSeverity;
            var capturedIsTimelineRaidwide = isTimelineRaidwide;
            var capturedIsPredictedSpike = isPredictedSpike;
            var capturedPatternConfidence = patternConfidence;
            var capturedRaidwideSource = raidwideSource;
            var capturedTargetHpPercent = targetHpPercent;
            var capturedProjectedHpPercent = projectedHpPercent;

            scheduler.PushOgcd(AstraeaAbilities.CelestialIntersection, target.GameObjectId, priority: Priority,
                onDispatched: _ =>
                {
                    context.HealingCoordination.TryReserveTarget(
                        capturedTarget.EntityId, context.PartyCoordinationService, capturedHealAmount, ciAction.ActionId, 0);

                    var sourceNote = capturedIsTimelineRaidwide ? $" via {capturedRaidwideSource}" : "";
                    context.Debug.PlannedAction = $"Celestial Intersection (preemptive{sourceNote}, severity {capturedSpikeSeverity:F2})";
                    context.Debug.CelestialIntersectionState = "Used (preemptive)";
                    context.LogHealDecision(
                        capturedTarget.Name?.TextValue ?? "Unknown",
                        capturedTargetHpPercent,
                        ciAction.Name,
                        capturedHealAmount,
                        $"Preemptive{sourceNote} - spike imminent (severity {capturedSpikeSeverity:F2}, projected {capturedProjectedHpPercent:P0})");

                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        var source = capturedIsTimelineRaidwide ? $"timeline ({capturedRaidwideSource})" :
                                     capturedIsPredictedSpike ? $"pattern (confidence {capturedPatternConfidence:P0})" :
                                     "reactive spike detection";
                        var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                        var isTank = JobRegistry.IsTank(capturedTarget.ClassJob.RowId);

                        context.TrainingService.RecordDecision(new ActionExplanation
                        {
                            Timestamp = DateTime.UtcNow,
                            ActionId = ciAction.ActionId,
                            ActionName = "Celestial Intersection",
                            Category = "Healing",
                            TargetName = targetName,
                            ShortReason = $"Preemptive Intersection - {targetName} projected to {capturedProjectedHpPercent:P0}",
                            DetailedReason = $"Celestial Intersection on {targetName} ahead of a predicted spike. HP is {capturedTargetHpPercent:P0} but projected to drop to {capturedProjectedHpPercent:P0} based on {source}. {(isTank ? "Tank receives 400 potency shield - great against incoming tankbusters." : "Non-tank receives 200 potency heal plus regen.")} Instant oGCD means zero GCD cost.",
                            Factors = new[]
                            {
                                $"Current HP: {capturedTargetHpPercent:P0}",
                                $"Projected HP: {capturedProjectedHpPercent:P0}",
                                $"Spike severity: {capturedSpikeSeverity:F2}",
                                $"Detection source: {source}",
                                isTank ? "Tank target (400 potency shield)" : "Non-tank (heal + regen)",
                            },
                            Alternatives = _intersectionAlternatives,
                            Tip = "Celestial Intersection is the ideal preemptive oGCD - instant, two charges, and the shield on tanks mitigates the spike directly.",
                            ConceptId = AstConcepts.ProactiveHealing,
                            Priority = ExplanationPriority.High,
                        });
                    }
                });
        }

        // GCD fallback: Aspected Benefic
        if (config.Astrologian.EnableAspectedBenefic &&
            player.Level >= ASTActions.AspectedBenefic.MinLevel)
        {
            var abAction = ASTActions.AspectedBenefic;
            var abHealAmount = abAction.HealPotency * 10;
            var capturedTarget = target;
            var capturedHealAmount = abHealAmount;
            var capturedAction = abAction;
            var capturedSpikeSeverity = spikeSeverity;
            var capturedIsTimelineRaidwide = isTimelineRaidwide;
            var capturedIsPredictedSpike = isPredictedSpike;
            var capturedPatternConfidence = patternConfidence;
            var capturedRaidwideSource = raidwideSource;
            var capturedTargetHpPercent = targetHpPercent;
            var capturedProjectedHpPercent = projectedHpPercent;

            scheduler.PushGcd(AstraeaAbilities.AspectedBenefic, target.GameObjectId, priority: Priority,
                onDispatched: _ =>
                {
                    var castTimeMs = (int)(capturedAction.CastTime * 1000);
                    context.HealingCoordination.TryReserveTarget(
                        capturedTarget.EntityId, context.PartyCoordinationService, capturedHealAmount, capturedAction.ActionId, castTimeMs);

                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var sourceNote = capturedIsTimelineRaidwide ? $" via {capturedRaidwideSource}" : "";
                    context.Debug.PlannedAction = $"Aspected Benefic (preemptive{sourceNote})";
                    context.Debug.SingleHealState = "Preemptive Aspected Benefic";
                    context.LogHealDecision(targetName, capturedTargetHpPercent, capturedAction.Name, capturedHealAmount,
                        $"Preemptive{sourceNote} - spike severity {capturedSpikeSeverity:F2}, projected {capturedProjectedHpPercent:P0}");

                    if (context.TrainingService?.IsTrainingEnabled == true)
                    {
                        var source = capturedIsTimelineRaidwide ? $"timeline ({capturedRaidwideSource})" :
                                     capturedIsPredictedSpike ? $"pattern (confidence {capturedPatternConfidence:P0})" :
                                     "reactive spike detection";

                        context.TrainingService.RecordDecision(new ActionExplanation
                        {
                            Timestamp = DateTime.UtcNow,
                            ActionId = capturedAction.ActionId,
                            ActionName = "Aspected Benefic",
                            Category = "Healing",
                            TargetName = targetName,
                            ShortReason = $"Preemptive Aspected Benefic - {targetName} projected to {capturedProjectedHpPercent:P0}",
                            DetailedReason = $"Preemptive GCD heal on {targetName}. Celestial Intersection unavailable, so Aspected Benefic covers the spike instead - instant cast with a 15s regen tail. Current HP {capturedTargetHpPercent:P0}, projected to drop to {capturedProjectedHpPercent:P0} based on {source}.",
                            Factors = new[]
                            {
                                $"Current HP: {capturedTargetHpPercent:P0}",
                                $"Projected HP: {capturedProjectedHpPercent:P0}",
                                $"Spike severity: {capturedSpikeSeverity:F2}",
                                $"Detection source: {source}",
                                "Instant cast (movement-safe)",
                            },
                            Alternatives = _aspectedBeneficAlternatives,
                            Tip = "When Celestial Intersection is on cooldown, Aspected Benefic's instant cast makes it the next-best preemptive tool.",
                            ConceptId = AstConcepts.ProactiveHealing,
                            Priority = ExplanationPriority.Normal,
                        });
                    }
                });
        }
    }
}
