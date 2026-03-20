using System;
using System.Collections.Generic;
using System.Numerics;
using Olympus.Models;
using Olympus.Services.Analytics;

namespace Olympus.Services;

/// <summary>
/// Tracks action attempts for debugging and GCD uptime analysis.
/// </summary>
public interface IActionTracker
{
    // Debug: current GCD state (updated each frame)
    float DebugGcdRemaining { get; }
    bool DebugIsCasting { get; }
    bool DebugHasAnimLock { get; }
    bool DebugGcdReady { get; }
    bool DebugIsActive { get; }

    // Debug: last downtime event
    string LastDowntimeReason { get; }
    DateTime LastDowntimeTime { get; }
    int DowntimeEventCount { get; }

    int HistorySize { get; }

    /// <summary>
    /// Log an action attempt with full details.
    /// </summary>
    void LogAttempt(
        uint actionId,
        string? targetName,
        uint? targetHp,
        ActionResult result,
        byte playerLevel,
        uint? statusCode = null);

    /// <summary>
    /// Get a cached copy of the action history.
    /// Only regenerates when history has changed, avoiding per-frame allocations.
    /// </summary>
    IReadOnlyList<ActionAttempt> GetHistory();

    /// <summary>
    /// Track GCD state each frame - call this every frame when you have a target.
    /// Basic overload for backwards compatibility.
    /// </summary>
    void TrackGcdState(bool gcdReady, float gcdRemaining = 0, bool isCasting = false, bool hasAnimLock = false, bool isActive = false);

    /// <summary>
    /// Track GCD state each frame with downtime categorization.
    /// </summary>
    void TrackGcdState(
        bool gcdReady,
        float gcdRemaining,
        bool isCasting,
        bool hasAnimLock,
        bool isActive,
        bool playerAlive,
        Vector3 playerPosition,
        bool inMechanicWindow);

    /// <summary>
    /// Get breakdown of downtime by cause.
    /// </summary>
    DowntimeBreakdown GetDowntimeBreakdown();

    /// <summary>
    /// Get recorded incapacitation windows (Willful, Stun, etc.) from the current/last combat session.
    /// </summary>
    IReadOnlyList<(DateTime Start, DateTime End)> GetIncapacitationWindows();

    /// <summary>
    /// Start tracking combat time. Call when player enters combat.
    /// </summary>
    void StartCombat();

    /// <summary>
    /// Stop tracking combat time. Call when player leaves combat.
    /// Caches the final uptime so it persists for review after combat.
    /// </summary>
    void EndCombat();

    /// <summary>
    /// Record a GCD cast with its duration (XIVAnalysis style).
    /// </summary>
    void LogGcdCast(float gcdDuration);

    /// <summary>
    /// Get current GCD uptime percentage using XIVAnalysis methodology.
    /// Returns cached value after combat ends, resets when new combat starts.
    /// </summary>
    float GetGcdUptime();

    /// <summary>
    /// Get average time between successful casts.
    /// </summary>
    float GetAverageTimeBetweenCasts();

    /// <summary>
    /// Get success rate percentage.
    /// </summary>
    float GetSuccessRate();

    /// <summary>
    /// Get the most common failure reason.
    /// </summary>
    (ActionResult reason, int count)? GetMostCommonFailure();

    /// <summary>
    /// Get statistics summary.
    /// </summary>
    (int total, int success, float successRate, float gcdUptime, float avgCastGap) GetStatistics();

    /// <summary>
    /// Clear all tracking data.
    /// </summary>
    void Clear();

    /// <summary>
    /// Resets only the spell usage counts dictionary.
    /// </summary>
    void ClearSpellUsageCounts();

    /// <summary>
    /// Get spell usage counts with resolved names, sorted by count descending.
    /// </summary>
    List<(string name, uint actionId, int count)> GetSpellUsageCounts();
}
