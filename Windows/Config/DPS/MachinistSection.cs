using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
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
        DrawQueenSection();
        DrawBurstSection();
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
                Loc.T(LocalizedStrings.Machinist.EnableDrillDesc, "Use Drill"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableAirAnchor, "Enable Air Anchor"),
                () => config.Machinist.EnableAirAnchor,
                v => config.Machinist.EnableAirAnchor = v,
                Loc.T(LocalizedStrings.Machinist.EnableAirAnchorDesc, "Use Air Anchor"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableChainSaw, "Enable Chain Saw"),
                () => config.Machinist.EnableChainSaw,
                v => config.Machinist.EnableChainSaw = v,
                Loc.T(LocalizedStrings.Machinist.EnableChainSawDesc, "Use Chain Saw"), save);

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
                    Loc.T(LocalizedStrings.Machinist.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);
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
                Loc.T(LocalizedStrings.Machinist.HeatMinGaugeDesc, "Minimum Heat for Hypercharge"), save);

            config.Machinist.HeatOvercapThreshold = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Machinist.HeatOvercapThreshold, "Heat Overcap Threshold"),
                config.Machinist.HeatOvercapThreshold, 50, 100,
                Loc.T(LocalizedStrings.Machinist.HeatOvercapThresholdDesc, "Dump Heat above this to avoid overcap"), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Machinist.BatteryLabel, "Battery Gauge:"));

            config.Machinist.BatteryMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Machinist.BatteryMinGauge, "Battery Min Gauge"),
                config.Machinist.BatteryMinGauge, 50, 100,
                Loc.T(LocalizedStrings.Machinist.BatteryMinGaugeDesc, "Minimum Battery to summon Queen"), save);

            config.Machinist.BatteryOvercapThreshold = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Machinist.BatteryOvercapThreshold, "Battery Overcap Threshold"),
                config.Machinist.BatteryOvercapThreshold, 50, 100,
                Loc.T(LocalizedStrings.Machinist.BatteryOvercapThresholdDesc, "Summon Queen above this to avoid overcap"), save);

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
                Loc.T(LocalizedStrings.Machinist.EnableAutomatonQueenDesc, "Summon Automaton Queen"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.EnableQueenOverdrive, "Enable Queen Overdrive"),
                () => config.Machinist.EnableQueenOverdrive,
                v => config.Machinist.EnableQueenOverdrive = v,
                Loc.T(LocalizedStrings.Machinist.EnableQueenOverdriveDesc, "Use Queen Overdrive for burst"), save);

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
                Loc.T(LocalizedStrings.Machinist.EnableWildfireDesc, "Use Wildfire"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Machinist.AlignWithParty, "Align with Party"),
                () => config.Machinist.AlignWildfireWithParty,
                v => config.Machinist.AlignWildfireWithParty = v,
                Loc.T(LocalizedStrings.Machinist.AlignWithPartyDesc, "Coordinate Wildfire with party burst"), save);

            config.Machinist.WildfireHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Machinist.WildfireHoldTime, "Wildfire Hold Time"),
                config.Machinist.WildfireHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Machinist.WildfireHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
