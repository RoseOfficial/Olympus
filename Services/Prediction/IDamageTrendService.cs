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
/// Service for analyzing damage intake trends over time.
/// Used to make proactive cooldown decisions (e.g., apply Divine Benison before tank buster).
/// </summary>
public interface IDamageTrendService
{
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
}
