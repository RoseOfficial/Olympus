using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Olympus.Data;
using Olympus.Localization;
using Olympus.Rotation;
using Olympus.Rotation.Common;

namespace Olympus.Windows;

public sealed class MainWindow : Window
{
    private readonly Configuration configuration;
    private readonly Action saveConfiguration;
    private readonly Action openSettings;
    private readonly Action openDebug;
    private readonly Action openAnalytics;
    private readonly Action openTraining;
    private readonly Action openOverlay;
    private readonly RotationManager rotationManager;
    private readonly ITextureProvider textureProvider;

    public MainWindow(
        Configuration configuration,
        Action saveConfiguration,
        Action openSettings,
        Action openDebug,
        Action openAnalytics,
        Action openTraining,
        Action openOverlay,
        string version,
        RotationManager rotationManager,
        ITextureProvider textureProvider)
        : base($"Olympus v{version}", ImGuiWindowFlags.NoCollapse)
    {
        this.configuration = configuration;
        this.saveConfiguration = saveConfiguration;
        this.openSettings = openSettings;
        this.openDebug = openDebug;
        this.openAnalytics = openAnalytics;
        this.openTraining = openTraining;
        this.openOverlay = openOverlay;
        this.rotationManager = rotationManager;
        this.textureProvider = textureProvider;

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
            var activeJobId = activeRotation.SupportedJobIds[0];
            var jobName = JobRegistry.GetJobName(activeJobId);
            var activeColor = new Vector4(0.4f, 0.8f, 1.0f, 1.0f);

            var iconId = JobRegistry.GetJobIconId(activeJobId);
            if (iconId != 0)
            {
                var wrap = textureProvider.GetFromGameIcon(new GameIconLookup(iconId)).GetWrapOrEmpty();
                ImGui.Image(wrap.Handle, new Vector2(20, 20));
                ImGui.SameLine();
            }

            ImGui.TextColored(activeColor, $"{activeRotation.Name} ({jobName})");
        }
        else
        {
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Main.SwitchToSupported, "None (switch to supported job)"));
        }

        // Positional indicator — only shown for melee DPS jobs with an active target
        if (activeRotation is IHasPositionals posRotation)
        {
            var pos = posRotation.Positionals;
            if (pos.HasTarget)
            {
                ImGui.Separator();
                ImGui.Text(Loc.T(LocalizedStrings.Main.Positional, "Position:"));
                ImGui.SameLine();
                if (pos.TargetHasImmunity)
                {
                    ImGui.TextDisabled(Loc.T(LocalizedStrings.Main.PositionalImmune, "Immune"));
                }
                else if (pos.IsAtRear)
                {
                    ImGui.TextColored(new Vector4(0.4f, 0.8f, 1f, 1f), Loc.T(LocalizedStrings.Main.PositionalRear, "Rear"));
                }
                else if (pos.IsAtFlank)
                {
                    ImGui.TextColored(new Vector4(0.8f, 0.5f, 1f, 1f), Loc.T(LocalizedStrings.Main.PositionalFlank, "Flank"));
                }
                else
                {
                    ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), Loc.T(LocalizedStrings.Main.PositionalFront, "Front"));
                }
            }
        }

        ImGui.Separator();

        // List available rotations
        ImGui.Text(Loc.T(LocalizedStrings.Main.Available, "Available:"));
        foreach (var rotation in rotationManager.RegisteredRotations)
        {
            bool isActive = rotation == activeRotation;
            var listJobId = rotation.SupportedJobIds[0];
            var jobName = JobRegistry.GetJobName(listJobId);

            var listIconId = JobRegistry.GetJobIconId(listJobId);
            if (listIconId != 0)
            {
                var wrap = textureProvider.GetFromGameIcon(new GameIconLookup(listIconId)).GetWrapOrEmpty();
                var tint = isActive ? Vector4.One : new Vector4(1f, 1f, 1f, 0.45f);
                ImGui.Image(wrap.Handle, new Vector2(16, 16), Vector2.Zero, Vector2.One, tint);
                ImGui.SameLine();
            }

            if (isActive)
                ImGui.Text($"{rotation.Name} ({jobName})");
            else
                ImGui.TextDisabled($"{rotation.Name} ({jobName})");
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

        if (ImGui.Button(Loc.T(LocalizedStrings.Main.Overlay, "Overlay"), new Vector2(-1, 0)))
        {
            openOverlay();
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
