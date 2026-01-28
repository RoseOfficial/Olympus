using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
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

            var enablePhysick = config.Scholar.EnablePhysick;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnablePhysick, "Enable Physick"), ref enablePhysick))
            {
                config.Scholar.EnablePhysick = enablePhysick;
                save();
            }

            var enableAdlo = config.Scholar.EnableAdloquium;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableAdloquium, "Enable Adloquium"), ref enableAdlo))
            {
                config.Scholar.EnableAdloquium = enableAdlo;
                save();
            }

            var enableSuccor = config.Scholar.EnableSuccor;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableSuccor, "Enable Succor"), ref enableSuccor))
            {
                config.Scholar.EnableSuccor = enableSuccor;
                save();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.OgcdHeals, "oGCD Heals:"));

            var enableLustrate = config.Scholar.EnableLustrate;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableLustrate, "Enable Lustrate"), ref enableLustrate))
            {
                config.Scholar.EnableLustrate = enableLustrate;
                save();
            }

            var enableExcog = config.Scholar.EnableExcogitation;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableExcogitation, "Enable Excogitation"), ref enableExcog))
            {
                config.Scholar.EnableExcogitation = enableExcog;
                save();
            }

            var enableIndom = config.Scholar.EnableIndomitability;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableIndomitability, "Enable Indomitability"), ref enableIndom))
            {
                config.Scholar.EnableIndomitability = enableIndom;
                save();
            }

            var enableProtraction = config.Scholar.EnableProtraction;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableProtraction, "Enable Protraction"), ref enableProtraction))
            {
                config.Scholar.EnableProtraction = enableProtraction;
                save();
            }

            var enableRecitation = config.Scholar.EnableRecitation;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableRecitation, "Enable Recitation"), ref enableRecitation))
            {
                config.Scholar.EnableRecitation = enableRecitation;
                save();
            }

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

            var enableSoil = config.Scholar.EnableSacredSoil;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableSacredSoil, "Enable Sacred Soil"), ref enableSoil))
            {
                config.Scholar.EnableSacredSoil = enableSoil;
                save();
            }

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

            var autoSummon = config.Scholar.AutoSummonFairy;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.AutoSummonFairy, "Auto-Summon Fairy"), ref autoSummon))
            {
                config.Scholar.AutoSummonFairy = autoSummon;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.AutoSummonFairyDesc, "Automatically summon Eos if not present."));

            var enableAbilities = config.Scholar.EnableFairyAbilities;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableFairyAbilities, "Enable Fairy Abilities"), ref enableAbilities))
            {
                config.Scholar.EnableFairyAbilities = enableAbilities;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.EnableFairyAbilitiesDesc, "Automatically use Whispering Dawn, Fey Blessing, etc."));

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

            var enableConsolation = config.Scholar.EnableConsolation;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableConsolation, "Enable Consolation"), ref enableConsolation))
            {
                config.Scholar.EnableConsolation = enableConsolation;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.ConsolationDesc, "Seraph AoE heal + shield ability."));

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

            var enableET = config.Scholar.EnableEmergencyTactics;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EmergencyTactics, "Emergency Tactics"), ref enableET))
            {
                config.Scholar.EnableEmergencyTactics = enableET;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.EmergencyTacticsDesc, "Convert next shield to direct healing."));

            if (config.Scholar.EnableEmergencyTactics)
            {
                config.Scholar.EmergencyTacticsThreshold = ConfigUIHelpers.ThresholdSlider("ET HP Threshold",
                    config.Scholar.EmergencyTacticsThreshold, 20f, 60f, null, save);
            }

            ConfigUIHelpers.Spacing();

            var enableDT = config.Scholar.EnableDeploymentTactics;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.DeploymentTactics, "Deployment Tactics"), ref enableDT))
            {
                config.Scholar.EnableDeploymentTactics = enableDT;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.DeploymentTacticsDesc, "Spread Galvanize shield to party."));

            if (config.Scholar.EnableDeploymentTactics)
            {
                config.Scholar.DeploymentMinTargets = ConfigUIHelpers.IntSlider("Deploy Min Targets",
                    config.Scholar.DeploymentMinTargets, 2, 8, null, save);
            }

            ConfigUIHelpers.Spacing();

            var avoidSage = config.Scholar.AvoidOverwritingSageShields;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.AvoidSageShields, "Avoid Sage Shield Overwrite"), ref avoidSage))
            {
                config.Scholar.AvoidOverwritingSageShields = avoidSage;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.AvoidSageShieldsDesc, "Don't apply Galvanize if target has Sage shields."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.ExpedientLabel, "Expedient:"));

            var enableExp = config.Scholar.EnableExpedient;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableExpedient, "Enable Expedient"), ref enableExp))
            {
                config.Scholar.EnableExpedient = enableExp;
                save();
            }

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

            var enableED = config.Scholar.EnableEnergyDrain;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableEnergyDrain, "Enable Energy Drain"), ref enableED))
            {
                config.Scholar.EnableEnergyDrain = enableED;
                save();
            }

            config.Scholar.AetherflowDumpWindow = ConfigUIHelpers.FloatSlider(Loc.T(LocalizedStrings.Scholar.DumpWindow, "Dump Window (sec)"),
                config.Scholar.AetherflowDumpWindow, 0f, 15f, "%.1f",
                Loc.T(LocalizedStrings.Scholar.DumpWindowDesc, "Start dumping stacks when Aetherflow CD is below this."), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.DissipationLabel, "Dissipation:"));

            var enableDissipation = config.Scholar.EnableDissipation;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableDissipation, "Enable Dissipation"), ref enableDissipation))
            {
                config.Scholar.EnableDissipation = enableDissipation;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.DissipationDesc, "Sacrifice fairy for 3 Aetherflow + 20% heal boost."));

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

            var enableLucid = config.Scholar.EnableLucidDreaming;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableLucidDreaming, "Enable Lucid Dreaming"), ref enableLucid))
            {
                config.Scholar.EnableLucidDreaming = enableLucid;
                save();
            }

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

            var enableSingleTarget = config.Scholar.EnableSingleTargetDamage;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableBroilRuin, "Enable Broil/Ruin"), ref enableSingleTarget))
            {
                config.Scholar.EnableSingleTargetDamage = enableSingleTarget;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.BroilRuinDesc, "Casted single-target damage spells."));

            var enableRuinII = config.Scholar.EnableRuinII;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableRuinII, "Enable Ruin II"), ref enableRuinII))
            {
                config.Scholar.EnableRuinII = enableRuinII;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.RuinIIDesc, "Instant damage while moving."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.DotLabel, "DoT:"));

            var enableDot = config.Scholar.EnableDot;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableBioBiolysis, "Enable Bio/Biolysis"), ref enableDot))
            {
                config.Scholar.EnableDot = enableDot;
                save();
            }

            if (config.Scholar.EnableDot)
            {
                config.Scholar.DotRefreshThreshold = ConfigUIHelpers.FloatSlider("DoT Refresh (sec)",
                    config.Scholar.DotRefreshThreshold, 0f, 10f, "%.1f", null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.AoEDamageLabel, "AoE Damage:"));

            var enableAoEDamage = config.Scholar.EnableAoEDamage;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableArtOfWar, "Enable Art of War"), ref enableAoEDamage))
            {
                config.Scholar.EnableAoEDamage = enableAoEDamage;
                save();
            }

            if (config.Scholar.EnableAoEDamage)
            {
                config.Scholar.AoEDamageMinTargets = ConfigUIHelpers.IntSlider("Art of War Min Enemies",
                    config.Scholar.AoEDamageMinTargets, 2, 10, null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.AetherflowLabel, "Aetherflow:"));

            var enableAetherflow = config.Scholar.EnableAetherflow;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableAetherflow, "Enable Aetherflow"), ref enableAetherflow))
            {
                config.Scholar.EnableAetherflow = enableAetherflow;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.AetherflowDesc, "Use Aetherflow when stacks are empty."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Scholar.RaidBuffLabel, "Raid Buff:"));

            var enableChain = config.Scholar.EnableChainStratagem;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableChainStratagem, "Enable Chain Stratagem"), ref enableChain))
            {
                config.Scholar.EnableChainStratagem = enableChain;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.ChainStratagemDesc, "+10% crit rate on target for party."));

            var enableBaneful = config.Scholar.EnableBanefulImpaction;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Scholar.EnableBanefulImpaction, "Enable Baneful Impaction"), ref enableBaneful))
            {
                config.Scholar.EnableBanefulImpaction = enableBaneful;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Scholar.BanefulImpactionDesc, "AoE follow-up when Impact Imminent is active."));

            ConfigUIHelpers.EndIndent();
        }
    }
}
