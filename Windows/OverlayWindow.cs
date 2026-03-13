using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Config;

namespace Olympus.Windows;

public sealed class OverlayWindow : Window
{
    private readonly Configuration configuration;
    private readonly Action saveConfiguration;

    private static readonly Vector4 EnabledColor = new(0.4f, 0.8f, 0.4f, 1f);
    private static readonly Vector4 DisabledColor = new(0.5f, 0.5f, 0.5f, 1f);

    public OverlayWindow(Configuration configuration, Action saveConfiguration)
        : base(
            "##OlympusOverlay",
            ImGuiWindowFlags.NoTitleBar
            | ImGuiWindowFlags.NoResize
            | ImGuiWindowFlags.NoScrollbar
            | ImGuiWindowFlags.NoScrollWithMouse
            | ImGuiWindowFlags.NoCollapse
            | ImGuiWindowFlags.AlwaysAutoResize
            | ImGuiWindowFlags.NoFocusOnAppearing
            | ImGuiWindowFlags.NoNav)
    {
        this.configuration = configuration;
        this.saveConfiguration = saveConfiguration;

        Position = new Vector2(configuration.Overlay.X, configuration.Overlay.Y);
        PositionCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        ImGui.TextDisabled("Olympus");
        ImGui.Separator();

        DrawToggle("Rotation", configuration.Enabled, v => configuration.Enabled = v);
        DrawToggle("Healing", configuration.EnableHealing, v => configuration.EnableHealing = v);
        DrawToggle("Damage", configuration.EnableDamage, v => configuration.EnableDamage = v);
    }

    private void DrawToggle(string label, bool value, Action<bool> set)
    {
        ImGui.PushStyleColor(ImGuiCol.Text, value ? EnabledColor : DisabledColor);
        if (ImGui.Checkbox(label, ref value))
        {
            set(value);
            saveConfiguration();
        }
        ImGui.PopStyleColor();
    }
}
