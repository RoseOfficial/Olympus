using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Samurai.EnableIaijutsu, "Enable Iaijutsu"),
                () => config.Samurai.EnableIaijutsu,
                v => config.Samurai.EnableIaijutsu = v,
                Loc.T(LocalizedStrings.Samurai.EnableIaijutsuDesc, "Use Higanbana, Midare Setsugekka, etc."), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Samurai.EnableTsubamegaeshi, "Enable Tsubame-gaeshi"),
                () => config.Samurai.EnableTsubamegaeshi,
                v => config.Samurai.EnableTsubamegaeshi = v,
                null, save,
                actionId: SAMActions.TsubameGaeshi.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Samurai.EnableOgiNamikiri, "Enable Ogi Namikiri"),
                () => config.Samurai.EnableOgiNamikiri,
                v => config.Samurai.EnableOgiNamikiri = v,
                null, save,
                actionId: SAMActions.OgiNamikiri.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Samurai.EnableAoERotation, "Enable AoE Rotation"),
                () => config.Samurai.EnableAoERotation,
                v => config.Samurai.EnableAoERotation = v,
                Loc.T(LocalizedStrings.Samurai.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.Samurai.EnableAoERotation)
            {
                config.Samurai.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.Samurai.AoEMinTargets, "AoE Min Targets"),
                    config.Samurai.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Samurai.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawKenkiSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Samurai.KenkiSection, "Kenki Gauge"), "SAM"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Samurai.EnableShinten, "Enable Shinten"),
                () => config.Samurai.EnableShinten,
                v => config.Samurai.EnableShinten = v,
                null, save,
                actionId: SAMActions.Shinten.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Samurai.EnableSenei, "Enable Senei"),
                () => config.Samurai.EnableSenei,
                v => config.Samurai.EnableSenei = v,
                null, save,
                actionId: SAMActions.Senei.ActionId);

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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Samurai.MaintainHiganbana, "Maintain Higanbana"),
                () => config.Samurai.MaintainHiganbana,
                v => config.Samurai.MaintainHiganbana = v,
                Loc.T(LocalizedStrings.Samurai.MaintainHiganbanaDesc, "Keep Higanbana DoT active on target"), save);

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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Samurai.EnableIkishoten, "Enable Ikishoten"),
                () => config.Samurai.EnableIkishoten,
                v => config.Samurai.EnableIkishoten = v,
                null, save,
                actionId: SAMActions.Ikishoten.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Samurai.AlignWithParty, "Align with Party"),
                () => config.Samurai.AlignIkishotenWithParty,
                v => config.Samurai.AlignIkishotenWithParty = v,
                Loc.T(LocalizedStrings.Samurai.AlignWithPartyDesc, "Coordinate Ikishoten with party burst"), save);

            config.Samurai.IkishotenHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Samurai.IkishotenHoldTime, "Ikishoten Hold Time"),
                config.Samurai.IkishotenHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Samurai.IkishotenHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
