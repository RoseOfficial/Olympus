using Olympus.Data;
using Olympus.Rotation.AthenaCore.Context;

namespace Olympus.Rotation.AthenaCore.Modules;

/// <summary>
/// Handles resurrection logic for Scholar.
/// Uses Resurrection to revive fallen party members.
/// </summary>
public sealed class ResurrectionModule : IAthenaModule
{
    public int Priority => 5; // Very high priority - dead members are useless
    public string Name => "Resurrection";

    public bool TryExecute(AthenaContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Resurrection.EnableRaise)
            return false;

        // Can't raise while moving (8s cast time)
        if (isMoving)
            return false;

        // Need GCD available
        if (!context.CanExecuteGcd)
            return false;

        if (player.Level < SCHActions.Resurrection.MinLevel)
            return false;

        // Check MP cost (2400 MP)
        if (player.CurrentMp < SCHActions.Resurrection.MpCost)
        {
            context.Debug.RaiseState = "Insufficient MP";
            return false;
        }

        // Find dead party member
        var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
        if (deadMember == null)
        {
            context.Debug.RaiseState = "No dead members";
            return false;
        }

        // Check Swiftcast availability
        var hasSwiftcast = context.StatusHelper.HasSwiftcast(player);
        if (!hasSwiftcast && !config.Resurrection.AllowHardcastRaise)
        {
            context.Debug.RaiseState = "Waiting for Swiftcast";
            return false;
        }

        // Use Swiftcast first if available and not already active
        if (!hasSwiftcast && context.CanExecuteOgcd)
        {
            if (context.ActionService.IsActionReady(SCHActions.Swiftcast.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(SCHActions.Swiftcast, player.GameObjectId))
                {
                    context.Debug.PlannedAction = "Swiftcast";
                    context.Debug.RaiseState = "Swiftcast for Raise";
                    return true;
                }
            }
        }

        // Execute Resurrection
        var action = SCHActions.Resurrection;
        if (context.ActionService.ExecuteGcd(action, deadMember.GameObjectId))
        {
            var swiftNote = hasSwiftcast ? " (Swift)" : "";
            context.Debug.PlannedAction = action.Name + swiftNote;
            context.Debug.RaiseState = "Raising";
            context.Debug.RaiseTarget = deadMember.Name?.TextValue ?? "Unknown";
            return true;
        }

        return false;
    }

    public void UpdateDebugState(AthenaContext context)
    {
        // Check for dead party members
        var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(context.Player);
        if (deadMember != null)
        {
            context.Debug.RaiseState = "Dead member found";
            context.Debug.RaiseTarget = deadMember.Name?.TextValue ?? "Unknown";
        }
        else
        {
            context.Debug.RaiseState = "None needed";
            context.Debug.RaiseTarget = "";
        }
    }
}
