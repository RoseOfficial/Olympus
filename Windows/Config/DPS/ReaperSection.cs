using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Reaper (Thanatos) settings section.
/// </summary>
public sealed class ReaperSection
{
    private readonly Configuration config;
    private readonly Action save;

    public ReaperSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Reaper", "Thanatos", ConfigUIHelpers.ReaperColor);

        DrawDamageSection();
        DrawGaugeSection();
        DrawEnshroudSection();
        DrawBurstSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Reaper.DamageSection, "Damage"), "RPR"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableSoulReaver = config.Reaper.EnableSoulReaver;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Reaper.EnableSoulReaver, "Enable Soul Reaver"), ref enableSoulReaver,
                Loc.T(LocalizedStrings.Reaper.EnableSoulReaverDesc, "Use Gibbet/Gallows/Guillotine"), save))
            {
                config.Reaper.EnableSoulReaver = enableSoulReaver;
            }

            var enableCommunio = config.Reaper.EnableCommunio;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Reaper.EnableCommunio, "Enable Communio"), ref enableCommunio,
                Loc.T(LocalizedStrings.Reaper.EnableCommunioDesc, "Use Communio finisher"), save))
            {
                config.Reaper.EnableCommunio = enableCommunio;
            }

            var enablePerfectio = config.Reaper.EnablePerfectio;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Reaper.EnablePerfectio, "Enable Perfectio"), ref enablePerfectio,
                Loc.T(LocalizedStrings.Reaper.EnablePerfectioDesc, "Use Perfectio"), save))
            {
                config.Reaper.EnablePerfectio = enablePerfectio;
            }

            ConfigUIHelpers.Spacing();

            config.Reaper.AoEMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Reaper.AoEMinTargets, "AoE Min Targets"),
                config.Reaper.AoEMinTargets, 2, 8,
                Loc.T(LocalizedStrings.Reaper.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawGaugeSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Reaper.GaugeSection, "Soul/Shroud Gauges"), "RPR"))
        {
            ConfigUIHelpers.BeginIndent();

            config.Reaper.SoulMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Reaper.SoulMinGauge, "Soul Min Gauge"),
                config.Reaper.SoulMinGauge, 50, 100,
                Loc.T(LocalizedStrings.Reaper.SoulMinGaugeDesc, "Minimum Soul to use Blood Stalk/Grim Swathe"), save);

            config.Reaper.SoulOvercapThreshold = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Reaper.SoulOvercapThreshold, "Soul Overcap Threshold"),
                config.Reaper.SoulOvercapThreshold, 50, 100,
                Loc.T(LocalizedStrings.Reaper.SoulOvercapThresholdDesc, "Dump Soul above this to avoid overcap"), save);

            ConfigUIHelpers.Spacing();

            config.Reaper.ShroudMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Reaper.ShroudMinGauge, "Shroud Min Gauge"),
                config.Reaper.ShroudMinGauge, 50, 100,
                Loc.T(LocalizedStrings.Reaper.ShroudMinGaugeDesc, "Minimum Shroud to enter Enshroud"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawEnshroudSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Reaper.EnshroudSection, "Enshroud"), "RPR"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableEnshroud = config.Reaper.EnableEnshroud;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Reaper.EnableEnshroud, "Enable Enshroud"), ref enableEnshroud,
                Loc.T(LocalizedStrings.Reaper.EnableEnshroudDesc, "Enter Enshroud burst window"), save))
            {
                config.Reaper.EnableEnshroud = enableEnshroud;
            }

            var enableLemureAbilities = config.Reaper.EnableLemureAbilities;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Reaper.EnableLemureAbilities, "Enable Lemure Abilities"), ref enableLemureAbilities,
                Loc.T(LocalizedStrings.Reaper.EnableLemureAbilitiesDesc, "Use Lemure abilities during Enshroud"), save))
            {
                config.Reaper.EnableLemureAbilities = enableLemureAbilities;
            }

            var saveShroudForBurst = config.Reaper.SaveShroudForBurst;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Reaper.SaveShroudForBurst, "Save Shroud for Burst"), ref saveShroudForBurst,
                Loc.T(LocalizedStrings.Reaper.SaveShroudForBurstDesc, "Hold Shroud gauge for burst windows"), save))
            {
                config.Reaper.SaveShroudForBurst = saveShroudForBurst;
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Reaper.BurstSection, "Burst Windows"), "RPR", false))
        {
            ConfigUIHelpers.BeginIndent();

            var enableArcaneCircle = config.Reaper.EnableArcaneCircle;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Reaper.EnableArcaneCircle, "Enable Arcane Circle"), ref enableArcaneCircle,
                Loc.T(LocalizedStrings.Reaper.EnableArcaneCircleDesc, "Use Arcane Circle (party buff)"), save))
            {
                config.Reaper.EnableArcaneCircle = enableArcaneCircle;
            }

            var alignWithParty = config.Reaper.AlignArcaneCircleWithParty;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Reaper.AlignWithParty, "Align with Party"), ref alignWithParty,
                Loc.T(LocalizedStrings.Reaper.AlignWithPartyDesc, "Coordinate Arcane Circle with party burst"), save))
            {
                config.Reaper.AlignArcaneCircleWithParty = alignWithParty;
            }

            config.Reaper.ArcaneCircleHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Reaper.ArcaneCircleHoldTime, "Arcane Circle Hold Time"),
                config.Reaper.ArcaneCircleHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Reaper.ArcaneCircleHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
