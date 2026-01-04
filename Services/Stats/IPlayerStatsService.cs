namespace Olympus.Services.Stats;

/// <summary>
/// Interface for player combat stats service.
/// </summary>
public interface IPlayerStatsService
{
    /// <summary>
    /// Gets the player's current Mind stat.
    /// </summary>
    int GetMind();

    /// <summary>
    /// Gets the player's current Determination stat.
    /// </summary>
    int GetDetermination();

    /// <summary>
    /// Gets the player's weapon magic damage.
    /// </summary>
    int GetWeaponDamage(int level);

    /// <summary>
    /// Gets all relevant stats for healing calculations.
    /// </summary>
    (int Mind, int Determination, int WeaponDamage) GetHealingStats(int level);
}
