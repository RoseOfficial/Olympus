using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config;

/// <summary>
/// Reusable UI helper methods for consistent styling across config sections.
/// </summary>
public static class ConfigUIHelpers
{
    private const float SliderWidth = 200f;
    private const float SmallSliderWidth = 150f;

    /// <summary>
    /// Accent blue used for active/highlighted UI elements.
    /// </summary>
    public static readonly Vector4 AccentBlue = new(0.4f, 0.8f, 1.0f, 1.0f);

    /// <summary>
    /// Highlight color for search matches (yellow-gold).
    /// </summary>
    public static readonly Vector4 SearchHighlightColor = new(1.0f, 0.9f, 0.4f, 1.0f);

    /// <summary>
    /// Current search query for highlighting. Set by ConfigWindow.
    /// </summary>
    public static string? CurrentSearchQuery { get; set; }

    /// <summary>
    /// Texture provider for rendering job icons. Set by ConfigWindow.
    /// </summary>
    public static ITextureProvider? TextureProvider { get; set; }

    // Maps job display names (as passed to JobHeader) to their primary job IDs.
    private static readonly Dictionary<string, uint> JobNameToId = new(StringComparer.OrdinalIgnoreCase)
    {
        { "White Mage",    JobRegistry.WhiteMage },
        { "Scholar",       JobRegistry.Scholar },
        { "Astrologian",   JobRegistry.Astrologian },
        { "Sage",          JobRegistry.Sage },
        { "Paladin",       JobRegistry.Paladin },
        { "Warrior",       JobRegistry.Warrior },
        { "Dark Knight",   JobRegistry.DarkKnight },
        { "Gunbreaker",    JobRegistry.Gunbreaker },
        { "Dragoon",       JobRegistry.Dragoon },
        { "Ninja",         JobRegistry.Ninja },
        { "Samurai",       JobRegistry.Samurai },
        { "Monk",          JobRegistry.Monk },
        { "Reaper",        JobRegistry.Reaper },
        { "Viper",         JobRegistry.Viper },
        { "Bard",          JobRegistry.Bard },
        { "Machinist",     JobRegistry.Machinist },
        { "Dancer",        JobRegistry.Dancer },
        { "Black Mage",    JobRegistry.BlackMage },
        { "Summoner",      JobRegistry.Summoner },
        { "Red Mage",      JobRegistry.RedMage },
        { "Pictomancer",   JobRegistry.Pictomancer },
    };

    #region Job Header Colors

    // Healers
    public static readonly Vector4 WhiteMageColor = new(1.0f, 1.0f, 0.8f, 1.0f);
    public static readonly Vector4 ScholarColor = new(0.8f, 0.9f, 1.0f, 1.0f);
    public static readonly Vector4 AstrologianColor = new(1.0f, 0.9f, 0.6f, 1.0f);
    public static readonly Vector4 SageColor = new(0.6f, 1.0f, 0.8f, 1.0f);

    // Tanks
    public static readonly Vector4 PaladinColor = new(0.9f, 0.9f, 1.0f, 1.0f);
    public static readonly Vector4 WarriorColor = new(1.0f, 0.6f, 0.5f, 1.0f);
    public static readonly Vector4 DarkKnightColor = new(0.7f, 0.5f, 0.8f, 1.0f);
    public static readonly Vector4 GunbreakerColor = new(0.6f, 0.7f, 0.9f, 1.0f);

    // Melee DPS
    public static readonly Vector4 DragoonColor = new(0.4f, 0.5f, 0.9f, 1.0f);
    public static readonly Vector4 NinjaColor = new(0.7f, 0.3f, 0.5f, 1.0f);
    public static readonly Vector4 SamuraiColor = new(0.9f, 0.5f, 0.3f, 1.0f);
    public static readonly Vector4 MonkColor = new(0.8f, 0.7f, 0.3f, 1.0f);
    public static readonly Vector4 ReaperColor = new(0.6f, 0.3f, 0.4f, 1.0f);
    public static readonly Vector4 ViperColor = new(0.4f, 0.7f, 0.4f, 1.0f);

    // Ranged Physical DPS
    public static readonly Vector4 BardColor = new(0.6f, 0.8f, 0.5f, 1.0f);
    public static readonly Vector4 MachinistColor = new(0.5f, 0.8f, 0.9f, 1.0f);
    public static readonly Vector4 DancerColor = new(0.9f, 0.7f, 0.8f, 1.0f);

    // Casters
    public static readonly Vector4 BlackMageColor = new(0.6f, 0.4f, 0.8f, 1.0f);
    public static readonly Vector4 SummonerColor = new(0.3f, 0.7f, 0.5f, 1.0f);
    public static readonly Vector4 RedMageColor = new(0.9f, 0.4f, 0.5f, 1.0f);
    public static readonly Vector4 PictomancerColor = new(0.8f, 0.6f, 0.9f, 1.0f);

    #endregion

    #region Headers

    /// <summary>
    /// Renders a job header with the job name and Greek deity name, with an icon when available.
    /// </summary>
    public static void JobHeader(string jobName, string deityName, Vector4 color)
    {
        if (TextureProvider != null && JobNameToId.TryGetValue(jobName, out var jobId))
        {
            var iconId = JobRegistry.GetJobIconId(jobId);
            if (iconId != 0)
            {
                var wrap = TextureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrEmpty();
                ImGui.Image(wrap.Handle, new Vector2(20, 20));
                ImGui.SameLine();
            }
        }

        var headerText = Loc.TFormat(LocalizedStrings.Helpers.JobHeaderFormat, "{0} ({1}) Settings", deityName, jobName);
        ImGui.TextColored(color, headerText);
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
    /// Binds a boolean config property to a checkbox using getter/setter lambdas.
    /// When actionId is non-zero, renders a 16x16 action icon before the checkbox and shows
    /// a structured game-data tooltip on hover. When actionId is zero, delegates to ToggleCheckbox.
    /// </summary>
    public static void Toggle(string label, Func<bool> get, Action<bool> set, string? tooltip, Action save, uint actionId = 0)
    {
        if (actionId != 0)
        {
            var data = GameDataLocalizer.Instance?.GetActionTooltipData(actionId);
            if (data != null && TextureProvider != null && data.IconId != 0)
            {
                var wrap = TextureProvider.GetFromGameIcon(new GameIconLookup(data.IconId)).GetWrapOrEmpty();
                ImGui.Image(wrap.Handle, new Vector2(16, 16));
                ImGui.SameLine(0, 4);
            }

            var val = get();
            var changed = ImGui.Checkbox(label, ref val);
            if (changed)
            {
                set(val);
                save();
            }

            if (data != null && ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.Text($"{data.Name} (ID: {data.ActionId}) [{(data.IsGcd ? "GCD" : "oGCD")}]");
                ImGui.Text($"Cast: {data.CastTime:F1}s   Recast: {data.RecastTime:F1}s");
                ImGui.Text($"Range: {data.Range}y   AoE: {data.EffectRange}y");
                ImGui.EndTooltip();
            }
        }
        else
        {
            var val = get();
            if (ToggleCheckbox(label, ref val, tooltip, save))
                set(val);
        }
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

    #region Search Highlighting

    /// <summary>
    /// Checks if the given text matches the current search query.
    /// </summary>
    public static bool IsSearchMatch(string? text)
    {
        if (string.IsNullOrEmpty(CurrentSearchQuery) || string.IsNullOrEmpty(text))
            return false;

        return text.Contains(CurrentSearchQuery, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if either the label or description matches the current search query.
    /// </summary>
    public static bool IsSearchMatch(string? label, string? description)
    {
        return IsSearchMatch(label) || IsSearchMatch(description);
    }

    /// <summary>
    /// Begins search highlight styling if the text matches.
    /// Returns true if highlighting was applied (caller must call EndSearchHighlight).
    /// </summary>
    public static bool BeginSearchHighlight(string? text)
    {
        if (!IsSearchMatch(text))
            return false;

        ImGui.PushStyleColor(ImGuiCol.Text, SearchHighlightColor);
        return true;
    }

    /// <summary>
    /// Begins search highlight styling if either label or description matches.
    /// Returns true if highlighting was applied (caller must call EndSearchHighlight).
    /// </summary>
    public static bool BeginSearchHighlight(string? label, string? description)
    {
        if (!IsSearchMatch(label, description))
            return false;

        ImGui.PushStyleColor(ImGuiCol.Text, SearchHighlightColor);
        return true;
    }

    /// <summary>
    /// Ends search highlight styling.
    /// </summary>
    public static void EndSearchHighlight()
    {
        ImGui.PopStyleColor();
    }

    /// <summary>
    /// Renders text with search highlighting if it matches.
    /// </summary>
    public static void HighlightedText(string text)
    {
        var highlighted = BeginSearchHighlight(text);
        ImGui.Text(text);
        if (highlighted)
            EndSearchHighlight();
    }

    /// <summary>
    /// Renders disabled text with search highlighting if it matches.
    /// </summary>
    public static void HighlightedTextDisabled(string text)
    {
        if (IsSearchMatch(text))
        {
            // Use highlight color instead of disabled
            ImGui.TextColored(SearchHighlightColor, text);
        }
        else
        {
            ImGui.TextDisabled(text);
        }
    }

    /// <summary>
    /// Renders a checkbox with search highlighting support.
    /// When actionId is non-zero, renders a 16x16 action icon before the checkbox and shows
    /// a structured game-data tooltip on hover.
    /// </summary>
    public static bool HighlightedCheckbox(string label, ref bool value, string? description, Action save, uint actionId = 0)
    {
        var highlighted = BeginSearchHighlight(label, description);

        ActionTooltipData? data = null;
        if (actionId != 0)
        {
            data = GameDataLocalizer.Instance?.GetActionTooltipData(actionId);
            if (data != null && TextureProvider != null && data.IconId != 0)
            {
                var wrap = TextureProvider.GetFromGameIcon(new GameIconLookup(data.IconId)).GetWrapOrEmpty();
                ImGui.Image(wrap.Handle, new Vector2(16, 16));
                ImGui.SameLine(0, 4);
            }
        }

        var changed = ImGui.Checkbox(label, ref value);
        if (changed)
            save();

        if (highlighted)
            EndSearchHighlight();

        if (data != null && ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text($"{data.Name} (ID: {data.ActionId}) [{(data.IsGcd ? "GCD" : "oGCD")}]");
            ImGui.Text($"Cast: {data.CastTime:F1}s   Recast: {data.RecastTime:F1}s");
            ImGui.Text($"Range: {data.Range}y   AoE: {data.EffectRange}y");
            ImGui.EndTooltip();
        }

        if (!string.IsNullOrEmpty(description))
            HighlightedTextDisabled(description);

        return changed;
    }

    /// <summary>
    /// Renders a slider with search highlighting support.
    /// </summary>
    public static float HighlightedThresholdSlider(string label, float value, float min, float max, string? description, Action save)
    {
        var highlighted = BeginSearchHighlight(label, description);

        var displayValue = value * 100f;
        ImGui.SetNextItemWidth(SliderWidth);
        if (ImGui.SliderFloat(label, ref displayValue, min, max, "%.0f%%"))
        {
            save();
            if (highlighted)
                EndSearchHighlight();
            if (!string.IsNullOrEmpty(description))
                HighlightedTextDisabled(description);
            return displayValue / 100f;
        }

        if (highlighted)
            EndSearchHighlight();

        if (!string.IsNullOrEmpty(description))
            HighlightedTextDisabled(description);

        return value;
    }

    /// <summary>
    /// Renders an int slider with search highlighting support.
    /// </summary>
    public static int HighlightedIntSlider(string label, int value, int min, int max, string? description, Action save)
    {
        var highlighted = BeginSearchHighlight(label, description);

        var localValue = value;
        ImGui.SetNextItemWidth(SliderWidth);
        if (ImGui.SliderInt(label, ref localValue, min, max))
        {
            save();
            if (highlighted)
                EndSearchHighlight();
            if (!string.IsNullOrEmpty(description))
                HighlightedTextDisabled(description);
            return localValue;
        }

        if (highlighted)
            EndSearchHighlight();

        if (!string.IsNullOrEmpty(description))
            HighlightedTextDisabled(description);

        return value;
    }

    #endregion
}
