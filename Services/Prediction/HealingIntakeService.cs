using System;
using System.Collections.Generic;

namespace Olympus.Services.Prediction;

/// <summary>
/// Tracks healing intake per entity over a rolling time window.
/// Used in conjunction with DamageIntakeService to calculate net HP changes for trend analysis.
/// </summary>
public sealed class HealingIntakeService : IHealingIntakeService, IDisposable
{
    private readonly ICombatEventService _combatEventService;

    /// <summary>
    /// Represents a single healing entry with its amount and timestamp.
    /// </summary>
    private record HealingEntry(int Amount, DateTime Timestamp);

    // Healing records: entityId -> list of healing entries
    private readonly Dictionary<uint, List<HealingEntry>> _healingByEntity = new();
    private readonly object _healingLock = new();

    // Default window for healing tracking
    private const float DefaultWindowSeconds = 5f;

    // Maximum entries to keep per entity (prevents unbounded growth)
    private const int MaxEntriesPerEntity = 100;

    // Reusable buffer for cleanup to avoid per-call allocations
    private readonly List<uint> _cleanupKeyBuffer = new();

    public HealingIntakeService(ICombatEventService combatEventService)
    {
        _combatEventService = combatEventService;

        // Subscribe to healing received event (from any source)
        _combatEventService.OnAnyHealReceived += OnHealingReceived;
    }

    public void Dispose()
    {
        _combatEventService.OnAnyHealReceived -= OnHealingReceived;
    }

    /// <summary>
    /// Called when healing is received by any entity.
    /// </summary>
    private void OnHealingReceived(uint healerEntityId, uint targetEntityId, int amount)
    {
        RecordHealing(targetEntityId, amount);
    }

    /// <summary>
    /// Records healing received by an entity.
    /// </summary>
    public void RecordHealing(uint entityId, int amount)
    {
        if (amount <= 0)
            return;

        var entry = new HealingEntry(amount, DateTime.UtcNow);

        lock (_healingLock)
        {
            if (!_healingByEntity.TryGetValue(entityId, out var list))
            {
                list = new List<HealingEntry>();
                _healingByEntity[entityId] = list;
            }

            list.Add(entry);

            // Prune old entries to prevent unbounded growth
            if (list.Count > MaxEntriesPerEntity)
            {
                // Remove oldest entries beyond max
                list.RemoveRange(0, list.Count - MaxEntriesPerEntity);
            }
        }
    }

    /// <summary>
    /// Gets the total healing received by an entity within the specified time window.
    /// </summary>
    public int GetRecentHealingIntake(uint entityId, float windowSeconds = DefaultWindowSeconds)
    {
        lock (_healingLock)
        {
            if (!_healingByEntity.TryGetValue(entityId, out var list))
                return 0;

            var cutoff = DateTime.UtcNow.AddSeconds(-windowSeconds);
            var total = 0;
            foreach (var h in list)
            {
                if (h.Timestamp >= cutoff)
                    total += h.Amount;
            }
            return total;
        }
    }

    /// <summary>
    /// Gets the healing rate (healing per second) for an entity within the specified time window.
    /// </summary>
    public float GetHealingRate(uint entityId, float windowSeconds = DefaultWindowSeconds)
    {
        if (windowSeconds <= 0)
            return 0f;

        var totalHealing = GetRecentHealingIntake(entityId, windowSeconds);
        return totalHealing / windowSeconds;
    }

    /// <summary>
    /// Gets the total party-wide healing intake within the specified time window.
    /// </summary>
    public int GetPartyHealingIntake(float windowSeconds = DefaultWindowSeconds)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-windowSeconds);

        lock (_healingLock)
        {
            var total = 0;
            foreach (var kvp in _healingByEntity)
            {
                foreach (var h in kvp.Value)
                {
                    if (h.Timestamp >= cutoff)
                        total += h.Amount;
                }
            }
            return total;
        }
    }

    /// <summary>
    /// Gets the party-wide healing rate (healing per second).
    /// </summary>
    public float GetPartyHealingRate(float windowSeconds = DefaultWindowSeconds)
    {
        if (windowSeconds <= 0)
            return 0f;

        var totalHealing = GetPartyHealingIntake(windowSeconds);
        return totalHealing / windowSeconds;
    }

    /// <summary>
    /// Clears all tracked healing records. Call on zone transitions.
    /// </summary>
    public void Clear()
    {
        lock (_healingLock)
        {
            _healingByEntity.Clear();
        }
    }

    /// <summary>
    /// Clears healing records for a specific entity.
    /// </summary>
    public void ClearEntity(uint entityId)
    {
        lock (_healingLock)
        {
            _healingByEntity.Remove(entityId);
        }
    }

    /// <summary>
    /// Cleans up expired entries across all entities.
    /// Called periodically to prevent memory growth.
    /// </summary>
    internal void CleanupExpiredEntries(float maxAgeSeconds = 30f)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-maxAgeSeconds);

        lock (_healingLock)
        {
            foreach (var kvp in _healingByEntity)
            {
                kvp.Value.RemoveAll(h => h.Timestamp < cutoff);
            }

            // Remove empty lists
            _cleanupKeyBuffer.Clear();
            foreach (var kvp in _healingByEntity)
            {
                if (kvp.Value.Count == 0)
                    _cleanupKeyBuffer.Add(kvp.Key);
            }
            foreach (var key in _cleanupKeyBuffer)
            {
                _healingByEntity.Remove(key);
            }
        }
    }
}
