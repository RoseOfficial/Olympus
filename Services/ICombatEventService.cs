namespace Olympus.Services;

/// <summary>
/// Interface for combat event tracking, primarily used for shadow HP management.
/// </summary>
public interface ICombatEventService
{
    /// <summary>
    /// Event raised when a healing effect from the local player lands.
    /// </summary>
    event System.Action? OnLocalPlayerHealLanded;

    /// <summary>
    /// Gets the shadow HP for an entity, or the fallback value if not tracked.
    /// </summary>
    uint GetShadowHp(uint entityId, uint fallbackHp);

    /// <summary>
    /// Registers a predicted heal amount for calibration when the heal lands.
    /// </summary>
    void RegisterPredictionForCalibration(int predictedAmount);
}
