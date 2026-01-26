using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Olympus.Data;

namespace Olympus.Rotation;

/// <summary>
/// Manages rotation instances with lazy loading - rotations are created on-demand when the player
/// switches jobs, rather than instantiating all 21 rotations at startup.
/// </summary>
public sealed class RotationManager : IDisposable
{
    private readonly Dictionary<uint, IRotation> _rotations = new();
    private readonly Dictionary<uint, Func<IRotation>> _factories = new();
    private IRotation? _activeRotation;
    private uint _lastJobId;

    /// <summary>
    /// Gets the currently active rotation, or null if none is active.
    /// </summary>
    public IRotation? ActiveRotation => _activeRotation;

    /// <summary>
    /// Gets all instantiated rotations (only those that have been loaded).
    /// </summary>
    public IEnumerable<IRotation> RegisteredRotations => _rotations.Values.Distinct();

    /// <summary>
    /// Registers a factory function for creating a rotation for the specified job IDs.
    /// The rotation will be created lazily when the player switches to that job.
    /// </summary>
    /// <param name="jobId">The job ID this factory creates a rotation for.</param>
    /// <param name="factory">Factory function that creates the rotation instance.</param>
    public void RegisterFactory(uint jobId, Func<IRotation> factory)
    {
        _factories[jobId] = factory;
    }

    /// <summary>
    /// Registers a pre-created rotation for its supported job IDs.
    /// Used for backward compatibility with RotationFactory.
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
    /// Creates the rotation lazily if it hasn't been instantiated yet.
    /// </summary>
    /// <param name="jobId">The player's current job ID.</param>
    /// <returns>True if a rotation is available for this job.</returns>
    public bool UpdateActiveRotation(uint jobId)
    {
        if (jobId == _lastJobId && _activeRotation != null)
            return true;

        _lastJobId = jobId;

        // Try cached rotation first
        if (_rotations.TryGetValue(jobId, out _activeRotation))
            return true;

        // Try to create via factory
        if (_factories.TryGetValue(jobId, out var factory))
        {
            _activeRotation = factory();

            // Cache by all supported job IDs (e.g., WHM and CNJ share same rotation)
            foreach (var supportedJobId in _activeRotation.SupportedJobIds)
            {
                _rotations[supportedJobId] = _activeRotation;
            }

            return true;
        }

        _activeRotation = null;
        return false;
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
    /// Returns true if the specified job has a registered factory (rotation available).
    /// </summary>
    public bool HasRotationForJob(uint jobId) => _factories.ContainsKey(jobId);

    /// <summary>
    /// Gets the rotation for a specific job, or null if none exists.
    /// Note: This will create the rotation if it hasn't been instantiated yet.
    /// </summary>
    public IRotation? GetRotationForJob(uint jobId)
    {
        if (_rotations.TryGetValue(jobId, out var rotation))
            return rotation;

        if (_factories.TryGetValue(jobId, out var factory))
        {
            rotation = factory();
            foreach (var supportedJobId in rotation.SupportedJobIds)
            {
                _rotations[supportedJobId] = rotation;
            }
            return rotation;
        }

        return null;
    }

    /// <summary>
    /// Disposes all instantiated rotations.
    /// </summary>
    public void Dispose()
    {
        // Dispose each unique rotation instance (some jobs share rotations)
        foreach (var rotation in _rotations.Values.Distinct())
        {
            if (rotation is IDisposable disposable)
                disposable.Dispose();
        }
        _rotations.Clear();
        _factories.Clear();
        _activeRotation = null;
    }
}
