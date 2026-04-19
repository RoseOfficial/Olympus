using System;

namespace Olympus.Config;

/// <summary>
/// Cross-role timeline behavior settings. Controls whether rotations trust
/// timeline mechanic predictions and whether cast-time damage spells are
/// blocked before predicted mechanics.
/// </summary>
public sealed class TimelineConfig
{
    /// <summary>
    /// Master toggle for timeline-based mechanic predictions.
    /// When disabled, rotations ignore timeline data entirely.
    /// </summary>
    public bool EnableTimelinePredictions { get; set; } = true;

    /// <summary>
    /// Minimum timeline confidence required to trust predictions.
    /// Timeline confidence decays over time since the last sync point.
    /// Valid range: 0.5 to 1.0.
    /// </summary>
    private float _timelineConfidenceThreshold = 0.8f;
    public float TimelineConfidenceThreshold
    {
        get => _timelineConfidenceThreshold;
        set => _timelineConfidenceThreshold = Math.Clamp(value, 0.5f, 1f);
    }

    /// <summary>
    /// When enabled, rotations skip cast-time damage GCDs when a raidwide or
    /// tank buster is predicted to hit before the cast would complete.
    /// Applies to all roles with cast-time damage spells.
    /// </summary>
    public bool EnableMechanicAwareCasting { get; set; } = true;
}
