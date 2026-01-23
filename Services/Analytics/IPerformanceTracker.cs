using System;
using System.Collections.Generic;

namespace Olympus.Services.Analytics;

/// <summary>
/// Interface for real-time performance tracking during combat.
/// Collects metrics and produces snapshots for analysis.
/// </summary>
public interface IPerformanceTracker
{
    /// <summary>
    /// Whether tracking is currently active (in combat with tracking enabled).
    /// </summary>
    bool IsTracking { get; }

    /// <summary>
    /// Current combat duration in seconds.
    /// Returns 0 when not in combat.
    /// </summary>
    float CombatDuration { get; }

    /// <summary>
    /// Gets the current real-time metrics snapshot.
    /// Returns null if not currently tracking.
    /// </summary>
    CombatMetricsSnapshot? GetCurrentSnapshot();

    /// <summary>
    /// Gets all completed fight sessions from this session.
    /// Most recent first, limited to configured max history.
    /// </summary>
    IReadOnlyList<FightSession> GetSessionHistory();

    /// <summary>
    /// Gets the most recent completed fight session.
    /// Returns null if no sessions recorded.
    /// </summary>
    FightSession? GetLastSession();

    /// <summary>
    /// Gets trend data from recent sessions.
    /// Returns null if insufficient data (fewer than 3 sessions).
    /// </summary>
    PerformanceTrend? GetTrend();

    /// <summary>
    /// Clears all session history.
    /// </summary>
    void ClearHistory();

    /// <summary>
    /// Called each frame to update tracking state.
    /// </summary>
    void Update();

    /// <summary>
    /// Event fired when a new session is completed.
    /// </summary>
    event Action<FightSession>? OnSessionCompleted;

    /// <summary>
    /// Event fired when a near-death event occurs.
    /// </summary>
    event Action<uint, float>? OnNearDeath;

    /// <summary>
    /// Event fired when a party member dies.
    /// </summary>
    event Action<uint>? OnDeath;

    /// <summary>
    /// Records a cooldown use with context for detailed analysis.
    /// Call this when a tracked ability is used.
    /// </summary>
    /// <param name="actionId">The action ID used.</param>
    /// <param name="context">Optional context (e.g., "burst window", "post-raidwide").</param>
    void RecordCooldownUse(uint actionId, string? context = null);

    /// <summary>
    /// Notifies the tracker that a cooldown has become available.
    /// Call this when a tracked ability comes off cooldown.
    /// </summary>
    /// <param name="actionId">The action ID that became ready.</param>
    void OnCooldownBecameReady(uint actionId);

    /// <summary>
    /// Gets detailed cooldown analysis for the last completed fight.
    /// Returns empty list if no session available.
    /// </summary>
    IReadOnlyList<CooldownAnalysis> GetCooldownAnalysis();
}
