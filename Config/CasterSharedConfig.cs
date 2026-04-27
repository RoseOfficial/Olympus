using System;

namespace Olympus.Config;

/// <summary>
/// Shared role-action configuration for caster DPS jobs (BLM/SMN/RDM/PCT).
/// Holds toggles/thresholds for abilities that exist identically on every caster,
/// so the user sets them once instead of four times.
/// </summary>
public sealed class CasterSharedConfig
{
    /// <summary>Master toggle for Lucid Dreaming across all caster DPS.</summary>
    public bool EnableLucidDreaming { get; set; } = true;

    /// <summary>
    /// MP percentage threshold to trigger Lucid Dreaming (0.0 to 1.0).
    /// Default 0.70 = fire when MP drops below 70%.
    /// </summary>
    private float _lucidDreamingMpThreshold = 0.70f;
    public float LucidDreamingMpThreshold
    {
        get => _lucidDreamingMpThreshold;
        set => _lucidDreamingMpThreshold = Math.Clamp(value, 0f, 1f);
    }
}
