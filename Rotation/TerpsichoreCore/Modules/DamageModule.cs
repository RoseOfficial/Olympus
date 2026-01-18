using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.TerpsichoreCore.Context;

namespace Olympus.Rotation.TerpsichoreCore.Modules;

/// <summary>
/// Handles the Dancer GCD damage rotation.
/// Manages procs, Esprit spenders, Tillana, and filler GCDs.
/// </summary>
public sealed class DamageModule : ITerpsichoreModule
{
    public int Priority => 30; // Lower priority than buffs
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(ITerpsichoreContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.DamageState = "Not in combat";
            return false;
        }

        // Don't interrupt dances with GCDs (handled by BuffModule)
        if (context.IsDancing)
        {
            context.Debug.DamageState = "Dancing...";
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

        // Count nearby enemies for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = enemyCount;

        // GCD Phase
        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        // === HIGHEST PRIORITY PROCS ===

        // Priority 1: Starfall Dance (expires with Devilment)
        if (TryStarfallDance(context, target))
            return true;

        // Priority 2: Finishing Move (Lv.96+)
        if (TryFinishingMove(context))
            return true;

        // Priority 3: Last Dance (Lv.92+)
        if (TryLastDance(context, target))
            return true;

        // Priority 4: Tillana (after Technical Finish)
        if (TryTillana(context))
            return true;

        // === ESPRIT SPENDERS ===

        // Priority 5: Dance of the Dawn (Lv.100, replaces Saber Dance during buff)
        if (TryDanceOfTheDawn(context, target))
            return true;

        // Priority 6: Saber Dance (Esprit >= 80 to avoid overcap, or >= 50 during burst)
        if (TrySaberDance(context, target, enemyCount))
            return true;

        // === PROC CONSUMERS ===

        // Priority 7: Fountainfall (Silken Flow proc)
        if (TryFountainfall(context, target, enemyCount))
            return true;

        // Priority 8: Reverse Cascade (Silken Symmetry proc)
        if (TryReverseCascade(context, target, enemyCount))
            return true;

        // === COMBO FILLER ===

        // Priority 9: Fountain (combo finisher)
        if (TryFountain(context, target, enemyCount))
            return true;

        // Priority 10: Cascade (combo starter / filler)
        if (TryCascade(context, target, enemyCount))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(ITerpsichoreContext context)
    {
        // Debug state updated during TryExecute
    }

    #region High Priority Procs

    private bool TryStarfallDance(ITerpsichoreContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.StarfallDance.MinLevel)
            return false;

        if (!context.HasFlourishingStarfall)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.StarfallDance.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.StarfallDance, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.StarfallDance.Name;
            context.Debug.DamageState = "Starfall Dance";
            return true;
        }

        return false;
    }

    private bool TryFinishingMove(ITerpsichoreContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.FinishingMove.MinLevel)
            return false;

        if (!context.HasFinishingMoveReady)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.FinishingMove.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.FinishingMove, player.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.FinishingMove.Name;
            context.Debug.DamageState = "Finishing Move";
            return true;
        }

        return false;
    }

    private bool TryLastDance(ITerpsichoreContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.LastDance.MinLevel)
            return false;

        if (!context.HasLastDanceReady)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.LastDance.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.LastDance, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.LastDance.Name;
            context.Debug.DamageState = "Last Dance";
            return true;
        }

        return false;
    }

    private bool TryTillana(ITerpsichoreContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.Tillana.MinLevel)
            return false;

        if (!context.HasFlourishingFinish)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.Tillana.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.Tillana, player.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.Tillana.Name;
            context.Debug.DamageState = "Tillana";
            return true;
        }

        return false;
    }

    #endregion

    #region Esprit Spenders

    private bool TryDanceOfTheDawn(ITerpsichoreContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.DanceOfTheDawn.MinLevel)
            return false;

        if (!context.HasDanceOfTheDawnReady)
            return false;

        // Costs 50 Esprit
        if (context.Esprit < 50)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.DanceOfTheDawn.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.DanceOfTheDawn, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.DanceOfTheDawn.Name;
            context.Debug.DamageState = $"Dance of the Dawn ({context.Esprit} Esprit)";
            return true;
        }

        return false;
    }

    private bool TrySaberDance(ITerpsichoreContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DNCActions.SaberDance.MinLevel)
            return false;

        // Skip if Dance of the Dawn is available (higher potency)
        if (context.HasDanceOfTheDawnReady && level >= DNCActions.DanceOfTheDawn.MinLevel)
            return false;

        // Costs 50 Esprit
        if (context.Esprit < 50)
        {
            context.Debug.DamageState = $"Saber Dance: {context.Esprit}/50 Esprit";
            return false;
        }

        // Use at 80+ to prevent overcap
        // Or use at 50+ during burst
        bool shouldUse = context.Esprit >= 80 ||
                         (context.Esprit >= 50 && (context.HasDevilment || context.HasTechnicalFinish));

        if (!shouldUse)
        {
            context.Debug.DamageState = $"Holding Esprit ({context.Esprit}/50)";
            return false;
        }

        if (!context.ActionService.IsActionReady(DNCActions.SaberDance.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.SaberDance, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.SaberDance.Name;
            context.Debug.DamageState = $"Saber Dance ({context.Esprit} Esprit)";
            return true;
        }

        return false;
    }

    #endregion

    #region Proc Consumers

    private bool TryFountainfall(ITerpsichoreContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (!context.HasSilkenFlow)
            return false;

        // Use AoE version for 3+ targets
        if (enemyCount >= AoeThreshold && level >= DNCActions.Bloodshower.MinLevel)
        {
            if (context.ActionService.IsActionReady(DNCActions.Bloodshower.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DNCActions.Bloodshower, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DNCActions.Bloodshower.Name;
                    context.Debug.DamageState = "Bloodshower (AoE Flow)";
                    return true;
                }
            }
        }

        // Single target
        if (level < DNCActions.Fountainfall.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.Fountainfall.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.Fountainfall, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.Fountainfall.Name;
            context.Debug.DamageState = "Fountainfall (Flow)";
            return true;
        }

        return false;
    }

    private bool TryReverseCascade(ITerpsichoreContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (!context.HasSilkenSymmetry)
            return false;

        // Use AoE version for 3+ targets
        if (enemyCount >= AoeThreshold && level >= DNCActions.RisingWindmill.MinLevel)
        {
            if (context.ActionService.IsActionReady(DNCActions.RisingWindmill.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DNCActions.RisingWindmill, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DNCActions.RisingWindmill.Name;
                    context.Debug.DamageState = "Rising Windmill (AoE Symmetry)";
                    return true;
                }
            }
        }

        // Single target
        if (level < DNCActions.ReverseCascade.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.ReverseCascade.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.ReverseCascade, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.ReverseCascade.Name;
            context.Debug.DamageState = "Reverse Cascade (Symmetry)";
            return true;
        }

        return false;
    }

    #endregion

    #region Combo Filler

    private bool TryFountain(ITerpsichoreContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Use AoE combo finisher for 3+ targets
        if (enemyCount >= AoeThreshold && level >= DNCActions.Bladeshower.MinLevel)
        {
            // Check if we're in AoE combo (last action was Windmill)
            if (context.ComboTimeRemaining > 0 && context.LastComboAction == DNCActions.Windmill.ActionId)
            {
                if (context.ActionService.IsActionReady(DNCActions.Bladeshower.ActionId))
                {
                    if (context.ActionService.ExecuteGcd(DNCActions.Bladeshower, player.GameObjectId))
                    {
                        context.Debug.PlannedAction = DNCActions.Bladeshower.Name;
                        context.Debug.DamageState = "Bladeshower (AoE combo)";
                        return true;
                    }
                }
            }
        }

        // Single target combo finisher
        if (level < DNCActions.Fountain.MinLevel)
            return false;

        // Only use if we're in combo (last action was Cascade)
        if (context.ComboTimeRemaining <= 0 || context.LastComboAction != DNCActions.Cascade.ActionId)
            return false;

        if (!context.ActionService.IsActionReady(DNCActions.Fountain.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.Fountain, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.Fountain.Name;
            context.Debug.DamageState = "Fountain (combo)";
            return true;
        }

        return false;
    }

    private bool TryCascade(ITerpsichoreContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Use AoE starter for 3+ targets
        if (enemyCount >= AoeThreshold && level >= DNCActions.Windmill.MinLevel)
        {
            if (context.ActionService.IsActionReady(DNCActions.Windmill.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DNCActions.Windmill, player.GameObjectId))
                {
                    context.Debug.PlannedAction = DNCActions.Windmill.Name;
                    context.Debug.DamageState = "Windmill (AoE filler)";
                    return true;
                }
            }
        }

        // Single target - Cascade as filler
        if (!context.ActionService.IsActionReady(DNCActions.Cascade.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DNCActions.Cascade, target.GameObjectId))
        {
            context.Debug.PlannedAction = DNCActions.Cascade.Name;
            context.Debug.DamageState = "Cascade (filler)";
            return true;
        }

        return false;
    }

    #endregion
}
