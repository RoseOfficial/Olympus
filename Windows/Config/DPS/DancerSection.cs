using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Dancer (Terpsichore) settings section.
/// </summary>
public sealed class DancerSection
{
    private readonly Configuration config;
    private readonly Action save;

    public DancerSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Dancer", "Terpsichore", ConfigUIHelpers.DancerColor);

        DrawDamageSection();
        DrawDanceSection();
        DrawGaugeSection();
        DrawBurstSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dancer.DamageSection, "Damage"), "DNC"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableProcs, "Enable Procs"),
                () => config.Dancer.EnableProcs,
                v => config.Dancer.EnableProcs = v,
                Loc.T(LocalizedStrings.Dancer.EnableProcsDesc, "Use proc weaponskills"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableStarfallDance, "Enable Starfall Dance"),
                () => config.Dancer.EnableStarfallDance,
                v => config.Dancer.EnableStarfallDance = v,
                Loc.T(LocalizedStrings.Dancer.EnableStarfallDanceDesc, "Use Starfall Dance"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableTillana, "Enable Tillana"),
                () => config.Dancer.EnableTillana,
                v => config.Dancer.EnableTillana = v,
                Loc.T(LocalizedStrings.Dancer.EnableTillanaDesc, "Use Tillana"), save);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableAoERotation, "Enable AoE Rotation"),
                () => config.Dancer.EnableAoERotation,
                v => config.Dancer.EnableAoERotation = v,
                Loc.T(LocalizedStrings.Dancer.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.Dancer.EnableAoERotation)
            {
                config.Dancer.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.Dancer.AoEMinTargets, "AoE Min Targets"),
                    config.Dancer.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Dancer.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDanceSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dancer.DanceSection, "Dances"), "DNC"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableStandardStep, "Enable Standard Step"),
                () => config.Dancer.EnableStandardStep,
                v => config.Dancer.EnableStandardStep = v,
                Loc.T(LocalizedStrings.Dancer.EnableStandardStepDesc, "Use Standard Step"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableTechnicalStep, "Enable Technical Step"),
                () => config.Dancer.EnableTechnicalStep,
                v => config.Dancer.EnableTechnicalStep = v,
                Loc.T(LocalizedStrings.Dancer.EnableTechnicalStepDesc, "Use Technical Step"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.DelayStandardForTechnical, "Delay Standard for Technical"),
                () => config.Dancer.DelayStandardForTechnical,
                v => config.Dancer.DelayStandardForTechnical = v,
                Loc.T(LocalizedStrings.Dancer.DelayStandardForTechnicalDesc, "Hold Standard Step if Technical is coming soon"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawGaugeSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dancer.GaugeSection, "Esprit/Feathers"), "DNC"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableSaberDance, "Enable Saber Dance"),
                () => config.Dancer.EnableSaberDance,
                v => config.Dancer.EnableSaberDance = v,
                Loc.T(LocalizedStrings.Dancer.EnableSaberDanceDesc, "Use Saber Dance"), save);

            config.Dancer.SaberDanceMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Dancer.SaberDanceMinGauge, "Saber Dance Min Gauge"),
                config.Dancer.SaberDanceMinGauge, 50, 100,
                Loc.T(LocalizedStrings.Dancer.SaberDanceMinGaugeDesc, "Minimum Esprit for Saber Dance"), save);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableFanDance, "Enable Fan Dance"),
                () => config.Dancer.EnableFanDance,
                v => config.Dancer.EnableFanDance = v,
                Loc.T(LocalizedStrings.Dancer.EnableFanDanceDesc, "Use Fan Dance abilities"), save);

            config.Dancer.FanDanceMinFeathers = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Dancer.FanDanceMinFeathers, "Fan Dance Min Feathers"),
                config.Dancer.FanDanceMinFeathers, 1, 4,
                Loc.T(LocalizedStrings.Dancer.FanDanceMinFeathersDesc, "Minimum Feathers for Fan Dance"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dancer.BurstSection, "Burst Windows"), "DNC", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableDevilment, "Enable Devilment"),
                () => config.Dancer.EnableDevilment,
                v => config.Dancer.EnableDevilment = v,
                Loc.T(LocalizedStrings.Dancer.EnableDevilmentDesc, "Use Devilment"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.AlignWithParty, "Align with Party"),
                () => config.Dancer.AlignTechnicalWithParty,
                v => config.Dancer.AlignTechnicalWithParty = v,
                Loc.T(LocalizedStrings.Dancer.AlignWithPartyDesc, "Coordinate Technical Finish with party burst"), save);

            config.Dancer.TechnicalHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Dancer.TechnicalHoldTime, "Technical Hold Time"),
                config.Dancer.TechnicalHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Dancer.TechnicalHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
