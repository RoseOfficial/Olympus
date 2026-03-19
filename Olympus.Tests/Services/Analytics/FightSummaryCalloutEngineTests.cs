using System;
using System.Collections.Generic;
using System.Linq;
using Olympus.Data;
using Olympus.Models;
using Olympus.Services.Analytics;
using Xunit;

namespace Olympus.Tests.Services.Analytics;

public class FightSummaryCalloutEngineTests
{
    #region ComputeGrade Tests

    [Fact]
    public void ComputeGrade_PerfectExecution_ReturnsS()
    {
        // 100% uptime * 0.40 = 40, 0 drift => 25, 100% burst => 20, 0 deaths => 15 = 100
        var grade = FightSummaryCalloutEngine.ComputeGrade(100f, 0f, 100f, 0);
        Assert.Equal("S", grade);
    }

    [Fact]
    public void ComputeGrade_ModeratePerformance_ReturnsBRange()
    {
        // 85% uptime * 0.40 = 34, 3s drift => 25 - 7.5 = 17.5, 70% burst => 14, 0 deaths => 15 = 80.5
        var grade = FightSummaryCalloutEngine.ComputeGrade(85f, 3f, 70f, 0);
        Assert.StartsWith("B", grade);
    }

    [Fact]
    public void ComputeGrade_PoorPerformance_ReturnsDRange()
    {
        // 50% uptime * 0.40 = 20, 8s drift => 25 - 20 = 5, 30% burst => 6, 2 deaths => 5 = 36
        var grade = FightSummaryCalloutEngine.ComputeGrade(50f, 8f, 30f, 2);
        Assert.StartsWith("D", grade);
    }

    [Fact]
    public void ComputeGrade_NearUpperBound_IncludesPlus()
    {
        // Target: score in A range (85-92), near upper bound (90-92 for "+")
        // 95% uptime * 0.40 = 38, 0 drift => 25, 80% burst => 16, 1 death => 10 = 89
        // 89 is in A range. upper = 92, 92-89 = 3 which is NOT < 3.
        // Need score ~91: uptime=97 * 0.40 = 38.8, drift=0 => 25, burst=85% => 17, deaths=0 => 15 = 95.8 => S
        // Try: 95% * 0.40 = 38, drift=1 => 22.5, burst=80% => 16, deaths=1 => 10 = 86.5 => A-, not A+
        // For A+: need ~91: 95% * 0.40 = 38, drift=0 => 25, burst=90% => 18, deaths=1 => 10 = 91 => A+
        var grade = FightSummaryCalloutEngine.ComputeGrade(95f, 0f, 90f, 1);
        Assert.Equal("A+", grade);
    }

    [Fact]
    public void ComputeGrade_NearLowerBound_IncludesMinus()
    {
        // A range is 85-92, near lower bound (85-87) = A-
        // Need score ~86: 90% * 0.40 = 36, drift=2 => 20, burst=75% => 15, deaths=0 => 15 = 86 => A-?
        // 86 - 85 = 1 < 3, so A-
        var grade = FightSummaryCalloutEngine.ComputeGrade(90f, 2f, 75f, 0);
        Assert.Equal("A-", grade);
    }

    [Fact]
    public void ComputeGrade_SIsNeverSuffixed()
    {
        // Score >= 93 = always plain "S"
        var grade = FightSummaryCalloutEngine.ComputeGrade(100f, 0f, 100f, 0);
        Assert.Equal("S", grade);

        // Score 95: 98% * 0.40 = 39.2, drift=0 => 25, burst=100% => 20, deaths=1 => 10 = 94.2
        var grade2 = FightSummaryCalloutEngine.ComputeGrade(98f, 0f, 100f, 1);
        Assert.Equal("S", grade2);
    }

    [Fact]
    public void ComputeGrade_ZeroEverything_ReturnsD()
    {
        var grade = FightSummaryCalloutEngine.ComputeGrade(0f, 10f, 0f, 3);
        Assert.StartsWith("D", grade);
    }

    [Fact]
    public void ComputeGrade_ManyDeaths_CapsDeathScoreAtZero()
    {
        // 4 deaths: 15 - 20 = floored to 0, not negative
        // 100% uptime = 40, 0 drift = 25, 100% burst = 20, 4 deaths = 0 => 85 = A-
        var grade = FightSummaryCalloutEngine.ComputeGrade(100f, 0f, 100f, 4);
        Assert.Equal("A-", grade);
    }

    #endregion

    #region GenerateDriftCallouts Tests

    [Fact]
    public void GenerateDriftCallouts_HighDrift_ReturnsCritical()
    {
        var cooldowns = new List<CooldownAnalysis>
        {
            new()
            {
                ActionId = 1000,
                Name = "Battle Litany",
                CooldownDuration = 120f,
                TimesUsed = 3,
                AverageDrift = 8f
            }
        };

        var callouts = FightSummaryCalloutEngine.GenerateDriftCallouts(cooldowns);

        Assert.Single(callouts);
        Assert.Equal(CalloutSeverity.Critical, callouts[0].Severity);
        Assert.Equal(CalloutCategory.Drift, callouts[0].Category);
        Assert.Contains("8.0s", callouts[0].Title);
    }

    [Fact]
    public void GenerateDriftCallouts_ModerateDrift_ReturnsWarning()
    {
        var cooldowns = new List<CooldownAnalysis>
        {
            new()
            {
                ActionId = 1000,
                Name = "Battle Litany",
                CooldownDuration = 120f,
                TimesUsed = 2,
                AverageDrift = 4.5f
            }
        };

        var callouts = FightSummaryCalloutEngine.GenerateDriftCallouts(cooldowns);

        Assert.Single(callouts);
        Assert.Equal(CalloutSeverity.Warning, callouts[0].Severity);
    }

    [Fact]
    public void GenerateDriftCallouts_LowDrift_ReturnsEmpty()
    {
        var cooldowns = new List<CooldownAnalysis>
        {
            new()
            {
                ActionId = 1000,
                Name = "Battle Litany",
                CooldownDuration = 120f,
                TimesUsed = 3,
                AverageDrift = 2f
            }
        };

        var callouts = FightSummaryCalloutEngine.GenerateDriftCallouts(cooldowns);

        Assert.Empty(callouts);
    }

    [Fact]
    public void GenerateDriftCallouts_ShortCooldown_IsIgnored()
    {
        var cooldowns = new List<CooldownAnalysis>
        {
            new()
            {
                ActionId = 1000,
                Name = "Short CD",
                CooldownDuration = 60f,
                TimesUsed = 5,
                AverageDrift = 10f
            }
        };

        var callouts = FightSummaryCalloutEngine.GenerateDriftCallouts(cooldowns);

        Assert.Empty(callouts);
    }

    [Fact]
    public void GenerateDriftCallouts_OnlyUsedOnce_IsIgnored()
    {
        var cooldowns = new List<CooldownAnalysis>
        {
            new()
            {
                ActionId = 1000,
                Name = "Battle Litany",
                CooldownDuration = 120f,
                TimesUsed = 1,
                AverageDrift = 10f
            }
        };

        var callouts = FightSummaryCalloutEngine.GenerateDriftCallouts(cooldowns);

        Assert.Empty(callouts);
    }

    [Fact]
    public void GenerateDriftCallouts_EmptyList_ReturnsEmpty()
    {
        var callouts = FightSummaryCalloutEngine.GenerateDriftCallouts(new List<CooldownAnalysis>());
        Assert.Empty(callouts);
    }

    #endregion

    #region GenerateDeathCallouts Tests

    [Fact]
    public void GenerateDeathCallouts_WithDeaths_ReturnsCritical()
    {
        var callouts = FightSummaryCalloutEngine.GenerateDeathCallouts(2, 0, 300f, 5000f);

        Assert.Single(callouts);
        Assert.Equal(CalloutSeverity.Critical, callouts[0].Severity);
        Assert.Equal(CalloutCategory.Deaths, callouts[0].Category);
        Assert.Contains("2 deaths", callouts[0].Title);
        Assert.True(callouts[0].EstimatedPotencyLoss > 0);
    }

    [Fact]
    public void GenerateDeathCallouts_ZeroDeaths_ReturnsEmpty()
    {
        var callouts = FightSummaryCalloutEngine.GenerateDeathCallouts(0, 0, 300f, 5000f);

        Assert.Empty(callouts);
    }

    [Fact]
    public void GenerateDeathCallouts_NearDeathsOnly_ReturnsWarning()
    {
        var callouts = FightSummaryCalloutEngine.GenerateDeathCallouts(0, 3, 300f, 5000f);

        Assert.Single(callouts);
        Assert.Equal(CalloutSeverity.Warning, callouts[0].Severity);
        Assert.Contains("near-death", callouts[0].Title);
    }

    [Fact]
    public void GenerateDeathCallouts_DeathsTakePriorityOverNearDeaths()
    {
        // When there are deaths, near-deaths should not generate their own callout
        var callouts = FightSummaryCalloutEngine.GenerateDeathCallouts(1, 5, 300f, 5000f);

        Assert.Single(callouts);
        Assert.Equal(CalloutSeverity.Critical, callouts[0].Severity);
    }

    [Fact]
    public void GenerateDeathCallouts_SingleDeath_UseSingular()
    {
        var callouts = FightSummaryCalloutEngine.GenerateDeathCallouts(1, 0, 300f, 5000f);

        Assert.Single(callouts);
        Assert.Contains("1 death", callouts[0].Title);
        Assert.DoesNotContain("deaths", callouts[0].Title);
    }

    #endregion

    #region GenerateDowntimeCallouts Tests

    [Fact]
    public void GenerateDowntimeCallouts_LargeGap_ReturnsCritical()
    {
        var history = new List<ActionAttempt>
        {
            MakeAttempt(0f, 0.5f),
            MakeAttempt(0.5f, 3.5f),  // 3.5s gap -> Critical
            MakeAttempt(4f, 0.5f)
        };

        var callouts = FightSummaryCalloutEngine.GenerateDowntimeCallouts(history);

        Assert.Single(callouts);
        Assert.Equal(CalloutSeverity.Critical, callouts[0].Severity);
        Assert.Contains("3.5s", callouts[0].Title);
    }

    [Fact]
    public void GenerateDowntimeCallouts_SmallGaps_ReturnsEmpty()
    {
        var history = new List<ActionAttempt>
        {
            MakeAttempt(0f, 0.5f),
            MakeAttempt(0.5f, 0.7f),
            MakeAttempt(1.2f, 0.6f)
        };

        var callouts = FightSummaryCalloutEngine.GenerateDowntimeCallouts(history);

        Assert.Empty(callouts);
    }

    [Fact]
    public void GenerateDowntimeCallouts_CapsAtThreeGaps()
    {
        var history = new List<ActionAttempt>
        {
            MakeAttempt(0f, 1.0f),
            MakeAttempt(1f, 1.5f),
            MakeAttempt(2.5f, 2.0f),
            MakeAttempt(4.5f, 1.2f),
            MakeAttempt(5.7f, 3.0f)
        };

        var callouts = FightSummaryCalloutEngine.GenerateDowntimeCallouts(history);

        Assert.Equal(3, callouts.Count);
    }

    [Fact]
    public void GenerateDowntimeCallouts_MediumGap_ReturnsWarning()
    {
        var history = new List<ActionAttempt>
        {
            MakeAttempt(0f, 0.5f),
            MakeAttempt(0.5f, 1.5f),  // 1.5s gap -> Warning (> 0.8 but <= 2)
            MakeAttempt(2f, 0.5f)
        };

        var callouts = FightSummaryCalloutEngine.GenerateDowntimeCallouts(history);

        Assert.Single(callouts);
        Assert.Equal(CalloutSeverity.Warning, callouts[0].Severity);
    }

    [Fact]
    public void GenerateDowntimeCallouts_EmptyHistory_ReturnsEmpty()
    {
        var callouts = FightSummaryCalloutEngine.GenerateDowntimeCallouts(new List<ActionAttempt>());
        Assert.Empty(callouts);
    }

    #endregion

    #region GenerateWasteCallouts Tests

    [Fact]
    public void GenerateWasteCallouts_MissedUses_ReturnsWarning()
    {
        var cooldowns = new List<CooldownAnalysis>
        {
            new()
            {
                ActionId = 1000,
                Name = "Battle Litany",
                CooldownDuration = 120f,
                TimesUsed = 2,
                MissedOpportunities = new List<MissedCooldownOpportunity>
                {
                    new() { ActionId = 1000, AbilityName = "Battle Litany", FightTimeSeconds = 120f }
                }
            }
        };

        var callouts = FightSummaryCalloutEngine.GenerateWasteCallouts(cooldowns);

        Assert.Single(callouts);
        Assert.Equal(CalloutSeverity.Warning, callouts[0].Severity);
        Assert.Equal(CalloutCategory.Waste, callouts[0].Category);
    }

    [Fact]
    public void GenerateWasteCallouts_NoMissedUses_ReturnsEmpty()
    {
        var cooldowns = new List<CooldownAnalysis>
        {
            new()
            {
                ActionId = 1000,
                Name = "Battle Litany",
                CooldownDuration = 120f,
                TimesUsed = 3,
                MissedOpportunities = Array.Empty<MissedCooldownOpportunity>()
            }
        };

        var callouts = FightSummaryCalloutEngine.GenerateWasteCallouts(cooldowns);

        Assert.Empty(callouts);
    }

    #endregion

    #region GenerateBurstCallouts Tests

    [Fact]
    public void GenerateBurstCallouts_NoBurstWindows_ReturnsEmpty()
    {
        var history = new List<ActionAttempt> { MakeSuccessfulAttempt(100, 1f) };
        var callouts = FightSummaryCalloutEngine.GenerateBurstCallouts(
            history,
            Array.Empty<(DateTime, DateTime)>(),
            JobRegistry.Dragoon);

        Assert.Empty(callouts);
    }

    [Fact]
    public void GenerateBurstCallouts_LowBurstAlignment_ReturnsWarning()
    {
        var baseTime = new DateTime(2024, 1, 1, 0, 0, 0);
        var burstWindows = new List<(DateTime Start, DateTime End)>
        {
            (baseTime.AddSeconds(10), baseTime.AddSeconds(25))
        };

        // 10 actions total, only 2 inside burst window
        var history = new List<ActionAttempt>();
        for (var i = 0; i < 10; i++)
        {
            history.Add(MakeSuccessfulAttempt(100, i * 5f, baseTime));
        }

        var callouts = FightSummaryCalloutEngine.GenerateBurstCallouts(history, burstWindows, JobRegistry.Dragoon);

        Assert.Single(callouts);
        Assert.Equal(CalloutSeverity.Warning, callouts[0].Severity);
        Assert.Equal(CalloutCategory.BurstAlignment, callouts[0].Category);
    }

    #endregion

    #region GenerateRoleActionCallouts Tests

    [Fact]
    public void GenerateRoleActionCallouts_MeleeNoFeint_ReturnsWarning()
    {
        var history = new List<ActionAttempt>
        {
            MakeSuccessfulAttempt(100, 0f),  // Not Feint
            MakeSuccessfulAttempt(200, 5f)
        };

        var callouts = FightSummaryCalloutEngine.GenerateRoleActionCallouts(history, JobRegistry.Dragoon, 300f);

        Assert.Single(callouts);
        Assert.Contains("Feint", callouts[0].Title);
    }

    [Fact]
    public void GenerateRoleActionCallouts_HealerNoSwiftcast_ReturnsWarning()
    {
        var history = new List<ActionAttempt>
        {
            MakeSuccessfulAttempt(100, 0f)
        };

        var callouts = FightSummaryCalloutEngine.GenerateRoleActionCallouts(history, JobRegistry.WhiteMage, 300f);

        Assert.Single(callouts);
        Assert.Contains("Swiftcast", callouts[0].Title);
    }

    [Fact]
    public void GenerateRoleActionCallouts_TankUsedReprisal_ReturnsEmpty()
    {
        var history = new List<ActionAttempt>
        {
            MakeSuccessfulAttempt(RoleActions.Reprisal.ActionId, 10f)
        };

        var callouts = FightSummaryCalloutEngine.GenerateRoleActionCallouts(history, JobRegistry.Warrior, 300f);

        Assert.Empty(callouts);
    }

    [Fact]
    public void GenerateRoleActionCallouts_ShortFight_ReturnsEmpty()
    {
        var history = new List<ActionAttempt>
        {
            MakeSuccessfulAttempt(100, 0f)
        };

        var callouts = FightSummaryCalloutEngine.GenerateRoleActionCallouts(history, JobRegistry.Dragoon, 90f);

        Assert.Empty(callouts);
    }

    [Fact]
    public void GenerateRoleActionCallouts_CasterNoAddle_ReturnsWarning()
    {
        var history = new List<ActionAttempt>
        {
            MakeSuccessfulAttempt(100, 0f)
        };

        var callouts = FightSummaryCalloutEngine.GenerateRoleActionCallouts(history, JobRegistry.BlackMage, 300f);

        Assert.Single(callouts);
        Assert.Contains("Addle", callouts[0].Title);
    }

    #endregion

    #region GenerateDoTCallouts Tests

    [Fact]
    public void GenerateDoTCallouts_LowUptime_ReturnsWarning()
    {
        // WHM with Dia (16532): 1 cast over 120s fight = 30/120 = 25% uptime
        var history = new List<ActionAttempt>
        {
            MakeSuccessfulAttempt(16532, 0f)
        };

        var callouts = FightSummaryCalloutEngine.GenerateDoTCallouts(history, JobRegistry.WhiteMage, 120f);

        Assert.Single(callouts);
        Assert.Equal(CalloutSeverity.Warning, callouts[0].Severity);
        Assert.Equal(CalloutCategory.DoT, callouts[0].Category);
    }

    [Fact]
    public void GenerateDoTCallouts_GoodUptime_ReturnsEmpty()
    {
        // WHM with Dia: 4 casts in 120s fight = 120/120 = 100% uptime (capped)
        var history = new List<ActionAttempt>
        {
            MakeSuccessfulAttempt(16532, 0f),
            MakeSuccessfulAttempt(16532, 28f),
            MakeSuccessfulAttempt(16532, 56f),
            MakeSuccessfulAttempt(16532, 84f)
        };

        var callouts = FightSummaryCalloutEngine.GenerateDoTCallouts(history, JobRegistry.WhiteMage, 120f);

        Assert.Empty(callouts);
    }

    [Fact]
    public void GenerateDoTCallouts_JobWithoutDoT_ReturnsEmpty()
    {
        var history = new List<ActionAttempt>
        {
            MakeSuccessfulAttempt(100, 0f)
        };

        // DNC has no DoT
        var callouts = FightSummaryCalloutEngine.GenerateDoTCallouts(history, JobRegistry.Dancer, 300f);

        Assert.Empty(callouts);
    }

    [Fact]
    public void GenerateDoTCallouts_NeverCastDoT_ReturnsCritical()
    {
        // WHM but never cast any DoT action
        var history = new List<ActionAttempt>
        {
            MakeSuccessfulAttempt(100, 0f)  // Not a DoT action
        };

        var callouts = FightSummaryCalloutEngine.GenerateDoTCallouts(history, JobRegistry.WhiteMage, 300f);

        Assert.Single(callouts);
        Assert.Equal(CalloutSeverity.Critical, callouts[0].Severity);
        Assert.Contains("never applied", callouts[0].Title);
    }

    #endregion

    #region Generate (Top-Level) Tests

    [Fact]
    public void Generate_EnsuresGoodFallback_WhenNoGoodCallout()
    {
        var session = MakeSession(300f, deaths: 0, nearDeaths: 0);

        var callouts = FightSummaryCalloutEngine.Generate(
            session,
            JobRegistry.Dancer,  // No DoT, so fewer callouts
            new List<ActionAttempt> { MakeSuccessfulAttempt(RoleActions.Feint.ActionId, 0f) },
            new List<CooldownAnalysis>(),
            Array.Empty<(DateTime, DateTime)>(),
            95f);

        Assert.True(callouts.Any(c => c.Severity == CalloutSeverity.Good),
            "Should contain at least one Good callout as fallback");
    }

    [Fact]
    public void Generate_CapsAtFiveCallouts()
    {
        var session = MakeSession(300f, deaths: 2, nearDeaths: 3);

        // Create many cooldowns with drift to generate lots of callouts
        var cooldowns = new List<CooldownAnalysis>();
        for (var i = 0; i < 10; i++)
        {
            cooldowns.Add(new CooldownAnalysis
            {
                ActionId = (uint)(1000 + i),
                Name = $"Ability {i}",
                CooldownDuration = 120f,
                TimesUsed = 3,
                AverageDrift = 8f,
                MissedOpportunities = new List<MissedCooldownOpportunity>
                {
                    new() { ActionId = (uint)(1000 + i), AbilityName = $"Ability {i}", FightTimeSeconds = 120f }
                }
            });
        }

        var callouts = FightSummaryCalloutEngine.Generate(
            session,
            JobRegistry.Dragoon,
            new List<ActionAttempt>
            {
                MakeSuccessfulAttempt(100, 0f),
                MakeAttempt(5f, 5f)  // Large gap for downtime callout
            },
            cooldowns,
            Array.Empty<(DateTime, DateTime)>(),
            80f);

        Assert.True(callouts.Count <= 5, $"Should cap at 5 but got {callouts.Count}");
    }

    [Fact]
    public void Generate_SortsBySeverityDescending()
    {
        var session = MakeSession(300f, deaths: 1, nearDeaths: 0, personalDps: 5000f);

        var cooldowns = new List<CooldownAnalysis>
        {
            new()
            {
                ActionId = 1000,
                Name = "Test CD",
                CooldownDuration = 120f,
                TimesUsed = 3,
                AverageDrift = 4f  // Warning-level drift
            }
        };

        var callouts = FightSummaryCalloutEngine.Generate(
            session,
            JobRegistry.Dancer,
            new List<ActionAttempt> { MakeSuccessfulAttempt(100, 0f) },
            cooldowns,
            Array.Empty<(DateTime, DateTime)>(),
            80f);

        // Deaths should be Critical and come first
        Assert.True(callouts.Count > 0);
        Assert.Equal(CalloutSeverity.Critical, callouts[0].Severity);
    }

    [Fact]
    public void Generate_GoodFallbackShowsUptime()
    {
        var session = MakeSession(300f, deaths: 0, nearDeaths: 0);

        var callouts = FightSummaryCalloutEngine.Generate(
            session,
            JobRegistry.Dancer,
            new List<ActionAttempt>(),
            new List<CooldownAnalysis>(),
            Array.Empty<(DateTime, DateTime)>(),
            92f);

        var goodCallout = callouts.FirstOrDefault(c => c.Severity == CalloutSeverity.Good);
        Assert.NotNull(goodCallout);
        Assert.Contains("92", goodCallout.Title);
    }

    #endregion

    #region Test Helpers

    private static ActionAttempt MakeAttempt(float timeOffset, float timeSinceLastCast)
    {
        return new ActionAttempt
        {
            Timestamp = new DateTime(2024, 1, 1, 0, 0, 0).AddSeconds(timeOffset),
            ActionId = 100,
            SpellName = "Test",
            Result = ActionResult.Success,
            TimeSinceLastCast = timeSinceLastCast
        };
    }

    private static ActionAttempt MakeSuccessfulAttempt(uint actionId, float timeOffset, DateTime? baseTime = null)
    {
        var bTime = baseTime ?? new DateTime(2024, 1, 1, 0, 0, 0);
        return new ActionAttempt
        {
            Timestamp = bTime.AddSeconds(timeOffset),
            ActionId = actionId,
            SpellName = "Test",
            Result = ActionResult.Success,
            TimeSinceLastCast = 0.5f
        };
    }

    private static FightSession MakeSession(
        float durationSeconds,
        int deaths = 0,
        int nearDeaths = 0,
        float personalDps = 5000f)
    {
        var startTime = new DateTime(2024, 1, 1, 0, 0, 0);
        return new FightSession
        {
            StartTime = startTime,
            EndTime = startTime.AddSeconds(durationSeconds),
            JobId = JobRegistry.Dragoon,
            FinalMetrics = new CombatMetricsSnapshot
            {
                CombatDuration = durationSeconds,
                Deaths = deaths,
                NearDeaths = nearDeaths,
                PersonalDps = personalDps,
                GcdUptime = 90f
            }
        };
    }

    #endregion
}
