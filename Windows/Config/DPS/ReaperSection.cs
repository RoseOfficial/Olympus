using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
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
        DrawPositionalSection();
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
                Loc.T(LocalizedStrings.Reaper.AlternateGibbetGallows, "Alternate Gibbet/Gallows"),
                () => config.Reaper.AlternateGibbetGallows,
                v => config.Reaper.AlternateGibbetGallows = v,
                Loc.T(LocalizedStrings.Reaper.AlternateGibbetGallowsDesc, "Automatically alternate between Gibbet and Gallows based on position"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnableGluttony, "Enable Gluttony"),
                () => config.Reaper.EnableGluttony,
                v => config.Reaper.EnableGluttony = v,
                null, save,
                actionId: RPRActions.Gluttony.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnableCommunio, "Enable Communio"),
                () => config.Reaper.EnableCommunio,
                v => config.Reaper.EnableCommunio = v,
                null, save,
                actionId: RPRActions.Communio.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnablePerfectio, "Enable Perfectio"),
                () => config.Reaper.EnablePerfectio,
                v => config.Reaper.EnablePerfectio = v,
                null, save,
                actionId: RPRActions.Perfectio.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnablePlentifulHarvest, "Enable Plentiful Harvest"),
                () => config.Reaper.EnablePlentifulHarvest,
                v => config.Reaper.EnablePlentifulHarvest = v,
                null, save,
                actionId: RPRActions.PlentifulHarvest.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnableHarvestMoon, "Enable Harvest Moon"),
                () => config.Reaper.EnableHarvestMoon,
                v => config.Reaper.EnableHarvestMoon = v,
                null, save,
                actionId: RPRActions.HarvestMoon.ActionId);

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
                null, save,
                actionId: RPRActions.Enshroud.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnableLemureAbilities, "Enable Lemure Abilities"),
                () => config.Reaper.EnableLemureAbilities,
                v => config.Reaper.EnableLemureAbilities = v,
                Loc.T(LocalizedStrings.Reaper.EnableLemureAbilitiesDesc, "Use Lemure abilities during Enshroud"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnableAvatarSummon, "Enable Avatar Summon"),
                () => config.Reaper.EnableAvatarSummon,
                v => config.Reaper.EnableAvatarSummon = v,
                Loc.T(LocalizedStrings.Reaper.EnableAvatarSummonDesc, "Summon Immortal Sacrifice stacks via Enshroud"), save);

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
                null, save,
                actionId: RPRActions.ArcaneCircle.ActionId);

            config.Reaper.ArcaneCircleHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Reaper.ArcaneCircleHoldTime, "Arcane Circle Hold Time"),
                config.Reaper.ArcaneCircleHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Reaper.ArcaneCircleHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.UseEnshroudDuringArcaneCircle, "Enshroud During Arcane Circle"),
                () => config.Reaper.UseEnshroudDuringArcaneCircle,
                v => config.Reaper.UseEnshroudDuringArcaneCircle = v,
                Loc.T(LocalizedStrings.Reaper.UseEnshroudDuringArcaneCircleDesc, "Enter Enshroud inside Arcane Circle windows"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnableBurstPooling, "Enable Burst Pooling"),
                () => config.Reaper.EnableBurstPooling,
                v => config.Reaper.EnableBurstPooling = v,
                Loc.T(LocalizedStrings.Reaper.EnableBurstPoolingDesc, "Hold Soul spenders for party burst windows"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawPositionalSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Reaper.PositionalSection, "Positionals"), "RPR", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.EnforcePositionals, "Enforce Positionals"),
                () => config.Reaper.EnforcePositionals,
                v => config.Reaper.EnforcePositionals = v,
                Loc.T(LocalizedStrings.Reaper.EnforcePositionalsDesc, "Only use positional actions when in correct position"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Reaper.AllowPositionalLoss, "Allow Positional Loss"),
                () => config.Reaper.AllowPositionalLoss,
                v => config.Reaper.AllowPositionalLoss = v,
                Loc.T(LocalizedStrings.Reaper.AllowPositionalLossDesc, "Continue rotation even if positionals will miss"), save);

            config.Reaper.DeathsDesignRefreshThreshold = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Reaper.DeathsDesignRefreshThreshold, "Death's Design Refresh"),
                config.Reaper.DeathsDesignRefreshThreshold, 0f, 30f, "%.1f s",
                Loc.T(LocalizedStrings.Reaper.DeathsDesignRefreshThresholdDesc, "Seconds remaining on Death's Design before refreshing the DoT"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
