using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AresCore.Abilities;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.Common.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services;
using Olympus.Services.Training;

namespace Olympus.Rotation.AresCore.Modules;

/// <summary>
/// Handles the Warrior buff management (scheduler-driven).
/// Manages Defiance, Inner Release, and Infuriate.
/// </summary>
public sealed class BuffModule : BaseTankBuffModule<IAresContext>, IAresModule
{
    private readonly IBurstWindowService? _burstWindowService;

    public BuffModule(IBurstWindowService? burstWindowService = null)
    {
        _burstWindowService = burstWindowService;
    }

    private bool ShouldHoldForBurst(float thresholdSeconds = 8f) =>
        BurstHoldHelper.ShouldHoldForBurst(_burstWindowService, thresholdSeconds);

    protected override ActionDefinition GetTankStanceAction() => WARActions.Defiance;
    protected override bool HasJobTankStance(IAresContext context) => context.HasDefiance;
    protected override void SetBuffState(IAresContext context, string state) => context.Debug.BuffState = state;
    protected override void SetPlannedAction(IAresContext context, string action) => context.Debug.PlannedAction = action;

    public override bool TryExecute(IAresContext context, bool isMoving) => false;

    public override void UpdateDebugState(IAresContext context) { }

    public void CollectCandidates(IAresContext context, RotationScheduler scheduler, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return;
        }

        TryPushTankStance(context, scheduler);

        if (!context.Configuration.Tank.EnableDamage)
        {
            context.Debug.BuffState = "Damage disabled";
            return;
        }

        TryPushInnerRelease(context, scheduler);
        TryPushInfuriate(context, scheduler);
    }

    private void TryPushTankStance(IAresContext context, RotationScheduler scheduler)
    {
        var player = context.Player;
        if (player.Level < WARActions.Defiance.MinLevel) return;
        if (!context.Configuration.Tank.AutoTankStance)
        {
            context.Debug.BuffState = "AutoTankStance disabled";
            return;
        }
        if (context.HasDefiance) return;
        if (!context.ActionService.IsActionReady(WARActions.Defiance.ActionId)) return;

        scheduler.PushOgcd(AresAbilities.Defiance, player.GameObjectId, priority: 1,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.Defiance.Name;
                context.Debug.BuffState = "Enabling Defiance";
            });
    }

    private void TryPushInnerRelease(IAresContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableInnerRelease) return;

        var player = context.Player;
        if (player.Level < WARActions.InnerRelease.MinLevel) return;

        if (context.HasInnerRelease)
        {
            context.Debug.BuffState = $"Inner Release active ({context.InnerReleaseStacks} stacks)";
            return;
        }

        if (!context.HasSurgingTempest)
        {
            context.Debug.BuffState = "Waiting for Surging Tempest";
            return;
        }

        if (context.BeastGauge < 50 && context.SurgingTempestRemaining > 15f)
        {
            context.Debug.BuffState = $"Building gauge ({context.BeastGauge}/50)";
            return;
        }

        if (!context.ActionService.IsActionReady(WARActions.InnerRelease.ActionId))
        {
            context.Debug.BuffState = "Inner Release on CD";
            return;
        }

        if (ShouldHoldForBurst(8f))
        {
            context.Debug.BuffState = "Holding Inner Release for burst";
            return;
        }

        var stempRem = context.SurgingTempestRemaining;
        var gauge = context.BeastGauge;

        scheduler.PushOgcd(AresAbilities.InnerRelease, player.GameObjectId, priority: 2,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.InnerRelease.Name;
                context.Debug.BuffState = "Activating Inner Release";
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.InnerRelease.ActionId, WARActions.InnerRelease.Name)
                    .AsTankBurst()
                    .Reason("Inner Release activated - burst window begins.", "Grants 3 stacks making Fell Cleave/Decimate free and guaranteed crit + direct hit.")
                    .Factors($"Surging Tempest active ({stempRem:F1}s)", $"Beast Gauge at {gauge}")
                    .Alternatives("Wait for more gauge", "Hold for raid buffs")
                    .Tip("Use Inner Release on cooldown when Surging Tempest is up.")
                    .Concept("war_inner_release")
                    .Record();
                context.TrainingService?.RecordConceptApplication("war_inner_release", true, "Burst window activated");
            });
    }

    private void TryPushInfuriate(IAresContext context, RotationScheduler scheduler)
    {
        if (!context.Configuration.Tank.EnableInfuriate) return;

        var player = context.Player;
        if (player.Level < WARActions.Infuriate.MinLevel) return;

        if (context.BeastGauge > 50)
        {
            context.Debug.BuffState = $"Gauge too high ({context.BeastGauge})";
            return;
        }

        // During Inner Release: grab Nascent Chaos if we don't have it
        if (context.HasInnerRelease)
        {
            if (context.HasNascentChaos) return;
            if (!context.ActionService.IsActionReady(WARActions.Infuriate.ActionId)) return;
            PushInfuriate(context, scheduler, "Infuriate for Nascent Chaos");
            return;
        }

        var charges = (int)context.ActionService.GetCurrentCharges(WARActions.Infuriate.ActionId);
        if (charges < 1) return;

        if (charges < 2 && ShouldHoldForBurst(8f))
        {
            context.Debug.BuffState = "Holding Infuriate for burst";
            return;
        }

        if (!context.ActionService.IsActionReady(WARActions.Infuriate.ActionId)) return;
        PushInfuriate(context, scheduler, "Infuriate");
    }

    private void PushInfuriate(IAresContext context, RotationScheduler scheduler, string reason)
    {
        var player = context.Player;
        var duringIR = context.HasInnerRelease;
        var gauge = context.BeastGauge;

        scheduler.PushOgcd(AresAbilities.Infuriate, player.GameObjectId, priority: 3,
            onDispatched: _ =>
            {
                context.Debug.PlannedAction = WARActions.Infuriate.Name;
                context.Debug.BuffState = reason;
                TrainingHelper.Decision(context.TrainingService)
                    .Action(WARActions.Infuriate.ActionId, WARActions.Infuriate.Name)
                    .AsTankResource(gauge)
                    .Reason(
                        duringIR
                            ? "Infuriate during Inner Release grants Nascent Chaos."
                            : $"Infuriate to generate 50 Beast Gauge. Current gauge: {gauge}.",
                        "Infuriate grants 50 gauge and during Inner Release also grants Nascent Chaos.")
                    .Factors(duringIR
                        ? new[] { "Inner Release active", "Enables Inner Chaos" }
                        : new[] { $"Gauge at {gauge}", "Building resources" })
                    .Alternatives("Save for Inner Release", "Wait for lower gauge")
                    .Tip("Use Infuriate when gauge ≤50 to avoid overcapping.")
                    .Concept("war_infuriate_gauge")
                    .Record();
                context.TrainingService?.RecordConceptApplication("war_infuriate_gauge", true, duringIR ? "Nascent Chaos generation" : "Gauge building");
            });
    }

    protected override bool TryJobSpecificBuffs(IAresContext context) => false;
    protected override bool TryJobSpecificResourceGeneration(IAresContext context) => false;
}
