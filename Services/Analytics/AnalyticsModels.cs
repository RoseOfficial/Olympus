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
    public IReadOnlyList<CooldownUsage> Cooldowns { get; init; } = Array.Empty<CooldownUsage>();

    /// <summary>
    /// Timestamp when this snapshot was taken.
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.Now;
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
    public IReadOnlyList<float> DriftValues { get; init; } = Array.Empty<float>();
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
    public IReadOnlyList<PerformanceIssue> Issues { get; init; } = Array.Empty<PerformanceIssue>();

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
