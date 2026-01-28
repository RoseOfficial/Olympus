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
    Gunbreaker
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

    public ConfigSection CurrentSection { get; private set; } = ConfigSection.General;

    /// <summary>
    /// Renders the sidebar and returns true if the section changed.
    /// </summary>
    public bool Draw()
    {
        var sectionChanged = false;

        ImGui.BeginChild("##ConfigSidebar", new Vector2(SidebarWidth, 0), true);

        // GENERAL section
        DrawCategoryHeader(Loc.T(LocalizedStrings.Sidebar.General, "GENERAL"));
        sectionChanged |= DrawNavItem(Loc.T(LocalizedStrings.Sidebar.GeneralItem, "General"), ConfigSection.General);
        sectionChanged |= DrawNavItem(Loc.T(LocalizedStrings.Sidebar.Targeting, "Targeting"), ConfigSection.Targeting);
        sectionChanged |= DrawNavItem(Loc.T(LocalizedStrings.Sidebar.RoleActions, "Role Actions"), ConfigSection.RoleActions);

        ImGui.Spacing();

        // HEALERS section
        DrawCategoryHeader(Loc.T(LocalizedStrings.Sidebar.Healers, "HEALERS"));
        sectionChanged |= DrawNavItem(Loc.T(LocalizedStrings.Sidebar.WhiteMage, "White Mage"), ConfigSection.WhiteMage, ConfigUIHelpers.WhiteMageColor);
        sectionChanged |= DrawNavItem(Loc.T(LocalizedStrings.Sidebar.Scholar, "Scholar"), ConfigSection.Scholar, ConfigUIHelpers.ScholarColor);
        sectionChanged |= DrawNavItem(Loc.T(LocalizedStrings.Sidebar.Astrologian, "Astrologian"), ConfigSection.Astrologian, ConfigUIHelpers.AstrologianColor);
        sectionChanged |= DrawNavItem(Loc.T(LocalizedStrings.Sidebar.Sage, "Sage"), ConfigSection.Sage, ConfigUIHelpers.SageColor);

        ImGui.Spacing();

        // TANKS section
        DrawCategoryHeader(Loc.T(LocalizedStrings.Sidebar.Tanks, "TANKS"));
        sectionChanged |= DrawNavItem(Loc.T(LocalizedStrings.Sidebar.Shared, "Shared"), ConfigSection.TankShared);
        sectionChanged |= DrawNavItem(Loc.T(LocalizedStrings.Sidebar.Paladin, "Paladin"), ConfigSection.Paladin, ConfigUIHelpers.PaladinColor);
        sectionChanged |= DrawNavItem(Loc.T(LocalizedStrings.Sidebar.Warrior, "Warrior"), ConfigSection.Warrior, ConfigUIHelpers.WarriorColor);
        sectionChanged |= DrawNavItem(Loc.T(LocalizedStrings.Sidebar.DarkKnight, "Dark Knight"), ConfigSection.DarkKnight, ConfigUIHelpers.DarkKnightColor);
        sectionChanged |= DrawNavItem(Loc.T(LocalizedStrings.Sidebar.Gunbreaker, "Gunbreaker"), ConfigSection.Gunbreaker, ConfigUIHelpers.GunbreakerColor);

        ImGui.EndChild();

        return sectionChanged;
    }

    private static void DrawCategoryHeader(string label)
    {
        ImGui.TextColored(HeaderColor, label);
    }

    private bool DrawNavItem(string label, ConfigSection section, Vector4? color = null)
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
