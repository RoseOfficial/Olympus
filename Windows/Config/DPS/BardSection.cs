using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Bard (Calliope) settings section.
/// </summary>
public sealed class BardSection
{
    private readonly Configuration config;
    private readonly Action save;

    public BardSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Bard", "Calliope", ConfigUIHelpers.BardColor);

        DrawDamageSection();
        DrawSongSection();
        DrawDotSection();
        DrawBurstSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Bard.DamageSection, "Damage"), "BRD"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableRefulgentArrow, "Enable Refulgent Arrow"),
                () => config.Bard.EnableRefulgentArrow,
                v => config.Bard.EnableRefulgentArrow = v,
                null, save, actionId: BRDActions.RefulgentArrow.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableBloodletter, "Enable Bloodletter"),
                () => config.Bard.EnableBloodletter,
                v => config.Bard.EnableBloodletter = v,
                null, save, actionId: BRDActions.Bloodletter.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableEmpyrealArrow, "Enable Empyreal Arrow"),
                () => config.Bard.EnableEmpyrealArrow,
                v => config.Bard.EnableEmpyrealArrow = v,
                null, save, actionId: BRDActions.EmpyrealArrow.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableSidewinder, "Enable Sidewinder"),
                () => config.Bard.EnableSidewinder,
                v => config.Bard.EnableSidewinder = v,
                null, save, actionId: BRDActions.Sidewinder.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableApexArrow, "Enable Apex Arrow"),
                () => config.Bard.EnableApexArrow,
                v => config.Bard.EnableApexArrow = v,
                null, save, actionId: BRDActions.ApexArrow.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableBlastArrow, "Enable Blast Arrow"),
                () => config.Bard.EnableBlastArrow,
                v => config.Bard.EnableBlastArrow = v,
                null, save, actionId: BRDActions.BlastArrow.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableResonantArrow, "Enable Resonant Arrow"),
                () => config.Bard.EnableResonantArrow,
                v => config.Bard.EnableResonantArrow = v,
                null, save, actionId: BRDActions.ResonantArrow.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableRadiantEncore, "Enable Radiant Encore"),
                () => config.Bard.EnableRadiantEncore,
                v => config.Bard.EnableRadiantEncore = v,
                null, save, actionId: BRDActions.RadiantEncore.ActionId);

            config.Bard.ApexArrowMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Bard.ApexArrowMinGauge, "Apex Arrow Min Gauge"),
                config.Bard.ApexArrowMinGauge, 20, 100,
                Loc.T(LocalizedStrings.Bard.ApexArrowMinGaugeDesc, "Minimum Soul Voice for Apex Arrow"), save, v => config.Bard.ApexArrowMinGauge = v);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableAoERotation, "Enable AoE Rotation"),
                () => config.Bard.EnableAoERotation,
                v => config.Bard.EnableAoERotation = v,
                Loc.T(LocalizedStrings.Bard.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.Bard.EnableAoERotation)
            {
                config.Bard.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.Bard.AoEMinTargets, "AoE Min Targets"),
                    config.Bard.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Bard.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save, v => config.Bard.AoEMinTargets = v);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawSongSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Bard.SongSection, "Songs"), "BRD"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableSongRotation, "Enable Song Rotation"),
                () => config.Bard.EnableSongRotation,
                v => config.Bard.EnableSongRotation = v,
                Loc.T(LocalizedStrings.Bard.EnableSongRotationDesc, "Automatically rotate through Wanderer's Minuet, Mage's Ballad, and Army's Paeon"), save);

            var songRotation = config.Bard.SongRotation;
            if (ConfigUIHelpers.EnumCombo(Loc.T(LocalizedStrings.Bard.SongRotation, "Song Rotation"), ref songRotation,
                Loc.T(LocalizedStrings.Bard.SongRotationDesc, "Preferred song rotation order"), save))
            {
                config.Bard.SongRotation = songRotation;
            }

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnablePitchPerfect, "Enable Pitch Perfect"),
                () => config.Bard.EnablePitchPerfect,
                v => config.Bard.EnablePitchPerfect = v,
                null, save, actionId: BRDActions.PitchPerfect.ActionId);

            config.Bard.PitchPerfectMinStacks = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Bard.PitchPerfectMinStacks, "Pitch Perfect Min Stacks"),
                config.Bard.PitchPerfectMinStacks, 1, 3,
                Loc.T(LocalizedStrings.Bard.PitchPerfectMinStacksDesc, "Minimum Repertoire for Pitch Perfect"), save, v => config.Bard.PitchPerfectMinStacks = v);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.UsePitchPerfectEarly, "Use Pitch Perfect Early"),
                () => config.Bard.UsePitchPerfectEarly,
                v => config.Bard.UsePitchPerfectEarly = v,
                Loc.T(LocalizedStrings.Bard.UsePitchPerfectEarlyDesc, "Use at 2 stacks if song is ending"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDotSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Bard.DotSection, "DoTs"), "BRD"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableCausticBite, "Enable Caustic Bite"),
                () => config.Bard.EnableCausticBite,
                v => config.Bard.EnableCausticBite = v,
                null, save, actionId: BRDActions.CausticBite.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableStormbite, "Enable Stormbite"),
                () => config.Bard.EnableStormbite,
                v => config.Bard.EnableStormbite = v,
                null, save, actionId: BRDActions.Stormbite.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableIronJaws, "Enable Iron Jaws"),
                () => config.Bard.EnableIronJaws,
                v => config.Bard.EnableIronJaws = v,
                null, save, actionId: BRDActions.IronJaws.ActionId);

            config.Bard.DotRefreshThreshold = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Bard.DotRefreshThreshold, "DoT Refresh Threshold"),
                config.Bard.DotRefreshThreshold, 0f, 15f, "%.0f s",
                Loc.T(LocalizedStrings.Bard.DotRefreshThresholdDesc, "Seconds remaining before refreshing DoTs"), save, v => config.Bard.DotRefreshThreshold = v);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.SpreadDots, "Spread DoTs"),
                () => config.Bard.SpreadDots,
                v => config.Bard.SpreadDots = v,
                Loc.T(LocalizedStrings.Bard.SpreadDotsDesc, "Apply DoTs to multiple targets"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Bard.BurstSection, "Burst Windows"), "BRD", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableRagingStrikes, "Enable Raging Strikes"),
                () => config.Bard.EnableRagingStrikes,
                v => config.Bard.EnableRagingStrikes = v,
                null, save, actionId: BRDActions.RagingStrikes.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableBarrage, "Enable Barrage"),
                () => config.Bard.EnableBarrage,
                v => config.Bard.EnableBarrage = v,
                null, save, actionId: BRDActions.Barrage.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableBattleVoice, "Enable Battle Voice"),
                () => config.Bard.EnableBattleVoice,
                v => config.Bard.EnableBattleVoice = v,
                null, save, actionId: BRDActions.BattleVoice.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableRadiantFinale, "Enable Radiant Finale"),
                () => config.Bard.EnableRadiantFinale,
                v => config.Bard.EnableRadiantFinale = v,
                null, save, actionId: BRDActions.RadiantFinale.ActionId);

            config.Bard.BuffHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Bard.BuffHoldTime, "Buff Hold Time"),
                config.Bard.BuffHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Bard.BuffHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save, v => config.Bard.BuffHoldTime = v);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableBurstPooling, "Enable Burst Pooling"),
                () => config.Bard.EnableBurstPooling,
                v => config.Bard.EnableBurstPooling = v,
                Loc.T(LocalizedStrings.Bard.EnableBurstPoolingDesc, "Pool Soul Voice gauge for burst windows"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

}
