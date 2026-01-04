using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using Olympus.Rotation.ApolloCore.Helpers;

namespace Olympus.Services.Targeting;

/// <summary>
/// Centralized targeting service with optimized filtering, caching, and multiple strategies.
/// </summary>
public sealed class TargetingService : ITargetingService
{
    private readonly IObjectTable _objectTable;
    private readonly IPartyList _partyList;
    private readonly ITargetManager _targetManager;
    private readonly Configuration _configuration;

    // Cache for valid enemies
    private readonly List<IBattleNpc> _cachedEnemies = new(32);
    private readonly Stopwatch _cacheTimer = new();
    private float _lastCacheRange;

    // Tank job IDs: PLD=19, WAR=21, DRK=32, GNB=37
    private static readonly HashSet<uint> TankJobIds = [19, 21, 32, 37];

    public TargetingService(
        IObjectTable objectTable,
        IPartyList partyList,
        ITargetManager targetManager,
        Configuration configuration)
    {
        _objectTable = objectTable;
        _partyList = partyList;
        _targetManager = targetManager;
        _configuration = configuration;
        _cacheTimer.Start();
    }

    /// <summary>
    /// Finds an enemy target using the specified strategy.
    /// </summary>
    /// <param name="strategy">Targeting strategy to use.</param>
    /// <param name="maxRange">Maximum range in yalms.</param>
    /// <param name="player">Current player character.</param>
    /// <returns>Best target according to strategy, or null if none found.</returns>
    public IBattleNpc? FindEnemy(EnemyTargetingStrategy strategy, float maxRange, IPlayerCharacter player)
    {
        // Try primary strategy
        var target = FindEnemyByStrategy(strategy, maxRange, player);

        // If TankAssist fails and fallback is enabled, try LowestHp
        if (target == null && strategy == EnemyTargetingStrategy.TankAssist && _configuration.Targeting.UseTankAssistFallback)
        {
            target = FindEnemyByStrategy(EnemyTargetingStrategy.LowestHp, maxRange, player);
        }

        // If CurrentTarget/FocusTarget fails, fall back to LowestHp
        if (target == null && strategy is EnemyTargetingStrategy.CurrentTarget or EnemyTargetingStrategy.FocusTarget)
        {
            target = FindEnemyByStrategy(EnemyTargetingStrategy.LowestHp, maxRange, player);
        }

        return target;
    }

    /// <summary>
    /// Finds an enemy that needs DoT applied or refreshed.
    /// </summary>
    /// <param name="dotStatusId">Status ID to check for (Aero/Dia variant).</param>
    /// <param name="refreshThreshold">Seconds remaining before DoT should be refreshed.</param>
    /// <param name="maxRange">Maximum range in yalms.</param>
    /// <param name="player">Current player character.</param>
    /// <returns>Enemy needing DoT, prioritizing those without DoT or lowest remaining duration.</returns>
    public IBattleNpc? FindEnemyNeedingDot(
        uint dotStatusId,
        float refreshThreshold,
        float maxRange,
        IPlayerCharacter player)
    {
        IBattleNpc? bestTarget = null;
        float lowestDuration = float.MaxValue;

        foreach (var enemy in GetValidEnemies(maxRange, player))
        {
            var dotDuration = GetDotDuration(enemy, dotStatusId);

            // Needs DoT if none present or expiring soon
            if (dotDuration < refreshThreshold && dotDuration < lowestDuration)
            {
                lowestDuration = dotDuration;
                bestTarget = enemy;
            }
        }

        return bestTarget;
    }

    /// <summary>
    /// Counts the number of valid enemies within the specified radius of the player.
    /// Used for AoE damage decisions (e.g., Holy when 3+ enemies).
    /// </summary>
    /// <param name="radius">Radius in yalms to check.</param>
    /// <param name="player">Current player character.</param>
    /// <returns>Number of valid enemies within radius.</returns>
    public int CountEnemiesInRange(float radius, IPlayerCharacter player)
    {
        int count = 0;
        foreach (var _ in GetValidEnemies(radius, player))
        {
            count++;
        }
        return count;
    }

    /// <summary>
    /// Finds the enemy that has the most other enemies within the specified radius.
    /// Used for targeted AoE spells like Glare IV and Afflatus Misery.
    /// </summary>
    /// <param name="aoeRadius">Radius around the target to count enemies.</param>
    /// <param name="maxRange">Maximum range from player to target.</param>
    /// <param name="player">Current player character.</param>
    /// <returns>Best AoE target and count of enemies that will be hit (including target).</returns>
    public (IBattleNpc? target, int hitCount) FindBestAoETarget(float aoeRadius, float maxRange, IPlayerCharacter player)
    {
        IBattleNpc? bestTarget = null;
        int bestHitCount = 0;

        // Collect all valid enemies in a list for efficient iteration
        var enemies = new List<IBattleNpc>();
        foreach (var enemy in GetValidEnemies(maxRange, player))
        {
            enemies.Add(enemy);
        }

        if (enemies.Count == 0)
            return (null, 0);

        // Early exit: if only 1 enemy, no need for O(n²) calculation
        if (enemies.Count == 1)
            return (enemies[0], 1);

        // For each potential target, count how many enemies would be hit
        var aoeRadiusSquared = aoeRadius * aoeRadius;
        foreach (var potentialTarget in enemies)
        {
            int hitCount = 1; // Always hits the target itself

            // Count other enemies within AoE radius of this target
            foreach (var other in enemies)
            {
                if (other.EntityId == potentialTarget.EntityId)
                    continue;

                var distSquared = Vector3.DistanceSquared(potentialTarget.Position, other.Position);
                if (distSquared <= aoeRadiusSquared)
                {
                    hitCount++;
                }
            }

            if (hitCount > bestHitCount)
            {
                bestHitCount = hitCount;
                bestTarget = potentialTarget;
            }
        }

        return (bestTarget, bestHitCount);
    }

    /// <summary>
    /// Invalidates the enemy cache. Call when targets may have changed significantly.
    /// </summary>
    public void InvalidateCache()
    {
        _cachedEnemies.Clear();
        _cacheTimer.Restart();
    }

    private IBattleNpc? FindEnemyByStrategy(EnemyTargetingStrategy strategy, float maxRange, IPlayerCharacter player)
    {
        return strategy switch
        {
            EnemyTargetingStrategy.LowestHp => FindLowestHpEnemy(maxRange, player),
            EnemyTargetingStrategy.HighestHp => FindHighestHpEnemy(maxRange, player),
            EnemyTargetingStrategy.Nearest => FindNearestEnemy(maxRange, player),
            EnemyTargetingStrategy.TankAssist => FindTankTarget(maxRange, player),
            EnemyTargetingStrategy.CurrentTarget => FindCurrentTarget(maxRange, player),
            EnemyTargetingStrategy.FocusTarget => FindFocusTarget(maxRange, player),
            _ => FindLowestHpEnemy(maxRange, player)
        };
    }

    private IBattleNpc? FindLowestHpEnemy(float maxRange, IPlayerCharacter player)
    {
        IBattleNpc? best = null;
        uint lowestHp = uint.MaxValue;

        foreach (var enemy in GetValidEnemies(maxRange, player))
        {
            if (enemy.CurrentHp < lowestHp)
            {
                lowestHp = enemy.CurrentHp;
                best = enemy;
            }
        }

        return best;
    }

    private IBattleNpc? FindHighestHpEnemy(float maxRange, IPlayerCharacter player)
    {
        IBattleNpc? best = null;
        uint highestHp = 0;

        foreach (var enemy in GetValidEnemies(maxRange, player))
        {
            if (enemy.CurrentHp > highestHp)
            {
                highestHp = enemy.CurrentHp;
                best = enemy;
            }
        }

        return best;
    }

    private IBattleNpc? FindNearestEnemy(float maxRange, IPlayerCharacter player)
    {
        IBattleNpc? best = null;
        float nearestDist = float.MaxValue;
        var playerPos = player.Position;

        foreach (var enemy in GetValidEnemies(maxRange, player))
        {
            var dist = Vector3.DistanceSquared(playerPos, enemy.Position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                best = enemy;
            }
        }

        return best;
    }

    private IBattleNpc? FindTankTarget(float maxRange, IPlayerCharacter player)
    {
        // Find tank in party and get their target
        foreach (var member in _partyList)
        {
            if (member.GameObject is not IBattleChara chara)
                continue;

            // Check if this party member is a tank
            if (!TankJobIds.Contains(chara.ClassJob.RowId))
                continue;

            // Get what the tank is targeting
            var targetId = chara.TargetObjectId;
            if (targetId is 0 or 0xE0000000)
                continue;

            var target = _objectTable.SearchById(targetId);
            if (target is IBattleNpc enemy && IsValidEnemy(enemy, maxRange, player))
                return enemy;
        }

        return null;
    }

    private IBattleNpc? FindCurrentTarget(float maxRange, IPlayerCharacter player)
    {
        var target = _targetManager.Target;
        if (target is IBattleNpc enemy && IsValidEnemy(enemy, maxRange, player))
            return enemy;

        return null;
    }

    private IBattleNpc? FindFocusTarget(float maxRange, IPlayerCharacter player)
    {
        var target = _targetManager.FocusTarget;
        if (target is IBattleNpc enemy && IsValidEnemy(enemy, maxRange, player))
            return enemy;

        return null;
    }

    /// <summary>
    /// Gets valid enemies in range, using cache when available.
    /// </summary>
    private IEnumerable<IBattleNpc> GetValidEnemies(float maxRange, IPlayerCharacter player)
    {
        // Check if cache is still valid
        var cacheAge = _cacheTimer.ElapsedMilliseconds;
        if (_cachedEnemies.Count > 0 &&
            cacheAge < _configuration.Targeting.TargetCacheTtlMs &&
            Math.Abs(_lastCacheRange - maxRange) < 0.1f)
        {
            // Validate cached entries are still valid (O(n) with RemoveAll vs O(n²) with RemoveAt)
            _cachedEnemies.RemoveAll(e => !IsStillValid(e));

            if (_cachedEnemies.Count > 0)
            {
                foreach (var enemy in _cachedEnemies)
                    yield return enemy;
                yield break;
            }
        }

        // Rebuild cache
        _cachedEnemies.Clear();
        _lastCacheRange = maxRange;
        _cacheTimer.Restart();

        var maxRangeSquared = maxRange * maxRange;
        var playerPos = player.Position;
        var maxRangeYalms = (byte)Math.Ceiling(maxRange);

        foreach (var obj in _objectTable)
        {
            // Cheapest checks first
            if (obj.ObjectKind != ObjectKind.BattleNpc)
                continue;

            if (!obj.IsTargetable)
                continue;

            if (obj.IsDead)
                continue;

            // Quick yalm-based range pre-filter (pre-calculated by game)
            if (obj.YalmDistanceX > maxRangeYalms)
                continue;

            // Type cast
            if (obj is not IBattleNpc npc)
                continue;

            // Check if hostile (enemy or striking dummy)
            if (npc.BattleNpcKind != BattleNpcSubKind.Enemy && npc.SubKind != 0)
                continue;

            // Precise distance check (most expensive)
            if (Vector3.DistanceSquared(playerPos, npc.Position) > maxRangeSquared)
                continue;

            _cachedEnemies.Add(npc);
            yield return npc;
        }
    }

    private static bool IsValidEnemy(IBattleNpc enemy, float maxRange, IPlayerCharacter player)
    {
        if (!enemy.IsTargetable || enemy.IsDead)
            return false;

        if (enemy.BattleNpcKind != BattleNpcSubKind.Enemy && enemy.SubKind != 0)
            return false;

        return DistanceHelper.IsInRange(player, enemy, maxRange);
    }

    private static bool IsStillValid(IBattleNpc enemy)
    {
        return enemy.IsTargetable && !enemy.IsDead;
    }

    private static float GetDotDuration(IBattleChara target, uint statusId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId)
                return status.RemainingTime;
        }
        return 0f;
    }
}
