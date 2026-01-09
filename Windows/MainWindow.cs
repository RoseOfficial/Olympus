using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;

namespace Olympus.Windows;

public sealed class MainWindow : Window
{
    private readonly Configuration configuration;
    private readonly Action saveConfiguration;
    private readonly Action openSettings;
    private readonly Action openDebug;

    public MainWindow(Configuration configuration, Action saveConfiguration, Action openSettings, Action openDebug, string version)
        : base($"Olympus v{version}", ImGuiWindowFlags.NoCollapse)
    {
        this.configuration = configuration;
        this.saveConfiguration = saveConfiguration;
        this.openSettings = openSettings;
        this.openDebug = openDebug;

        Size = new Vector2(250, 210);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var statusColor = configuration.Enabled
            ? new Vector4(0.0f, 1.0f, 0.0f, 1.0f)
            : new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        var statusText = configuration.Enabled ? "ACTIVE" : "INACTIVE";

        ImGui.Text("Status:");
        ImGui.SameLine();
        ImGui.TextColored(statusColor, statusText);

        ImGui.Separator();

        ImGui.Text("Modules:");
        ImGui.BulletText("Apollo (White Mage)");
        ImGui.BulletText("Athena (Scholar)");
        ImGui.TextDisabled("Intelligent healing, damage, and cooldown management");

        ImGui.Separator();

        if (ImGui.Button(configuration.Enabled ? "Disable" : "Enable", new Vector2(-1, 0)))
        {
            configuration.Enabled = !configuration.Enabled;
            saveConfiguration();
        }

        if (ImGui.Button("Settings", new Vector2(-1, 0)))
        {
            openSettings();
        }

        if (ImGui.Button("Debug", new Vector2(-1, 0)))
        {
            openDebug();
        }
    }
}
