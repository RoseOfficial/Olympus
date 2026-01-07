using System.Collections.Generic;

namespace Olympus.Services.Healing;

/// <summary>
/// Detects and tracks co-healer presence and healing activity in the party.
/// Used to coordinate healing decisions and avoid double-healing.
/// </summary>
public interface ICoHealerDetectionService
{
    /// <summary>
    /// Whether there is another healer in the party (8-person content).
    /// </summary>
    bool HasCoHealer { get; }

    /// <summary>
    /// Entity ID of the detected co-healer, or null if no co-healer present.
    /// </summary>
    uint? CoHealerEntityId { get; }

    /// <summary>
    /// Job ID of the co-healer (e.g., 28 for SCH, 33 for AST, 40 for SGE).
    /// </summary>
    uint CoHealerJobId { get; }

    /// <summary>
    /// Whether the co-healer has been actively healing recently.
    /// Based on configurable active window (default 10 seconds).
    /// </summary>
    bool IsCoHealerActive { get; }

    /// <summary>
    /// Healing per second from the co-healer (rolling average).
    /// </summary>
    float CoHealerHps { get; }

    /// <summary>
    /// Estimated pending heals from the co-healer, by target entity ID.
    /// Maps target entity ID to estimated incoming heal amount.
    /// </summary>
    IReadOnlyDictionary<uint, int> CoHealerPendingHeals { get; }

    /// <summary>
    /// Time since the co-healer last healed (in seconds).
    /// Returns float.MaxValue if no heals have been tracked.
    /// </summary>
    float SecondsSinceLastHeal { get; }

    /// <summary>
    /// Updates the service state. Call once per frame.
    /// </summary>
    /// <param name="localPlayerEntityId">The local player's entity ID to exclude from co-healer detection.</param>
    void Update(uint localPlayerEntityId);

    /// <summary>
    /// Clears all tracked state. Call on zone transitions.
    /// </summary>
    void Clear();
}
