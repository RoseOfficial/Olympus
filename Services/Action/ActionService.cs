using System;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using Olympus.Data;
using Olympus.Models.Action;

namespace Olympus.Services.Action;

/// <summary>
/// GCD state enumeration.
/// </summary>
public enum GcdState
{
    /// <summary>GCD is ready, can cast immediately.</summary>
    Ready,
    /// <summary>GCD is rolling, waiting for cooldown.</summary>
    Rolling,
    /// <summary>In weave window, can use oGCDs.</summary>
    WeaveWindow,
    /// <summary>Currently casting a spell.</summary>
    Casting,
    /// <summary>In animation lock from recent action.</summary>
    AnimationLock
}

/// <summary>
/// Simplified action execution service (RSR-style reactive).
/// No queuing - calculates and executes best action each frame.
/// </summary>
public sealed unsafe class ActionService
{
    private readonly ActionTracker _actionTracker;

    // GCD tracking state
    private float _lastGcdTotal;
    private float _lastGcdElapsed;
    private float _lastAnimationLock;
    private bool _lastIsCasting;

    // Last executed action (for debugging)
    private ActionDefinition? _lastExecutedAction;
    private DateTime _lastExecuteTime;

    // Track oGCD usage per GCD cycle (prevents double weaving)
    private bool _ogcdUsedThisCycle;

    /// <summary>Current GCD state.</summary>
    public GcdState CurrentGcdState { get; private set; } = GcdState.Ready;

    /// <summary>Time remaining on GCD (0 if ready).</summary>
    public float GcdRemaining => Math.Max(0, _lastGcdTotal - _lastGcdElapsed);

    /// <summary>Animation lock remaining.</summary>
    public float AnimationLockRemaining => Math.Max(0, _lastAnimationLock);

    /// <summary>Whether player is currently casting.</summary>
    public bool IsCasting => _lastIsCasting;

    /// <summary>Whether GCD is ready for a new action.</summary>
    public bool CanExecuteGcd => CurrentGcdState == GcdState.Ready;

    /// <summary>Whether we can weave an oGCD right now.</summary>
    public bool CanExecuteOgcd => IsInWeaveWindow();

    /// <summary>Last executed action (for debugging).</summary>
    public ActionDefinition? LastExecutedAction => _lastExecutedAction;

    public ActionService(ActionTracker actionTracker)
    {
        _actionTracker = actionTracker;
    }

    /// <summary>
    /// Called every frame to update GCD state.
    /// </summary>
    public void Update(bool isCasting)
    {
        var actionManager = ActionManager.Instance();
        if (actionManager == null)
            return;

        _lastIsCasting = isCasting;
        UpdateGcdState(actionManager);
    }

    private void UpdateGcdState(ActionManager* actionManager)
    {
        // Get GCD timing from Stone (shares GCD with all caster spells)
        var gcdGroup = actionManager->GetRecastGroup(1, 119);
        var recastDetail = actionManager->GetRecastGroupDetail(gcdGroup);

        _lastAnimationLock = actionManager->AnimationLock;

        if (recastDetail != null && recastDetail->IsActive)
        {
            _lastGcdTotal = recastDetail->Total;
            _lastGcdElapsed = recastDetail->Elapsed;
        }
        else
        {
            _lastGcdTotal = 0;
            _lastGcdElapsed = 0;
        }

        // Determine current state
        if (_lastIsCasting)
        {
            CurrentGcdState = GcdState.Casting;
        }
        else if (_lastAnimationLock > 0.1f)
        {
            CurrentGcdState = GcdState.AnimationLock;
        }
        else if (GcdRemaining <= 0)
        {
            CurrentGcdState = GcdState.Ready;
            _ogcdUsedThisCycle = false; // Reset for new GCD cycle
        }
        else if (IsInWeaveWindow())
        {
            CurrentGcdState = GcdState.WeaveWindow;
        }
        else
        {
            CurrentGcdState = GcdState.Rolling;
        }
    }

    /// <summary>
    /// Execute a GCD action immediately.
    /// Call this when GCD is ready and you've determined the best action.
    /// </summary>
    /// <returns>True if action was executed successfully.</returns>
    public bool ExecuteGcd(ActionDefinition action, ulong targetId)
    {
        if (!action.IsGCD)
            return false;

        var actionManager = ActionManager.Instance();
        if (actionManager == null)
            return false;

        // Check if action can be executed
        if (actionManager->GetActionStatus(ActionType.Action, action.ActionId) != 0)
            return false;

        // Execute
        var result = actionManager->UseAction(ActionType.Action, action.ActionId, targetId);

        if (result)
        {
            _lastExecutedAction = action;
            _lastExecuteTime = DateTime.UtcNow;

            // Track for statistics
            var gcdDuration = actionManager->GetRecastTime(ActionType.Action, action.ActionId);
            _actionTracker.LogGcdCast(gcdDuration);
        }

        return result;
    }

    /// <summary>
    /// Execute an oGCD action immediately.
    /// Call this during weave windows.
    /// </summary>
    /// <returns>True if action was executed successfully.</returns>
    public bool ExecuteOgcd(ActionDefinition action, ulong targetId)
    {
        if (!action.IsOGCD)
            return false;

        var actionManager = ActionManager.Instance();
        if (actionManager == null)
            return false;

        // Check if action can be executed
        if (actionManager->GetActionStatus(ActionType.Action, action.ActionId) != 0)
            return false;

        // Execute
        var result = actionManager->UseAction(ActionType.Action, action.ActionId, targetId);

        if (result)
        {
            _lastExecutedAction = action;
            _lastExecuteTime = DateTime.UtcNow;
            _ogcdUsedThisCycle = true; // Mark oGCD used this cycle
        }

        return result;
    }

    /// <summary>
    /// Execute a ground-targeted oGCD action at a specific position.
    /// Used for abilities like Asylum, Liturgy of the Bell that place effects on the ground.
    /// </summary>
    /// <returns>True if action was executed successfully.</returns>
    public bool ExecuteGroundTargetedOgcd(ActionDefinition action, Vector3 targetPosition)
    {
        if (!action.IsOGCD)
            return false;

        var actionManager = ActionManager.Instance();
        if (actionManager == null)
            return false;

        // Check if action can be executed
        if (actionManager->GetActionStatus(ActionType.Action, action.ActionId) != 0)
            return false;

        // Execute at target location
        var result = actionManager->UseActionLocation(ActionType.Action, action.ActionId, 0xE0000000, &targetPosition);

        if (result)
        {
            _lastExecutedAction = action;
            _lastExecuteTime = DateTime.UtcNow;
            _ogcdUsedThisCycle = true; // Mark oGCD used this cycle
        }

        return result;
    }

    /// <summary>
    /// Checks if we're in a valid weave window for oGCDs.
    /// </summary>
    public bool IsInWeaveWindow()
    {
        // In weave window if:
        // 1. GCD is rolling (not ready)
        // 2. No animation lock
        // 3. Not casting
        // 4. Enough time for oGCD + animation lock before GCD
        // 5. Haven't already used an oGCD this cycle (single weave only)
        return GcdRemaining > FFXIVTimings.AnimationLockBase + 0.1f
            && AnimationLockRemaining < 0.1f
            && !_lastIsCasting
            && !_ogcdUsedThisCycle;
    }

    /// <summary>
    /// Gets cooldown remaining for a specific action.
    /// </summary>
    public float GetCooldownRemaining(uint actionId)
    {
        var actionManager = ActionManager.Instance();
        if (actionManager == null)
            return float.MaxValue;

        var elapsed = actionManager->GetRecastTimeElapsed(ActionType.Action, actionId);
        var total = actionManager->GetRecastTime(ActionType.Action, actionId);

        if (total <= 0)
            return 0;

        return Math.Max(0, total - elapsed);
    }

    /// <summary>
    /// Checks if a specific action is off cooldown.
    /// </summary>
    public bool IsActionReady(uint actionId)
    {
        return GetCooldownRemaining(actionId) <= 0;
    }

    /// <summary>
    /// Checks if a specific action can be executed right now.
    /// </summary>
    public bool CanExecuteAction(ActionDefinition action)
    {
        var actionManager = ActionManager.Instance();
        if (actionManager == null)
            return false;

        return actionManager->GetActionStatus(ActionType.Action, action.ActionId) == 0;
    }

    /// <summary>
    /// Gets the number of available weave slots before the GCD is ready.
    /// </summary>
    public int GetAvailableWeaveSlots()
    {
        if (AnimationLockRemaining > 0.1f || _lastIsCasting)
            return 0;

        // Each oGCD takes ~0.7s animation lock
        var availableTime = GcdRemaining - 0.1f; // Leave small buffer
        var slots = (int)(availableTime / FFXIVTimings.AnimationLockBase);

        return Math.Max(0, Math.Min(2, slots)); // Max 2 weaves (double weave)
    }

    /// <summary>
    /// Debug info for display.
    /// </summary>
    public string GetDebugInfo()
    {
        var lastAction = _lastExecutedAction?.Name ?? "none";
        var timeSinceLast = (DateTime.UtcNow - _lastExecuteTime).TotalSeconds;

        return $"GCD: {CurrentGcdState} ({GcdRemaining:F2}s) | " +
               $"AnimLock: {AnimationLockRemaining:F2}s | " +
               $"Last: {lastAction} ({timeSinceLast:F1}s ago)";
    }
}
