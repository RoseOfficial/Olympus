namespace Olympus.Timeline.Models;

/// <summary>
/// Represents a predicted upcoming mechanic for rotation decision-making.
/// Passed to rotation modules to enable intelligent cooldown timing.
/// </summary>
public readonly struct MechanicPrediction
{
    /// <summary>
    /// Seconds until the mechanic occurs.
    /// Negative values indicate the mechanic is currently active (for duration mechanics).
    /// </summary>
    public float SecondsUntil { get; init; }

    /// <summary>
    /// The type of mechanic (Raidwide, TankBuster, etc.).
    /// </summary>
    public TimelineEntryType Type { get; init; }

    /// <summary>
    /// Display name of the mechanic.
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Confidence score (0.0 to 1.0) for this prediction.
    /// Higher values indicate recent sync and reliable timing.
    /// </summary>
    public float Confidence { get; init; }

    /// <summary>
    /// Duration of the mechanic in seconds.
    /// Zero for instant mechanics.
    /// </summary>
    public float Duration { get; init; }

    /// <summary>
    /// Creates a new mechanic prediction.
    /// </summary>
    public MechanicPrediction(float secondsUntil, TimelineEntryType type, string name, float confidence, float duration = 0f)
    {
        SecondsUntil = secondsUntil;
        Type = type;
        Name = name;
        Confidence = confidence;
        Duration = duration;
    }

    /// <summary>
    /// Whether this prediction is high confidence (synced recently).
    /// </summary>
    public bool IsHighConfidence => Confidence >= 0.8f;

    /// <summary>
    /// Whether the mechanic is imminent (within 3 seconds).
    /// </summary>
    public bool IsImminent => SecondsUntil <= 3f;

    /// <summary>
    /// Whether the mechanic is soon (within 8 seconds).
    /// This is the typical window for pre-shielding raidwides.
    /// </summary>
    public bool IsSoon => SecondsUntil <= 8f;

    /// <summary>
    /// Whether cooldowns should be held for this mechanic.
    /// Returns true if the mechanic is soon and we have high confidence.
    /// </summary>
    public bool ShouldHoldCooldowns => IsSoon && IsHighConfidence;

    public override string ToString()
        => $"{Name} ({Type}) in {SecondsUntil:F1}s [{Confidence:P0}]";
}
