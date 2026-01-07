using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Olympus.Data;
using Olympus.Services;
using Olympus.Services.Party;

namespace Olympus.Services.Prediction;

// Configuration forward declaration - actual class is in Olympus namespace

/// <summary>
/// HP prediction service that tracks multiple concurrent pending heals.
/// Prevents double-healing by making targets appear "healed" immediately after action execution.
/// Heals are cleared per-target when the actual heal effect lands.
/// Now includes shield awareness and damage forecasting for comprehensive survivability prediction.
/// </summary>
public sealed class HpPredictionService : IHpPredictionService, IDisposable
{
    private readonly ICombatEventService _combatEventService;
    private readonly Configuration _configuration;
    private readonly IShieldTrackingService? _shieldTrackingService;
    private readonly IDamageTrendService? _damageTrendService;

    /// <summary>
    /// Represents a single pending heal entry with its amount and registration time.
    /// </summary>
    private record PendingHealEntry(int Amount, DateTime RegisteredTime);

    // Pending heals: targetId → list of pending heal entries
    // Supports multiple concurrent heals per target (e.g., GCD + oGCD weaving)
    private readonly ConcurrentDictionary<uint, List<PendingHealEntry>> _pendingHealsByTarget = new();
    private readonly object _healsLock = new();

    public HpPredictionService(ICombatEventService combatEventService, Configuration configuration)
        : this(combatEventService, configuration, null, null)
    {
    }

    public HpPredictionService(
        ICombatEventService combatEventService,
        Configuration configuration,
        IShieldTrackingService? shieldTrackingService,
        IDamageTrendService? damageTrendService)
    {
        _combatEventService = combatEventService;
        _configuration = configuration;
        _shieldTrackingService = shieldTrackingService;
        _damageTrendService = damageTrendService;

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
    /// Applies pessimistic variance reduction when enabled to account for crit variance.
    /// </summary>
    public uint GetPredictedHp(uint entityId, uint currentHp, uint maxHp)
    {
        // Start with shadow HP
        var baseHp = (int)_combatEventService.GetShadowHp(entityId, currentHp);
        var totalPendingHeal = 0;

        // Copy list under lock to reduce contention, iterate outside lock
        List<PendingHealEntry>? localHeals = null;
        lock (_healsLock)
        {
            if (_pendingHealsByTarget.TryGetValue(entityId, out var heals) && heals.Count > 0)
            {
                localHeals = new List<PendingHealEntry>(heals);
            }
        }

        // Iterate outside lock for better frame performance
        if (localHeals != null)
        {
            var now = DateTime.UtcNow;
            foreach (var heal in localHeals)
            {
                // Only count non-expired heals
                if ((now - heal.RegisteredTime).TotalSeconds <= FFXIVTimings.HpPredictionTimeoutSeconds)
                {
                    totalPendingHeal += heal.Amount;
                }
            }
        }

        // Apply pessimistic variance reduction when enabled
        // This accounts for crit heals landing higher than predicted, which could cause overprediction
        if (_configuration.Healing.EnableCritVarianceReduction && totalPendingHeal > 0)
        {
            var varianceReduction = _configuration.Healing.CritVarianceReduction;
            totalPendingHeal = (int)(totalPendingHeal * (1f - varianceReduction));
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

    /// <summary>
    /// Gets effective HP including shields (current HP + shield value).
    /// </summary>
    public uint GetEffectiveHp(uint entityId, uint currentHp)
    {
        if (_shieldTrackingService == null)
            return currentHp;

        var shieldValue = _shieldTrackingService.GetTotalShieldValue(entityId);
        return (uint)(currentHp + shieldValue);
    }

    /// <summary>
    /// Gets predicted HP after accounting for pending heals, shields, AND predicted damage.
    /// This is the most comprehensive HP prediction.
    /// </summary>
    public int GetPredictedHpAfterDamage(uint entityId, uint currentHp, uint maxHp, float forecastSeconds = 3f)
    {
        // Start with shadow HP (accounts for recent damage not yet reflected in game HP)
        var baseHp = (int)_combatEventService.GetShadowHp(entityId, currentHp);

        // Add pending heals
        var pendingHeals = GetPendingHealAmount(entityId);
        if (_configuration.Healing.EnableCritVarianceReduction && pendingHeals > 0)
        {
            var varianceReduction = _configuration.Healing.CritVarianceReduction;
            pendingHeals = (int)(pendingHeals * (1f - varianceReduction));
        }

        // Add shields
        var shieldValue = _shieldTrackingService?.GetTotalShieldValue(entityId) ?? 0;

        // Calculate predicted damage over forecast window
        var predictedDamage = 0;
        if (_damageTrendService != null)
        {
            var damageRate = _damageTrendService.GetCurrentDamageRate(entityId, forecastSeconds);

            // Apply mitigation to damage prediction
            var mitigationPercent = _shieldTrackingService?.GetCombinedMitigation(entityId) ?? 0f;
            var effectiveDamageRate = damageRate * (1f - mitigationPercent);

            predictedDamage = (int)(effectiveDamageRate * forecastSeconds);
        }

        // Calculate predicted HP
        var predictedHp = baseHp + pendingHeals + shieldValue - predictedDamage;

        // Clamp to valid range
        return Math.Clamp(predictedHp, 0, (int)maxHp);
    }

    /// <summary>
    /// Gets comprehensive survivability info for a target.
    /// Includes HP, shields, mitigation, damage rate, and predicted outcome.
    /// </summary>
    public SurvivabilityInfo GetSurvivabilityInfo(uint entityId, uint currentHp, uint maxHp, float forecastSeconds = 3f)
    {
        // Get pending heals
        var pendingHeals = GetPendingHealAmount(entityId);
        if (_configuration.Healing.EnableCritVarianceReduction && pendingHeals > 0)
        {
            var varianceReduction = _configuration.Healing.CritVarianceReduction;
            pendingHeals = (int)(pendingHeals * (1f - varianceReduction));
        }

        // Get shield and mitigation data
        var shieldValue = _shieldTrackingService?.GetTotalShieldValue(entityId) ?? 0;
        var mitigationPercent = _shieldTrackingService?.GetCombinedMitigation(entityId) ?? 0f;
        var isInvulnerable = _shieldTrackingService?.IsInvulnerable(entityId) ?? false;

        // Get damage data
        var damageRate = _damageTrendService?.GetCurrentDamageRate(entityId, forecastSeconds) ?? 0f;
        var effectiveDamageRate = damageRate * (1f - mitigationPercent);
        var predictedDamage = (int)(effectiveDamageRate * forecastSeconds);

        // Calculate time until death
        var timeUntilDeath = float.MaxValue;
        if (_damageTrendService != null && !isInvulnerable)
        {
            // Use effective HP (HP + shields) for TTD calculation
            var effectiveHp = currentHp + (uint)shieldValue;
            timeUntilDeath = _damageTrendService.EstimateTimeToDeath(entityId, effectiveHp, forecastSeconds);

            // Adjust for mitigation (if they have mitigation, they'll live longer)
            if (mitigationPercent > 0 && timeUntilDeath < float.MaxValue)
            {
                timeUntilDeath /= (1f - mitigationPercent);
            }
        }

        return new SurvivabilityInfo
        {
            EntityId = entityId,
            CurrentHp = currentHp,
            MaxHp = maxHp,
            PendingHeals = pendingHeals,
            ShieldValue = shieldValue,
            MitigationPercent = mitigationPercent,
            DamageRate = damageRate,
            PredictedDamage = predictedDamage,
            TimeUntilDeath = timeUntilDeath,
            IsInvulnerable = isInvulnerable
        };
    }
}
