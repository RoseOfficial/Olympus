namespace Olympus.Timeline.Models;

/// <summary>
/// Defines how a timeline entry syncs to game events.
/// When the sync condition is met, the timeline position updates to this entry's timestamp.
/// </summary>
public readonly struct TimelineSync
{
    /// <summary>
    /// The type of game event to sync on.
    /// </summary>
    public SyncType Type { get; init; }

    /// <summary>
    /// The action ID to match (for Ability/StartsUsing sync types).
    /// Zero if not applicable.
    /// </summary>
    public uint ActionId { get; init; }

    /// <summary>
    /// Optional source name filter (e.g., boss name).
    /// Null or empty matches any source.
    /// </summary>
    public string? SourceName { get; init; }

    /// <summary>
    /// How many seconds before the entry timestamp to accept this sync.
    /// Default is 2.5 seconds.
    /// </summary>
    public float WindowBefore { get; init; }

    /// <summary>
    /// How many seconds after the entry timestamp to accept this sync.
    /// Default is 2.5 seconds.
    /// </summary>
    public float WindowAfter { get; init; }

    /// <summary>
    /// Creates a new timeline sync configuration.
    /// </summary>
    public TimelineSync(SyncType type, uint actionId = 0, string? sourceName = null,
        float windowBefore = 2.5f, float windowAfter = 2.5f)
    {
        Type = type;
        ActionId = actionId;
        SourceName = sourceName;
        WindowBefore = windowBefore;
        WindowAfter = windowAfter;
    }

    /// <summary>
    /// Creates an ability sync (action effect resolves).
    /// </summary>
    public static TimelineSync Ability(uint actionId, string? source = null, float windowBefore = 2.5f, float windowAfter = 2.5f)
        => new(SyncType.Ability, actionId, source, windowBefore, windowAfter);

    /// <summary>
    /// Creates a StartsUsing sync (cast bar begins).
    /// </summary>
    public static TimelineSync StartsUsing(uint actionId, string? source = null, float windowBefore = 2.5f, float windowAfter = 2.5f)
        => new(SyncType.StartsUsing, actionId, source, windowBefore, windowAfter);

    /// <summary>
    /// Creates an InCombat sync (combat begins).
    /// </summary>
    public static TimelineSync InCombat()
        => new(SyncType.InCombat);
}

/// <summary>
/// The type of game event that triggers a timeline sync.
/// </summary>
public enum SyncType : byte
{
    /// <summary>
    /// Sync when an ability effect resolves (21-line ACT log).
    /// Most common sync type for damage resolution.
    /// </summary>
    Ability = 0,

    /// <summary>
    /// Sync when a cast bar begins (14-line ACT log).
    /// Provides earlier warning than Ability sync.
    /// </summary>
    StartsUsing = 1,

    /// <summary>
    /// Sync when combat begins (entering InCombat state).
    /// Used to start the timeline at 0.
    /// </summary>
    InCombat = 2,
}
