namespace Olympus.Services.Targeting;

/// <summary>
/// Strategy for selecting enemy targets during combat.
/// </summary>
public enum EnemyTargetingStrategy
{
    /// <summary>
    /// Target enemy with lowest current HP (default).
    /// Good for finishing off weak enemies quickly.
    /// </summary>
    LowestHp,

    /// <summary>
    /// Target enemy with highest current HP.
    /// Useful for cleave/AoE optimization.
    /// </summary>
    HighestHp,

    /// <summary>
    /// Target closest enemy by distance.
    /// </summary>
    Nearest,

    /// <summary>
    /// Follow party tank's current target.
    /// Best for dungeons and raids to avoid pulling aggro.
    /// </summary>
    TankAssist,

    /// <summary>
    /// Use player's current hard target if valid.
    /// Falls back to LowestHp if no valid target.
    /// </summary>
    CurrentTarget,

    /// <summary>
    /// Use player's focus target if valid.
    /// Falls back to LowestHp if no valid focus target.
    /// </summary>
    FocusTarget
}
