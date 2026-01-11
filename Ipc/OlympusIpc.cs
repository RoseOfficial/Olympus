using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Ipc;
using Dalamud.Plugin.Services;

namespace Olympus.Ipc;

/// <summary>
/// Provides IPC interface for external plugins to interact with Olympus.
/// Follows the pattern used by RotationSolverReborn for simple enable/disable control.
/// </summary>
public sealed class OlympusIpc : IDisposable
{
    private readonly Configuration _configuration;
    private readonly Action _saveConfiguration;
    private readonly IPluginLog _log;

    private readonly ICallGateProvider<object> _test;
    private readonly ICallGateProvider<bool, object> _setEnabled;
    private readonly ICallGateProvider<bool> _isEnabled;

    public OlympusIpc(
        IDalamudPluginInterface pluginInterface,
        Configuration configuration,
        Action saveConfiguration,
        IPluginLog log)
    {
        _configuration = configuration;
        _saveConfiguration = saveConfiguration;
        _log = log;

        // Test endpoint - allows external plugins to verify Olympus is available
        _test = pluginInterface.GetIpcProvider<object>("Olympus.Test");
        _test.RegisterAction(Test);

        // SetEnabled endpoint - allows external plugins to enable/disable rotation
        _setEnabled = pluginInterface.GetIpcProvider<bool, object>("Olympus.SetEnabled");
        _setEnabled.RegisterAction(SetEnabled);

        // IsEnabled endpoint - allows external plugins to check if rotation is enabled
        _isEnabled = pluginInterface.GetIpcProvider<bool>("Olympus.IsEnabled");
        _isEnabled.RegisterFunc(IsEnabled);

        _log.Info("Olympus IPC initialized");
    }

    /// <summary>
    /// Test endpoint for availability checks. Does nothing but confirms IPC is working.
    /// </summary>
    private void Test()
    {
        // No-op - just validates IPC is callable
    }

    /// <summary>
    /// Enables or disables the Olympus rotation.
    /// </summary>
    /// <param name="enabled">True to enable, false to disable.</param>
    private void SetEnabled(bool enabled)
    {
        if (_configuration.Enabled != enabled)
        {
            _configuration.Enabled = enabled;
            _saveConfiguration();
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

    public void Dispose()
    {
        _test.UnregisterAction();
        _setEnabled.UnregisterAction();
        _isEnabled.UnregisterFunc();
        _log.Info("Olympus IPC disposed");
    }
}
