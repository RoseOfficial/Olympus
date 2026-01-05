using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Olympus.Data;
using Olympus.Services;

namespace Olympus.Services.Prediction;

/// <summary>
/// HP prediction service that tracks multiple concurrent pending heals.
/// Prevents double-healing by making targets appear "healed" immediately after action execution.
/// Heals are cleared per-target when the actual heal effect lands.
/// </summary>
public sealed class HpPredictionService : IHpPredictionService, IDisposable
{
    private readonly ICombatEventService _combatEventService;

    /// <summary>
    /// Represents a single pending heal entry with its amount and registration time.
    /// </summary>
    private record PendingHealEntry(int Amount, DateTime RegisteredTime);

    // Pending heals: targetId → list of pending heal entries
    // Supports multiple concurrent heals per target (e.g., GCD + oGCD weaving)
    private readonly ConcurrentDictionary<uint, List<PendingHealEntry>> _pendingHealsByTarget = new();
    private readonly object _healsLock = new();

    public HpPredictionService(ICombatEventService combatEventService)
    {
        _combatEventService = combatEventService;

        // Subscribe to heal landed event to clear pending heals for that target
        _combatEventService.OnLocalPlayerHealLanded += OnHealLanded;
    }

    public void Dispose()
    {
        _combatEventService.OnLocalPlayerHealLanded -= OnHealLanded;
    }

    /// <summary>
    /// Called when a heal from the local player lands on a target.
    /// Clears all pending heals for that specific target.
    /// </summary>
    private void OnHealLanded(uint targetId)
    {
        ClearPendingHeals(targetId);
    }

    /// <summary>
    /// Gets predicted HP for an entity (shadow HP + all pending heals).
    /// </summary>
    public uint GetPredictedHp(uint entityId, uint currentHp, uint maxHp)
    {
        // Start with shadow HP
        var baseHp = (int)_combatEventService.GetShadowHp(entityId, currentHp);
        var totalPendingHeal = 0;

        lock (_healsLock)
        {
            if (_pendingHealsByTarget.TryGetValue(entityId, out var heals))
            {
                var now = DateTime.UtcNow;
                foreach (var heal in heals)
                {
                    // Only count non-expired heals
                    if ((now - heal.RegisteredTime).TotalSeconds <= FFXIVTimings.HpPredictionTimeoutSeconds)
                    {
                        totalPendingHeal += heal.Amount;
                    }
                }
            }
        }

        return (uint)Math.Clamp(baseHp + totalPendingHeal, 0, (int)maxHp);
    }

    /// <summary>
    /// Gets predicted HP percent for an entity.
    /// </summary>
    public float GetPredictedHpPercent(uint entityId, uint currentHp, uint maxHp)
    {
        if (maxHp == 0) return 0f;
        return (float)GetPredictedHp(entityId, currentHp, maxHp) / maxHp;
    }

    /// <summary>
    /// Register a pending single-target heal.
    /// Call this immediately BEFORE executing the heal action.
    /// Multiple heals can be registered for the same target (they accumulate).
    /// </summary>
    public void RegisterPendingHeal(uint targetId, int amount)
    {
        var entry = new PendingHealEntry(amount, DateTime.UtcNow);

        lock (_healsLock)
        {
            if (!_pendingHealsByTarget.TryGetValue(targetId, out var list))
            {
                list = new List<PendingHealEntry>();
                _pendingHealsByTarget[targetId] = list;
            }
            list.Add(entry);
        }
    }

    /// <summary>
    /// Register pending AoE heals for multiple targets.
    /// Call this immediately BEFORE executing the AoE heal action.
    /// Multiple heals can be registered for the same targets (they accumulate).
    /// </summary>
    public void RegisterPendingAoEHeal(IEnumerable<uint> targetIds, int amountPerTarget)
    {
        var now = DateTime.UtcNow;

        lock (_healsLock)
        {
            foreach (var targetId in targetIds)
            {
                var entry = new PendingHealEntry(amountPerTarget, now);

                if (!_pendingHealsByTarget.TryGetValue(targetId, out var list))
                {
                    list = new List<PendingHealEntry>();
                    _pendingHealsByTarget[targetId] = list;
                }
                list.Add(entry);
            }
        }
    }

    /// <summary>
    /// Clear all pending heals for all targets.
    /// </summary>
    public void ClearPendingHeals()
    {
        lock (_healsLock)
        {
            _pendingHealsByTarget.Clear();
        }
    }

    /// <summary>
    /// Clear pending heals for a specific target.
    /// Called when a heal lands on that target.
    /// </summary>
    public void ClearPendingHeals(uint targetId)
    {
        lock (_healsLock)
        {
            _pendingHealsByTarget.TryRemove(targetId, out _);
        }
    }

    /// <summary>
    /// Check if there are any pending heals for any target.
    /// </summary>
    public bool HasPendingHeals
    {
        get
        {
            lock (_healsLock)
            {
                return _pendingHealsByTarget.Any(kvp => kvp.Value.Count > 0);
            }
        }
    }

    /// <summary>
    /// Get total pending heal amount for a specific target (for debugging).
    /// Returns the sum of all non-expired pending heals.
    /// </summary>
    public int GetPendingHealAmount(uint targetId)
    {
        lock (_healsLock)
        {
            if (!_pendingHealsByTarget.TryGetValue(targetId, out var heals))
                return 0;

            var now = DateTime.UtcNow;
            return heals
                .Where(h => (now - h.RegisteredTime).TotalSeconds <= FFXIVTimings.HpPredictionTimeoutSeconds)
                .Sum(h => h.Amount);
        }
    }

    /// <summary>
    /// Get all pending heals (for debugging).
    /// Returns a dictionary of targetId → total pending heal amount.
    /// </summary>
    public IReadOnlyDictionary<uint, int> GetAllPendingHeals()
    {
        lock (_healsLock)
        {
            var now = DateTime.UtcNow;
            var result = new Dictionary<uint, int>();

            foreach (var kvp in _pendingHealsByTarget)
            {
                var total = kvp.Value
                    .Where(h => (now - h.RegisteredTime).TotalSeconds <= FFXIVTimings.HpPredictionTimeoutSeconds)
                    .Sum(h => h.Amount);

                if (total > 0)
                {
                    result[kvp.Key] = total;
                }
            }

            return result;
        }
    }
}
