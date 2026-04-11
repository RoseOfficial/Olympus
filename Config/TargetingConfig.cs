using Olympus.Services.Targeting;

namespace Olympus.Config;

/// <summary>
/// Configuration for targeting settings.
/// </summary>
public sealed class TargetingConfig
{
    /// <summary>
    /// Strategy for selecting enemy targets during combat.
    /// </summary>
    public EnemyTargetingStrategy EnemyStrategy { get; set; } = EnemyTargetingStrategy.LowestHp;

    /// <summary>
    /// When using TankAssist strategy, fall back to LowestHp if no tank target is found.
    /// </summary>
    public bool UseTankAssistFallback { get; set; } = true;

    /// <summary>
    /// How long to cache valid enemy list in milliseconds.
    /// Higher values improve performance but may delay target switching.
    /// </summary>
    public int TargetCacheTtlMs { get; set; } = 100;

    /// <summary>
    /// When true, all damage targeting is suppressed while the player has no selected target.
    /// This is the primary safeguard for gaze mechanics (drop target to look away) and for
    /// any case where the player wants Olympus to stop attacking. Default ON.
    /// </summary>
    public bool PauseWhenNoTarget { get; set; } = true;

    /// <summary>
    /// When true, the fallback that retargets to LowestHp when CurrentTarget/FocusTarget
    /// strategies fail is disabled — a missing current target simply stops damage. This
    /// makes "drop target" a hard pause for players using explicit-target strategies.
    /// Default ON.
    /// </summary>
    public bool StrictCurrentTargetStrategy { get; set; } = true;

    /// <summary>
    /// Master toggle for gap closer safety heuristics. When ON:
    ///  - Gap closers will only fire on the enemy the player has explicitly targeted.
    ///  - Gap closers are blocked if the player has been moving away from the target recently.
    /// Default ON.
    /// </summary>
    public bool SafeGapCloser { get; set; } = true;

    /// <summary>
    /// How far back (milliseconds) to track player movement when deciding whether they are
    /// actively moving away from the current target. 400ms is roughly a server tick and
    /// catches intentional repositioning without being noisy on small jitters.
    /// </summary>
    public int GapCloserMovementLookbackMs { get; set; } = 400;

    /// <summary>
    /// Minimum distance the player must have gained from the target within the lookback
    /// window to be considered "moving away". Expressed in yalms. 1.0y is small enough
    /// to trigger on deliberate movement but large enough to ignore GCD-stutter jitter.
    /// </summary>
    public float GapCloserMovementAwayThresholdY { get; set; } = 1.0f;

    /// <summary>
    /// When true, auto-targeting filters out enemies that are behind walls or
    /// other geometry using a BGCollision raycast. Prevents the rotation from
    /// trying to cast through pillars in dungeons and raids.
    /// </summary>
    public bool EnableLineOfSightFiltering { get; set; } = true;

    /// <summary>
    /// When true, auto-targeting skips enemies that have known invulnerability status
    /// effects (boss phase transitions, invulnerable adds, untouchable objects).
    /// Prevents the rotation from wasting actions on immune targets.
    /// Only affects aggregate strategies (LowestHp, HighestHp, Nearest, TankAssist) —
    /// explicit CurrentTarget/FocusTarget selections are never filtered.
    /// </summary>
    public bool EnableInvulnerabilityFiltering { get; set; } = true;
}
