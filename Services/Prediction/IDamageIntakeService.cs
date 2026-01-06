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
}
