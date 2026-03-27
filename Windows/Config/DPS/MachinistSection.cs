using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Machinist (Prometheus) settings section.
/// </summary>
public sealed class MachinistSection
{
    private readonly Configuration config;
    private readonly Action save;

    public MachinistSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Machinist", "Prometheus", ConfigUIHelpers.MachinistColor);

        DrawDamageSection();
        DrawGaugeSection();
        DrawHyperchargeSection();
        DrawQueenSection();
        DrawBurstSection();
        DrawUtilitySection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Machinist.DamageSection, "Damage"), "MCH"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableDrill, "Enable Drill"),
                () => config.Machinist.EnableDrill,
                v => config.Machinist.EnableDrill = v,
                null, save, actionId: MCHActions.Drill.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableAirAnchor, "Enable Air Anchor"),
                () => config.Machinist.EnableAirAnchor,
                v => config.Machinist.EnableAirAnchor = v,
                null, save, actionId: MCHActions.AirAnchor.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableChainSaw, "Enable Chain Saw"),
                () => config.Machinist.EnableChainSaw,
                v => config.Machinist.EnableChainSaw = v,
                null, save, actionId: MCHActions.ChainSaw.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableExcavator, "Enable Excavator"),
                () => config.Machinist.EnableExcavator,
                v => config.Machinist.EnableExcavator = v,
                null, save, actionId: MCHActions.Excavator.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableFullMetalField, "Enable Full Metal Field"),
                () => config.Machinist.EnableFullMetalField,
                v => config.Machinist.EnableFullMetalField = v,
                null, save, actionId: MCHActions.FullMetalField.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableGaussRicochet, "Enable Gauss Round / Ricochet"),
                () => config.Machinist.EnableGaussRicochet,
                v => config.Machinist.EnableGaussRicochet = v,
                null, save, actionId: MCHActions.GaussRound.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableAoERotation, "Enable AoE Rotation"),
                () => config.Machinist.EnableAoERotation,
                v => config.Machinist.EnableAoERotation = v,
                Loc.T(LocalizedStrings.Machinist.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.Machinist.EnableAoERotation)
            {
                config.Machinist.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.Machinist.AoEMinTargets, "AoE Min Targets"),
                    config.Machinist.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Machinist.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save, v => config.Machinist.AoEMinTargets = v);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawGaugeSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Machinist.GaugeSection, "Heat/Battery Gauges"), "MCH"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Machinist.HeatLabel, "Heat Gauge:"));

            config.Machinist.HeatMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Machinist.HeatMinGauge, "Heat Min Gauge"),
                config.Machinist.HeatMinGauge, 50, 100,
                Loc.T(LocalizedStrings.Machinist.HeatMinGaugeDesc, "Minimum Heat for Hypercharge"), save, v => config.Machinist.HeatMinGauge = v);

            config.Machinist.HeatOvercapThreshold = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Machinist.HeatOvercapThreshold, "Heat Overcap Threshold"),
                config.Machinist.HeatOvercapThreshold, 50, 100,
                Loc.T(LocalizedStrings.Machinist.HeatOvercapThresholdDesc, "Dump Heat above this to avoid overcap"), save, v => config.Machinist.HeatOvercapThreshold = v);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.SaveHeatForWildfire, "Save Heat for Wildfire"),
                () => config.Machinist.SaveHeatForWildfire,
                v => config.Machinist.SaveHeatForWildfire = v,
                Loc.T(LocalizedStrings.Machinist.SaveHeatForWildfireDesc, "Hold Heat gauge for Wildfire windows"), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Machinist.BatteryLabel, "Battery Gauge:"));

            config.Machinist.BatteryMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Machinist.BatteryMinGauge, "Battery Min Gauge"),
                config.Machinist.BatteryMinGauge, 50, 100,
                Loc.T(LocalizedStrings.Machinist.BatteryMinGaugeDesc, "Minimum Battery to summon Queen"), save, v => config.Machinist.BatteryMinGauge = v);

            config.Machinist.BatteryOvercapThreshold = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Machinist.BatteryOvercapThreshold, "Battery Overcap Threshold"),
                config.Machinist.BatteryOvercapThreshold, 50, 100,
                Loc.T(LocalizedStrings.Machinist.BatteryOvercapThresholdDesc, "Summon Queen above this to avoid overcap"), save, v => config.Machinist.BatteryOvercapThreshold = v);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawQueenSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Machinist.QueenSection, "Automaton Queen"), "MCH"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableAutomatonQueen, "Enable Automaton Queen"),
                () => config.Machinist.EnableAutomatonQueen,
                v => config.Machinist.EnableAutomatonQueen = v,
                null, save, actionId: MCHActions.AutomatonQueen.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.SaveBatteryForBurst, "Save Battery for Burst"),
                () => config.Machinist.SaveBatteryForBurst,
                v => config.Machinist.SaveBatteryForBurst = v,
                Loc.T(LocalizedStrings.Machinist.SaveBatteryForBurstDesc, "Hold Battery gauge for burst windows"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Machinist.BurstSection, "Burst Windows"), "MCH", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableWildfire, "Enable Wildfire"),
                () => config.Machinist.EnableWildfire,
                v => config.Machinist.EnableWildfire = v,
                null, save, actionId: MCHActions.Wildfire.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableBarrelStabilizer, "Enable Barrel Stabilizer"),
                () => config.Machinist.EnableBarrelStabilizer,
                v => config.Machinist.EnableBarrelStabilizer = v,
                null, save, actionId: MCHActions.BarrelStabilizer.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableReassemble, "Enable Reassemble"),
                () => config.Machinist.EnableReassemble,
                v => config.Machinist.EnableReassemble = v,
                null, save, actionId: MCHActions.Reassemble.ActionId);

            if (config.Machinist.EnableReassemble)
            {
                var reassemblePriority = config.Machinist.ReassemblePriority;
                if (ConfigUIHelpers.EnumCombo(Loc.T(LocalizedStrings.Machinist.ReassemblePriority, "Reassemble Priority"), ref reassemblePriority,
                    Loc.T(LocalizedStrings.Machinist.ReassemblePriorityDesc, "Which weapon skill to use Reassemble on"), save))
                {
                    config.Machinist.ReassemblePriority = reassemblePriority;
                }
            }

            config.Machinist.WildfireHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Machinist.WildfireHoldTime, "Wildfire Hold Time"),
                config.Machinist.WildfireHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Machinist.WildfireHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save, v => config.Machinist.WildfireHoldTime = v);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableBurstPooling, "Enable Burst Pooling"),
                () => config.Machinist.EnableBurstPooling,
                v => config.Machinist.EnableBurstPooling = v,
                Loc.T(LocalizedStrings.Machinist.EnableBurstPoolingDesc, "Hold Heat gauge for party burst windows"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawHyperchargeSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Machinist.HyperchargeSection, "Hypercharge"), "MCH"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableHypercharge, "Enable Hypercharge"),
                () => config.Machinist.EnableHypercharge,
                v => config.Machinist.EnableHypercharge = v,
                null, save, actionId: MCHActions.Hypercharge.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableHeatBlast, "Enable Heat Blast"),
                () => config.Machinist.EnableHeatBlast,
                v => config.Machinist.EnableHeatBlast = v,
                null, save, actionId: MCHActions.HeatBlast.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableAutoCrossbow, "Enable Auto Crossbow"),
                () => config.Machinist.EnableAutoCrossbow,
                v => config.Machinist.EnableAutoCrossbow = v,
                null, save, actionId: MCHActions.AutoCrossbow.ActionId);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawUtilitySection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Machinist.UtilitySection, "Utility"), "MCH", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableHeadGraze, "Enable Head Graze"),
                () => config.Machinist.EnableHeadGraze,
                v => config.Machinist.EnableHeadGraze = v,
                Loc.T(LocalizedStrings.Machinist.EnableHeadGrazeDesc, "Use Head Graze for interrupts"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
