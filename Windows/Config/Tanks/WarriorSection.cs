using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Warrior.EnableVengeance, "Vengeance / Damnation"),
                () => config.Tank.EnableVengeance,
                v => config.Tank.EnableVengeance = v,
                null,
                save,
                actionId: WARActions.Vengeance.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Warrior.EnableBloodWhetting, "Bloodwhetting / Raw Intuition"),
                () => config.Tank.EnableBloodWhetting,
                v => config.Tank.EnableBloodWhetting = v,
                null,
                save,
                actionId: WARActions.Bloodwhetting.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Warrior.EnableThrillOfBattle, "Thrill of Battle"),
                () => config.Tank.EnableThrillOfBattle,
                v => config.Tank.EnableThrillOfBattle = v,
                null,
                save,
                actionId: WARActions.ThrillOfBattle.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Warrior.EnableEquilibrium, "Equilibrium"),
                () => config.Tank.EnableEquilibrium,
                v => config.Tank.EnableEquilibrium = v,
                null,
                save,
                actionId: WARActions.Equilibrium.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Warrior.EnableShakeItOff, "Shake It Off"),
                () => config.Tank.EnableShakeItOff,
                v => config.Tank.EnableShakeItOff = v,
                null,
                save,
                actionId: WARActions.ShakeItOff.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Warrior.EnableNascentFlash, "Nascent Flash"),
                () => config.Tank.EnableNascentFlash,
                v => config.Tank.EnableNascentFlash = v,
                null,
                save,
                actionId: WARActions.NascentFlash.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Warrior.EnableHolmgang, "Holmgang"),
                () => config.Tank.EnableHolmgang,
                v => config.Tank.EnableHolmgang = v,
                null,
                save,
                actionId: WARActions.Holmgang.ActionId);

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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Warrior.EnableInnerRelease, "Inner Release / Berserk"),
                () => config.Tank.EnableInnerRelease,
                v => config.Tank.EnableInnerRelease = v,
                null,
                save,
                actionId: WARActions.InnerRelease.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Warrior.EnableInfuriate, "Infuriate"),
                () => config.Tank.EnableInfuriate,
                v => config.Tank.EnableInfuriate = v,
                null,
                save,
                actionId: WARActions.Infuriate.ActionId);

            ConfigUIHelpers.Spacing();

            config.Tank.BeastGaugeCap = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Warrior.BeastGaugeCap, "Beast Gauge Cap"),
                config.Tank.BeastGaugeCap, 0, 100,
                Loc.T(LocalizedStrings.Warrior.BeastGaugeCapDesc, "Spend Beast Gauge before reaching this amount to avoid overcapping."),
                save, v => config.Tank.BeastGaugeCap = v);

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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Warrior.EnableOrogeny, "Upheaval / Orogeny"),
                () => config.Tank.EnableOrogeny,
                v => config.Tank.EnableOrogeny = v,
                null,
                save,
                actionId: WARActions.Upheaval.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Warrior.EnableOnslaught, "Onslaught"),
                () => config.Tank.EnableOnslaught,
                v => config.Tank.EnableOnslaught = v,
                null,
                save,
                actionId: WARActions.Onslaught.ActionId);

            ConfigUIHelpers.Toggle(
                "Primal Rend",
                () => config.Tank.EnablePrimalRend,
                v => config.Tank.EnablePrimalRend = v,
                null,
                save,
                actionId: WARActions.PrimalRend.ActionId);

            ConfigUIHelpers.Toggle(
                "Primal Ruination",
                () => config.Tank.EnablePrimalRuination,
                v => config.Tank.EnablePrimalRuination = v,
                null,
                save,
                actionId: WARActions.PrimalRuination.ActionId);

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
