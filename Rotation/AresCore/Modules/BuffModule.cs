using Olympus.Data;
using Olympus.Rotation.AresCore.Context;

namespace Olympus.Rotation.AresCore.Modules;

/// <summary>
/// Handles the Warrior buff management.
/// Manages Defiance (tank stance), Inner Release (burst window), and Infuriate (gauge generation).
/// </summary>
public sealed class BuffModule : IAresModule
{
    public int Priority => 20; // After enmity and mitigation
    public string Name => "Buff";

    public bool TryExecute(IAresContext context, bool isMoving)
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

        // Priority 1: Maintain tank stance (Defiance)
        if (TryDefiance(context))
            return true;

        // Priority 2: Inner Release for burst window
        if (TryInnerRelease(context))
            return true;

        // Priority 3: Infuriate for gauge generation
        if (TryInfuriate(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IAresContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Tank Stance

    private bool TryDefiance(IAresContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < WARActions.Defiance.MinLevel)
            return false;

        // Check configuration
        if (!context.Configuration.Tank.AutoTankStance)
        {
            context.Debug.BuffState = "AutoTankStance disabled";
            return false;
        }

        // Already have tank stance
        if (context.HasDefiance)
            return false;

        // Check if Defiance is ready
        if (!context.ActionService.IsActionReady(WARActions.Defiance.ActionId))
            return false;

        if (context.ActionService.ExecuteOgcd(WARActions.Defiance, player.GameObjectId))
        {
            context.Debug.PlannedAction = WARActions.Defiance.Name;
            context.Debug.BuffState = "Enabling Defiance";
            return true;
        }

        return false;
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
