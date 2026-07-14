using System;
using System.Collections.Generic;
using System.Reflection;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Config;
using Olympus.Services;
using Olympus.Services.Analytics;
using Xunit;

namespace Olympus.Tests.Services.Analytics;

/// <summary>
/// Tests for <see cref="PerformanceTracker.GetCooldownAnalysis"/> and its
/// internal session-conversion helper.
///
/// Bug C1: <c>GetCooldownAnalysis</c> was reading from the live
/// <c>cooldownStates</c> dictionary even when out of combat.  That dictionary
/// is cleared on <c>OnCombatStart</c>, so the analytics cooldown tab showed
/// zeros between fights.  The fix makes the method branch on
/// <c>IsTracking</c>: live state while in combat, session snapshot after.
/// </summary>
public class PerformanceTrackerCooldownAnalysisTests
{
    private const uint ActionId1 = 1001u;

    // -------------------------------------------------------------------------
    // Helpers
    // -------------------------------------------------------------------------

    /// <summary>
    /// Creates a <see cref="PerformanceTracker"/> with all Dalamud dependencies mocked
    /// (loose, no setup required) and minimal config.
    /// </summary>
    private static (PerformanceTracker tracker, AnalyticsConfig config) CreateTracker()
    {
        var config = new AnalyticsConfig { EnableTracking = true };

        var combatEvents = new Mock<ICombatEventService>(MockBehavior.Loose);
        combatEvents.Setup(c => c.IsInCombat).Returns(false);

        var actionTracker = new Mock<IActionTracker>(MockBehavior.Loose);
        var objectTable = new Mock<IObjectTable>(MockBehavior.Loose);
        var partyList = new Mock<IPartyList>(MockBehavior.Loose);
        partyList.Setup(p => p.GetEnumerator())
                 .Returns(new List<IPartyMember>().GetEnumerator());

        var log = new Mock<IPluginLog>(MockBehavior.Loose);
        var dataManager = new Mock<IDataManager>(MockBehavior.Loose);

        var tracker = new PerformanceTracker(
            config,
            actionTracker.Object,
            combatEvents.Object,
            objectTable.Object,
            partyList.Object,
            log.Object,
            dataManager.Object,
            partyCoordinationService: null,
            configDirectory: ""); // empty = no disk I/O

        return (tracker, config);
    }

    /// <summary>
    /// Injects a <see cref="FightSession"/> directly into the private
    /// <c>sessionHistory</c> linked list so tests can verify routing logic
    /// without having to drive the full 5-second minimum fight duration.
    /// No lock is needed here: test methods are single-threaded and the
    /// PerformanceTracker background work only runs when <c>Update()</c> is called.
    /// </summary>
    private static void InjectSession(PerformanceTracker tracker, FightSession session)
    {
        var historyField = typeof(PerformanceTracker)
            .GetField("sessionHistory", BindingFlags.NonPublic | BindingFlags.Instance)!;
        var history = (LinkedList<FightSession>)historyField.GetValue(tracker)!;
        history.AddFirst(session);
    }

    private static FightSession MakeSession(int timesUsed, uint actionId = ActionId1)
    {
        return new FightSession
        {
            StartTime = DateTime.UtcNow.AddMinutes(-5),
            EndTime = DateTime.UtcNow,
            JobId = 24,
            ZoneName = "Test",
            FinalMetrics = new CombatMetricsSnapshot
            {
                CombatDuration = 300f,
                GcdUptime = 90f,
                Cooldowns = new List<CooldownUsage>
                {
                    new()
                    {
                        ActionId = actionId,
                        Name = $"Action {actionId}",
                        CooldownDuration = 120f,
                        TimesUsed = timesUsed,
                        OptimalUses = 4,
                        AverageDrift = 1.5f,
                        DriftValues = new List<float> { 1f, 2f },
                    }
                }
            }
        };
    }

    // -------------------------------------------------------------------------
    // Pure unit tests for BuildCooldownAnalysisFromSession (the converter)
    // -------------------------------------------------------------------------

    [Fact]
    public void BuildCooldownAnalysisFromSession_SingleEntry_MapsFieldsCorrectly()
    {
        var cooldowns = new List<CooldownUsage>
        {
            new()
            {
                ActionId = ActionId1,
                Name = "Chain Stratagem",
                CooldownDuration = 120f,
                TimesUsed = 3,
                OptimalUses = 4,
                AverageDrift = 2.5f,
                DriftValues = new List<float> { 1f, 3f, 3.5f },
            }
        };

        var result = PerformanceTracker.BuildCooldownAnalysisFromSession(cooldowns);

        Assert.Single(result);
        var a = result[0];
        Assert.Equal(ActionId1, a.ActionId);
        Assert.Equal("Chain Stratagem", a.Name);
        Assert.Equal(120f, a.CooldownDuration);
        Assert.Equal(3, a.TimesUsed);
        Assert.Equal(4, a.OptimalUses);
        Assert.Equal(2.5f, a.AverageDrift, precision: 3);
        Assert.Equal(7.5f, a.TotalDriftSeconds, precision: 3); // sum of DriftValues
    }

    [Fact]
    public void BuildCooldownAnalysisFromSession_ZeroOptimalUses_Efficiency100Percent()
    {
        var cooldowns = new List<CooldownUsage>
        {
            new() { ActionId = ActionId1, OptimalUses = 0, TimesUsed = 1 }
        };

        var result = PerformanceTracker.BuildCooldownAnalysisFromSession(cooldowns);

        // OptimalUses == 0 → denominator is 0 → efficiency falls back to 100%
        Assert.Equal(100f, result[0].Efficiency, precision: 1);
    }

    [Fact]
    public void BuildCooldownAnalysisFromSession_EmptyList_ReturnsEmpty()
    {
        var result = PerformanceTracker.BuildCooldownAnalysisFromSession(new List<CooldownUsage>());
        Assert.Empty(result);
    }

    [Fact]
    public void BuildCooldownAnalysisFromSession_MultipleEntries_PreservesCount()
    {
        var cooldowns = new List<CooldownUsage>
        {
            new() { ActionId = 1u, TimesUsed = 2, OptimalUses = 3 },
            new() { ActionId = 2u, TimesUsed = 1, OptimalUses = 2 },
            new() { ActionId = 3u, TimesUsed = 4, OptimalUses = 4 },
        };

        var result = PerformanceTracker.BuildCooldownAnalysisFromSession(cooldowns);

        Assert.Equal(3, result.Count);
        Assert.Equal(2, result[0].TimesUsed);
        Assert.Equal(1, result[1].TimesUsed);
        Assert.Equal(4, result[2].TimesUsed);
    }

    // -------------------------------------------------------------------------
    // Routing tests: GetCooldownAnalysis branches on IsTracking
    // -------------------------------------------------------------------------

    [Fact]
    public void GetCooldownAnalysis_NotTrackingNoSession_ReturnsEmpty()
    {
        // Tracker with no session and not in combat returns empty, not an exception.
        var (tracker, _) = CreateTracker();
        using (tracker)
        {
            Assert.Empty(tracker.GetCooldownAnalysis());
        }
    }

    [Fact]
    public void GetCooldownAnalysis_NotTracking_ReadsLastSessionNotLiveState()
    {
        // This is the C1 bug regression test.
        //
        // Before the fix: GetCooldownAnalysis() always called BuildCooldownAnalysisList()
        // which reads from cooldownStates (cleared on combat start).  After a fight, the
        // cooldown tab showed all zeros.
        //
        // After the fix: when IsTracking == false, the method reads from
        // lastSession.FinalMetrics.Cooldowns — the snapshot saved when the fight ended.
        var (tracker, _) = CreateTracker();
        using (tracker)
        {
            // Inject a completed session with TimesUsed = 3 directly, bypassing the
            // 5-second MinCombatDuration gate (which cannot be set below 5f via the setter).
            InjectSession(tracker, MakeSession(timesUsed: 3));

            // Out of combat (IsTracking == false) → must read the snapshot.
            Assert.False(tracker.IsTracking);

            var result = tracker.GetCooldownAnalysis();

            Assert.Single(result);
            Assert.Equal(3, result[0].TimesUsed);
            // Efficiency: 3 uses out of 4 optimal = 75%
            Assert.Equal(75f, result[0].Efficiency, precision: 1);
        }
    }

    [Fact]
    public void GetCooldownAnalysis_NotTracking_MostRecentSessionWins()
    {
        // When multiple sessions are stored, GetCooldownAnalysis returns data
        // from the most recent one (TimesUsed = 7), not an older one (TimesUsed = 3).
        var (tracker, _) = CreateTracker();
        using (tracker)
        {
            // Older session added first, then newer session on top.
            InjectSession(tracker, MakeSession(timesUsed: 3)); // first insert = last in history after second
            InjectSession(tracker, MakeSession(timesUsed: 7)); // newest = first in LinkedList

            var result = tracker.GetCooldownAnalysis();

            Assert.Single(result);
            Assert.Equal(7, result[0].TimesUsed); // newest session wins
        }
    }
}
