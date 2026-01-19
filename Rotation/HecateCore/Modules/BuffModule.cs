using Olympus.Data;
using Olympus.Rotation.HecateCore.Context;
using Olympus.Timeline.Models;

namespace Olympus.Rotation.HecateCore.Modules;

/// <summary>
/// Handles Black Mage oGCD buffs and cooldowns.
/// Manages Ley Lines, Triplecast, Amplifier, and Manafont.
/// </summary>
public sealed class BuffModule : IHecateModule
{
    public int Priority => 20; // Higher priority than damage (lower number = higher priority)
    public string Name => "Buff";

    public bool TryExecute(IHecateContext context, bool isMoving)
    {
        if (!context.InCombat)
        {
            context.Debug.BuffState = "Not in combat";
            return false;
        }

        if (!context.CanExecuteOgcd)
        {
            context.Debug.BuffState = "oGCD not ready";
            return false;
        }

        var player = context.Player;
        var level = player.Level;

        // Priority 1: Amplifier - Generate Polyglot stack (don't overcap)
        if (TryAmplifier(context))
            return true;

        // Priority 2: Ley Lines - During burst windows
        if (TryLeyLines(context, isMoving))
            return true;

        // Priority 3: Triplecast - For movement or burst
        if (TryTriplecast(context, isMoving))
            return true;

        // Priority 4: Manafont - During Fire phase when low MP
        if (TryManafont(context))
            return true;

        context.Debug.BuffState = "No oGCD needed";
        return false;
    }

    public void UpdateDebugState(IHecateContext context)
    {
        // Debug state updated during TryExecute
    }

    #region Timeline Awareness

    /// <summary>
    /// Checks if burst abilities should be held for an imminent phase transition.
    /// Returns true if a phase transition is expected within the window.
    /// </summary>
    private bool ShouldHoldBurstForPhase(IHecateContext context, float windowSeconds = 8f)
    {
        var nextPhase = context.TimelineService?.GetNextMechanic(TimelineEntryType.Phase);
        if (nextPhase?.IsSoon != true || !nextPhase.Value.IsHighConfidence)
            return false;

        return nextPhase.Value.SecondsUntil <= windowSeconds;
    }

    /// <summary>
    /// Checks if movement is imminent (for Triplecast/Swiftcast planning).
    /// Returns true if a movement mechanic is expected soon.
    /// </summary>
    private bool IsMovementImminent(IHecateContext context, float windowSeconds = 5f)
    {
        var nextMovement = context.TimelineService?.GetNextMechanic(TimelineEntryType.Movement);
        if (nextMovement?.IsSoon != true || !nextMovement.Value.IsHighConfidence)
            return false;

        return nextMovement.Value.SecondsUntil <= windowSeconds;
    }

    #endregion

    #region oGCD Actions

    private bool TryAmplifier(IHecateContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BLMActions.Amplifier.MinLevel)
            return false;

        if (!context.AmplifierReady)
            return false;

        // Don't use if we'd overcap Polyglot (max 3 at Lv.100, 2 before)
        var maxPolyglot = level >= 98 ? 3 : 2;
        if (context.PolyglotStacks >= maxPolyglot)
        {
            context.Debug.BuffState = "Polyglot full, hold Amplifier";
            return false;
        }

        // Only use during Enochian (Polyglot only generates with active element)
        if (!context.IsEnochianActive)
        {
            context.Debug.BuffState = "No Enochian, skip Amplifier";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(BLMActions.Amplifier, player.GameObjectId))
        {
            context.Debug.PlannedAction = BLMActions.Amplifier.Name;
            context.Debug.BuffState = "Amplifier (+1 Polyglot)";
            return true;
        }

        return false;
    }

    private bool TryLeyLines(IHecateContext context, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BLMActions.LeyLines.MinLevel)
            return false;

        if (!context.LeyLinesReady)
            return false;

        // Don't use Ley Lines while moving
        if (isMoving)
        {
            context.Debug.BuffState = "Moving, skip Ley Lines";
            return false;
        }

        // Don't use if already active
        if (context.HasLeyLines)
            return false;

        // Timeline: Don't waste burst before phase transition or movement
        if (ShouldHoldBurstForPhase(context))
        {
            context.Debug.BuffState = "Holding Ley Lines (phase soon)";
            return false;
        }

        if (IsMovementImminent(context))
        {
            context.Debug.BuffState = "Holding Ley Lines (movement soon)";
            return false;
        }

        // Use during Fire phase for maximum DPS
        // Or at the start of combat
        if (!context.InAstralFire && context.InCombat)
        {
            context.Debug.BuffState = "Not in Fire, hold Ley Lines";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(BLMActions.LeyLines, player.GameObjectId))
        {
            context.Debug.PlannedAction = BLMActions.LeyLines.Name;
            context.Debug.BuffState = "Ley Lines placed";
            return true;
        }

        return false;
    }

    private bool TryTriplecast(IHecateContext context, bool isMoving)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BLMActions.Triplecast.MinLevel)
            return false;

        // Need at least 1 charge
        if (context.TriplecastCharges == 0)
            return false;

        // Don't use if already have stacks
        if (context.TriplecastStacksRemaining > 0)
            return false;

        // Use for movement
        if (isMoving && !context.HasInstantCast)
        {
            if (context.ActionService.ExecuteOgcd(BLMActions.Triplecast, player.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Triplecast.Name;
                context.Debug.BuffState = "Triplecast (movement)";
                return true;
            }
        }

        // Timeline: Use proactively for upcoming movement
        if (IsMovementImminent(context) && !context.HasInstantCast)
        {
            if (context.ActionService.ExecuteOgcd(BLMActions.Triplecast, player.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Triplecast.Name;
                context.Debug.BuffState = "Triplecast (prepping for movement)";
                return true;
            }
        }

        // Use during burst (Fire phase with Ley Lines or high stacks available)
        if (context.InAstralFire && context.TriplecastCharges >= 2)
        {
            if (context.ActionService.ExecuteOgcd(BLMActions.Triplecast, player.GameObjectId))
            {
                context.Debug.PlannedAction = BLMActions.Triplecast.Name;
                context.Debug.BuffState = "Triplecast (burst)";
                return true;
            }
        }

        return false;
    }

    private bool TryManafont(IHecateContext context)
    {
        var player = context.Player;
        var level = player.Level;

        if (level < BLMActions.Manafont.MinLevel)
            return false;

        if (!context.ManafontReady)
            return false;

        // Only use in Fire phase when MP is low
        if (!context.InAstralFire)
        {
            context.Debug.BuffState = "Not in Fire, skip Manafont";
            return false;
        }

        // Use when we've used all our Fire IV casts and need MP for Despair
        // Typically when MP is near 0 after Fire IVs
        if (context.CurrentMp > 1600)
        {
            context.Debug.BuffState = "MP too high, skip Manafont";
            return false;
        }

        if (context.ActionService.ExecuteOgcd(BLMActions.Manafont, player.GameObjectId))
        {
            context.Debug.PlannedAction = BLMActions.Manafont.Name;
            context.Debug.BuffState = "Manafont (MP restore)";
            return true;
        }

        return false;
    }

    #endregion
}
