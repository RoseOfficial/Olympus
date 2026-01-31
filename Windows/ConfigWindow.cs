using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Olympus.Config;
using Olympus.Localization;
using Olympus.Windows.Config;
using Olympus.Windows.Config.DPS;
using Olympus.Windows.Config.Healers;
using Olympus.Windows.Config.Shared;
using Olympus.Windows.Config.Tanks;

namespace Olympus.Windows;

/// <summary>
/// Main configuration window with sidebar navigation.
/// </summary>
public sealed class ConfigWindow : Window
{
    private readonly Configuration configuration;
    private readonly Action saveConfiguration;
    private ConfigurationPreset selectedPreset;

    // Search state
    private string searchQuery = string.Empty;
    private HashSet<ConfigSection>? matchingSections;
    private readonly SettingRegistry settingRegistry = new();

    // Sidebar navigation
    private readonly ConfigSidebar sidebar = new();

    // Section renderers
    private readonly GeneralSection generalSection;
    private readonly WhiteMageSection whiteMageSection;
    private readonly ScholarSection scholarSection;
    private readonly AstrologianSection astrologianSection;
    private readonly SageSection sageSection;
    private readonly TankSharedSection tankSharedSection;
    private readonly PaladinSection paladinSection;
    private readonly WarriorSection warriorSection;
    private readonly DarkKnightSection darkKnightSection;
    private readonly GunbreakerSection gunbreakerSection;

    // DPS Section renderers
    private readonly MeleeDpsSharedSection meleeDpsSharedSection;
    private readonly DragoonSection dragoonSection;
    private readonly NinjaSection ninjaSection;
    private readonly SamuraiSection samuraiSection;
    private readonly MonkSection monkSection;
    private readonly ReaperSection reaperSection;
    private readonly ViperSection viperSection;
    private readonly RangedDpsSharedSection rangedDpsSharedSection;
    private readonly MachinistSection machinistSection;
    private readonly BardSection bardSection;
    private readonly DancerSection dancerSection;
    private readonly CasterSharedSection casterSharedSection;
    private readonly BlackMageSection blackMageSection;
    private readonly SummonerSection summonerSection;
    private readonly RedMageSection redMageSection;
    private readonly PictomancerSection pictomancerSection;

    public ConfigWindow(Configuration configuration, Action saveConfiguration)
        : base(Loc.T(LocalizedStrings.Config.WindowTitle, "Olympus Settings"), ImGuiWindowFlags.NoCollapse)
    {
        this.configuration = configuration;
        this.saveConfiguration = saveConfiguration;

        // Initialize all section renderers
        generalSection = new GeneralSection(configuration, saveConfiguration);
        whiteMageSection = new WhiteMageSection(configuration, saveConfiguration);
        scholarSection = new ScholarSection(configuration, saveConfiguration);
        astrologianSection = new AstrologianSection(configuration, saveConfiguration);
        sageSection = new SageSection(configuration, saveConfiguration);
        tankSharedSection = new TankSharedSection(configuration, saveConfiguration);
        paladinSection = new PaladinSection(configuration, saveConfiguration);
        warriorSection = new WarriorSection(configuration, saveConfiguration);
        darkKnightSection = new DarkKnightSection(configuration, saveConfiguration);
        gunbreakerSection = new GunbreakerSection(configuration, saveConfiguration);

        // Initialize DPS section renderers
        meleeDpsSharedSection = new MeleeDpsSharedSection(configuration, saveConfiguration);
        dragoonSection = new DragoonSection(configuration, saveConfiguration);
        ninjaSection = new NinjaSection(configuration, saveConfiguration);
        samuraiSection = new SamuraiSection(configuration, saveConfiguration);
        monkSection = new MonkSection(configuration, saveConfiguration);
        reaperSection = new ReaperSection(configuration, saveConfiguration);
        viperSection = new ViperSection(configuration, saveConfiguration);
        rangedDpsSharedSection = new RangedDpsSharedSection(configuration, saveConfiguration);
        machinistSection = new MachinistSection(configuration, saveConfiguration);
        bardSection = new BardSection(configuration, saveConfiguration);
        dancerSection = new DancerSection(configuration, saveConfiguration);
        casterSharedSection = new CasterSharedSection(configuration, saveConfiguration);
        blackMageSection = new BlackMageSection(configuration, saveConfiguration);
        summonerSection = new SummonerSection(configuration, saveConfiguration);
        redMageSection = new RedMageSection(configuration, saveConfiguration);
        pictomancerSection = new PictomancerSection(configuration, saveConfiguration);

        Size = new Vector2(650, 700);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        DrawSearchBox();
        DrawHeader();

        ImGui.Separator();
        ImGui.Spacing();

        // Main layout: Sidebar + Content
        DrawMainLayout();

        ImGui.Spacing();
        ImGui.Separator();

        DrawFooter();
    }

    private void DrawSearchBox()
    {
        // Search box with icon hint
        ImGui.PushItemWidth(ImGui.GetContentRegionAvail().X - 30);
        var previousQuery = this.searchQuery;
        if (ImGui.InputTextWithHint("##ConfigSearch",
            Loc.T(LocalizedStrings.Config.SearchPlaceholder, "Search settings..."),
            ref this.searchQuery, 256))
        {
            UpdateSearchResults();
        }
        ImGui.PopItemWidth();

        // Clear button
        ImGui.SameLine();
        if (!string.IsNullOrEmpty(this.searchQuery))
        {
            if (ImGui.Button("X##ClearSearch"))
            {
                this.searchQuery = string.Empty;
                this.matchingSections = null;
                ConfigUIHelpers.CurrentSearchQuery = null;
            }
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Loc.T(LocalizedStrings.Config.SearchClearTooltip, "Clear search"));
            }
        }
        else
        {
            // Placeholder for alignment
            ImGui.Dummy(new Vector2(20, 0));
        }

        // Show result count when searching
        if (!string.IsNullOrEmpty(this.searchQuery))
        {
            var count = this.matchingSections?.Count ?? 0;
            if (count == 0)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f),
                    Loc.T(LocalizedStrings.Config.SearchNoResults, "No settings found"));
            }
            else
            {
                ImGui.TextDisabled($"{count} section(s) found");
            }
        }

        ImGui.Spacing();
    }

    private void UpdateSearchResults()
    {
        if (string.IsNullOrWhiteSpace(this.searchQuery))
        {
            this.matchingSections = null;
            ConfigUIHelpers.CurrentSearchQuery = null;
            return;
        }

        this.matchingSections = this.settingRegistry.Search(this.searchQuery);
        ConfigUIHelpers.CurrentSearchQuery = this.searchQuery;

        // Auto-navigate to first matching section if current section is not in results
        if (this.matchingSections.Count > 0 && !this.matchingSections.Contains(this.sidebar.CurrentSection))
        {
            foreach (var section in this.matchingSections)
            {
                this.sidebar.SetSection(section);
                break;
            }
        }
    }

    private void DrawHeader()
    {
        // Discord community button
        var discordColor = new Vector4(88f / 255f, 101f / 255f, 242f / 255f, 1.0f);
        ImGui.PushStyleColor(ImGuiCol.Button, discordColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, discordColor * 1.1f);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, discordColor * 0.9f);
        if (ImGui.Button(Loc.T(LocalizedStrings.Config.JoinDiscord, "Join Discord"), new Vector2(100, 0)))
        {
            Util.OpenLink("https://discord.gg/3gXYyqbdaU");
        }
        ImGui.PopStyleColor(3);

        ImGui.SameLine();

        var enabled = configuration.Enabled;
        if (ImGui.Checkbox(Loc.T(LocalizedStrings.Config.EnableRotation, "Enable Rotation"), ref enabled))
        {
            configuration.Enabled = enabled;
            saveConfiguration();
        }

        ImGui.TextDisabled(Loc.T(LocalizedStrings.Config.EnableRotationDesc, "When enabled, the rotation will automatically cast spells."));

        ImGui.Spacing();

        // Configuration Preset selector
        DrawPresetSelector();
    }

    private void DrawMainLayout()
    {
        // Calculate available height for the main area
        var availableHeight = ImGui.GetContentRegionAvail().Y - 40; // Reserve space for footer

        // Sidebar
        ImGui.BeginChild("##SidebarContainer", new Vector2(160, availableHeight), false);
        this.sidebar.Draw(this.searchQuery, this.matchingSections);
        ImGui.EndChild();

        ImGui.SameLine();

        // Content area
        ImGui.BeginChild("##ContentArea", new Vector2(0, availableHeight), true);
        DrawCurrentSection();
        ImGui.EndChild();
    }

    private void DrawCurrentSection()
    {
        switch (sidebar.CurrentSection)
        {
            case ConfigSection.General:
                generalSection.DrawGeneral();
                break;

            case ConfigSection.Targeting:
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), Loc.T(LocalizedStrings.Targeting.Header, "Targeting Settings"));
                ImGui.Spacing();
                generalSection.DrawTargeting();
                break;

            case ConfigSection.RoleActions:
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), Loc.T(LocalizedStrings.RoleActions.Header, "Role Actions"));
                ImGui.Spacing();
                generalSection.DrawRoleActions();
                break;

            case ConfigSection.WhiteMage:
                whiteMageSection.Draw();
                break;

            case ConfigSection.Scholar:
                scholarSection.Draw();
                break;

            case ConfigSection.Astrologian:
                astrologianSection.Draw();
                break;

            case ConfigSection.Sage:
                sageSection.Draw();
                break;

            case ConfigSection.TankShared:
                tankSharedSection.Draw();
                break;

            case ConfigSection.Paladin:
                paladinSection.Draw();
                break;

            case ConfigSection.Warrior:
                warriorSection.Draw();
                break;

            case ConfigSection.DarkKnight:
                darkKnightSection.Draw();
                break;

            case ConfigSection.Gunbreaker:
                gunbreakerSection.Draw();
                break;

            // Melee DPS
            case ConfigSection.MeleeDpsShared:
                meleeDpsSharedSection.Draw();
                break;

            case ConfigSection.Dragoon:
                dragoonSection.Draw();
                break;

            case ConfigSection.Ninja:
                ninjaSection.Draw();
                break;

            case ConfigSection.Samurai:
                samuraiSection.Draw();
                break;

            case ConfigSection.Monk:
                monkSection.Draw();
                break;

            case ConfigSection.Reaper:
                reaperSection.Draw();
                break;

            case ConfigSection.Viper:
                viperSection.Draw();
                break;

            // Ranged Physical DPS
            case ConfigSection.RangedDpsShared:
                rangedDpsSharedSection.Draw();
                break;

            case ConfigSection.Machinist:
                machinistSection.Draw();
                break;

            case ConfigSection.Bard:
                bardSection.Draw();
                break;

            case ConfigSection.Dancer:
                dancerSection.Draw();
                break;

            // Casters
            case ConfigSection.CasterShared:
                casterSharedSection.Draw();
                break;

            case ConfigSection.BlackMage:
                blackMageSection.Draw();
                break;

            case ConfigSection.Summoner:
                summonerSection.Draw();
                break;

            case ConfigSection.RedMage:
                redMageSection.Draw();
                break;

            case ConfigSection.Pictomancer:
                pictomancerSection.Draw();
                break;
        }
    }

    private void DrawFooter()
    {
        if (ImGui.Button(Loc.T(LocalizedStrings.Config.ResetToDefaults, "Reset to Defaults")))
        {
            ImGui.OpenPopup(Loc.T(LocalizedStrings.Config.ResetConfirmation, "Reset Confirmation"));
        }

        // Local variable for popup close button state - must be true to show close button
        var popupOpen = true;
        if (ImGui.BeginPopupModal(Loc.T(LocalizedStrings.Config.ResetConfirmation, "Reset Confirmation"), ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text(Loc.T(LocalizedStrings.Config.ResetQuestion, "Reset all settings to default values?"));
            ImGui.Text(Loc.T(LocalizedStrings.Config.ResetWarning, "This cannot be undone."));
            ImGui.Spacing();

            if (ImGui.Button(Loc.T(LocalizedStrings.Config.YesReset, "Yes, Reset"), new Vector2(120, 0)))
            {
                configuration.ResetToDefaults();
                saveConfiguration();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button(Loc.T(LocalizedStrings.Config.Cancel, "Cancel"), new Vector2(120, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    #region Preset Selector

    private static readonly string[] PresetNames = Enum.GetNames<ConfigurationPreset>();

    private void DrawPresetSelector()
    {
        ImGui.Text(Loc.T(LocalizedStrings.Config.ConfigPreset, "Configuration Preset"));
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text(Loc.T(LocalizedStrings.Config.PresetTooltip, "Presets quickly configure settings for different content types."));
            ImGui.Text(Loc.T(LocalizedStrings.Config.PresetRaid, "Raid: Co-healer aware, balanced DPS"));
            ImGui.Text(Loc.T(LocalizedStrings.Config.PresetDungeon, "Dungeon: Solo healer, aggressive DPS"));
            ImGui.Text(Loc.T(LocalizedStrings.Config.PresetCasual, "Casual: Safe mode, healing priority"));
            ImGui.EndTooltip();
        }

        var currentPreset = (int)configuration.ActivePreset;
        ImGui.SetNextItemWidth(150);
        if (ImGui.Combo("##PresetCombo", ref currentPreset, PresetNames, PresetNames.Length))
        {
            selectedPreset = (ConfigurationPreset)currentPreset;
            if (selectedPreset != ConfigurationPreset.Custom)
            {
                ImGui.OpenPopup(Loc.T(LocalizedStrings.Config.ApplyPresetConfirmation, "Apply Preset Confirmation"));
            }
        }

        ImGui.SameLine();
        ImGui.TextDisabled(ConfigurationPresets.GetDescription(configuration.ActivePreset));

        DrawPresetConfirmationPopup();
    }

    private void DrawPresetConfirmationPopup()
    {
        var popupOpen = true;
        if (ImGui.BeginPopupModal(Loc.T(LocalizedStrings.Config.ApplyPresetConfirmation, "Apply Preset Confirmation"), ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text(Loc.TFormat(LocalizedStrings.Config.ApplyPreset, "Apply {0} preset?", selectedPreset));
            ImGui.Spacing();
            ImGui.TextWrapped(ConfigurationPresets.GetDescription(selectedPreset));
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), Loc.T(LocalizedStrings.Config.OverwriteWarning, "This will overwrite behavior settings."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Config.PreservedSettings, "Spell toggles and targeting preferences are preserved."));
            ImGui.Spacing();

            if (ImGui.Button(Loc.T(LocalizedStrings.Config.Apply, "Apply"), new Vector2(100, 0)))
            {
                ConfigurationPresets.ApplyPreset(configuration, selectedPreset);
                saveConfiguration();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button(Loc.T(LocalizedStrings.Config.Cancel, "Cancel"), new Vector2(100, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    #endregion
}
