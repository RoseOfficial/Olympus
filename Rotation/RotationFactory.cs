using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Dalamud.Plugin.Services;
using Olympus.Services;

namespace Olympus.Rotation;

/// <summary>
/// Factory for creating and registering rotation instances.
/// Supports both attribute-based auto-discovery and manual registration.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// var factory = new RotationFactory(services, log);
/// factory.DiscoverAndRegisterFactories(rotationManager);
/// </code>
/// </remarks>
public sealed class RotationFactory
{
    private readonly ServiceContainer _services;
    private readonly IPluginLog _log;
    private readonly HashSet<IRotation> _createdRotations = new();

    /// <summary>
    /// Gets all rotations created by this factory.
    /// </summary>
    public IReadOnlyCollection<IRotation> CreatedRotations => _createdRotations;

    /// <summary>
    /// Creates a new rotation factory.
    /// </summary>
    /// <param name="services">Service container for dependency injection.</param>
    /// <param name="log">Logger for diagnostic output.</param>
    public RotationFactory(ServiceContainer services, IPluginLog log)
    {
        _services = services;
        _log = log;
    }

    /// <summary>
    /// Discovers all rotation classes and registers factories (preserves lazy loading).
    /// </summary>
    /// <param name="manager">The rotation manager to register factories with.</param>
    /// <returns>Number of rotation factories registered.</returns>
    public int DiscoverAndRegisterFactories(RotationManager manager)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var rotationTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<RotationAttribute>() != null)
            .Where(t => typeof(IRotation).IsAssignableFrom(t))
            .Where(t => !t.IsAbstract && !t.IsInterface);

        int count = 0;
        foreach (var type in rotationTypes)
        {
            try
            {
                var attr = type.GetCustomAttribute<RotationAttribute>()!;
                var capturedType = type;

                foreach (var jobId in attr.JobIds)
                {
                    manager.RegisterFactory(jobId, () => CreateRotationAndTrack(capturedType));
                }

                count++;
                _log.Debug("Registered factory for rotation {Name} (jobs: {Jobs})",
                    attr.Name, string.Join(", ", attr.JobIds));
                ValidateResolvability(type);
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to register factory for {Type}", type.Name);
            }
        }

        _log.Information("Discovered and registered {Count} rotation factories", count);
        return count;
    }

    /// <summary>
    /// Startup sanity check: logs an error immediately if a discovered rotation has no
    /// constructor whose required parameters all resolve from the container. Without
    /// this, a missing service surfaces only at first job-switch as a cached warning.
    /// </summary>
    internal void ValidateResolvability(Type rotationType)
    {
        List<string>? bestMissing = null;
        foreach (var ctor in rotationType.GetConstructors()
                     .OrderByDescending(c => c.GetParameters().Length))
        {
            var missing = new List<string>();
            foreach (var p in ctor.GetParameters())
            {
                if (!p.HasDefaultValue && ResolveService(p.ParameterType) is null)
                    missing.Add(p.ParameterType.Name);
            }
            if (missing.Count == 0)
                return; // satisfiable constructor found
            if (bestMissing == null || missing.Count < bestMissing.Count)
                bestMissing = missing;
        }
        _log.Error("Rotation {Type} has no satisfiable constructor; missing services: {Missing}",
            rotationType.Name, string.Join(", ", bestMissing ?? new List<string>()));
    }

    private IRotation CreateRotationAndTrack(Type rotationType)
    {
        var rotation = CreateRotation(rotationType)
            ?? throw new InvalidOperationException($"Failed to create rotation {rotationType.Name}");
        _createdRotations.Add(rotation);
        return rotation;
    }

    /// <summary>
    /// Creates a rotation instance using constructor injection from the service container.
    /// </summary>
    /// <param name="rotationType">The rotation type to instantiate.</param>
    /// <returns>The created rotation, or null if creation failed.</returns>
    public IRotation? CreateRotation(Type rotationType)
    {
        var constructors = rotationType.GetConstructors()
            .OrderByDescending(c => c.GetParameters().Length)
            .ToList();

        foreach (var constructor in constructors)
        {
            var parameters = constructor.GetParameters();
            var args = new object?[parameters.Length];
            bool canResolve = true;

            for (int i = 0; i < parameters.Length; i++)
            {
                var paramType = parameters[i].ParameterType;
                var resolved = ResolveService(paramType);

                if (resolved == null && !parameters[i].HasDefaultValue)
                {
                    canResolve = false;
                    _log.Warning("[RotationFactory] Cannot resolve required parameter '{ParamName}' ({ParamType}) for rotation '{RotationType}'. Rotation will not be registered.",
                        parameters[i].Name ?? "unknown", paramType.Name, rotationType.Name);
                    break;
                }

                args[i] = resolved ?? parameters[i].DefaultValue;
            }

            if (canResolve)
            {
                return (IRotation)constructor.Invoke(args);
            }
        }

        _log.Warning("No suitable constructor found for rotation {Type}", rotationType.Name);
        return null;
    }

    /// <summary>
    /// Creates a specific rotation type.
    /// </summary>
    /// <typeparam name="T">The rotation type.</typeparam>
    /// <returns>The created rotation.</returns>
    public T? Create<T>() where T : class, IRotation
    {
        return CreateRotation(typeof(T)) as T;
    }

    /// <summary>
    /// Manually registers a pre-created rotation.
    /// </summary>
    /// <param name="rotation">The rotation to track.</param>
    public void TrackRotation(IRotation rotation)
    {
        _createdRotations.Add(rotation);
    }

    /// <summary>
    /// Disposes all created rotations that implement IDisposable.
    /// </summary>
    public void DisposeRotations()
    {
        foreach (var rotation in _createdRotations)
        {
            if (rotation is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                }
                catch (Exception ex)
                {
                    _log.Error(ex, "Error disposing rotation {Name}", rotation.Name);
                }
            }
        }

        _createdRotations.Clear();
    }

    private object? ResolveService(Type serviceType)
    {
        return _services.TryGet(serviceType, out var service) ? service : null;
    }
}
