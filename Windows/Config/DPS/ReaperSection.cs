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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnableSoulReaver, "Enable Soul Reaver"),
                () => config.Reaper.EnableSoulReaver,
                v => config.Reaper.EnableSoulReaver = v,
                Loc.T(LocalizedStrings.Reaper.EnableSoulReaverDesc, "Use Gibbet/Gallows/Guillotine"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnableCommunio, "Enable Communio"),
                () => config.Reaper.EnableCommunio,
                v => config.Reaper.EnableCommunio = v,
                Loc.T(LocalizedStrings.Reaper.EnableCommunioDesc, "Use Communio finisher"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnablePerfectio, "Enable Perfectio"),
                () => config.Reaper.EnablePerfectio,
                v => config.Reaper.EnablePerfectio = v,
                Loc.T(LocalizedStrings.Reaper.EnablePerfectioDesc, "Use Perfectio"), save);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnableAoERotation, "Enable AoE Rotation"),
                () => config.Reaper.EnableAoERotation,
                v => config.Reaper.EnableAoERotation = v,
                Loc.T(LocalizedStrings.Reaper.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.Reaper.EnableAoERotation)
            {
                config.Reaper.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.Reaper.AoEMinTargets, "AoE Min Targets"),
                    config.Reaper.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Reaper.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);
            }

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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnableEnshroud, "Enable Enshroud"),
                () => config.Reaper.EnableEnshroud,
                v => config.Reaper.EnableEnshroud = v,
                Loc.T(LocalizedStrings.Reaper.EnableEnshroudDesc, "Enter Enshroud burst window"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnableLemureAbilities, "Enable Lemure Abilities"),
                () => config.Reaper.EnableLemureAbilities,
                v => config.Reaper.EnableLemureAbilities = v,
                Loc.T(LocalizedStrings.Reaper.EnableLemureAbilitiesDesc, "Use Lemure abilities during Enshroud"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.SaveShroudForBurst, "Save Shroud for Burst"),
                () => config.Reaper.SaveShroudForBurst,
                v => config.Reaper.SaveShroudForBurst = v,
                Loc.T(LocalizedStrings.Reaper.SaveShroudForBurstDesc, "Hold Shroud gauge for burst windows"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Reaper.BurstSection, "Burst Windows"), "RPR", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnableArcaneCircle, "Enable Arcane Circle"),
                () => config.Reaper.EnableArcaneCircle,
                v => config.Reaper.EnableArcaneCircle = v,
                Loc.T(LocalizedStrings.Reaper.EnableArcaneCircleDesc, "Use Arcane Circle (party buff)"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.AlignWithParty, "Align with Party"),
                () => config.Reaper.AlignArcaneCircleWithParty,
                v => config.Reaper.AlignArcaneCircleWithParty = v,
                Loc.T(LocalizedStrings.Reaper.AlignWithPartyDesc, "Coordinate Arcane Circle with party burst"), save);

            config.Reaper.ArcaneCircleHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Reaper.ArcaneCircleHoldTime, "Arcane Circle Hold Time"),
                config.Reaper.ArcaneCircleHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Reaper.ArcaneCircleHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
