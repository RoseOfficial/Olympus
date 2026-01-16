using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;

namespace Olympus.Windows.Config;

/// <summary>
/// Reusable UI helper methods for consistent styling across config sections.
/// </summary>
public static class ConfigUIHelpers
{
    private const float SliderWidth = 200f;
    private const float SmallSliderWidth = 150f;

    #region Job Header Colors

    public static readonly Vector4 WhiteMageColor = new(1.0f, 1.0f, 0.8f, 1.0f);
    public static readonly Vector4 ScholarColor = new(0.8f, 0.9f, 1.0f, 1.0f);
    public static readonly Vector4 AstrologianColor = new(1.0f, 0.9f, 0.6f, 1.0f);
    public static readonly Vector4 SageColor = new(0.6f, 1.0f, 0.8f, 1.0f);
    public static readonly Vector4 PaladinColor = new(0.9f, 0.9f, 1.0f, 1.0f);
    public static readonly Vector4 WarriorColor = new(1.0f, 0.6f, 0.5f, 1.0f);
    public static readonly Vector4 DarkKnightColor = new(0.7f, 0.5f, 0.8f, 1.0f);
    public static readonly Vector4 GunbreakerColor = new(0.6f, 0.7f, 0.9f, 1.0f);

    #endregion

    #region Headers

    /// <summary>
    /// Renders a job header with the job name and Greek deity name.
    /// </summary>
    public static void JobHeader(string jobName, string deityName, Vector4 color)
    {
        ImGui.TextColored(color, $"{deityName} ({jobName}) Settings");
        ImGui.Spacing();
    }

    /// <summary>
    /// Renders a collapsible section header.
    /// </summary>
    public static bool SectionHeader(string label, bool defaultOpen = true)
    {
        var flags = defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
        return ImGui.CollapsingHeader(label, flags);
    }

    /// <summary>
    /// Renders a collapsible section header with a unique ID suffix.
    /// </summary>
    public static bool SectionHeader(string label, string idSuffix, bool defaultOpen = true)
    {
        var flags = defaultOpen ? ImGuiTreeNodeFlags.DefaultOpen : ImGuiTreeNodeFlags.None;
        return ImGui.CollapsingHeader($"{label}##{idSuffix}", flags);
    }

    /// <summary>
    /// Renders a section label (non-collapsible).
    /// </summary>
    public static void SectionLabel(string label)
    {
        ImGui.TextDisabled(label);
    }

    #endregion

    #region Checkboxes

    /// <summary>
    /// Renders a checkbox with an optional description tooltip.
    /// </summary>
    public static bool ToggleCheckbox(string label, ref bool value, string? description, Action save)
    {
        var changed = ImGui.Checkbox(label, ref value);
        if (changed)
            save();

        if (!string.IsNullOrEmpty(description))
            ImGui.TextDisabled(description);

        return changed;
    }

    /// <summary>
    /// Renders a checkbox on the same line as the previous element.
    /// </summary>
    public static bool ToggleCheckboxSameLine(string label, ref bool value, Action save)
    {
        ImGui.SameLine();
        var changed = ImGui.Checkbox(label, ref value);
        if (changed)
            save();
        return changed;
    }

    /// <summary>
    /// Renders a disabled checkbox group.
    /// </summary>
    public static void BeginDisabledGroup(bool disabled)
    {
        ImGui.BeginDisabled(disabled);
    }

    public static void EndDisabledGroup()
    {
        ImGui.EndDisabled();
    }

    #endregion

    #region Sliders

    /// <summary>
    /// Renders a percentage threshold slider (0-100%).
    /// Returns the new value if changed, or the original value if unchanged.
    /// </summary>
    public static float ThresholdSlider(string label, float value, float min, float max, string? description, Action save)
    {
        var displayValue = value * 100f;
        ImGui.SetNextItemWidth(SliderWidth);
        if (ImGui.SliderFloat(label, ref displayValue, min, max, "%.0f%%"))
        {
            save();
            if (!string.IsNullOrEmpty(description))
                ImGui.TextDisabled(description);
            return displayValue / 100f;
        }

        if (!string.IsNullOrEmpty(description))
            ImGui.TextDisabled(description);

        return value;
    }

    /// <summary>
    /// Renders a small percentage threshold slider.
    /// Returns the new value if changed, or the original value if unchanged.
    /// </summary>
    public static float ThresholdSliderSmall(string label, float value, float min, float max, string? description, Action save)
    {
        var displayValue = value * 100f;
        ImGui.SetNextItemWidth(SmallSliderWidth);
        if (ImGui.SliderFloat(label, ref displayValue, min, max, "%.0f%%"))
        {
            save();
            if (!string.IsNullOrEmpty(description))
                ImGui.TextDisabled(description);
            return displayValue / 100f;
        }

        if (!string.IsNullOrEmpty(description))
            ImGui.TextDisabled(description);

        return value;
    }

    /// <summary>
    /// Renders an integer slider.
    /// Returns the new value if changed, or the original value if unchanged.
    /// </summary>
    public static int IntSlider(string label, int value, int min, int max, string? description, Action save)
    {
        var localValue = value;
        ImGui.SetNextItemWidth(SliderWidth);
        if (ImGui.SliderInt(label, ref localValue, min, max))
        {
            save();
            if (!string.IsNullOrEmpty(description))
                ImGui.TextDisabled(description);
            return localValue;
        }

        if (!string.IsNullOrEmpty(description))
            ImGui.TextDisabled(description);

        return value;
    }

    /// <summary>
    /// Renders a small integer slider.
    /// Returns the new value if changed, or the original value if unchanged.
    /// </summary>
    public static int IntSliderSmall(string label, int value, int min, int max, string? description, Action save)
    {
        var localValue = value;
        ImGui.SetNextItemWidth(SmallSliderWidth);
        if (ImGui.SliderInt(label, ref localValue, min, max))
        {
            save();
            if (!string.IsNullOrEmpty(description))
                ImGui.TextDisabled(description);
            return localValue;
        }

        if (!string.IsNullOrEmpty(description))
            ImGui.TextDisabled(description);

        return value;
    }

    /// <summary>
    /// Renders a float slider with custom format.
    /// Returns the new value if changed, or the original value if unchanged.
    /// </summary>
    public static float FloatSlider(string label, float value, float min, float max, string format, string? description, Action save)
    {
        var localValue = value;
        ImGui.SetNextItemWidth(SliderWidth);
        if (ImGui.SliderFloat(label, ref localValue, min, max, format))
        {
            save();
            if (!string.IsNullOrEmpty(description))
                ImGui.TextDisabled(description);
            return localValue;
        }

        if (!string.IsNullOrEmpty(description))
            ImGui.TextDisabled(description);

        return value;
    }

    #endregion

    #region Combos

    /// <summary>
    /// Renders an enum dropdown combo.
    /// </summary>
    public static bool EnumCombo<T>(string label, ref T value, string? description, Action save) where T : struct, Enum
    {
        var names = Enum.GetNames<T>();
        var currentIndex = Array.IndexOf(Enum.GetValues<T>(), value);
        ImGui.SetNextItemWidth(SmallSliderWidth);
        var changed = ImGui.Combo(label, ref currentIndex, names, names.Length);
        if (changed)
        {
            value = Enum.GetValues<T>()[currentIndex];
            save();
        }

        if (!string.IsNullOrEmpty(description))
            ImGui.TextDisabled(description);

        return changed;
    }

    /// <summary>
    /// Renders a string array dropdown combo.
    /// </summary>
    public static bool StringCombo(string label, ref int index, string[] options, string? description, Action save)
    {
        ImGui.SetNextItemWidth(SliderWidth);
        var changed = ImGui.Combo(label, ref index, options, options.Length);
        if (changed)
            save();

        if (!string.IsNullOrEmpty(description))
            ImGui.TextDisabled(description);

        return changed;
    }

    #endregion

    #region Layout Helpers

    /// <summary>
    /// Adds standard spacing.
    /// </summary>
    public static void Spacing()
    {
        ImGui.Spacing();
    }

    /// <summary>
    /// Adds a separator line.
    /// </summary>
    public static void Separator()
    {
        ImGui.Separator();
    }

    /// <summary>
    /// Begins an indented section.
    /// </summary>
    public static void BeginIndent()
    {
        ImGui.Indent();
    }

    /// <summary>
    /// Ends an indented section.
    /// </summary>
    public static void EndIndent()
    {
        ImGui.Unindent();
    }

    /// <summary>
    /// Renders a warning text in orange.
    /// </summary>
    public static void WarningText(string text)
    {
        ImGui.TextColored(new Vector4(1f, 0.7f, 0.3f, 1f), text);
    }

    /// <summary>
    /// Renders a danger warning text in red-orange.
    /// </summary>
    public static void DangerText(string text)
    {
        ImGui.TextColored(new Vector4(1f, 0.5f, 0f, 1f), text);
    }

    /// <summary>
    /// Renders an info tooltip (?) with hover text.
    /// </summary>
    public static void InfoTooltip(string tooltipText)
    {
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(tooltipText);
            ImGui.EndTooltip();
        }
    }

    #endregion

    #region Tree Nodes

    /// <summary>
    /// Begins an advanced/nested tree node section.
    /// </summary>
    public static bool BeginTreeNode(string label)
    {
        return ImGui.TreeNode(label);
    }

    /// <summary>
    /// Ends a tree node section.
    /// </summary>
    public static void EndTreeNode()
    {
        ImGui.TreePop();
    }

    #endregion
}
