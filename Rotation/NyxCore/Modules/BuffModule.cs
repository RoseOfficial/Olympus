using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.NyxCore.Abilities;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.NyxCore.Modules;

/// <summary>
/// Handles the Dark Knight buff management (scheduler-driven).
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

    protected override ActionDefinition GetTankStanceAction() => DRKActions.Grit;
    protected override bool HasJobTankStance(INyxContext context) => context.HasGrit;
    protected override void SetBuffState(INyxContext context, string state) => context.Debug.BuffState = state;
    protected override void SetPlannedAction(INyxContext context, string action) => context.Debug.PlannedAction = action;

    public override bool TryExecute(INyxContext context, bool isMoving) => false;
    public override void UpdateDebugState(INyxContext context) { }

    public void CollectCandidates(INyxContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat) { context.Debug.BuffState = "Not in combat"; return; }

        TryPushTankStance(context, scheduler);
        if (!context.Configuration.Tank.EnableDamage) { context.Debug.BuffState = "Damage disabled"; return; }

        TryPushBloodWeapon(context, scheduler);
        TryPushDelirium(context, scheduler);
        TryPushLivingShadow(context, scheduler);
    }

    private void TryPushTankStance(INyxContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < DRKActions.Grit.MinLevel) return;
        if (!context.Configuration.Tank.AutoTankStance) { context.Debug.BuffState = "AutoTankStance disabled"; return; }
        if (context.HasGrit) return;
        if (!context.ActionService.IsActionReady(DRKActions.Grit.ActionId)) return;

        scheduler.PushOgcd(NyxAbilities.Grit, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.Grit.Name;
                context.Debug.BuffState = "Enabling Grit";
            });
    }

    private void TryPushBloodWeapon(INyxContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableBloodWeapon) return;
        var player = context.Player;
        if (player.Level < DRKActions.BloodWeapon.MinLevel) return;
        if (context.HasBloodWeapon)
        {
            context.Debug.BuffState = $"Blood Weapon ({context.BloodWeaponRemaining:F1}s)";
            return;
        }
        if (!context.ActionService.IsActionReady(DRKActions.BloodWeapon.ActionId)) return;
        if (ShouldHoldForBurst(8f)) { context.Debug.BuffState = "Holding Blood Weapon for burst"; return; }

        scheduler.PushOgcd(NyxAbilities.BloodWeapon, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.BloodWeapon.Name;
                context.Debug.BuffState = "Blood Weapon activated";
            });
    }

    private void TryPushDelirium(INyxContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableDelirium) return;
        var player = context.Player;
        var level = player.Level;
        if (level < DRKActions.Delirium.MinLevel) return;
        if (context.HasDelirium)
        {
            context.Debug.BuffState = $"Delirium active ({context.DeliriumStacks} stacks)";
            return;
        }
        if (!context.HasDarkside) return;
        if (level < 96 && context.BloodGauge < 30) return;
        if (!context.ActionService.IsActionReady(DRKActions.Delirium.ActionId)) return;
        if (ShouldHoldForBurst(8f)) { context.Debug.BuffState = "Holding Delirium for burst"; return; }

        var darksideRem = context.DarksideRemaining;
        var gauge = context.BloodGauge;
        var lv = level;
        scheduler.PushOgcd(NyxAbilities.Delirium, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.Delirium.Name;
                context.Debug.BuffState = "Delirium activated";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DRKActions.Delirium.ActionId, DRKActions.Delirium.Name).AsTankBurst()
                    .Reason("Delirium activated.",
                        lv >= 96
                            ? "Enables Scarlet Delirium combo."
                            : "Grants 3 free Bloodspillers.")
                    .Factors($"Darkside: {darksideRem:F1}s", $"Blood: {gauge}")
                    .Alternatives("Wait for more gauge", "Hold for raid buffs")
                    .Tip("Use Delirium on cooldown with Darkside active.")
                    .Concept(DrkConcepts.Delirium).Record();
                context.TrainingService?.RecordConceptApplication(DrkConcepts.Delirium, true);
            });
    }

    private void TryPushLivingShadow(INyxContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableLivingShadow) return;
        var player = context.Player;
        var level = player.Level;
        if (level < DRKActions.LivingShadow.MinLevel) return;
        if (context.BloodGauge < DRKActions.LivingShadowCost) return;
        if (!context.HasDarkside) return;
        if (context.HasDelirium && level < 96 && context.DeliriumStacks > 1) return;
        if (!context.ActionService.IsActionReady(DRKActions.LivingShadow.ActionId)) return;
        if (ShouldHoldForBurst(8f)) { context.Debug.BuffState = "Holding Living Shadow for burst"; return; }

        var gauge = context.BloodGauge;
        scheduler.PushOgcd(NyxAbilities.LivingShadow, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = DRKActions.LivingShadow.Name;
                context.Debug.BuffState = "Living Shadow summoned";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(DRKActions.LivingShadow.ActionId, DRKActions.LivingShadow.Name).AsTankBurst()
                    .Reason("Living Shadow summoned.", "50 Blood for autonomous shadow damage over 20s.")
                    .Factors($"Blood: {gauge}", "Darkside active")
                    .Alternatives("Save Blood for Bloodspiller")
                    .Tip("Summon Living Shadow on cooldown.")
                    .Concept(DrkConcepts.LivingShadow).Record();
                context.TrainingService?.RecordConceptApplication(DrkConcepts.LivingShadow, true);
            });
    }

    protected override bool TryJobSpecificBuffs(INyxContext context) => false;
    protected override bool TryJobSpecificResourceGeneration(INyxContext context) => false;
}
