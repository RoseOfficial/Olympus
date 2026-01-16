namespace Olympus.Services;

/// <summary>
/// Interface for combat event tracking, primarily used for shadow HP management.
/// </summary>
public interface ICombatEventService
{
    /// <summary>
    /// Event raised when a healing effect from the local player lands.
    /// The uint parameter is the target entity ID that received the heal.
    /// </summary>
    event System.Action<uint>? OnLocalPlayerHealLanded;

    /// <summary>
    /// Event raised when damage is received by any party member.
    /// Parameters: (entityId, damageAmount)
    /// </summary>
    event System.Action<uint, int>? OnDamageReceived;

    /// <summary>
    /// Event raised when any heal effect lands (from any source, not just local player).
    /// Used for co-healer tracking.
    /// Parameters: (healerEntityId, targetEntityId, healAmount)
    /// </summary>
    event System.Action<uint, uint, int>? OnAnyHealReceived;

    /// <summary>
    /// Event raised when any ability is used (action effect resolves).
    /// Used for timeline sync.
    /// Parameters: (sourceEntityId, actionId)
    /// </summary>
    event System.Action<uint, uint>? OnAbilityUsed;

    /// <summary>
    /// Gets the shadow HP for an entity, or the fallback value if not tracked.
    /// </summary>
    uint GetShadowHp(uint entityId, uint fallbackHp);

    /// <summary>
    /// Registers a predicted heal amount for calibration when the heal lands.
    /// </summary>
    void RegisterPredictionForCalibration(int predictedAmount);

    /// <summary>
    /// Gets aggregated overheal statistics for the current session.
    /// </summary>
    CombatEventService.OverhealStatistics GetOverhealStatistics();

    /// <summary>
    /// Resets all overheal statistics for a new session.
    /// </summary>
    void ResetOverhealStatistics();

    /// <summary>
    /// Notifies the service that combat state has changed.
    /// Call this when entering or leaving combat.
    /// </summary>
    void UpdateCombatState(bool inCombat);

    /// <summary>
    /// Gets the duration of the current combat in seconds.
    /// Returns 0 if not in combat.
    /// </summary>
    float GetCombatDurationSeconds();

    /// <summary>
    /// Whether the player is currently in combat.
    /// </summary>
    bool IsInCombat { get; }
}
