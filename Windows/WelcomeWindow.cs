using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Olympus.Config;
using Olympus.Localization;

namespace Olympus.Windows;

public sealed class WelcomeWindow : Window
{
    private const string DiscordUrl = "https://discord.gg/3gXYyqbdaU";

    private readonly Configuration _configuration;
    private readonly Action _saveConfiguration;
    private readonly Action _openSettings;

    private static readonly string[] PresetNames = Enum.GetNames<ConfigurationPreset>();

    private static readonly Vector4 GoldColor    = new(1.0f, 0.84f, 0.0f, 1.0f);
    private static readonly Vector4 DiscordColor = new(88f / 255f, 101f / 255f, 242f / 255f, 1.0f);

    private const int PageCount = 3;
    private int _page;

    public WelcomeWindow(Configuration configuration, Action saveConfiguration, Action openSettings)
        : base("Welcome to Olympus!", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize | ImGuiWindowFlags.AlwaysAutoResize)
    {
        _configuration = configuration;
        _saveConfiguration = saveConfiguration;
        _openSettings = openSettings;

        SizeCondition = ImGuiCond.Always;
    }

    public override void Draw()
    {
        // Enforce minimum window width so content never wraps too tightly.
        ImGui.Dummy(new Vector2(400, 0));

        switch (_page)
        {
            case 0: DrawWelcomePage(); break;
            case 1: DrawSetupPage();   break;
            case 2: DrawReadyPage();   break;
        }
    }

    // ──────────────────────────────────────────────────────────────
    // Page 0: Welcome
    // ──────────────────────────────────────────────────────────────

    private void DrawWelcomePage()
    {
        ImGui.TextColored(GoldColor, Loc.T(LocalizedStrings.Welcome.Title, "Welcome to Olympus!"));
        ImGui.Spacing();
        ImGui.TextWrapped(Loc.T(
            LocalizedStrings.Welcome.Subtitle,
            "Your intelligent rotation assistant for all 21 FFXIV combat jobs."));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.BulletText(Loc.T(LocalizedStrings.Welcome.FeatureHealing,
            "Automates healing, resurrection, and defensive cooldowns"));
        ImGui.BulletText(Loc.T(LocalizedStrings.Welcome.FeatureDps,
            "Full DPS rotations for all 21 supported jobs"));
        ImGui.BulletText(Loc.T(LocalizedStrings.Welcome.FeaturePositionals,
            "Positional guidance for melee DPS jobs"));
        ImGui.BulletText(Loc.T(LocalizedStrings.Welcome.FeatureCoordination,
            "Party coordination across multiple Olympus instances"));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawPageIndicator();
        ImGui.Spacing();

        if (ImGui.Button(Loc.T(LocalizedStrings.Welcome.Next, "Next →"), new Vector2(-1, 0)))
            _page = 1;
    }

    // ──────────────────────────────────────────────────────────────
    // Page 1: Quick Setup
    // ──────────────────────────────────────────────────────────────

    private void DrawSetupPage()
    {
        ImGui.TextColored(GoldColor, Loc.T(LocalizedStrings.Welcome.SetupTitle, "Quick Setup"));
        ImGui.TextDisabled(Loc.T(LocalizedStrings.Welcome.SetupNote, "You can adjust these anytime in Settings."));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Enable / Disable toggle
        var enabled = _configuration.Enabled;
        if (ImGui.Checkbox(Loc.T(LocalizedStrings.Welcome.EnableRotation, "Enable Olympus"), ref enabled))
        {
            _configuration.Enabled = enabled;
            _saveConfiguration();
        }
        ImGui.TextDisabled(Loc.T(
            LocalizedStrings.Welcome.EnableRotationDesc,
            "Allow Olympus to automatically execute abilities."));

        ImGui.Spacing();

        // Behavior preset
        ImGui.Text(Loc.T(LocalizedStrings.Welcome.PresetLabel, "Behavior Style:"));
        var currentPreset = (int)_configuration.ActivePreset;
        ImGui.SetNextItemWidth(-1);
        if (ImGui.Combo("##WelcomePreset", ref currentPreset, PresetNames, PresetNames.Length))
        {
            var selected = (ConfigurationPreset)currentPreset;
            if (selected != ConfigurationPreset.Custom)
                ConfigurationPresets.ApplyPreset(_configuration, selected);
            else
                _configuration.ActivePreset = ConfigurationPreset.Custom;
            _saveConfiguration();
        }
        ImGui.TextDisabled(Loc.T(LocalizedStrings.Welcome.PresetDesc, "Choose how aggressively Olympus should act."));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        DrawPageIndicator();
        ImGui.Spacing();

        if (ImGui.Button(Loc.T(LocalizedStrings.Welcome.Back, "← Back"), new Vector2(100, 0)))
            _page = 0;
        ImGui.SameLine();
        if (ImGui.Button(Loc.T(LocalizedStrings.Welcome.Next, "Next →"), new Vector2(-1, 0)))
            _page = 2;
    }

    // ──────────────────────────────────────────────────────────────
    // Page 2: All Set
    // ──────────────────────────────────────────────────────────────

    private void DrawReadyPage()
    {
        ImGui.TextColored(GoldColor, Loc.T(LocalizedStrings.Welcome.ReadyTitle, "You're All Set!"));
        ImGui.Spacing();
        ImGui.TextWrapped(Loc.T(LocalizedStrings.Welcome.ReadySubtitle, "A few tips to help you get started:"));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.BulletText(Loc.T(LocalizedStrings.Welcome.TipCommand,
            "Use /olympus to toggle the main window at any time."));
        ImGui.BulletText(Loc.T(LocalizedStrings.Welcome.TipOverlay,
            "The overlay shows your next queued action in real time."));
        ImGui.BulletText(Loc.T(LocalizedStrings.Welcome.TipSettings,
            "Visit Settings to fine-tune behavior for each job."));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Discord button
        ImGui.PushStyleColor(ImGuiCol.Button,        DiscordColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, DiscordColor * 1.15f);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive,  DiscordColor * 0.85f);
        if (ImGui.Button(Loc.T(LocalizedStrings.Welcome.JoinDiscord, "Join Discord"), new Vector2(-1, 0)))
            Util.OpenLink(DiscordUrl);
        ImGui.PopStyleColor(3);

        ImGui.Spacing();

        DrawPageIndicator();
        ImGui.Spacing();

        // Navigation: [← Back]  [Open Settings]  [Let's Go!]
        if (ImGui.Button(Loc.T(LocalizedStrings.Welcome.Back, "← Back"), new Vector2(90, 0)))
            _page = 1;
        ImGui.SameLine();
        if (ImGui.Button(Loc.T(LocalizedStrings.Welcome.OpenSettings, "Open Settings"), new Vector2(-94, 0)))
        {
            _openSettings();
            MarkAsSeenAndClose();
        }
        ImGui.SameLine();
        if (ImGui.Button(Loc.T(LocalizedStrings.Welcome.LetsGo, "Let's Go!"), new Vector2(-1, 0)))
            MarkAsSeenAndClose();
    }

    // ──────────────────────────────────────────────────────────────
    // Helpers
    // ──────────────────────────────────────────────────────────────

    private void DrawPageIndicator()
    {
        ImGui.TextDisabled(Loc.TFormat(
            LocalizedStrings.Welcome.PageIndicator, "{0} / {1}",
            _page + 1, PageCount));
    }

    private void MarkAsSeenAndClose()
    {
        _configuration.HasSeenWelcome = true;
        _saveConfiguration();
        _page = 0;
        IsOpen = false;
    }

    /// <summary>Opens the window on first run (when the user hasn't seen it yet).</summary>
    public void ShowIfNeeded()
    {
        if (!_configuration.HasSeenWelcome)
            IsOpen = true;
    }
}
