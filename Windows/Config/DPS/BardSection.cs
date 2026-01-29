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

            var enableApexArrow = config.Bard.EnableApexArrow;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Bard.EnableApexArrow, "Enable Apex Arrow"), ref enableApexArrow,
                Loc.T(LocalizedStrings.Bard.EnableApexArrowDesc, "Use Apex Arrow"), save))
            {
                config.Bard.EnableApexArrow = enableApexArrow;
            }

            var enableBlastArrow = config.Bard.EnableBlastArrow;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Bard.EnableBlastArrow, "Enable Blast Arrow"), ref enableBlastArrow,
                Loc.T(LocalizedStrings.Bard.EnableBlastArrowDesc, "Use Blast Arrow"), save))
            {
                config.Bard.EnableBlastArrow = enableBlastArrow;
            }

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

            var enablePitchPerfect = config.Bard.EnablePitchPerfect;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Bard.EnablePitchPerfect, "Enable Pitch Perfect"), ref enablePitchPerfect,
                Loc.T(LocalizedStrings.Bard.EnablePitchPerfectDesc, "Use Pitch Perfect during Wanderer's Minuet"), save))
            {
                config.Bard.EnablePitchPerfect = enablePitchPerfect;
            }

            config.Bard.PitchPerfectMinStacks = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Bard.PitchPerfectMinStacks, "Pitch Perfect Min Stacks"),
                config.Bard.PitchPerfectMinStacks, 1, 3,
                Loc.T(LocalizedStrings.Bard.PitchPerfectMinStacksDesc, "Minimum Repertoire for Pitch Perfect"), save);

            var usePitchPerfectEarly = config.Bard.UsePitchPerfectEarly;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Bard.UsePitchPerfectEarly, "Use Pitch Perfect Early"), ref usePitchPerfectEarly,
                Loc.T(LocalizedStrings.Bard.UsePitchPerfectEarlyDesc, "Use at 2 stacks if song is ending"), save))
            {
                config.Bard.UsePitchPerfectEarly = usePitchPerfectEarly;
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDotSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Bard.DotSection, "DoTs"), "BRD"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableCausticBite = config.Bard.EnableCausticBite;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Bard.EnableCausticBite, "Enable Caustic Bite"), ref enableCausticBite,
                Loc.T(LocalizedStrings.Bard.EnableCausticBiteDesc, "Maintain Caustic Bite DoT"), save))
            {
                config.Bard.EnableCausticBite = enableCausticBite;
            }

            var enableStormbite = config.Bard.EnableStormbite;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Bard.EnableStormbite, "Enable Stormbite"), ref enableStormbite,
                Loc.T(LocalizedStrings.Bard.EnableStormBiteDesc, "Maintain Stormbite DoT"), save))
            {
                config.Bard.EnableStormbite = enableStormbite;
            }

            config.Bard.DotRefreshThreshold = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Bard.DotRefreshThreshold, "DoT Refresh Threshold"),
                config.Bard.DotRefreshThreshold, 0f, 15f, "%.0f s",
                Loc.T(LocalizedStrings.Bard.DotRefreshThresholdDesc, "Seconds remaining before refreshing DoTs"), save);

            var spreadDots = config.Bard.SpreadDots;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Bard.SpreadDots, "Spread DoTs"), ref spreadDots,
                Loc.T(LocalizedStrings.Bard.SpreadDotsDesc, "Apply DoTs to multiple targets"), save))
            {
                config.Bard.SpreadDots = spreadDots;
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Bard.BurstSection, "Burst Windows"), "BRD", false))
        {
            ConfigUIHelpers.BeginIndent();

            var enableBattleVoice = config.Bard.EnableBattleVoice;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Bard.EnableBattleVoice, "Enable Battle Voice"), ref enableBattleVoice,
                Loc.T(LocalizedStrings.Bard.EnableBattleVoiceDesc, "Use Battle Voice (party buff)"), save))
            {
                config.Bard.EnableBattleVoice = enableBattleVoice;
            }

            var enableRadiantFinale = config.Bard.EnableRadiantFinale;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Bard.EnableRadiantFinale, "Enable Radiant Finale"), ref enableRadiantFinale,
                Loc.T(LocalizedStrings.Bard.EnableRadiantFinaleDesc, "Use Radiant Finale (party buff)"), save))
            {
                config.Bard.EnableRadiantFinale = enableRadiantFinale;
            }

            var alignWithParty = config.Bard.AlignBuffsWithParty;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Bard.AlignWithParty, "Align with Party"), ref alignWithParty,
                Loc.T(LocalizedStrings.Bard.AlignWithPartyDesc, "Coordinate buffs with party burst"), save))
            {
                config.Bard.AlignBuffsWithParty = alignWithParty;
            }

            config.Bard.BuffHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Bard.BuffHoldTime, "Buff Hold Time"),
                config.Bard.BuffHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Bard.BuffHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
