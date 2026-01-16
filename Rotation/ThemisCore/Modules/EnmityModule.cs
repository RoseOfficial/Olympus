using System;
using Olympus.Data;
using Olympus.Rotation.ThemisCore.Context;

namespace Olympus.Rotation.ThemisCore.Modules;

/// <summary>
/// Handles the Paladin enmity management.
/// Manages Provoke and Shirk for threat control.
/// </summary>
public sealed class EnmityModule : IThemisModule
{
    public int Priority => 5; // Highest priority - enmity management is critical
    public string Name => "Enmity";

    private DateTime _lastProvokeTime = DateTime.MinValue;

    public bool TryExecute(IThemisContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.EnmityState = "Not in combat";
            return false;
        }

        // Only use enmity actions during oGCD windows
        if (!context.CanExecuteOgcd)
            return false;

        // Priority 1: Provoke if losing aggro
        if (TryProvoke(context))
            return true;

        // Priority 2: Shirk to co-tank if needed
        if (TryShirk(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IThemisContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Provoke

    private bool TryProvoke(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Provoke.MinLevel)
            return false;

        // Check configuration
        if (!context.Configuration.Tank.AutoProvoke)
        {
            context.Debug.EnmityState = "AutoProvoke disabled";
            return false;
        }

        // Find current target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            25f, // Provoke range
            player);

        if (target == null)
        {
            context.Debug.EnmityState = "No target";
            return false;
        }

        // Check if we're losing aggro on the target
        if (!context.EnmityService.IsLosingAggro(target, player.EntityId))
        {
            // We have aggro, all good
            var position = context.EnmityService.GetEnmityPosition(target, player.EntityId);
            context.Debug.EnmityState = position == 1 ? "Main tank" : $"Position {position}";
            return false;
        }

        // Apply provoke delay to prevent spam
        var timeSinceLastProvoke = (DateTime.UtcNow - _lastProvokeTime).TotalSeconds;
        if (timeSinceLastProvoke < context.Configuration.Tank.ProvokeDelay)
        {
            context.Debug.EnmityState = $"Provoke cooldown ({context.Configuration.Tank.ProvokeDelay - timeSinceLastProvoke:F1}s)";
            return false;
        }

        // Check if Provoke is ready
        if (!context.ActionService.IsActionReady(PLDActions.Provoke.ActionId))
        {
            context.Debug.EnmityState = "Provoke on CD";
            return false;
        }

        // Execute Provoke
        if (context.ActionService.ExecuteOgcd(PLDActions.Provoke, target.GameObjectId))
        {
            _lastProvokeTime = DateTime.UtcNow;
            context.Debug.PlannedAction = PLDActions.Provoke.Name;
            context.Debug.EnmityState = "Provoking (losing aggro)";
            return true;
        }

        return false;
    }

    #endregion

    #region Shirk

    private bool TryShirk(IThemisContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < PLDActions.Shirk.MinLevel)
            return false;

        // Check configuration
        if (!context.Configuration.Tank.AutoShirk)
        {
            return false;
        }

        // Only shirk if we're main tank and should be off-tank
        // This is complex to determine automatically
        // For now, only shirk when co-tank has aggro and we're #2

        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.MeleeTargetingRange,
            player);

        if (target == null)
            return false;

        // Check if co-tank has aggro
        if (!context.EnmityService.HasCoTankAggro(target, player.EntityId))
            return false;

        // Find co-tank to shirk to
        var coTank = context.PartyHelper.FindCoTank(player);
        if (coTank == null)
        {
            context.Debug.EnmityState = "No co-tank found";
            return false;
        }

        // Check distance to co-tank (Shirk range is 25y)
        var dx = player.Position.X - coTank.Position.X;
        var dy = player.Position.Y - coTank.Position.Y;
        var dz = player.Position.Z - coTank.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dy * dy + dz * dz);

        if (distance > 25f)
        {
            context.Debug.EnmityState = "Co-tank too far for Shirk";
            return false;
        }

        // Check if Shirk is ready
        if (!context.ActionService.IsActionReady(PLDActions.Shirk.ActionId))
        {
            context.Debug.EnmityState = "Shirk on CD";
            return false;
        }

        // Only auto-shirk if our enmity position is #2 (off-tank position)
        var myPosition = context.EnmityService.GetEnmityPosition(target, player.EntityId);
        if (myPosition != 2)
        {
            context.Debug.EnmityState = $"Position {myPosition}, not off-tanking";
            return false;
        }

        // Execute Shirk
        if (context.ActionService.ExecuteOgcd(PLDActions.Shirk, coTank.GameObjectId))
        {
            context.Debug.PlannedAction = PLDActions.Shirk.Name;
            context.Debug.EnmityState = "Shirking to co-tank";
            return true;
        }

        return false;
    }

    #endregion
}
