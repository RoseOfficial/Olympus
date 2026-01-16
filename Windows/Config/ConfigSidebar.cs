using System.Numerics;
using Dalamud.Bindings.ImGui;

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
        DrawCategoryHeader("GENERAL");
        sectionChanged |= DrawNavItem("General", ConfigSection.General);
        sectionChanged |= DrawNavItem("Targeting", ConfigSection.Targeting);
        sectionChanged |= DrawNavItem("Role Actions", ConfigSection.RoleActions);

        ImGui.Spacing();

        // HEALERS section
        DrawCategoryHeader("HEALERS");
        sectionChanged |= DrawNavItem("White Mage", ConfigSection.WhiteMage, ConfigUIHelpers.WhiteMageColor);
        sectionChanged |= DrawNavItem("Scholar", ConfigSection.Scholar, ConfigUIHelpers.ScholarColor);
        sectionChanged |= DrawNavItem("Astrologian", ConfigSection.Astrologian, ConfigUIHelpers.AstrologianColor);
        sectionChanged |= DrawNavItem("Sage", ConfigSection.Sage, ConfigUIHelpers.SageColor);

        ImGui.Spacing();

        // TANKS section
        DrawCategoryHeader("TANKS");
        sectionChanged |= DrawNavItem("Shared", ConfigSection.TankShared);
        sectionChanged |= DrawNavItem("Paladin", ConfigSection.Paladin, ConfigUIHelpers.PaladinColor);
        sectionChanged |= DrawNavItem("Warrior", ConfigSection.Warrior, ConfigUIHelpers.WarriorColor);
        sectionChanged |= DrawNavItem("Dark Knight", ConfigSection.DarkKnight, ConfigUIHelpers.DarkKnightColor);
        sectionChanged |= DrawNavItem("Gunbreaker", ConfigSection.Gunbreaker, ConfigUIHelpers.GunbreakerColor);

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
