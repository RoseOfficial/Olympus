using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Config;
using Olympus.Services.Analytics;
using Olympus.Windows.Analytics.Tabs;

namespace Olympus.Windows;

/// <summary>
/// Analytics window with tabbed interface for performance metrics display.
/// </summary>
public sealed class AnalyticsWindow : Window
{
    private readonly IPerformanceTracker performanceTracker;
    private readonly Configuration configuration;

    public AnalyticsWindow(IPerformanceTracker performanceTracker, Configuration configuration)
        : base("Olympus Analytics", ImGuiWindowFlags.NoSavedSettings)
    {
        this.performanceTracker = performanceTracker;
        this.configuration = configuration;

        Size = new Vector2(500, 400);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void OnOpen()
    {
        configuration.Analytics.AnalyticsWindowVisible = true;
    }

    public override void OnClose()
    {
        configuration.Analytics.AnalyticsWindowVisible = false;
    }

    public override void Draw()
    {
        // Settings dropdown
        DrawSettingsDropdown();

        ImGui.Separator();

        // Tab bar
        if (ImGui.BeginTabBar("AnalyticsTabs"))
        {
            if (ImGui.BeginTabItem("Realtime"))
            {
                RealtimeTab.Draw(performanceTracker, configuration.Analytics);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Fight Summary"))
            {
                FightSummaryTab.Draw(performanceTracker, configuration.Analytics);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("History"))
            {
                HistoryTab.Draw(performanceTracker, configuration.Analytics);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawSettingsDropdown()
    {
        if (ImGui.BeginCombo("##AnalyticsSettings", "Section Visibility", ImGuiComboFlags.NoArrowButton))
        {
            ImGui.Text("Realtime Tab");
            ImGui.Separator();
            DrawSectionToggle("RealtimeCombatStatus", "Combat Status");
            DrawSectionToggle("RealtimeMetrics", "Metrics");
            DrawSectionToggle("RealtimeCooldowns", "Cooldowns");

            ImGui.Spacing();
            ImGui.Text("Fight Summary Tab");
            ImGui.Separator();
            DrawSectionToggle("SummaryScores", "Scores");
            DrawSectionToggle("SummaryBreakdown", "Breakdown");
            DrawSectionToggle("SummaryDowntime", "Downtime Analysis");
            DrawSectionToggle("SummaryIssues", "Issues");

            ImGui.Spacing();
            ImGui.Text("History Tab");
            ImGui.Separator();
            DrawSectionToggle("HistorySessions", "Sessions");
            DrawSectionToggle("HistoryTrends", "Trends");

            ImGui.EndCombo();
        }

        ImGui.SameLine();

        // Tracking toggle
        var enableTracking = configuration.Analytics.EnableTracking;
        if (ImGui.Checkbox("Enable Tracking", ref enableTracking))
        {
            configuration.Analytics.EnableTracking = enableTracking;
        }
    }

    private void DrawSectionToggle(string key, string label)
    {
        if (!configuration.Analytics.SectionVisibility.TryGetValue(key, out var visible))
            visible = true;

        if (ImGui.Checkbox(label, ref visible))
        {
            configuration.Analytics.SectionVisibility[key] = visible;
        }
    }
}
