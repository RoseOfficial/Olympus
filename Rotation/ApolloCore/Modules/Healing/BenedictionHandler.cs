using System;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
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

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.Benediction, config,
            c => c.EnableHealing && c.Healing.EnableBenediction))
            return false;

        var target = context.PartyHelper.FindLowestHpPartyMember(player);
        if (target is null)
            return false;

        // Skip if another handler (local or remote Olympus instance) is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId, context.PartyCoordinationService))
            return false;

        var hpPercent = context.PartyHelper.GetHpPercent(target);

        // Get current damage rate for dynamic threshold calculation
        var targetDamageRate = context.DamageIntakeService.GetDamageRate(target.EntityId, 3f);

        // Two-tier Benediction logic:
        // 1. Emergency: Always use if below emergency threshold (default 30%)
        //    Threshold escalates based on damage rate:
        //    - Very high damage (>800 DPS): +20% threshold
        //    - High damage (>500 DPS): +10% threshold
        // 2. Proactive: Use at higher threshold (default 60%) if taking heavy damage
        var baseEmergencyThreshold = config.Healing.BenedictionEmergencyThreshold;
        var emergencyThreshold = targetDamageRate switch
        {
            > 800f => Math.Min(baseEmergencyThreshold + 0.20f, 0.50f),  // Cap at 50%
            > 500f => Math.Min(baseEmergencyThreshold + 0.10f, 0.50f),
            _ => baseEmergencyThreshold
        };
        var isEmergency = hpPercent < emergencyThreshold;

        // Proactive: Use at higher threshold if taking heavy damage (already calculated rate above)
        var isProactive = !isEmergency &&
            config.Healing.EnableProactiveBenediction &&
            hpPercent < config.Healing.ProactiveBenedictionHpThreshold &&
            targetDamageRate >= config.Healing.ProactiveBenedictionDamageRate;

        if (!isEmergency && !isProactive)
            return false;

        if (!DistanceHelper.IsInRange(player, target, WHMActions.Benediction.Range))
            return false;

        var missingHp = (int)(target.MaxHp - target.CurrentHp);
        if (ActionExecutor.ExecuteHealingOgcd(context, WHMActions.Benediction, target.GameObjectId,
            target.EntityId, target.Name?.TextValue ?? "Unknown", target.CurrentHp, missingHp))
        {
            // Reserve target to prevent other handlers (local or remote) from double-healing
            context.HealingCoordination.TryReserveTarget(
                target.EntityId, context.PartyCoordinationService, missingHp, WHMActions.Benediction.ActionId, 0);

            // Update debug info with reason
            var thresholdInfo = emergencyThreshold > baseEmergencyThreshold
                ? $", threshold escalated to {emergencyThreshold:P0}"
                : "";
            var reason = isEmergency
                ? $"emergency, {hpPercent:P0} HP{thresholdInfo}, DPS {targetDamageRate:F0}"
                : $"proactive, {hpPercent:P0} HP, DPS {targetDamageRate:F0}";
            context.Debug.PlannedAction = $"Benediction ({reason})";

            // Log the decision
            var logReason = isEmergency
                ? $"Emergency (below {emergencyThreshold:P0} threshold, DPS {targetDamageRate:F0})"
                : $"Proactive (damage rate {targetDamageRate:F0} DPS)";
            context.LogOgcdDecision(
                target.Name?.TextValue ?? "Unknown",
                hpPercent,
                "Benediction",
                logReason);

            // Training mode: capture explanation
            if (context.TrainingService?.IsTrainingEnabled == true)
            {
                var targetName = target.Name?.TextValue ?? "Unknown";
                var shortReason = isEmergency
                    ? $"Emergency heal - {targetName} at {hpPercent:P0}"
                    : $"Proactive heal - {targetName} taking {targetDamageRate:F0} DPS";

                var factors = new[]
                {
                    $"Target HP: {hpPercent:P0}",
                    $"Missing HP: {missingHp:N0}",
                    $"Damage intake: {targetDamageRate:F0} DPS",
                    isEmergency
                        ? $"Below emergency threshold ({emergencyThreshold:P0})"
                        : $"Above damage rate threshold ({config.Healing.ProactiveBenedictionDamageRate} DPS)",
                };

                var alternatives = isEmergency
                    ? new[] { "Tetragrammaton (but smaller heal)", "Cure II (but GCD)" }
                    : new[] { "Wait for HP to drop further", "Use smaller heal to conserve Benediction" };

                var tip = isEmergency
                    ? "Benediction is your emergency button - don't hesitate when HP is critical!"
                    : "Using Benediction proactively on heavy damage prevents emergencies.";

                context.TrainingService.RecordDecision(new ActionExplanation
                {
                    Timestamp = DateTime.Now,
                    ActionId = WHMActions.Benediction.ActionId,
                    ActionName = "Benediction",
                    Category = "Emergency Healing",
                    TargetName = targetName,
                    ShortReason = shortReason,
                    DetailedReason = $"Benediction instantly restores target to full HP. Used on {targetName} who was at {hpPercent:P0} HP with {targetDamageRate:F0} damage per second intake. This {(isEmergency ? "emergency" : "proactive")} usage ensures the target survives.",
                    Factors = factors,
                    Alternatives = alternatives,
                    Tip = tip,
                    ConceptId = isEmergency ? WhmConcepts.EmergencyHealing : WhmConcepts.BenedictionUsage,
                    Priority = isEmergency ? ExplanationPriority.Critical : ExplanationPriority.High,
                });

                // Record concept mastery: successful application of emergency healing
                // v3.28.0: Track mastery through opportunities and successes
                var masteryConceptId = isEmergency ? WhmConcepts.EmergencyHealing : WhmConcepts.BenedictionUsage;
                var masteryReason = isEmergency
                    ? $"Used emergency heal on critical target ({hpPercent:P0} HP)"
                    : $"Used proactive heal on high-damage target ({targetDamageRate:F0} DPS)";
                context.TrainingService.RecordConceptApplication(masteryConceptId, wasSuccessful: true, masteryReason);
            }

            return true;
        }

        return false;
    }
}
