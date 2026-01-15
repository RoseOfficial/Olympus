using System;
using System.Collections.Generic;

namespace Olympus.Services;

/// <summary>
/// Lightweight service container for dependency injection.
/// Provides simple singleton registration and resolution.
/// </summary>
/// <remarks>
/// Usage:
/// <code>
/// var services = new ServiceContainer();
/// services.Register&lt;IActionService&gt;(new ActionService(...));
/// var action = services.Get&lt;IActionService&gt;();
/// </code>
/// </remarks>
public sealed class ServiceContainer : IDisposable
{
    private readonly Dictionary<Type, object> _services = new();
    private readonly List<IDisposable> _disposables = new();
    private bool _disposed;

    /// <summary>
    /// Registers a service instance by its concrete type.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="instance">The service instance.</param>
    /// <returns>This container for fluent chaining.</returns>
    public ServiceContainer Register<T>(T instance) where T : class
    {
        ArgumentNullException.ThrowIfNull(instance);
        _services[typeof(T)] = instance;

        if (instance is IDisposable disposable)
            _disposables.Add(disposable);

        return this;
    }

    /// <summary>
    /// Registers a service instance by interface and implementation.
    /// </summary>
    /// <typeparam name="TInterface">The interface type.</typeparam>
    /// <typeparam name="TImpl">The implementation type.</typeparam>
    /// <param name="instance">The service instance.</param>
    /// <returns>This container for fluent chaining.</returns>
    public ServiceContainer Register<TInterface, TImpl>(TImpl instance)
        where TInterface : class
        where TImpl : class, TInterface
    {
        ArgumentNullException.ThrowIfNull(instance);
        _services[typeof(TInterface)] = instance;
        _services[typeof(TImpl)] = instance;

        if (instance is IDisposable disposable && !_disposables.Contains(disposable))
            _disposables.Add(disposable);

        return this;
    }

    /// <summary>
    /// Gets a registered service by type.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>The service instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if service is not registered.</exception>
    public T Get<T>() where T : class
    {
        if (_services.TryGetValue(typeof(T), out var service))
            return (T)service;

        throw new InvalidOperationException($"Service {typeof(T).Name} is not registered.");
    }

    /// <summary>
    /// Tries to get a registered service by type.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <param name="service">The service instance if found.</param>
    /// <returns>True if the service was found.</returns>
    public bool TryGet<T>(out T? service) where T : class
    {
        if (_services.TryGetValue(typeof(T), out var obj))
        {
            service = (T)obj;
            return true;
        }

        service = null;
        return false;
    }

    /// <summary>
    /// Checks if a service is registered.
    /// </summary>
    /// <typeparam name="T">The service type.</typeparam>
    /// <returns>True if the service is registered.</returns>
    public bool IsRegistered<T>() where T : class => _services.ContainsKey(typeof(T));

    /// <summary>
    /// Gets all registered services.
    /// </summary>
    public IEnumerable<object> AllServices => _services.Values;

    /// <summary>
    /// Disposes all registered disposable services.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        // Dispose in reverse order of registration
        for (int i = _disposables.Count - 1; i >= 0; i--)
        {
            try
            {
                _disposables[i].Dispose();
            }
            catch
            {
                // Best effort disposal - don't let one failure stop others
            }
        }

        _disposables.Clear();
        _services.Clear();
    }
}
