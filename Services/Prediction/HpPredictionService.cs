using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Olympus.Services.Prediction;

/// <summary>
/// Simplified HP prediction service (RSR-style).
/// Tracks pending heals for the currently-casting action only.
/// This prevents double-healing by making targets appear "healed" immediately.
/// </summary>
public sealed class HpPredictionService : IDisposable
{
    private readonly CombatEventService _combatEventService;

    // Pending heals: targetId → healAmount
    // Only tracks ONE action at a time (the current cast)
    private readonly ConcurrentDictionary<uint, int> _pendingHeals = new();

    // When pending heals were registered (for timeout)
    private DateTime _pendingHealTime = DateTime.MinValue;

    // Auto-clear after this many seconds if action effect never lands
    private const double TimeoutSeconds = 3.0;

    public HpPredictionService(CombatEventService combatEventService)
    {
        _combatEventService = combatEventService;

        // Subscribe to heal landed event to clear pending heals
        _combatEventService.OnLocalPlayerHealLanded += ClearPendingHeals;
    }

    public void Dispose()
    {
        _combatEventService.OnLocalPlayerHealLanded -= ClearPendingHeals;
    }

    /// <summary>
    /// Gets predicted HP for an entity (shadow HP + pending heals).
    /// </summary>
    public uint GetPredictedHp(uint entityId, uint currentHp, uint maxHp)
    {
        // Check for timeout
        if (DateTime.UtcNow > _pendingHealTime.AddSeconds(TimeoutSeconds))
        {
            _pendingHeals.Clear();
        }

        // Start with shadow HP
        var baseHp = (int)_combatEventService.GetShadowHp(entityId, currentHp);
        var predictedHp = baseHp;

        // Add pending heal if any
        if (_pendingHeals.TryGetValue(entityId, out var pendingHeal))
        {
            predictedHp += pendingHeal;
        }

        return (uint)Math.Clamp(predictedHp, 0, (int)maxHp);
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
    /// </summary>
    public void RegisterPendingHeal(uint targetId, int amount)
    {
        // Clear any previous pending heals (we only track one action)
        _pendingHeals.Clear();

        _pendingHeals[targetId] = amount;
        _pendingHealTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Register pending AoE heals for multiple targets.
    /// Call this immediately BEFORE executing the AoE heal action.
    /// </summary>
    public void RegisterPendingAoEHeal(IEnumerable<uint> targetIds, int amountPerTarget)
    {
        // Clear any previous pending heals (we only track one action)
        _pendingHeals.Clear();

        foreach (var targetId in targetIds)
        {
            _pendingHeals[targetId] = amountPerTarget;
        }
        _pendingHealTime = DateTime.UtcNow;
    }

    /// <summary>
    /// Clear all pending heals.
    /// Call this when action effect lands (via CombatEventService).
    /// </summary>
    public void ClearPendingHeals()
    {
        _pendingHeals.Clear();
    }

    /// <summary>
    /// Check if there are any pending heals.
    /// </summary>
    public bool HasPendingHeals => !_pendingHeals.IsEmpty;

    /// <summary>
    /// Get pending heal amount for a specific target (for debugging).
    /// </summary>
    public int GetPendingHealAmount(uint targetId)
    {
        return _pendingHeals.TryGetValue(targetId, out var amount) ? amount : 0;
    }

    /// <summary>
    /// Get all pending heals (for debugging).
    /// </summary>
    public IReadOnlyDictionary<uint, int> GetAllPendingHeals()
    {
        return _pendingHeals;
    }
}
