using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

namespace Olympus.Services.Targeting;

/// <summary>
/// Interface for targeting services with multiple strategies.
/// </summary>
public interface ITargetingService
{
    /// <summary>
    /// Finds an enemy target using the specified strategy.
    /// </summary>
    IBattleNpc? FindEnemy(EnemyTargetingStrategy strategy, float maxRange, IPlayerCharacter player);

    /// <summary>
    /// Finds an enemy that needs DoT applied or refreshed.
    /// </summary>
    IBattleNpc? FindEnemyNeedingDot(uint dotStatusId, float refreshThreshold, float maxRange, IPlayerCharacter player);

    /// <summary>
    /// Counts the number of valid enemies within the specified radius of the player.
    /// </summary>
    int CountEnemiesInRange(float radius, IPlayerCharacter player);

    /// <summary>
    /// Finds the enemy that has the most other enemies within the specified radius.
    /// </summary>
    (IBattleNpc? target, int hitCount) FindBestAoETarget(float aoeRadius, float maxRange, IPlayerCharacter player);

    /// <summary>
    /// Invalidates the enemy cache.
    /// </summary>
    void InvalidateCache();
}
