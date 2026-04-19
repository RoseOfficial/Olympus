using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.Healers;

/// <summary>
/// Renders settings that apply to all healer jobs (WHM/SCH/AST/SGE).
/// </summary>
public sealed class HealerSharedSection
{
    private readonly Configuration config;
    private readonly Action save;

    public HealerSharedSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.9f, 0.8f, 1f),
            Loc.T(LocalizedStrings.HealerShared.Header, "Shared Healer Settings"));
        ImGui.TextDisabled(Loc.T(LocalizedStrings.HealerShared.Description,
            "These settings apply to all healer jobs."));
        ConfigUIHelpers.Spacing();

        DrawMpManagement();
        DrawPredictionAndAwareness();
        DrawTimelineIntegration();
    }

    private void DrawMpManagement()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.HealerShared.MpManagement, "MP Management"), "Healer"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.HealerShared.EnableLucidDreaming, "Enable Lucid Dreaming"),
                () => config.HealerShared.EnableLucidDreaming,
                v => config.HealerShared.EnableLucidDreaming = v,
                null, save,
                actionId: RoleActions.LucidDreaming.ActionId);

            if (config.HealerShared.EnableLucidDreaming)
            {
                config.HealerShared.LucidDreamingThreshold = ConfigUIHelpers.ThresholdSlider(
                    Loc.T(LocalizedStrings.HealerShared.LucidMpThreshold, "Lucid MP Threshold"),
                    config.HealerShared.LucidDreamingThreshold, 40f, 90f,
                    Loc.T(LocalizedStrings.HealerShared.LucidMpThresholdDesc,
                        "Fire Lucid Dreaming when MP drops below this percentage. White Mage uses its own predictive MP forecast and ignores this slider."),
                    save,
                    v => config.HealerShared.LucidDreamingThreshold = v);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawPredictionAndAwareness()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.HealerShared.PredictionSection, "Prediction & Awareness"), "Healer"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.HealerShared.EnableMechanicAwareness, "Enable Mechanic Awareness"),
                () => config.Healing.EnableMechanicAwareness,
                v => config.Healing.EnableMechanicAwareness = v,
                Loc.T(LocalizedStrings.HealerShared.EnableMechanicAwarenessDesc,
                    "Detect raidwide and tank buster patterns from damage intake to pre-arm shields and mitigation."),
                save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.HealerShared.EnableCritVarianceReduction, "Account for Crit Variance"),
                () => config.Healing.EnableCritVarianceReduction,
                v => config.Healing.EnableCritVarianceReduction = v,
                Loc.T(LocalizedStrings.HealerShared.EnableCritVarianceReductionDesc,
                    "Discount pending heals by expected crit-roll variance so overheal prediction stays conservative."),
                save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.HealerShared.EnableSurvivabilityTrending, "Weight Survivability Trend"),
                () => config.Healing.EnableSurvivabilityTrending,
                v => config.Healing.EnableSurvivabilityTrending = v,
                Loc.T(LocalizedStrings.HealerShared.EnableSurvivabilityTrendingDesc,
                    "Favor stronger heals when party HP is trending down rapidly (scored selection only)."),
                save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawTimelineIntegration()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.HealerShared.TimelineSection, "Timeline Integration"), "Healer"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.HealerShared.TimelineMasterMoved,
                "Timeline master toggles now live in Shared > Timeline Integration."));
            ImGui.Spacing();

            if (config.Timeline.EnableTimelinePredictions)
            {
                config.Healing.RaidwidePreparationWindow = ConfigUIHelpers.FloatSlider(
                    Loc.T(LocalizedStrings.HealerShared.RaidwideWindow, "Raidwide Window (sec)"),
                    config.Healing.RaidwidePreparationWindow, 2f, 10f, "%.1f",
                    "Seconds before a predicted raidwide to start preparing shields and mitigation.",
                    save,
                    v => config.Healing.RaidwidePreparationWindow = v);

                config.Healing.TankBusterPreparationWindow = ConfigUIHelpers.FloatSlider(
                    Loc.T(LocalizedStrings.HealerShared.TankBusterWindow, "Tank Buster Window (sec)"),
                    config.Healing.TankBusterPreparationWindow, 1f, 6f, "%.1f",
                    "Seconds before a predicted tank buster to pre-arm single-target mitigation.",
                    save,
                    v => config.Healing.TankBusterPreparationWindow = v);
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
