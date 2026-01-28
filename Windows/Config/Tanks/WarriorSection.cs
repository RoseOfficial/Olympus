using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;

namespace Olympus.Windows.Config.Tanks;

/// <summary>
/// Renders the Warrior (Ares) settings section.
/// </summary>
public sealed class WarriorSection
{
    private readonly Configuration config;
    private readonly Action save;

    public WarriorSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Warrior", "Ares", ConfigUIHelpers.WarriorColor);

        DrawMitigationSection();
        DrawBeastGaugeSection();
        DrawDamageSection();
    }

    private void DrawMitigationSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Warrior.MitigationSection, "Mitigation"), "WAR"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.Warrior.MitigationDesc, "Warrior-specific mitigation settings:"));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Warrior.BeastGaugeAbilities, "Beast Gauge Abilities:"));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Warrior.BeastGaugeDesc, "Uses shared tank gauge setting for Raw Intuition."));
            ImGui.TextDisabled(Loc.TFormat(LocalizedStrings.Tank.CurrentMinGauge, "Current minimum: {0}", config.Tank.SheltronMinGauge));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Warrior.AvailableAbilities, "Available Abilities:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.RawIntuitionBloodwhetting, "Raw Intuition / Bloodwhetting"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.NascentFlash, "Nascent Flash (party member)"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.ThrillOfBattle, "Thrill of Battle"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.Equilibrium, "Equilibrium (self-heal)"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.ShakeItOff, "Shake It Off"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.Holmgang, "Holmgang"));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText(Loc.T(LocalizedStrings.Warrior.DetailedSettingsWarning, "Detailed WAR settings coming in future update."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBeastGaugeSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Warrior.BeastGaugeSection, "Beast Gauge"), "WAR", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Warrior.GaugeUsage, "Gauge Usage:"));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Warrior.GaugeBuilds, "Beast Gauge builds from combo actions."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Warrior.GaugeSpent, "Spent on Fell Cleave/Decimate and Raw Intuition."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Warrior.InnerReleaseLabel, "Inner Release:"));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Warrior.InnerReleaseDesc1, "Burst window with free Fell Cleaves."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Warrior.InnerReleaseDesc2, "Primal Rend follow-up."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText(Loc.T(LocalizedStrings.Warrior.BeastGaugeWarning, "Beast Gauge settings coming in future update."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Warrior.DamageSection, "Damage"), "WAR"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Warrior.RotationFeatures, "Rotation Features:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.HeavySwingCombo, "Heavy Swing combo"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.InnerReleaseWindow, "Inner Release window"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.FellCleaveSpam, "Fell Cleave spam"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.PrimalRendRuination, "Primal Rend + Primal Ruination"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.OnslaughtCharges, "Onslaught charges"));

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Warrior.AoERotation, "AoE Rotation:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.OverpowerCombo, "Overpower combo"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.DecimateInnerRelease, "Decimate under Inner Release"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Warrior.Orogeny, "Orogeny"));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.UsesSharedAoESettings, "Uses shared tank AoE settings."));
            ImGui.TextDisabled(Loc.TFormat(LocalizedStrings.Tank.CurrentMinTargets, "Current min targets: {0}", config.Tank.AoEMinTargets));

            ConfigUIHelpers.EndIndent();
        }
    }
}
