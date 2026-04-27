using System;

namespace Olympus.Config;

/// <summary>
/// Shared role-action configuration for melee DPS jobs (DRG/MNK/NIN/SAM/RPR/VPR).
/// </summary>
public sealed class MeleeSharedConfig
{
    /// <summary>Master toggle for Second Wind across all melee DPS.</summary>
    public bool EnableSecondWind { get; set; } = true;

    /// <summary>HP percentage threshold to trigger Second Wind (0.0 to 1.0). Default 0.50.</summary>
    private float _secondWindHpThreshold = 0.50f;
    public float SecondWindHpThreshold
    {
        get => _secondWindHpThreshold;
        set => _secondWindHpThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>Master toggle for Bloodbath across all melee DPS.</summary>
    public bool EnableBloodbath { get; set; } = true;

    /// <summary>HP percentage threshold to trigger Bloodbath (0.0 to 1.0). Default 0.85.</summary>
    private float _bloodbathHpThreshold = 0.85f;
    public float BloodbathHpThreshold
    {
        get => _bloodbathHpThreshold;
        set => _bloodbathHpThreshold = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Master toggle for True North auto-fire on missed positionals.
    /// Per-rotation positional detection logic determines when True North is needed;
    /// this gate only controls whether the rotation may fire it at all.
    /// </summary>
    public bool EnableTrueNorth { get; set; } = true;
}
