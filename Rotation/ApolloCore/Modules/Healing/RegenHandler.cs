using Olympus.Data;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;

namespace Olympus.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Handles Regen HoT maintenance with tank priority.
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

        var target = context.PartyHelper.FindRegenTarget(player, GameConstants.RegenHpThreshold, GameConstants.RegenRefreshThreshold);
        if (target is null)
            return false;

        if (isMoving && WHMActions.Regen.CastTime > 0)
            return false;

        if (ActionExecutor.ExecuteGcd(context, WHMActions.Regen, target.GameObjectId,
            target.Name?.TextValue ?? "Unknown", target.CurrentHp, "Regen",
            appendThinAirNote: false))
        {
            context.Debug.PlannedAction = "Regen (tank priority)";
            return true;
        }

        return false;
    }
}
