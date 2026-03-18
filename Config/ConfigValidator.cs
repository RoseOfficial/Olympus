using System.Collections.Generic;
using Olympus.Config.DPS;

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

        // Existing healer validation
        ValidateHealingThresholds(config.Healing, issues);
        ValidateTriageWeights(config.Healing, issues);
        ValidateDefensiveSettings(config.Defensive, issues);

        // New validations
        ValidateTankSettings(config.Tank, issues);
        ValidatePartyCoordinationSettings(config.PartyCoordination, issues);
        ValidateDpsSettings(config, issues);

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
    /// Validates tank configuration settings.
    /// </summary>
    private static void ValidateTankSettings(TankConfig tank, List<ValidationIssue> issues)
    {
        // Negative value guards for float percentage fields
        if (tank.MitigationThreshold < 0f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Tank",
                Message = "TankConfig.MitigationThreshold is negative — resetting to default",
                SuggestedFix = "Set to 0.70 (default)"
            });
        }

        if (tank.ClemencyThreshold < 0f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Tank",
                Message = "TankConfig.ClemencyThreshold is negative — resetting to default",
                SuggestedFix = "Set to 0.30 (default)"
            });
        }

        if (tank.TBNThreshold < 0f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Tank",
                Message = "TankConfig.TBNThreshold is negative — resetting to default",
                SuggestedFix = "Set to 0.80 (default)"
            });
        }

        if (tank.HeartOfCorundumThreshold < 0f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Tank",
                Message = "TankConfig.HeartOfCorundumThreshold is negative — resetting to default",
                SuggestedFix = "Set to 0.80 (default)"
            });
        }

        // Mitigation threshold sanity - very low values may leave tank vulnerable
        if (tank.MitigationThreshold < 0.50f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Tank",
                Message = $"Mitigation threshold ({tank.MitigationThreshold:P0}) is very low. Mitigations may not trigger until emergency.",
                SuggestedFix = "Set to 0.65-0.80 for proactive mitigation"
            });
        }

        // Invuln stagger should be >= defensive stagger (invulns are longer cooldowns)
        if (tank.InvulnerabilityStaggerWindowSeconds < tank.DefensiveStaggerWindowSeconds)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Tank",
                Message = $"Invuln stagger window ({tank.InvulnerabilityStaggerWindowSeconds}s) is shorter than defensive stagger ({tank.DefensiveStaggerWindowSeconds}s). Consider longer invuln stagger.",
                SuggestedFix = "Set invuln window to at least 5 seconds"
            });
        }

        // Provoke delay sanity - very long delays may cause aggro loss
        if (tank.ProvokeDelay > 3.0f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Tank",
                Message = $"Provoke delay ({tank.ProvokeDelay}s) is very long. May lose aggro before recovery.",
                SuggestedFix = "Set to 1-2 seconds for responsive aggro recovery"
            });
        }

        // Sheltron gauge threshold sanity
        if (tank.SheltronMinGauge > 75)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Tank",
                Message = $"Sheltron minimum gauge ({tank.SheltronMinGauge}) is very high. May waste gauge if capping.",
                SuggestedFix = "Set to 50 for balanced gauge usage"
            });
        }
    }

    /// <summary>
    /// Validates party coordination configuration settings.
    /// </summary>
    private static void ValidatePartyCoordinationSettings(PartyCoordinationConfig coord, List<ValidationIssue> issues)
    {
        // Instance timeout should be at least 2x heartbeat for reliability
        if (coord.InstanceTimeoutMs < coord.HeartbeatIntervalMs * 2)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Party Coordination",
                Message = $"Instance timeout ({coord.InstanceTimeoutMs}ms) should be at least 2x heartbeat interval ({coord.HeartbeatIntervalMs}ms) for reliability.",
                SuggestedFix = "Set timeout to at least 2000ms"
            });
        }

        // Cooldown overlap window sanity - very short may cause stacking
        if (coord.CooldownOverlapWindowSeconds < 1.5f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Party Coordination",
                Message = $"Cooldown overlap window ({coord.CooldownOverlapWindowSeconds}s) is very short. May not prevent cooldown stacking.",
                SuggestedFix = "Set to 2-4 seconds to prevent overlap"
            });
        }

        // Raid buff alignment vs max desync - alignment should be less than max desync
        if (coord.RaidBuffAlignmentWindowSeconds > coord.MaxBuffDesyncSeconds)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Party Coordination",
                Message = $"Raid buff alignment window ({coord.RaidBuffAlignmentWindowSeconds}s) exceeds max desync ({coord.MaxBuffDesyncSeconds}s). Alignment may never trigger.",
                SuggestedFix = "Set alignment window lower than max desync"
            });
        }

        // Secondary heal assist threshold sanity
        if (coord.SecondaryHealAssistThreshold < 0.40f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Party Coordination",
                Message = $"Secondary heal assist threshold ({coord.SecondaryHealAssistThreshold:P0}) is low. Secondary healer may intervene too early.",
                SuggestedFix = "Set to 0.50-0.60 for responsive backup healing"
            });
        }

        // Ground effect overlap threshold sanity
        if (coord.GroundEffectOverlapThreshold < 0.4f)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Info,
                Category = "Party Coordination",
                Message = $"Ground effect overlap threshold ({coord.GroundEffectOverlapThreshold:P1}) is very conservative. May skip useful ground effects.",
                SuggestedFix = "Set to 0.5 for balanced ground effect usage"
            });
        }
    }

    /// <summary>
    /// Validates DPS job-specific configuration settings.
    /// Only checks cross-field relationships that can silently lock out an ability.
    /// </summary>
    private static void ValidateDpsSettings(Configuration config, List<ValidationIssue> issues)
    {
        // Ninja: if minimum Ninki exceeds the overcap threshold, the Ninki spender is
        // never reached — the overcap dump fires (or not) before the minimum check passes.
        if (config.Ninja.NinkiMinGauge > config.Ninja.NinkiOvercapThreshold)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Ninja",
                Message = $"Ninja Ninki minimum gauge ({config.Ninja.NinkiMinGauge}) exceeds overcap threshold ({config.Ninja.NinkiOvercapThreshold}) — Ninki will never be spent.",
                SuggestedFix = "Lower the minimum gauge or raise the overcap threshold so the minimum is at or below the overcap value."
            });
        }

        // Samurai: same min/overcap pattern for Kenki gauge.
        if (config.Samurai.KenkiMinGauge > config.Samurai.KenkiOvercapThreshold)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Samurai",
                Message = $"Samurai Kenki minimum gauge ({config.Samurai.KenkiMinGauge}) exceeds overcap threshold ({config.Samurai.KenkiOvercapThreshold}) — Shinten and Kyuten will never be spent.",
                SuggestedFix = "Lower the minimum gauge or raise the overcap threshold so the minimum is at or below the overcap value."
            });
        }

        // Reaper: same min/overcap pattern for Soul gauge.
        if (config.Reaper.SoulMinGauge > config.Reaper.SoulOvercapThreshold)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Reaper",
                Message = $"Reaper Soul minimum gauge ({config.Reaper.SoulMinGauge}) exceeds overcap threshold ({config.Reaper.SoulOvercapThreshold}) — Blood Stalk and Grim Swathe will never be spent.",
                SuggestedFix = "Lower the minimum gauge or raise the overcap threshold so the minimum is at or below the overcap value."
            });
        }

        // Machinist: same min/overcap pattern for Heat gauge (Hypercharge).
        if (config.Machinist.HeatMinGauge > config.Machinist.HeatOvercapThreshold)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Machinist",
                Message = $"Machinist Heat minimum gauge ({config.Machinist.HeatMinGauge}) exceeds overcap threshold ({config.Machinist.HeatOvercapThreshold}) — Hypercharge will never fire.",
                SuggestedFix = "Lower the minimum gauge or raise the overcap threshold so the minimum is at or below the overcap value."
            });
        }

        // Machinist: same min/overcap pattern for Battery gauge (Automaton Queen).
        if (config.Machinist.BatteryMinGauge > config.Machinist.BatteryOvercapThreshold)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Machinist",
                Message = $"Machinist Battery minimum gauge ({config.Machinist.BatteryMinGauge}) exceeds overcap threshold ({config.Machinist.BatteryOvercapThreshold}) — Automaton Queen will never be summoned.",
                SuggestedFix = "Lower the minimum gauge or raise the overcap threshold so the minimum is at or below the overcap value."
            });
        }

        // Dancer: same min/overcap pattern for Esprit gauge (Saber Dance).
        if (config.Dancer.SaberDanceMinGauge > config.Dancer.EspritOvercapThreshold)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Dancer",
                Message = $"Dancer Saber Dance minimum Esprit ({config.Dancer.SaberDanceMinGauge}) exceeds overcap threshold ({config.Dancer.EspritOvercapThreshold}) — Saber Dance will never fire.",
                SuggestedFix = "Lower the minimum gauge or raise the overcap threshold so the minimum is at or below the overcap value."
            });
        }

        // Dancer: same min/overcap pattern for Feather gauge (Fan Dance).
        if (config.Dancer.FanDanceMinFeathers > config.Dancer.FeatherOvercapThreshold)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Dancer",
                Message = $"Dancer Fan Dance minimum feathers ({config.Dancer.FanDanceMinFeathers}) exceeds overcap threshold ({config.Dancer.FeatherOvercapThreshold}) — Fan Dance will never fire.",
                SuggestedFix = "Lower the minimum feathers or raise the overcap threshold so the minimum is at or below the overcap value."
            });
        }

        // DRG: warn if GeirskogulMinEyes > 0 but EnableGeirskogul is false
        if (!config.Dragoon.EnableGeirskogul && config.Dragoon.GeirskogulMinEyes > 0)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Dragoon",
                Message = "Dragoon: GeirskogulMinEyes is set but EnableGeirskogul is off — Life of the Dragon cannot be entered",
                SuggestedFix = "Enable Geirskogul or set GeirskogulMinEyes to 0."
            });
        }

        // PCT: warn if HolyMinPalette > 0 but EnableHolyInWhite is false
        if (!config.Pictomancer.EnableHolyInWhite && config.Pictomancer.HolyMinPalette > 0)
        {
            issues.Add(new ValidationIssue
            {
                Severity = ValidationSeverity.Warning,
                Category = "Pictomancer",
                Message = "Pictomancer: HolyMinPalette is set but EnableHolyInWhite is off",
                SuggestedFix = "Enable Holy in White or set HolyMinPalette to 0."
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

        // Fix inverted healing thresholds
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

        // Fix tank invuln stagger window
        if (config.Tank.InvulnerabilityStaggerWindowSeconds < config.Tank.DefensiveStaggerWindowSeconds)
        {
            config.Tank.InvulnerabilityStaggerWindowSeconds = config.Tank.DefensiveStaggerWindowSeconds + 2f;
            fixes++;
        }

        // Fix party coordination instance timeout
        if (config.PartyCoordination.InstanceTimeoutMs < config.PartyCoordination.HeartbeatIntervalMs * 2)
        {
            config.PartyCoordination.InstanceTimeoutMs = config.PartyCoordination.HeartbeatIntervalMs * 2;
            fixes++;
        }

        // Fix raid buff alignment exceeding max desync
        if (config.PartyCoordination.RaidBuffAlignmentWindowSeconds > config.PartyCoordination.MaxBuffDesyncSeconds)
        {
            config.PartyCoordination.RaidBuffAlignmentWindowSeconds = config.PartyCoordination.MaxBuffDesyncSeconds * 0.5f;
            fixes++;
        }

        return fixes;
    }
}
