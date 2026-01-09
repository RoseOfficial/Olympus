using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;

namespace Olympus.Services;

/// <summary>
/// Simple anonymous telemetry service that pings a configured endpoint on plugin load.
/// Sends only plugin version - no personal data, character info, or identifiers.
/// </summary>
public sealed class TelemetryService : IDisposable
{
    private readonly Configuration _configuration;
    private readonly IPluginLog _log;
    private readonly HttpClient _httpClient;
    private readonly CancellationTokenSource _cts;
    private bool _hasSentPing;

    public TelemetryService(Configuration configuration, IPluginLog log)
    {
        _configuration = configuration;
        _log = log;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Sends an anonymous ping to the telemetry endpoint if enabled and configured.
    /// This is fire-and-forget - failures are silently ignored.
    /// </summary>
    public void SendStartupPing(string version)
    {
        // Only send once per session
        if (_hasSentPing)
            return;

        // Check if telemetry is enabled
        if (!_configuration.TelemetryEnabled)
            return;

        // Check if endpoint is configured
        if (string.IsNullOrWhiteSpace(_configuration.TelemetryEndpoint))
            return;

        _hasSentPing = true;

        // Fire and forget - don't await, don't block
        _ = SendPingAsync(version);
    }

    private async Task SendPingAsync(string version)
    {
        try
        {
            var url = $"{_configuration.TelemetryEndpoint.TrimEnd('/')}?v={Uri.EscapeDataString(version)}";
            using var response = await _httpClient.GetAsync(url, _cts.Token).ConfigureAwait(false);
            // We don't care about the response - just that it was sent
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            // Silently ignore network errors - telemetry should never impact user experience
            _log.Debug($"Telemetry ping failed (this is fine): {ex.Message}");
        }
        catch (Exception ex)
        {
            // Log unexpected errors for debugging but don't throw
            _log.Debug($"Unexpected telemetry error: {ex.Message}");
        }
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _httpClient.Dispose();
    }
}
