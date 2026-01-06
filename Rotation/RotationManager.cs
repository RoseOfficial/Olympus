using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Olympus.Data;

namespace Olympus.Rotation;

/// <summary>
/// Manages rotation instances and selects the appropriate rotation based on current job.
/// Rotations are registered during plugin initialization.
/// </summary>
public sealed class RotationManager
{
    private readonly Dictionary<uint, IRotation> _rotations = new();
    private IRotation? _activeRotation;
    private uint _lastJobId;

    /// <summary>
    /// Gets the currently active rotation, or null if none is active.
    /// </summary>
    public IRotation? ActiveRotation => _activeRotation;

    /// <summary>
    /// Gets all registered rotations.
    /// </summary>
    public IEnumerable<IRotation> RegisteredRotations => _rotations.Values.Distinct();

    /// <summary>
    /// Registers a rotation for its supported job IDs.
    /// </summary>
    /// <param name="rotation">The rotation to register.</param>
    public void Register(IRotation rotation)
    {
        foreach (var jobId in rotation.SupportedJobIds)
        {
            _rotations[jobId] = rotation;
        }
    }

    /// <summary>
    /// Updates the active rotation based on the player's current job.
    /// </summary>
    /// <param name="jobId">The player's current job ID.</param>
    /// <returns>True if a rotation is available for this job.</returns>
    public bool UpdateActiveRotation(uint jobId)
    {
        if (jobId == _lastJobId && _activeRotation != null)
            return true;

        _lastJobId = jobId;
        _rotations.TryGetValue(jobId, out _activeRotation);
        return _activeRotation != null;
    }

    /// <summary>
    /// Executes the active rotation if one is available.
    /// </summary>
    /// <param name="player">The local player character.</param>
    /// <returns>True if a rotation was executed.</returns>
    public bool Execute(IPlayerCharacter player)
    {
        if (_activeRotation == null)
            return false;

        _activeRotation.Execute(player);
        return true;
    }

    /// <summary>
    /// Returns true if the specified job has a registered rotation.
    /// </summary>
    public bool HasRotationForJob(uint jobId) => _rotations.ContainsKey(jobId);

    /// <summary>
    /// Gets the rotation for a specific job, or null if none exists.
    /// </summary>
    public IRotation? GetRotationForJob(uint jobId) =>
        _rotations.TryGetValue(jobId, out var rotation) ? rotation : null;
}
