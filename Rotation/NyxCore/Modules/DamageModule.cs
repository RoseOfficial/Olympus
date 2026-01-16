using Olympus.Data;
using Olympus.Rotation.NyxCore.Context;

namespace Olympus.Rotation.NyxCore.Modules;

/// <summary>
/// Handles the Dark Knight DPS rotation.
/// Manages Darkside maintenance, combo actions, and gauge spending.
/// </summary>
public sealed class DamageModule : INyxModule
{
    public int Priority => 30; // Lower priority - damage comes after survival
    public string Name => "Damage";

    public bool TryExecute(INyxContext context, bool isMoving)
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
            3f,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);

        // oGCD Phase - Darkside maintenance and damage oGCDs
        if (context.CanExecuteOgcd)
        {
            // Priority 1: Edge/Flood of Shadow with Dark Arts (FREE)
            if (TryDarkArtsProc(context, enemyCount, target.GameObjectId))
                return true;

            // Priority 2: Shadowbringer (high potency oGCD)
            if (TryShadowbringer(context, target.GameObjectId))
                return true;

            // Priority 3: Salted Earth (ground DoT)
            if (TrySaltedEarth(context))
                return true;

            // Priority 4: Salt and Darkness (enhance Salted Earth)
            if (TrySaltAndDarkness(context))
                return true;

            // Priority 5: Edge/Flood of Shadow for Darkside maintenance
            if (TryDarksideMaintenance(context, enemyCount, target.GameObjectId))
                return true;

            // Priority 6: Carve and Spit
            if (TryCarveAndSpit(context, target.GameObjectId))
                return true;

            // Priority 7: Abyssal Drain (AoE situations)
            if (TryAbyssalDrain(context, enemyCount, target.GameObjectId))
                return true;

            // Priority 8: Plunge (gap closer / damage filler)
            if (TryPlunge(context, target.GameObjectId))
                return true;
        }

        // GCD Phase
        if (context.CanExecuteGcd)
        {
            // Priority 1: Delirium combo (Lv.96+)
            if (TryDeliriumCombo(context, target.GameObjectId))
                return true;

            // Priority 2: Disesteem (after Torcleaver)
            if (TryDisesteem(context, target.GameObjectId))
                return true;

            // Priority 3: Blood Gauge spenders
            if (TryBloodSpender(context, enemyCount, target.GameObjectId))
                return true;

            // Priority 4: Combo actions
            if (TryCombo(context, enemyCount, target.GameObjectId))
                return true;
        }

        return false;
    }

    public void UpdateDebugState(INyxContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Actions

    /// <summary>
    /// Use Dark Arts proc (free Edge/Flood from broken TBN).
    /// This is highest priority to not waste the proc.
    /// </summary>
    private bool TryDarkArtsProc(INyxContext context, int enemyCount, ulong targetId)
    {
        if (!context.HasDarkArts)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Dark Arts grants a free Edge/Flood of Shadow
        var action = enemyCount >= 3 ? DRKActions.GetFloodAction(level) : DRKActions.GetEdgeAction(level);

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(action, targetId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = "Dark Arts proc!";
            return true;
        }

        return false;
    }

    private bool TryShadowbringer(INyxContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.Shadowbringer.MinLevel)
            return false;

        // Shadowbringer has 2 charges, use when available
        if (!context.ActionService.IsActionReady(DRKActions.Shadowbringer.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.Shadowbringer, targetId))
        {
            context.Debug.PlannedAction = DRKActions.Shadowbringer.Name;
            context.Debug.DamageState = "Shadowbringer";
            return true;
        }

        return false;
    }

    private bool TrySaltedEarth(INyxContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.SaltedEarth.MinLevel)
            return false;

        // Salted Earth is a ground DoT, use on cooldown
        if (!context.ActionService.IsActionReady(DRKActions.SaltedEarth.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.SaltedEarth, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.SaltedEarth.Name;
            context.Debug.DamageState = "Salted Earth";
            return true;
        }

        return false;
    }

    private bool TrySaltAndDarkness(INyxContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.SaltAndDarkness.MinLevel)
            return false;

        // Salt and Darkness requires Salted Earth to be active
        // Since we can't easily track ground effects, use when off cooldown
        // The game will fail silently if Salted Earth isn't active
        if (!context.ActionService.IsActionReady(DRKActions.SaltAndDarkness.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.SaltAndDarkness, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.SaltAndDarkness.Name;
            context.Debug.DamageState = "Salt and Darkness";
            return true;
        }

        return false;
    }

    /// <summary>
    /// Darkside maintenance logic.
    /// Edge/Flood of Shadow grants 30s Darkside.
    /// Critical to maintain 100% uptime for 10% damage buff.
    /// </summary>
    private bool TryDarksideMaintenance(INyxContext context, int enemyCount, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // Need MP for Edge/Flood
        if (!context.HasEnoughMpForEdge)
        {
            context.Debug.DamageState = "Low MP, can't maintain Darkside";
            return false;
        }

        var action = enemyCount >= 3 ? DRKActions.GetFloodAction(level) : DRKActions.GetEdgeAction(level);

        // Priority 1: Darkside about to expire (< 10s)
        if (context.HasDarkside && context.DarksideRemaining < 10f && context.DarksideRemaining > 0f)
        {
            if (context.ActionService.IsActionReady(action.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(action, targetId))
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.DamageState = $"Darkside refresh ({context.DarksideRemaining:F1}s)";
                    return true;
                }
            }
        }

        // Priority 2: No Darkside at all
        if (!context.HasDarkside)
        {
            if (context.ActionService.IsActionReady(action.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(action, targetId))
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.DamageState = "Darkside activate";
                    return true;
                }
            }
        }

        // Priority 3: MP dump to avoid overcap (>= 9400 MP)
        if (context.CurrentMp >= 9400)
        {
            if (context.ActionService.IsActionReady(action.ActionId))
            {
                if (context.ActionService.ExecuteOgcd(action, targetId))
                {
                    context.Debug.PlannedAction = action.Name;
                    context.Debug.DamageState = "MP dump";
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryCarveAndSpit(INyxContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.CarveAndSpit.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.CarveAndSpit.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.CarveAndSpit, targetId))
        {
            context.Debug.PlannedAction = DRKActions.CarveAndSpit.Name;
            context.Debug.DamageState = "Carve and Spit";
            return true;
        }

        return false;
    }

    private bool TryAbyssalDrain(INyxContext context, int enemyCount, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.AbyssalDrain.MinLevel)
            return false;

        // Abyssal Drain is better in AoE situations (shares cooldown with Carve and Spit)
        if (enemyCount < 2)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.AbyssalDrain.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.AbyssalDrain, targetId))
        {
            context.Debug.PlannedAction = DRKActions.AbyssalDrain.Name;
            context.Debug.DamageState = $"Abyssal Drain ({enemyCount} enemies)";
            return true;
        }

        return false;
    }

    private bool TryPlunge(INyxContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.Plunge.MinLevel)
            return false;

        // Plunge is a gap closer with 2 charges
        // Use as a damage filler when in melee range
        if (!context.ActionService.IsActionReady(DRKActions.Plunge.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.Plunge, targetId))
        {
            context.Debug.PlannedAction = DRKActions.Plunge.Name;
            context.Debug.DamageState = "Plunge";
            return true;
        }

        return false;
    }

    #endregion

    #region GCD Actions

    /// <summary>
    /// Delirium combo at Lv.96+.
    /// Scarlet Delirium -> Comeuppance -> Torcleaver -> Disesteem
    /// </summary>
    private bool TryDeliriumCombo(INyxContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.ScarletDelirium.MinLevel)
            return false;

        // Only available during Delirium
        if (!context.HasDelirium)
            return false;

        // The game handles combo tracking for Delirium combo
        // Just try to use Scarlet Delirium - it will be replaced by combo actions
        if (context.ActionService.IsActionReady(DRKActions.ScarletDelirium.ActionId))
        {
            if (context.ActionService.ExecuteGcd(DRKActions.ScarletDelirium, targetId))
            {
                context.Debug.PlannedAction = "Delirium Combo";
                context.Debug.DamageState = "Delirium combo";
                return true;
            }
        }

        return false;
    }

    private bool TryDisesteem(INyxContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.Disesteem.MinLevel)
            return false;

        // Disesteem requires Scornful Edge buff (from Torcleaver)
        if (!context.HasScornfulEdge)
            return false;

        if (!context.ActionService.IsActionReady(DRKActions.Disesteem.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(DRKActions.Disesteem, targetId))
        {
            context.Debug.PlannedAction = DRKActions.Disesteem.Name;
            context.Debug.DamageState = "Disesteem";
            return true;
        }

        return false;
    }

    private bool TryBloodSpender(INyxContext context, int enemyCount, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // Check if we should spend Blood Gauge
        var shouldSpendBlood = false;

        // During Delirium (pre-96), Bloodspiller is free
        if (context.HasDelirium && level >= DRKActions.Bloodspiller.MinLevel && level < DRKActions.ScarletDelirium.MinLevel)
        {
            shouldSpendBlood = true;
        }
        // Have gauge >= 50 (avoid overcap)
        else if (context.BloodGauge >= DRKActions.BloodspillerCost)
        {
            // Spend if near cap or during burst
            if (context.BloodGauge >= 80 || context.HasDelirium)
                shouldSpendBlood = true;

            // Also spend if we'd overcap from combo finisher
            if (context.ComboStep == 2 && context.BloodGauge >= 80)
                shouldSpendBlood = true;
        }

        if (!shouldSpendBlood)
            return false;

        // Choose between Bloodspiller (ST) and Quietus (AoE)
        if (enemyCount >= 3 && level >= DRKActions.Quietus.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRKActions.Quietus.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRKActions.Quietus, targetId))
                {
                    context.Debug.PlannedAction = DRKActions.Quietus.Name;
                    context.Debug.DamageState = $"Quietus ({context.BloodGauge} Blood)";
                    return true;
                }
            }
        }
        else if (level >= DRKActions.Bloodspiller.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRKActions.Bloodspiller.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRKActions.Bloodspiller, targetId))
                {
                    context.Debug.PlannedAction = DRKActions.Bloodspiller.Name;
                    context.Debug.DamageState = context.HasDelirium ? "Free Bloodspiller!" : $"Bloodspiller ({context.BloodGauge} Blood)";
                    return true;
                }
            }
        }

        return false;
    }

    private bool TryCombo(INyxContext context, int enemyCount, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // AoE combo (3+ enemies)
        if (enemyCount >= 3 && level >= DRKActions.Unleash.MinLevel)
        {
            return TryAoECombo(context, targetId);
        }

        // Single-target combo
        return TrySingleTargetCombo(context, targetId);
    }

    private bool TrySingleTargetCombo(INyxContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // Combo step 2: Souleater (finisher)
        if (context.ComboStep == 2 && level >= DRKActions.Souleater.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRKActions.Souleater.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRKActions.Souleater, targetId))
                {
                    context.Debug.PlannedAction = DRKActions.Souleater.Name;
                    context.Debug.DamageState = "Souleater (combo)";
                    return true;
                }
            }
        }

        // Combo step 1: Syphon Strike
        if (context.ComboStep == 1 && level >= DRKActions.SyphonStrike.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRKActions.SyphonStrike.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRKActions.SyphonStrike, targetId))
                {
                    context.Debug.PlannedAction = DRKActions.SyphonStrike.Name;
                    context.Debug.DamageState = "Syphon Strike (combo)";
                    return true;
                }
            }
        }

        // Combo starter: Hard Slash
        if (context.ActionService.IsActionReady(DRKActions.HardSlash.ActionId))
        {
            if (context.ActionService.ExecuteGcd(DRKActions.HardSlash, targetId))
            {
                context.Debug.PlannedAction = DRKActions.HardSlash.Name;
                context.Debug.DamageState = "Hard Slash (start)";
                return true;
            }
        }

        return false;
    }

    private bool TryAoECombo(INyxContext context, ulong targetId)
    {
        var player = context.Player;
        var level = player.Level;

        // Combo step 1: Stalwart Soul
        if (context.ComboStep == 1 && level >= DRKActions.StalwartSoul.MinLevel)
        {
            if (context.ActionService.IsActionReady(DRKActions.StalwartSoul.ActionId))
            {
                if (context.ActionService.ExecuteGcd(DRKActions.StalwartSoul, targetId))
                {
                    context.Debug.PlannedAction = DRKActions.StalwartSoul.Name;
                    context.Debug.DamageState = "Stalwart Soul (AoE combo)";
                    return true;
                }
            }
        }

        // AoE starter: Unleash
        if (context.ActionService.IsActionReady(DRKActions.Unleash.ActionId))
        {
            if (context.ActionService.ExecuteGcd(DRKActions.Unleash, targetId))
            {
                context.Debug.PlannedAction = DRKActions.Unleash.Name;
                context.Debug.DamageState = "Unleash (AoE start)";
                return true;
            }
        }

        return false;
    }

    #endregion
}
