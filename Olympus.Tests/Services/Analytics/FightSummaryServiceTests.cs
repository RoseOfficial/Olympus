using System;
using System.Collections.Generic;
using Moq;
using Olympus.Config;
using Olympus.Models;
using Olympus.Services;
using Olympus.Services.Analytics;
using Xunit;

namespace Olympus.Tests.Services.Analytics;

public class FightSummaryServiceTests
{
    private readonly Mock<IPerformanceTracker> _performanceTracker;
    private readonly Mock<IActionTracker> _actionTracker;
    private readonly Mock<IBurstWindowService> _burstWindowService;
    private readonly Configuration _configuration;

    public FightSummaryServiceTests()
    {
        _performanceTracker = new Mock<IPerformanceTracker>();
        _actionTracker = new Mock<IActionTracker>();
        _burstWindowService = new Mock<IBurstWindowService>();

        _actionTracker.Setup(a => a.GetHistory()).Returns(Array.Empty<ActionAttempt>());
        _actionTracker.Setup(a => a.GetGcdUptime()).Returns(85f);
        _performanceTracker.Setup(p => p.GetCooldownAnalysis()).Returns(Array.Empty<CooldownAnalysis>());
        _burstWindowService.Setup(b => b.BurstWindowHistory).Returns(Array.Empty<(DateTime, DateTime)>());

        _configuration = new Configuration();
        _configuration.Analytics.SummaryMinimumDurationSeconds = 60;
        _configuration.Analytics.MaxStoredSummaries = 10;
    }

    private FightSummaryService CreateService()
    {
        return new FightSummaryService(
            _performanceTracker.Object,
            _actionTracker.Object,
            _burstWindowService.Object,
            null,
            _configuration);
    }

    private static FightSession CreateSession(float durationSeconds, uint jobId = 24)
    {
        var start = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        return new FightSession
        {
            StartTime = start,
            EndTime = start.AddSeconds(durationSeconds),
            JobId = jobId,
            ZoneName = "Test Zone",
            FinalMetrics = new CombatMetricsSnapshot
            {
                PersonalDps = 12000f,
                Deaths = 0,
                NearDeaths = 0,
                GcdUptime = 85f,
                CombatDuration = durationSeconds,
            },
        };
    }

    // -------------------------------------------------------------------------
    // Long fight fires OnSummaryReady
    // -------------------------------------------------------------------------

    [Fact]
    public void OnSessionCompleted_LongFight_FiresOnSummaryReady()
    {
        using var service = CreateService();
        FightSummaryRecord? received = null;
        service.OnSummaryReady += record => received = record;

        var session = CreateSession(120f);
        _performanceTracker.Raise(p => p.OnSessionCompleted += null, session);

        Assert.NotNull(received);
        Assert.Equal(session.JobId, received!.JobId);
        Assert.Equal("Test Zone", received.ZoneName);
        Assert.Equal(12000f, received.EstimatedDps);
        Assert.NotNull(received.Grade);
        Assert.Single(service.RecentSummaries);
    }

    // -------------------------------------------------------------------------
    // Short fight does NOT fire
    // -------------------------------------------------------------------------

    [Fact]
    public void OnSessionCompleted_ShortFight_DoesNotFire()
    {
        using var service = CreateService();
        FightSummaryRecord? received = null;
        service.OnSummaryReady += record => received = record;

        var session = CreateSession(30f); // below 60s minimum
        _performanceTracker.Raise(p => p.OnSessionCompleted += null, session);

        Assert.Null(received);
        Assert.Empty(service.RecentSummaries);
    }

    // -------------------------------------------------------------------------
    // History capped at MaxStoredSummaries
    // -------------------------------------------------------------------------

    [Fact]
    public void RecentSummaries_CappedAtMaxStoredSummaries()
    {
        _configuration.Analytics.MaxStoredSummaries = 5;
        using var service = CreateService();

        for (var i = 0; i < 8; i++)
        {
            var session = CreateSession(120f + i);
            _performanceTracker.Raise(p => p.OnSessionCompleted += null, session);
        }

        Assert.Equal(5, service.RecentSummaries.Count);
    }

    // -------------------------------------------------------------------------
    // Most recent summary is first in list
    // -------------------------------------------------------------------------

    [Fact]
    public void RecentSummaries_MostRecentIsFirst()
    {
        using var service = CreateService();

        var session1 = CreateSession(120f, jobId: 24);
        var session2 = CreateSession(180f, jobId: 25);

        _performanceTracker.Raise(p => p.OnSessionCompleted += null, session1);
        _performanceTracker.Raise(p => p.OnSessionCompleted += null, session2);

        Assert.Equal(2, service.RecentSummaries.Count);
        Assert.Equal(25u, service.RecentSummaries[0].JobId);
        Assert.Equal(24u, service.RecentSummaries[1].JobId);
    }

    // -------------------------------------------------------------------------
    // Dispose unsubscribes from event
    // -------------------------------------------------------------------------

    [Fact]
    public void Dispose_UnsubscribesFromEvent()
    {
        var service = CreateService();
        FightSummaryRecord? received = null;
        service.OnSummaryReady += record => received = record;

        service.Dispose();

        var session = CreateSession(120f);
        _performanceTracker.Raise(p => p.OnSessionCompleted += null, session);

        Assert.Null(received);
    }

    // -------------------------------------------------------------------------
    // FFLogs percentile is null (deferred)
    // -------------------------------------------------------------------------

    [Fact]
    public void OnSessionCompleted_FflogsPercentile_IsNull()
    {
        using var service = CreateService();
        FightSummaryRecord? received = null;
        service.OnSummaryReady += record => received = record;

        var session = CreateSession(120f);
        _performanceTracker.Raise(p => p.OnSessionCompleted += null, session);

        Assert.NotNull(received);
        Assert.Null(received!.FflogsPercentile);
    }

    // -------------------------------------------------------------------------
    // Deaths are propagated from FinalMetrics
    // -------------------------------------------------------------------------

    [Fact]
    public void OnSessionCompleted_PropagatesDeathCount()
    {
        using var service = CreateService();
        FightSummaryRecord? received = null;
        service.OnSummaryReady += record => received = record;

        var start = new DateTime(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);
        var session = new FightSession
        {
            StartTime = start,
            EndTime = start.AddSeconds(120),
            JobId = 24,
            ZoneName = "Test Zone",
            FinalMetrics = new CombatMetricsSnapshot
            {
                PersonalDps = 10000f,
                Deaths = 3,
                CombatDuration = 120f,
            },
        };

        _performanceTracker.Raise(p => p.OnSessionCompleted += null, session);

        Assert.NotNull(received);
        Assert.Equal(3, received!.DeathCount);
    }
}
