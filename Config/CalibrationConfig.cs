using System;

namespace Olympus.Config;

/// <summary>
/// Persisted calibration data for healing calculations.
/// Stores learned correction factors from observed heal amounts.
/// </summary>
public sealed class CalibrationConfig
{
    /// <summary>Calibrated correction factor from observed heals.</summary>
    public double CalibratedFactor { get; set; } = 1.0;

    /// <summary>Number of samples used for calibration.</summary>
    public int CalibrationSamples { get; set; } = 0;

    /// <summary>Timestamp (UTC ticks) when calibration was last updated.</summary>
    public long LastCalibrationTicks { get; set; } = 0;

    /// <summary>Maximum age in days before calibration is considered stale.</summary>
    public const int MaxCalibrationAgeDays = 7;

    /// <summary>
    /// Checks if calibration data is still valid (has enough samples and is not too old).
    /// </summary>
    public bool IsValid()
    {
        if (CalibrationSamples < 3)
            return false;

        var age = DateTime.UtcNow.Ticks - LastCalibrationTicks;
        return age < TimeSpan.FromDays(MaxCalibrationAgeDays).Ticks;
    }
}
