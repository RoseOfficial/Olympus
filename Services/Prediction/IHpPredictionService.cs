using System;
using System.Collections.Generic;
using Olympus.Services.Party;

namespace Olympus.Services.Prediction;

/// <summary>
/// Survivability assessment result for a target.
/// </summary>
public record SurvivabilityInfo
{
    /// <summary>
    /// The entity ID.
    /// </summary>
    public uint EntityId { get; init; }

    /// <summary>
    /// Current HP.
    /// </summary>
    public uint CurrentHp { get; init; }

    /// <summary>
    /// Maximum HP.
    /// </summary>
    public uint MaxHp { get; init; }

    /// <summary>
    /// Pending heals registered for this target.
    /// </summary>
    public int PendingHeals { get; init; }

    /// <summary>
    /// Total shield value on this target.
    /// </summary>
    public int ShieldValue { get; init; }

    /// <summary>
    /// Combined mitigation percentage (0.0-1.0).
    /// </summary>
    public float MitigationPercent { get; init; }

    /// <summary>
    /// Current damage rate (DPS taken).
    /// </summary>
    public float DamageRate { get; init; }

    /// <summary>
    /// Predicted damage over the forecast window.
    /// </summary>
    public int PredictedDamage { get; init; }

    /// <summary>
    /// Effective HP (Current HP + Shields).
    /// </summary>
    public uint EffectiveHp => (uint)(CurrentHp + ShieldValue);

    /// <summary>
    /// Predicted HP after heals and damage.
    /// PredictedHp = CurrentHp + PendingHeals + Shields - PredictedDamage
    /// </summary>
    public int PredictedHpAfterDamage => (int)(CurrentHp + PendingHeals + ShieldValue - PredictedDamage);

    /// <summary>
    /// Predicted HP percent after heals and damage.
    /// </summary>
    public float PredictedHpPercent => MaxHp > 0 ? (float)Math.Max(0, PredictedHpAfterDamage) / MaxHp : 0f;

    /// <summary>
    /// Estimated seconds until death at current damage rate.
    /// float.MaxValue if not in danger.
    /// </summary>
    public float TimeUntilDeath { get; init; } = float.MaxValue;

    /// <summary>
    /// Whether this target is invulnerable (Hallowed Ground, etc.)
    /// </summary>
    public bool IsInvulnerable { get; init; }

    /// <summary>
    /// Survivability score (0.0 = dead, 1.0 = full HP with shields).
    /// Lower score = more urgent healing needed.
    /// </summary>
    public float SurvivabilityScore
    {
        get
        {
            if (IsInvulnerable) return 1.0f;
            if (MaxHp == 0) return 0f;

            // Base score from predicted HP percent
            var baseScore = PredictedHpPercent;

            // Boost for mitigation
            baseScore += MitigationPercent * 0.1f;

            // Penalty for low time-to-death
            if (TimeUntilDeath < 3f)
                baseScore *= 0.5f;
            else if (TimeUntilDeath < 5f)
                baseScore *= 0.7f;
            else if (TimeUntilDeath < 10f)
                baseScore *= 0.85f;

            return Math.Clamp(baseScore, 0f, 1f);
        }
    }
}

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
    /// Gets effective HP including shields (current HP + shield value).
    /// </summary>
    uint GetEffectiveHp(uint entityId, uint currentHp);

    /// <summary>
    /// Gets predicted HP after accounting for pending heals, shields, AND predicted damage.
    /// This is the most comprehensive HP prediction.
    /// </summary>
    /// <param name="entityId">Target entity ID.</param>
    /// <param name="currentHp">Current HP.</param>
    /// <param name="maxHp">Maximum HP.</param>
    /// <param name="forecastSeconds">How far ahead to predict damage (default 3s).</param>
    int GetPredictedHpAfterDamage(uint entityId, uint currentHp, uint maxHp, float forecastSeconds = 3f);

    /// <summary>
    /// Gets comprehensive survivability info for a target.
    /// Includes HP, shields, mitigation, damage rate, and predicted outcome.
    /// </summary>
    /// <param name="entityId">Target entity ID.</param>
    /// <param name="currentHp">Current HP.</param>
    /// <param name="maxHp">Maximum HP.</param>
    /// <param name="forecastSeconds">How far ahead to predict damage (default 3s).</param>
    SurvivabilityInfo GetSurvivabilityInfo(uint entityId, uint currentHp, uint maxHp, float forecastSeconds = 3f);

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
