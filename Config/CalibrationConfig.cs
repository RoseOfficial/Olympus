using System;

namespace Olympus.Config;

/// <summary>
/// Persisted calibration data for healing calculations.
/// Stores learned correction factors from observed heal amounts.
/// All numeric values are bounds-checked to prevent invalid configurations.
/// </summary>
public sealed class CalibrationConfig
{
    /// <summary>
    /// Calibrated correction factor from observed heals.
    /// Valid range: 0.5 to 2.0 (reasonable healing formula variance).
    /// </summary>
    private double _calibratedFactor = 1.0;
    public double CalibratedFactor
    {
        get => _calibratedFactor;
        set => _calibratedFactor = Math.Clamp(value, 0.5, 2.0);
    }

    /// <summary>
    /// Number of samples used for calibration.
    /// Valid range: 0 to 1000.
    /// </summary>
    private int _calibrationSamples = 0;
    public int CalibrationSamples
    {
        get => _calibrationSamples;
        set => _calibrationSamples = Math.Clamp(value, 0, 1000);
    }

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
