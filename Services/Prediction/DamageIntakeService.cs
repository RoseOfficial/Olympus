using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Olympus.Services.Prediction;

/// <summary>
/// Tracks damage intake per entity over a rolling time window.
/// Used to identify party members taking active damage for intelligent healing triage.
/// Supports predictive damage forecasting using historical rates, boss mechanics, and DoTs.
/// </summary>
public sealed class DamageIntakeService : IDamageIntakeService, IDisposable
{
    private readonly ICombatEventService _combatEventService;

    /// <summary>
    /// Represents a single damage entry with its amount and timestamp.
    /// </summary>
    private record DamageEntry(int Amount, DateTime Timestamp);

    /// <summary>
    /// Represents an active DoT/bleed effect.
    /// </summary>
    private record ActiveDoT(int DamagePerTick, DateTime ExpiresAt);

    // Damage records: entityId -> list of damage entries
    private readonly ConcurrentDictionary<uint, List<DamageEntry>> _damageByEntity = new();
    private readonly object _damageLock = new();

    // Active DoT tracking: entityId -> list of active DoTs
    private readonly ConcurrentDictionary<uint, List<ActiveDoT>> _activeDoTs = new();
    private readonly object _dotLock = new();

    // Boss mechanic detector for predictive damage
    private IBossMechanicDetector? _bossMechanicDetector;

    // Default window for damage tracking
    private const float DefaultWindowSeconds = 5f;

    // Maximum entries to keep per entity (prevents unbounded growth)
    private const int MaxEntriesPerEntity = 100;

    // Server tick rate for DoT damage (3 seconds)
    private const float DoTTickInterval = 3.0f;

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

        // Also cleanup expired DoTs
        CleanupExpiredDoTs();
    }

    /// <summary>
    /// Sets the boss mechanic detector for predictive damage forecasting.
    /// </summary>
    public void SetBossMechanicDetector(IBossMechanicDetector detector)
    {
        _bossMechanicDetector = detector;
    }

    /// <summary>
    /// Registers an active DoT or bleed effect on an entity.
    /// </summary>
    public void RegisterActiveDoT(uint entityId, int damagePerTick, float remainingDuration)
    {
        if (damagePerTick <= 0 || remainingDuration <= 0)
            return;

        var dot = new ActiveDoT(damagePerTick, DateTime.UtcNow.AddSeconds(remainingDuration));

        lock (_dotLock)
        {
            if (!_activeDoTs.TryGetValue(entityId, out var list))
            {
                list = new List<ActiveDoT>();
                _activeDoTs[entityId] = list;
            }

            list.Add(dot);
        }
    }

    /// <summary>
    /// Clears all active DoT tracking for an entity.
    /// </summary>
    public void ClearActiveDoTs(uint entityId)
    {
        lock (_dotLock)
        {
            _activeDoTs.TryRemove(entityId, out _);
        }
    }

    /// <summary>
    /// Forecasts total party damage expected within the specified time window.
    /// Combines historical damage rate, predicted mechanics, and active DoTs.
    /// </summary>
    public int ForecastPartyDamage(float forecastSeconds = DefaultWindowSeconds)
    {
        var totalForecast = 0;

        // Sum forecasted damage for all tracked entities
        lock (_damageLock)
        {
            foreach (var entityId in _damageByEntity.Keys)
            {
                totalForecast += ForecastEntityDamage(entityId, forecastSeconds);
            }
        }

        // Add predicted raidwide damage if imminent
        if (_bossMechanicDetector?.PredictedRaidwide is { } raidwide &&
            raidwide.SecondsUntil <= forecastSeconds)
        {
            // Estimate raidwide damage (assume 8 party members average)
            // This is a rough estimate; actual implementation would use party list
            var raidwideDamage = (int)(raidwide.EstimatedDamagePercent * 100000 * 8);
            totalForecast += raidwideDamage;
        }

        return totalForecast;
    }

    /// <summary>
    /// Forecasts damage expected for a specific entity within the time window.
    /// </summary>
    public int ForecastEntityDamage(uint entityId, float forecastSeconds = DefaultWindowSeconds)
    {
        var forecast = 0;

        // Base forecast on historical damage rate
        var historicalRate = GetDamageRate(entityId, DefaultWindowSeconds);
        forecast += (int)(historicalRate * forecastSeconds);

        // Add DoT damage forecast
        forecast += ForecastDoTDamage(entityId, forecastSeconds);

        // Add tank buster damage if this entity is the target
        if (_bossMechanicDetector?.PredictedTankBuster is { } tankBuster &&
            tankBuster.TargetTankEntityId == entityId &&
            tankBuster.SecondsUntil <= forecastSeconds)
        {
            forecast += tankBuster.EstimatedDamage;
        }

        return forecast;
    }

    /// <summary>
    /// Gets forecasted damage for a specific entity as a percentage of their max HP.
    /// </summary>
    public float ForecastDamagePercent(uint entityId, int maxHp, float forecastSeconds = DefaultWindowSeconds)
    {
        if (maxHp <= 0)
            return 0f;

        var forecastedDamage = ForecastEntityDamage(entityId, forecastSeconds);

        // Add raidwide damage if predicted (affects all entities)
        if (_bossMechanicDetector?.PredictedRaidwide is { } raidwide &&
            raidwide.SecondsUntil <= forecastSeconds)
        {
            forecastedDamage += (int)(raidwide.EstimatedDamagePercent * maxHp);
        }

        return (float)forecastedDamage / maxHp;
    }

    /// <summary>
    /// Calculates expected DoT damage for an entity over the forecast window.
    /// </summary>
    private int ForecastDoTDamage(uint entityId, float forecastSeconds)
    {
        var now = DateTime.UtcNow;
        var forecastEnd = now.AddSeconds(forecastSeconds);
        var totalDotDamage = 0;

        lock (_dotLock)
        {
            if (!_activeDoTs.TryGetValue(entityId, out var dots))
                return 0;

            foreach (var dot in dots)
            {
                // Only count DoTs that haven't expired
                if (dot.ExpiresAt <= now)
                    continue;

                // Calculate how many ticks will occur in the forecast window
                var effectiveEnd = dot.ExpiresAt < forecastEnd ? dot.ExpiresAt : forecastEnd;
                var effectiveDuration = (float)(effectiveEnd - now).TotalSeconds;
                var ticksInWindow = (int)(effectiveDuration / DoTTickInterval);

                totalDotDamage += dot.DamagePerTick * Math.Max(1, ticksInWindow);
            }
        }

        return totalDotDamage;
    }

    /// <summary>
    /// Removes expired DoT entries.
    /// </summary>
    private void CleanupExpiredDoTs()
    {
        var now = DateTime.UtcNow;

        lock (_dotLock)
        {
            foreach (var kvp in _activeDoTs)
            {
                kvp.Value.RemoveAll(d => d.ExpiresAt <= now);
            }

            // Remove empty lists
            var emptyKeys = _activeDoTs
                .Where(kvp => kvp.Value.Count == 0)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var key in emptyKeys)
            {
                _activeDoTs.TryRemove(key, out _);
            }
        }
    }
}
