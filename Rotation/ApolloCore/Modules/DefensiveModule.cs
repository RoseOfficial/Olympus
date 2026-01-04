using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using Olympus.Data;
using Olympus.Models;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;

namespace Olympus.Rotation.ApolloCore.Modules;

/// <summary>
/// Handles all defensive cooldowns for the WHM rotation.
/// Includes Temperance, Divine Caress, Plenary Indulgence, Divine Benison, Aquaveil, Liturgy of the Bell.
/// </summary>
public sealed class DefensiveModule : IApolloModule
{
    public int Priority => 20; // Medium-high priority for defensive cooldowns
    public string Name => "Defensive";

    public bool TryExecute(ApolloContext context, bool isMoving)
    {
        if (!context.CanExecuteOgcd || !context.InCombat)
            return false;

        var (avgHpPercent, lowestHpPercent, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(context.Player);

        // Divine Caress is auto-triggered when Divine Grace (from Temperance) is active
        if (TryExecuteDivineCaress(context))
            return true;

        // Temperance: Major raid cooldown
        if (TryExecuteTemperance(context, avgHpPercent, injuredCount))
            return true;

        // Plenary Indulgence: 10% damage reduction, synergizes with AoE heals
        if (TryExecutePlenaryIndulgence(context, injuredCount))
            return true;

        // Divine Benison: Shield tank proactively
        if (TryExecuteDivineBenison(context))
            return true;

        // Aquaveil: Damage reduction on tank
        if (TryExecuteAquaveil(context))
            return true;

        // Liturgy of the Bell: Ground-targeted reactive healer
        if (TryExecuteLiturgyOfTheBell(context, injuredCount))
            return true;

        context.Debug.DefensiveState = $"Idle (avg HP {avgHpPercent:P0}, {injuredCount} injured)";
        return false;
    }

    public void UpdateDebugState(ApolloContext context)
    {
        if (!context.InCombat)
            return;

        var player = context.Player;
        var config = context.Configuration;
        var (avgHpPercent, _, injuredCount) = context.PartyHelper.CalculatePartyHealthMetrics(player);

        // Update Temperance state
        if (!config.EnableHealing || !config.Defensive.EnableTemperance)
        {
            context.Debug.TemperanceState = "Disabled";
        }
        else if (player.Level < WHMActions.Temperance.MinLevel)
        {
            context.Debug.TemperanceState = $"Level {player.Level} < {WHMActions.Temperance.MinLevel}";
        }
        else if (!context.ActionService.IsActionReady(WHMActions.Temperance.ActionId))
        {
            var cd = context.ActionService.GetCooldownRemaining(WHMActions.Temperance.ActionId);
            context.Debug.TemperanceState = $"CD {cd:F1}s";
        }
        else
        {
            var shouldUse = injuredCount >= 3 || avgHpPercent < config.Defensive.DefensiveCooldownThreshold;
            context.Debug.TemperanceState = shouldUse
                ? $"Ready ({injuredCount} injured, avg HP {avgHpPercent:P0})"
                : $"Waiting ({injuredCount} injured, avg HP {avgHpPercent:P0})";
        }

        context.Debug.DefensiveState = $"Monitoring (avg HP {avgHpPercent:P0}, {injuredCount} injured)";
    }

    private bool TryExecuteDivineCaress(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Defensive.EnableDivineCaress)
            return false;

        if (player.Level < WHMActions.DivineCaress.MinLevel)
            return false;

        if (!StatusHelper.HasDivineGrace(player))
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.DivineCaress.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WHMActions.DivineCaress, player.GameObjectId))
        {
            context.Debug.PlannedAction = "Divine Caress";
            context.Debug.DefensiveState = "Divine Caress (triggered)";
            context.ActionTracker.LogAttempt(WHMActions.DivineCaress.ActionId, player.Name?.TextValue ?? "Unknown", player.CurrentHp, ActionResult.Success, player.Level);
            return true;
        }

        return false;
    }

    private unsafe bool TryExecuteTemperance(ApolloContext context, float avgHpPercent, int injuredCount)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Defensive.EnableTemperance)
        {
            context.Debug.TemperanceState = "Disabled";
            return false;
        }

        if (player.Level < WHMActions.Temperance.MinLevel)
        {
            context.Debug.TemperanceState = $"Level {player.Level} < {WHMActions.Temperance.MinLevel}";
            return false;
        }

        if (!context.ActionService.IsActionReady(WHMActions.Temperance.ActionId))
        {
            var cd = context.ActionService.GetCooldownRemaining(WHMActions.Temperance.ActionId);
            context.Debug.TemperanceState = $"CD {cd:F1}s";
            return false;
        }

        var shouldUse = injuredCount >= 3 || avgHpPercent < config.Defensive.DefensiveCooldownThreshold;

        if (!shouldUse)
        {
            context.Debug.TemperanceState = $"Waiting ({injuredCount} injured, avg HP {avgHpPercent:P0})";
            return false;
        }

        // Check action status before attempting execution
        var actionManager = ActionManager.Instance();
        if (actionManager is not null)
        {
            var status = actionManager->GetActionStatus(ActionType.Action, WHMActions.Temperance.ActionId);
            if (status != 0)
            {
                context.Debug.TemperanceState = $"Blocked (status={status})";
                return false;
            }
        }

        context.Debug.TemperanceState = $"Executing ({injuredCount} injured, avg HP {avgHpPercent:P0})";

        if (context.ActionService.ExecuteOgcd(WHMActions.Temperance, player.GameObjectId))
        {
            context.Debug.PlannedAction = "Temperance";
            context.Debug.DefensiveState = $"Temperance ({injuredCount} injured, avg HP {avgHpPercent:P0})";
            context.ActionTracker.LogAttempt(WHMActions.Temperance.ActionId, player.Name?.TextValue ?? "Unknown", player.CurrentHp, ActionResult.Success, player.Level);
            return true;
        }

        context.Debug.TemperanceState = "UseAction failed";
        return false;
    }

    private bool TryExecutePlenaryIndulgence(ApolloContext context, int injuredCount)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Defensive.EnablePlenaryIndulgence)
            return false;

        if (player.Level < WHMActions.PlenaryIndulgence.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.PlenaryIndulgence.ActionId))
            return false;

        var shouldUse = config.Defensive.UseDefensivesWithAoEHeals && injuredCount >= config.Healing.AoEHealMinTargets;

        if (!shouldUse)
            return false;

        if (context.ActionService.ExecuteOgcd(WHMActions.PlenaryIndulgence, player.GameObjectId))
        {
            context.Debug.PlannedAction = "Plenary Indulgence";
            context.Debug.DefensiveState = $"Plenary Indulgence ({injuredCount} injured, pre-AoE heal)";
            context.ActionTracker.LogAttempt(WHMActions.PlenaryIndulgence.ActionId, player.Name?.TextValue ?? "Unknown", player.CurrentHp, ActionResult.Success, player.Level);
            return true;
        }

        return false;
    }

    private bool TryExecuteDivineBenison(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableHealing || !config.Defensive.EnableDivineBenison)
            return false;

        if (player.Level < WHMActions.DivineBenison.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.DivineBenison.ActionId))
            return false;

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank is null)
            return false;

        if (StatusHelper.HasStatus(tank, StatusHelper.StatusIds.DivineBenison))
            return false;

        var tankHpPct = context.PartyHelper.GetHpPercent(tank);
        if (tankHpPct >= 0.95f)
            return false;

        if (Vector3.DistanceSquared(player.Position, tank.Position) >
            WHMActions.DivineBenison.Range * WHMActions.DivineBenison.Range)
            return false;

        if (context.ActionService.ExecuteOgcd(WHMActions.DivineBenison, tank.GameObjectId))
        {
            var tankName = tank.Name?.TextValue ?? "Unknown";
            context.Debug.PlannedAction = "Divine Benison";
            context.Debug.DefensiveState = $"Divine Benison on {tankName} ({tankHpPct:P0} HP)";
            context.ActionTracker.LogAttempt(WHMActions.DivineBenison.ActionId, tankName, tank.CurrentHp, ActionResult.Success, player.Level);
            return true;
        }

        return false;
    }

    private bool TryExecuteAquaveil(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Defensive.EnableAquaveil)
            return false;

        if (player.Level < WHMActions.Aquaveil.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.Aquaveil.ActionId))
            return false;

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank is null)
            return false;

        if (StatusHelper.HasStatus(tank, StatusHelper.StatusIds.Aquaveil))
            return false;

        var tankHpPct = context.PartyHelper.GetHpPercent(tank);
        if (tankHpPct >= 0.90f)
            return false;

        if (Vector3.DistanceSquared(player.Position, tank.Position) >
            WHMActions.Aquaveil.Range * WHMActions.Aquaveil.Range)
            return false;

        if (context.ActionService.ExecuteOgcd(WHMActions.Aquaveil, tank.GameObjectId))
        {
            var tankName = tank.Name?.TextValue ?? "Unknown";
            context.Debug.PlannedAction = "Aquaveil";
            context.Debug.DefensiveState = $"Aquaveil on {tankName} ({tankHpPct:P0} HP)";
            context.ActionTracker.LogAttempt(WHMActions.Aquaveil.ActionId, tankName, tank.CurrentHp, ActionResult.Success, player.Level);
            return true;
        }

        return false;
    }

    private bool TryExecuteLiturgyOfTheBell(ApolloContext context, int injuredCount)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.Defensive.EnableLiturgyOfTheBell)
            return false;

        if (player.Level < WHMActions.LiturgyOfTheBell.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(WHMActions.LiturgyOfTheBell.ActionId))
            return false;

        if (injuredCount < 2)
            return false;

        var tank = context.PartyHelper.FindTankInParty(player);
        Vector3 targetPosition;
        string targetName;

        if (tank is not null)
        {
            var distance = Vector3.Distance(player.Position, tank.Position);
            if (distance > WHMActions.LiturgyOfTheBell.Range)
            {
                targetPosition = player.Position;
                targetName = player.Name?.TextValue ?? "Unknown";
            }
            else
            {
                targetPosition = tank.Position;
                targetName = tank.Name?.TextValue ?? "Unknown";
            }
        }
        else
        {
            targetPosition = player.Position;
            targetName = player.Name?.TextValue ?? "Unknown";
        }

        if (context.ActionService.ExecuteGroundTargetedOgcd(WHMActions.LiturgyOfTheBell, targetPosition))
        {
            context.Debug.PlannedAction = "Liturgy of the Bell";
            context.Debug.DefensiveState = $"Bell placed at {targetName} ({injuredCount} injured)";
            context.ActionTracker.LogAttempt(WHMActions.LiturgyOfTheBell.ActionId, targetName, tank?.CurrentHp ?? player.CurrentHp, ActionResult.Success, player.Level);
            return true;
        }

        return false;
    }
}
