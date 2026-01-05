using System.Collections.Generic;

namespace Olympus.Services.Prediction;

/// <summary>
/// Interface for HP prediction service.
/// </summary>
public interface IHpPredictionService
{
    /// <summary>
    /// Gets predicted HP for an entity (shadow HP + pending heals).
    /// </summary>
    uint GetPredictedHp(uint entityId, uint currentHp, uint maxHp);

    /// <summary>
    /// Gets predicted HP percent for an entity.
    /// </summary>
    float GetPredictedHpPercent(uint entityId, uint currentHp, uint maxHp);

    /// <summary>
    /// Register a pending single-target heal.
    /// </summary>
    void RegisterPendingHeal(uint targetId, int amount);

    /// <summary>
    /// Register pending AoE heals for multiple targets.
    /// </summary>
    void RegisterPendingAoEHeal(IEnumerable<uint> targetIds, int amountPerTarget);

    /// <summary>
    /// Clear all pending heals.
    /// </summary>
    void ClearPendingHeals();

    /// <summary>
    /// Clear pending heals for a specific target.
    /// </summary>
    void ClearPendingHeals(uint targetId);

    /// <summary>
    /// Check if there are any pending heals.
    /// </summary>
    bool HasPendingHeals { get; }

    /// <summary>
    /// Get pending heal amount for a specific target.
    /// </summary>
    int GetPendingHealAmount(uint targetId);

    /// <summary>
    /// Get all pending heals.
    /// </summary>
    IReadOnlyDictionary<uint, int> GetAllPendingHeals();
}
