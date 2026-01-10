using Olympus.Rotation.AsclepiusCore.Context;

namespace Olympus.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Interface for Asclepius (Sage) rotation modules.
/// Each module handles a specific aspect of the rotation (healing, damage, etc.).
/// Modules are executed in priority order (lower number = higher priority).
/// </summary>
public interface IAsclepiusModule
{
    /// <summary>
    /// Module execution priority. Lower numbers execute first.
    /// Standard priorities:
    ///   3 - Kardia (ensure Kardia is placed)
    ///   5 - Resurrection (raise dead party members)
    ///  10 - Healing (Addersgall heals, Pneuma, GCD heals)
    ///  15 - Shields (Haima, Panhaima, E.Diagnosis)
    ///  20 - Defensive (Kerachole, Taurochole mitigation)
    ///  30 - Buffs (Physis II, Krasis, Zoe, Soteria)
    ///  50 - Damage (Dosis, DoT, Phlegma, Psyche)
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Module name for logging and debugging.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Attempts to execute an action from this module.
    /// Returns true if an action was executed, false otherwise.
    /// </summary>
    /// <param name="context">The shared Asclepius context.</param>
    /// <param name="isMoving">Whether the player is currently moving.</param>
    /// <returns>True if an action was executed.</returns>
    bool TryExecute(IAsclepiusContext context, bool isMoving);

    /// <summary>
    /// Updates the debug state with this module's current state.
    /// Called every frame regardless of whether the module executed an action.
    /// </summary>
    /// <param name="context">The shared Asclepius context.</param>
    void UpdateDebugState(IAsclepiusContext context);
}
