using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles Assize as a healing oGCD when party needs healing.
/// Balances DPS value against healing value - triggers when multiple party members are injured.
/// </summary>
public sealed class AssizeHealingHandler : IHealingHandler
{
    public HealingPriority Priority => HealingPriority.AssizeHealing;
    public string Name => "AssizeHealing";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        // Check if Assize healing mode is enabled
        if (!config.EnableHealing || !config.Healing.EnableAssizeHealing)
            return false;

        if (player.Level < WHMActions.Assize.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.Assize.ActionId))
            return false;

        // Check party health conditions
        var (avgHpPercent, _, injuredCount) = context.PartyHealthMetrics;

        // Need enough injured targets AND party HP below threshold
        var shouldUseForHealing = injuredCount >= config.Healing.AssizeHealingMinTargets &&
                                  avgHpPercent < config.Healing.AssizeHealingHpThreshold;

        if (!shouldUseForHealing)
            return false;

        // Execute Assize for healing
        if (ActionExecutor.ExecuteOgcd(context, WHMActions.Assize, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentHp,
            $"Assize (healing: {injuredCount} injured, {avgHpPercent:P0} avg HP)"))
        {
            context.Debug.AssizeState = $"Healing mode ({injuredCount} injured, {avgHpPercent:P0} avg)";

            // Log the healing decision
            context.LogOgcdDecision(
                $"{injuredCount} party members",
                avgHpPercent,
                "Assize",
                $"Healing mode - {injuredCount} injured, avg HP {avgHpPercent:P0}");

            return true;
        }

        return false;
    }
}
