using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using Olympus.Data;
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
    PartyCoordination,
    DrawHelper,
    ActionFeed
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

    private static readonly ConfigSection[] GeneralSections  = [ConfigSection.General, ConfigSection.Targeting, ConfigSection.RoleActions, ConfigSection.PartyCoordination, ConfigSection.DrawHelper, ConfigSection.ActionFeed];
    private static readonly ConfigSection[] HealerSections   = [ConfigSection.WhiteMage, ConfigSection.Scholar, ConfigSection.Astrologian, ConfigSection.Sage];
    private static readonly ConfigSection[] TankSections     = [ConfigSection.TankShared, ConfigSection.Paladin, ConfigSection.Warrior, ConfigSection.DarkKnight, ConfigSection.Gunbreaker];
    private static readonly ConfigSection[] MeleeSections    = [ConfigSection.MeleeDpsShared, ConfigSection.Dragoon, ConfigSection.Ninja, ConfigSection.Samurai, ConfigSection.Monk, ConfigSection.Reaper, ConfigSection.Viper];
    private static readonly ConfigSection[] RangedSections   = [ConfigSection.RangedDpsShared, ConfigSection.Machinist, ConfigSection.Bard, ConfigSection.Dancer];
    private static readonly ConfigSection[] CasterSections   = [ConfigSection.CasterShared, ConfigSection.BlackMage, ConfigSection.Summoner, ConfigSection.RedMage, ConfigSection.Pictomancer];

    // Maps sidebar sections to their primary job ID for icon lookup.
    private static readonly Dictionary<ConfigSection, uint> SectionJobIds = new()
    {
        { ConfigSection.WhiteMage,   JobRegistry.WhiteMage },
        { ConfigSection.Scholar,     JobRegistry.Scholar },
        { ConfigSection.Astrologian, JobRegistry.Astrologian },
        { ConfigSection.Sage,        JobRegistry.Sage },
        { ConfigSection.Paladin,     JobRegistry.Paladin },
        { ConfigSection.Warrior,     JobRegistry.Warrior },
        { ConfigSection.DarkKnight,  JobRegistry.DarkKnight },
        { ConfigSection.Gunbreaker,  JobRegistry.Gunbreaker },
        { ConfigSection.Dragoon,     JobRegistry.Dragoon },
        { ConfigSection.Ninja,       JobRegistry.Ninja },
        { ConfigSection.Samurai,     JobRegistry.Samurai },
        { ConfigSection.Monk,        JobRegistry.Monk },
        { ConfigSection.Reaper,      JobRegistry.Reaper },
        { ConfigSection.Viper,       JobRegistry.Viper },
        { ConfigSection.Bard,        JobRegistry.Bard },
        { ConfigSection.Machinist,   JobRegistry.Machinist },
        { ConfigSection.Dancer,      JobRegistry.Dancer },
        { ConfigSection.BlackMage,   JobRegistry.BlackMage },
        { ConfigSection.Summoner,    JobRegistry.Summoner },
        { ConfigSection.RedMage,     JobRegistry.RedMage },
        { ConfigSection.Pictomancer, JobRegistry.Pictomancer },
    };

    private readonly ITextureProvider? _textureProvider;

    public ConfigSidebar(ITextureProvider? textureProvider = null)
    {
        _textureProvider = textureProvider;
    }

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
        if (ShouldShowCategory(GeneralSections, matchingSections, hasSearch))
        {
            DrawCategoryHeader(Loc.T(LocalizedStrings.Sidebar.General, "GENERAL"));
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.GeneralItem, "General"), ConfigSection.General, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Targeting, "Targeting"), ConfigSection.Targeting, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.RoleActions, "Role Actions"), ConfigSection.RoleActions, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.PartyCoordination, "Party Coordination"), ConfigSection.PartyCoordination, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.DrawHelper, "Draw Helper"), ConfigSection.DrawHelper, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T("ui.sidebar.action_feed", "Action Feed"), ConfigSection.ActionFeed, null, matchingSections, hasSearch);
            ImGui.Spacing();
        }

        // HEALERS section
        if (ShouldShowCategory(HealerSections, matchingSections, hasSearch))
        {
            DrawCategoryHeader(Loc.T(LocalizedStrings.Sidebar.Healers, "HEALERS"));
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.WhiteMage, "White Mage"), ConfigSection.WhiteMage, ConfigUIHelpers.WhiteMageColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Scholar, "Scholar"), ConfigSection.Scholar, ConfigUIHelpers.ScholarColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Astrologian, "Astrologian"), ConfigSection.Astrologian, ConfigUIHelpers.AstrologianColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Sage, "Sage"), ConfigSection.Sage, ConfigUIHelpers.SageColor, matchingSections, hasSearch);
            ImGui.Spacing();
        }

        // TANKS section
        if (ShouldShowCategory(TankSections, matchingSections, hasSearch))
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
        if (ShouldShowCategory(MeleeSections, matchingSections, hasSearch))
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
        if (ShouldShowCategory(RangedSections, matchingSections, hasSearch))
        {
            DrawCategoryHeader(Loc.T(LocalizedStrings.Sidebar.RangedDps, "RANGED DPS"));
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Shared, "Shared"), ConfigSection.RangedDpsShared, null, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Machinist, "Machinist"), ConfigSection.Machinist, ConfigUIHelpers.MachinistColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Bard, "Bard"), ConfigSection.Bard, ConfigUIHelpers.BardColor, matchingSections, hasSearch);
            sectionChanged |= DrawNavItemFiltered(Loc.T(LocalizedStrings.Sidebar.Dancer, "Dancer"), ConfigSection.Dancer, ConfigUIHelpers.DancerColor, matchingSections, hasSearch);
            ImGui.Spacing();
        }

        // CASTERS section
        if (ShouldShowCategory(CasterSections, matchingSections, hasSearch))
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

        var isMatch = hasSearch && matchingSections != null;
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

        ImGui.Indent(10);

        // Draw job icon when available
        var hasIcon = false;
        if (_textureProvider != null && SectionJobIds.TryGetValue(section, out var jobId))
        {
            var iconId = JobRegistry.GetJobIconId(jobId);
            if (iconId != 0)
            {
                var wrap = _textureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrEmpty();
                ImGui.Image(wrap.Handle, new Vector2(16, 16));
                ImGui.SameLine(0, 4);
                hasIcon = true;
            }
        }

        var textColor = color ?? new Vector4(1f, 1f, 1f, 1f);
        if (isSelected)
            textColor = new Vector4(1f, 1f, 1f, 1f);
        else if (isSearchMatch)
            textColor = SearchMatchColor;

        ImGui.PushStyleColor(ImGuiCol.Text, textColor);
        ImGui.PushStyleColor(ImGuiCol.Header, SelectedColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderHovered, HoverColor);
        ImGui.PushStyleColor(ImGuiCol.HeaderActive, SelectedColor);

        var selectableWidth = hasIcon ? SidebarWidth - 45 : SidebarWidth - 25;
        var clicked = ImGui.Selectable($"  {label}", isSelected, ImGuiSelectableFlags.None,
            new Vector2(selectableWidth, 0));

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
