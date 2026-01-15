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
/// factory.DiscoverAndRegister(rotationManager);
/// </code>
/// </remarks>
public sealed class RotationFactory
{
    private readonly ServiceContainer _services;
    private readonly IPluginLog _log;
    private readonly List<IRotation> _createdRotations = new();

    /// <summary>
    /// Gets all rotations created by this factory.
    /// </summary>
    public IReadOnlyList<IRotation> CreatedRotations => _createdRotations;

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
    /// Discovers all rotation classes with [Rotation] attribute and registers them.
    /// </summary>
    /// <param name="manager">The rotation manager to register with.</param>
    /// <returns>Number of rotations registered.</returns>
    public int DiscoverAndRegister(RotationManager manager)
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
                var rotation = CreateRotation(type);
                if (rotation != null)
                {
                    manager.Register(rotation);
                    _createdRotations.Add(rotation);
                    count++;

                    var attr = type.GetCustomAttribute<RotationAttribute>()!;
                    _log.Debug("Registered rotation {Name} for jobs: {Jobs}",
                        attr.Name, string.Join(", ", attr.JobIds));
                }
            }
            catch (Exception ex)
            {
                _log.Error(ex, "Failed to create rotation {Type}", type.Name);
            }
        }

        _log.Information("Discovered and registered {Count} rotations", count);
        return count;
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
                    _log.Debug("Cannot resolve parameter {Param} of type {Type} for {Rotation}",
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
        // Use reflection to call TryGet<T> with the correct type
        var method = typeof(ServiceContainer)
            .GetMethod(nameof(ServiceContainer.TryGet))!
            .MakeGenericMethod(serviceType);

        var args = new object?[] { null };
        var result = (bool)method.Invoke(_services, args)!;

        return result ? args[0] : null;
    }
}
