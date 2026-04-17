using System;

namespace Olympus.Config;

/// <summary>
/// Shared role-action configuration for all healer jobs (WHM, SCH, AST, SGE).
/// Holds toggles/thresholds for abilities that exist identically on every healer,
/// so the user sets them once instead of four times.
/// </summary>
public sealed class HealerSharedConfig
{
    /// <summary>
    /// Master toggle for Lucid Dreaming across all healers.
    /// </summary>
    public bool EnableLucidDreaming { get; set; } = true;

    /// <summary>
    /// MP percentage threshold to trigger Lucid Dreaming (0.0 to 1.0).
    /// Default 0.70 = fire when MP drops below 70%.
    /// Note: Apollo (WHM) uses a separate predictive MP-forecast system in BuffConfig
    /// and does not consult this threshold directly.
    /// </summary>
    private float _lucidDreamingThreshold = 0.70f;
    public float LucidDreamingThreshold
    {
        get => _lucidDreamingThreshold;
        set => _lucidDreamingThreshold = Math.Clamp(value, 0f, 1f);
    }
}
