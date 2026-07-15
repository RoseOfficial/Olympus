using System;
using System.Numerics;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using Olympus.Data;
using Olympus.Models;
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
public sealed unsafe class ActionService : IActionService
{
    private readonly IActionTracker _actionTracker;
    private readonly IErrorMetricsService? _errorMetrics;
    private readonly IObjectTable? _objectTable;
    private readonly Configuration? _configuration;

    // GCD tracking state
    private float _lastGcdTotal;
    private float _lastGcdElapsed;
    private float _lastAnimationLock;
    private bool _lastIsCasting;

    // Last executed action (for debugging)
    private ActionDefinition? _lastExecutedAction;
    private DateTime _lastExecuteTime;

    // Track oGCD usage per GCD cycle (allows up to 2 weaves)
    private int _ogcdsUsedThisCycle;

    // Guard so modules can't spam UseAction every frame during the ~0.5s queue window.
    // Set on successful submit while GcdRemaining > 0; cleared at true rollover (GcdRemaining == 0).
    private bool _gcdSubmittedThisCycle;

    // Ping compensation: smoothed request->ActionEffect delay for the local player.
    private float _animLockDelayEstimate;
    private DateTime _lastUseActionUtc = DateTime.MinValue;

    /// <summary>Current GCD state.</summary>
    public GcdState CurrentGcdState { get; private set; } = GcdState.Ready;

    /// <summary>Time remaining on GCD (0 if ready).</summary>
    public float GcdRemaining => Math.Max(0, _lastGcdTotal - _lastGcdElapsed);

    /// <summary>Animation lock remaining.</summary>
    public float AnimationLockRemaining => Math.Max(0, _lastAnimationLock);

    /// <summary>Whether player is currently casting.</summary>
    public bool IsCasting => _lastIsCasting;

    /// <summary>
    /// Whether GCD is ready for a new action.
    /// True during the queue window (last <see cref="FFXIVTimings.QueueWindow"/>s) as well as at true rollover
    /// so we can submit into the server-side action queue and avoid eating a full latency round-trip on every GCD.
    /// </summary>
    public bool CanExecuteGcd => CurrentGcdState == GcdState.Ready;

    /// <summary>Whether we can weave an oGCD right now.</summary>
    public bool CanExecuteOgcd => IsInWeaveWindow();

    /// <summary>Last executed action (for debugging).</summary>
    public ActionDefinition? LastExecutedAction => _lastExecutedAction;

    /// <summary>
    /// Fired after a successful action execution. Used by the action feed overlay.
    /// </summary>
    public event Action<ActionExecutedEvent>? ActionExecuted;

    /// <summary>Smoothed extra animation-lock delay caused by network latency (seconds).</summary>
    public float AnimationLockDelayEstimate => _animLockDelayEstimate;

    public ActionService(IActionTracker actionTracker, IErrorMetricsService? errorMetrics = null, IObjectTable? objectTable = null, Configuration? configuration = null)
    {
        _actionTracker = actionTracker;
        _errorMetrics = errorMetrics;
        _objectTable = objectTable;
        _configuration = configuration;
    }

    /// <summary>
    /// EWMA update for the delay estimate (internal for tests): 20% weight per sample,
    /// samples clamped to [0, 0.3]s.
    /// </summary>
    internal static float SmoothDelaySample(float current, float sample)
    {
        var clamped = Math.Clamp(sample, 0f, 0.3f);
        return current * 0.8f + clamped * 0.2f;
    }

    /// <summary>
    /// Wired by Plugin to ICombatEventService.OnAbilityUsed. Samples the delay between
    /// our last successful UseAction submit and the server's ActionEffect for the local
    /// player. Events more than 1s after the submit are unrelated and skipped.
    /// </summary>
    public void OnLocalActionEffect(DateTime effectUtc)
    {
        if (_lastUseActionUtc == DateTime.MinValue) return;
        var delay = (float)(effectUtc - _lastUseActionUtc).TotalSeconds;
        if (delay is < 0f or > 1f) return;
        _animLockDelayEstimate = SmoothDelaySample(_animLockDelayEstimate, delay);
    }

    /// <summary>
    /// Called every frame to update GCD state.
    /// </summary>
    public void Update(bool isCasting)
    {
        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager is null)
            return;

        _lastIsCasting = isCasting;
        UpdateGcdState(actionManager);
    }

    private void UpdateGcdState(ActionManager* actionManager)
    {
        // Group 57 is hardcoded by the game as the global GCD recast group
        // Works for all jobs (caster, healer, tank, melee, ranged)
        var recastDetail = actionManager->GetRecastGroupDetail(57);

        _lastAnimationLock = actionManager->AnimationLock;

        if (recastDetail is not null && recastDetail->IsActive)
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
        else if (_lastAnimationLock > FFXIVConstants.WeaveWindowBuffer)
        {
            CurrentGcdState = GcdState.AnimationLock;
        }
        else if (GcdRemaining <= 0)
        {
            CurrentGcdState = GcdState.Ready;
            _ogcdsUsedThisCycle = 0; // Reset for new GCD cycle
            _gcdSubmittedThisCycle = false;
        }
        else if (GcdRemaining <= FFXIVTimings.QueueWindow)
        {
            // Queue window: submit the next GCD early so the game's action queue fires it on rollover.
            CurrentGcdState = GcdState.Ready;
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

        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager is null)
            return false;

        // If we already queued a GCD for this cycle, don't spam UseAction every frame during the queue window.
        if (_gcdSubmittedThisCycle && GcdRemaining > 0)
            return false;

        // Do NOT pre-check GetActionStatus here: while the global GCD is rolling it returns 583 ("not ready"),
        // but UseAction still accepts the call in the last ~0.5s and queues the action to fire on rollover.
        // Pre-checking defeats the server-side action queue, so we delegate the "can fire now?" decision to UseAction.
        var result = actionManager->UseAction(ActionType.Action, action.ActionId, targetId);

        if (result)
        {
            if (GcdRemaining > 0)
                _gcdSubmittedThisCycle = true;

            _lastExecutedAction = action;
            _lastExecuteTime = DateTime.UtcNow;
            _lastUseActionUtc = _lastExecuteTime;

            // Track for statistics
            var gcdDuration = actionManager->GetRecastTime(ActionType.Action, action.ActionId);
            _actionTracker.LogGcdCast(gcdDuration);
            _actionTracker.LogAttempt(action.ActionId, null, null, ActionResult.Success, 0);
            RaiseActionExecuted(action);
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

        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager is null)
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
            _lastUseActionUtc = _lastExecuteTime;
            _ogcdsUsedThisCycle++; // Increment oGCD count for double-weave tracking
            _actionTracker.LogAttempt(action.ActionId, null, null, ActionResult.Success, 0);
            RaiseActionExecuted(action);
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

        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager is null)
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
            _lastUseActionUtc = _lastExecuteTime;
            _ogcdsUsedThisCycle++; // Increment oGCD count for double-weave tracking
            _actionTracker.LogAttempt(action.ActionId, null, null, ActionResult.Success, 0);
            RaiseActionExecuted(action);
        }

        return result;
    }

    /// <summary>
    /// Execute a GCD targeting the optimal enemy for a directional AoE (cone/line).
    /// The game auto-faces toward the target, so by picking the right target
    /// we control the cone/line direction to hit the most enemies.
    /// </summary>
    public bool ExecuteDirectionalGcd(ActionDefinition action, ulong optimalTargetId)
    {
        // Just a regular ExecuteGcd with the smart-selected target
        return ExecuteGcd(action, optimalTargetId);
    }

    /// <inheritdoc/>
    public bool ExecuteGcdRaw(ActionDefinition action, uint rawDispatchId, ulong targetId)
    {
        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager is null)
            return false;

        // Same spam guard as ExecuteGcd — the Raw bypass is for validation checks,
        // not for cycle accounting.
        if (_gcdSubmittedThisCycle && GcdRemaining > 0)
            return false;

        var result = actionManager->UseAction(ActionType.Action, rawDispatchId, targetId);

        if (result)
        {
            if (GcdRemaining > 0)
                _gcdSubmittedThisCycle = true;

            _lastExecutedAction = action;
            _lastExecuteTime = DateTime.UtcNow;
            _lastUseActionUtc = _lastExecuteTime;

            var gcdDuration = actionManager->GetRecastTime(ActionType.Action, rawDispatchId);
            _actionTracker.LogGcdCast(gcdDuration);
            _actionTracker.LogAttempt(action.ActionId, null, null, ActionResult.Success, 0);
            RaiseActionExecuted(action);
        }

        return result;
    }

    /// <inheritdoc/>
    public bool ExecuteOgcdRaw(ActionDefinition action, uint rawDispatchId, ulong targetId)
    {
        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager is null)
            return false;

        // NO GetActionStatus pre-check — that's what "Raw" intentionally bypasses.
        var result = actionManager->UseAction(ActionType.Action, rawDispatchId, targetId);

        if (result)
        {
            _lastExecutedAction = action;
            _lastExecuteTime = DateTime.UtcNow;
            _lastUseActionUtc = _lastExecuteTime;
            _ogcdsUsedThisCycle++;
            _actionTracker.LogAttempt(action.ActionId, null, null, ActionResult.Success, 0);
            RaiseActionExecuted(action);
        }

        return result;
    }

    /// <inheritdoc/>
    public bool ExecuteItem(uint itemId, bool preferHq, ulong targetId)
    {
        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager is null) return false;

        var resolvedId = preferHq ? itemId + 1_000_000u : itemId;
        // extraParam: 0xFFFF is the standard "use any quality" sentinel for items.
        return actionManager->UseAction(ActionType.Item, resolvedId, targetId, 0xFFFF);
    }

    /// <inheritdoc/>
    public uint GetAdjustedActionId(uint baseActionId)
    {
        var am = SafeGameAccess.GetActionManager(_errorMetrics);
        if (am == null) return baseActionId;
        return am->GetAdjustedActionId(baseActionId);
    }

    /// <inheritdoc/>
    public bool PlayerHasStatus(uint statusId)
    {
        var player = _objectTable?.LocalPlayer;
        if (player?.StatusList == null) return false;
        foreach (var status in player.StatusList)
        {
            if (status != null && status.StatusId == statusId) return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if we're in a valid weave window for oGCDs. All conditions (not casting,
    /// no animation lock, queue-window tail reserved, per-cycle cap) live in
    /// <see cref="ComputeWeaveSlots"/>.
    /// </summary>
    public bool IsInWeaveWindow() => GetAvailableWeaveSlots() > 0;

    /// <summary>
    /// Gets cooldown remaining for a specific action.
    /// </summary>
    public float GetCooldownRemaining(uint actionId)
    {
        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager is null)
            return float.MaxValue;

        var elapsed = actionManager->GetRecastTimeElapsed(ActionType.Action, actionId);
        var total = actionManager->GetRecastTime(ActionType.Action, actionId);

        if (total <= 0)
            return 0;

        return Math.Max(0, total - elapsed);
    }

    /// <summary>
    /// Checks if a specific action is ready to use.
    /// For charge-based abilities, returns true if any charges are available.
    /// For non-charge abilities, returns true if cooldown is complete.
    /// </summary>
    public bool IsActionReady(uint actionId)
    {
        // For charge-based abilities, check if any charges are available
        // GetCurrentCharges returns 1 for non-charge abilities when ready, 0 when on cooldown
        return GetCurrentCharges(actionId) > 0;
    }

    /// <summary>
    /// Checks if a specific action can be executed right now.
    /// </summary>
    public bool CanExecuteAction(ActionDefinition action)
    {
        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager is null)
            return false;

        return actionManager->GetActionStatus(ActionType.Action, action.ActionId) == 0;
    }

    /// <summary>
    /// Gets the number of oGCDs that still fit before the GCD comes back up.
    /// When <see cref="Configuration.EnablePingCompensation"/> is on, the per-weave cost
    /// includes the smoothed request-to-ActionEffect delay so high-latency players get a
    /// tighter weave window automatically.
    /// </summary>
    public int GetAvailableWeaveSlots()
    {
        var perWeaveCost = FFXIVTimings.AnimationLockBase
            + (_configuration?.EnablePingCompensation == true ? _animLockDelayEstimate : 0f);
        return ComputeWeaveSlots(GcdRemaining, AnimationLockRemaining, _lastIsCasting,
                                 _ogcdsUsedThisCycle, perWeaveCost);
    }

    /// <summary>
    /// Pure weave-capacity computation (internal for unit tests). One more weave fits
    /// when the oGCD's animation lock plus the clip-prevention buffer completes before
    /// the GCD comes back up, capped at 2 per cycle. The queue-window tail (last
    /// <see cref="FFXIVTimings.QueueWindow"/>s) is reserved for the next GCD's early
    /// submit. When no GCD is rolling the answer is 0: the GCD pass dispatches in the
    /// same frame and an oGCD fired at GCD-ready would clip it.
    /// </summary>
    internal static int ComputeWeaveSlots(
        float gcdRemaining, float animationLockRemaining, bool isCasting,
        int ogcdsUsedThisCycle, float perWeaveCost)
    {
        if (isCasting || animationLockRemaining > FFXIVConstants.WeaveWindowBuffer)
            return 0;
        if (gcdRemaining <= FFXIVTimings.QueueWindow)
            return 0;

        var capacityNow = (int)((gcdRemaining - FFXIVTimings.ClipPreventionBuffer) / perWeaveCost);
        var remainingCap = 2 - ogcdsUsedThisCycle;
        return Math.Max(0, Math.Min(capacityNow, remainingCap));
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

    /// <summary>
    /// Gets the current number of charges available for an action.
    /// For non-charge actions, returns 1 if ready, 0 if on cooldown.
    /// </summary>
    public uint GetCurrentCharges(uint actionId)
    {
        var actionManager = SafeGameAccess.GetActionManager(_errorMetrics);
        if (actionManager is null)
            return 0;

        return actionManager->GetCurrentCharges(actionId);
    }

    /// <summary>
    /// Gets the maximum number of charges for an action at a given level.
    /// Pass level 0 to get max charges for current level.
    /// </summary>
    public ushort GetMaxCharges(uint actionId, uint level)
    {
        return ActionManager.GetMaxCharges(actionId, level);
    }

    private void RaiseActionExecuted(ActionDefinition action)
    {
        var handler = ActionExecuted;
        if (handler is null)
            return;

        handler(new ActionExecutedEvent(
            ActionId: action.ActionId,
            ActionName: action.Name,
            IsGcd: action.IsGCD,
            TimestampUtc: _lastExecuteTime));
    }
}
