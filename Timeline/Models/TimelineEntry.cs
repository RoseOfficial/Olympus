namespace Olympus.Timeline.Models;

/// <summary>
/// Represents a single entry in a fight timeline.
/// Immutable struct for zero-allocation usage in hot paths.
/// </summary>
public readonly struct TimelineEntry
{
    /// <summary>
    /// Timestamp in seconds from fight start.
    /// Pre-sorted during parsing for efficient binary search.
    /// </summary>
    public float Timestamp { get; init; }

    /// <summary>
    /// Display name of the mechanic (e.g., "Black Cat Crossing").
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Categorization for rotation decision-making.
    /// </summary>
    public TimelineEntryType EntryType { get; init; }

    /// <summary>
    /// Optional sync configuration for timeline alignment.
    /// Null if this entry doesn't sync.
    /// </summary>
    public TimelineSync? Sync { get; init; }

    /// <summary>
    /// Duration in seconds for multi-phase mechanics.
    /// Zero for instant mechanics.
    /// </summary>
    public float Duration { get; init; }

    /// <summary>
    /// Target timestamp for jump directives (phase transitions).
    /// -1 if no jump.
    /// </summary>
    public float JumpTarget { get; init; }

    /// <summary>
    /// Label for reference by jump directives.
    /// Null if this entry has no label.
    /// </summary>
    public string? Label { get; init; }

    /// <summary>
    /// Whether this entry should be hidden from display.
    /// Hidden entries still participate in sync and jump logic.
    /// </summary>
    public bool IsHidden { get; init; }

    /// <summary>
    /// Creates a new timeline entry.
    /// </summary>
    public TimelineEntry(
        float timestamp,
        string name,
        TimelineEntryType entryType = TimelineEntryType.Ability,
        TimelineSync? sync = null,
        float duration = 0f,
        float jumpTarget = -1f,
        string? label = null,
        bool isHidden = false)
    {
        Timestamp = timestamp;
        Name = name;
        EntryType = entryType;
        Sync = sync;
        Duration = duration;
        JumpTarget = jumpTarget;
        Label = label;
        IsHidden = isHidden;
    }

    /// <summary>
    /// Returns true if this entry is a phase transition (has a label).
    /// </summary>
    public bool IsPhaseMarker => !string.IsNullOrEmpty(Label);

    /// <summary>
    /// Returns true if this entry causes a timeline jump.
    /// </summary>
    public bool HasJump => JumpTarget >= 0f;

    /// <summary>
    /// Returns true if this entry is a damage mechanic (raidwide or tankbuster).
    /// </summary>
    public bool IsDamageMechanic => EntryType is TimelineEntryType.Raidwide or TimelineEntryType.TankBuster;

    public override string ToString()
        => $"[{Timestamp:F1}s] {Name} ({EntryType})";
}
