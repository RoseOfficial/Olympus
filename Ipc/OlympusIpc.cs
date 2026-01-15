using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;
using Olympus.Rotation;

namespace Olympus.Ipc;

/// <summary>
/// Provides IPC interface for external plugins to interact with Olympus.
/// </summary>
/// <remarks>
/// Available IPC endpoints:
/// - Olympus.Test: Availability check (no args, no return)
/// - Olympus.IsEnabled: Check if rotation is enabled (returns bool)
/// - Olympus.SetEnabled: Enable/disable rotation (takes bool)
/// - Olympus.GetVersion: Get plugin version (returns string)
/// - Olympus.GetActiveRotation: Get active rotation name (returns string or empty)
/// - Olympus.GetSupportedJobs: Get array of supported job IDs (returns uint[])
/// - Olympus.OnStateChanged: Event fired when enabled state changes
/// </remarks>
public sealed class OlympusIpc : IDisposable
{
    private readonly Configuration _configuration;
    private readonly Action _saveConfiguration;
    private readonly IPluginLog _log;
    private readonly Func<RotationManager?> _getRotationManager;
    private readonly string _version;

    // Basic endpoints
    private readonly ICallGateProvider<object> _test;
    private readonly ICallGateProvider<bool, object> _setEnabled;
    private readonly ICallGateProvider<bool> _isEnabled;

    // Extended endpoints
    private readonly ICallGateProvider<string> _getVersion;
    private readonly ICallGateProvider<string> _getActiveRotation;
    private readonly ICallGateProvider<uint[]> _getSupportedJobs;

    // Events
    private readonly ICallGateProvider<bool, object> _onStateChanged;

    public OlympusIpc(
        IDalamudPluginInterface pluginInterface,
        Configuration configuration,
        Action saveConfiguration,
        IPluginLog log,
        string version = "",
        Func<RotationManager?>? getRotationManager = null)
    {
        _configuration = configuration;
        _saveConfiguration = saveConfiguration;
        _log = log;
        _version = string.IsNullOrEmpty(version) ? Plugin.PluginVersion : version;
        _getRotationManager = getRotationManager ?? (() => null);

        // Basic endpoints
        _test = pluginInterface.GetIpcProvider<object>("Olympus.Test");
        _test.RegisterAction(Test);

        _setEnabled = pluginInterface.GetIpcProvider<bool, object>("Olympus.SetEnabled");
        _setEnabled.RegisterAction(SetEnabled);

        _isEnabled = pluginInterface.GetIpcProvider<bool>("Olympus.IsEnabled");
        _isEnabled.RegisterFunc(IsEnabled);

        // Extended endpoints
        _getVersion = pluginInterface.GetIpcProvider<string>("Olympus.GetVersion");
        _getVersion.RegisterFunc(GetVersion);

        _getActiveRotation = pluginInterface.GetIpcProvider<string>("Olympus.GetActiveRotation");
        _getActiveRotation.RegisterFunc(GetActiveRotation);

        _getSupportedJobs = pluginInterface.GetIpcProvider<uint[]>("Olympus.GetSupportedJobs");
        _getSupportedJobs.RegisterFunc(GetSupportedJobs);

        // Event providers
        _onStateChanged = pluginInterface.GetIpcProvider<bool, object>("Olympus.OnStateChanged");

        _log.Info("Olympus IPC initialized (v{0})", _version);
    }

    #region Basic Endpoints

    /// <summary>
    /// Test endpoint for availability checks. Does nothing but confirms IPC is working.
    /// </summary>
    private void Test()
    {
        // No-op - just validates IPC is callable
    }

    /// <summary>
    /// Enables or disables the Olympus rotation.
    /// Fires OnStateChanged event when state changes.
    /// </summary>
    /// <param name="enabled">True to enable, false to disable.</param>
    private void SetEnabled(bool enabled)
    {
        if (_configuration.Enabled != enabled)
        {
            _configuration.Enabled = enabled;
            _saveConfiguration();

            // Fire state changed event
            try
            {
                _onStateChanged.SendMessage(enabled);
            }
            catch (Exception ex)
            {
                _log.Warning(ex, "Failed to send OnStateChanged event");
            }

            var status = enabled ? "enabled" : "disabled";
            _log.Info($"Olympus {status} via IPC");
        }
    }

    /// <summary>
    /// Returns whether Olympus rotation is currently enabled.
    /// </summary>
    private bool IsEnabled()
    {
        return _configuration.Enabled;
    }

    #endregion

    #region Extended Endpoints

    /// <summary>
    /// Returns the current plugin version.
    /// </summary>
    private string GetVersion()
    {
        return _version;
    }

    /// <summary>
    /// Returns the name of the currently active rotation, or empty string if none.
    /// </summary>
    private string GetActiveRotation()
    {
        var manager = _getRotationManager();
        return manager?.ActiveRotation?.Name ?? string.Empty;
    }

    /// <summary>
    /// Returns an array of all supported job IDs.
    /// </summary>
    private uint[] GetSupportedJobs()
    {
        var manager = _getRotationManager();
        if (manager == null)
            return Array.Empty<uint>();

        var jobs = new System.Collections.Generic.List<uint>();
        foreach (var rotation in manager.RegisteredRotations)
        {
            foreach (var jobId in rotation.SupportedJobIds)
            {
                if (!jobs.Contains(jobId))
                    jobs.Add(jobId);
            }
        }
        return jobs.ToArray();
    }

    #endregion

    /// <summary>
    /// Notifies external plugins that the enabled state has changed.
    /// Call this when state changes from sources other than IPC (e.g., UI, command).
    /// </summary>
    public void NotifyStateChanged(bool enabled)
    {
        try
        {
            _onStateChanged.SendMessage(enabled);
        }
        catch (Exception ex)
        {
            _log.Warning(ex, "Failed to send OnStateChanged event");
        }
    }

    public void Dispose()
    {
        _test.UnregisterAction();
        _setEnabled.UnregisterAction();
        _isEnabled.UnregisterFunc();
        _getVersion.UnregisterFunc();
        _getActiveRotation.UnregisterFunc();
        _getSupportedJobs.UnregisterFunc();
        _log.Info("Olympus IPC disposed");
    }
}
