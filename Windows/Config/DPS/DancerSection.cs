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

            var enableProcs = config.Dancer.EnableProcs;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dancer.EnableProcs, "Enable Procs"), ref enableProcs,
                Loc.T(LocalizedStrings.Dancer.EnableProcsDesc, "Use proc weaponskills"), save))
            {
                config.Dancer.EnableProcs = enableProcs;
            }

            var enableStarfallDance = config.Dancer.EnableStarfallDance;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dancer.EnableStarfallDance, "Enable Starfall Dance"), ref enableStarfallDance,
                Loc.T(LocalizedStrings.Dancer.EnableStarfallDanceDesc, "Use Starfall Dance"), save))
            {
                config.Dancer.EnableStarfallDance = enableStarfallDance;
            }

            var enableTillana = config.Dancer.EnableTillana;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dancer.EnableTillana, "Enable Tillana"), ref enableTillana,
                Loc.T(LocalizedStrings.Dancer.EnableTillanaDesc, "Use Tillana"), save))
            {
                config.Dancer.EnableTillana = enableTillana;
            }

            ConfigUIHelpers.Spacing();

            config.Dancer.AoEMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Dancer.AoEMinTargets, "AoE Min Targets"),
                config.Dancer.AoEMinTargets, 2, 8,
                Loc.T(LocalizedStrings.Dancer.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDanceSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dancer.DanceSection, "Dances"), "DNC"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableStandardStep = config.Dancer.EnableStandardStep;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dancer.EnableStandardStep, "Enable Standard Step"), ref enableStandardStep,
                Loc.T(LocalizedStrings.Dancer.EnableStandardStepDesc, "Use Standard Step"), save))
            {
                config.Dancer.EnableStandardStep = enableStandardStep;
            }

            var enableTechnicalStep = config.Dancer.EnableTechnicalStep;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dancer.EnableTechnicalStep, "Enable Technical Step"), ref enableTechnicalStep,
                Loc.T(LocalizedStrings.Dancer.EnableTechnicalStepDesc, "Use Technical Step"), save))
            {
                config.Dancer.EnableTechnicalStep = enableTechnicalStep;
            }

            var delayStandardForTechnical = config.Dancer.DelayStandardForTechnical;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dancer.DelayStandardForTechnical, "Delay Standard for Technical"), ref delayStandardForTechnical,
                Loc.T(LocalizedStrings.Dancer.DelayStandardForTechnicalDesc, "Hold Standard Step if Technical is coming soon"), save))
            {
                config.Dancer.DelayStandardForTechnical = delayStandardForTechnical;
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawGaugeSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dancer.GaugeSection, "Esprit/Feathers"), "DNC"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableSaberDance = config.Dancer.EnableSaberDance;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dancer.EnableSaberDance, "Enable Saber Dance"), ref enableSaberDance,
                Loc.T(LocalizedStrings.Dancer.EnableSaberDanceDesc, "Use Saber Dance"), save))
            {
                config.Dancer.EnableSaberDance = enableSaberDance;
            }

            config.Dancer.SaberDanceMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Dancer.SaberDanceMinGauge, "Saber Dance Min Gauge"),
                config.Dancer.SaberDanceMinGauge, 50, 100,
                Loc.T(LocalizedStrings.Dancer.SaberDanceMinGaugeDesc, "Minimum Esprit for Saber Dance"), save);

            ConfigUIHelpers.Spacing();

            var enableFanDance = config.Dancer.EnableFanDance;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dancer.EnableFanDance, "Enable Fan Dance"), ref enableFanDance,
                Loc.T(LocalizedStrings.Dancer.EnableFanDanceDesc, "Use Fan Dance abilities"), save))
            {
                config.Dancer.EnableFanDance = enableFanDance;
            }

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

            var enableDevilment = config.Dancer.EnableDevilment;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dancer.EnableDevilment, "Enable Devilment"), ref enableDevilment,
                Loc.T(LocalizedStrings.Dancer.EnableDevilmentDesc, "Use Devilment"), save))
            {
                config.Dancer.EnableDevilment = enableDevilment;
            }

            var alignWithParty = config.Dancer.AlignTechnicalWithParty;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Dancer.AlignWithParty, "Align with Party"), ref alignWithParty,
                Loc.T(LocalizedStrings.Dancer.AlignWithPartyDesc, "Coordinate Technical Finish with party burst"), save))
            {
                config.Dancer.AlignTechnicalWithParty = alignWithParty;
            }

            config.Dancer.TechnicalHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Dancer.TechnicalHoldTime, "Technical Hold Time"),
                config.Dancer.TechnicalHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Dancer.TechnicalHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
