namespace Olympus.Services.Action;

/// <summary>
/// Interface for action execution service, providing GCD/oGCD state and cooldown information.
/// </summary>
public interface IActionService
{
    /// <summary>
    /// Current GCD state (Ready, Rolling, WeaveWindow, Casting, AnimationLock).
    /// </summary>
    GcdState CurrentGcdState { get; }

    /// <summary>
    /// Time remaining on GCD (0 if ready).
    /// </summary>
    float GcdRemaining { get; }

    /// <summary>
    /// Animation lock remaining.
    /// </summary>
    float AnimationLockRemaining { get; }

    /// <summary>
    /// Whether player is currently casting.
    /// </summary>
    bool IsCasting { get; }

    /// <summary>
    /// Whether GCD is ready for a new action.
    /// </summary>
    bool CanExecuteGcd { get; }

    /// <summary>
    /// Whether we can weave an oGCD right now.
    /// </summary>
    bool CanExecuteOgcd { get; }

    /// <summary>
    /// Checks if a specific action is off cooldown.
    /// </summary>
    bool IsActionReady(uint actionId);

    /// <summary>
    /// Gets cooldown remaining for a specific action.
    /// </summary>
    float GetCooldownRemaining(uint actionId);

    /// <summary>
    /// Gets the number of available weave slots before the GCD is ready.
    /// </summary>
    int GetAvailableWeaveSlots();
}
