using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Olympus.Config;
using Olympus.Models.Action;
using Olympus.Services.Action;

namespace Olympus.Rotation.ApolloCore.Helpers;

/// <summary>
/// Helper for validating action preconditions.
/// Eliminates repetitive level/config/cooldown checks across modules.
/// </summary>
public static class ActionValidator
{
    /// <summary>
    /// Checks if a player has unlocked an action based on level.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="action">The action to check.</param>
    /// <returns>True if the player's level meets the action's minimum level requirement.</returns>
    public static bool IsUnlocked(IPlayerCharacter player, ActionDefinition action)
    {
        return player.Level >= action.MinLevel;
    }

    /// <summary>
    /// Checks if an action is ready (not on cooldown).
    /// </summary>
    /// <param name="actionService">The action service.</param>
    /// <param name="action">The action to check.</param>
    /// <returns>True if the action is ready to use.</returns>
    public static bool IsReady(IActionService actionService, ActionDefinition action)
    {
        return actionService.IsActionReady(action.ActionId);
    }

    /// <summary>
    /// Checks if an action can be executed (config enabled, level met, and ready).
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="actionService">The action service.</param>
    /// <param name="action">The action to check.</param>
    /// <param name="config">The configuration.</param>
    /// <param name="configCheck">Function to check if the action is enabled in config.</param>
    /// <returns>True if all preconditions are met.</returns>
    public static bool CanExecute(
        IPlayerCharacter player,
        IActionService actionService,
        ActionDefinition action,
        Configuration config,
        Func<Configuration, bool> configCheck)
    {
        return configCheck(config) &&
               IsUnlocked(player, action) &&
               IsReady(actionService, action);
    }

    /// <summary>
    /// Checks if an action can be executed and provides a reason if not.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="actionService">The action service.</param>
    /// <param name="action">The action to check.</param>
    /// <param name="config">The configuration.</param>
    /// <param name="configCheck">Function to check if the action is enabled in config.</param>
    /// <param name="reason">Output parameter with the reason why the action cannot be executed.</param>
    /// <returns>True if all preconditions are met, false otherwise with reason set.</returns>
    public static bool CanExecute(
        IPlayerCharacter player,
        IActionService actionService,
        ActionDefinition action,
        Configuration config,
        Func<Configuration, bool> configCheck,
        out string? reason)
    {
        if (!configCheck(config))
        {
            reason = "Disabled";
            return false;
        }

        if (!IsUnlocked(player, action))
        {
            reason = $"Level {player.Level} < {action.MinLevel}";
            return false;
        }

        if (!IsReady(actionService, action))
        {
            reason = "On cooldown";
            return false;
        }

        reason = null;
        return true;
    }

    /// <summary>
    /// Checks if an action can be executed and provides detailed cooldown info if not ready.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="actionService">The action service.</param>
    /// <param name="action">The action to check.</param>
    /// <param name="config">The configuration.</param>
    /// <param name="configCheck">Function to check if the action is enabled in config.</param>
    /// <param name="reason">Output parameter with the reason why the action cannot be executed.</param>
    /// <returns>True if all preconditions are met, false otherwise with reason set.</returns>
    public static bool CanExecuteWithCooldownInfo(
        IPlayerCharacter player,
        IActionService actionService,
        ActionDefinition action,
        Configuration config,
        Func<Configuration, bool> configCheck,
        out string? reason)
    {
        if (!configCheck(config))
        {
            reason = "Disabled";
            return false;
        }

        if (!IsUnlocked(player, action))
        {
            reason = $"Level {player.Level} < {action.MinLevel}";
            return false;
        }

        if (!IsReady(actionService, action))
        {
            var cd = actionService.GetCooldownRemaining(action.ActionId);
            reason = $"CD: {cd:F1}s";
            return false;
        }

        reason = null;
        return true;
    }

    /// <summary>
    /// Simple check for action preconditions without config check.
    /// Useful when config is already validated elsewhere.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="actionService">The action service.</param>
    /// <param name="action">The action to check.</param>
    /// <returns>True if level and cooldown requirements are met.</returns>
    public static bool IsAvailable(
        IPlayerCharacter player,
        IActionService actionService,
        ActionDefinition action)
    {
        return IsUnlocked(player, action) && IsReady(actionService, action);
    }
}
