using Dalamud.Game.ClientState.Objects.Types;

namespace Olympus.Services.Positional;

/// <summary>
/// Positional type for melee DPS.
/// </summary>
public enum PositionalType
{
    /// <summary>Player is in front of target (0-45 or 315-360 degrees).</summary>
    Front,

    /// <summary>Player is at target's flank (45-135 or 225-315 degrees).</summary>
    Flank,

    /// <summary>Player is at target's rear (135-225 degrees).</summary>
    Rear
}

/// <summary>
/// Service for determining player position relative to target (rear, flank, front).
/// Essential for melee DPS jobs that have positional requirements.
/// </summary>
public interface IPositionalService
{
    /// <summary>
    /// Gets the current positional type relative to the target.
    /// </summary>
    /// <param name="player">The player character.</param>
    /// <param name="target">The enemy target.</param>
    /// <returns>The positional type (Front, Flank, or Rear).</returns>
    PositionalType GetPositional(IBattleChara player, IBattleChara target);

    /// <summary>
    /// Checks if the player is at the target's rear (135-225 degrees).
    /// </summary>
    bool IsAtRear(IBattleChara player, IBattleChara target);

    /// <summary>
    /// Checks if the player is at the target's flank (45-135 or 225-315 degrees).
    /// </summary>
    bool IsAtFlank(IBattleChara player, IBattleChara target);

    /// <summary>
    /// Checks if the player is in front of the target (0-45 or 315-360 degrees).
    /// </summary>
    bool IsAtFront(IBattleChara player, IBattleChara target);

    /// <summary>
    /// Checks if the target has positional immunity (ring/circle indicator).
    /// Some enemies don't have positional weaknesses.
    /// </summary>
    /// <param name="target">The enemy target to check.</param>
    /// <returns>True if the target has no positional requirements.</returns>
    bool HasPositionalImmunity(IBattleChara target);
}
