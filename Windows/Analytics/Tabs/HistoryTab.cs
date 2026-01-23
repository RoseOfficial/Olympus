using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Services.Analytics;

namespace Olympus.Windows.Analytics.Tabs;

/// <summary>
/// History tab: past sessions and performance trends.
/// </summary>
public static class HistoryTab
{
    // Colors
    private static readonly Vector4 GradeA = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 GradeB = new(0.5f, 0.8f, 0.3f, 1.0f);
    private static readonly Vector4 GradeC = new(0.9f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 GradeD = new(0.9f, 0.6f, 0.3f, 1.0f);
    private static readonly Vector4 GradeF = new(0.9f, 0.3f, 0.3f, 1.0f);
    private static readonly Vector4 NeutralColor = new(0.7f, 0.7f, 0.7f, 1.0f);
    private static readonly Vector4 ImprovingColor = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 DecliningColor = new(0.9f, 0.3f, 0.3f, 1.0f);

    public static void Draw(IPerformanceTracker tracker, AnalyticsConfig config)
    {
        var sessions = tracker.GetSessionHistory();
        var trend = tracker.GetTrend();

        // Trends Section
        if (IsSectionVisible(config, "HistoryTrends"))
        {
            DrawTrends(trend, sessions.Count);
            ImGui.Spacing();
        }

        // Sessions Section
        if (IsSectionVisible(config, "HistorySessions"))
        {
            DrawSessions(sessions, tracker);
        }
    }

    private static void DrawTrends(PerformanceTrend? trend, int sessionCount)
    {
        ImGui.Text("Performance Trends");
        ImGui.Separator();

        if (trend == null)
        {
            ImGui.TextColored(NeutralColor, $"Need at least 3 sessions for trends ({sessionCount} recorded).");
            return;
        }

        if (ImGui.BeginTable("TrendsTable", 2, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Metric", ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Sessions analyzed
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Sessions:");
            ImGui.TableNextColumn();
            ImGui.Text(trend.SessionCount.ToString());

            // Average score
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Avg Score:");
            ImGui.TableNextColumn();
            var avgColor = GetGradeColor(PerformanceScore.GetGrade(trend.AverageScore));
            ImGui.TextColored(avgColor, $"{trend.AverageScore:F0}/100");

            // Average GCD uptime
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Avg GCD Uptime:");
            ImGui.TableNextColumn();
            ImGui.Text($"{trend.AverageGcdUptime:F1}%");

            // Trend direction
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Trend:");
            ImGui.TableNextColumn();
            var trendColor = trend.ScoreTrend switch
            {
                > 2f => ImprovingColor,
                < -2f => DecliningColor,
                _ => NeutralColor
            };
            var trendIcon = trend.ScoreTrend switch
            {
                > 2f => "+",
                < -2f => "-",
                _ => "="
            };
            ImGui.TextColored(trendColor, $"{trendIcon} {trend.TrendDescription} ({trend.ScoreTrend:+0.0;-0.0;0})");

            ImGui.EndTable();
        }
    }

    private static void DrawSessions(System.Collections.Generic.IReadOnlyList<FightSession> sessions, IPerformanceTracker tracker)
    {
        ImGui.Text($"Session History ({sessions.Count})");
        ImGui.Separator();

        if (sessions.Count == 0)
        {
            ImGui.TextColored(NeutralColor, "No sessions recorded yet.");
            ImGui.TextColored(NeutralColor, "Complete combat encounters to build history.");
            return;
        }

        // Clear history button
        if (ImGui.Button("Clear History"))
        {
            tracker.ClearHistory();
        }

        ImGui.Spacing();

        // Sessions table
        if (ImGui.BeginTable("SessionsTable", 5,
            ImGuiTableFlags.BordersOuter | ImGuiTableFlags.BordersInnerH |
            ImGuiTableFlags.RowBg | ImGuiTableFlags.ScrollY,
            new Vector2(0, 200)))
        {
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Duration", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Score", ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn("Grade", ImGuiTableColumnFlags.WidthFixed, 40);
            ImGui.TableSetupColumn("GCD%", ImGuiTableColumnFlags.WidthFixed, 50);

            ImGui.TableHeadersRow();

            foreach (var session in sessions)
            {
                ImGui.TableNextRow();

                // Time
                ImGui.TableNextColumn();
                ImGui.Text(session.StartTime.ToString("HH:mm"));

                // Duration
                ImGui.TableNextColumn();
                var minutes = (int)(session.Duration / 60);
                var seconds = (int)(session.Duration % 60);
                ImGui.Text($"{minutes}:{seconds:D2}");

                // Score
                ImGui.TableNextColumn();
                var score = session.Score?.Overall ?? 0;
                ImGui.Text($"{score:F0}");

                // Grade
                ImGui.TableNextColumn();
                var grade = session.Score?.OverallGrade ?? "?";
                var gradeColor = GetGradeColor(grade);
                ImGui.TextColored(gradeColor, grade);

                // GCD Uptime
                ImGui.TableNextColumn();
                var gcdUptime = session.FinalMetrics?.GcdUptime ?? 0;
                ImGui.Text($"{gcdUptime:F0}%");
            }

            ImGui.EndTable();
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
