using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
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
    private readonly HashSet<IRotation> _uniqueRotations = new();
    private readonly HashSet<uint> _failedJobIds = new();
    private readonly IPluginLog? _log;
    private IRotation? _activeRotation;
    private uint _lastJobId;

    public RotationManager(IPluginLog? log = null)
    {
        _log = log;
    }

    /// <summary>
    /// Gets the currently active rotation, or null if none is active.
    /// </summary>
    public IRotation? ActiveRotation => _activeRotation;

    /// <summary>
    /// Gets all instantiated rotations (only those that have been loaded).
    /// </summary>
    public IEnumerable<IRotation> RegisteredRotations => _uniqueRotations;

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
        _uniqueRotations.Add(rotation);
    }

    /// <summary>
    /// Updates the active rotation based on the player's current job.
    /// Creates the rotation lazily if it hasn't been instantiated yet.
    /// If the factory throws, the failure is cached and not retried until the player
    /// changes jobs, preventing ~60 exceptions/second from flooding the error log.
    /// </summary>
    /// <param name="jobId">The player's current job ID.</param>
    /// <returns>True if a rotation is available for this job.</returns>
    public bool UpdateActiveRotation(uint jobId)
    {
        var jobChanged = jobId != _lastJobId;

        if (!jobChanged && _activeRotation != null)
            return true;

        if (jobChanged)
        {
            // New job: clear failure cache so the incoming job gets a fresh attempt,
            // and any previously-failed job would get a retry if the player returns to it.
            _lastJobId = jobId;
            _failedJobIds.Clear();
        }

        // Try cached rotation first
        if (_rotations.TryGetValue(jobId, out _activeRotation))
            return true;

        // Do not retry a factory that already failed this job session; wait for a job switch.
        if (_failedJobIds.Contains(jobId))
        {
            _activeRotation = null;
            return false;
        }

        // Try to create via factory
        if (_factories.TryGetValue(jobId, out var factory))
        {
            try
            {
                _activeRotation = factory();
            }
            catch (Exception ex)
            {
                _log?.Error(ex, "Failed to create rotation for job {JobId}; will not retry until job changes", jobId);
                _failedJobIds.Add(jobId);
                _activeRotation = null;
                return false;
            }

            // Cache by all supported job IDs (e.g., WHM and CNJ share same rotation)
            foreach (var supportedJobId in _activeRotation.SupportedJobIds)
            {
                _rotations[supportedJobId] = _activeRotation;
            }

            _uniqueRotations.Add(_activeRotation);

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
            _uniqueRotations.Add(rotation);
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
        foreach (var rotation in _uniqueRotations)
        {
            if (rotation is IDisposable disposable)
                disposable.Dispose();
        }
        _rotations.Clear();
        _factories.Clear();
        _uniqueRotations.Clear();
        _activeRotation = null;
    }
}
