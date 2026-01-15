namespace Olympus.Services.Prediction;

/// <summary>
/// Interface for tracking damage intake per entity over a rolling time window.
/// Used to identify party members taking active damage for healing triage.
/// </summary>
public interface IDamageIntakeService
{
    /// <summary>
    /// Records damage received by an entity.
    /// </summary>
    /// <param name="entityId">The entity that received damage.</param>
    /// <param name="amount">The amount of damage received.</param>
    void RecordDamage(uint entityId, int amount);

    /// <summary>
    /// Gets the total damage received by an entity within the specified time window.
    /// </summary>
    /// <param name="entityId">The entity to check.</param>
    /// <param name="windowSeconds">The time window in seconds (default 5s).</param>
    /// <returns>Total damage received in the window.</returns>
    int GetRecentDamageIntake(uint entityId, float windowSeconds = 5f);

    /// <summary>
    /// Gets the damage rate (damage per second) for an entity within the specified time window.
    /// </summary>
    /// <param name="entityId">The entity to check.</param>
    /// <param name="windowSeconds">The time window in seconds (default 5s).</param>
    /// <returns>Damage per second rate.</returns>
    float GetDamageRate(uint entityId, float windowSeconds = 5f);

    /// <summary>
    /// Gets the total party-wide damage intake within the specified time window.
    /// </summary>
    /// <param name="windowSeconds">The time window in seconds (default 5s).</param>
    /// <returns>Total party damage received in the window.</returns>
    int GetPartyDamageIntake(float windowSeconds = 5f);

    /// <summary>
    /// Gets the party-wide damage rate (damage per second).
    /// </summary>
    /// <param name="windowSeconds">The time window in seconds (default 5s).</param>
    /// <returns>Party damage per second rate.</returns>
    float GetPartyDamageRate(float windowSeconds = 5f);

    /// <summary>
    /// Clears all tracked damage records.
    /// </summary>
    void Clear();

    /// <summary>
    /// Clears damage records for a specific entity.
    /// </summary>
    /// <param name="entityId">The entity to clear.</param>
    void ClearEntity(uint entityId);

    /// <summary>
    /// Forecasts total party damage expected within the specified time window.
    /// Combines historical damage rate, predicted mechanics, and active DoTs.
    /// </summary>
    /// <param name="forecastSeconds">Time window to forecast into (default 5s).</param>
    /// <returns>Predicted total party damage.</returns>
    int ForecastPartyDamage(float forecastSeconds = 5f);

    /// <summary>
    /// Forecasts damage expected for a specific entity within the time window.
    /// </summary>
    /// <param name="entityId">The entity to forecast damage for.</param>
    /// <param name="forecastSeconds">Time window to forecast into.</param>
    /// <returns>Predicted damage for the entity.</returns>
    int ForecastEntityDamage(uint entityId, float forecastSeconds = 5f);

    /// <summary>
    /// Registers an active DoT or bleed effect on an entity.
    /// </summary>
    /// <param name="entityId">The entity with the DoT.</param>
    /// <param name="damagePerTick">Damage per 3-second tick.</param>
    /// <param name="remainingDuration">Remaining duration in seconds.</param>
    void RegisterActiveDoT(uint entityId, int damagePerTick, float remainingDuration);

    /// <summary>
    /// Clears all active DoT tracking for an entity.
    /// </summary>
    /// <param name="entityId">The entity to clear DoTs for.</param>
    void ClearActiveDoTs(uint entityId);

    /// <summary>
    /// Sets the boss mechanic detector for predictive damage forecasting.
    /// </summary>
    /// <param name="detector">The boss mechanic detector to use.</param>
    void SetBossMechanicDetector(IBossMechanicDetector detector);

    /// <summary>
    /// Gets forecasted damage for a specific entity as a percentage of their max HP.
    /// </summary>
    /// <param name="entityId">The entity to forecast.</param>
    /// <param name="maxHp">The entity's max HP.</param>
    /// <param name="forecastSeconds">Time window to forecast into.</param>
    /// <returns>Predicted damage as a percentage (0.0 to 1.0+).</returns>
    float ForecastDamagePercent(uint entityId, int maxHp, float forecastSeconds = 5f);
}
