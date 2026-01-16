using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Data;
using Olympus.Rotation;

namespace Olympus.Windows;

public sealed class MainWindow : Window
{
    private readonly Configuration configuration;
    private readonly Action saveConfiguration;
    private readonly Action openSettings;
    private readonly Action openDebug;
    private readonly RotationManager rotationManager;

    public MainWindow(
        Configuration configuration,
        Action saveConfiguration,
        Action openSettings,
        Action openDebug,
        string version,
        RotationManager rotationManager)
        : base($"Olympus v{version}", ImGuiWindowFlags.NoCollapse)
    {
        this.configuration = configuration;
        this.saveConfiguration = saveConfiguration;
        this.openSettings = openSettings;
        this.openDebug = openDebug;
        this.rotationManager = rotationManager;

        Size = new Vector2(250, 230);
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

        // Show active rotation
        ImGui.Text("Active Rotation:");
        var activeRotation = rotationManager.ActiveRotation;
        if (activeRotation != null)
        {
            var jobName = JobRegistry.GetJobName(activeRotation.SupportedJobIds[0]);
            var activeColor = new Vector4(0.4f, 0.8f, 1.0f, 1.0f);
            ImGui.TextColored(activeColor, $"{activeRotation.Name} ({jobName})");
        }
        else
        {
            ImGui.TextDisabled("None (switch to supported job)");
        }

        ImGui.Separator();

        // List available rotations
        ImGui.Text("Available:");
        foreach (var rotation in rotationManager.RegisteredRotations)
        {
            bool isActive = rotation == activeRotation;
            var jobName = JobRegistry.GetJobName(rotation.SupportedJobIds[0]);
            if (isActive)
                ImGui.BulletText($"{rotation.Name} ({jobName})");
            else
                ImGui.TextDisabled($"  {rotation.Name} ({jobName})");
        }

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
