using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;

namespace Olympus.Windows;

public sealed class WelcomeWindow : Window
{
    private const string DiscordUrl = "https://discord.gg/3gXYyqbdaU";

    private readonly Configuration _configuration;
    private readonly Action _saveConfiguration;

    public WelcomeWindow(Configuration configuration, Action saveConfiguration)
        : base("Welcome to Olympus!", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize)
    {
        _configuration = configuration;
        _saveConfiguration = saveConfiguration;

        Size = new Vector2(350, 250);
        SizeCondition = ImGuiCond.Always;
    }

    public override void Draw()
    {
        ImGui.TextColored(new Vector4(1.0f, 0.84f, 0.0f, 1.0f), "Welcome to Olympus!");
        ImGui.Spacing();

        ImGui.TextWrapped("Olympus automates healing and damage rotations for White Mage and Scholar in FFXIV.");
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.Text("Join our Discord community for:");
        ImGui.BulletText("Support and troubleshooting");
        ImGui.BulletText("Feature requests");
        ImGui.BulletText("Updates and announcements");

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Discord button with Discord brand color
        var discordColor = new Vector4(88f / 255f, 101f / 255f, 242f / 255f, 1.0f);
        ImGui.PushStyleColor(ImGuiCol.Button, discordColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, discordColor * 1.1f);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, discordColor * 0.9f);

        if (ImGui.Button("Join Discord", new Vector2(150, 30)))
        {
            Util.OpenLink(DiscordUrl);
            MarkAsSeenAndClose();
        }

        ImGui.PopStyleColor(3);

        ImGui.SameLine();

        if (ImGui.Button("Maybe Later", new Vector2(150, 30)))
        {
            MarkAsSeenAndClose();
        }
    }

    private void MarkAsSeenAndClose()
    {
        _configuration.HasSeenWelcome = true;
        _saveConfiguration();
        IsOpen = false;
    }

    /// <summary>
    /// Shows the window if the user hasn't seen the welcome message yet.
    /// </summary>
    public void ShowIfNeeded()
    {
        if (!_configuration.HasSeenWelcome)
        {
            IsOpen = true;
        }
    }
}
