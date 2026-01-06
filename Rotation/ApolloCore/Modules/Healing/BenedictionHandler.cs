using System;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;

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

        // Skip if another handler is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId))
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
            // Reserve target to prevent other handlers from double-healing
            context.HealingCoordination.TryReserveTarget(target.EntityId);

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

            return true;
        }

        return false;
    }
}
