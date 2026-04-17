using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;

namespace Olympus.Windows.Config.Shared;

/// <summary>
/// Controls which collapsible sections are visible within the Debug window's tabs.
/// Writes to Configuration.Debug.DebugSectionVisibility.
/// </summary>
public sealed class DebugDisplaySection
{
    private readonly Configuration config;
    private readonly Action save;

    public DebugDisplaySection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SectionVisibilityDesc,
            "Choose which sections are visible within each Debug window tab."));
        ImGui.Spacing();

        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Debug.OverviewTabLabel, "Overview Tab")))
        {
            ConfigUIHelpers.BeginIndent();
            DrawToggle("GcdPlanning", Loc.T(LocalizedStrings.Debug.GcdPlanning, "GCD Planning"));
            DrawToggle("QuickStats", Loc.T(LocalizedStrings.Debug.QuickStats, "Quick Stats"));
            ConfigUIHelpers.EndIndent();
        }

        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Debug.WhyStuckTabLabel, "Why Stuck? Tab")))
        {
            ConfigUIHelpers.BeginIndent();
            DrawToggle("GcdPriority", Loc.T(LocalizedStrings.Debug.GcdPriorityChain, "GCD Priority Chain"));
            DrawToggle("OgcdState", Loc.T(LocalizedStrings.Debug.OgcdState, "oGCD State"));
            DrawToggle("DpsDetails", Loc.T(LocalizedStrings.Debug.DpsDetails, "DPS Details"));
            DrawToggle("Resources", Loc.T(LocalizedStrings.Debug.ResourcesSection, "Resources"));
            ConfigUIHelpers.EndIndent();
        }

        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Debug.HealingTabLabel, "Healing Tab")))
        {
            ConfigUIHelpers.BeginIndent();
            DrawToggle("SpellStatus", Loc.T(LocalizedStrings.Debug.SpellStatus, "Spell Status"));
            DrawToggle("SpellSelection", Loc.T(LocalizedStrings.Debug.SpellSelection, "Spell Selection"));
            DrawToggle("HpPrediction", Loc.T(LocalizedStrings.Debug.HpPrediction, "HP Prediction"));
            DrawToggle("AoEHealing", Loc.T(LocalizedStrings.Debug.AoEHealing, "AoE Healing"));
            DrawToggle("RecentHeals", Loc.T(LocalizedStrings.Debug.RecentHeals, "Recent Heals"));
            DrawToggle("ShadowHp", Loc.T(LocalizedStrings.Debug.ShadowHp, "Shadow HP"));
            ConfigUIHelpers.EndIndent();
        }

        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Debug.OverhealTabLabel, "Overheal Tab")))
        {
            ConfigUIHelpers.BeginIndent();
            DrawToggle("OverhealSummary", Loc.T(LocalizedStrings.Debug.OverhealSummary, "Summary"));
            DrawToggle("OverhealBySpell", Loc.T(LocalizedStrings.Debug.OverhealBySpell, "By Spell"));
            DrawToggle("OverhealByTarget", Loc.T(LocalizedStrings.Debug.OverhealByTarget, "By Target"));
            DrawToggle("OverhealTimeline", Loc.T(LocalizedStrings.Debug.OverhealTimeline, "Timeline"));
            DrawToggle("OverhealControls", Loc.T(LocalizedStrings.Debug.OverhealControls, "Controls"));
            ConfigUIHelpers.EndIndent();
        }

        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Debug.ActionsTabLabel, "Actions Tab")))
        {
            ConfigUIHelpers.BeginIndent();
            DrawToggle("GcdDetails", Loc.T(LocalizedStrings.Debug.GcdDetails, "GCD Details"));
            DrawToggle("SpellUsage", Loc.T(LocalizedStrings.Debug.SpellUsage, "Spell Usage"));
            DrawToggle("ActionHistory", Loc.T(LocalizedStrings.Debug.ActionHistory, "Action History"));
            ConfigUIHelpers.EndIndent();
        }

        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Debug.PerformanceTabLabel, "Performance Tab")))
        {
            ConfigUIHelpers.BeginIndent();
            DrawToggle("Statistics", Loc.T(LocalizedStrings.Debug.Statistics, "Statistics"));
            DrawToggle("Downtime", Loc.T(LocalizedStrings.Debug.Downtime, "Downtime"));
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawToggle(string key, string label)
    {
        if (!config.Debug.DebugSectionVisibility.TryGetValue(key, out var visible))
            visible = true;

        if (ImGui.Checkbox(label, ref visible))
        {
            config.Debug.DebugSectionVisibility[key] = visible;
            save();
        }
    }
}
