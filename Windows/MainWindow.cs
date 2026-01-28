using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Data;
using Olympus.Localization;
using Olympus.Rotation;

namespace Olympus.Windows;

public sealed class MainWindow : Window
{
    private readonly Configuration configuration;
    private readonly Action saveConfiguration;
    private readonly Action openSettings;
    private readonly Action openDebug;
    private readonly Action openAnalytics;
    private readonly Action openTraining;
    private readonly RotationManager rotationManager;

    public MainWindow(
        Configuration configuration,
        Action saveConfiguration,
        Action openSettings,
        Action openDebug,
        Action openAnalytics,
        Action openTraining,
        string version,
        RotationManager rotationManager)
        : base($"Olympus v{version}", ImGuiWindowFlags.NoCollapse)
    {
        this.configuration = configuration;
        this.saveConfiguration = saveConfiguration;
        this.openSettings = openSettings;
        this.openDebug = openDebug;
        this.openAnalytics = openAnalytics;
        this.openTraining = openTraining;
        this.rotationManager = rotationManager;

        Size = new Vector2(250, 290);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var statusColor = configuration.Enabled
            ? new Vector4(0.0f, 1.0f, 0.0f, 1.0f)
            : new Vector4(1.0f, 0.0f, 0.0f, 1.0f);
        var statusText = configuration.Enabled
            ? Loc.T(LocalizedStrings.Main.Active, "ACTIVE")
            : Loc.T(LocalizedStrings.Main.Inactive, "INACTIVE");

        ImGui.Text(Loc.T(LocalizedStrings.Main.Status, "Status:"));
        ImGui.SameLine();
        ImGui.TextColored(statusColor, statusText);

        ImGui.Separator();

        // Show active rotation
        ImGui.Text(Loc.T(LocalizedStrings.Main.ActiveRotation, "Active Rotation:"));
        var activeRotation = rotationManager.ActiveRotation;
        if (activeRotation != null)
        {
            var jobName = JobRegistry.GetJobName(activeRotation.SupportedJobIds[0]);
            var activeColor = new Vector4(0.4f, 0.8f, 1.0f, 1.0f);
            ImGui.TextColored(activeColor, $"{activeRotation.Name} ({jobName})");
        }
        else
        {
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Main.SwitchToSupported, "None (switch to supported job)"));
        }

        ImGui.Separator();

        // List available rotations
        ImGui.Text(Loc.T(LocalizedStrings.Main.Available, "Available:"));
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

        var enableDisableText = configuration.Enabled
            ? Loc.T(LocalizedStrings.Main.Disable, "Disable")
            : Loc.T(LocalizedStrings.Main.Enable, "Enable");

        if (ImGui.Button(enableDisableText, new Vector2(-1, 0)))
        {
            configuration.Enabled = !configuration.Enabled;
            saveConfiguration();
        }

        if (ImGui.Button(Loc.T(LocalizedStrings.Main.Settings, "Settings"), new Vector2(-1, 0)))
        {
            openSettings();
        }

        if (ImGui.Button(Loc.T(LocalizedStrings.Main.Analytics, "Analytics"), new Vector2(-1, 0)))
        {
            openAnalytics();
        }

        if (ImGui.Button(Loc.T(LocalizedStrings.Main.Training, "Training"), new Vector2(-1, 0)))
        {
            openTraining();
        }

        if (ImGui.Button(Loc.T(LocalizedStrings.Main.Debug, "Debug"), new Vector2(-1, 0)))
        {
            openDebug();
        }
    }
}
