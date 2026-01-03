using Olympus.Services.Calculation;

namespace Olympus.Tests.Services.Calculation;

/// <summary>
/// Tests for the HealingCalculator which implements the FFXIV healing formula.
/// </summary>
public class HealingCalculatorTests
{
    public HealingCalculatorTests()
    {
        // Reset calibration before each test to ensure consistent results
        HealingCalculator.ResetCalibration();
    }

    #region CalculateHeal Tests

    [Fact]
    public void CalculateHeal_ZeroPotency_ReturnsZero()
    {
        var result = HealingCalculator.CalculateHeal(
            potency: 0,
            mind: 3000,
            determination: 2000,
            weaponDamage: 132,
            level: 90);

        Assert.Equal(0, result);
    }

    [Fact]
    public void CalculateHeal_NegativePotency_ReturnsZero()
    {
        var result = HealingCalculator.CalculateHeal(
            potency: -100,
            mind: 3000,
            determination: 2000,
            weaponDamage: 132,
            level: 90);

        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(90)] // Level 90 = 130% (Maim and Mend II)
    [InlineData(80)] // Level 80 = 130%
    [InlineData(70)] // Level 70 = 130%
    [InlineData(50)] // Level 50 = 130%
    [InlineData(40)] // Level 40 = 130%
    [InlineData(39)] // Level 39 = 110% (Maim and Mend I)
    [InlineData(20)] // Level 20 = 110%
    [InlineData(19)] // Level 19 = 100% (no trait)
    [InlineData(1)]  // Level 1 = 100%
    public void CalculateHeal_TraitApplicationByLevel(int level)
    {
        // Same inputs except level - the heal amount should scale with trait
        var heal = HealingCalculator.CalculateHealRaw(
            potency: 400,
            mind: 300,
            determination: 200,
            weaponDamage: 50,
            level: level);

        // Verify heal is positive (formula works)
        Assert.True(heal > 0, $"Heal at level {level} should be positive");
    }

    [Theory]
    [InlineData(400)]   // Cure II potency
    [InlineData(700)]   // Cure III potency
    [InlineData(500)]   // Medica II potency
    [InlineData(800)]   // Afflatus Solace potency
    public void CalculateHeal_PositivePotency_ReturnsPositiveHeal(int potency)
    {
        var result = HealingCalculator.CalculateHeal(
            potency: potency,
            mind: 3000,
            determination: 2000,
            weaponDamage: 132,
            level: 90);

        Assert.True(result > 0, $"Heal with potency {potency} should be positive");
    }

    [Fact]
    public void CalculateHeal_HigherPotency_ReturnsHigherHeal()
    {
        var lowPotencyHeal = HealingCalculator.CalculateHeal(
            potency: 400, mind: 3000, determination: 2000, weaponDamage: 132, level: 90);

        var highPotencyHeal = HealingCalculator.CalculateHeal(
            potency: 800, mind: 3000, determination: 2000, weaponDamage: 132, level: 90);

        Assert.True(highPotencyHeal > lowPotencyHeal,
            "Higher potency should result in higher heal");

        // The heal should roughly scale with potency (800/400 = 2x)
        var ratio = (double)highPotencyHeal / lowPotencyHeal;
        Assert.True(ratio > 1.8 && ratio < 2.2,
            $"800 potency heal should be roughly 2x 400 potency heal, but ratio was {ratio:F2}");
    }

    [Fact]
    public void CalculateHeal_HigherMind_ReturnsHigherHeal()
    {
        var lowMindHeal = HealingCalculator.CalculateHeal(
            potency: 400, mind: 2000, determination: 2000, weaponDamage: 132, level: 90);

        var highMindHeal = HealingCalculator.CalculateHeal(
            potency: 400, mind: 4000, determination: 2000, weaponDamage: 132, level: 90);

        Assert.True(highMindHeal > lowMindHeal,
            "Higher Mind stat should result in higher heal");
    }

    [Fact]
    public void CalculateHeal_HigherDetermination_ReturnsHigherHeal()
    {
        var lowDetHeal = HealingCalculator.CalculateHeal(
            potency: 400, mind: 3000, determination: 1500, weaponDamage: 132, level: 90);

        var highDetHeal = HealingCalculator.CalculateHeal(
            potency: 400, mind: 3000, determination: 2500, weaponDamage: 132, level: 90);

        Assert.True(highDetHeal > lowDetHeal,
            "Higher Determination stat should result in higher heal");
    }

    [Fact]
    public void CalculateHeal_HigherWeaponDamage_ReturnsHigherHeal()
    {
        var lowWdHeal = HealingCalculator.CalculateHeal(
            potency: 400, mind: 3000, determination: 2000, weaponDamage: 100, level: 90);

        var highWdHeal = HealingCalculator.CalculateHeal(
            potency: 400, mind: 3000, determination: 2000, weaponDamage: 150, level: 90);

        Assert.True(highWdHeal > lowWdHeal,
            "Higher weapon damage should result in higher heal");
    }

    #endregion

    #region Calibration Tests

    [Fact]
    public void GetCorrectionFactor_NoCalibration_ReturnsDefaultFactor()
    {
        HealingCalculator.ResetCalibration();

        var factor = HealingCalculator.GetCorrectionFactor();

        // Default factor is 1.10 per the source code
        Assert.Equal(1.10, factor);
    }

    [Fact]
    public void CalibrateFromActual_ValidSample_UpdatesFactor()
    {
        HealingCalculator.ResetCalibration();

        // Add 3 calibration samples (minimum needed to use calibrated value)
        HealingCalculator.CalibrateFromActual(1000, 1100);
        HealingCalculator.CalibrateFromActual(1000, 1100);
        HealingCalculator.CalibrateFromActual(1000, 1100);

        var factor = HealingCalculator.GetCorrectionFactor();

        // Should be close to 1.1 (actual/predicted = 1100/1000)
        Assert.True(factor > 1.05 && factor < 1.15,
            $"Calibrated factor should be around 1.1, but was {factor}");
    }

    [Fact]
    public void CalibrateFromActual_ZeroPredicted_DoesNotUpdate()
    {
        HealingCalculator.ResetCalibration();

        HealingCalculator.CalibrateFromActual(0, 1000);

        var factor = HealingCalculator.GetCorrectionFactor();

        // Should still be default since invalid input
        Assert.Equal(1.10, factor);
    }

    [Fact]
    public void CalibrateFromActual_ZeroActual_DoesNotUpdate()
    {
        HealingCalculator.ResetCalibration();

        HealingCalculator.CalibrateFromActual(1000, 0);

        var factor = HealingCalculator.GetCorrectionFactor();

        Assert.Equal(1.10, factor);
    }

    [Fact]
    public void CalibrateFromActual_ExtremeFactor_Rejected()
    {
        HealingCalculator.ResetCalibration();

        // Factor of 2.0 (actual/predicted = 2000/1000) should be rejected
        HealingCalculator.CalibrateFromActual(1000, 2000);
        HealingCalculator.CalibrateFromActual(1000, 2000);
        HealingCalculator.CalibrateFromActual(1000, 2000);

        var factor = HealingCalculator.GetCorrectionFactor();

        // Should still be default since extreme values rejected
        Assert.Equal(1.10, factor);
    }

    [Fact]
    public void CalibrateFromActual_LessThanThreeSamples_UsesDefaultFactor()
    {
        HealingCalculator.ResetCalibration();

        // Only 2 samples
        HealingCalculator.CalibrateFromActual(1000, 1200);
        HealingCalculator.CalibrateFromActual(1000, 1200);

        var factor = HealingCalculator.GetCorrectionFactor();

        // Should use default until we have 3+ samples
        Assert.Equal(1.10, factor);
    }

    [Fact]
    public void ResetCalibration_ClearsAllData()
    {
        // Add some calibration data
        HealingCalculator.CalibrateFromActual(1000, 1200);
        HealingCalculator.CalibrateFromActual(1000, 1200);
        HealingCalculator.CalibrateFromActual(1000, 1200);

        // Reset
        HealingCalculator.ResetCalibration();

        var factor = HealingCalculator.GetCorrectionFactor();

        // Should be back to default
        Assert.Equal(1.10, factor);
    }

    #endregion

    #region Level Modifier Tests

    [Theory]
    [InlineData(1)]
    [InlineData(10)]
    [InlineData(20)]
    [InlineData(30)]
    [InlineData(40)]
    [InlineData(50)]
    [InlineData(60)]
    [InlineData(70)]
    [InlineData(80)]
    [InlineData(90)]
    [InlineData(100)]
    public void CalculateHeal_AllLevelBrackets_ReturnsPositiveHeal(int level)
    {
        // Use stats appropriate for the level
        var mind = level * 5 + 100;
        var determination = level * 3 + 100;
        var weaponDamage = level + 10;

        var result = HealingCalculator.CalculateHeal(
            potency: 400,
            mind: mind,
            determination: determination,
            weaponDamage: weaponDamage,
            level: level);

        Assert.True(result > 0, $"Heal at level {level} should be positive");
    }

    [Fact]
    public void CalculateHeal_IntermediateLevel_UsesLowerBracket()
    {
        // Level 55 should use level 50 modifiers
        // Level 50 and 55 with same stats should give same result
        var heal50 = HealingCalculator.CalculateHeal(
            potency: 400, mind: 500, determination: 400, weaponDamage: 60, level: 50);

        var heal55 = HealingCalculator.CalculateHeal(
            potency: 400, mind: 500, determination: 400, weaponDamage: 60, level: 55);

        // Both should use level 50 modifiers, so results should be identical
        Assert.Equal(heal50, heal55);
    }

    #endregion
}
