using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// SGE-specific damage module.
/// Handles Dosis, Eukrasian Dosis (DoT), Phlegma, Toxikon, Dyskrasia, and Psyche.
///
/// Priority order:
/// 1. Psyche (oGCD damage)
/// 2. Phlegma (high potency instant GCD with charges)
/// 3. Toxikon (instant when moving, consumes Addersting)
/// 4. Eukrasian Dosis (DoT maintenance)
/// 5. Dyskrasia (AoE damage)
/// 6. Dosis (single-target filler)
/// </summary>
public sealed class DamageModule : IAsclepiusModule
{
    public int Priority => 50; // Low priority - DPS after healing
    public string Name => "Damage";

    public bool TryExecute(IAsclepiusContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DpsState = "Not in combat";
            return false;
        }

        var config = context.Configuration;

        if (!config.EnableDamage)
        {
            context.Debug.DpsState = "Disabled";
            return false;
        }

        // oGCD damage
        if (context.CanExecuteOgcd)
        {
            // Psyche - oGCD AoE damage
            if (TryPsyche(context))
                return false; // Don't block, just weave
        }

        // GCD damage
        if (context.CanExecuteGcd)
        {
            // Priority 1: Phlegma (high potency, instant, charges)
            if (TryPhlegma(context))
                return false; // SGE DPS doesn't block

            // Priority 2: Toxikon while moving (consumes Addersting)
            if (isMoving && TryToxikon(context))
                return false;

            // Priority 3: DoT maintenance (Eukrasian Dosis)
            if (TryDoT(context, isMoving))
                return false;

            // Priority 4: AoE damage (Dyskrasia)
            if (TryAoEDamage(context))
                return false;

            // Priority 5: Single-target damage (Dosis)
            if (!isMoving && TrySingleTargetDamage(context))
                return false;

            // Priority 6: Toxikon as filler when moving
            if (isMoving && TryToxikon(context))
                return false;
        }

        return false;
    }

    public void UpdateDebugState(IAsclepiusContext context)
    {
        var player = context.Player;

        // Update DoT state
        var dotAction = SGEActions.GetDotForLevel(player.Level);
        var dotStatusId = SGEActions.GetDotStatusId(player.Level);

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            dotAction.Range,
            player);

        if (enemy != null)
        {
            var dotRemaining = GetStatusRemainingTime(enemy, dotStatusId, player.GameObjectId);
            context.Debug.DoTRemaining = dotRemaining;
            context.Debug.DoTState = dotRemaining > 0 ? $"{dotRemaining:F1}s" : "Not applied";
        }
        else
        {
            context.Debug.DoTState = "No target";
        }

        // Phlegma charges
        var phlegmaAction = SGEActions.GetPhlegmaForLevel(player.Level);
        if (phlegmaAction != null)
        {
            var charges = (int)context.ActionService.GetCurrentCharges(phlegmaAction.ActionId);
            context.Debug.PhlegmaCharges = charges;
            context.Debug.PhlegmaState = charges > 0 ? $"{charges} charges" : "No charges";
        }

        // Addersting for Toxikon
        context.Debug.AdderstingStacks = context.AdderstingStacks;
        context.Debug.ToxikonState = context.AdderstingStacks > 0 ? $"{context.AdderstingStacks} stacks" : "No Addersting";
    }

    #region oGCD Damage

    private bool TryPsyche(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePsyche)
            return false;

        if (player.Level < SGEActions.Psyche.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(SGEActions.Psyche.ActionId))
        {
            context.Debug.PsycheState = "On CD";
            return false;
        }

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            SGEActions.Psyche.Range,
            player);

        if (enemy == null)
        {
            context.Debug.PsycheState = "No target";
            return false;
        }

        var action = SGEActions.Psyche;
        if (context.ActionService.ExecuteOgcd(action, enemy.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DpsState = "Psyche";
            context.Debug.PsycheState = "Executing";
            return true;
        }

        return false;
    }

    #endregion

    #region GCD Damage

    private bool TryPhlegma(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnablePhlegma)
            return false;

        var phlegmaAction = SGEActions.GetPhlegmaForLevel(player.Level);
        if (phlegmaAction == null)
        {
            context.Debug.PhlegmaState = "Level too low";
            return false;
        }

        // Check charges
        var charges = context.ActionService.GetCurrentCharges(phlegmaAction.ActionId);
        if (charges < 1)
        {
            context.Debug.PhlegmaState = "No charges";
            return false;
        }

        // Use if we'd overcap charges
        var maxCharges = 2;
        var rechargingTime = context.ActionService.GetCooldownRemaining(phlegmaAction.ActionId);
        var shouldUse = charges >= maxCharges || (charges == maxCharges - 1 && rechargingTime < 5f);

        if (!shouldUse && charges < maxCharges)
        {
            context.Debug.PhlegmaState = $"Saving ({charges}/{maxCharges})";
            return false;
        }

        // Find enemy in range (Phlegma is close range)
        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            phlegmaAction.Range,
            player);

        if (enemy == null)
        {
            context.Debug.PhlegmaState = "Out of range";
            return false;
        }

        if (context.ActionService.ExecuteGcd(phlegmaAction, enemy.GameObjectId))
        {
            context.Debug.PlannedAction = phlegmaAction.Name;
            context.Debug.DpsState = phlegmaAction.Name;
            context.Debug.PhlegmaState = "Executing";
            return true;
        }

        return false;
    }

    private bool TryToxikon(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        if (!config.EnableToxikon)
            return false;

        var toxikonAction = SGEActions.GetToxikonForLevel(player.Level);
        if (toxikonAction == null)
        {
            context.Debug.ToxikonState = "Level too low";
            return false;
        }

        // Requires Addersting
        if (context.AdderstingStacks < 1)
        {
            context.Debug.ToxikonState = "No Addersting";
            return false;
        }

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            toxikonAction.Range,
            player);

        if (enemy == null)
        {
            context.Debug.ToxikonState = "No target";
            return false;
        }

        if (context.ActionService.ExecuteGcd(toxikonAction, enemy.GameObjectId))
        {
            context.Debug.PlannedAction = toxikonAction.Name;
            context.Debug.DpsState = toxikonAction.Name;
            context.Debug.ToxikonState = "Executing";
            return true;
        }

        return false;
    }

    private bool TryDoT(IAsclepiusContext context, bool isMoving)
    {
        var config = context.Configuration;
        var player = context.Player;

        if (!config.EnableDoT)
            return false;

        if (player.Level < SGEActions.EukrasianDosis.MinLevel)
            return false;

        var dotAction = SGEActions.GetDotForLevel(player.Level);
        var dotStatusId = SGEActions.GetDotStatusId(player.Level);

        // If we have Eukrasia, apply the DoT
        if (context.HasEukrasia)
        {
            var enemy = context.TargetingService.FindEnemy(
                config.Targeting.EnemyStrategy,
                dotAction.Range,
                player);

            if (enemy == null)
                return false;

            if (context.ActionService.ExecuteGcd(dotAction, enemy.GameObjectId))
            {
                context.Debug.PlannedAction = dotAction.Name;
                context.Debug.DpsState = "DoT Applied";
                return true;
            }

            return false;
        }

        // Check if we need to apply/refresh DoT
        var target = context.TargetingService.FindEnemyNeedingDot(
            dotStatusId,
            FFXIVConstants.DotRefreshThreshold,
            dotAction.Range,
            player);

        if (target == null)
        {
            context.Debug.DoTState = "Active";
            return false;
        }

        // Activate Eukrasia for DoT (this is an oGCD)
        if (context.CanExecuteOgcd)
        {
            var eukrasiaAction = SGEActions.Eukrasia;
            if (context.ActionService.ExecuteOgcd(eukrasiaAction, player.GameObjectId))
            {
                context.Debug.PlannedAction = eukrasiaAction.Name;
                context.Debug.DpsState = "Eukrasia for DoT";
                context.Debug.EukrasiaState = "Activating";
                return true;
            }
        }

        return false;
    }

    private bool TryAoEDamage(IAsclepiusContext context)
    {
        var config = context.Configuration.Sage;
        var player = context.Player;

        var aoeAction = SGEActions.GetAoEDamageGcdForLevel(player.Level);
        if (aoeAction == null)
        {
            context.Debug.AoEDpsState = "Level too low";
            return false;
        }

        // Count enemies in range
        var enemyCount = context.TargetingService.CountEnemiesInRange(aoeAction.Radius, player);
        context.Debug.AoEDpsEnemyCount = enemyCount;

        if (enemyCount < config.AoEDamageMinTargets)
        {
            context.Debug.AoEDpsState = $"{enemyCount} < {config.AoEDamageMinTargets}";
            return false;
        }

        // Dyskrasia is self-centered
        if (context.ActionService.ExecuteGcd(aoeAction, player.GameObjectId))
        {
            context.Debug.PlannedAction = aoeAction.Name;
            context.Debug.DpsState = $"AoE ({enemyCount})";
            context.Debug.AoEDpsState = $"{enemyCount} enemies";
            return true;
        }

        return false;
    }

    private bool TrySingleTargetDamage(IAsclepiusContext context)
    {
        var player = context.Player;

        var action = SGEActions.GetDamageGcdForLevel(player.Level);

        var enemy = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            action.Range,
            player);

        if (enemy == null)
        {
            context.Debug.DpsState = "No target";
            return false;
        }

        if (context.ActionService.ExecuteGcd(action, enemy.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DpsState = action.Name;
            return true;
        }

        return false;
    }

    #endregion

    #region Helpers

    private float GetStatusRemainingTime(Dalamud.Game.ClientState.Objects.Types.IBattleChara target, uint statusId, ulong sourceId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId && status.SourceId == (uint)sourceId)
            {
                return status.RemainingTime;
            }
        }

        return 0f;
    }

    #endregion
}
