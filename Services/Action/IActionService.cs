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

    /// <summary>
    /// Checks if it's safe to weave an oGCD without clipping the GCD.
    /// Use this before executing oGCDs to prevent DPS loss from GCD delays.
    /// </summary>
    /// <param name="oGcdAnimationLock">Animation lock of the oGCD (default: 0.6s).</param>
    /// <returns>True if the oGCD can be safely weaved without clipping.</returns>
    bool IsSafeToWeave(float oGcdAnimationLock = 0.6f);

    /// <summary>
    /// Checks if a specific oGCD would clip the GCD if used now.
    /// </summary>
    /// <param name="oGcdAnimationLock">Animation lock of the oGCD.</param>
    /// <returns>True if executing the oGCD would cause clipping.</returns>
    bool WouldClipGcd(float oGcdAnimationLock = 0.6f);

    /// <summary>
    /// Gets the WeaveOptimizer for intelligent oGCD timing and prioritization.
    /// </summary>
    IWeaveOptimizer WeaveOptimizer { get; }

    /// <summary>
    /// Execute a GCD targeting the optimal enemy for a directional AoE.
    /// The game auto-faces toward the target, controlling the cone/line direction.
    /// </summary>
    bool ExecuteDirectionalGcd(ActionDefinition action, ulong optimalTargetId);

    /// <summary>
    /// Executes a GCD for a replacement-chain action, bypassing <c>GetActionStatus</c>
    /// and <c>IsGCD</c> pre-flight validation but still performing all per-cycle
    /// accounting (spam guard, <c>_lastExecutedAction</c>, tracker, <c>ActionExecuted</c> event).
    /// </summary>
    /// <param name="action">
    /// The logical action the rotation chose. Used for tracking, analytics, and the
    /// <c>ActionExecuted</c> event — e.g. Raiton (2267) when the rotation decided to cast Raiton.
    /// </param>
    /// <param name="rawDispatchId">
    /// The base action ID that must go to <c>UseAction</c>. Different from
    /// <paramref name="action"/>.ActionId in replacement-chain scenarios — e.g. the
    /// base Ninjutsu ID (2260) that the game replaces with Raiton at execution time.
    /// </param>
    /// <param name="targetId">Target game object ID.</param>
    bool ExecuteGcdRaw(ActionDefinition action, uint rawDispatchId, ulong targetId);

    /// <summary>
    /// Executes an oGCD for a replacement-chain action — counterpart to
    /// <see cref="ExecuteGcdRaw(ActionDefinition, uint, ulong)"/>. Bypasses
    /// <c>GetActionStatus</c> pre-flight validation but still performs
    /// <c>_ogcdsUsedThisCycle</c> tracking, <c>_lastExecutedAction</c>,
    /// tracker logging, and the <c>ActionExecuted</c> event.
    /// </summary>
    /// <param name="action">The logical action chosen by the rotation (for tracking/events).</param>
    /// <param name="rawDispatchId">The base action ID sent to <c>UseAction</c>.</param>
    /// <param name="targetId">Target game object ID.</param>
    bool ExecuteOgcdRaw(ActionDefinition action, uint rawDispatchId, ulong targetId);

    /// <summary>
    /// Returns the action ID the game will actually use if the player presses
    /// the given base action ID right now (accounts for combos, status-procs,
    /// and level upgrades).
    /// </summary>
    uint GetAdjustedActionId(uint baseActionId);

    /// <summary>
    /// Returns true if the local player currently has the given status effect.
    /// Provided as an action-service-level check so the scheduler's proc gate
    /// is mockable without touching native <c>StatusList</c>.
    /// </summary>
    bool PlayerHasStatus(uint statusId);

    /// <summary>
    /// Execute a consumable item (e.g. combat tincture).
    /// Routes through <c>ActionManager.UseAction(ActionType.Item, ...)</c>.
    /// </summary>
    /// <param name="itemId">NQ item ID. HQ resolution happens internally if <paramref name="preferHq"/> is true.</param>
    /// <param name="preferHq">When true, dispatches the HQ variant (NQ + 1_000_000).</param>
    /// <param name="targetId">Target object ID. Pass 0 for self.</param>
    bool ExecuteItem(uint itemId, bool preferHq, ulong targetId);
}
