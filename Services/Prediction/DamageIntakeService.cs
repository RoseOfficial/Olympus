using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Olympus.Services.Prediction;

/// <summary>
/// Tracks damage intake per entity over a rolling time window.
/// Used to identify party members taking active damage for intelligent healing triage.
/// </summary>
public sealed class DamageIntakeService : IDamageIntakeService, IDisposable
{
    private readonly ICombatEventService _combatEventService;

    /// <summary>
    /// Represents a single damage entry with its amount and timestamp.
    /// </summary>
    private record DamageEntry(int Amount, DateTime Timestamp);

    // Damage records: entityId -> list of damage entries
    private readonly ConcurrentDictionary<uint, List<DamageEntry>> _damageByEntity = new();
    private readonly object _damageLock = new();

    // Default window for damage tracking
    private const float DefaultWindowSeconds = 5f;

    // Maximum entries to keep per entity (prevents unbounded growth)
    private const int MaxEntriesPerEntity = 100;

    public DamageIntakeService(ICombatEventService combatEventService)
    {
        _combatEventService = combatEventService;

        // Subscribe to damage received event
        _combatEventService.OnDamageReceived += OnDamageReceived;
    }

    public void Dispose()
    {
        _combatEventService.OnDamageReceived -= OnDamageReceived;
    }

    /// <summary>
    /// Called when damage is received by any entity.
    /// </summary>
    private void OnDamageReceived(uint entityId, int amount)
    {
        RecordDamage(entityId, amount);
    }

    /// <summary>
    /// Records damage received by an entity.
    /// </summary>
    public void RecordDamage(uint entityId, int amount)
    {
        if (amount <= 0)
            return;

        var entry = new DamageEntry(amount, DateTime.UtcNow);

        lock (_damageLock)
        {
            if (!_damageByEntity.TryGetValue(entityId, out var list))
            {
                list = new List<DamageEntry>();
                _damageByEntity[entityId] = list;
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
    /// Gets the total damage received by an entity within the specified time window.
    /// </summary>
    public int GetRecentDamageIntake(uint entityId, float windowSeconds = DefaultWindowSeconds)
    {
        lock (_damageLock)
        {
            if (!_damageByEntity.TryGetValue(entityId, out var list))
                return 0;

            var cutoff = DateTime.UtcNow.AddSeconds(-windowSeconds);
            return list
                .Where(d => d.Timestamp >= cutoff)
                .Sum(d => d.Amount);
        }
    }

    /// <summary>
    /// Gets the damage rate (damage per second) for an entity within the specified time window.
    /// </summary>
    public float GetDamageRate(uint entityId, float windowSeconds = DefaultWindowSeconds)
    {
        if (windowSeconds <= 0)
            return 0f;

        var totalDamage = GetRecentDamageIntake(entityId, windowSeconds);
        return totalDamage / windowSeconds;
    }

    /// <summary>
    /// Gets the total party-wide damage intake within the specified time window.
    /// </summary>
    public int GetPartyDamageIntake(float windowSeconds = DefaultWindowSeconds)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-windowSeconds);

        lock (_damageLock)
        {
            var total = 0;
            foreach (var kvp in _damageByEntity)
            {
                total += kvp.Value
                    .Where(d => d.Timestamp >= cutoff)
                    .Sum(d => d.Amount);
            }
            return total;
        }
    }

    /// <summary>
    /// Gets the party-wide damage rate (damage per second).
    /// </summary>
    public float GetPartyDamageRate(float windowSeconds = DefaultWindowSeconds)
    {
        if (windowSeconds <= 0)
            return 0f;

        var totalDamage = GetPartyDamageIntake(windowSeconds);
        return totalDamage / windowSeconds;
    }

    /// <summary>
    /// Clears all tracked damage records. Call on zone transitions.
    /// </summary>
    public void Clear()
    {
        lock (_damageLock)
        {
            _damageByEntity.Clear();
        }
    }

    /// <summary>
    /// Clears damage records for a specific entity.
    /// </summary>
    public void ClearEntity(uint entityId)
    {
        lock (_damageLock)
        {
            _damageByEntity.TryRemove(entityId, out _);
        }
    }

    /// <summary>
    /// Cleans up expired entries across all entities.
    /// Called periodically to prevent memory growth.
    /// </summary>
    internal void CleanupExpiredEntries(float maxAgeSeconds = 30f)
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-maxAgeSeconds);

        lock (_damageLock)
        {
            foreach (var kvp in _damageByEntity)
            {
                kvp.Value.RemoveAll(d => d.Timestamp < cutoff);
            }

            // Remove empty lists
            var emptyKeys = _damageByEntity
                .Where(kvp => kvp.Value.Count == 0)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in emptyKeys)
            {
                _damageByEntity.TryRemove(key, out _);
            }
        }
    }
}
