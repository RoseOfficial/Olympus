namespace Olympus.Services.Action;

/// <summary>
/// Optimizes oGCD weaving by managing priorities, timing, and single vs double weave decisions.
/// </summary>
public interface IWeaveOptimizer
{
    /// <summary>
    /// Gets the current weave mode recommendation.
    /// </summary>
    WeaveMode RecommendedWeaveMode { get; }

    /// <summary>
    /// Gets whether a double weave is safe given current GCD timing.
    /// </summary>
    bool CanDoubleWeave { get; }

    /// <summary>
    /// Gets the optimal time (in seconds from now) to execute the next oGCD.
    /// Returns 0 if should execute immediately, negative if should wait for next GCD.
    /// </summary>
    float OptimalWeaveTime { get; }

    /// <summary>
    /// Registers an oGCD that wants to be used this GCD cycle.
    /// </summary>
    /// <param name="actionId">The action ID of the oGCD.</param>
    /// <param name="priority">Priority level (lower = higher priority).</param>
    /// <param name="animationLock">Expected animation lock of this oGCD.</param>
    void RegisterPendingOgcd(uint actionId, OgcdPriority priority, float animationLock = 0.6f);

    /// <summary>
    /// Gets the highest priority oGCD that should be executed next.
    /// </summary>
    /// <returns>The action ID of the next oGCD to execute, or 0 if none pending.</returns>
    uint GetNextOgcd();

    /// <summary>
    /// Removes an oGCD from the pending queue (e.g., after successful execution).
    /// </summary>
    /// <param name="actionId">The action ID to remove.</param>
    void RemoveOgcd(uint actionId);

    /// <summary>
    /// Clears all pending oGCDs. Called at the start of each GCD cycle.
    /// </summary>
    void ClearPendingOgcds();

    /// <summary>
    /// Updates the optimizer with current GCD state.
    /// Should be called each frame.
    /// </summary>
    /// <param name="gcdRemaining">Time remaining on GCD.</param>
    /// <param name="gcdTotal">Total GCD duration.</param>
    /// <param name="animationLockRemaining">Current animation lock remaining.</param>
    /// <param name="ogcdsUsedThisCycle">Number of oGCDs already used this GCD cycle.</param>
    void Update(float gcdRemaining, float gcdTotal, float animationLockRemaining, int ogcdsUsedThisCycle);

    /// <summary>
    /// Checks if a specific oGCD can be safely weaved right now.
    /// </summary>
    /// <param name="animationLock">The animation lock of the oGCD to check.</param>
    /// <returns>True if the oGCD can be weaved without clipping.</returns>
    bool CanWeaveNow(float animationLock = 0.6f);

    /// <summary>
    /// Gets the number of oGCDs that can still be safely weaved this cycle.
    /// </summary>
    int RemainingWeaveSlots { get; }
}

/// <summary>
/// Recommended weave mode based on current timing.
/// </summary>
public enum WeaveMode
{
    /// <summary>No weaving possible (in animation lock or GCD not active).</summary>
    None,

    /// <summary>Only safe to single weave.</summary>
    Single,

    /// <summary>Safe to double weave.</summary>
    Double,

    /// <summary>Late weave window - only fast oGCDs safe.</summary>
    Late
}

/// <summary>
/// Priority levels for oGCDs. Lower values = higher priority.
/// </summary>
public enum OgcdPriority
{
    /// <summary>Emergency abilities (Benediction, Essential Dignity at low HP).</summary>
    Emergency = 0,

    /// <summary>Buff/debuff maintenance (Lucid Dreaming about to fall off).</summary>
    BuffMaintenance = 10,

    /// <summary>Damage abilities (Assize, Energy Drain).</summary>
    Damage = 20,

    /// <summary>Healing oGCDs (Tetragrammaton, Divine Benison).</summary>
    Healing = 30,

    /// <summary>Resource abilities (Thin Air, Presence of Mind).</summary>
    Resource = 40,

    /// <summary>Defensive/mitigation abilities.</summary>
    Defensive = 50,

    /// <summary>Low priority abilities that can wait.</summary>
    Low = 100
}
