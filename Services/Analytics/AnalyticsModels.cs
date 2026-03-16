using System;
using System.Collections.Generic;

namespace Olympus.Services.Analytics;

/// <summary>
/// Real-time combat metrics snapshot.
/// Captured periodically during combat and at combat end.
/// </summary>
public sealed class CombatMetricsSnapshot
{
    /// <summary>
    /// Duration of the current combat in seconds.
    /// </summary>
    public float CombatDuration { get; init; }

    /// <summary>
    /// GCD uptime percentage (0-100).
    /// Higher is better - indicates how efficiently GCDs are being used.
    /// </summary>
    public float GcdUptime { get; init; }

    /// <summary>
    /// Personal DPS calculated from total damage over combat duration.
    /// </summary>
    public float PersonalDps { get; init; }

    /// <summary>
    /// Total damage dealt during combat.
    /// </summary>
    public long TotalDamage { get; init; }

    /// <summary>
    /// Total healing done during combat.
    /// </summary>
    public long TotalHealing { get; init; }

    /// <summary>
    /// Percentage of healing that was overheal (0-100).
    /// Lower is better for healers.
    /// </summary>
    public float OverhealPercent { get; init; }

    /// <summary>
    /// Number of party member deaths during combat.
    /// </summary>
    public int Deaths { get; init; }

    /// <summary>
    /// Number of times a party member dropped below the near-death threshold.
    /// </summary>
    public int NearDeaths { get; init; }

    /// <summary>
    /// Cooldown usage efficiency for tracked abilities.
    /// </summary>
    public List<CooldownUsage> Cooldowns { get; init; } = new();

    /// <summary>
    /// Timestamp when this snapshot was taken.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;

    /// <summary>
    /// Breakdown of downtime causes (movement, death, mechanics, unexplained).
    /// Only populated when TrackDowntimeBreakdown is enabled.
    /// </summary>
    public DowntimeBreakdown? DowntimeAnalysis { get; init; }
}

/// <summary>
/// Breakdown of GCD downtime by cause.
/// Helps players understand why uptime was lost.
/// </summary>
public sealed class DowntimeBreakdown
{
    /// <summary>
    /// Total downtime in seconds (GCD ready but no action taken).
    /// </summary>
    public float TotalDowntimeSeconds { get; set; }

    /// <summary>
    /// Downtime caused by player movement while GCD was ready.
    /// </summary>
    public float MovementSeconds { get; set; }

    /// <summary>
    /// Downtime while player was dead or incapacitated.
    /// </summary>
    public float DeathSeconds { get; set; }

    /// <summary>
    /// Downtime during known boss mechanics (from timeline).
    /// </summary>
    public float MechanicSeconds { get; set; }

    /// <summary>
    /// Unexplained downtime - GCD ready, not moving, not dead, no mechanic.
    /// This is the "bad" downtime players should minimize.
    /// </summary>
    public float UnforcedSeconds { get; set; }

    /// <summary>
    /// Percentage of downtime from movement.
    /// </summary>
    public float MovementPercent => TotalDowntimeSeconds > 0
        ? MovementSeconds / TotalDowntimeSeconds * 100f : 0f;

    /// <summary>
    /// Percentage of downtime from death.
    /// </summary>
    public float DeathPercent => TotalDowntimeSeconds > 0
        ? DeathSeconds / TotalDowntimeSeconds * 100f : 0f;

    /// <summary>
    /// Percentage of downtime from mechanics.
    /// </summary>
    public float MechanicPercent => TotalDowntimeSeconds > 0
        ? MechanicSeconds / TotalDowntimeSeconds * 100f : 0f;

    /// <summary>
    /// Percentage of unexplained downtime.
    /// </summary>
    public float UnforcedPercent => TotalDowntimeSeconds > 0
        ? UnforcedSeconds / TotalDowntimeSeconds * 100f : 0f;
}

/// <summary>
/// Tracks usage efficiency for a single cooldown ability.
/// </summary>
public sealed class CooldownUsage
{
    /// <summary>
    /// The action ID of this cooldown.
    /// </summary>
    public uint ActionId { get; init; }

    /// <summary>
    /// Display name of the action.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Base cooldown duration in seconds.
    /// </summary>
    public float CooldownDuration { get; init; }

    /// <summary>
    /// Number of times this ability was used.
    /// </summary>
    public int TimesUsed { get; init; }

    /// <summary>
    /// Optimal number of uses based on fight duration / cooldown.
    /// </summary>
    public int OptimalUses { get; init; }

    /// <summary>
    /// Efficiency percentage (TimesUsed / OptimalUses * 100).
    /// </summary>
    public float Efficiency => OptimalUses > 0 ? (float)TimesUsed / OptimalUses * 100f : 0f;

    /// <summary>
    /// Average drift in seconds (how late abilities were used on average).
    /// 0 = perfect, higher = worse.
    /// </summary>
    public float AverageDrift { get; init; }

    /// <summary>
    /// Individual drift values for each use (for detailed analysis).
    /// </summary>
    public List<float> DriftValues { get; init; } = new();
}

/// <summary>
/// Performance scores on a 0-100 scale with letter grades.
/// </summary>
public sealed class PerformanceScore
{
    /// <summary>
    /// Overall weighted score combining all categories.
    /// </summary>
    public float Overall { get; init; }

    /// <summary>
    /// GCD uptime score - how efficiently GCDs were used.
    /// </summary>
    public float GcdUptime { get; init; }

    /// <summary>
    /// Cooldown efficiency - how well key abilities were used on cooldown.
    /// </summary>
    public float CooldownEfficiency { get; init; }

    /// <summary>
    /// Healing efficiency (for healers) - effective healing vs overheal ratio.
    /// </summary>
    public float HealingEfficiency { get; init; }

    /// <summary>
    /// Survival score - based on deaths and near-deaths.
    /// </summary>
    public float Survival { get; init; }

    /// <summary>
    /// Gets the letter grade for a score value.
    /// </summary>
    public static string GetGrade(float score) => score switch
    {
        >= 99f => "A+",
        >= 95f => "A",
        >= 90f => "A-",
        >= 85f => "B+",
        >= 80f => "B",
        >= 75f => "B-",
        >= 70f => "C+",
        >= 65f => "C",
        >= 60f => "C-",
        >= 55f => "D+",
        >= 50f => "D",
        >= 45f => "D-",
        _ => "F"
    };

    /// <summary>
    /// Gets the letter grade for the overall score.
    /// </summary>
    public string OverallGrade => GetGrade(Overall);
}

/// <summary>
/// Type of performance issue detected.
/// </summary>
public enum IssueType
{
    /// <summary>
    /// GCD-related issue (downtime, clipping).
    /// </summary>
    GcdDowntime,

    /// <summary>
    /// Cooldown not used optimally.
    /// </summary>
    CooldownDrift,

    /// <summary>
    /// High overheal percentage.
    /// </summary>
    HighOverheal,

    /// <summary>
    /// Party member death.
    /// </summary>
    PartyDeath,

    /// <summary>
    /// Party member dropped to critical HP.
    /// </summary>
    NearDeath,

    /// <summary>
    /// Resource capped (MP, gauge, etc.).
    /// </summary>
    ResourceCapped,

    /// <summary>
    /// Ability not used at all during fight.
    /// </summary>
    AbilityUnused
}

/// <summary>
/// Severity of a performance issue.
/// </summary>
public enum IssueSeverity
{
    /// <summary>
    /// Minor issue - small optimization opportunity.
    /// </summary>
    Info,

    /// <summary>
    /// Moderate issue - noticeable impact on performance.
    /// </summary>
    Warning,

    /// <summary>
    /// Significant issue - major impact on performance.
    /// </summary>
    Error
}

/// <summary>
/// A specific performance issue detected during analysis.
/// </summary>
public sealed class PerformanceIssue
{
    /// <summary>
    /// Type of issue detected.
    /// </summary>
    public IssueType Type { get; init; }

    /// <summary>
    /// Severity of the issue.
    /// </summary>
    public IssueSeverity Severity { get; init; }

    /// <summary>
    /// Human-readable description of the issue.
    /// </summary>
    public string Description { get; init; } = "";

    /// <summary>
    /// Actionable suggestion to fix the issue.
    /// </summary>
    public string Suggestion { get; init; } = "";

    /// <summary>
    /// Time in fight when issue occurred (seconds from start).
    /// -1 if applies to entire fight.
    /// </summary>
    public float TimeInFight { get; init; } = -1f;

    /// <summary>
    /// Related action ID if applicable.
    /// </summary>
    public uint? ActionId { get; init; }
}

/// <summary>
/// Complete record of a single combat session.
/// </summary>
public sealed class FightSession
{
    /// <summary>
    /// Unique identifier for this session.
    /// </summary>
    public Guid Id { get; init; } = Guid.NewGuid();

    /// <summary>
    /// When the fight started.
    /// </summary>
    public DateTime StartTime { get; init; }

    /// <summary>
    /// When the fight ended.
    /// </summary>
    public DateTime EndTime { get; init; }

    /// <summary>
    /// Duration in seconds.
    /// </summary>
    public float Duration => (float)(EndTime - StartTime).TotalSeconds;

    /// <summary>
    /// Job ID of the player during this session.
    /// </summary>
    public uint JobId { get; init; }

    /// <summary>
    /// Zone/duty name if available.
    /// </summary>
    public string ZoneName { get; init; } = "Unknown";

    /// <summary>
    /// Final metrics snapshot from the fight.
    /// </summary>
    public CombatMetricsSnapshot? FinalMetrics { get; init; }

    /// <summary>
    /// Calculated performance score.
    /// </summary>
    public PerformanceScore? Score { get; init; }

    /// <summary>
    /// Issues detected during the fight.
    /// </summary>
    public List<PerformanceIssue> Issues { get; init; } = new();

    /// <summary>
    /// Brief summary for history display.
    /// </summary>
    public string Summary => $"{Duration:F0}s | Score: {Score?.Overall:F0 ?? 0}/100 ({Score?.OverallGrade ?? "?"})";
}

/// <summary>
/// Trend data for historical analysis.
/// </summary>
public sealed class PerformanceTrend
{
    /// <summary>
    /// Average overall score across sessions.
    /// </summary>
    public float AverageScore { get; init; }

    /// <summary>
    /// Average GCD uptime across sessions.
    /// </summary>
    public float AverageGcdUptime { get; init; }

    /// <summary>
    /// Is performance improving over time?
    /// Positive = improving, negative = declining, 0 = stable.
    /// </summary>
    public float ScoreTrend { get; init; }

    /// <summary>
    /// Number of sessions included in trend calculation.
    /// </summary>
    public int SessionCount { get; init; }

    /// <summary>
    /// Trend direction description.
    /// </summary>
    public string TrendDescription => ScoreTrend switch
    {
        > 2f => "Improving",
        < -2f => "Declining",
        _ => "Stable"
    };
}

/// <summary>
/// Combat phase for contextual cooldown analysis.
/// </summary>
public enum CooldownPhase
{
    /// <summary>
    /// First 15 seconds of combat - opener sequence.
    /// </summary>
    Opener,

    /// <summary>
    /// During raid buff window - burst phase.
    /// </summary>
    Burst,

    /// <summary>
    /// Normal rotation outside of special windows.
    /// </summary>
    Sustained,

    /// <summary>
    /// Post-death or post-mechanic recovery period.
    /// </summary>
    Recovery
}

/// <summary>
/// Record of a single cooldown use with full context.
/// </summary>
public sealed class CooldownUseRecord
{
    /// <summary>
    /// The action ID that was used.
    /// </summary>
    public uint ActionId { get; init; }

    /// <summary>
    /// When the ability was used.
    /// </summary>
    public DateTime Timestamp { get; init; }

    /// <summary>
    /// Seconds into the fight when used.
    /// </summary>
    public float FightTimeSeconds { get; init; }

    /// <summary>
    /// How many seconds late the ability was used vs optimal timing.
    /// 0 = perfect, higher = more drift.
    /// </summary>
    public float DriftSeconds { get; init; }

    /// <summary>
    /// Combat phase when the ability was used.
    /// </summary>
    public CooldownPhase Phase { get; init; }

    /// <summary>
    /// Whether the cooldown was ready when used.
    /// False indicates a charge-based ability or timing quirk.
    /// </summary>
    public bool WasAvailable { get; init; }

    /// <summary>
    /// Optional context describing why/when used (e.g., "post-raidwide", "burst window").
    /// </summary>
    public string? Context { get; init; }
}

/// <summary>
/// Record of a missed opportunity to use a cooldown.
/// </summary>
public sealed class MissedCooldownOpportunity
{
    /// <summary>
    /// The action ID that was available but not used.
    /// </summary>
    public uint ActionId { get; init; }

    /// <summary>
    /// Display name of the ability.
    /// </summary>
    public string AbilityName { get; init; } = "";

    /// <summary>
    /// When during the fight this opportunity occurred (seconds from start).
    /// </summary>
    public float FightTimeSeconds { get; init; }

    /// <summary>
    /// How long the cooldown sat available without being used (seconds).
    /// </summary>
    public float AvailableForSeconds { get; init; }

    /// <summary>
    /// Reason the ability wasn't used (e.g., "Movement", "Mechanic", "Unknown").
    /// </summary>
    public string Reason { get; init; } = "Unknown";
}

/// <summary>
/// Enhanced cooldown analysis with per-use details and missed opportunities.
/// </summary>
public sealed class CooldownAnalysis
{
    /// <summary>
    /// The action ID being analyzed.
    /// </summary>
    public uint ActionId { get; init; }

    /// <summary>
    /// Display name of the ability.
    /// </summary>
    public string Name { get; init; } = "";

    /// <summary>
    /// Base cooldown duration in seconds.
    /// </summary>
    public float CooldownDuration { get; init; }

    /// <summary>
    /// Number of times this ability was used.
    /// </summary>
    public int TimesUsed { get; init; }

    /// <summary>
    /// Optimal number of uses based on fight duration.
    /// </summary>
    public int OptimalUses { get; init; }

    /// <summary>
    /// Usage efficiency percentage (TimesUsed / OptimalUses * 100).
    /// </summary>
    public float Efficiency => OptimalUses > 0 ? (float)TimesUsed / OptimalUses * 100f : 0f;

    /// <summary>
    /// Average drift in seconds across all uses.
    /// </summary>
    public float AverageDrift { get; init; }

    /// <summary>
    /// Detailed record of each use.
    /// </summary>
    public IReadOnlyList<CooldownUseRecord> Uses { get; init; } = Array.Empty<CooldownUseRecord>();

    /// <summary>
    /// Detected missed opportunities where cooldown sat unused.
    /// </summary>
    public IReadOnlyList<MissedCooldownOpportunity> MissedOpportunities { get; init; } = Array.Empty<MissedCooldownOpportunity>();

    /// <summary>
    /// Number of uses during opener phase (first 15s).
    /// </summary>
    public int OpenerUses { get; init; }

    /// <summary>
    /// Number of uses during burst windows.
    /// </summary>
    public int BurstUses { get; init; }

    /// <summary>
    /// Number of uses during sustained/normal rotation.
    /// </summary>
    public int SustainedUses { get; init; }

    /// <summary>
    /// Total drift accumulated across all uses (seconds).
    /// </summary>
    public float TotalDriftSeconds { get; init; }

    /// <summary>
    /// Count of detected missed use opportunities.
    /// </summary>
    public int MissedUsesCount => MissedOpportunities.Count;

    /// <summary>
    /// Primary issue affecting this cooldown's usage.
    /// </summary>
    public string PrimaryIssue { get; init; } = "Good";

    /// <summary>
    /// Rating for UI display.
    /// </summary>
    public string Rating => Efficiency switch
    {
        >= 90f => "Excellent",
        >= 75f => "Good",
        >= 50f => "Needs Work",
        _ => "Poor"
    };

    /// <summary>
    /// Actionable tip based on detected issues.
    /// </summary>
    public string? Tip { get; init; }
}
