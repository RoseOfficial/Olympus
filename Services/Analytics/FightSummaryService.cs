using System;
using System.Collections.Generic;
using System.Linq;
using Olympus.Models;
using Olympus.Services.FFLogs;

namespace Olympus.Services.Analytics;

/// <summary>
/// Orchestrator that listens for completed fight sessions and produces
/// post-fight coaching summaries with callouts, grades, and metrics.
/// </summary>
public sealed class FightSummaryService : IFightSummaryService, IDisposable
{
    private readonly IPerformanceTracker _performanceTracker;
    private readonly IActionTracker _actionTracker;
    private readonly IBurstWindowService _burstWindowService;
    private readonly IFFlogsService? _fflogsService;
    private readonly Configuration _configuration;

    private readonly List<FightSummaryRecord> _recentSummaries = new();
    private readonly object _summaryLock = new();

    public event Action<FightSummaryRecord>? OnSummaryReady;

    public IReadOnlyList<FightSummaryRecord> RecentSummaries
    {
        get { lock (_summaryLock) { return _recentSummaries.ToList(); } }
    }

    public FightSummaryService(
        IPerformanceTracker performanceTracker,
        IActionTracker actionTracker,
        IBurstWindowService burstWindowService,
        IFFlogsService? fflogsService,
        Configuration configuration)
    {
        _performanceTracker = performanceTracker;
        _actionTracker = actionTracker;
        _burstWindowService = burstWindowService;
        _fflogsService = fflogsService;
        _configuration = configuration;

        _performanceTracker.OnSessionCompleted += OnSessionCompleted;
    }

    private void OnSessionCompleted(FightSession session)
    {
        if (session.Duration < _configuration.Analytics.SummaryMinimumDurationSeconds)
            return;

        // Snapshot data from existing services.
        var history = _actionTracker.GetHistory();
        var cooldowns = _performanceTracker.GetCooldownAnalysis();
        var burstWindows = _burstWindowService.BurstWindowHistory;
        var gcdUptime = _actionTracker.GetGcdUptime();
        var unableToActWindows = _performanceTracker.GetUnableToActWindows();

        // Generate callouts.
        var callouts = FightSummaryCalloutEngine.Generate(
            session, session.JobId, history, cooldowns, burstWindows, gcdUptime, unableToActWindows);

        // Compute average major cooldown drift (120s+ recast, used >= 2 times).
        var majorCooldowns = cooldowns
            .Where(cd => cd.CooldownDuration >= 120f && cd.TimesUsed >= 2)
            .ToList();

        var avgDrift = majorCooldowns.Count > 0
            ? majorCooldowns.Average(cd => cd.AverageDrift)
            : 0f;

        // Compute grade.
        var deaths = session.FinalMetrics?.Deaths ?? 0;
        var grade = FightSummaryCalloutEngine.ComputeGrade(gcdUptime, avgDrift, 100f, deaths);

        // Use already-computed DPS from the session.
        var estimatedDps = session.FinalMetrics?.PersonalDps ?? 0f;

        // FFLogs percentile: deferred (zone-to-encounter mapping not yet implemented).
        int? fflogsPercentile = null;

        var record = new FightSummaryRecord
        {
            Timestamp = DateTime.Now,
            JobId = session.JobId,
            ZoneName = session.ZoneName,
            Duration = session.EndTime - session.StartTime,
            GcdUptimePercent = gcdUptime,
            EstimatedDps = estimatedDps,
            FflogsPercentile = fflogsPercentile,
            Grade = grade,
            DeathCount = deaths,
            Callouts = callouts,
        };

        lock (_summaryLock)
        {
            _recentSummaries.Insert(0, record);

            var max = _configuration.Analytics.MaxStoredSummaries;
            while (_recentSummaries.Count > max)
                _recentSummaries.RemoveAt(_recentSummaries.Count - 1);
        }

        OnSummaryReady?.Invoke(record);
    }

    public void Dispose()
    {
        _performanceTracker.OnSessionCompleted -= OnSessionCompleted;
    }
}
