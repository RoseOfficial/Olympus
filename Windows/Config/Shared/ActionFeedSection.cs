using System;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;

namespace Olympus.Windows.Config.Shared;

/// <summary>
/// Config section for the Action Feed overlay — the visual strip of icons showing
/// what Olympus has just pressed.
/// </summary>
public sealed class ActionFeedSection
{
    private readonly Configuration config;
    private readonly Action save;

    public ActionFeedSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        var af = config.ActionFeed;

        ImGui.Separator();
        ImGui.Text(Loc.T("ui.config.action_feed.title", "Action Feed"));
        ImGui.TextDisabled(Loc.T("ui.config.action_feed.description",
            "A visual strip showing what Olympus has just pressed. Drag the overlay in-game to reposition."));

        ImGui.Spacing();

        var visible = af.IsVisible;
        if (ImGui.Checkbox(Loc.T("ui.config.action_feed.show", "Show action feed"), ref visible))
        {
            af.IsVisible = visible;
            save();
        }

        if (!af.IsVisible)
        {
            ImGui.TextDisabled(Loc.T("ui.config.action_feed.hidden_hint",
                "Action feed is hidden. Enable to configure appearance."));
            return;
        }

        ImGui.Spacing();

        var showLabels = af.ShowLabels;
        if (ImGui.Checkbox(Loc.T("ui.config.action_feed.show_labels", "Show action names next to icons"), ref showLabels))
        {
            af.ShowLabels = showLabels;
            save();
        }
        if (ImGui.IsItemHovered())
            ImGui.SetTooltip(Loc.T("ui.config.action_feed.show_labels_tooltip",
                "Off by default — names always appear on hover."));

        ImGui.Spacing();

        var iconSize = af.IconSize;
        if (ImGui.SliderFloat(Loc.T("ui.config.action_feed.icon_size", "Icon size"), ref iconSize, 16f, 96f, "%.0fpx"))
        {
            af.IconSize = iconSize;
            save();
        }

        var maxIcons = af.MaxIcons;
        if (ImGui.SliderInt(Loc.T("ui.config.action_feed.max_icons", "Max icons shown"), ref maxIcons, 1, 12))
        {
            af.MaxIcons = maxIcons;
            save();
        }

        var duration = af.DurationSeconds;
        if (ImGui.SliderFloat(Loc.T("ui.config.action_feed.duration", "Fade duration"), ref duration, 0.5f, 10f, "%.1fs"))
        {
            af.DurationSeconds = duration;
            save();
        }

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Text(Loc.T("ui.config.action_feed.position_header", "Position"));
        ImGui.TextDisabled($"X: {af.X:F0}  Y: {af.Y:F0}");
        if (ImGui.Button(Loc.T("ui.config.action_feed.reset_position", "Reset Position")))
        {
            af.X = 400f;
            af.Y = 200f;
            save();
        }
    }
}
