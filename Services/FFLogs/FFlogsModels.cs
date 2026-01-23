using System;
using System.Collections.Generic;

namespace Olympus.Services.FFLogs;

/// <summary>
/// OAuth token response from FFLogs API.
/// </summary>
public sealed class FFlogsTokenResponse
{
    public string AccessToken { get; set; } = "";
    public string TokenType { get; set; } = "Bearer";
    public int ExpiresIn { get; set; }
}

/// <summary>
/// Character data from FFLogs.
/// </summary>
public sealed class FFlogsCharacter
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Server { get; set; } = "";
    public string Region { get; set; } = "";
    public long? LodestoneId { get; set; }
}

/// <summary>
/// Encounter ranking data for a specific fight.
/// </summary>
public sealed class FFlogsEncounterRank
{
    public int EncounterId { get; set; }
    public string EncounterName { get; set; } = "";
    public float BestPercentile { get; set; }
    public float MedianPercentile { get; set; }
    public float BestAmount { get; set; }  // Best rDPS/HPS
    public int TotalKills { get; set; }
    public DateTime? LastKillTime { get; set; }
    public string? BestReportCode { get; set; }

    /// <summary>
    /// Trend compared to previous data point.
    /// Positive = improving, negative = declining.
    /// </summary>
    public float PercentileTrend { get; set; }
}

/// <summary>
/// Zone ranking data (e.g., Arcadion Savage tier).
/// </summary>
public sealed class FFlogsZoneRanking
{
    public int ZoneId { get; set; }
    public string ZoneName { get; set; } = "";
    public int Difficulty { get; set; }  // 101 = Savage
    public float AllStarsPoints { get; set; }
    public int AllStarsRank { get; set; }
    public int AllStarsRankPercent { get; set; }
    public List<FFlogsEncounterRank> Encounters { get; set; } = new();
}

/// <summary>
/// Historical parse data for a specific encounter.
/// </summary>
public sealed class FFlogsParseRecord
{
    public float Percentile { get; set; }
    public float TodayPercentile { get; set; }  // Percentile based on today's rankings
    public float Amount { get; set; }  // rDPS/HPS
    public DateTime StartTime { get; set; }
    public string ReportCode { get; set; } = "";
    public int FightId { get; set; }
    public TimeSpan Duration { get; set; }
}

/// <summary>
/// Comparison between local fight metrics and FFLogs data.
/// </summary>
public sealed class FFlogsParseComparison
{
    /// <summary>
    /// Encounter ID for this comparison.
    /// </summary>
    public int EncounterId { get; set; }

    /// <summary>
    /// Encounter name for display.
    /// </summary>
    public string EncounterName { get; set; } = "";

    /// <summary>
    /// Player's local DPS from the fight.
    /// </summary>
    public float LocalDps { get; set; }

    /// <summary>
    /// Player's best DPS on FFLogs for this encounter.
    /// </summary>
    public float FFlogsbestDps { get; set; }

    /// <summary>
    /// Estimated percentile based on current FFLogs rankings.
    /// </summary>
    public float EstimatedPercentile { get; set; }

    /// <summary>
    /// Difference between local and FFLogs best (percentage).
    /// Negative = below FFLogs best, positive = above.
    /// </summary>
    public float DpsDifferencePercent => FFlogsbestDps > 0
        ? (LocalDps - FFlogsbestDps) / FFlogsbestDps * 100f
        : 0f;

    /// <summary>
    /// Local GCD uptime percentage.
    /// </summary>
    public float LocalGcdUptime { get; set; }

    /// <summary>
    /// Typical GCD uptime for top parses (approximation).
    /// </summary>
    public float TopParseGcdUptime { get; set; } = 98.5f;

    /// <summary>
    /// Local cooldown efficiency percentage.
    /// </summary>
    public float LocalCooldownEfficiency { get; set; }

    /// <summary>
    /// Typical cooldown efficiency for top parses (approximation).
    /// </summary>
    public float TopParseCooldownEfficiency { get; set; } = 95f;

    /// <summary>
    /// Improvement tips based on comparison.
    /// </summary>
    public List<string> ImprovementTips { get; set; } = new();

    /// <summary>
    /// When this comparison was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Rate limit information from FFLogs API.
/// </summary>
public sealed class FFlogsRateLimitInfo
{
    public int PointsRemaining { get; set; }
    public int PointsLimit { get; set; }
    public int PointsResetIn { get; set; }  // Seconds until reset

    public float UsagePercent => PointsLimit > 0
        ? (float)(PointsLimit - PointsRemaining) / PointsLimit * 100f
        : 0f;

    public bool IsLowOnPoints => PointsRemaining < 500;
}

/// <summary>
/// Cached FFLogs data with expiry tracking.
/// </summary>
public sealed class FFlogsCache<T>
{
    public T? Data { get; set; }
    public DateTime CachedAt { get; set; }
    public DateTime ExpiresAt { get; set; }

    public bool IsExpired => DateTime.Now > ExpiresAt;
    public bool IsValid => Data != null && !IsExpired;
}

/// <summary>
/// Result of an FFLogs API operation.
/// </summary>
public sealed class FFlogsResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public FFlogsErrorType ErrorType { get; set; }

    public static FFlogsResult<T> Ok(T data) => new() { Success = true, Data = data };
    public static FFlogsResult<T> Fail(string error, FFlogsErrorType type = FFlogsErrorType.Unknown) =>
        new() { Success = false, Error = error, ErrorType = type };
}

/// <summary>
/// Types of FFLogs API errors.
/// </summary>
public enum FFlogsErrorType
{
    None,
    Unknown,
    InvalidCredentials,
    RateLimited,
    NetworkError,
    CharacterNotFound,
    NoParses,
    ServerError
}

/// <summary>
/// Encounter IDs for current savage tier (Arcadion).
/// </summary>
public static class FFlogsEncounterIds
{
    // Arcadion Savage (M1S-M4S)
    public const int BlackCatM1S = 93;
    public const int HoneyBLovelyM2S = 94;
    public const int BruteBomberM3S = 95;
    public const int WickedThunderM4S = 96;

    // Zone ID for Arcadion Savage
    public const int ArcadionSavageZone = 62;

    /// <summary>
    /// Gets the encounter name from ID.
    /// </summary>
    public static string GetEncounterName(int encounterId) => encounterId switch
    {
        BlackCatM1S => "AAC Light-heavyweight M1 (Savage)",
        HoneyBLovelyM2S => "AAC Light-heavyweight M2 (Savage)",
        BruteBomberM3S => "AAC Light-heavyweight M3 (Savage)",
        WickedThunderM4S => "AAC Light-heavyweight M4 (Savage)",
        _ => $"Encounter {encounterId}"
    };
}
