using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;

namespace Olympus.Windows.Config.Tanks;

/// <summary>
/// Renders the Paladin (Themis) settings section.
/// </summary>
public sealed class PaladinSection
{
    private readonly Configuration config;
    private readonly Action save;

    public PaladinSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Paladin", "Themis", ConfigUIHelpers.PaladinColor);

        DrawMitigationSection();
        DrawHealingSection();
        DrawDamageSection();
    }

    private void DrawMitigationSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Paladin.MitigationSection, "Mitigation"), "PLD"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.Paladin.MitigationDesc, "Paladin-specific mitigation settings:"));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Paladin.OathGaugeAbilities, "Oath Gauge Abilities:"));

            ImGui.TextDisabled(Loc.T(LocalizedStrings.Paladin.SheltronDesc, "Sheltron uses shared tank gauge setting."));
            ImGui.TextDisabled(Loc.TFormat(LocalizedStrings.Tank.CurrentMinGauge, "Current minimum: {0}", config.Tank.SheltronMinGauge));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Paladin.AvailableAbilities, "Available Abilities:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.SheltronHolySheltron, "Sheltron / Holy Sheltron"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.Intervention, "Intervention"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.Cover, "Cover"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.DivineVeil, "Divine Veil"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.PassageOfArms, "Passage of Arms"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.HallowedGround, "Hallowed Ground"));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText(Loc.T(LocalizedStrings.Paladin.DetailedSettingsWarning, "Detailed PLD settings coming in future update."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawHealingSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Paladin.SelfHealingSection, "Self-Healing"), "PLD", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Paladin.ClemencyLabel, "Clemency:"));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Paladin.ClemencyDesc1, "GCD heal that can be used on self or party members."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Paladin.ClemencyDesc2, "Uses MP that could be spent on damage spells."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText(Loc.T(LocalizedStrings.Paladin.ClemencyWarning, "Clemency settings coming in future update."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Paladin.DamageSection, "Damage"), "PLD"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Paladin.RotationFeatures, "Rotation Features:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.FastBladeCombo, "Fast Blade combo"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.RequiescatWindow, "Requiescat window"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.FightOrFlightWindow, "Fight or Flight window"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.ConfiteorCombo, "Confiteor combo"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.BladeOfHonor, "Blade of Honor"));

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Paladin.AoERotation, "AoE Rotation:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.TotalEclipseCombo, "Total Eclipse combo"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.CircleOfScorn, "Circle of Scorn"));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.UsesSharedAoESettings, "Uses shared tank AoE settings."));
            ImGui.TextDisabled(Loc.TFormat(LocalizedStrings.Tank.CurrentMinTargets, "Current min targets: {0}", config.Tank.AoEMinTargets));

            ConfigUIHelpers.EndIndent();
        }
    }
}
