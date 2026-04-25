using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services.Training;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles emergency full heal with Benediction.
/// Two-tier logic: emergency (below threshold) and proactive (heavy damage).
/// </summary>
public sealed class BenedictionHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.Benediction;
    public string Name => "Benediction";

    public void CollectCandidates(IApolloContext context, RotationScheduler scheduler, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.Benediction, config,
            c => c.EnableHealing && c.Healing.EnableBenediction))
            return;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target is null) return;
        if (HealerPartyHelper.HasNoHealStatus(target)) return;
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService)) return;

        var hpPercent = context.PartyHelper.GetHpPercent(target);
        var targetDamageRate = context.DamageIntakeService.GetDamageRate(target.EntityId, 3f);

        var baseEmergencyThreshold = config.Healing.BenedictionEmergencyThreshold;
        var emergencyThreshold = targetDamageRate switch
        {
            > 800f => Math.Min(baseEmergencyThreshold + 0.20f, 0.50f),
            > 500f => Math.Min(baseEmergencyThreshold + 0.10f, 0.50f),
            _ => baseEmergencyThreshold
        };
        var isEmergency = hpPercent < emergencyThreshold;

        var isProactive = !isEmergency &&
            config.Healing.EnableProactiveBenediction &&
            hpPercent < config.Healing.ProactiveBenedictionHpThreshold &&
            targetDamageRate >= config.Healing.ProactiveBenedictionDamageRate;

        if (!isEmergency && !isProactive) return;
        if (!DistanceHelper.IsInRange(player, target, WHMActions.Benediction.Range)) return;

        var missingHp = (int)(target.MaxHp - target.CurrentHp);
        var capturedTarget = target;
        var capturedEmergencyThreshold = emergencyThreshold;
        var capturedIsEmergency = isEmergency;
        var capturedTargetDamageRate = targetDamageRate;
        var capturedHpPercent = hpPercent;

        scheduler.PushOgcd(ApolloAbilities.Benediction, target.GameObjectId, priority: (int)Priority,
            onDispatched: _ =>
            {
                context.HealingCoordination.TryReserveTarget(
                    capturedTarget.EntityId, context.PartyCoordinationService, missingHp, WHMActions.Benediction.ActionId, 0);
                context.HpPredictionService.RegisterPendingHeal(capturedTarget.EntityId, missingHp);

                var thresholdInfo = capturedEmergencyThreshold > baseEmergencyThreshold
                    ? $", threshold escalated to {capturedEmergencyThreshold:P0}"
                    : "";
                var reason = capturedIsEmergency
                    ? $"emergency, {capturedHpPercent:P0} HP{thresholdInfo}, DPS {capturedTargetDamageRate:F0}"
                    : $"proactive, {capturedHpPercent:P0} HP, DPS {capturedTargetDamageRate:F0}";
                context.Debug.PlannedAction = $"Benediction ({reason})";

                var logReason = capturedIsEmergency
                    ? $"Emergency (below {capturedEmergencyThreshold:P0} threshold, DPS {capturedTargetDamageRate:F0})"
                    : $"Proactive (damage rate {capturedTargetDamageRate:F0} DPS)";
                context.LogOgcdDecision(
                    capturedTarget.Name?.TextValue ?? "Unknown",
                    capturedHpPercent,
                    "Benediction",
                    logReason);

                if (context.TrainingService?.IsTrainingEnabled == true)
                {
                    var targetName = capturedTarget.Name?.TextValue ?? "Unknown";
                    var shortReason = capturedIsEmergency
                        ? $"Emergency heal - {targetName} at {capturedHpPercent:P0}"
                        : $"Proactive heal - {targetName} taking {capturedTargetDamageRate:F0} DPS";

                    var factors = new[]
                    {
                        $"Target HP: {capturedHpPercent:P0}",
                        $"Missing HP: {missingHp:N0}",
                        $"Damage intake: {capturedTargetDamageRate:F0} DPS",
                        capturedIsEmergency
                            ? $"Below emergency threshold ({capturedEmergencyThreshold:P0})"
                            : $"Above damage rate threshold ({config.Healing.ProactiveBenedictionDamageRate} DPS)",
                    };

                    var alternatives = capturedIsEmergency
                        ? new[] { "Tetragrammaton (but smaller heal)", "Cure II (but GCD)" }
                        : new[] { "Wait for HP to drop further", "Use smaller heal to conserve Benediction" };

                    var tip = capturedIsEmergency
                        ? "Benediction is your emergency button - don't hesitate when HP is critical!"
                        : "Using Benediction proactively on heavy damage prevents emergencies.";

                    context.TrainingService.RecordDecision(new ActionExplanation
                    {
                        Timestamp = DateTime.UtcNow,
                        ActionId = WHMActions.Benediction.ActionId,
                        ActionName = "Benediction",
                        Category = "Emergency Healing",
                        TargetName = targetName,
                        ShortReason = shortReason,
                        DetailedReason = $"Benediction instantly restores target to full HP. Used on {targetName} who was at {capturedHpPercent:P0} HP with {capturedTargetDamageRate:F0} damage per second intake. This {(capturedIsEmergency ? "emergency" : "proactive")} usage ensures the target survives.",
                        Factors = factors,
                        Alternatives = alternatives,
                        Tip = tip,
                        ConceptId = capturedIsEmergency ? WhmConcepts.EmergencyHealing : WhmConcepts.BenedictionUsage,
                        Priority = capturedIsEmergency ? ExplanationPriority.Critical : ExplanationPriority.High,
                    });

                    var masteryConceptId = capturedIsEmergency ? WhmConcepts.EmergencyHealing : WhmConcepts.BenedictionUsage;
                    var masteryReason = capturedIsEmergency
                        ? $"Used emergency heal on critical target ({capturedHpPercent:P0} HP)"
                        : $"Used proactive heal on high-damage target ({capturedTargetDamageRate:F0} DPS)";
                    context.TrainingService.RecordConceptApplication(masteryConceptId, wasSuccessful: true, masteryReason);
                }
            });
    }
}
