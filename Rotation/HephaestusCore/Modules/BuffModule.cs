using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HephaestusCore.Abilities;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.HephaestusCore.Modules;

/// <summary>
/// Handles the Gunbreaker buff management.
/// Manages tank stance, No Mercy, and Bloodfest.
/// </summary>
public sealed class BuffModule : BaseTankBuffModule<IHephaestusContext>, IHephaestusModule
{
    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    #region Abstract Method Implementations

    protected override ActionDefinition GetTankStanceAction() => GNBActions.RoyalGuard;

    protected override bool HasJobTankStance(IHephaestusContext context) => context.HasRoyalGuard;

    protected override void SetBuffState(IHephaestusContext context, string state)
        => context.Debug.BuffState = state;

    protected override void SetPlannedAction(IHephaestusContext context, string action)
        => context.Debug.PlannedAction = action;

    #endregion

    public override bool TryExecute(IHephaestusContext context, bool isMoving) => false;

    #region Job-Specific Overrides

    protected override bool TryJobSpecificBuffs(IHephaestusContext context) => false;

    protected override bool TryJobSpecificResourceGeneration(IHephaestusContext context) => false;

    #endregion

    #region CollectCandidates

    public void CollectCandidates(IHephaestusContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return;
        }

        if (!IsDamageEnabled(context))
        {
            context.Debug.BuffState = "Damage disabled";
            return;
        }

        TryPushNoMercy(context, scheduler);
        TryPushBloodfest(context, scheduler);
    }

    private void TryPushNoMercy(IHephaestusContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableNoMercy) return;

        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.NoMercy.MinLevel)
            return;

        if (context.HasNoMercy)
        {
            context.Debug.BuffState = $"No Mercy ({context.NoMercyRemaining:F1}s)";
            return;
        }

        if (level >= GNBActions.DoubleDown.MinLevel && context.Cartridges < 1)
        {
            context.Debug.BuffState = "No Mercy: waiting for cartridges";
            return;
        }

        if (!context.ActionService.IsActionReady(GNBActions.NoMercy.ActionId))
            return;

        if (ShouldHoldForBurst(8f))
        {
            context.Debug.BuffState = "Holding No Mercy for burst";
            return;
        }

        scheduler.PushOgcd(
            GnbAbilities.NoMercy,
            player.GameObjectId,
            priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.NoMercy.Name;
                context.Debug.BuffState = "No Mercy activated";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.NoMercy.ActionId, GNBActions.NoMercy.Name)
                    .AsTankBurst()
                    .Target("Self")
                    .Reason(
                        $"Activating No Mercy with {context.Cartridges} cartridge(s)",
                        "No Mercy is GNB's main damage buff (+20% for 20 seconds, 60s cooldown). " +
                        "The goal is to fit as many high-potency abilities into this window as possible: " +
                        "Double Down, Gnashing Fang combo, Sonic Break, Blasting Zone, and Bow Shock.")
                    .Factors($"Have {context.Cartridges} cartridge(s) to spend", "60s cooldown ready", "Will enable +20% damage for 20 seconds")
                    .Alternatives("Wait for more cartridges (risk holding too long)", "Wait for Gnashing Fang CD (minor optimization)")
                    .Tip("No Mercy is your burst window - plan to have Double Down (2 carts) and Gnashing Fang ready when you press it.")
                    .Concept("gnb_no_mercy")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_no_mercy", true, "Burst window activation");
            });
    }

    private void TryPushBloodfest(IHephaestusContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableBloodfest) return;

        var player = context.Player;
        var level = player.Level;

        if (level < GNBActions.Bloodfest.MinLevel)
            return;

        var maxBenefit = GNBActions.MaxCartridges - context.Cartridges;
        if (maxBenefit < 2)
        {
            context.Debug.BuffState = $"Bloodfest: {context.Cartridges}/3 cartridges (wait)";
            return;
        }

        var nmCooldown = context.ActionService.GetCooldownRemaining(GNBActions.NoMercy.ActionId);
        if (!context.HasNoMercy && context.Cartridges > 0 && nmCooldown < 15f)
            return;

        if (!context.ActionService.IsActionReady(GNBActions.Bloodfest.ActionId))
            return;

        if (!context.HasNoMercy && ShouldHoldForBurst(8f))
        {
            context.Debug.BuffState = "Holding Bloodfest for burst";
            return;
        }

        var duringNoMercy = context.HasNoMercy;
        scheduler.PushOgcd(
            GnbAbilities.Bloodfest,
            player.GameObjectId,
            priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = GNBActions.Bloodfest.Name;
                context.Debug.BuffState = $"Bloodfest (+{GNBActions.BloodfestCartridges} cartridges)";

                TrainingHelper.Decision(context.TrainingService)
                    .Action(GNBActions.Bloodfest.ActionId, GNBActions.Bloodfest.Name)
                    .AsTankResource(context.Cartridges)
                    .Reason(
                        duringNoMercy
                            ? $"Bloodfest during No Mercy ({context.Cartridges} -> 3 cartridges)"
                            : $"Bloodfest to refill cartridges ({context.Cartridges} -> 3)",
                        "Bloodfest instantly grants 3 cartridges (120s cooldown). " +
                        "At Lv.100+, also grants Ready to Reign for the Reign of Beasts combo. " +
                        "Best used when cartridges are low (0-1) during No Mercy to maximize burst damage.")
                    .Factors(
                        $"Currently at {context.Cartridges} cartridges",
                        $"Will gain {GNBActions.BloodfestCartridges - context.Cartridges} net cartridges",
                        duringNoMercy ? "No Mercy active - can spend immediately" : "Refilling for next burst window",
                        player.Level >= 100 ? "Grants Ready to Reign at Lv.100" : "Below Lv.100 - no Reign combo")
                    .Alternatives("Wait for No Mercy (might delay too long)", "Use basic combo to generate cartridges slowly")
                    .Tip("Bloodfest aligns with No Mercy every other burst window. Plan your cartridge spending so you're at 0-1 when Bloodfest is ready.")
                    .Concept("gnb_bloodfest")
                    .Record();
                context.TrainingService?.RecordConceptApplication("gnb_cartridge_gauge", true, "Cartridge refill");
            });
    }

    #endregion

}
