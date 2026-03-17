using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.Healers;

/// <summary>
/// Renders the Scholar (Athena) settings section.
/// </summary>
public sealed class ScholarSection
{
    private readonly Configuration config;
    private readonly Action save;

    public ScholarSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Scholar", "Athena", ConfigUIHelpers.ScholarColor);

        DrawHealingSection();
        DrawFairySection();
        DrawShieldSection();
        DrawAetherflowSection();
        DrawDamageSection();
    }

    private void DrawHealingSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Scholar.HealingSection, "Healing"), "SCH"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.GcdHeals, "GCD Heals:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnablePhysick, "Enable Physick"), () => config.Scholar.EnablePhysick, v => config.Scholar.EnablePhysick = v, null, save,
                actionId: SCHActions.Physick.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableAdloquium, "Enable Adloquium"), () => config.Scholar.EnableAdloquium, v => config.Scholar.EnableAdloquium = v, null, save,
                actionId: SCHActions.Adloquium.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableSuccor, "Enable Succor"), () => config.Scholar.EnableSuccor, v => config.Scholar.EnableSuccor = v, null, save,
                actionId: SCHActions.Succor.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.OgcdHeals, "oGCD Heals:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableLustrate, "Enable Lustrate"), () => config.Scholar.EnableLustrate, v => config.Scholar.EnableLustrate = v, null, save,
                actionId: SCHActions.Lustrate.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableExcogitation, "Enable Excogitation"), () => config.Scholar.EnableExcogitation, v => config.Scholar.EnableExcogitation = v, null, save,
                actionId: SCHActions.Excogitation.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableIndomitability, "Enable Indomitability"), () => config.Scholar.EnableIndomitability, v => config.Scholar.EnableIndomitability = v, null, save,
                actionId: SCHActions.Indomitability.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableProtraction, "Enable Protraction"), () => config.Scholar.EnableProtraction, v => config.Scholar.EnableProtraction = v, null, save,
                actionId: SCHActions.Protraction.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableRecitation, "Enable Recitation"), () => config.Scholar.EnableRecitation, v => config.Scholar.EnableRecitation = v, null, save,
                actionId: SCHActions.Recitation.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.SingleTargetThresholds, "Single-Target Thresholds:"));

            config.Scholar.PhysickThreshold = ConfigUIHelpers.ThresholdSlider("Physick",
                config.Scholar.PhysickThreshold, 20f, 80f, null, save);
            config.Scholar.AdloquiumThreshold = ConfigUIHelpers.ThresholdSlider("Adloquium",
                config.Scholar.AdloquiumThreshold, 40f, 90f, null, save);
            config.Scholar.LustrateThreshold = ConfigUIHelpers.ThresholdSlider("Lustrate",
                config.Scholar.LustrateThreshold, 30f, 80f, null, save);
            config.Scholar.ExcogitationThreshold = ConfigUIHelpers.ThresholdSlider("Excogitation",
                config.Scholar.ExcogitationThreshold, 60f, 95f,
                Loc.T(LocalizedStrings.Scholar.ExcogitationDesc, "Apply Excogitation proactively at this HP%."), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.AoEHealing, "AoE Healing:"));

            config.Scholar.AoEHealThreshold = ConfigUIHelpers.ThresholdSlider("AoE HP Threshold",
                config.Scholar.AoEHealThreshold, 50f, 90f, null, save);

            config.Scholar.AoEHealMinTargets = ConfigUIHelpers.IntSlider("AoE Min Targets##SCH",
                config.Scholar.AoEHealMinTargets, 2, 8, null, save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.RecitationPriorityLabel, "Recitation Priority:"));

            var recitationNames = Enum.GetNames<RecitationPriority>();
            var currentRecitation = (int)config.Scholar.RecitationPriority;
            ImGui.SetNextItemWidth(180);
            if (ImGui.Combo(Loc.T(LocalizedStrings.Scholar.RecitationTarget, "Recitation Target"), ref currentRecitation, recitationNames, recitationNames.Length))
            {
                config.Scholar.RecitationPriority = (RecitationPriority)currentRecitation;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.RecitationTargetDesc, "Which ability to use with Recitation (guaranteed crit, free)."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.SacredSoilLabel, "Sacred Soil:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableSacredSoil, "Enable Sacred Soil"), () => config.Scholar.EnableSacredSoil, v => config.Scholar.EnableSacredSoil = v, null, save,
                actionId: SCHActions.SacredSoil.ActionId);

            if (config.Scholar.EnableSacredSoil)
            {
                ConfigUIHelpers.BeginIndent();
                config.Scholar.SacredSoilThreshold = ConfigUIHelpers.ThresholdSliderSmall("Soil HP Threshold",
                    config.Scholar.SacredSoilThreshold, 50f, 90f, null, save);

                config.Scholar.SacredSoilMinTargets = ConfigUIHelpers.IntSliderSmall("Soil Min Targets",
                    config.Scholar.SacredSoilMinTargets, 2, 8, null, save);
                ConfigUIHelpers.EndIndent();
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawFairySection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Scholar.FairySection, "Fairy"), "SCH"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.AutoSummonFairy, "Auto-Summon Fairy"), () => config.Scholar.AutoSummonFairy, v => config.Scholar.AutoSummonFairy = v,
                Loc.T(LocalizedStrings.Scholar.AutoSummonFairyDesc, "Automatically summon Eos if not present."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableFairyAbilities, "Enable Fairy Abilities"), () => config.Scholar.EnableFairyAbilities, v => config.Scholar.EnableFairyAbilities = v,
                Loc.T(LocalizedStrings.Scholar.EnableFairyAbilitiesDesc, "Automatically use Whispering Dawn, Fey Blessing, etc."), save);

            ConfigUIHelpers.BeginDisabledGroup(!config.Scholar.EnableFairyAbilities);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.WhisperingDawnLabel, "Whispering Dawn:"));

            config.Scholar.WhisperingDawnThreshold = ConfigUIHelpers.ThresholdSlider("WD HP Threshold",
                config.Scholar.WhisperingDawnThreshold, 50f, 95f, null, save);

            config.Scholar.WhisperingDawnMinTargets = ConfigUIHelpers.IntSlider("WD Min Targets",
                config.Scholar.WhisperingDawnMinTargets, 1, 8, null, save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.FeyBlessingLabel, "Fey Blessing:"));

            config.Scholar.FeyBlessingThreshold = ConfigUIHelpers.ThresholdSlider("FB HP Threshold",
                config.Scholar.FeyBlessingThreshold, 50f, 90f, null, save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.FeyUnionLabel, "Fey Union:"));

            config.Scholar.FeyUnionThreshold = ConfigUIHelpers.ThresholdSlider("FU HP Threshold",
                config.Scholar.FeyUnionThreshold, 40f, 80f, null, save);

            config.Scholar.FeyUnionMinGauge = ConfigUIHelpers.IntSlider("FU Min Gauge",
                config.Scholar.FeyUnionMinGauge, 10, 100, null, save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.SeraphLabel, "Seraph:"));

            var seraphNames = Enum.GetNames<SeraphUsageStrategy>();
            var currentSeraph = (int)config.Scholar.SeraphStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo(Loc.T(LocalizedStrings.Scholar.SeraphStrategy, "Seraph Strategy"), ref currentSeraph, seraphNames, seraphNames.Length))
            {
                config.Scholar.SeraphStrategy = (SeraphUsageStrategy)currentSeraph;
                save();
            }

            config.Scholar.SeraphPartyHpThreshold = ConfigUIHelpers.ThresholdSlider("Seraph HP Trigger",
                config.Scholar.SeraphPartyHpThreshold, 50f, 90f, null, save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableConsolation, "Enable Consolation"), () => config.Scholar.EnableConsolation, v => config.Scholar.EnableConsolation = v,
                null, save, actionId: SCHActions.Consolation.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.SeraphismLabel, "Seraphism (Lv100):"));

            var seraphismNames = Enum.GetNames<SeraphismUsageStrategy>();
            var currentSeraphism = (int)config.Scholar.SeraphismStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo(Loc.T(LocalizedStrings.Scholar.SeraphismStrategy, "Seraphism Strategy"), ref currentSeraphism, seraphismNames, seraphismNames.Length))
            {
                config.Scholar.SeraphismStrategy = (SeraphismUsageStrategy)currentSeraphism;
                save();
            }

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawShieldSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Scholar.ShieldsSection, "Shields"), "SCH"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EmergencyTactics, "Emergency Tactics"), () => config.Scholar.EnableEmergencyTactics, v => config.Scholar.EnableEmergencyTactics = v,
                Loc.T(LocalizedStrings.Scholar.EmergencyTacticsDesc, "Convert next shield to direct healing."), save,
                actionId: SCHActions.EmergencyTactics.ActionId);

            if (config.Scholar.EnableEmergencyTactics)
            {
                config.Scholar.EmergencyTacticsThreshold = ConfigUIHelpers.ThresholdSlider("ET HP Threshold",
                    config.Scholar.EmergencyTacticsThreshold, 20f, 60f, null, save);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.DeploymentTactics, "Deployment Tactics"), () => config.Scholar.EnableDeploymentTactics, v => config.Scholar.EnableDeploymentTactics = v,
                Loc.T(LocalizedStrings.Scholar.DeploymentTacticsDesc, "Spread Galvanize shield to party."), save,
                actionId: SCHActions.DeploymentTactics.ActionId);

            if (config.Scholar.EnableDeploymentTactics)
            {
                config.Scholar.DeploymentMinTargets = ConfigUIHelpers.IntSlider("Deploy Min Targets",
                    config.Scholar.DeploymentMinTargets, 2, 8, null, save);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.AvoidSageShields, "Avoid Sage Shield Overwrite"), () => config.Scholar.AvoidOverwritingSageShields, v => config.Scholar.AvoidOverwritingSageShields = v,
                Loc.T(LocalizedStrings.Scholar.AvoidSageShieldsDesc, "Don't apply Galvanize if target has Sage shields."), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.ExpedientLabel, "Expedient:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableExpedient, "Enable Expedient"), () => config.Scholar.EnableExpedient, v => config.Scholar.EnableExpedient = v, null, save,
                actionId: SCHActions.Expedient.ActionId);

            if (config.Scholar.EnableExpedient)
            {
                config.Scholar.ExpedientThreshold = ConfigUIHelpers.ThresholdSlider("Expedient HP Trigger",
                    config.Scholar.ExpedientThreshold, 40f, 80f, null, save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawAetherflowSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Scholar.AetherflowSection, "Aetherflow"), "SCH"))
        {
            ConfigUIHelpers.BeginIndent();

            var strategyNames = Enum.GetNames<AetherflowUsageStrategy>();
            var currentStrategy = (int)config.Scholar.AetherflowStrategy;
            ImGui.SetNextItemWidth(180);
            if (ImGui.Combo(Loc.T(LocalizedStrings.Scholar.AetherflowStrategy, "Aetherflow Strategy"), ref currentStrategy, strategyNames, strategyNames.Length))
            {
                config.Scholar.AetherflowStrategy = (AetherflowUsageStrategy)currentStrategy;
                save();
            }

            var strategyDesc = config.Scholar.AetherflowStrategy switch
            {
                AetherflowUsageStrategy.Balanced => Loc.T(LocalizedStrings.Scholar.StrategyBalanced, "Balance healing and Energy Drain"),
                AetherflowUsageStrategy.HealingPriority => Loc.T(LocalizedStrings.Scholar.StrategyHealingPriority, "Prioritize healing, minimal DPS"),
                AetherflowUsageStrategy.AggressiveDps => Loc.T(LocalizedStrings.Scholar.StrategyAggressiveDps, "Aggressive Energy Drain when safe"),
                _ => ""
            };
            ImGui.TextDisabled(strategyDesc);

            ConfigUIHelpers.Spacing();

            config.Scholar.AetherflowReserve = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Scholar.StackReserve, "Stack Reserve"),
                config.Scholar.AetherflowReserve, 0, 3, Loc.T(LocalizedStrings.Scholar.StackReserveDesc, "Stacks to keep for emergency healing."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableEnergyDrain, "Enable Energy Drain"), () => config.Scholar.EnableEnergyDrain, v => config.Scholar.EnableEnergyDrain = v, null, save,
                actionId: SCHActions.EnergyDrain.ActionId);

            config.Scholar.AetherflowDumpWindow = ConfigUIHelpers.FloatSlider(Loc.T(LocalizedStrings.Scholar.DumpWindow, "Dump Window (sec)"),
                config.Scholar.AetherflowDumpWindow, 0f, 15f, "%.1f",
                Loc.T(LocalizedStrings.Scholar.DumpWindowDesc, "Start dumping stacks when Aetherflow CD is below this."), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.DissipationLabel, "Dissipation:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableDissipation, "Enable Dissipation"), () => config.Scholar.EnableDissipation, v => config.Scholar.EnableDissipation = v,
                Loc.T(LocalizedStrings.Scholar.DissipationDesc, "Sacrifice fairy for 3 Aetherflow + 20% heal boost."), save,
                actionId: SCHActions.Dissipation.ActionId);

            if (config.Scholar.EnableDissipation)
            {
                ConfigUIHelpers.BeginIndent();
                config.Scholar.DissipationMaxFairyGauge = ConfigUIHelpers.IntSliderSmall(Loc.T(LocalizedStrings.Scholar.MaxFairyGauge, "Max Fairy Gauge"),
                    config.Scholar.DissipationMaxFairyGauge, 0, 100,
                    Loc.T(LocalizedStrings.Scholar.MaxFairyGaugeDesc, "Only use when gauge is below this (avoid waste)."), save);

                config.Scholar.DissipationSafePartyHp = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Scholar.SafePartyHp, "Safe Party HP"),
                    config.Scholar.DissipationSafePartyHp, 60f, 95f, Loc.T(LocalizedStrings.Scholar.SafePartyHpDesc, "Only use when party HP is above this."), save);
                ConfigUIHelpers.EndIndent();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.MpManagement, "MP Management:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableLucidDreaming, "Enable Lucid Dreaming"), () => config.Scholar.EnableLucidDreaming, v => config.Scholar.EnableLucidDreaming = v, null, save);

            if (config.Scholar.EnableLucidDreaming)
            {
                config.Scholar.LucidDreamingThreshold = ConfigUIHelpers.ThresholdSlider("Lucid MP Threshold",
                    config.Scholar.LucidDreamingThreshold, 40f, 90f, null, save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Scholar.DamageSection, "Damage"), "SCH"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.SingleTargetDamage, "Single-Target Damage:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableBroilRuin, "Enable Broil/Ruin"), () => config.Scholar.EnableSingleTargetDamage, v => config.Scholar.EnableSingleTargetDamage = v,
                Loc.T(LocalizedStrings.Scholar.BroilRuinDesc, "Casted single-target damage spells."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableRuinII, "Enable Ruin II"), () => config.Scholar.EnableRuinII, v => config.Scholar.EnableRuinII = v,
                Loc.T(LocalizedStrings.Scholar.RuinIIDesc, "Instant damage while moving."), save,
                actionId: SCHActions.RuinII.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.DotLabel, "DoT:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableBioBiolysis, "Enable Bio/Biolysis"), () => config.Scholar.EnableDot, v => config.Scholar.EnableDot = v, null, save);

            if (config.Scholar.EnableDot)
            {
                config.Scholar.DotRefreshThreshold = ConfigUIHelpers.FloatSlider("DoT Refresh (sec)",
                    config.Scholar.DotRefreshThreshold, 0f, 10f, "%.1f", null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.AoEDamageLabel, "AoE Damage:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableArtOfWar, "Enable Art of War"), () => config.Scholar.EnableAoEDamage, v => config.Scholar.EnableAoEDamage = v, null, save);

            if (config.Scholar.EnableAoEDamage)
            {
                config.Scholar.AoEDamageMinTargets = ConfigUIHelpers.IntSlider("Art of War Min Enemies",
                    config.Scholar.AoEDamageMinTargets, 2, 10, null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.AetherflowLabel, "Aetherflow:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableAetherflow, "Enable Aetherflow"), () => config.Scholar.EnableAetherflow, v => config.Scholar.EnableAetherflow = v,
                Loc.T(LocalizedStrings.Scholar.AetherflowDesc, "Use Aetherflow when stacks are empty."), save,
                actionId: SCHActions.Aetherflow.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.RaidBuffLabel, "Raid Buff:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableChainStratagem, "Enable Chain Stratagem"), () => config.Scholar.EnableChainStratagem, v => config.Scholar.EnableChainStratagem = v,
                Loc.T(LocalizedStrings.Scholar.ChainStratagemDesc, "+10% crit rate on target for party."), save,
                actionId: SCHActions.ChainStratagem.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Scholar.EnableBanefulImpaction, "Enable Baneful Impaction"), () => config.Scholar.EnableBanefulImpaction, v => config.Scholar.EnableBanefulImpaction = v,
                Loc.T(LocalizedStrings.Scholar.BanefulImpactionDesc, "AoE follow-up when Impact Imminent is active."), save,
                actionId: SCHActions.BanefulImpaction.ActionId);

            ConfigUIHelpers.EndIndent();
        }
    }
}
