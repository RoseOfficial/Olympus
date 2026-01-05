using Olympus.Data;
using Olympus.Models;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Handles resurrection logic for the WHM rotation.
/// Includes Raise (GCD) and Swiftcast for instant raise (oGCD).
/// </summary>
public sealed class ResurrectionModule : IApolloModule
{
    private const int RaiseMpCost = 2400;

    public int Priority => 5; // Very high priority - dead party members are useless
    public string Name => "Resurrection";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        // oGCD: Swiftcast for pending raise
        if (context.CanExecuteOgcd && TrySwiftcastForRaise(context))
            return true;

        // GCD: Execute Raise
        if (context.CanExecuteGcd && TryExecuteRaise(context, isMoving))
        {
            context.Debug.PlanningState = "Raise";
            return true;
        }

        return false;
    }

    public void UpdateDebugState(ApolloContext context)
    {
        // Debug state is updated during execution
    }

    private bool TryExecuteRaise(ApolloContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Resurrection.EnableRaise)
        {
            context.Debug.RaiseState = "Disabled";
            return false;
        }

        if (player.Level < WHMActions.Raise.MinLevel)
        {
            context.Debug.RaiseState = $"Level {player.Level} < 12";
            return false;
        }

        var mpPercent = (float)player.CurrentMp / player.MaxMp;
        if (mpPercent < config.Resurrection.RaiseMpThreshold)
        {
            context.Debug.RaiseState = $"MP {mpPercent:P0} < {config.Resurrection.RaiseMpThreshold:P0}";
            return false;
        }

        if (player.CurrentMp < RaiseMpCost)
        {
            context.Debug.RaiseState = $"MP {player.CurrentMp} < {RaiseMpCost}";
            return false;
        }

        var target = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
        if (target is null)
        {
            context.Debug.RaiseState = "No target";
            context.Debug.RaiseTarget = "None";
            return false;
        }

        var targetName = target.Name?.TextValue ?? "Unknown";
        context.Debug.RaiseTarget = targetName;

        if (context.HasSwiftcast)
        {
            if (ShouldWaitForThinAir(context))
            {
                context.Debug.RaiseState = "Waiting for Thin Air";
                return false;
            }

            context.Debug.RaiseState = "Swiftcast Raise";
            var success = context.ActionService.ExecuteGcd(WHMActions.Raise, target.GameObjectId);
            if (success)
            {
                context.Debug.PlannedAction = context.HasThinAir
                    ? "Raise (Swiftcast + Thin Air)"
                    : "Raise (Swiftcast)";
                context.ActionTracker.LogAttempt(WHMActions.Raise.ActionId, targetName, 0, ActionResult.Success, player.Level);
            }
            return success;
        }

        // Hardcast Raise (if allowed and not moving)
        if (config.Resurrection.AllowHardcastRaise && !isMoving)
        {
            var swiftcastCooldown = context.ActionService.GetCooldownRemaining(WHMActions.Swiftcast.ActionId);

            if (swiftcastCooldown > 10f)
            {
                if (ShouldWaitForThinAir(context))
                {
                    context.Debug.RaiseState = "Waiting for Thin Air";
                    return false;
                }

                context.Debug.RaiseState = "Hardcast Raise";
                var success = context.ActionService.ExecuteGcd(WHMActions.Raise, target.GameObjectId);
                if (success)
                {
                    context.Debug.PlannedAction = context.HasThinAir
                        ? "Raise (Hardcast + Thin Air)"
                        : "Raise (Hardcast)";
                    context.ActionTracker.LogAttempt(WHMActions.Raise.ActionId, targetName, 0, ActionResult.Success, player.Level);
                }
                return success;
            }
            else
            {
                context.Debug.RaiseState = $"Waiting for Swiftcast ({swiftcastCooldown:F1}s)";
            }
        }
        else if (!context.HasSwiftcast && !config.Resurrection.AllowHardcastRaise)
        {
            context.Debug.RaiseState = "No Swiftcast (hardcast disabled)";
        }
        else if (isMoving)
        {
            context.Debug.RaiseState = "Moving (can't hardcast)";
        }

        return false;
    }

    private bool TrySwiftcastForRaise(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Resurrection.EnableRaise)
            return false;

        if (player.Level < WHMActions.Swiftcast.MinLevel)
            return false;

        if (context.HasSwiftcast)
            return false;

        var deadMember = context.PartyHelper.FindDeadPartyMemberNeedingRaise(player);
        if (deadMember is null)
            return false;

        if (player.CurrentMp < RaiseMpCost)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.Swiftcast.ActionId))
            return false;

        return context.ActionService.ExecuteOgcd(WHMActions.Swiftcast, player.GameObjectId);
    }

    private static bool ShouldWaitForThinAir(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Buffs.EnableThinAir || player.Level < WHMActions.ThinAir.MinLevel)
            return false;

        if (context.HasThinAir)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.ThinAir.ActionId))
            return false;

        return true;
    }
}
