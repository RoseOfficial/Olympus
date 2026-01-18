using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ThanatosCore.Context;

namespace Olympus.Rotation.ThanatosCore.Modules;

/// <summary>
/// Handles the Reaper damage rotation.
/// Manages Enshroud sequences, Soul Reaver, combo actions, and resource building.
/// </summary>
public sealed class DamageModule : IThanatosModule
{
    public int Priority => 30; // Lowest priority - damage after utility
    public string Name => "Damage";

    // Threshold for AoE rotation
    private const int AoeThreshold = 3;

    public bool TryExecute(IThanatosContext context, bool isMoving)
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
            FFXIVConstants.MeleeTargetingRange,
            player);

        if (target == null)
        {
            context.Debug.DamageState = "No target";
            return false;
        }

        // Count nearby enemies for AoE decisions
        var enemyCount = context.TargetingService.CountEnemiesInRange(5f, player);
        context.Debug.NearbyEnemies = enemyCount;

        // oGCD Phase - weave damage oGCDs during GCD
        if (context.CanExecuteOgcd)
        {
            if (TryOgcdDamage(context, target, enemyCount))
                return true;
        }

        // GCD Phase
        if (!context.CanExecuteGcd)
        {
            context.Debug.DamageState = "GCD not ready";
            return false;
        }

        // === ENSHROUD STATE ===
        if (context.IsEnshrouded)
        {
            // Priority 1: Perfectio (post-Communio proc)
            if (TryPerfectio(context, target))
                return true;

            // Priority 2: Communio (Enshroud finisher at 1 Lemure Shroud)
            if (TryCommunio(context, target))
                return true;

            // Priority 3: Void/Cross Reaping (Enshroud GCDs)
            if (TryEnshroudGcd(context, target, enemyCount))
                return true;
        }

        // === NORMAL STATE ===

        // Priority 4: Perfectio Parata proc (if carried outside Enshroud)
        if (TryPerfectio(context, target))
            return true;

        // Priority 5: Plentiful Harvest (consume Immortal Sacrifice)
        if (TryPlentifulHarvest(context, target))
            return true;

        // Priority 6: Soul Reaver GCDs (Gibbet/Gallows/Guillotine)
        if (TrySoulReaverGcd(context, target, enemyCount))
            return true;

        // Priority 7: Harvest Moon (if Soulsow available and ranged)
        if (TryHarvestMoon(context, target))
            return true;

        // Priority 8: Shadow of Death / Whorl of Death (maintain debuff)
        if (TryDeathsDesign(context, target, enemyCount))
            return true;

        // Priority 9: Soul Slice / Soul Scythe (build Soul gauge)
        if (TrySoulBuilder(context, target, enemyCount))
            return true;

        // Priority 10: Basic combo rotation
        if (TryBasicCombo(context, target, enemyCount))
            return true;

        context.Debug.DamageState = "No action available";
        return false;
    }

    public void UpdateDebugState(IThanatosContext context)
    {
        // Debug state updated during TryExecute
    }

    #region oGCD Damage

    private bool TryOgcdDamage(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // During Enshroud: Lemure's Slice/Scythe
        if (context.IsEnshrouded)
        {
            if (TryLemuresSlice(context, target, enemyCount))
                return true;

            // Sacrificium (Dawntrail)
            if (TrySacrificium(context, target))
                return true;
        }

        // Soul spenders (outside Enshroud and Soul Reaver)
        if (!context.IsEnshrouded && !context.HasSoulReaver)
        {
            if (TrySoulSpender(context, target, enemyCount))
                return true;
        }

        return false;
    }

    private bool TryLemuresSlice(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.LemuresSlice.MinLevel)
            return false;

        // Need 2 Void Shroud
        if (context.VoidShroud < 2)
            return false;

        var useAoe = enemyCount >= AoeThreshold;
        var action = useAoe ? RPRActions.LemuresScythe : RPRActions.LemuresSlice;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (Void: {context.VoidShroud})";
            return true;
        }

        return false;
    }

    private bool TrySacrificium(IThanatosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.Sacrificium.MinLevel)
            return false;

        // Requires Oblatio proc
        if (!context.HasOblatio)
            return false;

        if (!context.ActionService.IsActionReady(RPRActions.Sacrificium.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(RPRActions.Sacrificium, target.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.Sacrificium.Name;
            context.Debug.DamageState = "Sacrificium";
            return true;
        }

        return false;
    }

    private bool TrySoulSpender(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Need 50 Soul to spend
        if (context.Soul < 50)
            return false;

        // Prioritize Gluttony (2 Soul Reaver stacks, 60s CD)
        if (level >= RPRActions.Gluttony.MinLevel)
        {
            if (context.ActionService.IsActionReady(RPRActions.Gluttony.ActionId))
            {
                // Only use Gluttony if we can spend the Soul Reaver stacks
                // (not about to enter Enshroud)
                if (context.Shroud < 50 || !context.ActionService.IsActionReady(RPRActions.Enshroud.ActionId))
                {
                    if (context.ActionService.ExecuteOgcd(RPRActions.Gluttony, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = RPRActions.Gluttony.Name;
                        context.Debug.DamageState = "Gluttony (2 Soul Reaver)";
                        return true;
                    }
                }
            }
        }

        // Unveiled variants if we have Enhanced buffs
        if (level >= RPRActions.UnveiledGibbet.MinLevel)
        {
            if (context.HasEnhancedGibbet)
            {
                if (context.ActionService.IsActionReady(RPRActions.UnveiledGibbet.ActionId))
                {
                    if (context.ActionService.ExecuteOgcd(RPRActions.UnveiledGibbet, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = RPRActions.UnveiledGibbet.Name;
                        context.Debug.DamageState = "Unveiled Gibbet";
                        return true;
                    }
                }
            }
            else if (context.HasEnhancedGallows)
            {
                if (context.ActionService.IsActionReady(RPRActions.UnveiledGallows.ActionId))
                {
                    if (context.ActionService.ExecuteOgcd(RPRActions.UnveiledGallows, target.GameObjectId))
                    {
                        context.Debug.PlannedAction = RPRActions.UnveiledGallows.Name;
                        context.Debug.DamageState = "Unveiled Gallows";
                        return true;
                    }
                }
            }
        }

        // Basic Blood Stalk / Grim Swathe
        var useAoe = enemyCount >= AoeThreshold && level >= RPRActions.GrimSwathe.MinLevel;
        var action = useAoe ? RPRActions.GrimSwathe : RPRActions.BloodStalk;

        if (level < action.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (1 Soul Reaver)";
            return true;
        }

        return false;
    }

    #endregion

    #region Enshroud GCDs

    private bool TryPerfectio(IThanatosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.Perfectio.MinLevel)
            return false;

        // Requires Perfectio Parata proc (from Communio)
        if (!context.HasPerfectioParata)
            return false;

        if (!context.ActionService.IsActionReady(RPRActions.Perfectio.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(RPRActions.Perfectio, target.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.Perfectio.Name;
            context.Debug.DamageState = "Perfectio";
            return true;
        }

        return false;
    }

    private bool TryCommunio(IThanatosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.Communio.MinLevel)
            return false;

        // Only use at 1 Lemure Shroud remaining (or if timer is low)
        if (context.LemureShroud > 1 && context.EnshroudTimer > 5f)
            return false;

        if (!context.ActionService.IsActionReady(RPRActions.Communio.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(RPRActions.Communio, target.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.Communio.Name;
            context.Debug.DamageState = "Communio (Enshroud finisher)";
            return true;
        }

        return false;
    }

    private bool TryEnshroudGcd(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Need Lemure Shroud to use Enshroud GCDs
        if (context.LemureShroud <= 0)
            return false;

        var useAoe = enemyCount >= AoeThreshold;
        ActionDefinition action;

        if (useAoe)
        {
            action = RPRActions.GrimReaping;
        }
        else
        {
            // Use enhanced version if available
            if (context.HasEnhancedVoidReaping)
                action = RPRActions.VoidReaping;
            else if (context.HasEnhancedCrossReaping)
                action = RPRActions.CrossReaping;
            else
                action = RPRActions.VoidReaping; // Default to Void Reaping
        }

        if (level < action.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (L:{context.LemureShroud})";
            return true;
        }

        return false;
    }

    #endregion

    #region Soul Reaver GCDs

    private bool TrySoulReaverGcd(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Need Soul Reaver to use these
        if (!context.HasSoulReaver)
            return false;

        if (level < RPRActions.Gibbet.MinLevel)
            return false;

        var useAoe = enemyCount >= AoeThreshold;
        ActionDefinition action;

        if (useAoe)
        {
            action = RPRActions.Guillotine;
        }
        else
        {
            // Follow the enhanced buff for optimal damage
            if (context.HasEnhancedGibbet)
                action = RPRActions.Gibbet;
            else if (context.HasEnhancedGallows)
                action = RPRActions.Gallows;
            else
            {
                // No enhanced buff - choose based on positional
                // Gibbet = flank, Gallows = rear
                if (context.IsAtFlank || context.HasTrueNorth || context.TargetHasPositionalImmunity)
                    action = RPRActions.Gibbet;
                else if (context.IsAtRear || context.HasTrueNorth || context.TargetHasPositionalImmunity)
                    action = RPRActions.Gallows;
                else
                    action = RPRActions.Gibbet; // Default
            }
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        string positional = "";
        if (!useAoe)
        {
            positional = action == RPRActions.Gibbet ? " (flank)" : " (rear)";
        }

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name}{positional} [SR:{context.SoulReaverStacks}]";
            return true;
        }

        return false;
    }

    #endregion

    #region Plentiful Harvest

    private bool TryPlentifulHarvest(IThanatosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.PlentifulHarvest.MinLevel)
            return false;

        // Requires Immortal Sacrifice stacks
        if (context.ImmortalSacrificeStacks <= 0)
            return false;

        // Don't use during Soul Reaver
        if (context.HasSoulReaver)
            return false;

        // Don't use during Enshroud
        if (context.IsEnshrouded)
            return false;

        if (!context.ActionService.IsActionReady(RPRActions.PlentifulHarvest.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(RPRActions.PlentifulHarvest, target.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.PlentifulHarvest.Name;
            context.Debug.DamageState = $"Plentiful Harvest ({context.ImmortalSacrificeStacks} stacks)";
            return true;
        }

        return false;
    }

    #endregion

    #region Harvest Moon

    private bool TryHarvestMoon(IThanatosContext context, IBattleChara target)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.HarvestMoon.MinLevel)
            return false;

        // Requires Soulsow buff
        if (!context.HasSoulsow)
            return false;

        // Check distance - use Harvest Moon as ranged filler
        var dx = player.Position.X - target.Position.X;
        var dz = player.Position.Z - target.Position.Z;
        var distance = (float)System.Math.Sqrt(dx * dx + dz * dz);

        // Only use at range or during movement
        if (distance <= FFXIVConstants.MeleeTargetingRange && !context.IsMoving)
            return false;

        if (!context.ActionService.IsActionReady(RPRActions.HarvestMoon.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(RPRActions.HarvestMoon, target.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.HarvestMoon.Name;
            context.Debug.DamageState = "Harvest Moon (ranged)";
            return true;
        }

        return false;
    }

    #endregion

    #region Death's Design

    private bool TryDeathsDesign(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Check if we need to apply/refresh Death's Design
        // Refresh at < 5s remaining to avoid clipping
        if (context.HasDeathsDesign && context.DeathsDesignRemaining > 5f)
            return false;

        var useAoe = enemyCount >= AoeThreshold && level >= RPRActions.WhorlOfDeath.MinLevel;
        var action = useAoe ? RPRActions.WhorlOfDeath : RPRActions.ShadowOfDeath;

        if (level < action.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = context.HasDeathsDesign
                ? $"{action.Name} (refresh {context.DeathsDesignRemaining:F1}s)"
                : $"{action.Name} (apply)";
            return true;
        }

        return false;
    }

    #endregion

    #region Soul Builder

    private bool TrySoulBuilder(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;

        // Soul Slice / Soul Scythe grants 50 Soul
        // Use when Soul < 50 to enable spenders
        if (context.Soul >= 50)
            return false;

        var useAoe = enemyCount >= AoeThreshold && level >= RPRActions.SoulScythe.MinLevel;
        var action = useAoe ? RPRActions.SoulScythe : RPRActions.SoulSlice;

        if (level < action.MinLevel)
            return false;

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (+50 Soul)";
            return true;
        }

        return false;
    }

    #endregion

    #region Basic Combo

    private bool TryBasicCombo(IThanatosContext context, IBattleChara target, int enemyCount)
    {
        var player = context.Player;
        var level = player.Level;
        var useAoe = enemyCount >= AoeThreshold;

        ActionDefinition action;

        if (useAoe && level >= RPRActions.SpinningScythe.MinLevel)
        {
            // AoE combo: Spinning Scythe -> Nightmare Scythe
            if (context.ComboStep == 1 && context.LastComboAction == RPRActions.SpinningScythe.ActionId &&
                level >= RPRActions.NightmareScythe.MinLevel)
            {
                action = RPRActions.NightmareScythe;
            }
            else
            {
                action = RPRActions.SpinningScythe;
            }
        }
        else
        {
            // Single target combo: Slice -> Waxing Slice -> Infernal Slice
            if (context.ComboStep == 2 && context.LastComboAction == RPRActions.WaxingSlice.ActionId &&
                level >= RPRActions.InfernalSlice.MinLevel)
            {
                action = RPRActions.InfernalSlice;
            }
            else if (context.ComboStep == 1 && context.LastComboAction == RPRActions.Slice.ActionId &&
                     level >= RPRActions.WaxingSlice.MinLevel)
            {
                action = RPRActions.WaxingSlice;
            }
            else
            {
                action = RPRActions.Slice;
            }
        }

        if (!context.ActionService.IsActionReady(action.ActionId))
            return false;

        if (context.ActionService.ExecuteGcd(action, target.GameObjectId))
        {
            context.Debug.PlannedAction = action.Name;
            context.Debug.DamageState = $"{action.Name} (combo {context.ComboStep + 1})";
            return true;
        }

        return false;
    }

    #endregion
}
