namespace Olympus.Rotation.Common;

/// <summary>
/// Extended rotation context for caster DPS jobs.
/// Casters don't have combos, but need MP management and cast time awareness.
/// </summary>
public interface ICasterDpsRotationContext : IRotationContext
{
    #region Resource State

    /// <summary>
    /// Current MP value.
    /// </summary>
    int CurrentMp { get; }

    /// <summary>
    /// Maximum MP value.
    /// </summary>
    int MaxMp { get; }

    /// <summary>
    /// Current MP percentage (0.0 - 1.0).
    /// </summary>
    float MpPercent { get; }

    #endregion

    #region Cast State

    /// <summary>
    /// Whether the player is currently casting.
    /// </summary>
    bool IsCasting { get; }

    /// <summary>
    /// Time remaining on current cast (in seconds).
    /// </summary>
    float CastRemaining { get; }

    /// <summary>
    /// Whether the player can slidecast (cast is nearly complete).
    /// Typically within 0.5s of cast completion.
    /// </summary>
    bool CanSlidecast { get; }

    #endregion

    #region Instant Cast Buffs

    /// <summary>
    /// Whether the player has Triplecast active (caster-specific).
    /// </summary>
    bool HasTriplecast { get; }

    /// <summary>
    /// Number of Triplecast stacks remaining.
    /// </summary>
    int TriplecastStacks { get; }

    /// <summary>
    /// Whether any instant cast buff is active (Swiftcast, Triplecast, etc.).
    /// </summary>
    bool HasInstantCast { get; }

    #endregion
}
