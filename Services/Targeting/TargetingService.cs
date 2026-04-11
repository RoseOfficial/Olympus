using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Common.Component.BGCollision;
using Olympus.Data;
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

    // Reusable work list for AoE target methods (all called from game thread)
    private readonly List<IBattleNpc> _aoeWorkList = new();

    // Tank job IDs: PLD=19, WAR=21, DRK=32, GNB=37
    private static readonly HashSet<uint> TankJobIds = [19, 21, 32, 37];

    public IGapCloserSafetyService GapCloserSafety { get; }

    public TargetingService(
        IObjectTable objectTable,
        IPartyList partyList,
        ITargetManager targetManager,
        Configuration configuration,
        IGapCloserSafetyService gapCloserSafety)
    {
        _objectTable = objectTable;
        _partyList = partyList;
        _targetManager = targetManager;
        _configuration = configuration;
        GapCloserSafety = gapCloserSafety;
        _cacheTimer.Start();
    }

    /// <summary>
    /// Returns true when damage targeting should be suppressed because the player has
    /// dropped their target and <see cref="Config.TargetingConfig.PauseWhenNoTarget"/> is on.
    /// This is the primary safeguard for gaze mechanics and any moment the player wants
    /// Olympus to stop attacking — dropping the target is a hard pause signal.
    /// </summary>
    public bool IsDamageTargetingPaused()
    {
        return _configuration.Targeting.PauseWhenNoTarget && _targetManager.Target == null;
    }

    /// <inheritdoc />
    public IBattleNpc? GetUserEnemyTarget()
    {
        return _targetManager.Target is IBattleNpc enemy && IsStillValid(enemy) ? enemy : null;
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
        // Hard pause: player has no target and PauseWhenNoTarget is on. Covers gaze mechanics.
        if (IsDamageTargetingPaused())
            return null;

        // Try primary strategy
        var target = FindEnemyByStrategy(strategy, maxRange, player);

        // If TankAssist fails and fallback is enabled, try LowestHp
        if (target == null && strategy == EnemyTargetingStrategy.TankAssist && _configuration.Targeting.UseTankAssistFallback)
        {
            target = FindEnemyByStrategy(EnemyTargetingStrategy.LowestHp, maxRange, player);
        }

        // If CurrentTarget/FocusTarget fails, fall back to LowestHp — unless strict mode
        // is on, in which case an explicit-target strategy with no target stays empty
        // (prevents auto-retargeting when the player is trying to stop attacking)
        if (target == null && strategy is EnemyTargetingStrategy.CurrentTarget or EnemyTargetingStrategy.FocusTarget
            && !_configuration.Targeting.StrictCurrentTargetStrategy)
        {
            target = FindEnemyByStrategy(EnemyTargetingStrategy.LowestHp, maxRange, player);
        }

        return target;
    }

    /// <summary>
    /// Finds an enemy that needs DoT applied or refreshed. Respects the configured
    /// <see cref="EnemyTargetingStrategy"/> — on explicit strategies (CurrentTarget,
    /// FocusTarget) the DoT will never spill onto enemies the player did not pick.
    /// On aggregate strategies (LowestHp, HighestHp, Nearest, TankAssist) the DoT
    /// still goes to the strategy's chosen enemy, but only if that enemy actually
    /// needs the DoT applied or refreshed.
    /// </summary>
    /// <param name="dotStatusId">Status ID to check for (Aero/Dia variant).</param>
    /// <param name="refreshThreshold">Seconds remaining before DoT should be refreshed.</param>
    /// <param name="maxRange">Maximum range in yalms.</param>
    /// <param name="player">Current player character.</param>
    /// <returns>Enemy needing DoT under the current strategy, or null.</returns>
    public IBattleNpc? FindEnemyNeedingDot(
        uint dotStatusId,
        float refreshThreshold,
        float maxRange,
        IPlayerCharacter player)
    {
        // Hard pause: player has no target — don't DoT anything.
        if (IsDamageTargetingPaused())
            return null;

        var strategy = _configuration.Targeting.EnemyStrategy;

        // Explicit-target strategies: only consider the player's selected target/focus,
        // never spread DoT to unrelated enemies. This is the "smart DoT" safety fix —
        // hitting an add that isn't supposed to take damage (reflect, vulnerability down,
        // damage debuff) breaks fights, so honor player intent here.
        if (strategy is EnemyTargetingStrategy.CurrentTarget or EnemyTargetingStrategy.FocusTarget)
        {
            var explicitTarget = strategy == EnemyTargetingStrategy.CurrentTarget
                ? _targetManager.Target as IBattleNpc
                : _targetManager.FocusTarget as IBattleNpc;

            if (explicitTarget == null || !IsStillValid(explicitTarget))
                return null;

            if (!DistanceHelper.IsInRange(player.Position, explicitTarget.Position, maxRange + explicitTarget.HitboxRadius + player.HitboxRadius))
                return null;

            return GetDotDuration(explicitTarget, dotStatusId) < refreshThreshold ? explicitTarget : null;
        }

        // Aggregate strategies: pick the strategy's best enemy, but only DoT it if it
        // actually needs the DoT. This prevents the old behavior of scanning every
        // in-combat enemy and targeting whichever had the lowest DoT duration.
        IBattleNpc? strategyTarget = strategy switch
        {
            EnemyTargetingStrategy.TankAssist => FindEnemyByStrategy(strategy, maxRange, player),
            EnemyTargetingStrategy.Nearest => FindEnemyByStrategy(strategy, maxRange, player),
            EnemyTargetingStrategy.HighestHp => FindEnemyByStrategy(strategy, maxRange, player),
            _ => FindEnemyByStrategy(EnemyTargetingStrategy.LowestHp, maxRange, player)
        };

        if (strategyTarget == null)
            return null;

        return GetDotDuration(strategyTarget, dotStatusId) < refreshThreshold ? strategyTarget : null;
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
        // Hard pause: no target → report 0 enemies so AoE thresholds can't trigger.
        if (IsDamageTargetingPaused())
            return 0;

        int count = 0;
        var currentTargetId = _targetManager.Target is IBattleNpc ? _targetManager.Target.GameObjectId : 0UL;
        foreach (var enemy in GetValidEnemies(radius, player))
        {
            // Only count enemies in combat or explicitly targeted — avoids counting
            // non-engaged targets like unattacked dummies or non-pulled packs
            if ((enemy.StatusFlags & StatusFlags.InCombat) == 0 && enemy.GameObjectId != currentTargetId)
                continue;
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
        // Hard pause: no target.
        if (IsDamageTargetingPaused())
            return (null, 0);

        IBattleNpc? bestTarget = null;
        int bestHitCount = 0;

        // Collect all valid enemies in reusable work list
        _aoeWorkList.Clear();
        foreach (var enemy in GetValidEnemies(maxRange, player))
        {
            _aoeWorkList.Add(enemy);
        }

        if (_aoeWorkList.Count == 0)
            return (null, 0);

        // Early exit: if only 1 enemy, no need for O(n²) calculation
        if (_aoeWorkList.Count == 1)
            return (_aoeWorkList[0], 1);

        // For each potential target, count how many enemies would be hit
        var aoeRadiusSquared = aoeRadius * aoeRadius;
        foreach (var potentialTarget in _aoeWorkList)
        {
            int hitCount = 1; // Always hits the target itself

            // Count other enemies within AoE radius of this target
            foreach (var other in _aoeWorkList)
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

    /// <inheritdoc />
    public IBattleNpc? FindEnemyForAction(EnemyTargetingStrategy strategy, uint actionId, IPlayerCharacter player)
    {
        // Hard pause: no target → no damage targeting at all.
        if (IsDamageTargetingPaused())
            return null;

        var target = FindEnemyByActionStrategy(strategy, actionId, player);

        if (target == null && strategy == EnemyTargetingStrategy.TankAssist && _configuration.Targeting.UseTankAssistFallback)
            target = FindEnemyByActionStrategy(EnemyTargetingStrategy.LowestHp, actionId, player);

        // Fall back from explicit-target strategies to LowestHp only when strict mode
        // is off. Strict mode keeps explicit-target intent as a hard stop — important
        // for players who use CurrentTarget to manually control every engagement.
        if (target == null && strategy is EnemyTargetingStrategy.CurrentTarget or EnemyTargetingStrategy.FocusTarget
            && !_configuration.Targeting.StrictCurrentTargetStrategy)
            target = FindEnemyByActionStrategy(EnemyTargetingStrategy.LowestHp, actionId, player);

        return target;
    }

    /// <summary>
    /// Invalidates the enemy cache. Call when targets may have changed significantly.
    /// </summary>
    public void InvalidateCache()
    {
        _cachedEnemies.Clear();
        _cacheTimer.Restart();
    }

    private IBattleNpc? FindEnemyByActionStrategy(EnemyTargetingStrategy strategy, uint actionId, IPlayerCharacter player)
    {
        return strategy switch
        {
            EnemyTargetingStrategy.LowestHp => FindLowestHpEnemyForAction(actionId, player),
            EnemyTargetingStrategy.HighestHp => FindHighestHpEnemyForAction(actionId, player),
            EnemyTargetingStrategy.Nearest => FindNearestEnemyForAction(actionId, player),
            EnemyTargetingStrategy.TankAssist => FindTankTargetForAction(actionId, player),
            EnemyTargetingStrategy.CurrentTarget => FindCurrentTargetForAction(actionId, player),
            EnemyTargetingStrategy.FocusTarget => FindFocusTargetForAction(actionId, player),
            _ => FindLowestHpEnemyForAction(actionId, player)
        };
    }

    private IBattleNpc? FindLowestHpEnemyForAction(uint actionId, IPlayerCharacter player)
    {
        IBattleNpc? best = null;
        uint lowestHp = uint.MaxValue;
        foreach (var candidate in GetActionCandidates(player))
        {
            if (!IsActionInRange(actionId, player, candidate)) continue;
            if (candidate.CurrentHp < lowestHp)
            {
                lowestHp = candidate.CurrentHp;
                best = candidate;
            }
        }
        return best;
    }

    private IBattleNpc? FindHighestHpEnemyForAction(uint actionId, IPlayerCharacter player)
    {
        IBattleNpc? best = null;
        uint highestHp = 0;
        foreach (var candidate in GetActionCandidates(player))
        {
            if (!IsActionInRange(actionId, player, candidate)) continue;
            if (candidate.CurrentHp > highestHp)
            {
                highestHp = candidate.CurrentHp;
                best = candidate;
            }
        }
        return best;
    }

    private IBattleNpc? FindNearestEnemyForAction(uint actionId, IPlayerCharacter player)
    {
        IBattleNpc? best = null;
        float nearestDist = float.MaxValue;
        var playerPos = player.Position;
        foreach (var candidate in GetActionCandidates(player))
        {
            if (!IsActionInRange(actionId, player, candidate)) continue;
            var dist = Vector3.DistanceSquared(playerPos, candidate.Position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                best = candidate;
            }
        }
        return best;
    }

    private IBattleNpc? FindTankTargetForAction(uint actionId, IPlayerCharacter player)
    {
        foreach (var member in _partyList)
        {
            if (member.GameObject is not IBattleChara chara) continue;
            if (!TankJobIds.Contains(chara.ClassJob.RowId)) continue;
            var targetId = chara.TargetObjectId;
            if (targetId is 0 or 0xE0000000) continue;
            var target = _objectTable.SearchById(targetId);
            if (target is IBattleNpc enemy && IsStillValid(enemy) && IsActionInRange(actionId, player, enemy))
                return enemy;
        }
        return null;
    }

    private IBattleNpc? FindCurrentTargetForAction(uint actionId, IPlayerCharacter player)
    {
        var target = _targetManager.Target;
        if (target is IBattleNpc enemy && IsStillValid(enemy) && IsActionInRange(actionId, player, enemy))
            return enemy;
        return null;
    }

    private IBattleNpc? FindFocusTargetForAction(uint actionId, IPlayerCharacter player)
    {
        var target = _targetManager.FocusTarget;
        if (target is IBattleNpc enemy && IsStillValid(enemy) && IsActionInRange(actionId, player, enemy))
            return enemy;
        return null;
    }

    /// <summary>
    /// Iterates all nearby battle NPCs as candidates for action-based range checks.
    /// Uses a generous 15y pre-filter (safe for any 3y melee action including large boss hitboxes).
    /// </summary>
    private IEnumerable<IBattleNpc> GetActionCandidates(IPlayerCharacter player)
    {
        foreach (var obj in _objectTable)
        {
            if (obj.ObjectKind != ObjectKind.BattleNpc) continue;
            if (!obj.IsTargetable) continue;
            if (obj.IsDead) continue;
            if (obj.YalmDistanceX > 15) continue;
            if (obj is not IBattleNpc npc) continue;
            if (npc.BattleNpcKind != BattleNpcSubKind.Enemy && npc.SubKind != 0) continue;
            if (_configuration.Targeting.EnableInvulnerabilityFiltering &&
                HasInvulnerabilityStatus(npc))
                continue;
            yield return npc;
        }
    }

    /// <summary>
    /// Uses the game's native GetActionInRangeOrLoS to check if an action can reach a target.
    /// Returns true for result 0 (in range + LoS) or 565 (in range but facing wrong way).
    /// </summary>
    private static unsafe bool IsActionInRange(uint actionId, IGameObject player, IGameObject target)
    {
        try
        {
            var playerStruct = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)player.Address;
            var targetStruct = (FFXIVClientStructs.FFXIV.Client.Game.Object.GameObject*)target.Address;
            if (playerStruct == null || targetStruct == null) return false;
            var result = ActionManager.GetActionInRangeOrLoS(actionId, playerStruct, targetStruct);
            return result is 0 or 565; // 0=in range+LoS, 565=in range but facing wrong way
        }
        catch
        {
            return false;
        }
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
        var currentTargetId = _targetManager.Target is IBattleNpc ? _targetManager.Target.GameObjectId : 0UL;

        foreach (var enemy in GetValidEnemies(maxRange, player))
        {
            // Only consider enemies in combat or explicitly targeted by the player —
            // avoids targeting non-engaged enemies like unattacked dummies or non-pulled packs
            if ((enemy.StatusFlags & StatusFlags.InCombat) == 0 && enemy.GameObjectId != currentTargetId)
                continue;

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
        var currentTargetId = _targetManager.Target is IBattleNpc ? _targetManager.Target.GameObjectId : 0UL;

        foreach (var enemy in GetValidEnemies(maxRange, player))
        {
            if ((enemy.StatusFlags & StatusFlags.InCombat) == 0 && enemy.GameObjectId != currentTargetId)
                continue;

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
        var currentTargetId = _targetManager.Target is IBattleNpc ? _targetManager.Target.GameObjectId : 0UL;

        foreach (var enemy in GetValidEnemies(maxRange, player))
        {
            if ((enemy.StatusFlags & StatusFlags.InCombat) == 0 && enemy.GameObjectId != currentTargetId)
                continue;

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

            // Quick yalm-based range pre-filter (generous buffer for large hitboxes)
            if (obj.YalmDistanceX > maxRangeYalms + (int)Math.Ceiling(obj.HitboxRadius))
                continue;

            // Type cast
            if (obj is not IBattleNpc npc)
                continue;

            // Check if hostile (enemy or striking dummy)
            if (npc.BattleNpcKind != BattleNpcSubKind.Enemy && npc.SubKind != 0)
                continue;

            // Precise distance check — effective range includes both hitbox radii
            var effectiveRange = maxRange + npc.HitboxRadius + player.HitboxRadius;
            if (Vector3.DistanceSquared(playerPos, npc.Position) > effectiveRange * effectiveRange)
                continue;

            // Line-of-sight check — reject enemies behind walls/pillars
            if (_configuration.Targeting.EnableLineOfSightFiltering &&
                !HasLineOfSight(playerPos, npc.Position))
                continue;

            // Invulnerability check — skip enemies with known invuln status effects
            // (boss phase transitions, invulnerable adds, untouchable objects).
            // Only applied to auto-targeting; explicit CurrentTarget/FocusTarget bypass this.
            if (_configuration.Targeting.EnableInvulnerabilityFiltering &&
                HasInvulnerabilityStatus(npc))
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

        return DistanceHelper.IsInRange(player.Position, enemy.Position, maxRange + enemy.HitboxRadius + player.HitboxRadius);
    }

    private static bool IsStillValid(IBattleNpc enemy)
    {
        return enemy.IsTargetable && !enemy.IsDead;
    }

    /// <summary>
    /// Checks line of sight from the player's approximate eye height to an enemy position
    /// using a BGCollision raycast. Returns false if geometry blocks the path.
    /// </summary>
    private static unsafe bool HasLineOfSight(Vector3 playerPos, Vector3 enemyPos)
    {
        try
        {
            var eyePos = playerPos with { Y = playerPos.Y + 2f };
            var direction = enemyPos - eyePos;
            var distance = direction.Length();
            if (distance < 0.01f)
                return true;

            direction /= distance;
            return !BGCollisionModule.RaycastMaterialFilter(eyePos, direction, out _, distance);
        }
        catch
        {
            // BGCollision unavailable (loading screen, etc.) — assume LoS is fine
            return true;
        }
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

    /// <summary>
    /// Checks whether an enemy has a known invulnerability status effect.
    /// Used to skip immune targets during auto-targeting (boss phase transitions,
    /// invulnerable adds, untouchable objects like ARR crystals).
    /// </summary>
    private static bool HasInvulnerabilityStatus(IBattleNpc npc)
    {
        if (npc.StatusList == null)
            return false;

        foreach (var status in npc.StatusList)
        {
            if (FFXIVConstants.EnemyInvulnerabilityStatusIds.Contains(status.StatusId))
                return true;
        }

        return false;
    }

    // ── Cone / Line AoE Targeting ──

    /// <inheritdoc />
    public (IBattleNpc? target, int hitCount, float optimalAngle) FindBestConeAoETarget(
        float coneHalfAngle, float radius, float maxRange, IPlayerCharacter player)
    {
        if (IsDamageTargetingPaused())
            return (null, 0, 0f);

        // Use the ability's effect range for candidate filtering, not the rotation's targeting range
        var candidateRange = MathF.Max(radius, maxRange);
        _aoeWorkList.Clear();
        foreach (var e in GetValidEnemies(candidateRange, player))
            _aoeWorkList.Add(e);

        if (_aoeWorkList.Count == 0) return (null, 0, 0f);
        if (_aoeWorkList.Count == 1)
        {
            var dx = _aoeWorkList[0].Position.X - player.Position.X;
            var dz = _aoeWorkList[0].Position.Z - player.Position.Z;
            return (_aoeWorkList[0], 1, MathF.Atan2(dx, dz));
        }

        int bestCount = 0;
        float bestAngle = 0f;
        IBattleNpc? bestTarget = null;
        var playerPos = player.Position;

        // For each enemy as potential target: aim at them and count how many others
        // the cone/line would clip. We can only face enemies we target (game auto-faces).
        foreach (var candidate in _aoeWorkList)
        {
            var dx = candidate.Position.X - playerPos.X;
            var dz = candidate.Position.Z - playerPos.Z;
            var aimAngle = MathF.Atan2(dx, dz);

            var count = 0;
            foreach (var e in _aoeWorkList)
            {
                var edx = e.Position.X - playerPos.X;
                var edz = e.Position.Z - playerPos.Z;
                var dist = MathF.Sqrt(edx * edx + edz * edz);
                if (dist - e.HitboxRadius > radius) continue;

                var angleToE = MathF.Atan2(edx, edz);
                var diff = NormalizeAngle(angleToE - aimAngle);
                if (MathF.Abs(diff) <= coneHalfAngle)
                    count++;
            }

            if (count > bestCount)
            {
                bestCount = count;
                bestAngle = aimAngle;
                bestTarget = candidate;
            }
        }

        return (bestTarget, bestCount, bestAngle);
    }

    /// <inheritdoc />
    public (IBattleNpc? target, int hitCount, float optimalAngle) FindBestLineAoETarget(
        float lineWidth, float length, float maxRange, IPlayerCharacter player)
    {
        if (IsDamageTargetingPaused())
            return (null, 0, 0f);

        // Use the ability's effect range for candidate filtering, not the rotation's targeting range
        var candidateRange = MathF.Max(length, maxRange);
        _aoeWorkList.Clear();
        foreach (var e in GetValidEnemies(candidateRange, player))
            _aoeWorkList.Add(e);

        if (_aoeWorkList.Count == 0) return (null, 0, 0f);
        if (_aoeWorkList.Count == 1)
        {
            var dx = _aoeWorkList[0].Position.X - player.Position.X;
            var dz = _aoeWorkList[0].Position.Z - player.Position.Z;
            return (_aoeWorkList[0], 1, MathF.Atan2(dx, dz));
        }

        int bestCount = 0;
        float bestAngle = 0f;
        IBattleNpc? bestTarget = null;
        var playerPos = player.Position;
        var halfWidth = lineWidth * 0.5f;

        // For each enemy as potential target: aim at them and count how many others
        // the line would clip. We can only face enemies we target.
        foreach (var candidate in _aoeWorkList)
        {
            var dx = candidate.Position.X - playerPos.X;
            var dz = candidate.Position.Z - playerPos.Z;
            var aimAngle = MathF.Atan2(dx, dz);
            var sinH = MathF.Sin(aimAngle);
            var cosH = MathF.Cos(aimAngle);

            var count = 0;
            foreach (var e in _aoeWorkList)
            {
                var edx = e.Position.X - playerPos.X;
                var edz = e.Position.Z - playerPos.Z;

                var forward = edx * sinH + edz * cosH;
                var lateral = edx * cosH - edz * sinH;

                if (forward >= -e.HitboxRadius
                    && forward <= length + e.HitboxRadius
                    && MathF.Abs(lateral) <= halfWidth + e.HitboxRadius)
                    count++;
            }

            if (count > bestCount)
            {
                bestCount = count;
                bestAngle = aimAngle;
                bestTarget = candidate;
            }
        }

        return (bestTarget, bestCount, bestAngle);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI) angle -= 2f * MathF.PI;
        while (angle < -MathF.PI) angle += 2f * MathF.PI;
        return angle;
    }
}
