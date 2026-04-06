using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
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

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.DarkKnight.EnableTheBlackestNight, "The Blackest Night"),
                () => config.Tank.EnableTheBlackestNight,
                v => config.Tank.EnableTheBlackestNight = v,
                null,
                save,
                actionId: DRKActions.TheBlackestNight.ActionId);

            ConfigUIHelpers.BeginDisabledGroup(!config.Tank.EnableTheBlackestNight);
            config.Tank.TBNThreshold = ConfigUIHelpers.ThresholdSlider(
                Loc.T(LocalizedStrings.DarkKnight.TBNThreshold, "TBN Threshold"),
                config.Tank.TBNThreshold, 50f, 100f,
                Loc.T(LocalizedStrings.DarkKnight.TBNThresholdDesc, "Apply TBN when HP is above this %, ensuring the shield will break for Dark Arts."),
                save, v => config.Tank.TBNThreshold = v);
            ConfigUIHelpers.EndDisabledGroup();

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.DarkKnight.EnableLivingDead, "Living Dead"),
                () => config.Tank.EnableLivingDead,
                v => config.Tank.EnableLivingDead = v,
                null,
                save,
                actionId: DRKActions.LivingDead.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.DarkKnight.EnableDarkMissionary, "Dark Missionary"),
                () => config.Tank.EnableDarkMissionary,
                v => config.Tank.EnableDarkMissionary = v,
                null,
                save,
                actionId: DRKActions.DarkMissionary.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                "Shadow Wall / Shadowed Vigil",
                () => config.Tank.EnableShadowWall,
                v => config.Tank.EnableShadowWall = v,
                null,
                save,
                actionId: DRKActions.ShadowWall.ActionId);

            ConfigUIHelpers.Toggle(
                "Dark Mind",
                () => config.Tank.EnableDarkMind,
                v => config.Tank.EnableDarkMind = v,
                null,
                save,
                actionId: DRKActions.DarkMind.ActionId);

            ConfigUIHelpers.Toggle(
                "Oblation",
                () => config.Tank.EnableOblation,
                v => config.Tank.EnableOblation = v,
                null,
                save,
                actionId: DRKActions.Oblation.ActionId);

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

            config.Tank.BloodGaugeCap = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.DarkKnight.BloodGaugeCap, "Blood Gauge Cap"),
                config.Tank.BloodGaugeCap, 0, 100,
                Loc.T(LocalizedStrings.DarkKnight.BloodGaugeCapDesc, "Spend Blood Gauge before reaching this amount to avoid overcapping."),
                save, v => config.Tank.BloodGaugeCap = v);

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

            ConfigUIHelpers.Toggle(
                "Blood Weapon",
                () => config.Tank.EnableBloodWeapon,
                v => config.Tank.EnableBloodWeapon = v,
                null,
                save,
                actionId: DRKActions.BloodWeapon.ActionId);

            ConfigUIHelpers.Toggle(
                "Delirium",
                () => config.Tank.EnableDelirium,
                v => config.Tank.EnableDelirium = v,
                null,
                save,
                actionId: DRKActions.Delirium.ActionId);

            ConfigUIHelpers.Toggle(
                "Living Shadow",
                () => config.Tank.EnableLivingShadow,
                v => config.Tank.EnableLivingShadow = v,
                null,
                save,
                actionId: DRKActions.LivingShadow.ActionId);

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

            ConfigUIHelpers.SectionLabel("Damage oGCDs:");

            ConfigUIHelpers.Toggle(
                "Salted Earth",
                () => config.Tank.EnableSaltedEarth,
                v => config.Tank.EnableSaltedEarth = v,
                null,
                save,
                actionId: DRKActions.SaltedEarth.ActionId);

            ConfigUIHelpers.Toggle(
                "Carve and Spit",
                () => config.Tank.EnableCarveAndSpit,
                v => config.Tank.EnableCarveAndSpit = v,
                null,
                save,
                actionId: DRKActions.CarveAndSpit.ActionId);

            ConfigUIHelpers.Toggle(
                "Shadowbringer",
                () => config.Tank.EnableShadowbringer,
                v => config.Tank.EnableShadowbringer = v,
                null,
                save,
                actionId: DRKActions.Shadowbringer.ActionId);

            ConfigUIHelpers.Toggle(
                "Abyssal Drain",
                () => config.Tank.EnableAbyssalDrain,
                v => config.Tank.EnableAbyssalDrain = v,
                null,
                save,
                actionId: DRKActions.AbyssalDrain.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel("Gap Closer:");

            ConfigUIHelpers.Toggle(
                "Shadowstride",
                () => config.Tank.EnableShadowstride,
                v => config.Tank.EnableShadowstride = v,
                null,
                save,
                actionId: DRKActions.Shadowstride.ActionId);

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.UsesSharedAoESettings, "Uses shared tank AoE settings."));
            ImGui.TextDisabled(Loc.TFormat(LocalizedStrings.Tank.CurrentMinTargets, "Current min targets: {0}", config.Tank.AoEMinTargets));

            ConfigUIHelpers.EndIndent();
        }
    }
}
