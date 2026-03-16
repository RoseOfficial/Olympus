using System;
using System.Collections.Generic;
using System.IO;
using Olympus.Services.Analytics;
using Xunit;

namespace Olympus.Tests.Services;

public class PerformanceTrackerPersistenceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _sessionsFile;

    public PerformanceTrackerPersistenceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(_tempDir);
        _sessionsFile = Path.Combine(_tempDir, "sessions.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private static FightSession MakeSession(DateTime start, float overallScore = 80f) =>
        new FightSession
        {
            StartTime = start,
            EndTime = start.AddSeconds(60),
            JobId = 24,
            ZoneName = "The Copied Factory",
            FinalMetrics = new CombatMetricsSnapshot
            {
                CombatDuration = 60f, GcdUptime = 90f, PersonalDps = 5000f,
                TotalDamage = 300000, TotalHealing = 0, OverhealPercent = 0f,
                Deaths = 0, NearDeaths = 0,
                Cooldowns = new List<CooldownUsage>(),
                Timestamp = start.AddSeconds(60)
            },
            Score = new PerformanceScore
            {
                Overall = overallScore, GcdUptime = 88f,
                CooldownEfficiency = 75f, HealingEfficiency = 100f, Survival = 100f
            },
            Issues = new List<PerformanceIssue>()
        };

    [Fact]
    public void SaveSessions_WritesJsonFileToDisk()
    {
        var session = MakeSession(DateTime.Now.AddMinutes(-5));
        SessionPersistence.Save(_sessionsFile, new List<FightSession> { session });
        Assert.True(File.Exists(_sessionsFile));
        Assert.Contains(session.Id.ToString(), File.ReadAllText(_sessionsFile));
    }

    [Fact]
    public void LoadSessions_ReturnsEmptyList_WhenFileDoesNotExist()
    {
        var result = SessionPersistence.Load(_sessionsFile);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void LoadSessions_ReturnsEmptyList_WhenFileIsCorrupt()
    {
        File.WriteAllText(_sessionsFile, "{ not valid json }{{{");
        var result = SessionPersistence.Load(_sessionsFile);
        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsSessionData()
    {
        var start = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Local);
        var session = MakeSession(start, overallScore: 92.5f);
        SessionPersistence.Save(_sessionsFile, new List<FightSession> { session });
        var loaded = SessionPersistence.Load(_sessionsFile);
        Assert.Single(loaded);
        Assert.Equal(session.Id, loaded[0].Id);
        Assert.Equal(session.JobId, loaded[0].JobId);
        Assert.Equal(session.ZoneName, loaded[0].ZoneName);
        Assert.Equal(session.StartTime, loaded[0].StartTime);
        Assert.Equal(92.5f, loaded[0].Score!.Overall, precision: 2);
        Assert.Equal(90f, loaded[0].FinalMetrics!.GcdUptime, precision: 2);
    }

    [Fact]
    public void SaveThenLoad_RoundTripsMultipleSessions()
    {
        var sessions = new List<FightSession>
        {
            MakeSession(DateTime.Now.AddMinutes(-30), 95f),
            MakeSession(DateTime.Now.AddMinutes(-20), 85f),
            MakeSession(DateTime.Now.AddMinutes(-10), 75f),
        };
        SessionPersistence.Save(_sessionsFile, sessions);
        Assert.Equal(3, SessionPersistence.Load(_sessionsFile).Count);
    }

    [Fact]
    public void SaveThenLoad_PreservesIssuesList()
    {
        var session = new FightSession
        {
            StartTime = DateTime.Now.AddMinutes(-5),
            EndTime = DateTime.Now,
            JobId = 24, ZoneName = "Test",
            FinalMetrics = MakeSession(DateTime.Now.AddMinutes(-5)).FinalMetrics,
            Score = new PerformanceScore { Overall = 80f },
            Issues = new List<PerformanceIssue>
            {
                new PerformanceIssue
                {
                    Type = IssueType.GcdDowntime,
                    Severity = IssueSeverity.Warning,
                    Description = "GCD uptime was 78.0%",
                    Suggestion = "Try to always be casting"
                }
            }
        };
        SessionPersistence.Save(_sessionsFile, new List<FightSession> { session });
        var loaded = SessionPersistence.Load(_sessionsFile);
        Assert.Single(loaded[0].Issues);
        Assert.Equal(IssueType.GcdDowntime, loaded[0].Issues[0].Type);
    }

    [Fact]
    public void Load_HandlesEmptyArray()
    {
        File.WriteAllText(_sessionsFile, "[]");
        Assert.Empty(SessionPersistence.Load(_sessionsFile));
    }

    [Fact]
    public void SaveThenLoad_PreservesCooldownUsage()
    {
        var start = DateTime.Now.AddMinutes(-5);
        var session = new FightSession
        {
            StartTime = start, EndTime = start.AddSeconds(60),
            JobId = 24, ZoneName = "Test",
            FinalMetrics = new CombatMetricsSnapshot
            {
                CombatDuration = 60f, GcdUptime = 90f, Cooldowns = new List<CooldownUsage>
                {
                    new CooldownUsage
                    {
                        ActionId = 7561u, Name = "Presence of Mind",
                        CooldownDuration = 120f, TimesUsed = 1, OptimalUses = 1,
                        AverageDrift = 2.3f, DriftValues = new List<float> { 2.3f }
                    }
                },
                Timestamp = start.AddSeconds(60)
            },
            Score = new PerformanceScore { Overall = 80f },
            Issues = new List<PerformanceIssue>()
        };
        SessionPersistence.Save(_sessionsFile, new List<FightSession> { session });
        var loaded = SessionPersistence.Load(_sessionsFile);
        Assert.Single(loaded[0].FinalMetrics!.Cooldowns);
        Assert.Equal(7561u, loaded[0].FinalMetrics!.Cooldowns[0].ActionId);
        Assert.Equal(2.3f, loaded[0].FinalMetrics!.Cooldowns[0].AverageDrift, precision: 2);
    }
}
