using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Services.Training;

namespace Olympus.Rotation.NyxCore.Modules;

/// <summary>
/// Handles the Dark Knight buff management.
/// Manages tank stance, Blood Weapon, Delirium, and Living Shadow.
/// </summary>
public sealed class BuffModule : BaseTankBuffModule<INyxContext>, INyxModule
{
    #region Abstract Method Implementations

    protected override ActionDefinition GetTankStanceAction() => DRKActions.Grit;

    protected override bool HasJobTankStance(INyxContext context) => context.HasGrit;

    protected override void SetBuffState(INyxContext context, string state) => context.Debug.BuffState = state;

    protected override void SetPlannedAction(INyxContext context, string action) => context.Debug.PlannedAction = action;

    #endregion

    #region Job-Specific Buffs

    protected override bool TryJobSpecificBuffs(INyxContext context)
    {
        // Priority 1: Blood Weapon (MP/Blood regen)
        if (TryBloodWeapon(context))
            return true;

        // Priority 2: Delirium (burst window)
        if (TryDelirium(context))
            return true;

        // Priority 3: Living Shadow (during 2-minute windows)
        if (TryLivingShadow(context))
            return true;

        return false;
    }

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

            // Training: Record burst window activation
            TrainingHelper.Decision(context.TrainingService)
                .Action(DRKActions.Delirium.ActionId, DRKActions.Delirium.Name)
                .AsTankBurst()
                .Reason(
                    "Delirium activated - your burst window begins now. Spam Bloodspiller for massive damage.",
                    level >= 96
                        ? "Delirium at Lv.96+ enables the Scarlet Delirium combo: Scarlet Delirium -> Comeuppance -> Torcleaver. Higher potency than free Bloodspillers."
                        : "Delirium grants 3 stacks that make Bloodspiller free and guarantee crit + direct hit. 60s cooldown - align with raid buffs.")
                .Factors($"Darkside active ({context.DarksideRemaining:F1}s)", $"Blood Gauge at {context.BloodGauge}", "Ready to burst")
                .Alternatives("Wait for more gauge (may overcap)", "Hold for raid buffs (may lose a use)")
                .Tip("Use Delirium on cooldown when Darkside is active. At Lv.96+, you get the Scarlet Delirium combo instead of free Bloodspillers.")
                .Concept("drk_delirium")
                .Record();

            context.TrainingService?.RecordConceptApplication("drk_delirium", true, "Burst window activated");

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
