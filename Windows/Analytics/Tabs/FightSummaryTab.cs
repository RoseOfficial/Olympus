using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;
using Olympus.Services.Analytics;

namespace Olympus.Windows.Analytics.Tabs;

/// <summary>
/// Fight Summary tab: post-fight performance breakdown.
/// </summary>
public static class FightSummaryTab
{
    // Colors for grades
    private static readonly Vector4 GradeA = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 GradeB = new(0.5f, 0.8f, 0.3f, 1.0f);
    private static readonly Vector4 GradeC = new(0.9f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 GradeD = new(0.9f, 0.6f, 0.3f, 1.0f);
    private static readonly Vector4 GradeF = new(0.9f, 0.3f, 0.3f, 1.0f);
    private static readonly Vector4 NeutralColor = new(0.7f, 0.7f, 0.7f, 1.0f);
    private static readonly Vector4 InfoColor = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Vector4 WarningColor = new(0.9f, 0.7f, 0.2f, 1.0f);
    private static readonly Vector4 ErrorColor = new(0.9f, 0.3f, 0.3f, 1.0f);

    public static void Draw(IPerformanceTracker tracker, AnalyticsConfig config)
    {
        var lastSession = tracker.GetLastSession();

        if (lastSession == null)
        {
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Analytics.NoFightData, "No fight data available."));
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Analytics.CompleteCombat, "Complete a combat encounter to see analysis."));
            return;
        }

        // Header with fight info
        DrawFightHeader(lastSession);
        ImGui.Separator();

        // Scores Section
        if (IsSectionVisible(config, "SummaryScores") && lastSession.Score != null)
        {
            DrawScores(lastSession.Score);
            ImGui.Spacing();
        }

        // Breakdown Section
        if (IsSectionVisible(config, "SummaryBreakdown") && lastSession.FinalMetrics != null)
        {
            DrawBreakdown(lastSession.FinalMetrics);
            ImGui.Spacing();
        }

        // Downtime Analysis Section
        if (IsSectionVisible(config, "SummaryDowntime") && lastSession.FinalMetrics?.DowntimeAnalysis != null)
        {
            DrawDowntimeAnalysis(lastSession.FinalMetrics.DowntimeAnalysis);
            ImGui.Spacing();
        }

        // Cooldown Analysis Section
        if (IsSectionVisible(config, "SummaryCooldowns"))
        {
            DrawCooldownAnalysis(tracker);
            ImGui.Spacing();
        }

        // Issues Section
        if (IsSectionVisible(config, "SummaryIssues"))
        {
            DrawIssues(lastSession);
        }
    }

    private static void DrawFightHeader(FightSession session)
    {
        var duration = session.Duration;
        var minutes = (int)(duration / 60);
        var seconds = (int)(duration % 60);

        ImGui.Text($"{Loc.T(LocalizedStrings.Analytics.LastFight, "Last Fight:")} {minutes}:{seconds:D2}");
        ImGui.SameLine();
        ImGui.TextColored(NeutralColor, $"| {session.StartTime:HH:mm}");

        if (session.Score != null)
        {
            ImGui.SameLine();
            ImGui.Text("|");
            ImGui.SameLine();
            ImGui.Text(Loc.T(LocalizedStrings.Analytics.Score, "Score:"));
            ImGui.SameLine();
            var scoreColor = GetGradeColor(session.Score.OverallGrade);
            ImGui.TextColored(scoreColor, $"{session.Score.Overall:F0}/100 ({session.Score.OverallGrade})");
        }
    }

    private static void DrawScores(PerformanceScore score)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Analytics.PerformanceScores, "Performance Scores"));
        ImGui.Separator();

        if (ImGui.BeginTable("ScoresTable", 3, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Analytics.Category, "Category"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Analytics.Score, "Score"), ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Analytics.Grade, "Grade"), ImGuiTableColumnFlags.WidthFixed, 40);

            // GCD Uptime
            DrawScoreRow(Loc.T(LocalizedStrings.Analytics.GcdUptimeScore, "GCD Uptime"), score.GcdUptime);

            // Cooldown Efficiency
            DrawScoreRow(Loc.T(LocalizedStrings.Analytics.CooldownEff, "Cooldown Eff."), score.CooldownEfficiency);

            // Healing Efficiency
            DrawScoreRow(Loc.T(LocalizedStrings.Analytics.HealingEff, "Healing Eff."), score.HealingEfficiency);

            // Survival
            DrawScoreRow(Loc.T(LocalizedStrings.Analytics.Survival, "Survival"), score.Survival);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Separator();
            ImGui.TableNextColumn();
            ImGui.Separator();
            ImGui.TableNextColumn();
            ImGui.Separator();

            // Overall
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Analytics.Overall, "OVERALL"));
            ImGui.TableNextColumn();
            var overallColor = GetGradeColor(score.OverallGrade);
            ImGui.TextColored(overallColor, $"{score.Overall:F0}%");
            ImGui.TableNextColumn();
            ImGui.TextColored(overallColor, score.OverallGrade);

            ImGui.EndTable();
        }
    }

    private static void DrawScoreRow(string label, float score)
    {
        var grade = PerformanceScore.GetGrade(score);
        var color = GetGradeColor(grade);

        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(label);
        ImGui.TableNextColumn();
        ImGui.TextColored(color, $"{score:F0}%");
        ImGui.TableNextColumn();
        ImGui.TextColored(color, grade);
    }

    private static void DrawBreakdown(CombatMetricsSnapshot metrics)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Analytics.DetailedBreakdown, "Detailed Breakdown"));
        ImGui.Separator();

        if (ImGui.BeginTable("BreakdownTable", 2, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Analytics.Metric, "Metric"), ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Analytics.Value, "Value"), ImGuiTableColumnFlags.WidthStretch);

            // Duration
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Analytics.Duration, "Duration:"));
            ImGui.TableNextColumn();
            var minutes = (int)(metrics.CombatDuration / 60);
            var seconds = (int)(metrics.CombatDuration % 60);
            ImGui.Text($"{minutes}:{seconds:D2}");

            // GCD Uptime
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Analytics.GcdUptime, "GCD Uptime:"));
            ImGui.TableNextColumn();
            ImGui.Text($"{metrics.GcdUptime:F1}%");

            // Healing stats (if applicable)
            if (metrics.TotalHealing > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(Loc.T(LocalizedStrings.Analytics.TotalHealing, "Total Healing:"));
                ImGui.TableNextColumn();
                ImGui.Text($"{metrics.TotalHealing:N0}");

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(Loc.T(LocalizedStrings.Analytics.Overheal, "Overheal:"));
                ImGui.TableNextColumn();
                ImGui.Text($"{metrics.OverhealPercent:F1}%");
            }

            // Deaths
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Analytics.Deaths, "Deaths:"));
            ImGui.TableNextColumn();
            var deathColor = metrics.Deaths > 0 ? ErrorColor : NeutralColor;
            ImGui.TextColored(deathColor, metrics.Deaths.ToString());

            // Near-deaths
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Analytics.NearDeaths, "Near-Deaths:"));
            ImGui.TableNextColumn();
            var nearDeathColor = metrics.NearDeaths > 2 ? WarningColor : NeutralColor;
            ImGui.TextColored(nearDeathColor, metrics.NearDeaths.ToString());

            ImGui.EndTable();
        }
    }

    private static void DrawDowntimeAnalysis(DowntimeBreakdown breakdown)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Analytics.DowntimeAnalysis, "Downtime Analysis"));
        ImGui.Separator();

        var total = breakdown.TotalDowntimeSeconds;

        if (total < 0.1f)
        {
            ImGui.TextColored(GradeA, Loc.T(LocalizedStrings.Analytics.NoDowntime, "No significant downtime detected!"));
            return;
        }

        ImGui.Text(Loc.TFormat(LocalizedStrings.Analytics.TotalDowntime, "Total Downtime: {0}s", $"{total:F1}"));
        ImGui.Spacing();

        // Draw individual category bars
        if (breakdown.MovementSeconds > 0)
        {
            DrawDowntimeBar(
                Loc.T(LocalizedStrings.Analytics.Movement, "Movement:"),
                breakdown.MovementSeconds, total, NeutralColor,
                Loc.T(LocalizedStrings.Analytics.MovementTooltip, "Moving while GCD was ready"));
        }

        if (breakdown.MechanicSeconds > 0)
        {
            DrawDowntimeBar(
                Loc.T(LocalizedStrings.Analytics.Mechanics, "Mechanics:"),
                breakdown.MechanicSeconds, total, GradeC,
                Loc.T(LocalizedStrings.Analytics.MechanicsTooltip, "Boss mechanic required attention"));
        }

        if (breakdown.DeathSeconds > 0)
        {
            DrawDowntimeBar(
                Loc.T(LocalizedStrings.Analytics.Death, "Death:"),
                breakdown.DeathSeconds, total, ErrorColor,
                Loc.T(LocalizedStrings.Analytics.DeathTooltip, "Player was dead/incapacitated"));
        }

        if (breakdown.UnforcedSeconds > 0)
        {
            DrawDowntimeBar(
                Loc.T(LocalizedStrings.Analytics.Unexplained, "Unexplained:"),
                breakdown.UnforcedSeconds, total, ErrorColor,
                Loc.T(LocalizedStrings.Analytics.UnexplainedTooltip, "GCD ready with no apparent reason for delay"));
        }

        ImGui.Spacing();

        // Actionable feedback
        if (breakdown.UnforcedSeconds > 5f)
        {
            ImGui.TextColored(WarningColor,
                Loc.TFormat(LocalizedStrings.Analytics.TipUnexplained,
                    "Tip: {0}s of unexplained downtime. Try to always be casting or weaving oGCDs.",
                    $"{breakdown.UnforcedSeconds:F1}"));
        }
        else if (breakdown.MovementSeconds > total * 0.5f)
        {
            ImGui.TextColored(InfoColor,
                Loc.T(LocalizedStrings.Analytics.TipMovement, "Tip: Movement caused most downtime. Use instant casts or slidecast during movement."));
        }
    }

    private static void DrawDowntimeBar(string label, float seconds, float total, Vector4 color, string tooltip)
    {
        var percent = total > 0 ? seconds / total : 0f;

        ImGui.TextColored(color, $"{label}:");
        ImGui.SameLine(100);
        ImGui.Text($"{seconds:F1}s ({percent * 100f:F0}%)");

        // Draw progress bar
        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, color);
        ImGui.ProgressBar(percent, new Vector2(-1, 16), "");
        ImGui.PopStyleColor();

        // Tooltip on hover
        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }

    private static void DrawCooldownAnalysis(IPerformanceTracker tracker)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Analytics.CooldownAnalysis, "Cooldown Analysis"));
        ImGui.Separator();

        var cooldowns = tracker.GetCooldownAnalysis();
        if (cooldowns.Count == 0)
        {
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Analytics.NoCooldownData, "No cooldown data available."));
            return;
        }

        foreach (var cd in cooldowns)
        {
            DrawCooldownEntry(cd);
            ImGui.Spacing();
        }
    }

    private static void DrawCooldownEntry(CooldownAnalysis cd)
    {
        // Header: Ability name and cooldown duration
        ImGui.Text($"{cd.Name} ({cd.CooldownDuration:F0}s)");

        ImGui.Indent(16);

        // Uses line with efficiency bar
        var efficiencyColor = GetEfficiencyColor(cd.Efficiency);
        ImGui.Text(Loc.TFormat(LocalizedStrings.Analytics.UsesFormat,
            "Uses: {0}/{1} ({2}%)",
            cd.TimesUsed.ToString(), cd.OptimalUses.ToString(), $"{cd.Efficiency:F0}"));
        ImGui.SameLine(200);
        DrawEfficiencyBar(cd.Efficiency, efficiencyColor);
        ImGui.SameLine();
        ImGui.TextColored(efficiencyColor, cd.Rating);

        // Average drift (if any)
        if (cd.AverageDrift > 0.5f)
        {
            var driftColor = cd.AverageDrift > 5f ? WarningColor : NeutralColor;
            ImGui.TextColored(driftColor, Loc.TFormat(LocalizedStrings.Analytics.AvgDrift, "Avg Drift: {0}s", $"{cd.AverageDrift:F1}"));
        }

        // Missed opportunities
        if (cd.MissedUsesCount > 0)
        {
            var totalMissedTime = 0f;
            foreach (var missed in cd.MissedOpportunities)
                totalMissedTime += missed.AvailableForSeconds;

            ImGui.TextColored(WarningColor,
                Loc.TFormat(LocalizedStrings.Analytics.MissedFormat,
                    "Missed: {0} opportunity(s) ({1}s total)",
                    cd.MissedUsesCount.ToString(), $"{totalMissedTime:F0}"));
        }

        // Phase breakdown (if we have detailed data)
        if (cd.Uses.Count > 0)
        {
            var phaseText = new System.Text.StringBuilder();
            if (cd.OpenerUses > 0) phaseText.Append(Loc.TFormat(LocalizedStrings.Analytics.Opener, "Opener: {0}", cd.OpenerUses.ToString()) + "  ");
            if (cd.BurstUses > 0) phaseText.Append(Loc.TFormat(LocalizedStrings.Analytics.Burst, "Burst: {0}", cd.BurstUses.ToString()) + "  ");
            if (cd.SustainedUses > 0) phaseText.Append(Loc.TFormat(LocalizedStrings.Analytics.Sustained, "Sustained: {0}", cd.SustainedUses.ToString()));

            if (phaseText.Length > 0)
            {
                ImGui.TextColored(NeutralColor, phaseText.ToString());
            }
        }

        // Tip based on primary issue
        if (!string.IsNullOrEmpty(cd.Tip))
        {
            ImGui.TextColored(InfoColor, $"{Loc.T(LocalizedStrings.Analytics.Tip, "Tip:")} {cd.Tip}");
        }
        else if (cd.Rating == "Excellent")
        {
            ImGui.TextColored(GradeA, Loc.T(LocalizedStrings.Analytics.PerfectUsage, "Perfect usage!"));
        }

        ImGui.Unindent(16);
    }

    private static void DrawEfficiencyBar(float efficiency, Vector4 color)
    {
        // Draw a simple 10-segment bar
        var filled = (int)Math.Round(efficiency / 10f);
        var bar = new string('\u2593', filled) + new string('\u2591', 10 - filled);

        ImGui.PushStyleColor(ImGuiCol.Text, color);
        ImGui.Text($"[{bar}]");
        ImGui.PopStyleColor();
    }

    private static Vector4 GetEfficiencyColor(float efficiency) => efficiency switch
    {
        >= 90f => GradeA,
        >= 75f => GradeB,
        >= 50f => GradeC,
        _ => GradeF
    };

    private static void DrawIssues(FightSession session)
    {
        var issueCount = session.Issues.Count;

        ImGui.Text(Loc.TFormat(LocalizedStrings.Analytics.IssuesFormat, "Issues ({0})", issueCount.ToString()));
        ImGui.Separator();

        if (issueCount == 0)
        {
            ImGui.TextColored(GradeA, Loc.T(LocalizedStrings.Analytics.NoIssues, "No significant issues detected."));
            return;
        }

        foreach (var issue in session.Issues)
        {
            var (icon, color) = issue.Severity switch
            {
                IssueSeverity.Error => ("[!]", ErrorColor),
                IssueSeverity.Warning => ("[!]", WarningColor),
                _ => ("[i]", InfoColor)
            };

            ImGui.TextColored(color, icon);
            ImGui.SameLine();
            ImGui.TextWrapped(issue.Description);

            if (!string.IsNullOrEmpty(issue.Suggestion))
            {
                ImGui.Indent(20);
                ImGui.TextColored(NeutralColor, $"{Loc.T(LocalizedStrings.Analytics.Tip, "Tip:")} {issue.Suggestion}");
                ImGui.Unindent(20);
            }
        }
    }

    private static Vector4 GetGradeColor(string grade) => grade switch
    {
        "A+" or "A" or "A-" => GradeA,
        "B+" or "B" or "B-" => GradeB,
        "C+" or "C" or "C-" => GradeC,
        "D+" or "D" or "D-" => GradeD,
        _ => GradeF
    };

    private static bool IsSectionVisible(AnalyticsConfig config, string section)
    {
        if (config.SectionVisibility.TryGetValue(section, out var visible))
            return visible;
        return true;
    }
}
