using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Config;
using Olympus.Localization;
using Olympus.Services.Analytics;
using Olympus.Services.FFLogs;
using Olympus.Windows.Analytics.Tabs;

namespace Olympus.Windows;

/// <summary>
/// Analytics window with tabbed interface for performance metrics display.
/// </summary>
public sealed class AnalyticsWindow : Window
{
    private readonly IPerformanceTracker performanceTracker;
    private readonly Configuration configuration;
    private readonly Action saveConfiguration;
    private readonly IFFlogsService? fflogsService;
    private readonly IFightSummaryService? fightSummaryService;

    public AnalyticsWindow(IPerformanceTracker performanceTracker, Configuration configuration, Action saveConfiguration, IFFlogsService? fflogsService = null, IFightSummaryService? fightSummaryService = null)
        : base("Olympus Analytics", ImGuiWindowFlags.NoSavedSettings)
    {
        this.performanceTracker = performanceTracker;
        this.configuration = configuration;
        this.saveConfiguration = saveConfiguration;
        this.fflogsService = fflogsService;
        this.fightSummaryService = fightSummaryService;

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
            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Analytics.RealtimeTab, "Realtime")))
            {
                RealtimeTab.Draw(performanceTracker, configuration.Analytics);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Analytics.FightSummaryTab, "Fight Summary")))
            {
                FightSummaryTab.Draw(performanceTracker, configuration.Analytics);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Analytics.HistoryTab, "History")))
            {
                HistoryTab.Draw(performanceTracker, configuration.Analytics);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Analytics.FFlogsTab, "FFLogs")))
            {
                FFlogsTab.Draw(fflogsService, configuration.FFLogs);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Analytics.PullHistoryTab, "Pull History")))
            {
                if (fightSummaryService != null)
                    PullHistoryTab.Draw(fightSummaryService, configuration.Analytics);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawSettingsDropdown()
    {
        if (ImGui.BeginCombo("##AnalyticsSettings", Loc.T(LocalizedStrings.Analytics.SectionVisibility, "Section Visibility"), ImGuiComboFlags.NoArrowButton))
        {
            ImGui.Text(Loc.T(LocalizedStrings.Analytics.RealtimeTabLabel, "Realtime Tab"));
            ImGui.Separator();
            DrawSectionToggle("RealtimeCombatStatus", Loc.T(LocalizedStrings.Analytics.CombatStatus, "Combat Status"));
            DrawSectionToggle("RealtimeMetrics", Loc.T(LocalizedStrings.Analytics.Metrics, "Metrics"));
            DrawSectionToggle("RealtimeCooldowns", Loc.T(LocalizedStrings.Analytics.Cooldowns, "Cooldowns"));

            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Analytics.FightSummaryTabLabel, "Fight Summary Tab"));
            ImGui.Separator();
            DrawSectionToggle("SummaryScores", Loc.T(LocalizedStrings.Analytics.Scores, "Scores"));
            DrawSectionToggle("SummaryBreakdown", Loc.T(LocalizedStrings.Analytics.Breakdown, "Breakdown"));
            DrawSectionToggle("SummaryDowntime", Loc.T(LocalizedStrings.Analytics.DowntimeAnalysis, "Downtime Analysis"));
            DrawSectionToggle("SummaryCooldowns", Loc.T(LocalizedStrings.Analytics.Cooldowns, "Cooldowns"));
            DrawSectionToggle("SummaryIssues", Loc.T(LocalizedStrings.Analytics.Issues, "Issues"));

            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Analytics.HistoryTabLabel, "History Tab"));
            ImGui.Separator();
            DrawSectionToggle("HistorySessions", Loc.T(LocalizedStrings.Analytics.Sessions, "Sessions"));
            DrawSectionToggle("HistoryTrends", Loc.T(LocalizedStrings.Analytics.Trends, "Trends"));

            ImGui.EndCombo();
        }

        ImGui.SameLine();

        // Tracking toggle
        var enableTracking = configuration.Analytics.EnableTracking;
        if (ImGui.Checkbox(Loc.T(LocalizedStrings.Analytics.EnableTracking, "Enable Tracking"), ref enableTracking))
        {
            configuration.Analytics.EnableTracking = enableTracking;
            saveConfiguration();
        }
    }

    private void DrawSectionToggle(string key, string label)
    {
        if (!configuration.Analytics.SectionVisibility.TryGetValue(key, out var visible))
            visible = true;

        if (ImGui.Checkbox(label, ref visible))
        {
            configuration.Analytics.SectionVisibility[key] = visible;
            saveConfiguration();
        }
    }
}
