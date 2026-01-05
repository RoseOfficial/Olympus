using System;
using System.Collections.Generic;
using Olympus.Config;
using Olympus.Data;

namespace Olympus.Services.Calculation;

/// <summary>
/// Calculates heal amounts using the actual FFXIV healing formula.
/// Based on AkhMorning research: https://www.akhmorning.com/allagan-studies/how-to-be-a-math-wizard/shadowbringers/damage-and-healing/
/// Includes auto-calibration to learn the correct multiplier from observed heals.
/// </summary>
public static class HealingCalculator
{
    // Thread safety lock for calibration data
    private static readonly object _calibrationLock = new();

    // Auto-calibration: tracks predicted vs actual to refine the correction factor
    private static double _calibratedFactor = 1.0;
    private static int _calibrationSamples = 0;
    private const int MaxCalibrationSamples = 20;
    private const double DefaultFactor = 1.10; // Starting point based on testing

    /// <summary>
    /// Call this when an actual heal is observed to calibrate the formula.
    /// </summary>
    /// <param name="predicted">The predicted heal amount (before correction).</param>
    /// <param name="actual">The actual heal amount observed.</param>
    public static void CalibrateFromActual(int predicted, int actual)
    {
        if (predicted <= 0 || actual <= 0)
            return;

        var observedFactor = (double)actual / predicted;

        // Sanity check - factor should be reasonable
        if (observedFactor < FFXIVConstants.MinCalibrationFactor || observedFactor > FFXIVConstants.MaxCalibrationFactor)
            return;

        lock (_calibrationLock)
        {
            // Weighted average: give more weight to existing samples as we accumulate
            if (_calibrationSamples == 0)
            {
                _calibratedFactor = observedFactor;
            }
            else
            {
                var weight = Math.Min(_calibrationSamples, MaxCalibrationSamples);
                _calibratedFactor = (_calibratedFactor * weight + observedFactor) / (weight + 1);
            }

            _calibrationSamples = Math.Min(_calibrationSamples + 1, MaxCalibrationSamples);
        }
    }

    /// <summary>
    /// Gets the current calibrated correction factor.
    /// </summary>
    public static double GetCorrectionFactor()
    {
        lock (_calibrationLock)
        {
            return _calibrationSamples >= 3 ? _calibratedFactor : DefaultFactor;
        }
    }

    /// <summary>
    /// Resets calibration data.
    /// </summary>
    public static void ResetCalibration()
    {
        lock (_calibrationLock)
        {
            _calibratedFactor = 1.0;
            _calibrationSamples = 0;
        }
    }

    /// <summary>
    /// Loads calibration data from persisted configuration.
    /// Only loads if the saved data is valid (has enough samples, is not too old, and factor is in valid range).
    /// </summary>
    public static void LoadCalibration(CalibrationConfig config)
    {
        lock (_calibrationLock)
        {
            // Validate data is recent, has enough samples, and factor is within valid range
            if (config.IsValid() &&
                config.CalibratedFactor >= FFXIVConstants.MinCalibrationFactor &&
                config.CalibratedFactor <= FFXIVConstants.MaxCalibrationFactor)
            {
                _calibratedFactor = config.CalibratedFactor;
                _calibrationSamples = config.CalibrationSamples;
            }
        }
    }

    /// <summary>
    /// Saves current calibration data to configuration for persistence.
    /// </summary>
    public static void SaveCalibration(CalibrationConfig config)
    {
        lock (_calibrationLock)
        {
            config.CalibratedFactor = _calibratedFactor;
            config.CalibrationSamples = _calibrationSamples;
            config.LastCalibrationTicks = DateTime.UtcNow.Ticks;
        }
    }

    /// <summary>
    /// Level modifiers from the game data.
    /// Format: (MAIN, SUB, DIV)
    /// </summary>
    private static readonly Dictionary<int, (int Main, int Sub, int Div)> LevelMods = new()
    {
        { 1, (20, 56, 56) },
        { 10, (40, 76, 96) },
        { 20, (60, 96, 136) },
        { 30, (100, 136, 176) },
        { 40, (140, 176, 216) },
        { 50, (202, 341, 341) },
        { 60, (218, 354, 858) },
        { 70, (292, 364, 2170) },
        { 80, (340, 380, 1900) },
        { 90, (390, 400, 1900) },
        { 100, (440, 420, 2780) },
    };

    /// <summary>
    /// Healer job modifier for Mind stat.
    /// WHM, SCH, AST, SGE all use 115.
    /// </summary>
    private const int HealerMindJobMod = 115;

    /// <summary>
    /// Calculates the expected heal amount using the FFXIV healing formula.
    /// </summary>
    /// <param name="potency">The healing potency of the action.</param>
    /// <param name="mind">Player's current Mind stat.</param>
    /// <param name="determination">Player's current Determination stat.</param>
    /// <param name="weaponDamage">Player's weapon magic damage.</param>
    /// <param name="level">Player's current level (synced if applicable).</param>
    /// <returns>The estimated heal amount (average, no crit/variance).</returns>
    public static int CalculateHeal(int potency, int mind, int determination, int weaponDamage, int level)
    {
        if (potency <= 0)
            return 0;

        var (levelMain, _, levelDiv) = GetLevelMod(level);

        // f(HMP) - Healing Magic Potency (Mind)
        // Formula: floor(100 * (MND - LevelMod[MAIN]) / 304) + 100
        var fHmp = Math.Floor(100.0 * (mind - levelMain) / 304.0) + 100;

        // f(DET) - Determination
        // Formula: floor(140 * (DET - LevelMod[MAIN]) / LevelMod[DIV]) + 1000
        // Note: Coefficient changed from 130 to 140 in patch 6.0 (Endwalker)
        var fDet = Math.Floor(140.0 * (determination - levelMain) / levelDiv) + 1000;

        // f(WD) - Weapon Damage
        // Formula: floor(LevelMod[MAIN] * JobMod[MND] / 1000) + WeaponDamage
        var fWd = Math.Floor((double)levelMain * HealerMindJobMod / 1000.0) + weaponDamage;

        // Trait - Maim and Mend II (30% bonus for level 40+ healers)
        var trait = level >= 40 ? 130.0 : (level >= 20 ? 110.0 : 100.0);

        // Base healing formula:
        // H1 = floor(Potency * f(HMP) * f(DET) / 100 / 1000)
        // H2 = floor(H1 * f(TNC) / 1000 * f(WD) / 100 * Trait / 100)
        // Note: f(TNC) = 1000 for non-tanks (no effect)
        var h1 = Math.Floor(potency * fHmp * fDet / 100.0 / 1000.0);
        var h2 = Math.Floor(h1 * 1000.0 / 1000.0 * fWd / 100.0 * trait / 100.0);

        // Apply calibrated correction factor (auto-learned from observed heals)
        var correctionFactor = GetCorrectionFactor();
        var corrected = (int)Math.Floor(h2 * correctionFactor);

        return corrected;
    }

    /// <summary>
    /// Calculates heal without correction factor - used for calibration.
    /// </summary>
    public static int CalculateHealRaw(int potency, int mind, int determination, int weaponDamage, int level)
    {
        if (potency <= 0)
            return 0;

        var (levelMain, _, levelDiv) = GetLevelMod(level);

        var fHmp = Math.Floor(100.0 * (mind - levelMain) / 304.0) + 100;
        var fDet = Math.Floor(140.0 * (determination - levelMain) / levelDiv) + 1000;
        var fWd = Math.Floor((double)levelMain * HealerMindJobMod / 1000.0) + weaponDamage;
        var trait = level >= 40 ? 130.0 : (level >= 20 ? 110.0 : 100.0);

        var h1 = Math.Floor(potency * fHmp * fDet / 100.0 / 1000.0);
        var h2 = Math.Floor(h1 * 1000.0 / 1000.0 * fWd / 100.0 * trait / 100.0);

        return (int)h2;
    }

    /// <summary>
    /// Gets the level modifier for a given level.
    /// Interpolates for levels not in the table.
    /// </summary>
    private static (int Main, int Sub, int Div) GetLevelMod(int level)
    {
        // Find the highest level bracket at or below the given level
        var closestLevel = 1;
        foreach (var kvp in LevelMods)
        {
            if (kvp.Key <= level && kvp.Key > closestLevel)
                closestLevel = kvp.Key;
        }

        return LevelMods[closestLevel];
    }
}
