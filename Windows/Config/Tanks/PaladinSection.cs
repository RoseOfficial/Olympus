using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
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

            ConfigUIHelpers.Toggle(
                "Sheltron / Holy Sheltron",
                () => config.Tank.EnableSheltron,
                v => config.Tank.EnableSheltron = v,
                null,
                save,
                actionId: PLDActions.Sheltron.ActionId);

            ConfigUIHelpers.Toggle(
                "Sentinel / Guardian",
                () => config.Tank.EnableSentinel,
                v => config.Tank.EnableSentinel = v,
                null,
                save,
                actionId: PLDActions.Sentinel.ActionId);

            ConfigUIHelpers.Toggle(
                "Hallowed Ground",
                () => config.Tank.EnableHallowedGround,
                v => config.Tank.EnableHallowedGround = v,
                null,
                save,
                actionId: PLDActions.HallowedGround.ActionId);

            ConfigUIHelpers.Toggle(
                "Bulwark",
                () => config.Tank.EnableBulwark,
                v => config.Tank.EnableBulwark = v,
                null,
                save,
                actionId: PLDActions.Bulwark.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Paladin.EnableCover, "Cover"),
                () => config.Tank.EnableCover,
                v => config.Tank.EnableCover = v,
                null,
                save,
                actionId: PLDActions.Cover.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Paladin.EnableDivineVeil, "Divine Veil"),
                () => config.Tank.EnableDivineVeil,
                v => config.Tank.EnableDivineVeil = v,
                null,
                save,
                actionId: PLDActions.DivineVeil.ActionId);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawHealingSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Paladin.SelfHealingSection, "Self-Healing"), "PLD", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Paladin.EnableClemency, "Clemency"),
                () => config.Tank.EnableClemency,
                v => config.Tank.EnableClemency = v,
                null,
                save,
                actionId: PLDActions.Clemency.ActionId);

            ConfigUIHelpers.BeginDisabledGroup(!config.Tank.EnableClemency);
            config.Tank.ClemencyThreshold = ConfigUIHelpers.ThresholdSlider(
                Loc.T(LocalizedStrings.Paladin.ClemencyThreshold, "Clemency Threshold"),
                config.Tank.ClemencyThreshold, 20f, 70f,
                Loc.T(LocalizedStrings.Paladin.ClemencyThresholdDesc, "Use Clemency when HP falls below this %."),
                save, v => config.Tank.ClemencyThreshold = v);
            ConfigUIHelpers.EndDisabledGroup();

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Paladin.DamageSection, "Damage"), "PLD"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Buff Abilities:");

            ConfigUIHelpers.Toggle(
                "Fight or Flight",
                () => config.Tank.EnableFightOrFlight,
                v => config.Tank.EnableFightOrFlight = v,
                null,
                save,
                actionId: PLDActions.FightOrFlight.ActionId);

            ConfigUIHelpers.Toggle(
                "Requiescat",
                () => config.Tank.EnableRequiescat,
                v => config.Tank.EnableRequiescat = v,
                null,
                save,
                actionId: PLDActions.Requiescat.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Damage oGCDs:");

            ConfigUIHelpers.Toggle(
                "Circle of Scorn",
                () => config.Tank.EnableCircleOfScorn,
                v => config.Tank.EnableCircleOfScorn = v,
                null,
                save,
                actionId: PLDActions.CircleOfScorn.ActionId);

            ConfigUIHelpers.Toggle(
                "Spirits Within / Expiacion",
                () => config.Tank.EnableSpiritsWithin,
                v => config.Tank.EnableSpiritsWithin = v,
                null,
                save,
                actionId: PLDActions.SpiritsWithin.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Gap Closer:");

            ConfigUIHelpers.Toggle(
                "Intervene",
                () => config.Tank.EnableIntervene,
                v => config.Tank.EnableIntervene = v,
                null,
                save,
                actionId: PLDActions.Intervene.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Paladin.RotationFeatures, "Rotation Features:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.FastBladeCombo, "Fast Blade combo"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.ConfiteorCombo, "Confiteor combo"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.BladeOfHonor, "Blade of Honor"));

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Paladin.AoERotation, "AoE Rotation:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Paladin.TotalEclipseCombo, "Total Eclipse combo"));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.UsesSharedAoESettings, "Uses shared tank AoE settings."));
            ImGui.TextDisabled(Loc.TFormat(LocalizedStrings.Tank.CurrentMinTargets, "Current min targets: {0}", config.Tank.AoEMinTargets));

            ConfigUIHelpers.EndIndent();
        }
    }
}
