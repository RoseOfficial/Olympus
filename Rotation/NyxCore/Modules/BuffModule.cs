using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.NyxCore.Modules;

/// <summary>
/// Handles the Dark Knight buff management.
/// Manages tank stance, Blood Weapon, Delirium, and Living Shadow.
/// </summary>
public sealed class BuffModule : BaseTankBuffModule<INyxContext>, INyxModule
{
    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

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
        if (!context.Configuration.Tank.EnableBloodWeapon) return false;

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

        // Hold Blood Weapon if a burst window is imminent
        if (ShouldHoldForBurst(8f))
        {
            context.Debug.BuffState = "Holding Blood Weapon for burst";
            return false;
        }

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
        if (!context.Configuration.Tank.EnableDelirium) return false;

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
        // 1. Have Darkside active
        // 2. Preferably have gauge for initial Bloodspillers
        if (!context.HasDarkside)
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

        // Hold Delirium if a burst window is imminent
        if (ShouldHoldForBurst(8f))
        {
            context.Debug.BuffState = "Holding Delirium for burst";
            return false;
        }

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
                .Concept(DrkConcepts.Delirium)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.Delirium, wasSuccessful: true);

            return true;
        }

        return false;
    }

    private bool TryLivingShadow(INyxContext context)
    {
        if (!context.Configuration.Tank.EnableLivingShadow) return false;

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

        // Hold Living Shadow if a burst window is imminent
        if (ShouldHoldForBurst(8f))
        {
            context.Debug.BuffState = "Holding Living Shadow for burst";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(DRKActions.LivingShadow, player.GameObjectId))
        {
            context.Debug.PlannedAction = DRKActions.LivingShadow.Name;
            context.Debug.BuffState = "Living Shadow summoned";

            TrainingHelper.Decision(context.TrainingService)
                .Action(DRKActions.LivingShadow.ActionId, DRKActions.LivingShadow.Name)
                .AsTankBurst()
                .Reason(
                    "Living Shadow summoned — autonomous shadow companion deals significant sustained damage.",
                    "Living Shadow costs 50 Blood Gauge and summons a shadow that attacks independently for 20 seconds. It performs a fixed sequence of 7 attacks. Use on cooldown during Darkside for maximum DPS contribution.")
                .Factors($"Blood Gauge at {context.BloodGauge} (50+ required)", "Darkside active", "Living Shadow ready")
                .Alternatives("Save Blood for Bloodspiller (lower total damage)", "Skip (loses major DPS)")
                .Tip("Living Shadow is one of DRK's most powerful abilities. Summon it on cooldown whenever you have 50+ Blood and Darkside is active. It attacks independently, adding to your damage without any extra input.")
                .Concept(DrkConcepts.LivingShadow)
                .Record();

            context.TrainingService?.RecordConceptApplication(DrkConcepts.LivingShadow, wasSuccessful: true);

            return true;
        }

        return false;
    }

    #endregion
}
