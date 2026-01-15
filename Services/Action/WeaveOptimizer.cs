using System.Collections.Generic;
using Olympus.Data;

namespace Olympus.Services.Action;

/// <summary>
/// Optimizes oGCD weaving by managing priorities, timing, and single vs double weave decisions.
/// </summary>
public sealed class WeaveOptimizer : IWeaveOptimizer
{
    // Pending oGCDs sorted by priority
    private readonly SortedList<int, PendingOgcd> _pendingOgcds = new();
    private int _nextSortKey; // Ensures stable sort for same priority

    // Current state from ActionService
    private float _gcdRemaining;
    private float _gcdTotal;
    private float _animationLockRemaining;
    private int _ogcdsUsedThisCycle;

    /// <inheritdoc />
    public WeaveMode RecommendedWeaveMode
    {
        get
        {
            // Can't weave if in animation lock
            if (_animationLockRemaining > FFXIVConstants.WeaveWindowBuffer)
                return WeaveMode.None;

            // Can't weave if GCD not active
            if (_gcdRemaining <= 0)
                return WeaveMode.None;

            // Check available time for weaving
            var availableTime = _gcdRemaining - FFXIVTimings.ClipPreventionBuffer;

            if (availableTime < FFXIVTimings.AnimationLockBase)
                return WeaveMode.None;

            // Late window - only fast oGCDs safe
            if (availableTime < FFXIVTimings.AnimationLockBase + 0.2f)
                return WeaveMode.Late;

            // Check if we can double weave
            if (CanDoubleWeave && _ogcdsUsedThisCycle == 0)
                return WeaveMode.Double;

            return WeaveMode.Single;
        }
    }

    /// <inheritdoc />
    public bool CanDoubleWeave
    {
        get
        {
            // Need enough GCD time for two animation locks plus safety buffer
            var requiredTime = (FFXIVTimings.AnimationLockBase * 2) + FFXIVTimings.ClipPreventionBuffer;
            return _gcdTotal >= FFXIVTimings.DoubleWeaveThreshold &&
                   _gcdRemaining >= requiredTime &&
                   _animationLockRemaining <= FFXIVConstants.WeaveWindowBuffer;
        }
    }

    /// <inheritdoc />
    public float OptimalWeaveTime
    {
        get
        {
            // If in animation lock, wait for it to end
            if (_animationLockRemaining > FFXIVConstants.WeaveWindowBuffer)
                return _animationLockRemaining;

            // If GCD not active, wait for next GCD
            if (_gcdRemaining <= 0)
                return -1f;

            // Can weave now
            return 0f;
        }
    }

    /// <inheritdoc />
    public int RemainingWeaveSlots
    {
        get
        {
            if (_animationLockRemaining > FFXIVConstants.WeaveWindowBuffer)
                return 0;

            if (_gcdRemaining <= FFXIVTimings.AnimationLockBase + FFXIVTimings.ClipPreventionBuffer)
                return 0;

            // Calculate how many oGCDs can fit
            var availableTime = _gcdRemaining - FFXIVTimings.ClipPreventionBuffer;
            var slotsFromTime = (int)(availableTime / FFXIVTimings.AnimationLockBase);

            // Max 2 weaves per cycle, minus already used
            var maxRemaining = 2 - _ogcdsUsedThisCycle;

            return System.Math.Min(slotsFromTime, maxRemaining);
        }
    }

    /// <inheritdoc />
    public void RegisterPendingOgcd(uint actionId, OgcdPriority priority, float animationLock = 0.6f)
    {
        // Check if already registered
        foreach (var kvp in _pendingOgcds)
        {
            if (kvp.Value.ActionId == actionId)
                return;
        }

        // Create composite key: priority * 1000 + sequence for stable sort
        var sortKey = ((int)priority * 1000) + _nextSortKey++;

        _pendingOgcds.Add(sortKey, new PendingOgcd
        {
            ActionId = actionId,
            Priority = priority,
            AnimationLock = animationLock
        });
    }

    /// <inheritdoc />
    public uint GetNextOgcd()
    {
        if (_pendingOgcds.Count == 0)
            return 0;

        // Return highest priority (lowest key) oGCD
        return _pendingOgcds.Values[0].ActionId;
    }

    /// <inheritdoc />
    public void RemoveOgcd(uint actionId)
    {
        int? keyToRemove = null;

        foreach (var kvp in _pendingOgcds)
        {
            if (kvp.Value.ActionId == actionId)
            {
                keyToRemove = kvp.Key;
                break;
            }
        }

        if (keyToRemove.HasValue)
            _pendingOgcds.Remove(keyToRemove.Value);
    }

    /// <inheritdoc />
    public void ClearPendingOgcds()
    {
        _pendingOgcds.Clear();
        _nextSortKey = 0;
    }

    /// <inheritdoc />
    public void Update(float gcdRemaining, float gcdTotal, float animationLockRemaining, int ogcdsUsedThisCycle)
    {
        // Detect new GCD cycle (GCD remaining increased significantly)
        var isNewCycle = gcdRemaining > _gcdRemaining + 0.5f;

        _gcdRemaining = gcdRemaining;
        _gcdTotal = gcdTotal;
        _animationLockRemaining = animationLockRemaining;
        _ogcdsUsedThisCycle = ogcdsUsedThisCycle;

        // Clear pending oGCDs on new GCD cycle
        if (isNewCycle)
            ClearPendingOgcds();
    }

    /// <inheritdoc />
    public bool CanWeaveNow(float animationLock = 0.6f)
    {
        // Can't weave if already in animation lock
        if (_animationLockRemaining > FFXIVConstants.WeaveWindowBuffer)
            return false;

        // Need enough time for the oGCD's animation lock plus safety buffer
        var requiredTime = animationLock + FFXIVTimings.ClipPreventionBuffer;
        return _gcdRemaining >= requiredTime;
    }

    /// <summary>
    /// Internal struct for tracking pending oGCDs.
    /// </summary>
    private struct PendingOgcd
    {
        public uint ActionId;
        public OgcdPriority Priority;
        public float AnimationLock;
    }
}
