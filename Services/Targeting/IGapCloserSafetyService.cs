using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

namespace Olympus.Services.Targeting;

/// <summary>
/// Decides whether a gap closer (Onslaught, Trajectory, Shadowstride, Intervene,
/// Thunderclap, Dragoon jumps, etc.) is safe to fire this frame. The service is
/// purely advisory — rotations call <see cref="ShouldBlockGapCloser"/> before
/// committing their oGCD and skip the cast when it returns true.
///
/// <para>
/// Design philosophy: Marker-status detection (spread, stack, prey) is fragile and
/// content-specific. Instead this service uses player-intent heuristics:
///   1. <b>Explicit target match</b> — only gap close onto the enemy the player has
///      actively selected, never a strategy fallback.
///   2. <b>Moving-away guard</b> — if the player has been gaining distance from the
///      target within the lookback window, they are repositioning out of danger.
/// Together these cover the common failure modes (spread markers with AoE at the
/// destination, kiting during ground AoE, gaze mechanics) without maintaining a
/// per-fight status ID registry.
/// </para>
/// </summary>
public interface IGapCloserSafetyService
{
    /// <summary>
    /// Updates movement tracking. Call once per frame from the plugin's main loop
    /// with the current player and their selected target (may be null if the player
    /// has no target or a non-enemy target).
    /// </summary>
    void Update(IPlayerCharacter? player, IBattleChara? currentTarget);

    /// <summary>
    /// Returns true when the rotation should skip its gap closer this frame.
    /// </summary>
    /// <param name="target">The enemy the rotation wants to gap-close onto.</param>
    /// <param name="player">The player character.</param>
    bool ShouldBlockGapCloser(IBattleChara target, IPlayerCharacter player);

    /// <summary>
    /// Human-readable reason for the most recent block decision, for debug display.
    /// Null when the last call allowed the gap closer or nothing has been checked.
    /// </summary>
    string? LastBlockReason { get; }
}
