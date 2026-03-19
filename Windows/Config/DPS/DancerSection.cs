using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Dancer (Terpsichore) settings section.
/// </summary>
public sealed class DancerSection
{
    private readonly Configuration config;
    private readonly Action save;

    public DancerSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Dancer", "Terpsichore", ConfigUIHelpers.DancerColor);

        DrawDamageSection();
        DrawDanceSection();
        DrawGaugeSection();
        DrawBurstSection();
        DrawPartnerSection();
        DrawUtilitySection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dancer.DamageSection, "Damage"), "DNC"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableProcs, "Enable Procs"),
                () => config.Dancer.EnableProcs,
                v => config.Dancer.EnableProcs = v,
                Loc.T(LocalizedStrings.Dancer.EnableProcsDesc, "Use proc weaponskills"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableStarfallDance, "Enable Starfall Dance"),
                () => config.Dancer.EnableStarfallDance,
                v => config.Dancer.EnableStarfallDance = v,
                null, save, actionId: DNCActions.StarfallDance.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableTillana, "Enable Tillana"),
                () => config.Dancer.EnableTillana,
                v => config.Dancer.EnableTillana = v,
                null, save, actionId: DNCActions.Tillana.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableFinishingMove, "Enable Finishing Move"),
                () => config.Dancer.EnableFinishingMove,
                v => config.Dancer.EnableFinishingMove = v,
                null, save, actionId: DNCActions.FinishingMove.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableLastDance, "Enable Last Dance"),
                () => config.Dancer.EnableLastDance,
                v => config.Dancer.EnableLastDance = v,
                null, save, actionId: DNCActions.LastDance.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableFanDanceIV, "Enable Fan Dance IV"),
                () => config.Dancer.EnableFanDanceIV,
                v => config.Dancer.EnableFanDanceIV = v,
                null, save, actionId: DNCActions.FanDanceIV.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableAoERotation, "Enable AoE Rotation"),
                () => config.Dancer.EnableAoERotation,
                v => config.Dancer.EnableAoERotation = v,
                Loc.T(LocalizedStrings.Dancer.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.Dancer.EnableAoERotation)
            {
                config.Dancer.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.Dancer.AoEMinTargets, "AoE Min Targets"),
                    config.Dancer.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Dancer.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDanceSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dancer.DanceSection, "Dances"), "DNC"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableStandardStep, "Enable Standard Step"),
                () => config.Dancer.EnableStandardStep,
                v => config.Dancer.EnableStandardStep = v,
                null, save, actionId: DNCActions.StandardStep.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableTechnicalStep, "Enable Technical Step"),
                () => config.Dancer.EnableTechnicalStep,
                v => config.Dancer.EnableTechnicalStep = v,
                null, save, actionId: DNCActions.TechnicalStep.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.DelayStandardForTechnical, "Delay Standard for Technical"),
                () => config.Dancer.DelayStandardForTechnical,
                v => config.Dancer.DelayStandardForTechnical = v,
                Loc.T(LocalizedStrings.Dancer.DelayStandardForTechnicalDesc, "Hold Standard Step if Technical is coming soon"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawGaugeSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dancer.GaugeSection, "Esprit/Feathers"), "DNC"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableSaberDance, "Enable Saber Dance"),
                () => config.Dancer.EnableSaberDance,
                v => config.Dancer.EnableSaberDance = v,
                null, save, actionId: DNCActions.SaberDance.ActionId);

            config.Dancer.SaberDanceMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Dancer.SaberDanceMinGauge, "Saber Dance Min Gauge"),
                config.Dancer.SaberDanceMinGauge, 50, 100,
                Loc.T(LocalizedStrings.Dancer.SaberDanceMinGaugeDesc, "Minimum Esprit for Saber Dance"), save);

            config.Dancer.EspritOvercapThreshold = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Dancer.EspritOvercapThreshold, "Esprit Overcap Threshold"),
                config.Dancer.EspritOvercapThreshold, 50, 100,
                Loc.T(LocalizedStrings.Dancer.EspritOvercapThresholdDesc, "Use Saber Dance above this Esprit to avoid overcap"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.SaveEspritForBurst, "Save Esprit for Burst"),
                () => config.Dancer.SaveEspritForBurst,
                v => config.Dancer.SaveEspritForBurst = v,
                Loc.T(LocalizedStrings.Dancer.SaveEspritForBurstDesc, "Hold Esprit gauge for burst windows"), save);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableFanDance, "Enable Fan Dance"),
                () => config.Dancer.EnableFanDance,
                v => config.Dancer.EnableFanDance = v,
                Loc.T(LocalizedStrings.Dancer.EnableFanDanceDesc, "Use Fan Dance abilities"), save);

            config.Dancer.FanDanceMinFeathers = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Dancer.FanDanceMinFeathers, "Fan Dance Min Feathers"),
                config.Dancer.FanDanceMinFeathers, 1, 4,
                Loc.T(LocalizedStrings.Dancer.FanDanceMinFeathersDesc, "Minimum Feathers for Fan Dance"), save);

            config.Dancer.FeatherOvercapThreshold = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Dancer.FeatherOvercapThreshold, "Feather Overcap Threshold"),
                config.Dancer.FeatherOvercapThreshold, 1, 4,
                Loc.T(LocalizedStrings.Dancer.FeatherOvercapThresholdDesc, "Use Fan Dance above this count to avoid overcap"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.SaveFeathersForBurst, "Save Feathers for Burst"),
                () => config.Dancer.SaveFeathersForBurst,
                v => config.Dancer.SaveFeathersForBurst = v,
                Loc.T(LocalizedStrings.Dancer.SaveFeathersForBurstDesc, "Hold Feathers for burst windows"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dancer.BurstSection, "Burst Windows"), "DNC", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableDevilment, "Enable Devilment"),
                () => config.Dancer.EnableDevilment,
                v => config.Dancer.EnableDevilment = v,
                null, save, actionId: DNCActions.Devilment.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableFlourish, "Enable Flourish"),
                () => config.Dancer.EnableFlourish,
                v => config.Dancer.EnableFlourish = v,
                null, save, actionId: DNCActions.Flourish.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableDevilmentAfterTechnical, "Enable Devilment After Technical"),
                () => config.Dancer.UseDevilmentAfterTechnical,
                v => config.Dancer.UseDevilmentAfterTechnical = v,
                Loc.T(LocalizedStrings.Dancer.EnableDevilmentAfterTechnicalDesc, "Delay Devilment to use inside Technical Finish window"), save);

            config.Dancer.TechnicalHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Dancer.TechnicalHoldTime, "Technical Hold Time"),
                config.Dancer.TechnicalHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Dancer.TechnicalHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableBurstPooling, "Enable Burst Pooling"),
                () => config.Dancer.EnableBurstPooling,
                v => config.Dancer.EnableBurstPooling = v,
                Loc.T(LocalizedStrings.Dancer.EnableBurstPoolingDesc, "Pool Esprit and Feathers for party burst windows"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawPartnerSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dancer.PartnerSection, "Dance Partner"), "DNC", false))
        {
            ConfigUIHelpers.BeginIndent();

            var partnerMode = config.Dancer.PartnerSelectionMode;
            if (ConfigUIHelpers.EnumCombo(Loc.T(LocalizedStrings.Dancer.PartnerSelectionMode, "Partner Selection Mode"), ref partnerMode,
                Loc.T(LocalizedStrings.Dancer.PartnerSelectionModeDesc, "How to choose your Dance Partner"), save))
            {
                config.Dancer.PartnerSelectionMode = partnerMode;
            }

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.AutoRepartner, "Auto Repartner"),
                () => config.Dancer.AutoRepartner,
                v => config.Dancer.AutoRepartner = v,
                Loc.T(LocalizedStrings.Dancer.AutoRepartnerDesc, "Automatically re-assign Dance Partner when your partner dies or leaves"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawUtilitySection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Dancer.UtilitySection, "Utility"), "DNC", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableHeadGraze, "Enable Head Graze"),
                () => config.Dancer.EnableHeadGraze,
                v => config.Dancer.EnableHeadGraze = v,
                Loc.T(LocalizedStrings.Dancer.EnableHeadGrazeDesc, "Use Head Graze for interrupts"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableCuringWaltz, "Enable Curing Waltz"),
                () => config.Dancer.EnableCuringWaltz,
                v => config.Dancer.EnableCuringWaltz = v,
                Loc.T(LocalizedStrings.Dancer.EnableCuringWaltzDesc, "Use Curing Waltz for party healing support"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Dancer.EnableShieldSamba, "Enable Shield Samba"),
                () => config.Dancer.EnableShieldSamba,
                v => config.Dancer.EnableShieldSamba = v,
                Loc.T(LocalizedStrings.Dancer.EnableShieldSambaDesc, "Use Shield Samba to mitigate incoming damage"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
