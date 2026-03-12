using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Localization;
using Olympus.Services.Debug;
using Olympus.Timeline;
using Olympus.Windows.Debug.Tabs;

namespace Olympus.Windows;

/// <summary>
/// Debug window with tabbed interface for organized debug information display.
/// </summary>
public sealed class DebugWindow : Window
{
    private readonly DebugService _debugService;
    private readonly Configuration _configuration;
    private readonly ITimelineService? _timelineService;

    public DebugWindow(DebugService debugService, Configuration configuration, ITimelineService? timelineService = null)
        : base(Loc.T(LocalizedStrings.Debug.WindowTitle, "Olympus Debug"), ImGuiWindowFlags.NoSavedSettings)
    {
        _debugService = debugService;
        _configuration = configuration;
        _timelineService = timelineService;

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
            // Tank Tabs
            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabWarrior, "Warrior")))
            {
                AresTab.Draw(_debugService.GetAresDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabDarkKnight, "Dark Knight")))
            {
                NyxTab.Draw(_debugService.GetNyxDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabPaladin, "Paladin")))
            {
                ThemisTab.Draw(_debugService.GetThemisDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabGunbreaker, "Gunbreaker")))
            {
                HephaestusTab.Draw(_debugService.GetHephaestusDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabOverview, "Overview")))
            {
                OverviewTab.Draw(snapshot, _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabWhyStuck, "Why Stuck?")))
            {
                WhyStuckTab.Draw(snapshot, _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabHealing, "Healing")))
            {
                HealingTab.Draw(snapshot, _configuration, _debugService);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabOverheal, "Overheal")))
            {
                OverhealTab.Draw(snapshot, _configuration, _debugService);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabActions, "Actions")))
            {
                ActionsTab.Draw(snapshot, _configuration, _debugService);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabPerformance, "Performance")))
            {
                PerformanceTab.Draw(snapshot, _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabWhiteMage, "White Mage")))
            {
                ApolloTab.Draw(_debugService.GetApolloDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabSage, "Sage")))
            {
                AsclepiusTab.Draw(_debugService.GetAsclepiusDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabScholar, "Scholar")))
            {
                ScholarTab.Draw(_debugService.GetAthenaDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabAstrologian, "Astrologian")))
            {
                AstrologianTab.Draw(_debugService.GetAstraeaDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            // Melee DPS Tabs
            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabDragoon, "Dragoon")))
            {
                ZeusTab.Draw(_debugService.GetZeusDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabNinja, "Ninja")))
            {
                HermesTab.Draw(_debugService.GetHermesDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabSamurai, "Samurai")))
            {
                NikeTab.Draw(_debugService.GetNikeDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabMonk, "Monk")))
            {
                KratosTab.Draw(_debugService.GetKratosDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabReaper, "Reaper")))
            {
                ThanatosTab.Draw(_debugService.GetThanatosDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabViper, "Viper")))
            {
                EchidnaTab.Draw(_debugService.GetEchidnaDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            // Ranged Physical DPS Tabs
            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabMachinist, "Machinist")))
            {
                PrometheusTab.Draw(_debugService.GetPrometheusDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabBard, "Bard")))
            {
                CalliopeTab.Draw(_debugService.GetCalliopeDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabDancer, "Dancer")))
            {
                TerpsichoreTab.Draw(_debugService.GetTerpsichoreDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            // Caster DPS Tabs
            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabBlackMage, "Black Mage")))
            {
                HecateTab.Draw(_debugService.GetHecateDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabSummoner, "Summoner")))
            {
                PersephoneTab.Draw(_debugService.GetPersephoneDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabRedMage, "Red Mage")))
            {
                CirceTab.Draw(_debugService.GetCirceDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabPictomancer, "Pictomancer")))
            {
                IrisTab.Draw(_debugService.GetIrisDebugState(), _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabTimeline, "Timeline")))
            {
                TimelineTab.Draw(_timelineService, _configuration);
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }
    }

    private void DrawSettingsButton()
    {
        // Settings dropdown for section visibility
        if (ImGui.BeginCombo("##SectionSettings", Loc.T(LocalizedStrings.Debug.SectionVisibility, "Section Visibility"), ImGuiComboFlags.NoArrowButton))
        {
            ImGui.Text(Loc.T(LocalizedStrings.Debug.OverviewTabLabel, "Overview Tab"));
            ImGui.Separator();
            DrawSectionToggle("GcdPlanning", Loc.T(LocalizedStrings.Debug.GcdPlanning, "GCD Planning"));
            DrawSectionToggle("QuickStats", Loc.T(LocalizedStrings.Debug.QuickStats, "Quick Stats"));

            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.WhyStuckTabLabel, "Why Stuck? Tab"));
            ImGui.Separator();
            DrawSectionToggle("GcdPriority", Loc.T(LocalizedStrings.Debug.GcdPriorityChain, "GCD Priority Chain"));
            DrawSectionToggle("OgcdState", Loc.T(LocalizedStrings.Debug.OgcdState, "oGCD State"));
            DrawSectionToggle("DpsDetails", Loc.T(LocalizedStrings.Debug.DpsDetails, "DPS Details"));
            DrawSectionToggle("Resources", Loc.T(LocalizedStrings.Debug.ResourcesSection, "Resources"));

            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.HealingTabLabel, "Healing Tab"));
            ImGui.Separator();
            DrawSectionToggle("SpellStatus", Loc.T(LocalizedStrings.Debug.SpellStatus, "Spell Status"));
            DrawSectionToggle("SpellSelection", Loc.T(LocalizedStrings.Debug.SpellSelection, "Spell Selection"));
            DrawSectionToggle("HpPrediction", Loc.T(LocalizedStrings.Debug.HpPrediction, "HP Prediction"));
            DrawSectionToggle("AoEHealing", Loc.T(LocalizedStrings.Debug.AoEHealing, "AoE Healing"));
            DrawSectionToggle("RecentHeals", Loc.T(LocalizedStrings.Debug.RecentHeals, "Recent Heals"));
            DrawSectionToggle("ShadowHp", Loc.T(LocalizedStrings.Debug.ShadowHp, "Shadow HP"));

            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.OverhealTabLabel, "Overheal Tab"));
            ImGui.Separator();
            DrawSectionToggle("OverhealSummary", Loc.T(LocalizedStrings.Debug.OverhealSummary, "Summary"));
            DrawSectionToggle("OverhealBySpell", Loc.T(LocalizedStrings.Debug.OverhealBySpell, "By Spell"));
            DrawSectionToggle("OverhealByTarget", Loc.T(LocalizedStrings.Debug.OverhealByTarget, "By Target"));
            DrawSectionToggle("OverhealTimeline", Loc.T(LocalizedStrings.Debug.OverhealTimeline, "Timeline"));
            DrawSectionToggle("OverhealControls", Loc.T(LocalizedStrings.Debug.OverhealControls, "Controls"));

            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ActionsTabLabel, "Actions Tab"));
            ImGui.Separator();
            DrawSectionToggle("GcdDetails", Loc.T(LocalizedStrings.Debug.GcdDetails, "GCD Details"));
            DrawSectionToggle("SpellUsage", Loc.T(LocalizedStrings.Debug.SpellUsage, "Spell Usage"));
            DrawSectionToggle("ActionHistory", Loc.T(LocalizedStrings.Debug.ActionHistory, "Action History"));

            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PerformanceTabLabel, "Performance Tab"));
            ImGui.Separator();
            DrawSectionToggle("Statistics", Loc.T(LocalizedStrings.Debug.Statistics, "Statistics"));
            DrawSectionToggle("Downtime", Loc.T(LocalizedStrings.Debug.Downtime, "Downtime"));

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
