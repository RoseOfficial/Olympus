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
}
