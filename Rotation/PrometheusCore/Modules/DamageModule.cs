using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.PrometheusCore.Context;

namespace Olympus.Rotation.PrometheusCore.Modules;

/// <summary>
/// Handles the Machinist damage rotation.
/// Manages tool actions, Heat Blast spam during Overheated, and 1-2-3 combo.
/// </summary>
public sealed class DamageModule : IPrometheusModule
{
    public int Priority => 30; // Lowest priority - damage after buffs
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(IPrometheusContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Find target
        var target = context.TargetingService.FindEnemy(
            context.Configuration.Targeting.EnemyStrategy,
            FFXIVConstants.RangedTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // oGCD: Interrupt enemy casts (highest priority)
        if (context.CanExecuteOgcd && TryInterrupt(context, target))
            return true;

        // Count nearby enemies for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(12f, player);
        context.Debug.NearbyEnemies = enemyCount;

        // GCD Phase
        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        // === OVERHEATED STATE ===
        if (context.IsOverheated)
        {
            if (TryOverheatedGcd(context, target, enemyCount))
                return true;
        }

        // === NORMAL STATE ===

        // Priority 1: Full Metal Field (proc from Barrel Stabilizer at Lv.100)
        if (TryFullMetalField(context, target))
            return true;

        // Priority 2: Excavator (proc from Chain Saw at Lv.96)
        if (TryExcavator(context, target))
            return true;

        // Priority 3: Drill (highest priority tool)
        if (TryDrill(context, target, enemyCount))
            return true;

        // Priority 4: Air Anchor (+20 Battery)
        if (TryAirAnchor(context, target))
            return true;

        // Priority 5: Chain Saw (+20 Battery, grants Excavator Ready)
        if (TryChainSaw(context, target))
            return true;

        // Priority 6: AoE or Single Target Combo
        if (TryCombo(context, target, enemyCount))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IPrometheusContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Overheated State

    private bool TryOverheatedGcd(IPrometheusContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        var action = MCHActions.GetOverheatedGcd(level, useAoe);

        if (level < action.MinLevel)
        {
            // Fallback to Heat Blast
            action = MCHActions.HeatBlast;
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
        {
            context.Debug.DamageState = "Heat action not ready";
            return false;
        }

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (Overheat: {context.OverheatRemaining:F1}s)";
            return true;
        }

        return false;
    }

    #endregion

    #region Tool Actions

    private bool TryFullMetalField(IPrometheusContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MCHActions.FullMetalField.MinLevel)
            return false;

        if (!context.HasFullMetalMachinist)
            return false;

        if (!context.ActionService.IsActionReady(MCHActions.FullMetalField.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(MCHActions.FullMetalField, target.GameObjectId))
        {
            context.Debug.PlannedAction = MCHActions.FullMetalField.Name;
            context.Debug.DamageState = "Full Metal Field (proc)";
            return true;
        }

        return false;
    }

    private bool TryExcavator(IPrometheusContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MCHActions.Excavator.MinLevel)
            return false;

        if (!context.HasExcavatorReady)
            return false;

        if (!context.ActionService.IsActionReady(MCHActions.Excavator.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(MCHActions.Excavator, target.GameObjectId))
        {
            context.Debug.PlannedAction = MCHActions.Excavator.Name;
            context.Debug.DamageState = "Excavator (+20 Battery)";
            return true;
        }

        return false;
    }

    private bool TryDrill(IPrometheusContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        // Use Bioblaster for AoE if available
        if (useAoe && level >= MCHActions.Bioblaster.MinLevel)
        {
            // Check if Bioblaster DoT needs refresh
            if (!context.HasBioblaster || context.BioblasterRemaining < 3f)
            {
                if (context.ActionService.IsActionReady(MCHActions.Bioblaster.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(MCHActions.Bioblaster, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = MCHActions.Bioblaster.Name;
                        context.Debug.DamageState = "Bioblaster (DoT AoE)";
                        return true;
                    }
                }
            }
        }

        // Single target Drill
        if (level < MCHActions.Drill.MinLevel)
            return false;

        if (context.DrillCharges == 0)
            return false;

        if (!context.ActionService.IsActionReady(MCHActions.Drill.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(MCHActions.Drill, target.GameObjectId))
        {
            context.Debug.PlannedAction = MCHActions.Drill.Name;
            context.Debug.DamageState = $"Drill (charges: {context.DrillCharges})";
            return true;
        }

        return false;
    }

    private bool TryAirAnchor(IPrometheusContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        var action = MCHActions.GetAirAnchor(level);

        if (level < action.MinLevel)
            return false;

        // Don't use if we'd overcap Battery (Air Anchor gives +20)
        if (context.Battery > 80)
        {
            context.Debug.DamageState = "Battery too high for Air Anchor";
            return false;
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (+20 Battery)";
            return true;
        }

        return false;
    }

    private bool TryChainSaw(IPrometheusContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < MCHActions.ChainSaw.MinLevel)
            return false;

        // Don't use if we'd overcap Battery (Chain Saw gives +20)
        if (context.Battery > 80)
        {
            context.Debug.DamageState = "Battery too high for Chain Saw";
            return false;
        }

        if (!context.ActionService.IsActionReady(MCHActions.ChainSaw.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(MCHActions.ChainSaw, target.GameObjectId))
        {
            context.Debug.PlannedAction = MCHActions.ChainSaw.Name;
            context.Debug.DamageState = "Chain Saw (+20 Battery)";
            return true;
        }

        return false;
    }

    #endregion

    #region Combo

    private bool TryCombo(IPrometheusContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        if (useAoe)
        {
            return TryAoeCombo(context, target);
        }

        return TrySingleTargetCombo(context, target);
    }

    private bool TrySingleTargetCombo(IPrometheusContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Determine combo action based on step
        Models.Action.ActionDefinition action;
        string comboInfo;

        // Step 3: Finisher
        if (context.ComboStep == 2 &&
            (context.LastComboAction == MCHActions.HeatedSlugShot.ActionId ||
             context.LastComboAction == MCHActions.SlugShot.ActionId))
        {
            action = MCHActions.GetComboFinisher(level);
            comboInfo = $"{action.Name} (+5 Heat, +10 Battery)";
        }
        // Step 2: Second hit
        else if (context.ComboStep == 1 &&
                 (context.LastComboAction == MCHActions.HeatedSplitShot.ActionId ||
                  context.LastComboAction == MCHActions.SplitShot.ActionId))
        {
            action = MCHActions.GetComboSecond(level);
            comboInfo = $"{action.Name} (+5 Heat)";
        }
        // Step 1: Starter
        else
        {
            action = MCHActions.GetComboStarter(level);
            comboInfo = $"{action.Name} (+5 Heat)";
        }

        if (level < action.MinLevel)
        {
            // Fall back to basic action
            action = MCHActions.SplitShot;
            comboInfo = "Split Shot";
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{comboInfo} (combo {context.ComboStep + 1})";
            return true;
        }

        return false;
    }

    private bool TryAoeCombo(IPrometheusContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // AoE is simpler - just spam Scattergun/Spread Shot
        var action = MCHActions.GetAoeAction(level);

        if (level < action.MinLevel)
        {
            // Fall back to Spread Shot
            action = MCHActions.SpreadShot;
        }

        if (level < MCHActions.SpreadShot.MinLevel)
        {
            // Too low level for AoE, use single target
            return TrySingleTargetCombo(context, target);
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, player.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (AoE)";
            return true;
        }

        return false;
    }

    #endregion

    #region Interrupt

    /// <summary>
    /// Attempts to interrupt an enemy cast using Head Graze.
    /// Coordinates with other Olympus instances to prevent duplicate interrupts.
    /// </summary>
    private bool TryInterrupt(IPrometheusContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        // Need at least Head Graze (Lv.24)
        if (level < MCHActions.HeadGraze.MinLevel)
            return false;

        // Check if target is casting something interruptible
        if (!target.IsCasting)
            return false;

        // Check the cast interruptible flag (game indicates this)
        if (!target.IsCastInterruptible)
            return false;

        var targetId = target.EntityId;
        var partyCoord = context.PartyCoordinationService;
        var coordConfig = context.Configuration.PartyCoordination;

        // Check IPC reservation
        if (coordConfig.EnableInterruptCoordination &&
            partyCoord?.IsInterruptTargetReservedByOther(targetId) == true)
        {
            context.Debug.DamageState = "Interrupt reserved by other";
            return false;
        }

        // Calculate remaining cast time in milliseconds
        var remainingCastTime = (target.TotalCastTime - target.CurrentCastTime) * 1000f;
        var castTimeMs = (int)remainingCastTime;

        // Try Head Graze
        if (context.ActionService.IsActionReady(MCHActions.HeadGraze.ActionId))
        {
            // Reserve the interrupt target
            if (coordConfig.EnableInterruptCoordination)
            {
                if (!partyCoord?.ReserveInterruptTarget(targetId, MCHActions.HeadGraze.ActionId, castTimeMs) ?? false)
                {
                    context.Debug.DamageState = "Failed to reserve interrupt";
                    return false;
                }
            }

            if (context.ActionService.ExecuteOgcd(MCHActions.HeadGraze, target.GameObjectId))
            {
                context.Debug.PlannedAction = MCHActions.HeadGraze.Name;
                context.Debug.DamageState = "Interrupted cast";
                return true;
            }

            // Failed to execute, clear reservation
            partyCoord?.ClearInterruptReservation(targetId);
        }

        return false;
    }

    #endregion
}
