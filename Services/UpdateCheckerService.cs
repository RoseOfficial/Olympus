using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;

namespace Olympus.Services;

public enum UpdateCheckStatus
{
    Unknown,
    Checking,
    UpToDate,
    UpdateAvailable,
    Failed
}

/// <summary>
/// Checks for Olympus updates by fetching the remote repo.json and comparing versions.
/// Runs once at startup (delayed 15s) and on demand via CheckAsync().
/// </summary>
public sealed class UpdateCheckerService : IDisposable
{
    private const string RepoJsonUrl =
        "https://raw.githubusercontent.com/RoseOfficial/Olympus/main/repo.json";

    private readonly string _currentVersion;
    private readonly INotificationManager _notificationManager;
    private readonly IPluginLog _log;
    private readonly HttpClient _httpClient;
    private readonly CancellationTokenSource _cts;

    private bool _startupCheckDone;
    private readonly object _statusLock = new();

    public UpdateCheckStatus Status { get; private set; } = UpdateCheckStatus.Unknown;
    public string? LatestVersion { get; private set; }

    public UpdateCheckerService(
        string currentVersion,
        INotificationManager notificationManager,
        IPluginLog log)
    {
        _currentVersion = currentVersion;
        _notificationManager = notificationManager;
        _log = log;
        _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _cts = new CancellationTokenSource();
    }

    /// <summary>
    /// Fires a background task that waits 15 seconds then checks for updates.
    /// Only runs once per session. Shows a Dalamud toast if a newer version is found.
    /// </summary>
    public void StartupCheck()
    {
        if (_startupCheckDone) return;
        _startupCheckDone = true;
        _ = RunStartupCheckAsync();
    }

    private async Task RunStartupCheckAsync()
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(15), _cts.Token).ConfigureAwait(false);
            var latest = await FetchLatestVersionAsync(_cts.Token).ConfigureAwait(false);
            if (latest != null && IsNewer(latest, _currentVersion))
            {
                Status = UpdateCheckStatus.UpdateAvailable;
                LatestVersion = latest;
                var displayVersion = Version.TryParse(latest, out var lv) && lv.Build >= 0 && lv.Revision <= 0
                    ? lv.ToString(3)
                    : latest;
                _notificationManager.AddNotification(new Notification
                {
                    Content = $"Olympus {displayVersion} is available. Update via /xlplugins.",
                    Title = "Olympus Update Available",
                    Type = NotificationType.Info,
                    Minimized = false,
                });
            }
            else if (latest != null)
            {
                Status = UpdateCheckStatus.UpToDate;
                LatestVersion = latest;
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _log.Debug($"Startup update check failed: {ex.Message}");
            Status = UpdateCheckStatus.Failed;
        }
    }

    /// <summary>
    /// Checks for updates on demand. No-ops if a check is already in progress.
    /// Updates Status and LatestVersion on completion.
    /// </summary>
    public async Task CheckAsync()
    {
        bool alreadyChecking;
        lock (_statusLock)
        {
            alreadyChecking = Status == UpdateCheckStatus.Checking;
            if (!alreadyChecking)
            {
                Status = UpdateCheckStatus.Checking;
                LatestVersion = null;
            }
        }
        if (alreadyChecking) return;

        try
        {
            var latest = await FetchLatestVersionAsync(_cts.Token).ConfigureAwait(false);
            LatestVersion = latest;
            Status = latest != null && IsNewer(latest, _currentVersion)
                ? UpdateCheckStatus.UpdateAvailable
                : UpdateCheckStatus.UpToDate;
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _log.Debug($"Update check failed: {ex.Message}");
            Status = UpdateCheckStatus.Failed;
        }
    }

    private async Task<string?> FetchLatestVersionAsync(CancellationToken ct)
    {
        using var response = await _httpClient.GetAsync(RepoJsonUrl, ct).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var json = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        if (root.ValueKind == JsonValueKind.Array && root.GetArrayLength() > 0)
            return root[0].GetProperty("AssemblyVersion").GetString();
        return null;
    }

    /// <summary>
    /// Version comparison with part-count normalization (internal for tests).
    /// repo.json's AssemblyVersion is 4-part ("4.17.2.0", padded by the release
    /// workflow for Dalamud's installer string-compare) while PluginVersion is
    /// 3-part ("4.17.2"). System.Version treats a missing component as -1, so a
    /// naive compare says 4.17.2.0 > 4.17.2 and toasts a phantom update on every
    /// login. Normalize missing components to 0 before comparing.
    /// </summary>
    internal static bool IsNewer(string latest, string current) =>
        Version.TryParse(latest, out var l) &&
        Version.TryParse(current, out var c) &&
        Normalize(l) > Normalize(c);

    private static Version Normalize(Version v) =>
        new(v.Major, v.Minor, Math.Max(v.Build, 0), Math.Max(v.Revision, 0));

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
        _httpClient.Dispose();
    }
}
