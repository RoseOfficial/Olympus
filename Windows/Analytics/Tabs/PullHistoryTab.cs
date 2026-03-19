using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
using Olympus.Localization;
using Olympus.Models;
using Olympus.Services.Analytics;

namespace Olympus.Windows.Analytics.Tabs;

/// <summary>
/// Pull History tab: browse past coaching summaries with callout detail and trend display.
/// </summary>
public static class PullHistoryTab
{
    private static int _selectedIndex;

    // Colors (match FightSummaryWindow)
    private static readonly Vector4 GradeS = new(1.0f, 0.84f, 0.0f, 1f);
    private static readonly Vector4 GradeA = new(0.13f, 0.77f, 0.37f, 1f);
    private static readonly Vector4 GradeB = new(0.98f, 0.75f, 0.14f, 1f);
    private static readonly Vector4 GradeC = new(0.96f, 0.62f, 0.04f, 1f);
    private static readonly Vector4 GradeD = new(0.97f, 0.44f, 0.44f, 1f);
    private static readonly Vector4 NeutralColor = new(0.7f, 0.7f, 0.7f, 1f);
    private static readonly Vector4 CriticalColor = new(0.97f, 0.44f, 0.44f, 1f);
    private static readonly Vector4 WarningColor = new(0.98f, 0.75f, 0.14f, 1f);
    private static readonly Vector4 GoodColor = new(0.53f, 0.94f, 0.67f, 1f);
    private static readonly Vector4 GcdUptimeColor = new(0.13f, 0.77f, 0.37f, 1f);

    public static void Draw(IFightSummaryService service, AnalyticsConfig config)
    {
        var summaries = service.RecentSummaries;

        if (summaries.Count == 0)
        {
            ImGui.TextColored(NeutralColor,
                Loc.T(LocalizedStrings.Analytics.NoPullHistory, "No pulls recorded this session."));
            return;
        }

        // Pull list section
        if (IsSectionVisible(config, "PullHistoryList"))
        {
            DrawPullList(summaries);
            ImGui.Spacing();
        }

        // Selected pull detail section
        if (IsSectionVisible(config, "PullHistoryDetail"))
        {
            DrawSelectedDetail(summaries);
            ImGui.Spacing();
        }

        // Trend section
        if (IsSectionVisible(config, "PullHistoryTrend"))
        {
            DrawTrend(summaries);
        }
    }

    private static void DrawPullList(System.Collections.Generic.IReadOnlyList<FightSummaryRecord> summaries)
    {
        if (ImGui.BeginTable("PullHistoryTable", 5,
            ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerH |
            ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
            new Vector2(0, 180)))
        {
            ImGui.TableSetupColumn("#", ImGuiTableColumnFlags.WidthFixed, 30);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.FightSummary.Grade, "Grade"), ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Analytics.Duration, "Duration"), ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.FightSummary.GcdUptime, "GCD Uptime"), ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Job", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableHeadersRow();

            for (var i = 0; i < summaries.Count; i++)
            {
                var summary = summaries[i];
                ImGui.TableNextRow();

                // Pull number column — selectable spans entire row
                ImGui.TableNextColumn();
                var isSelected = _selectedIndex == i;
                if (ImGui.Selectable($"{summaries.Count - i}##pull{i}", isSelected,
                    ImGuiSelectableFlags.SpanAllColumns))
                {
                    _selectedIndex = i;
                }

                // Grade
                ImGui.TableNextColumn();
                ImGui.TextColored(GetGradeColor(summary.Grade), summary.Grade);

                // Duration
                ImGui.TableNextColumn();
                ImGui.Text($"{summary.Duration.Minutes}:{summary.Duration.Seconds:D2}");

                // GCD Uptime
                ImGui.TableNextColumn();
                ImGui.Text($"{summary.GcdUptimePercent:F1}%");

                // Job
                ImGui.TableNextColumn();
                ImGui.Text(JobRegistry.GetJobName(summary.JobId));
            }

            ImGui.EndTable();
        }
    }

    private static void DrawSelectedDetail(System.Collections.Generic.IReadOnlyList<FightSummaryRecord> summaries)
    {
        if (_selectedIndex < 0 || _selectedIndex >= summaries.Count)
        {
            _selectedIndex = 0;
            if (summaries.Count == 0)
                return;
        }

        var selected = summaries[_selectedIndex];

        ImGui.Separator();

        // Header
        var jobName = JobRegistry.GetJobName(selected.JobId);
        ImGui.TextDisabled($"{jobName} · {selected.ZoneName} · {selected.Duration.Minutes}m{selected.Duration.Seconds:D2}s");

        ImGui.Spacing();

        // Callouts (same rendering as popup)
        if (selected.Callouts.Count == 0)
        {
            ImGui.TextColored(GoodColor,
                Loc.T(LocalizedStrings.Analytics.NoIssues, "No significant issues detected."));
        }
        else
        {
            ImGui.TextDisabled(
                Loc.T(LocalizedStrings.FightSummary.ImproveNextPull, "Improve Next Pull")
                    .ToUpperInvariant());
            ImGui.Spacing();

            foreach (var callout in selected.Callouts)
            {
                var severityColor = GetSeverityColor(callout.Severity);

                ImGui.PushStyleColor(ImGuiCol.Text, severityColor);
                ImGui.Text(callout.Title);
                ImGui.PopStyleColor();

                ImGui.TextDisabled(callout.Description);
                ImGui.Spacing();
            }
        }
    }

    private static void DrawTrend(System.Collections.Generic.IReadOnlyList<FightSummaryRecord> summaries)
    {
        ImGui.Separator();
        ImGui.Text(Loc.T(LocalizedStrings.Analytics.TrendLabel, "GCD Uptime Trend"));
        ImGui.Spacing();

        // Show GCD uptime per pull as progress bars (most recent first in list, but display oldest→newest)
        for (var i = summaries.Count - 1; i >= 0; i--)
        {
            var s = summaries[i];
            var pullNum = summaries.Count - i;
            var fraction = s.GcdUptimePercent / 100f;
            if (fraction > 1f) fraction = 1f;

            ImGui.PushStyleColor(ImGuiCol.PlotHistogram, GcdUptimeColor);
            ImGui.ProgressBar(fraction, new Vector2(-1, 16),
                $"#{pullNum} {s.GcdUptimePercent:F1}% ({s.Grade})");
            ImGui.PopStyleColor();
        }
    }

    private static Vector4 GetGradeColor(string grade)
    {
        if (grade.StartsWith("S"))
            return GradeS;
        if (grade.StartsWith("A"))
            return GradeA;
        if (grade.StartsWith("B"))
            return GradeB;
        if (grade.StartsWith("C"))
            return GradeC;
        return GradeD;
    }

    private static Vector4 GetSeverityColor(CalloutSeverity severity) => severity switch
    {
        CalloutSeverity.Critical => CriticalColor,
        CalloutSeverity.Warning => WarningColor,
        CalloutSeverity.Good => GoodColor,
        _ => WarningColor,
    };

    private static bool IsSectionVisible(AnalyticsConfig config, string section)
    {
        if (config.SectionVisibility.TryGetValue(section, out var visible))
            return visible;
        return true;
    }
}
