using System.Collections.Generic;

namespace Olympus.Services.Prediction;

/// <summary>
/// Damage trend over time - used for proactive cooldown decisions.
/// </summary>
public enum DamageTrend
{
    /// <summary>Damage is decreasing or stopped.</summary>
    Decreasing = 0,

    /// <summary>Damage is relatively constant.</summary>
    Stable = 1,

    /// <summary>Damage is increasing.</summary>
    Increasing = 2,

    /// <summary>Damage is rapidly increasing (spike detected).</summary>
    Spiking = 3
}

/// <summary>
/// HP trend for a target - used for survivability triage.
/// </summary>
public enum HpTrend
{
    /// <summary>HP is rising (receiving heals or regen ticking).</summary>
    Rising = 0,

    /// <summary>HP is stable (no significant change).</summary>
    Stable = 1,

    /// <summary>HP is falling (taking damage).</summary>
    Falling = 2,

    /// <summary>HP is rapidly falling (critical - taking heavy damage).</summary>
    Critical = 3
}

/// <summary>
/// Service for analyzing damage intake trends over time.
/// Used to make proactive cooldown decisions (e.g., apply Divine Benison before tank buster).
/// </summary>
public interface IDamageTrendService
{
    /// <summary>
    /// Updates the service each frame. Automatically detects and records spike events.
    /// </summary>
    /// <param name="deltaSeconds">Time since last frame.</param>
    /// <param name="partyEntityIds">Entity IDs of party members to monitor for spikes.</param>
    void Update(float deltaSeconds, IEnumerable<uint> partyEntityIds);
    /// <summary>
    /// Gets the damage trend for the entire party over the specified time window.
    /// </summary>
    /// <param name="windowSeconds">Time window to analyze (default 10 seconds).</param>
    /// <returns>The detected damage trend.</returns>
    DamageTrend GetPartyDamageTrend(float windowSeconds = 10f);

    /// <summary>
    /// Gets the damage trend for a specific entity.
    /// </summary>
    /// <param name="entityId">The entity to check.</param>
    /// <param name="windowSeconds">Time window to analyze (default 10 seconds).</param>
    /// <returns>The detected damage trend.</returns>
    DamageTrend GetEntityDamageTrend(uint entityId, float windowSeconds = 10f);

    /// <summary>
    /// Checks if a damage spike is imminent based on recent damage patterns.
    /// </summary>
    /// <param name="confidenceThreshold">Confidence threshold (0.0-1.0).</param>
    /// <returns>True if spike is likely imminent.</returns>
    bool IsDamageSpikeImminent(float confidenceThreshold = 0.8f);

    /// <summary>
    /// Gets the rate of change of damage (DPS acceleration).
    /// Positive = damage increasing, negative = damage decreasing.
    /// </summary>
    /// <param name="entityId">The entity to check.</param>
    /// <param name="windowSeconds">Time window to analyze.</param>
    /// <returns>The DPS rate of change per second.</returns>
    float GetDamageAcceleration(uint entityId, float windowSeconds = 5f);

    /// <summary>
    /// Gets the current damage rate for an entity (DPS taken).
    /// </summary>
    /// <param name="entityId">The entity to check.</param>
    /// <param name="windowSeconds">Time window for calculation.</param>
    /// <returns>Damage per second.</returns>
    float GetCurrentDamageRate(uint entityId, float windowSeconds = 3f);

    /// <summary>
    /// Gets the severity of a damage spike, factoring in both spike detection and party HP state.
    /// Higher severity means more urgent need for healing/mitigation.
    /// </summary>
    /// <param name="avgPartyHpPercent">Average party HP percentage (0.0-1.0).</param>
    /// <returns>
    /// Severity score (0.0-1.0+):
    /// - 0.0: No spike detected
    /// - 0.0-0.5: Low severity (spike + healthy party)
    /// - 0.5-0.8: Medium severity (spike + moderately damaged party)
    /// - 0.8+: High severity (spike + low HP party, needs immediate attention)
    /// </returns>
    float GetSpikeSeverity(float avgPartyHpPercent);

    /// <summary>
    /// Predicts when the next damage spike will occur based on detected patterns.
    /// Detects periodic damage patterns (e.g., tank buster every 8 seconds).
    /// </summary>
    /// <param name="entityId">The entity to predict spikes for.</param>
    /// <returns>
    /// A tuple containing:
    /// - secondsUntilSpike: Time until next predicted spike (float.MaxValue if no pattern detected)
    /// - confidence: Confidence in the prediction (0.0-1.0)
    /// </returns>
    (float secondsUntilSpike, float confidence) PredictNextSpike(uint entityId);

    /// <summary>
    /// Records a spike event for pattern detection.
    /// Should be called when a significant damage spike is detected.
    /// </summary>
    /// <param name="entityId">The entity that received the spike.</param>
    /// <param name="damageAmount">The amount of damage from the spike.</param>
    void RecordSpikeEvent(uint entityId, int damageAmount);

    /// <summary>
    /// Checks if the party is in a sustained high-damage phase.
    /// Unlike spike detection (which requires changes in damage rate),
    /// this detects consistently high damage that may not trigger spike logic.
    /// </summary>
    /// <param name="thresholdDps">Minimum DPS to consider "high damage" (default 800).</param>
    /// <param name="durationSeconds">How long damage must stay high (default 3 seconds).</param>
    /// <returns>True if in sustained high-damage phase.</returns>
    bool IsInHighDamagePhase(float thresholdDps = 800f, float durationSeconds = 3f);

    /// <summary>
    /// Gets the duration the party has been in a high-damage phase.
    /// Returns 0 if not currently in a high-damage phase.
    /// </summary>
    /// <param name="thresholdDps">DPS threshold for "high damage".</param>
    /// <returns>Seconds in high-damage phase, or 0 if not in one.</returns>
    float GetHighDamagePhaseDuration(float thresholdDps = 800f);

    /// <summary>
    /// Gets the HP trend for a specific entity based on recent damage/healing.
    /// Used for survivability triage - targets with falling HP are more urgent.
    /// </summary>
    /// <param name="entityId">The entity to check.</param>
    /// <param name="currentHp">Current HP of the entity.</param>
    /// <param name="maxHp">Maximum HP of the entity.</param>
    /// <param name="windowSeconds">Time window to analyze (default 3 seconds).</param>
    /// <returns>The detected HP trend.</returns>
    HpTrend GetHpTrend(uint entityId, uint currentHp, uint maxHp, float windowSeconds = 3f);

    /// <summary>
    /// Estimates time until an entity dies at current damage rate.
    /// Returns float.MaxValue if not taking damage or would not die.
    /// </summary>
    /// <param name="entityId">The entity to check.</param>
    /// <param name="currentHp">Current HP of the entity.</param>
    /// <param name="windowSeconds">Time window for damage rate calculation (default 3 seconds).</param>
    /// <returns>Estimated seconds until death, or float.MaxValue if not in danger.</returns>
    float EstimateTimeToDeath(uint entityId, uint currentHp, float windowSeconds = 3f);
}
