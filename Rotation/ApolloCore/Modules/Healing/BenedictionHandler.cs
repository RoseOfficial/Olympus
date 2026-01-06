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

        var hpPercent = context.PartyHelper.GetHpPercent(target);

        // Two-tier Benediction logic:
        // 1. Emergency: Always use if below emergency threshold (default 30%)
        // 2. Proactive: Use at higher threshold (default 60%) if taking heavy damage
        var isEmergency = hpPercent < config.Healing.BenedictionEmergencyThreshold;

        var isProactive = false;
        var targetDamageRate = 0f;
        if (!isEmergency && config.Healing.EnableProactiveBenediction &&
            hpPercent < config.Healing.ProactiveBenedictionHpThreshold)
        {
            // Check if target is taking sustained heavy damage
            targetDamageRate = context.DamageIntakeService.GetDamageRate(target.EntityId, 3f);
            isProactive = targetDamageRate >= config.Healing.ProactiveBenedictionDamageRate;
        }

        if (!isEmergency && !isProactive)
            return false;

        if (!DistanceHelper.IsInRange(player, target, WHMActions.Benediction.Range))
            return false;

        var missingHp = (int)(target.MaxHp - target.CurrentHp);
        if (ActionExecutor.ExecuteHealingOgcd(context, WHMActions.Benediction, target.GameObjectId,
            target.EntityId, target.Name?.TextValue ?? "Unknown", target.CurrentHp, missingHp))
        {
            // Update debug info with reason
            var reason = isEmergency
                ? $"emergency, {hpPercent:P0} HP"
                : $"proactive, {hpPercent:P0} HP, DPS {targetDamageRate:F0}";
            context.Debug.PlannedAction = $"Benediction ({reason})";

            // Log the decision
            context.LogOgcdDecision(
                target.Name?.TextValue ?? "Unknown",
                hpPercent,
                "Benediction",
                isEmergency ? "Emergency (below threshold)" : $"Proactive (damage rate {targetDamageRate:F0} DPS)");

            return true;
        }

        return false;
    }
}
