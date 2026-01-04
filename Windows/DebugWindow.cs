using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Services.Debug;
using Olympus.Windows.Debug.Tabs;

namespace Olympus.Windows;

/// <summary>
/// Debug window with tabbed interface for organized debug information display.
/// </summary>
public sealed class DebugWindow : Window
{
    private readonly DebugService _debugService;
    private readonly Configuration _configuration;

    public DebugWindow(DebugService debugService, Configuration configuration)
        : base("Olympus Debug", ImGuiWindowFlags.None)
    {
        _debugService = debugService;
        _configuration = configuration;

        Size = new Vector2(550, 450);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void OnOpen()
    {
        _configuration.IsDebugWindowOpen = true;
    }

    public override void OnClose()
    {
        _configuration.IsDebugWindowOpen = false;
    }

    public override void Draw()
    {
        var snapshot = _debugService.GetSnapshot();

        // Settings button
        DrawSettingsButton();

        ImGui.Separator();

        // Tab bar
        if (ImGui.BeginTabBar("DebugTabs"))
        {
            if (ImGui.BeginTabItem("Overview"))
            {
                OverviewTab.Draw(snapshot, _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Why Stuck?"))
            {
                WhyStuckTab.Draw(snapshot, _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Healing"))
            {
                HealingTab.Draw(snapshot, _configuration, _debugService);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Actions"))
            {
                ActionsTab.Draw(snapshot, _configuration, _debugService);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Performance"))
            {
                PerformanceTab.Draw(snapshot, _configuration);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawSettingsButton()
    {
        // Settings dropdown for section visibility
        if (ImGui.BeginCombo("##SectionSettings", "Section Visibility", ImGuiComboFlags.NoArrowButton))
        {
            ImGui.Text("Overview Tab");
            ImGui.Separator();
            DrawSectionToggle("GcdPlanning", "GCD Planning");
            DrawSectionToggle("QuickStats", "Quick Stats");

            ImGui.Spacing();
            ImGui.Text("Why Stuck? Tab");
            ImGui.Separator();
            DrawSectionToggle("GcdPriority", "GCD Priority Chain");
            DrawSectionToggle("OgcdState", "oGCD State");
            DrawSectionToggle("DpsDetails", "DPS Details");
            DrawSectionToggle("Resources", "Resources");

            ImGui.Spacing();
            ImGui.Text("Healing Tab");
            ImGui.Separator();
            DrawSectionToggle("SpellStatus", "Spell Status");
            DrawSectionToggle("SpellSelection", "Spell Selection");
            DrawSectionToggle("HpPrediction", "HP Prediction");
            DrawSectionToggle("AoEHealing", "AoE Healing");
            DrawSectionToggle("RecentHeals", "Recent Heals");
            DrawSectionToggle("ShadowHp", "Shadow HP");

            ImGui.Spacing();
            ImGui.Text("Actions Tab");
            ImGui.Separator();
            DrawSectionToggle("GcdDetails", "GCD Details");
            DrawSectionToggle("SpellUsage", "Spell Usage");
            DrawSectionToggle("ActionHistory", "Action History");

            ImGui.Spacing();
            ImGui.Text("Performance Tab");
            ImGui.Separator();
            DrawSectionToggle("Statistics", "Statistics");
            DrawSectionToggle("Downtime", "Downtime");

            ImGui.EndCombo();
        }
    }

    private void DrawSectionToggle(string key, string label)
    {
        if (!_configuration.Debug.DebugSectionVisibility.TryGetValue(key, out var visible))
            visible = true;

        if (ImGui.Checkbox(label, ref visible))
        {
            _configuration.Debug.DebugSectionVisibility[key] = visible;
        }
    }
}
