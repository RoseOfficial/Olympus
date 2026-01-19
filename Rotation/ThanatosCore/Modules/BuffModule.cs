using Olympus.Data;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.ThanatosCore.Modules;

/// <summary>
/// Handles the Reaper buff management.
/// Manages Arcane Circle (party buff), Enshroud (burst state),
/// and Plentiful Harvest (Immortal Sacrifice consumption).
/// </summary>
public sealed class BuffModule : IThanatosModule
{
    public int Priority => 20; // After role actions
    public string Name => "Buff";

    public bool TryExecute(IThanatosContext context, bool isMoving)
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

        // Priority 1: Arcane Circle (party buff)
        if (TryArcaneCircle(context))
            return true;

        // Priority 2: Enshroud (burst state)
        if (TryEnshroud(context))
            return true;

        return false;
    }

    public void UpdateDebugState(IThanatosContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(IThanatosContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region Arcane Circle

    private bool TryArcaneCircle(IThanatosContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.ArcaneCircle.MinLevel)
            return false;

        // Already have Arcane Circle active
        if (context.HasArcaneCircle)
        {
            context.Debug.BuffState = $"AC active ({context.ArcaneCircleRemaining:F1}s)";
            return false;
        }

        // Check if Arcane Circle is ready
        if (!context.ActionService.IsActionReady(RPRActions.ArcaneCircle.ActionId))
        {
            context.Debug.BuffState = "Arcane Circle on cooldown";
            return false;
        }

        // Timeline: Don't waste burst before phase transition
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Arcane Circle (phase soon)";
            return false;
        }

        // Requirements for optimal Arcane Circle usage:
        // 1. Have Death's Design on target (damage buff)
        // 2. Have at least 50 Shroud or be in good resource state
        if (!context.HasDeathsDesign)
        {
            context.Debug.BuffState = "Waiting for Death's Design";
            return false;
        }

        // Use Arcane Circle
        if (context.ActionService.ExecuteOgcd(RPRActions.ArcaneCircle, player.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.ArcaneCircle.Name;
            context.Debug.BuffState = "Activating Arcane Circle";
            return true;
        }

        return false;
    }

    #endregion

    #region Enshroud

    private bool TryEnshroud(IThanatosContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < RPRActions.Enshroud.MinLevel)
            return false;

        // Already in Enshroud
        if (context.IsEnshrouded)
        {
            context.Debug.BuffState = context.Debug.GetEnshroudState();
            return false;
        }

        // Can't Enshroud during Soul Reaver
        if (context.HasSoulReaver)
        {
            context.Debug.BuffState = "In Soul Reaver state";
            return false;
        }

        // Need 50 Shroud to Enshroud
        if (context.Shroud < 50)
        {
            context.Debug.BuffState = $"Need 50 Shroud ({context.Shroud}/50)";
            return false;
        }

        // Check if Enshroud is ready
        if (!context.ActionService.IsActionReady(RPRActions.Enshroud.ActionId))
        {
            context.Debug.BuffState = "Enshroud on cooldown";
            return false;
        }

        // Optimal Enshroud timing:
        // 1. During Arcane Circle window for maximum burst
        // 2. Or when about to cap on Shroud
        bool shouldEnshroud = context.HasArcaneCircle ||
                              context.Shroud >= 90 ||
                              (context.HasDeathsDesign && context.DeathsDesignRemaining > 15f);

        if (!shouldEnshroud)
        {
            context.Debug.BuffState = "Waiting for burst window";
            return false;
        }

        // Make sure Death's Design is on target with good duration
        if (!context.HasDeathsDesign || context.DeathsDesignRemaining < 10f)
        {
            context.Debug.BuffState = "Need Death's Design refresh";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(RPRActions.Enshroud, player.GameObjectId))
        {
            context.Debug.PlannedAction = RPRActions.Enshroud.Name;
            context.Debug.BuffState = "Entering Enshroud";
            return true;
        }

        return false;
    }

    #endregion
}
