using System;
using System.Collections.Generic;
using System.Linq;
using Olympus.Data;
using Olympus.Models;

namespace Olympus.Services.Analytics;

/// <summary>
/// Static analysis engine that produces post-fight coaching callouts.
/// Examines GCD uptime, cooldown drift, burst alignment, role action usage,
/// DoT uptime, deaths, and downtime gaps to generate prioritized feedback.
/// </summary>
public static class FightSummaryCalloutEngine
{
    /// <summary>Standard DoT tick duration in seconds.</summary>
    private const float DotTickDuration = 30f;

    /// <summary>Maximum callouts returned by Generate.</summary>
    private const int MaxCallouts = 5;

    /// <summary>Minimum fight duration (seconds) before role action callouts apply.</summary>
    private const float MinFightDurationForRoleActions = 120f;

    // Primary single-target DoT action IDs per job.
    // Each array entry is a variant (level upgrade) of the same DoT — any hit counts.
    private static readonly Dictionary<uint, uint[]> PrimaryDotActions = new()
    {
        // WHM: Aero / Aero II / Dia
        [JobRegistry.WhiteMage] = new uint[] { 121, 132, 16532 },
        [JobRegistry.Conjurer] = new uint[] { 121, 132 },
        // SCH: Bio / Bio II / Biolysis
        [JobRegistry.Scholar] = new uint[] { 17864, 17865, 16540 },
        [JobRegistry.Arcanist] = new uint[] { 17864, 17865 },
        // AST: Combust / Combust II / Combust III
        [JobRegistry.Astrologian] = new uint[] { 3599, 3608, 16554 },
        // SGE: Eukrasian Dosis / II / III
        [JobRegistry.Sage] = new uint[] { 24293, 24308, 24314 },
        // BRD: Venomous Bite / Caustic Bite + Windbite / Stormbite + Iron Jaws
        [JobRegistry.Bard] = new uint[] { 100, 7406, 113, 7407, 3560 },
        [JobRegistry.Archer] = new uint[] { 100, 113 },
        // BLM: Thunder / III / High Thunder (single-target variants)
        [JobRegistry.BlackMage] = new uint[] { 144, 153, 36986 },
        [JobRegistry.Thaumaturge] = new uint[] { 144 },
        // DRG: Chaos Thrust / Chaotic Spring
        [JobRegistry.Dragoon] = new uint[] { 88, 25772 },
        [JobRegistry.Lancer] = new uint[] { 88 },
        // MNK: Demolish
        [JobRegistry.Monk] = new uint[] { 66 },
        [JobRegistry.Pugilist] = new uint[] { 66 },
        // SAM: Higanbana
        [JobRegistry.Samurai] = new uint[] { 7489 },
        // RPR: Shadow of Death
        [JobRegistry.Reaper] = new uint[] { 24378 },
    };

    #region Grade Computation

    /// <summary>
    /// Computes a letter grade from four performance components.
    /// Score 0-100: GCD uptime (40), cooldown drift (25), burst alignment (20), deaths (15).
    /// Bands: S (93+), A (85-92), B (73-84), C (60-72), D (0-59).
    /// Suffix: "+" within 3 of upper bound, "-" within 3 of lower bound. S never gets a suffix.
    /// </summary>
    public static string ComputeGrade(
        float gcdUptimePercent,
        float avgMajorCooldownDrift,
        float burstAlignmentPercent,
        int deathCount)
    {
        var gcdScore = Math.Clamp(gcdUptimePercent, 0f, 100f) * 0.40f;
        var driftScore = Math.Max(0f, 25f - avgMajorCooldownDrift * 2.5f);
        var burstScore = Math.Clamp(burstAlignmentPercent, 0f, 100f) / 100f * 20f;
        var deathScore = Math.Max(0f, 15f - deathCount * 5f);

        var total = gcdScore + driftScore + burstScore + deathScore;
        total = Math.Clamp(total, 0f, 100f);

        return ScoreToGrade(total);
    }

    private static string ScoreToGrade(float score)
    {
        // Determine base grade and band boundaries.
        string baseGrade;
        float lowerBound;
        float upperBound;

        if (score >= 93f)
        {
            return "S"; // S never gets suffix
        }
        else if (score >= 85f)
        {
            baseGrade = "A";
            lowerBound = 85f;
            upperBound = 92f;
        }
        else if (score >= 73f)
        {
            baseGrade = "B";
            lowerBound = 73f;
            upperBound = 84f;
        }
        else if (score >= 60f)
        {
            baseGrade = "C";
            lowerBound = 60f;
            upperBound = 72f;
        }
        else
        {
            baseGrade = "D";
            lowerBound = 0f;
            upperBound = 59f;
        }

        // Apply suffix.
        if (upperBound - score < 3f)
            return baseGrade + "+";
        if (score - lowerBound < 3f)
            return baseGrade + "-";

        return baseGrade;
    }

    #endregion

    #region Callout Generators

    /// <summary>
    /// Flags cooldowns with >= 120s recast, used at least twice, where average drift exceeds 3s.
    /// Drift > 6s = Critical, otherwise Warning.
    /// </summary>
    public static IReadOnlyList<FightCallout> GenerateDriftCallouts(IReadOnlyList<CooldownAnalysis> cooldowns)
    {
        if (cooldowns == null || cooldowns.Count == 0)
            return Array.Empty<FightCallout>();

        var results = new List<FightCallout>();

        foreach (var cd in cooldowns)
        {
            if (cd.CooldownDuration < 120f || cd.TimesUsed < 2 || cd.AverageDrift <= 3f)
                continue;

            var severity = cd.AverageDrift > 6f ? CalloutSeverity.Critical : CalloutSeverity.Warning;

            results.Add(new FightCallout
            {
                Severity = severity,
                Category = CalloutCategory.Drift,
                Title = $"{cd.Name} \u2014 drifted avg {cd.AverageDrift:F1}s",
                Description = $"{cd.Name} was used {cd.TimesUsed} times with an average drift of {cd.AverageDrift:F1}s. Try to press it as soon as it comes off cooldown.",
                EstimatedPotencyLoss = cd.AverageDrift * cd.TimesUsed * 10f
            });
        }

        return results;
    }

    /// <summary>
    /// Flags cooldowns that had detected missed use opportunities.
    /// </summary>
    public static IReadOnlyList<FightCallout> GenerateWasteCallouts(IReadOnlyList<CooldownAnalysis> cooldowns)
    {
        if (cooldowns == null || cooldowns.Count == 0)
            return Array.Empty<FightCallout>();

        var results = new List<FightCallout>();

        foreach (var cd in cooldowns)
        {
            if (cd.MissedUsesCount <= 0)
                continue;

            results.Add(new FightCallout
            {
                Severity = CalloutSeverity.Warning,
                Category = CalloutCategory.Waste,
                Title = $"{cd.Name} \u2014 {cd.MissedUsesCount} missed use{(cd.MissedUsesCount > 1 ? "s" : "")}",
                Description = $"{cd.Name} sat available without being used {cd.MissedUsesCount} time{(cd.MissedUsesCount > 1 ? "s" : "")}. You could have gotten {cd.MissedUsesCount} extra use{(cd.MissedUsesCount > 1 ? "s" : "")} during this fight.",
                EstimatedPotencyLoss = cd.MissedUsesCount * 300f
            });
        }

        return results;
    }

    /// <summary>
    /// Identifies the 3 largest GCD gaps (> 0.8s) in the action history.
    /// Subtracts any time the player was incapacitated (Willful, Stun, etc.) from each gap.
    /// Gap > 2s = Critical, otherwise Warning.
    /// </summary>
    public static IReadOnlyList<FightCallout> GenerateDowntimeCallouts(
        IReadOnlyList<ActionAttempt> history,
        IReadOnlyList<(DateTime Start, DateTime End)>? incapacitationWindows = null)
    {
        if (history == null || history.Count == 0)
            return Array.Empty<FightCallout>();

        var gaps = new List<(float Gap, DateTime Timestamp)>();

        foreach (var entry in history)
        {
            if (entry.TimeSinceLastCast <= 0.8f)
                continue;

            // If this gap overlaps with ANY unable-to-act window (death, incapacitation),
            // skip it entirely. Deaths already generate their own callout via
            // GenerateDeathCallouts() — reporting "idle" on top double-counts.
            if (incapacitationWindows != null && incapacitationWindows.Count > 0)
            {
                var gapEnd = entry.Timestamp;
                var gapStart = gapEnd.AddSeconds(-entry.TimeSinceLastCast);

                var overlapsIncapacitation = false;
                foreach (var (incapStart, incapEnd) in incapacitationWindows)
                {
                    var overlapStart = incapStart > gapStart ? incapStart : gapStart;
                    var overlapEnd = incapEnd < gapEnd ? incapEnd : gapEnd;

                    if (overlapStart < overlapEnd)
                    {
                        overlapsIncapacitation = true;
                        break;
                    }
                }

                if (overlapsIncapacitation)
                    continue;
            }

            gaps.Add((entry.TimeSinceLastCast, entry.Timestamp));
        }

        if (gaps.Count == 0)
            return Array.Empty<FightCallout>();

        var topGaps = gaps
            .OrderByDescending(g => g.Gap)
            .Take(3)
            .ToList();

        var results = new List<FightCallout>();

        foreach (var (gap, timestamp) in topGaps)
        {
            var severity = gap > 2f ? CalloutSeverity.Critical : CalloutSeverity.Warning;

            results.Add(new FightCallout
            {
                Severity = severity,
                Category = CalloutCategory.Downtime,
                Title = $"GCD gap \u2014 {gap:F1}s idle",
                Description = $"A {gap:F1}s gap was detected between GCD casts. Keep your GCD rolling to maximize damage.",
                FightTimestamp = timestamp - timestamp.Date > TimeSpan.Zero ? TimeSpan.FromTicks(timestamp.Ticks) : null,
                EstimatedPotencyLoss = gap * 150f
            });
        }

        return results;
    }

    /// <summary>
    /// Checks what percentage of actions fell inside burst windows.
    /// If fewer than 70% of actions land inside burst windows, generates a Warning.
    /// </summary>
    public static IReadOnlyList<FightCallout> GenerateBurstCallouts(
        IReadOnlyList<ActionAttempt> history,
        IReadOnlyList<(DateTime Start, DateTime End)> burstWindows,
        uint jobId)
    {
        if (history == null || history.Count == 0 || burstWindows == null || burstWindows.Count == 0)
            return Array.Empty<FightCallout>();

        var actionsInBurst = 0;
        var totalActions = 0;

        foreach (var action in history)
        {
            if (action.Result != ActionResult.Success)
                continue;

            totalActions++;

            foreach (var (start, end) in burstWindows)
            {
                if (action.Timestamp >= start && action.Timestamp <= end)
                {
                    actionsInBurst++;
                    break;
                }
            }
        }

        if (totalActions == 0)
            return Array.Empty<FightCallout>();

        var percent = (float)actionsInBurst / totalActions * 100f;

        if (percent >= 70f)
            return Array.Empty<FightCallout>();

        return new[]
        {
            new FightCallout
            {
                Severity = CalloutSeverity.Warning,
                Category = CalloutCategory.BurstAlignment,
                Title = $"Burst alignment \u2014 {percent:F0}% of actions in windows",
                Description = $"Only {percent:F0}% of your actions landed inside burst windows. Try to pool resources and align big hits with party buffs.",
                EstimatedPotencyLoss = (70f - percent) * 50f
            }
        };
    }

    /// <summary>
    /// Checks whether critical role actions were used during fights longer than 120s.
    /// Healers: Swiftcast; Melee DPS: Feint; Casters: Addle; Tanks: Reprisal.
    /// </summary>
    public static IReadOnlyList<FightCallout> GenerateRoleActionCallouts(
        IReadOnlyList<ActionAttempt> history,
        uint jobId,
        float fightDurationSeconds)
    {
        if (fightDurationSeconds < MinFightDurationForRoleActions)
            return Array.Empty<FightCallout>();

        if (history == null || history.Count == 0)
            return Array.Empty<FightCallout>();

        var requiredActions = GetRequiredRoleActions(jobId);
        if (requiredActions.Count == 0)
            return Array.Empty<FightCallout>();

        var usedActionIds = new HashSet<uint>();
        foreach (var action in history)
        {
            if (action.Result == ActionResult.Success)
                usedActionIds.Add(action.ActionId);
        }

        var results = new List<FightCallout>();

        foreach (var (actionId, actionName) in requiredActions)
        {
            if (usedActionIds.Contains(actionId))
                continue;

            results.Add(new FightCallout
            {
                Severity = CalloutSeverity.Warning,
                Category = CalloutCategory.RoleActions,
                Title = $"{actionName} \u2014 never used",
                Description = $"{actionName} was not used during a {fightDurationSeconds:F0}s fight. This role action provides important utility for your party.",
                EstimatedPotencyLoss = 0f
            });
        }

        return results;
    }

    /// <summary>
    /// Generates death and near-death callouts.
    /// Deaths produce Critical callouts with estimated DPS loss; near-deaths produce Warning callouts.
    /// </summary>
    public static IReadOnlyList<FightCallout> GenerateDeathCallouts(
        int deathCount,
        int nearDeathCount,
        float fightDurationSeconds,
        float estimatedDps)
    {
        var results = new List<FightCallout>();

        if (deathCount > 0)
        {
            // Estimate ~10s of lost uptime per death.
            var estimatedLoss = deathCount * 10f * estimatedDps;

            results.Add(new FightCallout
            {
                Severity = CalloutSeverity.Critical,
                Category = CalloutCategory.Deaths,
                Title = $"{deathCount} death{(deathCount > 1 ? "s" : "")} \u2014 ~{estimatedLoss:F0} DPS lost",
                Description = $"You died {deathCount} time{(deathCount > 1 ? "s" : "")} during this fight, losing roughly {estimatedLoss:F0} damage worth of uptime. Prioritize survival mechanics.",
                EstimatedPotencyLoss = estimatedLoss
            });
        }
        else if (nearDeathCount > 0)
        {
            results.Add(new FightCallout
            {
                Severity = CalloutSeverity.Warning,
                Category = CalloutCategory.Deaths,
                Title = $"{nearDeathCount} near-death moment{(nearDeathCount > 1 ? "s" : "")}",
                Description = $"You dropped to critical HP {nearDeathCount} time{(nearDeathCount > 1 ? "s" : "")} but survived. Consider using defensive cooldowns earlier.",
                EstimatedPotencyLoss = 0f
            });
        }

        return results;
    }

    /// <summary>
    /// Checks DoT uptime for jobs that have a primary DoT action.
    /// If uptime falls below 95%, generates a Warning callout.
    /// Each DoT cast is assumed to cover 30s of uptime.
    /// </summary>
    public static IReadOnlyList<FightCallout> GenerateDoTCallouts(
        IReadOnlyList<ActionAttempt> history,
        uint jobId,
        float fightDurationSeconds)
    {
        if (fightDurationSeconds <= 0f || history == null || history.Count == 0)
            return Array.Empty<FightCallout>();

        if (!PrimaryDotActions.TryGetValue(jobId, out var dotActionIds))
            return Array.Empty<FightCallout>();

        var dotActionIdSet = new HashSet<uint>(dotActionIds);

        var dotCastCount = 0;
        foreach (var action in history)
        {
            if (action.Result == ActionResult.Success && dotActionIdSet.Contains(action.ActionId))
                dotCastCount++;
        }

        if (dotCastCount == 0)
        {
            return new[]
            {
                new FightCallout
                {
                    Severity = CalloutSeverity.Critical,
                    Category = CalloutCategory.DoT,
                    Title = "DoT never applied",
                    Description = "Your DoT was never cast during this fight. Keeping your DoT active is a significant source of damage.",
                    EstimatedPotencyLoss = fightDurationSeconds * 10f
                }
            };
        }

        var coverage = Math.Min(dotCastCount * DotTickDuration, fightDurationSeconds);
        var uptimePercent = coverage / fightDurationSeconds * 100f;

        if (uptimePercent >= 95f)
            return Array.Empty<FightCallout>();

        return new[]
        {
            new FightCallout
            {
                Severity = CalloutSeverity.Warning,
                Category = CalloutCategory.DoT,
                Title = $"DoT uptime \u2014 {uptimePercent:F0}%",
                Description = $"Your DoT was active for approximately {uptimePercent:F0}% of the fight. Aim for 95%+ by refreshing before it falls off.",
                EstimatedPotencyLoss = (95f - uptimePercent) * fightDurationSeconds * 0.1f
            }
        };
    }

    #endregion

    #region Top-Level Entry Point

    /// <summary>
    /// Generates a prioritized list of post-fight coaching callouts.
    /// Calls all 7 generators, ensures at least one Good callout exists,
    /// sorts by severity descending then potency loss descending, and caps at 5.
    /// </summary>
    public static IReadOnlyList<FightCallout> Generate(
        FightSession session,
        uint jobId,
        IReadOnlyList<ActionAttempt> actionHistory,
        IReadOnlyList<CooldownAnalysis> cooldowns,
        IReadOnlyList<(DateTime Start, DateTime End)> burstWindows,
        float gcdUptime,
        IReadOnlyList<(DateTime Start, DateTime End)>? incapacitationWindows = null)
    {
        var allCallouts = new List<FightCallout>();

        var fightDuration = session.Duration;
        var deaths = session.FinalMetrics?.Deaths ?? 0;
        var nearDeaths = session.FinalMetrics?.NearDeaths ?? 0;
        var estimatedDps = session.FinalMetrics?.PersonalDps ?? 0f;

        allCallouts.AddRange(GenerateDriftCallouts(cooldowns));
        allCallouts.AddRange(GenerateWasteCallouts(cooldowns));
        allCallouts.AddRange(GenerateDowntimeCallouts(actionHistory, incapacitationWindows));
        allCallouts.AddRange(GenerateBurstCallouts(actionHistory, burstWindows, jobId));
        allCallouts.AddRange(GenerateRoleActionCallouts(actionHistory, jobId, fightDuration));
        allCallouts.AddRange(GenerateDeathCallouts(deaths, nearDeaths, fightDuration, estimatedDps));
        allCallouts.AddRange(GenerateDoTCallouts(actionHistory, jobId, fightDuration));

        // Ensure at least one Good callout exists.
        var hasGood = false;
        foreach (var c in allCallouts)
        {
            if (c.Severity == CalloutSeverity.Good)
            {
                hasGood = true;
                break;
            }
        }

        if (!hasGood)
        {
            allCallouts.Add(new FightCallout
            {
                Severity = CalloutSeverity.Good,
                Category = CalloutCategory.Downtime,
                Title = $"GCD uptime {gcdUptime:F0}%",
                Description = $"You maintained {gcdUptime:F0}% GCD uptime during this fight.",
                EstimatedPotencyLoss = 0f
            });
        }

        // Sort: Critical first, then Warning, then Good.
        // Within same severity, higher potency loss first.
        allCallouts.Sort((a, b) =>
        {
            var severityCompare = SeverityRank(b.Severity).CompareTo(SeverityRank(a.Severity));
            if (severityCompare != 0)
                return severityCompare;

            return (b.EstimatedPotencyLoss ?? 0f).CompareTo(a.EstimatedPotencyLoss ?? 0f);
        });

        if (allCallouts.Count <= MaxCallouts)
            return allCallouts;

        return allCallouts.GetRange(0, MaxCallouts);
    }

    #endregion

    #region Helpers

    private static int SeverityRank(CalloutSeverity severity) => severity switch
    {
        CalloutSeverity.Critical => 2,
        CalloutSeverity.Warning => 1,
        CalloutSeverity.Good => 0,
        _ => -1
    };

    private static List<(uint ActionId, string Name)> GetRequiredRoleActions(uint jobId)
    {
        var actions = new List<(uint, string)>();

        if (JobRegistry.IsHealer(jobId))
        {
            actions.Add((RoleActions.Swiftcast.ActionId, RoleActions.Swiftcast.Name));
        }
        else if (JobRegistry.IsMeleeDps(jobId))
        {
            actions.Add((RoleActions.Feint.ActionId, RoleActions.Feint.Name));
        }
        else if (JobRegistry.IsCasterDps(jobId))
        {
            actions.Add((RoleActions.Addle.ActionId, RoleActions.Addle.Name));
        }
        else if (JobRegistry.IsTank(jobId))
        {
            actions.Add((RoleActions.Reprisal.ActionId, RoleActions.Reprisal.Name));
        }

        return actions;
    }

    #endregion
}
