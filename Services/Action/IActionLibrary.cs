using System.Collections.Generic;
using Olympus.Models.Action;

namespace Olympus.Services.Action;

/// <summary>
/// Interface for accessing job action definitions.
/// Provides a unified way to look up actions across all jobs.
/// </summary>
public interface IActionLibrary
{
    /// <summary>
    /// Gets an action definition by its game action ID.
    /// </summary>
    /// <param name="actionId">The game's action ID.</param>
    /// <returns>The action definition, or null if not found.</returns>
    ActionDefinition? GetAction(uint actionId);

    /// <summary>
    /// Gets all actions for a specific job.
    /// </summary>
    /// <param name="jobId">The job ID (e.g., 24 for WHM).</param>
    /// <returns>All action definitions for that job.</returns>
    IEnumerable<ActionDefinition> GetJobActions(uint jobId);

    /// <summary>
    /// Gets all healing actions for a specific job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <returns>All healing action definitions for that job.</returns>
    IEnumerable<ActionDefinition> GetHealingActions(uint jobId);

    /// <summary>
    /// Gets all damage actions for a specific job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <returns>All damage action definitions for that job.</returns>
    IEnumerable<ActionDefinition> GetDamageActions(uint jobId);

    /// <summary>
    /// Gets actions available at a specific level for a job.
    /// </summary>
    /// <param name="jobId">The job ID.</param>
    /// <param name="level">The player's current level.</param>
    /// <returns>All actions available at that level.</returns>
    IEnumerable<ActionDefinition> GetActionsAtLevel(uint jobId, byte level);

    /// <summary>
    /// Checks if an action is registered in the library.
    /// </summary>
    /// <param name="actionId">The game's action ID.</param>
    /// <returns>True if the action is known.</returns>
    bool HasAction(uint actionId);
}
