using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Data;
using Olympus.Localization;
using Olympus.Services.Debug;
using Olympus.Services.Targeting;
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
    private readonly SmartAoETab? _smartAoETab;

    private uint _selectedJobId; // 0 = unset; auto-selects active job on next Draw

    // Ordered list for the Job Details combo. Advanced jobs only — no base classes.
    // Entries with JobId == 0 are non-selectable role group headers.
    private static readonly (uint JobId, string DisplayName)[] JobList =
    [
        (0,                       "── Tanks ──"),
        (JobRegistry.Warrior,     JobRegistry.GetJobName(JobRegistry.Warrior)),
        (JobRegistry.Paladin,     JobRegistry.GetJobName(JobRegistry.Paladin)),
        (JobRegistry.DarkKnight,  JobRegistry.GetJobName(JobRegistry.DarkKnight)),
        (JobRegistry.Gunbreaker,  JobRegistry.GetJobName(JobRegistry.Gunbreaker)),
        (0,                       "── Healers ──"),
        (JobRegistry.WhiteMage,   JobRegistry.GetJobName(JobRegistry.WhiteMage)),
        (JobRegistry.Scholar,     JobRegistry.GetJobName(JobRegistry.Scholar)),
        (JobRegistry.Astrologian, JobRegistry.GetJobName(JobRegistry.Astrologian)),
        (JobRegistry.Sage,        JobRegistry.GetJobName(JobRegistry.Sage)),
        (0,                       "── Melee DPS ──"),
        (JobRegistry.Monk,        JobRegistry.GetJobName(JobRegistry.Monk)),
        (JobRegistry.Dragoon,     JobRegistry.GetJobName(JobRegistry.Dragoon)),
        (JobRegistry.Ninja,       JobRegistry.GetJobName(JobRegistry.Ninja)),
        (JobRegistry.Samurai,     JobRegistry.GetJobName(JobRegistry.Samurai)),
        (JobRegistry.Reaper,      JobRegistry.GetJobName(JobRegistry.Reaper)),
        (JobRegistry.Viper,       JobRegistry.GetJobName(JobRegistry.Viper)),
        (0,                       "── Physical Ranged ──"),
        (JobRegistry.Bard,        JobRegistry.GetJobName(JobRegistry.Bard)),
        (JobRegistry.Machinist,   JobRegistry.GetJobName(JobRegistry.Machinist)),
        (JobRegistry.Dancer,      JobRegistry.GetJobName(JobRegistry.Dancer)),
        (0,                       "── Casters ──"),
        (JobRegistry.BlackMage,   JobRegistry.GetJobName(JobRegistry.BlackMage)),
        (JobRegistry.Summoner,    JobRegistry.GetJobName(JobRegistry.Summoner)),
        (JobRegistry.RedMage,     JobRegistry.GetJobName(JobRegistry.RedMage)),
        (JobRegistry.Pictomancer, JobRegistry.GetJobName(JobRegistry.Pictomancer)),
    ];

    public DebugWindow(DebugService debugService, Configuration configuration, ITimelineService? timelineService = null, SmartAoETab? smartAoETab = null)
        : base(Loc.T(LocalizedStrings.Debug.WindowTitle, "Olympus Debug"), ImGuiWindowFlags.NoSavedSettings)
    {
        _debugService = debugService;
        _configuration = configuration;
        _timelineService = timelineService;
        _smartAoETab = smartAoETab;

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
        _selectedJobId = 0;
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
            // Generic Tabs
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

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabChecklist, "Checklist")))
            {
                ChecklistTab.Draw(snapshot, _debugService);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabPerformance, "Performance")))
            {
                PerformanceTab.Draw(snapshot, _configuration);
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabJobDetails, "Job Details")))
            {
                DrawJobDetailsTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem(Loc.T(LocalizedStrings.Debug.TabTimeline, "Timeline")))
            {
                TimelineTab.Draw(_timelineService, _configuration);
                ImGui.EndTabItem();
            }

            if (_smartAoETab != null && ImGui.BeginTabItem("Smart AoE"))
            {
                _smartAoETab.Draw(_configuration);
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

    private void DrawJobDetailsTab()
    {
        // Auto-select current job each frame until a valid job is resolved (resets on close)
        if (_selectedJobId == 0)
        {
            var rawJobId = _debugService.GetJobId();
            _selectedJobId = MapToAdvancedJobId(rawJobId);
        }

        // Resolve display name for the combo preview
        var previewName = _selectedJobId != 0
            ? JobRegistry.GetJobName(_selectedJobId)
            : "Unknown";

        // Job selector combo
        if (ImGui.BeginCombo("##JobSelector", previewName))
        {
            foreach (var (jobId, displayName) in JobList)
            {
                if (jobId == 0)
                {
                    // Non-selectable role header
                    ImGui.Selectable(displayName, false, ImGuiSelectableFlags.Disabled);
                }
                else
                {
                    var isSelected = jobId == _selectedJobId;
                    if (ImGui.Selectable("  " + displayName, isSelected))
                        _selectedJobId = jobId;
                    if (isSelected)
                        ImGui.SetItemDefaultFocus();
                }
            }
            ImGui.EndCombo();
        }

        ImGui.Separator();

        // Fallback: player not logged in or job unresolved
        if (_selectedJobId == 0)
        {
            ImGui.TextDisabled("Not logged in.");
            return;
        }

        // Dispatch to the appropriate job tab
        switch (_selectedJobId)
        {
            // Tanks
            case JobRegistry.Warrior:     AresTab.Draw(_debugService.GetAresDebugState(), _configuration); break;
            case JobRegistry.Paladin:     ThemisTab.Draw(_debugService.GetThemisDebugState(), _configuration); break;
            case JobRegistry.DarkKnight:  NyxTab.Draw(_debugService.GetNyxDebugState(), _configuration); break;
            case JobRegistry.Gunbreaker:  HephaestusTab.Draw(_debugService.GetHephaestusDebugState(), _configuration); break;

            // Healers
            case JobRegistry.WhiteMage:   ApolloTab.Draw(_debugService.GetApolloDebugState(), _configuration); break;
            case JobRegistry.Scholar:     ScholarTab.Draw(_debugService.GetAthenaDebugState(), _configuration); break;
            case JobRegistry.Astrologian: AstrologianTab.Draw(_debugService.GetAstraeaDebugState(), _configuration); break;
            case JobRegistry.Sage:        AsclepiusTab.Draw(_debugService.GetAsclepiusDebugState(), _configuration); break;

            // Melee DPS
            case JobRegistry.Monk:        KratosTab.Draw(_debugService.GetKratosDebugState(), _configuration); break;
            case JobRegistry.Dragoon:     ZeusTab.Draw(_debugService.GetZeusDebugState(), _configuration); break;
            case JobRegistry.Ninja:       HermesTab.Draw(_debugService.GetHermesDebugState(), _configuration); break;
            case JobRegistry.Samurai:     NikeTab.Draw(_debugService.GetNikeDebugState(), _configuration); break;
            case JobRegistry.Reaper:      ThanatosTab.Draw(_debugService.GetThanatosDebugState(), _configuration); break;
            case JobRegistry.Viper:       EchidnaTab.Draw(_debugService.GetEchidnaDebugState(), _configuration); break;

            // Physical Ranged
            case JobRegistry.Bard:        CalliopeTab.Draw(_debugService.GetCalliopeDebugState(), _configuration); break;
            case JobRegistry.Machinist:   PrometheusTab.Draw(_debugService.GetPrometheusDebugState(), _configuration); break;
            case JobRegistry.Dancer:      TerpsichoreTab.Draw(_debugService.GetTerpsichoreDebugState(), _configuration); break;

            // Casters
            case JobRegistry.BlackMage:   HecateTab.Draw(_debugService.GetHecateDebugState(), _configuration); break;
            case JobRegistry.Summoner:    PersephoneTab.Draw(_debugService.GetPersephoneDebugState(), _configuration); break;
            case JobRegistry.RedMage:     CirceTab.Draw(_debugService.GetCirceDebugState(), _configuration); break;
            case JobRegistry.Pictomancer: IrisTab.Draw(_debugService.GetIrisDebugState(), _configuration); break;

            default:
                ImGui.TextDisabled("No debug info available for this job.");
                break;
        }
    }

    /// <summary>
    /// Maps a raw job ID (which may be a base class) to the canonical advanced job ID
    /// used in JobList. Returns 0 if the ID cannot be mapped to any supported job.
    /// </summary>
    private static uint MapToAdvancedJobId(uint rawJobId) => rawJobId switch
    {
        JobRegistry.Conjurer    => JobRegistry.WhiteMage,
        JobRegistry.Arcanist    => JobRegistry.Scholar,
        JobRegistry.Gladiator   => JobRegistry.Paladin,
        JobRegistry.Marauder    => JobRegistry.Warrior,
        JobRegistry.Pugilist    => JobRegistry.Monk,
        JobRegistry.Lancer      => JobRegistry.Dragoon,
        JobRegistry.Rogue       => JobRegistry.Ninja,
        JobRegistry.Archer      => JobRegistry.Bard,
        JobRegistry.Thaumaturge => JobRegistry.BlackMage,
        _ => rawJobId
    };
}
