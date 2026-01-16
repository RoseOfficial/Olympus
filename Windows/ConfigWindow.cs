using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Olympus.Config;
using Olympus.Windows.Config;
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

    public ConfigWindow(Configuration configuration, Action saveConfiguration)
        : base("Olympus Settings", ImGuiWindowFlags.NoCollapse)
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

        Size = new Vector2(650, 700);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        DrawHeader();

        ImGui.Separator();
        ImGui.Spacing();

        // Main layout: Sidebar + Content
        DrawMainLayout();

        ImGui.Spacing();
        ImGui.Separator();

        DrawFooter();
    }

    private void DrawHeader()
    {
        // Discord community button
        var discordColor = new Vector4(88f / 255f, 101f / 255f, 242f / 255f, 1.0f);
        ImGui.PushStyleColor(ImGuiCol.Button, discordColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, discordColor * 1.1f);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, discordColor * 0.9f);
        if (ImGui.Button("Join Discord", new Vector2(100, 0)))
        {
            Util.OpenLink("https://discord.gg/3gXYyqbdaU");
        }
        ImGui.PopStyleColor(3);

        ImGui.SameLine();

        var enabled = configuration.Enabled;
        if (ImGui.Checkbox("Enable Rotation", ref enabled))
        {
            configuration.Enabled = enabled;
            saveConfiguration();
        }

        ImGui.TextDisabled("When enabled, the rotation will automatically cast spells.");

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
        sidebar.Draw();
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
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Targeting Settings");
                ImGui.Spacing();
                generalSection.DrawTargeting();
                break;

            case ConfigSection.RoleActions:
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.8f, 1f), "Role Actions");
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
        }
    }

    private void DrawFooter()
    {
        if (ImGui.Button("Reset to Defaults"))
        {
            ImGui.OpenPopup("Reset Confirmation");
        }

        // Local variable for popup close button state - must be true to show close button
        var popupOpen = true;
        if (ImGui.BeginPopupModal("Reset Confirmation", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Reset all settings to default values?");
            ImGui.Text("This cannot be undone.");
            ImGui.Spacing();

            if (ImGui.Button("Yes, Reset", new Vector2(120, 0)))
            {
                configuration.ResetToDefaults();
                saveConfiguration();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(120, 0)))
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
        ImGui.Text("Configuration Preset");
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text("Presets quickly configure settings for different content types.");
            ImGui.Text("Raid: Co-healer aware, balanced DPS");
            ImGui.Text("Dungeon: Solo healer, aggressive DPS");
            ImGui.Text("Casual: Safe mode, healing priority");
            ImGui.EndTooltip();
        }

        var currentPreset = (int)configuration.ActivePreset;
        ImGui.SetNextItemWidth(150);
        if (ImGui.Combo("##PresetCombo", ref currentPreset, PresetNames, PresetNames.Length))
        {
            selectedPreset = (ConfigurationPreset)currentPreset;
            if (selectedPreset != ConfigurationPreset.Custom)
            {
                ImGui.OpenPopup("Apply Preset Confirmation");
            }
        }

        ImGui.SameLine();
        ImGui.TextDisabled(ConfigurationPresets.GetDescription(configuration.ActivePreset));

        DrawPresetConfirmationPopup();
    }

    private void DrawPresetConfirmationPopup()
    {
        var popupOpen = true;
        if (ImGui.BeginPopupModal("Apply Preset Confirmation", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text($"Apply {selectedPreset} preset?");
            ImGui.Spacing();
            ImGui.TextWrapped(ConfigurationPresets.GetDescription(selectedPreset));
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), "This will overwrite behavior settings.");
            ImGui.TextDisabled("Spell toggles and targeting preferences are preserved.");
            ImGui.Spacing();

            if (ImGui.Button("Apply", new Vector2(100, 0)))
            {
                ConfigurationPresets.ApplyPreset(configuration, selectedPreset);
                saveConfiguration();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(100, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    #endregion
}
