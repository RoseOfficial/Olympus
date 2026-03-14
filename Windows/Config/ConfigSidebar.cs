using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;

namespace Olympus.Windows.Config;

/// <summary>
/// Navigation sections available in the config sidebar.
/// </summary>
public enum ConfigSection
{
    // General
    General,
    Targeting,
    RoleActions,

    // Healers
    WhiteMage,
    Scholar,
    Astrologian,
    Sage,

    // Tanks
    TankShared,
    Paladin,
    Warrior,
    DarkKnight,
    Gunbreaker,

    // Melee DPS
    MeleeDpsShared,
    Dragoon,
    Ninja,
    Samurai,
    Monk,
    Reaper,
    Viper,

    // Ranged Physical DPS
    RangedDpsShared,
    Machinist,
    Bard,
    Dancer,

    // Casters
    CasterShared,
    BlackMage,
    Summoner,
    RedMage,
    Pictomancer,

    // Utility
    DrawHelper
}

/// <summary>
/// Renders the sidebar navigation for the config window.
/// </summary>
public sealed class ConfigSidebar
{
    private const float SidebarWidth = 150f;
    private static readonly Vector4 HeaderColor = new(0.7f, 0.7f, 0.7f, 1.0f);
    private static readonly Vector4 SelectedColor = new(0.3f, 0.5f, 0.8f, 1.0f);
    private static readonly Vector4 HoverColor = new(0.25f, 0.4f, 0.6f, 1.0f);
    private static readonly Vector4 SearchMatchColor = new(1.0f, 0.9f, 0.4f, 1.0f);

    public ConfigSection CurrentSection { get; private set; } = ConfigSection.General;

    /// <summary>
    /// Renders the sidebar and returns true if the section changed.
    /// </summary>
    public bool Draw()
    {
        return Draw(null, null);
    }

    /// <summary>
    /// Renders the sidebar with search filtering and returns true if the section changed.
    /// </summary>
    public bool Draw(string? searchQuery, HashSet<ConfigSection>? matchingSections)
    {
        var sectionChanged = false;
        var hasSearch = !string.IsNullOrWhiteSpace(searchQuery) && matchingSections != null;

        ImGui.BeginChild("##ConfigSidebar", new Vector2(SidebarWidth, 0), true);

        // GENERAL section
        var generalSections = new[] { ConfigSection.General, ConfigSection.Targeting, ConfigSection.RoleActions, ConfigSection.DrawHelper };
        if (ShouldShowCategory(generalSections, matchingSections, hasSearch))
        {
            DrawCategoryHeader(Loc.T(LocalizedStrings.Sidebar.General, "GENERAL"));
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.GeneralItem, "General"), ConfigSection.General, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Targeting, "Targeting"), ConfigSection.Targeting, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.RoleActions, "Role Actions"), ConfigSection.RoleActions, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered("Draw Helper", ConfigSection.DrawHelper, null, matchingSections, hasSearch);
            ImGui.Spacing();
        }

        // HEALERS section
        var healerSections = new[] { ConfigSection.WhiteMage, ConfigSection.Scholar, ConfigSection.Astrologian, ConfigSection.Sage };
        if (ShouldShowCategory(healerSections, matchingSections, hasSearch))
        {
            DrawCategoryHeader(Loc.T(LocalizedStrings.Sidebar.Healers, "HEALERS"));
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.WhiteMage, "White Mage"), ConfigSection.WhiteMage, ConfigUIHelpers.WhiteMageColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Scholar, "Scholar"), ConfigSection.Scholar, ConfigUIHelpers.ScholarColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Astrologian, "Astrologian"), ConfigSection.Astrologian, ConfigUIHelpers.AstrologianColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Sage, "Sage"), ConfigSection.Sage, ConfigUIHelpers.SageColor, matchingSections, hasSearch);
            ImGui.Spacing();
        }

        // TANKS section
        var tankSections = new[] { ConfigSection.TankShared, ConfigSection.Paladin, ConfigSection.Warrior, ConfigSection.DarkKnight, ConfigSection.Gunbreaker };
        if (ShouldShowCategory(tankSections, matchingSections, hasSearch))
        {
            DrawCategoryHeader(Loc.T(LocalizedStrings.Sidebar.Tanks, "TANKS"));
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Shared, "Shared"), ConfigSection.TankShared, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Paladin, "Paladin"), ConfigSection.Paladin, ConfigUIHelpers.PaladinColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Warrior, "Warrior"), ConfigSection.Warrior, ConfigUIHelpers.WarriorColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.DarkKnight, "Dark Knight"), ConfigSection.DarkKnight, ConfigUIHelpers.DarkKnightColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Gunbreaker, "Gunbreaker"), ConfigSection.Gunbreaker, ConfigUIHelpers.GunbreakerColor, matchingSections, hasSearch);
            ImGui.Spacing();
        }

        // MELEE DPS section
        var meleeSections = new[] { ConfigSection.MeleeDpsShared, ConfigSection.Dragoon, ConfigSection.Ninja, ConfigSection.Samurai, ConfigSection.Monk, ConfigSection.Reaper, ConfigSection.Viper };
        if (ShouldShowCategory(meleeSections, matchingSections, hasSearch))
        {
            DrawCategoryHeader(Loc.T(LocalizedStrings.Sidebar.MeleeDps, "MELEE DPS"));
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Shared, "Shared"), ConfigSection.MeleeDpsShared, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Dragoon, "Dragoon"), ConfigSection.Dragoon, ConfigUIHelpers.DragoonColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Ninja, "Ninja"), ConfigSection.Ninja, ConfigUIHelpers.NinjaColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Samurai, "Samurai"), ConfigSection.Samurai, ConfigUIHelpers.SamuraiColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Monk, "Monk"), ConfigSection.Monk, ConfigUIHelpers.MonkColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Reaper, "Reaper"), ConfigSection.Reaper, ConfigUIHelpers.ReaperColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Viper, "Viper"), ConfigSection.Viper, ConfigUIHelpers.ViperColor, matchingSections, hasSearch);
            ImGui.Spacing();
        }

        // RANGED DPS section
        var rangedSections = new[] { ConfigSection.RangedDpsShared, ConfigSection.Machinist, ConfigSection.Bard, ConfigSection.Dancer };
        if (ShouldShowCategory(rangedSections, matchingSections, hasSearch))
        {
            DrawCategoryHeader(Loc.T(LocalizedStrings.Sidebar.RangedDps, "RANGED DPS"));
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Shared, "Shared"), ConfigSection.RangedDpsShared, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Machinist, "Machinist"), ConfigSection.Machinist, ConfigUIHelpers.MachinistColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Bard, "Bard"), ConfigSection.Bard, ConfigUIHelpers.BardColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Dancer, "Dancer"), ConfigSection.Dancer, ConfigUIHelpers.DancerColor, matchingSections, hasSearch);
            ImGui.Spacing();
        }

        // CASTERS section
        var casterSections = new[] { ConfigSection.CasterShared, ConfigSection.BlackMage, ConfigSection.Summoner, ConfigSection.RedMage, ConfigSection.Pictomancer };
        if (ShouldShowCategory(casterSections, matchingSections, hasSearch))
        {
            DrawCategoryHeader(Loc.T(LocalizedStrings.Sidebar.Casters, "CASTERS"));
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Shared, "Shared"), ConfigSection.CasterShared, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.BlackMage, "Black Mage"), ConfigSection.BlackMage, ConfigUIHelpers.BlackMageColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Summoner, "Summoner"), ConfigSection.Summoner, ConfigUIHelpers.SummonerColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.RedMage, "Red Mage"), ConfigSection.RedMage, ConfigUIHelpers.RedMageColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Pictomancer, "Pictomancer"), ConfigSection.Pictomancer, ConfigUIHelpers.PictomancerColor, matchingSections, hasSearch);
        }

        ImGui.EndChild();

        return sectionChanged;
    }

    private static bool ShouldShowCategory(ConfigSection[] sections, HashSet<ConfigSection>? matchingSections, bool hasSearch)
    {
        if (!hasSearch || matchingSections == null)
            return true;

        foreach (var section in sections)
        {
            if (matchingSections.Contains(section))
                return true;
        }
        return false;
    }

    private bool DrawNavItemFiltered(string label, ConfigSection section, Vector4? color, HashSet<ConfigSection>? matchingSections, bool hasSearch)
    {
        // Filter out non-matching sections when searching
        if (hasSearch && matchingSections != null && !matchingSections.Contains(section))
            return false;

        var isMatch = hasSearch && matchingSections != null && matchingSections.Contains(section);
        return DrawNavItem(label, section, color, isMatch);
    }

    private static void DrawCategoryHeader(string label)
    {
        ImGui.TextColored(HeaderColor, label);
    }

    private bool DrawNavItem(string label, ConfigSection section, Vector4? color = null, bool isSearchMatch = false)
    {
        var isSelected = CurrentSection == section;

        // Draw selection highlight
        if (isSelected)
        {
            var cursorPos = ImGui.GetCursorScreenPos();
            var regionAvail = ImGui.GetContentRegionAvail();
            var drawList = ImGui.GetWindowDrawList();
            drawList.AddRectFilled(
                cursorPos,
                new Vector2(cursorPos.X + regionAvail.X, cursorPos.Y + ImGui.GetTextLineHeightWithSpacing()),
                ImGui.GetColorU32(SelectedColor));
        }

        // Draw the selectable item
        ImGui.Indent(10);

        var textColor = color ?? new Vector4(1f, 1f, 1f, 1f);
        if (isSelected)
            textColor = new Vector4(1f, 1f, 1f, 1f); // Always white when selected
        else if (isSearchMatch)
            textColor = SearchMatchColor; // Yellow highlight for search matches

        ImGui.PushStyleColor(ImGuiCol.Text, textColor);
        ImGui.PushStyleColor(ImGuiCol.Header, SelectedColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, HoverColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, SelectedColor);

        var clicked = ImGui.Selectable($"  {label}", isSelected, ImGuiSelectableFlags.None,
            new Vector2(SidebarWidth - 25, 0));

        ImGui.PopStyleColor(4);
        ImGui.Unindent(10);

        if (clicked && !isSelected)
        {
            CurrentSection = section;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Sets the current section programmatically.
    /// </summary>
    public void SetSection(ConfigSection section)
    {
        CurrentSection = section;
    }
}
