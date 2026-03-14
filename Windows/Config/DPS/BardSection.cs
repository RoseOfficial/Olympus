using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
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
                Loc.T(LocalizedStrings.Bard.EnableApexArrow, "Enable Apex Arrow"),
                () => config.Bard.EnableApexArrow,
                v => config.Bard.EnableApexArrow = v,
                Loc.T(LocalizedStrings.Bard.EnableApexArrowDesc, "Use Apex Arrow"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableBlastArrow, "Enable Blast Arrow"),
                () => config.Bard.EnableBlastArrow,
                v => config.Bard.EnableBlastArrow = v,
                Loc.T(LocalizedStrings.Bard.EnableBlastArrowDesc, "Use Blast Arrow"), save);

            config.Bard.ApexArrowMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Bard.ApexArrowMinGauge, "Apex Arrow Min Gauge"),
                config.Bard.ApexArrowMinGauge, 20, 100,
                Loc.T(LocalizedStrings.Bard.ApexArrowMinGaugeDesc, "Minimum Soul Voice for Apex Arrow"), save);

            ConfigUIHelpers.Spacing();

            config.Bard.AoEMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Bard.AoEMinTargets, "AoE Min Targets"),
                config.Bard.AoEMinTargets, 2, 8,
                Loc.T(LocalizedStrings.Bard.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawSongSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Bard.SongSection, "Songs"), "BRD"))
        {
            ConfigUIHelpers.BeginIndent();

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
                Loc.T(LocalizedStrings.Bard.EnablePitchPerfectDesc, "Use Pitch Perfect during Wanderer's Minuet"), save);

            config.Bard.PitchPerfectMinStacks = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Bard.PitchPerfectMinStacks, "Pitch Perfect Min Stacks"),
                config.Bard.PitchPerfectMinStacks, 1, 3,
                Loc.T(LocalizedStrings.Bard.PitchPerfectMinStacksDesc, "Minimum Repertoire for Pitch Perfect"), save);

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
                Loc.T(LocalizedStrings.Bard.EnableCausticBiteDesc, "Maintain Caustic Bite DoT"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableStormbite, "Enable Stormbite"),
                () => config.Bard.EnableStormbite,
                v => config.Bard.EnableStormbite = v,
                Loc.T(LocalizedStrings.Bard.EnableStormBiteDesc, "Maintain Stormbite DoT"), save);

            config.Bard.DotRefreshThreshold = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Bard.DotRefreshThreshold, "DoT Refresh Threshold"),
                config.Bard.DotRefreshThreshold, 0f, 15f, "%.0f s",
                Loc.T(LocalizedStrings.Bard.DotRefreshThresholdDesc, "Seconds remaining before refreshing DoTs"), save);

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
                Loc.T(LocalizedStrings.Bard.EnableBattleVoice, "Enable Battle Voice"),
                () => config.Bard.EnableBattleVoice,
                v => config.Bard.EnableBattleVoice = v,
                Loc.T(LocalizedStrings.Bard.EnableBattleVoiceDesc, "Use Battle Voice (party buff)"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.EnableRadiantFinale, "Enable Radiant Finale"),
                () => config.Bard.EnableRadiantFinale,
                v => config.Bard.EnableRadiantFinale = v,
                Loc.T(LocalizedStrings.Bard.EnableRadiantFinaleDesc, "Use Radiant Finale (party buff)"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Bard.AlignWithParty, "Align with Party"),
                () => config.Bard.AlignBuffsWithParty,
                v => config.Bard.AlignBuffsWithParty = v,
                Loc.T(LocalizedStrings.Bard.AlignWithPartyDesc, "Coordinate buffs with party burst"), save);

            config.Bard.BuffHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Bard.BuffHoldTime, "Buff Hold Time"),
                config.Bard.BuffHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Bard.BuffHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
