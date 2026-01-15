using System.Collections.Generic;

namespace Olympus.Config;

/// <summary>
/// Validates configuration settings and relationships.
/// Ensures thresholds are logically consistent and within valid bounds.
/// </summary>
public static class ConfigValidator
{
    /// <summary>
    /// Represents a validation issue.
    /// </summary>
    public sealed class ValidationIssue
    {
        public ValidationSeverity Severity { get; init; }
        public string Category { get; init; } = string.Empty;
        public string Message { get; init; } = string.Empty;
        public string? SuggestedFix { get; init; }
    }

    public enum ValidationSeverity
    {
        Info,
        Warning,
        Error
    }

    /// <summary>
    /// Validates the entire configuration and returns any issues found.
    /// </summary>
    /// <param name="config">The configuration to validate.</param>
    /// <returns>List of validation issues.</returns>
    public static List<ValidationIssue> Validate(Configuration config)
    {
        var issues = new List<ValidationIssue>();

        ValidateHealingThresholds(config.Healing, issues);
        ValidateTriageWeights(config.Healing, issues);
        ValidateRoleActionSettings(config, issues);
        ValidateDefensiveSettings(config.Defensive, issues);

        return issues;
    }

    /// <summary>
    /// Validates healing threshold relationships.
    /// </summary>
    private static void ValidateHealingThresholds(HealingConfig healing, List<ValidationIssue> issues)
    {
        // Benediction (emergency) should trigger at lower HP than Tetra (oGCD emergency)
        if (healing.BenedictionEmergencyThreshold >= healing.OgcdEmergencyThreshold)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Healing",
                Message = $"Benediction threshold ({healing.BenedictionEmergencyThreshold:P0}) should be lower than oGCD emergency threshold ({healing.OgcdEmergencyThreshold:P0})",
                SuggestedFix = "Set Benediction threshold to 0.30 or lower, oGCD threshold to 0.50+"
            });
        }

        // oGCD emergency should be at or below GCD emergency
        if (healing.OgcdEmergencyThreshold > healing.GcdEmergencyThreshold)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Healing",
                Message = $"oGCD emergency threshold ({healing.OgcdEmergencyThreshold:P0}) should not exceed GCD emergency threshold ({healing.GcdEmergencyThreshold:P0})",
                SuggestedFix = "Set oGCD threshold at or below GCD threshold"
            });
        }

        // Proactive Benediction threshold should be higher than emergency
        if (healing.EnableProactiveBenediction &&
            healing.ProactiveBenedictionHpThreshold <= healing.BenedictionEmergencyThreshold)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Healing",
                Message = $"Proactive Benediction HP threshold ({healing.ProactiveBenedictionHpThreshold:P0}) should be higher than emergency threshold ({healing.BenedictionEmergencyThreshold:P0})",
                SuggestedFix = "Set proactive threshold above emergency threshold"
            });
        }

        // Preemptive healing threshold should be between GCD emergency and oGCD emergency
        if (healing.EnablePreemptiveHealing)
        {
            if (healing.PreemptiveHealingThreshold < healing.BenedictionEmergencyThreshold)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Info,
                    Category = "Healing",
                    Message = $"Preemptive healing threshold ({healing.PreemptiveHealingThreshold:P0}) is very low. May not trigger preemptive healing often.",
                    SuggestedFix = "Consider raising to 0.35-0.50 for more proactive healing"
                });
            }
        }

        // Regen high damage threshold should be above normal threshold (90%)
        if (healing.EnableDynamicRegenThreshold && healing.RegenHighDamageThreshold <= 0.90f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Healing",
                Message = $"Regen high damage threshold ({healing.RegenHighDamageThreshold:P0}) is at or below default. Dynamic threshold has no effect.",
                SuggestedFix = "Set to 0.92-0.98 for proactive Regen during damage"
            });
        }

        // Damage rate thresholds - moderate should be less than aggressive
        if (healing.EnableDamageAwareLilySelection &&
            healing.ModerateLilyDamageRate >= healing.AggressiveLilyDamageRate)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Healing",
                Message = $"Moderate lily damage rate ({healing.ModerateLilyDamageRate}) should be less than aggressive rate ({healing.AggressiveLilyDamageRate})",
                SuggestedFix = "Set moderate to ~200 and aggressive to ~400"
            });
        }

        // AoE heal targets should be at least 2
        if (healing.AoEHealMinTargets < 2)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Healing",
                Message = "AoE heal minimum targets is 1. Consider single-target heals for single targets.",
                SuggestedFix = "Set to 2-3 for efficient AoE healing"
            });
        }

        // Assize healing targets should be reasonable
        if (healing.EnableAssizeHealing && healing.AssizeHealingMinTargets < 2)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Healing",
                Message = "Assize healing minimum targets is 1. May use Assize inefficiently for single target healing.",
                SuggestedFix = "Set to 2-3 for better Assize efficiency"
            });
        }
    }

    /// <summary>
    /// Validates triage weight relationships.
    /// </summary>
    private static void ValidateTriageWeights(HealingConfig healing, List<ValidationIssue> issues)
    {
        if (healing.TriagePreset != TriagePreset.Custom)
            return;

        var weights = healing.CustomTriageWeights;
        var totalCore = weights.DamageRate + weights.TankBonus + weights.MissingHp + weights.DamageAcceleration;

        // Core weights should sum to approximately 1.0
        if (totalCore < 0.8f || totalCore > 1.2f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Triage",
                Message = $"Core triage weights sum to {totalCore:F2}. Should be close to 1.0 for balanced triage.",
                SuggestedFix = "Adjust DamageRate, TankBonus, MissingHp, and DamageAcceleration to sum to ~1.0"
            });
        }

        // Enhanced weights are optional but should not exceed reasonable limits
        var totalEnhanced = weights.ShieldPenalty + weights.MitigationPenalty + weights.HealerBonus + weights.TtdUrgency;
        if (totalEnhanced > 0.5f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Triage",
                Message = $"Enhanced triage modifiers sum to {totalEnhanced:F2}. High values may cause erratic priority.",
                SuggestedFix = "Keep enhanced weights below 0.50 total"
            });
        }
    }

    /// <summary>
    /// Validates role action settings.
    /// </summary>
    private static void ValidateRoleActionSettings(Configuration config, List<ValidationIssue> issues)
    {
        // Lucid Dreaming threshold should be reasonable
        if (config.EnableLucidDreaming && config.LucidDreamingThreshold < 0.50f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Role Actions",
                Message = $"Lucid Dreaming threshold ({config.LucidDreamingThreshold:P0}) is very low. May run out of MP before triggering.",
                SuggestedFix = "Set to 0.60-0.80 for better MP management"
            });
        }

        if (config.EnableLucidDreaming && config.LucidDreamingThreshold > 0.90f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Role Actions",
                Message = $"Lucid Dreaming threshold ({config.LucidDreamingThreshold:P0}) is very high. May waste cooldown when not needed.",
                SuggestedFix = "Set to 0.60-0.80 for efficient MP management"
            });
        }
    }

    /// <summary>
    /// Validates defensive configuration settings.
    /// </summary>
    private static void ValidateDefensiveSettings(DefensiveConfig defensive, List<ValidationIssue> issues)
    {
        // Proactive Aquaveil damage rate should be lower than Benison (Aquaveil is more valuable)
        if (defensive.EnableProactiveCooldowns && defensive.EnableAquaveil && defensive.EnableDivineBenison)
        {
            if (defensive.ProactiveAquaveilDamageRate >= defensive.ProactiveBenisonDamageRate)
            {
                issues.Add(new ValidationIssue
                {
                    Severity = ValidationSeverity.Info,
                    Category = "Defensive",
                    Message = $"Proactive Aquaveil damage rate ({defensive.ProactiveAquaveilDamageRate}) is at or above Benison rate ({defensive.ProactiveBenisonDamageRate}). Aquaveil provides more mitigation.",
                    SuggestedFix = "Set Aquaveil rate lower (~300) to apply it more readily"
                });
            }
        }

        // Defensive cooldown threshold should be reasonable
        if (defensive.DefensiveCooldownThreshold < 0.50f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Defensive",
                Message = $"Defensive cooldown threshold ({defensive.DefensiveCooldownThreshold:P0}) is very low. Defensives may not trigger until emergency.",
                SuggestedFix = "Set to 0.70-0.85 for proactive mitigation"
            });
        }
    }

    /// <summary>
    /// Auto-fixes critical configuration issues.
    /// </summary>
    /// <param name="config">The configuration to fix.</param>
    /// <returns>Number of issues fixed.</returns>
    public static int AutoFix(Configuration config)
    {
        int fixes = 0;

        // Fix inverted thresholds
        if (config.Healing.BenedictionEmergencyThreshold >= config.Healing.OgcdEmergencyThreshold)
        {
            config.Healing.BenedictionEmergencyThreshold = 0.30f;
            config.Healing.OgcdEmergencyThreshold = 0.50f;
            fixes++;
        }

        if (config.Healing.OgcdEmergencyThreshold > config.Healing.GcdEmergencyThreshold)
        {
            config.Healing.GcdEmergencyThreshold = config.Healing.OgcdEmergencyThreshold + 0.10f;
            fixes++;
        }

        // Fix damage rate thresholds
        if (config.Healing.ModerateLilyDamageRate >= config.Healing.AggressiveLilyDamageRate)
        {
            config.Healing.ModerateLilyDamageRate = 200f;
            config.Healing.AggressiveLilyDamageRate = 400f;
            fixes++;
        }

        return fixes;
    }
}
