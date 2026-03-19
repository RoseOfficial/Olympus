using Olympus.Config;

namespace Olympus.Tests.Config;

public class ConfigValidatorTests
{
    #region Healing Threshold Tests

    [Fact]
    public void Validate_ValidThresholds_ReturnsNoErrors()
    {
        var config = CreateValidConfig();

        var issues = ConfigValidator.Validate(config);

        var errors = issues.Where(i => i.Severity == ConfigValidator.ValidationSeverity.Error).ToList();
        Assert.Empty(errors);
    }

    [Fact]
    public void Validate_BenedictionThresholdAboveOgcd_ReturnsWarning()
    {
        var config = CreateValidConfig();
        config.Healing.BenedictionEmergencyThreshold = 0.60f;
        config.Healing.OgcdEmergencyThreshold = 0.50f;

        var issues = ConfigValidator.Validate(config);

        Assert.Contains(issues, i =>
            i.Severity == ConfigValidator.ValidationSeverity.Warning &&
            i.Category == "Healing" &&
            i.Message.Contains("Benediction"));
    }

    [Fact]
    public void Validate_GcdThresholdAboveOgcd_ReturnsWarning()
    {
        // GCD threshold must be strictly lower than oGCD — oGCD heals fire first (they're instant),
        // GCD heals interrupt damage and should only fire at lower HP.
        // Having GCD >= oGCD (inverted) is the bug we detect.
        var config = CreateValidConfig();
        config.Healing.OgcdEmergencyThreshold = 0.50f;
        config.Healing.GcdEmergencyThreshold = 0.70f; // GCD > oGCD = invalid

        var issues = ConfigValidator.Validate(config);

        Assert.Contains(issues, i =>
            i.Severity == ConfigValidator.ValidationSeverity.Warning &&
            i.Category == "Healing" &&
            i.Message.Contains("GCD emergency threshold"));
    }

    [Fact]
    public void Validate_ProactiveBenedictionBelowEmergency_ReturnsWarning()
    {
        var config = CreateValidConfig();
        config.Healing.EnableProactiveBenediction = true;
        config.Healing.ProactiveBenedictionHpThreshold = 0.25f;
        config.Healing.BenedictionEmergencyThreshold = 0.30f;

        var issues = ConfigValidator.Validate(config);

        Assert.Contains(issues, i =>
            i.Severity == ConfigValidator.ValidationSeverity.Warning &&
            i.Category == "Healing" &&
            i.Message.Contains("Proactive Benediction"));
    }

    [Fact]
    public void Validate_ModerateDamageRateAboveAggressive_ReturnsWarning()
    {
        var config = CreateValidConfig();
        config.Healing.EnableDamageAwareLilySelection = true;
        config.Healing.ModerateLilyDamageRate = 500f;
        config.Healing.AggressiveLilyDamageRate = 400f;

        var issues = ConfigValidator.Validate(config);

        Assert.Contains(issues, i =>
            i.Severity == ConfigValidator.ValidationSeverity.Warning &&
            i.Category == "Healing" &&
            i.Message.Contains("Moderate lily damage rate"));
    }

    [Fact]
    public void Validate_AoEMinTargetsLessThanTwo_ReturnsInfo()
    {
        var config = CreateValidConfig();
        config.Healing.AoEHealMinTargets = 1;

        var issues = ConfigValidator.Validate(config);

        Assert.Contains(issues, i =>
            i.Severity == ConfigValidator.ValidationSeverity.Info &&
            i.Category == "Healing" &&
            i.Message.Contains("AoE heal minimum"));
    }

    #endregion

    #region Triage Weight Tests

    [Fact]
    public void Validate_CustomTriageWeightsBalanced_NoWarnings()
    {
        var config = CreateValidConfig();
        config.Healing.TriagePreset = TriagePreset.Custom;
        config.Healing.CustomTriageWeights.DamageRate = 0.35f;
        config.Healing.CustomTriageWeights.TankBonus = 0.25f;
        config.Healing.CustomTriageWeights.MissingHp = 0.30f;
        config.Healing.CustomTriageWeights.DamageAcceleration = 0.10f;

        var issues = ConfigValidator.Validate(config);

        var triageWarnings = issues.Where(i => i.Category == "Triage" && i.Severity == ConfigValidator.ValidationSeverity.Warning);
        Assert.Empty(triageWarnings);
    }

    [Fact]
    public void Validate_CustomTriageWeightsTooLow_ReturnsWarning()
    {
        var config = CreateValidConfig();
        config.Healing.TriagePreset = TriagePreset.Custom;
        config.Healing.CustomTriageWeights.DamageRate = 0.10f;
        config.Healing.CustomTriageWeights.TankBonus = 0.10f;
        config.Healing.CustomTriageWeights.MissingHp = 0.10f;
        config.Healing.CustomTriageWeights.DamageAcceleration = 0.10f;

        var issues = ConfigValidator.Validate(config);

        Assert.Contains(issues, i =>
            i.Severity == ConfigValidator.ValidationSeverity.Warning &&
            i.Category == "Triage" &&
            i.Message.Contains("sum to"));
    }

    [Fact]
    public void Validate_CustomTriageWeightsTooHigh_ReturnsWarning()
    {
        var config = CreateValidConfig();
        config.Healing.TriagePreset = TriagePreset.Custom;
        config.Healing.CustomTriageWeights.DamageRate = 0.50f;
        config.Healing.CustomTriageWeights.TankBonus = 0.50f;
        config.Healing.CustomTriageWeights.MissingHp = 0.50f;
        config.Healing.CustomTriageWeights.DamageAcceleration = 0.20f;

        var issues = ConfigValidator.Validate(config);

        Assert.Contains(issues, i =>
            i.Severity == ConfigValidator.ValidationSeverity.Warning &&
            i.Category == "Triage");
    }

    [Fact]
    public void Validate_EnhancedTriageWeightsTooHigh_ReturnsInfo()
    {
        var config = CreateValidConfig();
        config.Healing.TriagePreset = TriagePreset.Custom;
        config.Healing.CustomTriageWeights.ShieldPenalty = 0.20f;
        config.Healing.CustomTriageWeights.MitigationPenalty = 0.20f;
        config.Healing.CustomTriageWeights.HealerBonus = 0.20f;
        config.Healing.CustomTriageWeights.TtdUrgency = 0.10f;

        var issues = ConfigValidator.Validate(config);

        Assert.Contains(issues, i =>
            i.Severity == ConfigValidator.ValidationSeverity.Info &&
            i.Category == "Triage" &&
            i.Message.Contains("Enhanced triage"));
    }

    [Fact]
    public void Validate_NonCustomPreset_SkipsTriageValidation()
    {
        var config = CreateValidConfig();
        config.Healing.TriagePreset = TriagePreset.Balanced;
        // Even with unbalanced weights, no warning since preset is not Custom
        config.Healing.CustomTriageWeights.DamageRate = 0.10f;
        config.Healing.CustomTriageWeights.TankBonus = 0.10f;
        config.Healing.CustomTriageWeights.MissingHp = 0.10f;
        config.Healing.CustomTriageWeights.DamageAcceleration = 0.10f;

        var issues = ConfigValidator.Validate(config);

        var triageIssues = issues.Where(i => i.Category == "Triage");
        Assert.Empty(triageIssues);
    }

    #endregion

    #region Defensive Settings Tests

    [Fact]
    public void Validate_DefensiveThresholdTooLow_ReturnsInfo()
    {
        var config = CreateValidConfig();
        config.Defensive.DefensiveCooldownThreshold = 0.40f;

        var issues = ConfigValidator.Validate(config);

        Assert.Contains(issues, i =>
            i.Severity == ConfigValidator.ValidationSeverity.Info &&
            i.Category == "Defensive" &&
            i.Message.Contains("Defensive cooldown threshold"));
    }

    [Fact]
    public void Validate_AquaveilRateAboveBenison_ReturnsInfo()
    {
        var config = CreateValidConfig();
        config.Defensive.EnableProactiveCooldowns = true;
        config.Defensive.EnableAquaveil = true;
        config.Defensive.EnableDivineBenison = true;
        config.Defensive.ProactiveAquaveilDamageRate = 500f;
        config.Defensive.ProactiveBenisonDamageRate = 400f;

        var issues = ConfigValidator.Validate(config);

        Assert.Contains(issues, i =>
            i.Severity == ConfigValidator.ValidationSeverity.Info &&
            i.Category == "Defensive" &&
            i.Message.Contains("Aquaveil"));
    }

    #endregion

    #region AutoFix Tests

    [Fact]
    public void AutoFix_InvertedBenedictionThreshold_FixesValues()
    {
        var config = CreateValidConfig();
        config.Healing.BenedictionEmergencyThreshold = 0.60f;
        config.Healing.OgcdEmergencyThreshold = 0.50f;

        var fixCount = ConfigValidator.AutoFix(config);

        Assert.Equal(1, fixCount);
        Assert.True(config.Healing.BenedictionEmergencyThreshold < config.Healing.OgcdEmergencyThreshold);
    }

    [Fact]
    public void AutoFix_InvertedGcdOgcdThreshold_FixesValues()
    {
        // When GCD >= oGCD (inverted — GCD heals firing before oGCDs, wrong), AutoFix lowers GCD.
        var config = CreateValidConfig();
        config.Healing.OgcdEmergencyThreshold = 0.50f;
        config.Healing.GcdEmergencyThreshold = 0.60f; // GCD > oGCD = invalid

        var fixCount = ConfigValidator.AutoFix(config);

        // After fix, GCD must be strictly lower than oGCD
        Assert.True(config.Healing.GcdEmergencyThreshold < config.Healing.OgcdEmergencyThreshold);
    }

    [Fact]
    public void AutoFix_InvertedDamageRates_FixesValues()
    {
        var config = CreateValidConfig();
        config.Healing.ModerateLilyDamageRate = 500f;
        config.Healing.AggressiveLilyDamageRate = 400f;

        var fixCount = ConfigValidator.AutoFix(config);

        Assert.True(config.Healing.ModerateLilyDamageRate < config.Healing.AggressiveLilyDamageRate);
    }

    [Fact]
    public void AutoFix_ValidConfig_ReturnsZero()
    {
        var config = CreateValidConfig();

        var fixCount = ConfigValidator.AutoFix(config);

        Assert.Equal(0, fixCount);
    }

    [Fact]
    public void AutoFix_MultipleIssues_FixesAll()
    {
        var config = CreateValidConfig();
        config.Healing.BenedictionEmergencyThreshold = 0.60f;
        config.Healing.OgcdEmergencyThreshold = 0.50f;
        config.Healing.ModerateLilyDamageRate = 500f;
        config.Healing.AggressiveLilyDamageRate = 400f;

        var fixCount = ConfigValidator.AutoFix(config);

        Assert.True(fixCount >= 2);
    }

    #endregion

    #region Test Helpers

    private static Configuration CreateValidConfig()
    {
        return new Configuration
        {
            Healing = new HealingConfig
            {
                BenedictionEmergencyThreshold = 0.30f,
                OgcdEmergencyThreshold = 0.50f,
                GcdEmergencyThreshold = 0.40f, // GCD must be strictly lower than oGCD
                ProactiveBenedictionHpThreshold = 0.50f,
                EnableProactiveBenediction = false,
                ModerateLilyDamageRate = 200f,
                AggressiveLilyDamageRate = 400f,
                EnableDamageAwareLilySelection = true,
                AoEHealMinTargets = 3,
                EnableAssizeHealing = true,
                AssizeHealingMinTargets = 3,
                EnablePreemptiveHealing = false,
                EnableDynamicRegenThreshold = false,
                TriagePreset = TriagePreset.Balanced
            },
            Defensive = new DefensiveConfig
            {
                EnableProactiveCooldowns = true,
                EnableAquaveil = true,
                EnableDivineBenison = true,
                ProactiveAquaveilDamageRate = 300f,
                ProactiveBenisonDamageRate = 400f,
                DefensiveCooldownThreshold = 0.75f
            }
        };
    }

    #endregion
}
