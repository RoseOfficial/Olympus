using System.Numerics;
using Olympus.Models.Action;

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

    /// <summary>
    /// Execute a GCD action immediately.
    /// </summary>
    bool ExecuteGcd(ActionDefinition action, ulong targetId);

    /// <summary>
    /// Execute an oGCD action immediately.
    /// </summary>
    bool ExecuteOgcd(ActionDefinition action, ulong targetId);

    /// <summary>
    /// Execute a ground-targeted oGCD action at a specific position.
    /// </summary>
    bool ExecuteGroundTargetedOgcd(ActionDefinition action, Vector3 targetPosition);

    /// <summary>
    /// Checks if a specific action can be executed right now.
    /// </summary>
    bool CanExecuteAction(ActionDefinition action);

    /// <summary>
    /// Gets the current number of charges available for an action.
    /// For non-charge actions, returns 1 if ready, 0 if on cooldown.
    /// </summary>
    uint GetCurrentCharges(uint actionId);

    /// <summary>
    /// Gets the maximum number of charges for an action at a given level.
    /// </summary>
    ushort GetMaxCharges(uint actionId, uint level);
}
