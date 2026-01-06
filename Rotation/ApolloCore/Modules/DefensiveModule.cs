using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using Olympus.Data;
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

        var (avgHpPercent, _, injuredCount) = context.PartyHealthMetrics;

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
        var (avgHpPercent, _, injuredCount) = context.PartyHealthMetrics;
        var partyDamageRate = context.DamageIntakeService.GetPartyDamageRate(5f);
        var dmgRateStr = partyDamageRate > 0 ? $", DPS {partyDamageRate:F0}" : "";

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
            var highDamageIntake = config.Defensive.UseDynamicDefensiveThresholds &&
                                   partyDamageRate >= config.Defensive.DamageSpikeTriggerRate;
            var effectiveThreshold = highDamageIntake
                ? config.Defensive.DefensiveCooldownThreshold + 0.10f
                : config.Defensive.DefensiveCooldownThreshold;

            var shouldUse = injuredCount >= 3 || avgHpPercent < effectiveThreshold || highDamageIntake;
            context.Debug.TemperanceState = shouldUse
                ? $"Ready ({injuredCount} injured, avg HP {avgHpPercent:P0}{dmgRateStr})"
                : $"Waiting ({injuredCount} injured, avg HP {avgHpPercent:P0}{dmgRateStr})";
        }

        context.Debug.DefensiveState = $"Monitoring (avg HP {avgHpPercent:P0}, {injuredCount} injured{dmgRateStr})";
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

        if (ActionExecutor.ExecuteOgcd(context, WHMActions.DivineCaress, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentHp))
        {
            context.Debug.DefensiveState = "Divine Caress (triggered)";
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

        // Calculate party damage rate for dynamic thresholds
        var partyDamageRate = context.DamageIntakeService.GetPartyDamageRate(5f);
        var highDamageIntake = config.Defensive.UseDynamicDefensiveThresholds &&
                               partyDamageRate >= config.Defensive.DamageSpikeTriggerRate;

        // Check damage trend for proactive Temperance usage
        var damageSpikeImminent = config.Defensive.UseTemperanceTrendAnalysis &&
                                  context.DamageTrendService.IsDamageSpikeImminent(0.8f);

        // Standard threshold or lowered threshold during high damage
        var effectiveThreshold = highDamageIntake || damageSpikeImminent
            ? config.Defensive.DefensiveCooldownThreshold + 0.10f  // More proactive during damage spike
            : config.Defensive.DefensiveCooldownThreshold;

        var shouldUse = injuredCount >= 3 ||
                        avgHpPercent < effectiveThreshold ||
                        highDamageIntake ||
                        damageSpikeImminent;

        if (!shouldUse)
        {
            var dmgRateStr = partyDamageRate > 0 ? $", DPS {partyDamageRate:F0}" : "";
            context.Debug.TemperanceState = $"Waiting ({injuredCount} injured, avg HP {avgHpPercent:P0}{dmgRateStr})";
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

        var execDmgRateStr = partyDamageRate > 0 ? $", DPS {partyDamageRate:F0}" : "";
        context.Debug.TemperanceState = $"Executing ({injuredCount} injured, avg HP {avgHpPercent:P0}{execDmgRateStr})";

        if (ActionExecutor.ExecuteOgcd(context, WHMActions.Temperance, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentHp))
        {
            var reason = damageSpikeImminent ? "spike imminent" :
                         highDamageIntake ? "damage spike" : $"{injuredCount} injured";
            context.Debug.DefensiveState = $"Temperance ({reason}, avg HP {avgHpPercent:P0})";
            return true;
        }

        context.Debug.TemperanceState = "UseAction failed";
        return false;
    }

    private bool TryExecutePlenaryIndulgence(ApolloContext context, int injuredCount)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.PlenaryIndulgence, config,
            c => c.EnableHealing && c.Defensive.EnablePlenaryIndulgence))
            return false;

        var shouldUse = config.Defensive.UseDefensivesWithAoEHeals && injuredCount >= config.Healing.AoEHealMinTargets;

        if (!shouldUse)
            return false;

        if (ActionExecutor.ExecuteOgcd(context, WHMActions.PlenaryIndulgence, player.GameObjectId,
            player.Name?.TextValue ?? "Unknown", player.CurrentHp))
        {
            context.Debug.DefensiveState = $"Plenary Indulgence ({injuredCount} injured, pre-AoE heal)";
            return true;
        }

        return false;
    }

    private bool TryExecuteDivineBenison(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.DivineBenison, config,
            c => c.EnableHealing && c.Defensive.EnableDivineBenison))
            return false;

        var tank = context.PartyHelper.FindTankInParty(player);
        if (tank is null)
            return false;

        if (StatusHelper.HasStatus(tank, StatusHelper.StatusIds.DivineBenison))
            return false;

        if (Vector3.DistanceSquared(player.Position, tank.Position) >
            WHMActions.DivineBenison.RangeSquared)
            return false;

        var tankHpPct = context.PartyHelper.GetHpPercent(tank);
        var tankDamageRate = context.DamageIntakeService.GetDamageRate(tank.EntityId, 3f);

        // Proactive application: Apply if tank is taking significant sustained damage
        // even if HP is still high (anticipate tank buster)
        var shouldApplyProactively = config.Defensive.EnableProactiveCooldowns &&
                                     tankDamageRate >= config.Defensive.ProactiveBenisonDamageRate;

        // Standard application: Apply if tank HP is low
        var shouldApplyStandard = tankHpPct < 0.95f;

        if (!shouldApplyProactively && !shouldApplyStandard)
            return false;

        var tankName = tank.Name?.TextValue ?? "Unknown";
        if (ActionExecutor.ExecuteOgcd(context, WHMActions.DivineBenison, tank.GameObjectId,
            tankName, tank.CurrentHp))
        {
            var reason = shouldApplyProactively
                ? $"proactive, DPS {tankDamageRate:F0}"
                : $"{tankHpPct:P0} HP";
            context.Debug.DefensiveState = $"Divine Benison on {tankName} ({reason})";
            return true;
        }

        return false;
    }

    private bool TryExecuteAquaveil(ApolloContext context)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.Aquaveil, config,
            c => c.Defensive.EnableAquaveil))
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
            WHMActions.Aquaveil.RangeSquared)
            return false;

        var tankName = tank.Name?.TextValue ?? "Unknown";
        if (ActionExecutor.ExecuteOgcd(context, WHMActions.Aquaveil, tank.GameObjectId,
            tankName, tank.CurrentHp))
        {
            context.Debug.DefensiveState = $"Aquaveil on {tankName} ({tankHpPct:P0} HP)";
            return true;
        }

        return false;
    }

    private bool TryExecuteLiturgyOfTheBell(ApolloContext context, int injuredCount)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!ActionValidator.CanExecute(player, context.ActionService, WHMActions.LiturgyOfTheBell, config,
            c => c.Defensive.EnableLiturgyOfTheBell))
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

        if (ActionExecutor.ExecuteGroundTargeted(context, WHMActions.LiturgyOfTheBell, targetPosition,
            targetName, tank?.CurrentHp ?? player.CurrentHp))
        {
            context.Debug.DefensiveState = $"Bell placed at {targetName} ({injuredCount} injured)";
            return true;
        }

        return false;
    }
}
