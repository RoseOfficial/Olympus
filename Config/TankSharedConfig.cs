using System;

namespace Olympus.Config;

/// <summary>
/// Shared role-action configuration for tank jobs (PLD/WAR/DRK/GNB).
/// </summary>
public sealed class TankSharedConfig
{
    /// <summary>Master toggle for Rampart across all tanks.</summary>
    public bool EnableRampart { get; set; } = true;

    /// <summary>HP percentage threshold to trigger Rampart (0.0 to 1.0). Default 0.85.</summary>
    private float _rampartHpThreshold = 0.85f;
    public float RampartHpThreshold
    {
        get => _rampartHpThreshold;
        set => _rampartHpThreshold = Math.Clamp(value, 0f, 1f);
    }
}
