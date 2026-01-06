using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles Regen HoT maintenance with tank priority.
/// Supports dynamic threshold based on damage rate.
/// </summary>
public sealed class RegenHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.Regen;
    public string Name => "Regen";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Healing.EnableRegen)
            return false;

        if (player.Level < WHMActions.Regen.MinLevel)
            return false;

        // Calculate dynamic Regen threshold based on party damage state
        var regenHpThreshold = GetDynamicRegenThreshold(context);

        var target = context.PartyHelper.FindRegenTarget(player, regenHpThreshold, GameConstants.RegenRefreshThreshold);
        if (target is null)
            return false;

        // Skip if another handler is already healing this target
        if (context.HealingCoordination.IsTargetReserved(target.EntityId))
            return false;

        if (isMoving && WHMActions.Regen.CastTime > 0)
            return false;

        if (ActionExecutor.ExecuteGcd(context, WHMActions.Regen, target.GameObjectId,
            target.Name?.TextValue ?? "Unknown", target.CurrentHp, "Regen",
            appendThinAirNote: false))
        {
            // Reserve target to prevent other handlers from double-healing
            context.HealingCoordination.TryReserveTarget(target.EntityId);

            var thresholdNote = regenHpThreshold > GameConstants.RegenHpThreshold
                ? $" (dynamic {regenHpThreshold:P0})"
                : "";
            context.Debug.PlannedAction = $"Regen (tank priority{thresholdNote})";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Calculates the dynamic Regen HP threshold based on damage patterns.
    /// During high-damage phases, Regen is applied at a higher threshold.
    /// </summary>
    private static float GetDynamicRegenThreshold(ApolloContext context)
    {
        var config = context.Configuration;

        // If dynamic threshold disabled, use default
        if (!config.Healing.EnableDynamicRegenThreshold)
            return GameConstants.RegenHpThreshold;

        // Check if anyone is taking high damage
        var partyDamageRate = context.DamageIntakeService.GetPartyDamageRate(3f);

        // If party is taking significant damage, use higher threshold
        if (partyDamageRate >= config.Healing.RegenHighDamageDpsThreshold)
            return config.Healing.RegenHighDamageThreshold;

        return GameConstants.RegenHpThreshold;
    }
}
