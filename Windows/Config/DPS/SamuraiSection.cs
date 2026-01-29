using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Samurai (Nike) settings section.
/// </summary>
public sealed class SamuraiSection
{
    private readonly Configuration config;
    private readonly Action save;

    public SamuraiSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Samurai", "Nike", ConfigUIHelpers.SamuraiColor);

        DrawDamageSection();
        DrawKenkiSection();
        DrawSenSection();
        DrawBurstSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Samurai.DamageSection, "Damage"), "SAM"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableIaijutsu = config.Samurai.EnableIaijutsu;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Samurai.EnableIaijutsu, "Enable Iaijutsu"), ref enableIaijutsu,
                Loc.T(LocalizedStrings.Samurai.EnableIaijutsuDesc, "Use Higanbana, Midare Setsugekka, etc."), save))
            {
                config.Samurai.EnableIaijutsu = enableIaijutsu;
            }

            var enableTsubamegaeshi = config.Samurai.EnableTsubamegaeshi;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Samurai.EnableTsubamegaeshi, "Enable Tsubame-gaeshi"), ref enableTsubamegaeshi,
                Loc.T(LocalizedStrings.Samurai.EnableTsubamegaeshiDesc, "Use Tsubame-gaeshi after Iaijutsu"), save))
            {
                config.Samurai.EnableTsubamegaeshi = enableTsubamegaeshi;
            }

            var enableOgiNamikiri = config.Samurai.EnableOgiNamikiri;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Samurai.EnableOgiNamikiri, "Enable Ogi Namikiri"), ref enableOgiNamikiri,
                Loc.T(LocalizedStrings.Samurai.EnableOgiNamikiriDesc, "Use Ogi Namikiri"), save))
            {
                config.Samurai.EnableOgiNamikiri = enableOgiNamikiri;
            }

            ConfigUIHelpers.Spacing();

            config.Samurai.AoEMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Samurai.AoEMinTargets, "AoE Min Targets"),
                config.Samurai.AoEMinTargets, 2, 8,
                Loc.T(LocalizedStrings.Samurai.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawKenkiSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Samurai.KenkiSection, "Kenki Gauge"), "SAM"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableShinten = config.Samurai.EnableShinten;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Samurai.EnableShinten, "Enable Shinten"), ref enableShinten,
                Loc.T(LocalizedStrings.Samurai.EnableShintenDesc, "Use Shinten (single-target Kenki spender)"), save))
            {
                config.Samurai.EnableShinten = enableShinten;
            }

            var enableSenei = config.Samurai.EnableSenei;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Samurai.EnableSenei, "Enable Senei"), ref enableSenei,
                Loc.T(LocalizedStrings.Samurai.EnableSeneiDesc, "Use Senei (high-damage Kenki spender)"), save))
            {
                config.Samurai.EnableSenei = enableSenei;
            }

            config.Samurai.KenkiMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Samurai.KenkiMinGauge, "Kenki Min Gauge"),
                config.Samurai.KenkiMinGauge, 25, 100,
                Loc.T(LocalizedStrings.Samurai.KenkiMinGaugeDesc, "Minimum Kenki to use spenders"), save);

            config.Samurai.KenkiOvercapThreshold = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Samurai.KenkiOvercapThreshold, "Kenki Overcap Threshold"),
                config.Samurai.KenkiOvercapThreshold, 25, 100,
                Loc.T(LocalizedStrings.Samurai.KenkiOvercapThresholdDesc, "Dump Kenki above this to avoid overcap"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawSenSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Samurai.SenSection, "Sen Management"), "SAM"))
        {
            ConfigUIHelpers.BeginIndent();

            var maintainHiganbana = config.Samurai.MaintainHiganbana;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Samurai.MaintainHiganbana, "Maintain Higanbana"), ref maintainHiganbana,
                Loc.T(LocalizedStrings.Samurai.MaintainHiganbanaDesc, "Keep Higanbana DoT active on target"), save))
            {
                config.Samurai.MaintainHiganbana = maintainHiganbana;
            }

            config.Samurai.HiganbanaRefreshThreshold = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Samurai.HiganbanaRefreshThreshold, "Higanbana Refresh"),
                config.Samurai.HiganbanaRefreshThreshold, 0f, 30f, "%.0f s",
                Loc.T(LocalizedStrings.Samurai.HiganbanaRefreshThresholdDesc, "Seconds remaining before refreshing"), save);

            config.Samurai.HiganbanaMinTargetHp = ConfigUIHelpers.ThresholdSlider(
                Loc.T(LocalizedStrings.Samurai.HiganbanaMinTargetHp, "Higanbana Min Target HP"),
                config.Samurai.HiganbanaMinTargetHp, 0f, 50f,
                Loc.T(LocalizedStrings.Samurai.HiganbanaMinTargetHpDesc, "Skip Higanbana on low HP targets"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Samurai.BurstSection, "Burst Windows"), "SAM", false))
        {
            ConfigUIHelpers.BeginIndent();

            var enableIkishoten = config.Samurai.EnableIkishoten;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Samurai.EnableIkishoten, "Enable Ikishoten"), ref enableIkishoten,
                Loc.T(LocalizedStrings.Samurai.EnableIkishotenDesc, "Use Ikishoten"), save))
            {
                config.Samurai.EnableIkishoten = enableIkishoten;
            }

            var alignWithParty = config.Samurai.AlignIkishotenWithParty;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Samurai.AlignWithParty, "Align with Party"), ref alignWithParty,
                Loc.T(LocalizedStrings.Samurai.AlignWithPartyDesc, "Coordinate Ikishoten with party burst"), save))
            {
                config.Samurai.AlignIkishotenWithParty = alignWithParty;
            }

            config.Samurai.IkishotenHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Samurai.IkishotenHoldTime, "Ikishoten Hold Time"),
                config.Samurai.IkishotenHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Samurai.IkishotenHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
