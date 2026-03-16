using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.Common.Modules;
using Olympus.Services.Training;

namespace Olympus.Rotation.AresCore.Modules;

/// <summary>
/// Handles the Warrior buff management.
/// Manages Defiance (tank stance), Inner Release (burst window), and Infuriate (gauge generation).
/// </summary>
public sealed class BuffModule : BaseTankBuffModule<IAresContext>, IAresModule
{
    #region Abstract Implementation

    protected override ActionDefinition GetTankStanceAction() => WARActions.Defiance;

    protected override bool HasJobTankStance(IAresContext context) => context.HasDefiance;

    protected override void SetBuffState(IAresContext context, string state) => context.Debug.BuffState = state;

    protected override void SetPlannedAction(IAresContext context, string action) => context.Debug.PlannedAction = action;

    #endregion

    #region Job-Specific Overrides

    protected override bool TryJobSpecificBuffs(IAresContext context)
    {
        return TryInnerRelease(context);
    }

    protected override bool TryJobSpecificResourceGeneration(IAresContext context)
    {
        return TryInfuriate(context);
    }

    #endregion

    #region Burst Window

    private bool TryInnerRelease(IAresContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.InnerRelease.MinLevel)
            return false;

        // Don't use if already in Inner Release
        if (context.HasInnerRelease)
        {
            context.Debug.BuffState = $"Inner Release active ({context.InnerReleaseStacks} stacks)";
            return false;
        }

        // Requirements for Inner Release:
        // 1. Surging Tempest must be active (damage buff)
        // 2. Beast Gauge should be at least 50 (to maximize free Fell Cleaves)
        if (!context.HasSurgingTempest)
        {
            context.Debug.BuffState = "Waiting for Surging Tempest";
            return false;
        }

        // Ideally want gauge >= 50 to maximize burst
        // But if Surging Tempest is about to fall off, use anyway
        if (context.BeastGauge < 50 && context.SurgingTempestRemaining > 15f)
        {
            context.Debug.BuffState = $"Building gauge ({context.BeastGauge}/50)";
            return false;
        }

        // Check if Inner Release is ready
        if (!context.ActionService.IsActionReady(WARActions.InnerRelease.ActionId))
        {
            context.Debug.BuffState = "Inner Release on CD";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(WARActions.InnerRelease, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.InnerRelease.Name;
            context.Debug.BuffState = "Activating Inner Release";

            // Training: Record burst window activation
            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.InnerRelease.ActionId, WARActions.InnerRelease.Name)
                .AsTankBurst()
                .Reason(
                    "Inner Release activated - your burst window begins now. Spam Fell Cleave for massive damage.",
                    "Inner Release grants 3 stacks that make Fell Cleave/Decimate free and guaranteed crit + direct hit. 60s cooldown - align with raid buffs.")
                .Factors($"Surging Tempest active ({context.SurgingTempestRemaining:F1}s)", $"Beast Gauge at {context.BeastGauge}", "Ready to burst")
                .Alternatives("Wait for more gauge (minor optimization)", "Hold for raid buffs (may lose a use)")
                .Tip("Use Inner Release on cooldown when Surging Tempest is up. Delaying loses damage - the 60s cooldown means frequent windows.")
                .Concept("war_inner_release")
                .Record();

            context.TrainingService?.RecordConceptApplication("war_inner_release", true, "Burst window activated");

            return true;
        }

        return false;
    }

    #endregion

    #region Gauge Generation

    private bool TryInfuriate(IAresContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Infuriate.MinLevel)
            return false;

        // Don't overcap gauge - Infuriate grants 50 gauge
        if (context.BeastGauge > 50)
        {
            context.Debug.BuffState = $"Gauge too high ({context.BeastGauge})";
            return false;
        }

        // During Inner Release, save one charge of Infuriate for Nascent Chaos
        // Infuriate during IR grants Nascent Chaos which enables Inner Chaos
        if (context.HasInnerRelease)
        {
            // Use Infuriate during IR to get Nascent Chaos if we don't have it
            if (!context.HasNascentChaos)
            {
                return TryExecuteInfuriate(context, "Infuriate for Nascent Chaos");
            }
            // Already have Nascent Chaos, save charge
            return false;
        }

        // Outside of Inner Release, use Infuriate to build gauge
        // But try to save a charge for the next IR window
        // Infuriate has 2 charges at level 74+

        // Check charges
        var charges = GetInfuriateCharges(context);

        // At level 74+, we have 2 charges - try to save one for IR
        // Since we can't easily check charges, use freely when gauge is low
        if (charges >= 1)
        {
            return TryExecuteInfuriate(context, "Infuriate");
        }

        return false;
    }

    private bool TryExecuteInfuriate(IAresContext context, string reason)
    {
        var player = context.Player;

        if (!context.ActionService.IsActionReady(WARActions.Infuriate.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Infuriate, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Infuriate.Name;
            context.Debug.BuffState = reason;

            // Training: Record gauge generation
            var duringIR = context.HasInnerRelease;
            TrainingHelper.Decision(context.TrainingService)
                .Action(WARActions.Infuriate.ActionId, WARActions.Infuriate.Name)
                .AsTankResource(context.BeastGauge)
                .Reason(
                    duringIR
                        ? "Infuriate during Inner Release grants Nascent Chaos, enabling Inner Chaos (highest single-hit potency)."
                        : $"Infuriate to generate 50 Beast Gauge. Current gauge: {context.BeastGauge}.",
                    "Infuriate grants 50 gauge and, during Inner Release, also grants Nascent Chaos for Inner Chaos. 2 charges - don't overcap.")
                .Factors(duringIR
                    ? new[] { "Inner Release active", "Nascent Chaos not active", "Enables Inner Chaos" }
                    : new[] { $"Gauge at {context.BeastGauge} (room for 50)", "Building resources", "Charge available" })
                .Alternatives("Save for Inner Release (may overcap charges)", "Wait for lower gauge (may overcap)")
                .Tip("Use Infuriate when gauge ≤50 to avoid overcapping. During Inner Release, use one to get Nascent Chaos for Inner Chaos.")
                .Concept("war_infuriate_gauge")
                .Record();

            context.TrainingService?.RecordConceptApplication("war_infuriate_gauge", true, duringIR ? "Nascent Chaos generation" : "Gauge building");

            return true;
        }

        return false;
    }

    private int GetInfuriateCharges(IAresContext context)
    {
        // Get current charges of Infuriate
        // This is a simplification - ideally we'd read from the game
        // For now, check if action is ready (has at least 1 charge)
        return context.ActionService.IsActionReady(WARActions.Infuriate.ActionId) ? 1 : 0;
    }

    #endregion
}
