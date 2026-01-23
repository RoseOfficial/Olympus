namespace Olympus.Services.Training;

using System;
using Olympus.Config;

/// <summary>
/// Represents a single action decision with its explanation.
/// </summary>
public sealed class ActionExplanation
{
    /// <summary>
    /// When this action was taken.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// The action ID that was executed.
    /// </summary>
    public uint ActionId { get; init; }

    /// <summary>
    /// Human-readable name of the action.
    /// </summary>
    public string ActionName { get; init; } = string.Empty;

    /// <summary>
    /// Category of action (e.g., "Healing", "Damage", "Defensive", "Utility").
    /// </summary>
    public string Category { get; init; } = string.Empty;

    /// <summary>
    /// Target of the action (if applicable).
    /// </summary>
    public string? TargetName { get; init; }

    /// <summary>
    /// Brief reason for taking this action (shown in history list).
    /// Example: "Emergency heal - tank critical"
    /// </summary>
    public string ShortReason { get; init; } = string.Empty;

    /// <summary>
    /// Full explanation with numbers and context (shown when expanded).
    /// Example: "Tank was at 22% HP with high damage intake. Benediction was used as emergency heal."
    /// </summary>
    public string DetailedReason { get; init; } = string.Empty;

    /// <summary>
    /// Key factors that influenced this decision.
    /// Example: ["Tank HP: 22%", "Damage intake: 1200 DPS", "No other emergency heal available"]
    /// </summary>
    public string[] Factors { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Alternative actions that were considered but not chosen.
    /// Example: ["Tetragrammaton (but slower)", "Cure II (but GCD)"]
    /// </summary>
    public string[] Alternatives { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Learning tip for this type of scenario.
    /// Example: "Benediction is best saved for emergencies, but don't let it sit unused!"
    /// </summary>
    public string? Tip { get; init; }

    /// <summary>
    /// Concept ID for tracking learning progress.
    /// Example: "whm.emergency_healing", "whm.ogcd_weaving"
    /// </summary>
    public string? ConceptId { get; init; }

    /// <summary>
    /// How important this explanation is to show.
    /// </summary>
    public ExplanationPriority Priority { get; init; } = ExplanationPriority.Normal;
}

/// <summary>
/// Tracks learning progress across concepts.
/// </summary>
public sealed class LearningProgress
{
    /// <summary>
    /// Total number of unique concepts available to learn.
    /// </summary>
    public int TotalConcepts { get; init; }

    /// <summary>
    /// Number of concepts marked as learned.
    /// </summary>
    public int LearnedConcepts { get; init; }

    /// <summary>
    /// Concepts that have been seen many times but not marked as learned.
    /// These may need extra attention.
    /// </summary>
    public string[] ConceptsNeedingAttention { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Recent concepts that were demonstrated in gameplay.
    /// </summary>
    public string[] RecentlyDemonstratedConcepts { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Progress percentage (0-100).
    /// </summary>
    public float ProgressPercent => TotalConcepts > 0 ? (float)LearnedConcepts / TotalConcepts * 100f : 0f;
}

/// <summary>
/// Well-known concept IDs for WHM healing.
/// </summary>
public static class WhmConcepts
{
    // Emergency Healing
    public const string EmergencyHealing = "whm.emergency_healing";
    public const string BenedictionUsage = "whm.benediction_usage";
    public const string TetragrammatonUsage = "whm.tetragrammaton_usage";

    // Healing Priority
    public const string HealingPriority = "whm.healing_priority";
    public const string TankPriority = "whm.tank_priority";
    public const string PartyWideDamage = "whm.party_wide_damage";

    // oGCD Weaving
    public const string OgcdWeaving = "whm.ogcd_weaving";
    public const string AssizeUsage = "whm.assize_usage";
    public const string DivineBenisonUsage = "whm.divine_benison_usage";

    // Lily System
    public const string LilyManagement = "whm.lily_management";
    public const string AfflatusRaptureUsage = "whm.afflatus_rapture_usage";
    public const string AfflatusSolaceUsage = "whm.afflatus_solace_usage";
    public const string AfflatusMiseryTiming = "whm.afflatus_misery_timing";
    public const string BloodLilyBuilding = "whm.blood_lily_building";

    // Defensive Cooldowns
    public const string TemperanceUsage = "whm.temperance_usage";
    public const string AquaveilUsage = "whm.aquaveil_usage";
    public const string LiturgyOfTheBellUsage = "whm.liturgy_usage";

    // Proactive Healing
    public const string ProactiveHealing = "whm.proactive_healing";
    public const string RegenMaintenance = "whm.regen_maintenance";
    public const string ShieldTiming = "whm.shield_timing";

    // Damage Optimization
    public const string DpsOptimization = "whm.dps_optimization";
    public const string GlarePriority = "whm.glare_priority";
    public const string DotMaintenance = "whm.dot_maintenance";

    // Utility
    public const string EsunaUsage = "whm.esuna_usage";
    public const string RaiseDecision = "whm.raise_decision";

    // Coordination
    public const string CoHealerAwareness = "whm.cohealer_awareness";
    public const string PartyCoordination = "whm.party_coordination";

    /// <summary>
    /// All WHM concepts for counting.
    /// </summary>
    public static readonly string[] AllConcepts = new[]
    {
        EmergencyHealing, BenedictionUsage, TetragrammatonUsage,
        HealingPriority, TankPriority, PartyWideDamage,
        OgcdWeaving, AssizeUsage, DivineBenisonUsage,
        LilyManagement, AfflatusRaptureUsage, AfflatusSolaceUsage, AfflatusMiseryTiming, BloodLilyBuilding,
        TemperanceUsage, AquaveilUsage, LiturgyOfTheBellUsage,
        ProactiveHealing, RegenMaintenance, ShieldTiming,
        DpsOptimization, GlarePriority, DotMaintenance,
        EsunaUsage, RaiseDecision,
        CoHealerAwareness, PartyCoordination,
    };
}
