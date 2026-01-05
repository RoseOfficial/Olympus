using System.Numerics;
using Olympus.Models;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;

namespace Olympus.Rotation.ApolloCore.Helpers;

/// <summary>
/// Helper for executing actions with consistent logging and debug state updates.
/// Eliminates repetitive execute-log-track patterns across modules.
/// </summary>
public static class ActionExecutor
{
    /// <summary>
    /// Executes a GCD action and handles logging/debug state.
    /// </summary>
    /// <param name="context">The Apollo context.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="targetId">The target's game object ID.</param>
    /// <param name="targetName">The target's name for logging.</param>
    /// <param name="targetHp">The target's current HP for logging (optional).</param>
    /// <param name="planningState">The planning state to set on success (optional).</param>
    /// <param name="appendThinAirNote">Whether to append "+ Thin Air" to PlannedAction if buff is active.</param>
    /// <returns>True if the action was executed successfully.</returns>
    public static bool ExecuteGcd(
        ApolloContext context,
        ActionDefinition action,
        ulong targetId,
        string targetName,
        uint? targetHp = null,
        string? planningState = null,
        bool appendThinAirNote = false)
    {
        var success = context.ActionService.ExecuteGcd(action, targetId);
        if (success)
        {
            var actionName = action.Name;
            if (appendThinAirNote && context.HasThinAir)
            {
                actionName += " + Thin Air";
            }

            context.Debug.PlannedAction = actionName;
            if (planningState is not null)
            {
                context.Debug.PlanningState = planningState;
            }

            context.ActionTracker.LogAttempt(
                action.ActionId,
                targetName,
                targetHp,
                ActionResult.Success,
                context.Player.Level);
        }

        return success;
    }

    /// <summary>
    /// Executes an oGCD action and handles logging/debug state.
    /// </summary>
    /// <param name="context">The Apollo context.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="targetId">The target's game object ID.</param>
    /// <param name="targetName">The target's name for logging.</param>
    /// <param name="targetHp">The target's current HP for logging (optional).</param>
    /// <param name="plannedActionName">Custom name for PlannedAction (defaults to action.Name).</param>
    /// <returns>True if the action was executed successfully.</returns>
    public static bool ExecuteOgcd(
        ApolloContext context,
        ActionDefinition action,
        ulong targetId,
        string targetName,
        uint? targetHp = null,
        string? plannedActionName = null)
    {
        var success = context.ActionService.ExecuteOgcd(action, targetId);
        if (success)
        {
            context.Debug.PlannedAction = plannedActionName ?? action.Name;
            context.ActionTracker.LogAttempt(
                action.ActionId,
                targetName,
                targetHp,
                ActionResult.Success,
                context.Player.Level);
        }

        return success;
    }

    /// <summary>
    /// Executes a ground-targeted oGCD action and handles logging/debug state.
    /// </summary>
    /// <param name="context">The Apollo context.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="position">The ground target position.</param>
    /// <param name="targetName">The target's name for logging (e.g., player or tank name).</param>
    /// <param name="targetHp">The target's current HP for logging (optional).</param>
    /// <param name="plannedActionName">Custom name for PlannedAction (defaults to action.Name).</param>
    /// <returns>True if the action was executed successfully.</returns>
    public static bool ExecuteGroundTargeted(
        ApolloContext context,
        ActionDefinition action,
        Vector3 position,
        string targetName,
        uint? targetHp = null,
        string? plannedActionName = null)
    {
        var success = context.ActionService.ExecuteGroundTargetedOgcd(action, position);
        if (success)
        {
            context.Debug.PlannedAction = plannedActionName ?? action.Name;
            context.ActionTracker.LogAttempt(
                action.ActionId,
                targetName,
                targetHp,
                ActionResult.Success,
                context.Player.Level);
        }

        return success;
    }

    /// <summary>
    /// Executes a healing GCD with HP prediction registration.
    /// </summary>
    /// <param name="context">The Apollo context.</param>
    /// <param name="action">The healing action to execute.</param>
    /// <param name="targetId">The target's game object ID.</param>
    /// <param name="targetEntityId">The target's entity ID for HP prediction.</param>
    /// <param name="targetName">The target's name for logging.</param>
    /// <param name="targetHp">The target's current HP for logging.</param>
    /// <param name="healAmount">The estimated heal amount for HP prediction.</param>
    /// <param name="planningState">The planning state to set on success.</param>
    /// <returns>True if the action was executed successfully.</returns>
    public static bool ExecuteHealingGcd(
        ApolloContext context,
        ActionDefinition action,
        ulong targetId,
        uint targetEntityId,
        string targetName,
        uint targetHp,
        int healAmount,
        string planningState)
    {
        // Register pending heal before execution
        context.HpPredictionService.RegisterPendingHeal(targetEntityId, healAmount);

        var success = context.ActionService.ExecuteGcd(action, targetId);
        if (success)
        {
            var actionName = action.Name;
            if (context.HasThinAir)
            {
                actionName += " + Thin Air";
            }

            context.Debug.PlannedAction = actionName;
            context.Debug.PlanningState = planningState;
            context.ActionTracker.LogAttempt(
                action.ActionId,
                targetName,
                targetHp,
                ActionResult.Success,
                context.Player.Level);
        }
        else
        {
            // Clear pending heals on failure
            context.HpPredictionService.ClearPendingHeals();
        }

        return success;
    }

    /// <summary>
    /// Executes a healing oGCD with HP prediction registration.
    /// </summary>
    /// <param name="context">The Apollo context.</param>
    /// <param name="action">The healing action to execute.</param>
    /// <param name="targetId">The target's game object ID.</param>
    /// <param name="targetEntityId">The target's entity ID for HP prediction.</param>
    /// <param name="targetName">The target's name for logging.</param>
    /// <param name="targetHp">The target's current HP for logging.</param>
    /// <param name="healAmount">The estimated heal amount for HP prediction.</param>
    /// <param name="plannedActionName">Custom name for PlannedAction (defaults to action.Name).</param>
    /// <returns>True if the action was executed successfully.</returns>
    public static bool ExecuteHealingOgcd(
        ApolloContext context,
        ActionDefinition action,
        ulong targetId,
        uint targetEntityId,
        string targetName,
        uint targetHp,
        int healAmount,
        string? plannedActionName = null)
    {
        var success = context.ActionService.ExecuteOgcd(action, targetId);
        if (success)
        {
            context.Debug.PlannedAction = plannedActionName ?? action.Name;
            context.HpPredictionService.RegisterPendingHeal(targetEntityId, healAmount);
            context.ActionTracker.LogAttempt(
                action.ActionId,
                targetName,
                targetHp,
                ActionResult.Success,
                context.Player.Level);
        }

        return success;
    }
}
