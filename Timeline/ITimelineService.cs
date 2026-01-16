using Olympus.Timeline.Models;

namespace Olympus.Timeline;

/// <summary>
/// Service interface for fight timeline tracking and mechanic prediction.
/// Provides runtime sync with game events and accurate mechanic forecasting.
/// </summary>
public interface ITimelineService
{
    /// <summary>
    /// Whether a timeline is currently active and tracking.
    /// </summary>
    bool IsActive { get; }

    /// <summary>
    /// Current timeline position in seconds from fight start.
    /// Returns 0 if no timeline is active.
    /// </summary>
    float CurrentTime { get; }

    /// <summary>
    /// Current fight phase name (from timeline labels).
    /// Returns empty string if no phase has been reached.
    /// </summary>
    string CurrentPhase { get; }

    /// <summary>
    /// Name of the currently loaded fight.
    /// Returns empty string if no timeline is loaded.
    /// </summary>
    string FightName { get; }

    /// <summary>
    /// Confidence score for current timeline position (0.0 to 1.0).
    /// Higher values indicate recent sync and reliable predictions.
    /// </summary>
    float Confidence { get; }

    /// <summary>
    /// The next predicted raidwide mechanic, or null if none upcoming.
    /// </summary>
    MechanicPrediction? NextRaidwide { get; }

    /// <summary>
    /// The next predicted tank buster, or null if none upcoming.
    /// </summary>
    MechanicPrediction? NextTankBuster { get; }

    /// <summary>
    /// Checks if a mechanic of the specified type is imminent.
    /// </summary>
    /// <param name="type">The mechanic type to check for.</param>
    /// <param name="withinSeconds">Time window to check (e.g., 8 seconds for pre-shielding).</param>
    /// <returns>True if a matching mechanic is predicted within the time window.</returns>
    bool IsMechanicImminent(TimelineEntryType type, float withinSeconds);

    /// <summary>
    /// Gets the next mechanic prediction of a specific type.
    /// </summary>
    /// <param name="type">The mechanic type to look for.</param>
    /// <returns>The prediction, or null if no matching mechanic is upcoming.</returns>
    MechanicPrediction? GetNextMechanic(TimelineEntryType type);

    /// <summary>
    /// Updates the timeline state. Called every frame.
    /// </summary>
    void Update();

    /// <summary>
    /// Loads the timeline for the specified zone.
    /// </summary>
    /// <param name="zoneId">The zone (territory) ID.</param>
    void LoadForZone(uint zoneId);

    /// <summary>
    /// Clears the current timeline and stops tracking.
    /// </summary>
    void Clear();

    /// <summary>
    /// Notifies the service of an ability being used.
    /// Used for timeline sync.
    /// </summary>
    /// <param name="sourceId">The entity ID of the ability source.</param>
    /// <param name="actionId">The action ID that was used.</param>
    void OnAbilityUsed(uint sourceId, uint actionId);

    #region Simulation (Debug)

    /// <summary>
    /// Whether the service is currently running a simulation.
    /// </summary>
    bool IsSimulating { get; }

    /// <summary>
    /// Starts a simulated timeline for testing purposes.
    /// Works from any zone without needing to be in actual content.
    /// </summary>
    void StartSimulation();

    /// <summary>
    /// Stops the current simulation and clears timeline state.
    /// </summary>
    void StopSimulation();

    /// <summary>
    /// Manually triggers a sync point during simulation.
    /// </summary>
    /// <param name="actionId">The action ID to sync to.</param>
    void SimulateSyncPoint(uint actionId);

    /// <summary>
    /// Advances simulation time manually (for testing).
    /// </summary>
    /// <param name="seconds">Seconds to advance.</param>
    void AdvanceSimulationTime(float seconds);

    /// <summary>
    /// Gets all upcoming mechanics within a time window (for debug display).
    /// </summary>
    /// <param name="windowSeconds">How far ahead to look.</param>
    /// <returns>Array of predictions.</returns>
    MechanicPrediction[] GetUpcomingMechanics(float windowSeconds);

    #endregion
}
