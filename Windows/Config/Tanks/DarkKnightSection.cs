using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;

namespace Olympus.Windows.Config.Tanks;

/// <summary>
/// Renders the Dark Knight (Nyx) settings section.
/// </summary>
public sealed class DarkKnightSection
{
    private readonly Configuration config;
    private readonly Action save;

    public DarkKnightSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Dark Knight", "Nyx", ConfigUIHelpers.DarkKnightColor);

        DrawMitigationSection();
        DrawBloodGaugeSection();
        DrawDamageSection();
    }

    private void DrawMitigationSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.DarkKnight.MitigationSection, "Mitigation"), "DRK"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.DarkKnight.MitigationDesc, "Dark Knight-specific mitigation settings:"));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.DarkKnight.TBNLabel, "The Blackest Night (TBN):"));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.DarkKnight.TBNDesc1, "Powerful single-target shield."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.DarkKnight.TBNDesc2, "Grants Dark Arts when shield breaks."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.UsesSharedGaugeSetting, "Uses shared tank gauge setting."));
            ImGui.TextDisabled(Loc.TFormat(LocalizedStrings.Tank.CurrentMinGauge, "Current minimum: {0}", config.Tank.SheltronMinGauge));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.DarkKnight.AvailableAbilities, "Available Abilities:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.TheBlackestNight, "The Blackest Night"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.Oblation, "Oblation"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.DarkMind, "Dark Mind (magic only)"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.DarkMissionary, "Dark Missionary"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.LivingDead, "Living Dead"));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText(Loc.T(LocalizedStrings.DarkKnight.DetailedSettingsWarning, "Detailed DRK settings coming in future update."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBloodGaugeSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.DarkKnight.BloodGaugeMPSection, "Blood Gauge & MP"), "DRK", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.DarkKnight.BloodGaugeLabel, "Blood Gauge:"));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.DarkKnight.BloodGaugeDesc1, "Built from combo actions."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.DarkKnight.BloodGaugeDesc2, "Spent on Bloodspiller/Quietus and Living Shadow."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.DarkKnight.MPManagement, "MP Management:"));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.DarkKnight.MPDesc1, "Edge of Shadow/Flood of Shadow costs 3000 MP."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.DarkKnight.MPDesc2, "TBN costs 3000 MP."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.DarkKnight.MPDesc3, "Balance offense and defense."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.DarkKnight.DeliriumLabel, "Delirium:"));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.DarkKnight.DeliriumDesc1, "Grants 3 free Bloodspillers."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.DarkKnight.DeliriumDesc2, "Scarlet Delirium follow-up combo."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText(Loc.T(LocalizedStrings.DarkKnight.BloodGaugeWarning, "Blood Gauge settings coming in future update."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.DarkKnight.DamageSection, "Damage"), "DRK"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.DarkKnight.RotationFeatures, "Rotation Features:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.HardSlashCombo, "Hard Slash combo"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.DeliriumScarletCombo, "Delirium + Scarlet Delirium combo"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.EdgeOfShadowWeaves, "Edge of Shadow weaves"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.Shadowbringer, "Shadowbringer"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.LivingShadow, "Living Shadow"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.SaltedEarthDarkness, "Salted Earth + Salt and Darkness"));

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.DarkKnight.AoERotation, "AoE Rotation:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.UnleashCombo, "Unleash combo"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.QuietusDelirium, "Quietus under Delirium"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.FloodOfShadow, "Flood of Shadow"));
            ImGui.BulletText(Loc.T(LocalizedStrings.DarkKnight.AbyssalDrain, "Abyssal Drain"));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.UsesSharedAoESettings, "Uses shared tank AoE settings."));
            ImGui.TextDisabled(Loc.TFormat(LocalizedStrings.Tank.CurrentMinTargets, "Current min targets: {0}", config.Tank.AoEMinTargets));

            ConfigUIHelpers.EndIndent();
        }
    }
}
