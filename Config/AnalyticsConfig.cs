using System;
using System.Collections.Generic;

namespace Olympus.Config;

/// <summary>
/// Configuration for performance analytics and tracking.
/// </summary>
public sealed class AnalyticsConfig
{
    /// <summary>
    /// Whether analytics tracking is enabled.
    /// When disabled, no metrics are collected to minimize performance impact.
    /// </summary>
    public bool EnableTracking { get; set; } = true;

    /// <summary>
    /// Whether the analytics window is currently open.
    /// </summary>
    public bool AnalyticsWindowVisible { get; set; } = false;

    /// <summary>
    /// Maximum number of fight sessions to keep in memory.
    /// Older sessions are discarded when this limit is reached.
    /// Valid range: 10-100.
    /// </summary>
    private int _maxSessionHistory = 50;
    public int MaxSessionHistory
    {
        get => _maxSessionHistory;
        set => _maxSessionHistory = Math.Clamp(value, 10, 100);
    }

    /// <summary>
    /// HP threshold for "near-death" events (percentage).
    /// When a party member drops below this HP%, it counts as a near-death event.
    /// Valid range: 5-30%.
    /// </summary>
    private float _nearDeathThreshold = 0.15f;
    public float NearDeathThreshold
    {
        get => _nearDeathThreshold;
        set => _nearDeathThreshold = Math.Clamp(value, 0.05f, 0.30f);
    }

    /// <summary>
    /// Minimum combat duration (seconds) to record a session.
    /// Fights shorter than this are not tracked to avoid clutter from trash pulls.
    /// Valid range: 5-60 seconds.
    /// </summary>
    private float _minCombatDuration = 10f;
    public float MinCombatDuration
    {
        get => _minCombatDuration;
        set => _minCombatDuration = Math.Clamp(value, 5f, 60f);
    }

    /// <summary>
    /// Whether to track cooldown drift (late usage of abilities).
    /// Disabling reduces memory usage but loses cooldown efficiency metrics.
    /// </summary>
    public bool TrackCooldownDrift { get; set; } = true;

    /// <summary>
    /// Whether to track downtime breakdown (movement, death, mechanics, unexplained).
    /// Provides detailed explanation of why GCD uptime was lost.
    /// </summary>
    public bool TrackDowntimeBreakdown { get; set; } = true;

    /// <summary>
    /// Whether to track detailed cooldown usage (per-use records, missed opportunities).
    /// Provides enhanced analysis with actionable feedback.
    /// Slightly higher memory usage when enabled.
    /// </summary>
    public bool TrackCooldownDetails { get; set; } = true;

    /// <summary>
    /// Cooldowns to track for efficiency metrics.
    /// Maps action ID to cooldown duration in seconds.
    /// Populated automatically based on job when tracking starts.
    /// </summary>
    public Dictionary<uint, float> TrackedCooldowns { get; set; } = new();

    /// <summary>
    /// Section visibility for the analytics window.
    /// Key is section name, value is whether it's visible.
    /// </summary>
    public Dictionary<string, bool> SectionVisibility { get; set; } = new()
    {
        // Realtime tab
        ["RealtimeCombatStatus"] = true,
        ["RealtimeMetrics"] = true,
        ["RealtimeCooldowns"] = true,

        // Fight Summary tab
        ["SummaryScores"] = true,
        ["SummaryBreakdown"] = true,
        ["SummaryDowntime"] = true,
        ["SummaryCooldowns"] = true,
        ["SummaryIssues"] = true,

        // History tab
        ["HistorySessions"] = true,
        ["HistoryTrends"] = true,
    };
}
