namespace Olympus.Services.Prediction;

/// <summary>
/// Interface for tracking healing intake per entity over a rolling time window.
/// Used in conjunction with DamageIntakeService to calculate net HP changes for trend analysis.
/// </summary>
public interface IHealingIntakeService
{
    /// <summary>
    /// Records healing received by an entity.
    /// </summary>
    /// <param name="entityId">The entity that received healing.</param>
    /// <param name="amount">The amount of healing received.</param>
    void RecordHealing(uint entityId, int amount);

    /// <summary>
    /// Gets the total healing received by an entity within the specified time window.
    /// </summary>
    /// <param name="entityId">The entity to check.</param>
    /// <param name="windowSeconds">The time window in seconds (default 5s).</param>
    /// <returns>Total healing received in the window.</returns>
    int GetRecentHealingIntake(uint entityId, float windowSeconds = 5f);

    /// <summary>
    /// Gets the healing rate (healing per second) for an entity within the specified time window.
    /// </summary>
    /// <param name="entityId">The entity to check.</param>
    /// <param name="windowSeconds">The time window in seconds (default 5s).</param>
    /// <returns>Healing per second rate.</returns>
    float GetHealingRate(uint entityId, float windowSeconds = 5f);

    /// <summary>
    /// Gets the total party-wide healing intake within the specified time window.
    /// </summary>
    /// <param name="windowSeconds">The time window in seconds (default 5s).</param>
    /// <returns>Total party healing received in the window.</returns>
    int GetPartyHealingIntake(float windowSeconds = 5f);

    /// <summary>
    /// Gets the party-wide healing rate (healing per second).
    /// </summary>
    /// <param name="windowSeconds">The time window in seconds (default 5s).</param>
    /// <returns>Party healing per second rate.</returns>
    float GetPartyHealingRate(float windowSeconds = 5f);

    /// <summary>
    /// Clears all tracked healing records.
    /// </summary>
    void Clear();

    /// <summary>
    /// Clears healing records for a specific entity.
    /// </summary>
    /// <param name="entityId">The entity to clear.</param>
    void ClearEntity(uint entityId);
}
