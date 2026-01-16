using Olympus.Data;
using Olympus.Rotation.NyxCore.Context;

namespace Olympus.Rotation.NyxCore.Modules;

/// <summary>
/// Handles the Dark Knight buff management.
/// Manages tank stance, Blood Weapon, Delirium, and Living Shadow.
/// </summary>
public sealed class BuffModule : INyxModule
{
    public int Priority => 20; // Medium priority for buffs
    public string Name => "Buff";

    public bool TryExecute(INyxContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        // Only use buff actions during oGCD windows
        if (!context.CanExecuteOgcd)
            return false;

        var player = context.Player;
        var level = player.Level;

        // Priority 1: Tank stance (Grit) if missing
        if (TryGrit(context))
            return true;

        // Priority 2: Blood Weapon (MP/Blood regen)
        if (TryBloodWeapon(context))
            return true;

        // Priority 3: Delirium (burst window)
        if (TryDelirium(context))
            return true;

        // Priority 4: Living Shadow (during 2-minute windows)
        if (TryLivingShadow(context))
            return true;

        return false;
    }

    public void UpdateDebugState(INyxContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Tank Stance

    private bool TryGrit(INyxContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.Grit.MinLevel)
            return false;

        // Check configuration
        if (!context.Configuration.Tank.AutoTankStance)
        {
            context.Debug.BuffState = "AutoTankStance disabled";
            return false;
        }

        // Already have Grit
        if (context.HasGrit)
        {
            context.Debug.BuffState = "Grit active";
            return false;
        }

        // Only auto-enable if we're in combat and don't have tank stance
        if (!context.ActionService.IsActionReady(DRKActions.Grit.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.Grit, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.Grit.Name;
            context.Debug.BuffState = "Enabling Grit";
            return true;
        }

        return false;
    }

    #endregion

    #region Damage Buffs

    private bool TryBloodWeapon(INyxContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.BloodWeapon.MinLevel)
            return false;

        // Don't use if already active
        if (context.HasBloodWeapon)
        {
            context.Debug.BuffState = $"Blood Weapon ({context.BloodWeaponRemaining:F1}s)";
            return false;
        }

        // Use Blood Weapon on cooldown during combat for MP/Blood generation
        // Best used when we can land 5 weaponskills
        if (!context.ActionService.IsActionReady(DRKActions.BloodWeapon.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.BloodWeapon, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.BloodWeapon.Name;
            context.Debug.BuffState = "Blood Weapon activated";
            return true;
        }

        return false;
    }

    private bool TryDelirium(INyxContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.Delirium.MinLevel)
            return false;

        // Don't use if already active
        if (context.HasDelirium)
        {
            context.Debug.BuffState = $"Delirium active ({context.DeliriumStacks} stacks)";
            return false;
        }

        // Requirements:
        // 1. Have Darkside active (or about to activate)
        // 2. Preferably have gauge for initial Bloodspillers
        if (!context.HasDarkside && context.DarksideRemaining < 5f)
        {
            // Don't pop Delirium without Darkside - wasted damage
            return false;
        }

        // At level 96+, Delirium enables Scarlet Delirium combo
        // At level 68-95, grants 3 free Bloodspillers (need 50 Blood to use efficiently)
        if (level < 96 && context.BloodGauge < 50)
        {
            // Pre-96, want some gauge to start spending during Delirium
            // But don't wait too long if we have Darkside
            if (context.BloodGauge < 30)
                return false;
        }

        if (!context.ActionService.IsActionReady(DRKActions.Delirium.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.Delirium, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.Delirium.Name;
            context.Debug.BuffState = "Delirium activated";
            return true;
        }

        return false;
    }

    private bool TryLivingShadow(INyxContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < DRKActions.LivingShadow.MinLevel)
            return false;

        // Living Shadow costs 50 Blood Gauge
        if (context.BloodGauge < DRKActions.LivingShadowCost)
        {
            return false;
        }

        // Use during burst windows (when Delirium is active or about to be)
        // Or on cooldown if we have gauge and Darkside
        if (!context.HasDarkside)
            return false;

        // Don't use if we'd overcap Blood during Delirium
        // Living Shadow takes time to start attacking, so use early
        if (context.HasDelirium && level < 96)
        {
            // Pre-96 Delirium gives free Bloodspillers, prioritize those
            // Use Living Shadow after spending some stacks
            if (context.DeliriumStacks > 1)
                return false;
        }

        if (!context.ActionService.IsActionReady(DRKActions.LivingShadow.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(DRKActions.LivingShadow, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.LivingShadow.Name;
            context.Debug.BuffState = "Living Shadow summoned";
            return true;
        }

        return false;
    }

    #endregion
}
