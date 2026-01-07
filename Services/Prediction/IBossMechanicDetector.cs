namespace Olympus.Services.Prediction;

/// <summary>
/// Prediction data for an upcoming raidwide damage event.
/// </summary>
/// <param name="SecondsUntil">Estimated seconds until the raidwide lands.</param>
/// <param name="Confidence">Confidence in the prediction (0.0 to 1.0).</param>
/// <param name="EstimatedDamagePercent">Estimated damage as percentage of max HP.</param>
/// <param name="DetectedInterval">The detected interval between raidwides (seconds).</param>
public record RaidwidePrediction(
    float SecondsUntil,
    float Confidence,
    float EstimatedDamagePercent,
    float DetectedInterval);

/// <summary>
/// Prediction data for an upcoming tank buster.
/// </summary>
/// <param name="SecondsUntil">Estimated seconds until the tank buster lands.</param>
/// <param name="Confidence">Confidence in the prediction (0.0 to 1.0).</param>
/// <param name="EstimatedDamage">Estimated damage amount.</param>
/// <param name="TargetTankEntityId">Entity ID of the tank being targeted.</param>
public record TankBusterPrediction(
    float SecondsUntil,
    float Confidence,
    int EstimatedDamage,
    uint TargetTankEntityId);

/// <summary>
/// Detects boss mechanic patterns (raidwides, tank busters) for proactive healing.
/// </summary>
public interface IBossMechanicDetector
{
    /// <summary>
    /// Whether a raidwide is predicted to happen within the preparation window.
    /// </summary>
    bool IsRaidwideImminent { get; }

    /// <summary>
    /// Whether a tank buster is predicted to happen within the preparation window.
    /// </summary>
    bool IsTankBusterImminent { get; }

    /// <summary>
    /// Seconds until the next predicted raidwide.
    /// Returns float.MaxValue if no raidwide is predicted.
    /// </summary>
    float SecondsUntilNextRaidwide { get; }

    /// <summary>
    /// Seconds until the next predicted tank buster.
    /// Returns float.MaxValue if no tank buster is predicted.
    /// </summary>
    float SecondsUntilNextTankBuster { get; }

    /// <summary>
    /// Detailed prediction for the next raidwide, or null if none predicted.
    /// </summary>
    RaidwidePrediction? PredictedRaidwide { get; }

    /// <summary>
    /// Detailed prediction for the next tank buster, or null if none predicted.
    /// </summary>
    TankBusterPrediction? PredictedTankBuster { get; }

    /// <summary>
    /// Time since the last detected raidwide (seconds).
    /// </summary>
    float SecondsSinceLastRaidwide { get; }

    /// <summary>
    /// Time since the last detected tank buster (seconds).
    /// </summary>
    float SecondsSinceLastTankBuster { get; }

    /// <summary>
    /// Updates the detector state. Call once per frame.
    /// </summary>
    void Update();

    /// <summary>
    /// Records a coordinated damage event (multiple party members hit).
    /// Called by DamageTrendService when multi-target damage is detected.
    /// </summary>
    /// <param name="affectedCount">Number of party members affected.</param>
    /// <param name="averageDamagePercent">Average damage as percentage of max HP.</param>
    void RecordRaidwideDamage(int affectedCount, float averageDamagePercent);

    /// <summary>
    /// Records a tank buster hit.
    /// </summary>
    /// <param name="tankEntityId">Entity ID of the tank that was hit.</param>
    /// <param name="damagePercent">Damage as percentage of max HP.</param>
    /// <param name="damageAmount">Raw damage amount.</param>
    void RecordTankBusterDamage(uint tankEntityId, float damagePercent, int damageAmount);

    /// <summary>
    /// Clears all tracked state. Call on zone transitions.
    /// </summary>
    void Clear();
}
