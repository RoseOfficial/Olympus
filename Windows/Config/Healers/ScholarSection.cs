using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;

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
        if (ConfigUIHelpers.SectionHeader("Healing", "SCH"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("GCD Heals:");

            var enablePhysick = config.Scholar.EnablePhysick;
            if (ImGui.Checkbox("Enable Physick", ref enablePhysick))
            {
                config.Scholar.EnablePhysick = enablePhysick;
                save();
            }

            var enableAdlo = config.Scholar.EnableAdloquium;
            if (ImGui.Checkbox("Enable Adloquium", ref enableAdlo))
            {
                config.Scholar.EnableAdloquium = enableAdlo;
                save();
            }

            var enableSuccor = config.Scholar.EnableSuccor;
            if (ImGui.Checkbox("Enable Succor", ref enableSuccor))
            {
                config.Scholar.EnableSuccor = enableSuccor;
                save();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("oGCD Heals:");

            var enableLustrate = config.Scholar.EnableLustrate;
            if (ImGui.Checkbox("Enable Lustrate", ref enableLustrate))
            {
                config.Scholar.EnableLustrate = enableLustrate;
                save();
            }

            var enableExcog = config.Scholar.EnableExcogitation;
            if (ImGui.Checkbox("Enable Excogitation", ref enableExcog))
            {
                config.Scholar.EnableExcogitation = enableExcog;
                save();
            }

            var enableIndom = config.Scholar.EnableIndomitability;
            if (ImGui.Checkbox("Enable Indomitability", ref enableIndom))
            {
                config.Scholar.EnableIndomitability = enableIndom;
                save();
            }

            var enableProtraction = config.Scholar.EnableProtraction;
            if (ImGui.Checkbox("Enable Protraction", ref enableProtraction))
            {
                config.Scholar.EnableProtraction = enableProtraction;
                save();
            }

            var enableRecitation = config.Scholar.EnableRecitation;
            if (ImGui.Checkbox("Enable Recitation", ref enableRecitation))
            {
                config.Scholar.EnableRecitation = enableRecitation;
                save();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Single-Target Thresholds:");

            config.Scholar.PhysickThreshold = ConfigUIHelpers.ThresholdSlider("Physick",
                config.Scholar.PhysickThreshold, 20f, 80f, null, save);
            config.Scholar.AdloquiumThreshold = ConfigUIHelpers.ThresholdSlider("Adloquium",
                config.Scholar.AdloquiumThreshold, 40f, 90f, null, save);
            config.Scholar.LustrateThreshold = ConfigUIHelpers.ThresholdSlider("Lustrate",
                config.Scholar.LustrateThreshold, 30f, 80f, null, save);
            config.Scholar.ExcogitationThreshold = ConfigUIHelpers.ThresholdSlider("Excogitation",
                config.Scholar.ExcogitationThreshold, 60f, 95f,
                "Apply Excogitation proactively at this HP%.", save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("AoE Healing:");

            config.Scholar.AoEHealThreshold = ConfigUIHelpers.ThresholdSlider("AoE HP Threshold",
                config.Scholar.AoEHealThreshold, 50f, 90f, null, save);

            config.Scholar.AoEHealMinTargets = ConfigUIHelpers.IntSlider("AoE Min Targets##SCH",
                config.Scholar.AoEHealMinTargets, 2, 8, null, save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Recitation Priority:");

            var recitationNames = Enum.GetNames<RecitationPriority>();
            var currentRecitation = (int)config.Scholar.RecitationPriority;
            ImGui.SetNextItemWidth(180);
            if (ImGui.Combo("Recitation Target", ref currentRecitation, recitationNames, recitationNames.Length))
            {
                config.Scholar.RecitationPriority = (RecitationPriority)currentRecitation;
                save();
            }
            ImGui.TextDisabled("Which ability to use with Recitation (guaranteed crit, free).");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Sacred Soil:");

            var enableSoil = config.Scholar.EnableSacredSoil;
            if (ImGui.Checkbox("Enable Sacred Soil", ref enableSoil))
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
        if (ConfigUIHelpers.SectionHeader("Fairy", "SCH"))
        {
            ConfigUIHelpers.BeginIndent();

            var autoSummon = config.Scholar.AutoSummonFairy;
            if (ImGui.Checkbox("Auto-Summon Fairy", ref autoSummon))
            {
                config.Scholar.AutoSummonFairy = autoSummon;
                save();
            }
            ImGui.TextDisabled("Automatically summon Eos if not present.");

            var enableAbilities = config.Scholar.EnableFairyAbilities;
            if (ImGui.Checkbox("Enable Fairy Abilities", ref enableAbilities))
            {
                config.Scholar.EnableFairyAbilities = enableAbilities;
                save();
            }
            ImGui.TextDisabled("Automatically use Whispering Dawn, Fey Blessing, etc.");

            ConfigUIHelpers.BeginDisabledGroup(!config.Scholar.EnableFairyAbilities);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Whispering Dawn:");

            config.Scholar.WhisperingDawnThreshold = ConfigUIHelpers.ThresholdSlider("WD HP Threshold",
                config.Scholar.WhisperingDawnThreshold, 50f, 95f, null, save);

            config.Scholar.WhisperingDawnMinTargets = ConfigUIHelpers.IntSlider("WD Min Targets",
                config.Scholar.WhisperingDawnMinTargets, 1, 8, null, save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Fey Blessing:");

            config.Scholar.FeyBlessingThreshold = ConfigUIHelpers.ThresholdSlider("FB HP Threshold",
                config.Scholar.FeyBlessingThreshold, 50f, 90f, null, save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Fey Union:");

            config.Scholar.FeyUnionThreshold = ConfigUIHelpers.ThresholdSlider("FU HP Threshold",
                config.Scholar.FeyUnionThreshold, 40f, 80f, null, save);

            config.Scholar.FeyUnionMinGauge = ConfigUIHelpers.IntSlider("FU Min Gauge",
                config.Scholar.FeyUnionMinGauge, 10, 100, null, save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Seraph:");

            var seraphNames = Enum.GetNames<SeraphUsageStrategy>();
            var currentSeraph = (int)config.Scholar.SeraphStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Seraph Strategy", ref currentSeraph, seraphNames, seraphNames.Length))
            {
                config.Scholar.SeraphStrategy = (SeraphUsageStrategy)currentSeraph;
                save();
            }

            config.Scholar.SeraphPartyHpThreshold = ConfigUIHelpers.ThresholdSlider("Seraph HP Trigger",
                config.Scholar.SeraphPartyHpThreshold, 50f, 90f, null, save);

            var enableConsolation = config.Scholar.EnableConsolation;
            if (ImGui.Checkbox("Enable Consolation", ref enableConsolation))
            {
                config.Scholar.EnableConsolation = enableConsolation;
                save();
            }
            ImGui.TextDisabled("Seraph AoE heal + shield ability.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Seraphism (Lv100):");

            var seraphismNames = Enum.GetNames<SeraphismUsageStrategy>();
            var currentSeraphism = (int)config.Scholar.SeraphismStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Seraphism Strategy", ref currentSeraphism, seraphismNames, seraphismNames.Length))
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
        if (ConfigUIHelpers.SectionHeader("Shields", "SCH"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableET = config.Scholar.EnableEmergencyTactics;
            if (ImGui.Checkbox("Emergency Tactics", ref enableET))
            {
                config.Scholar.EnableEmergencyTactics = enableET;
                save();
            }
            ImGui.TextDisabled("Convert next shield to direct healing.");

            if (config.Scholar.EnableEmergencyTactics)
            {
                config.Scholar.EmergencyTacticsThreshold = ConfigUIHelpers.ThresholdSlider("ET HP Threshold",
                    config.Scholar.EmergencyTacticsThreshold, 20f, 60f, null, save);
            }

            ConfigUIHelpers.Spacing();

            var enableDT = config.Scholar.EnableDeploymentTactics;
            if (ImGui.Checkbox("Deployment Tactics", ref enableDT))
            {
                config.Scholar.EnableDeploymentTactics = enableDT;
                save();
            }
            ImGui.TextDisabled("Spread Galvanize shield to party.");

            if (config.Scholar.EnableDeploymentTactics)
            {
                config.Scholar.DeploymentMinTargets = ConfigUIHelpers.IntSlider("Deploy Min Targets",
                    config.Scholar.DeploymentMinTargets, 2, 8, null, save);
            }

            ConfigUIHelpers.Spacing();

            var avoidSage = config.Scholar.AvoidOverwritingSageShields;
            if (ImGui.Checkbox("Avoid Sage Shield Overwrite", ref avoidSage))
            {
                config.Scholar.AvoidOverwritingSageShields = avoidSage;
                save();
            }
            ImGui.TextDisabled("Don't apply Galvanize if target has Sage shields.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Expedient:");

            var enableExp = config.Scholar.EnableExpedient;
            if (ImGui.Checkbox("Enable Expedient", ref enableExp))
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
        if (ConfigUIHelpers.SectionHeader("Aetherflow", "SCH"))
        {
            ConfigUIHelpers.BeginIndent();

            var strategyNames = Enum.GetNames<AetherflowUsageStrategy>();
            var currentStrategy = (int)config.Scholar.AetherflowStrategy;
            ImGui.SetNextItemWidth(180);
            if (ImGui.Combo("Aetherflow Strategy", ref currentStrategy, strategyNames, strategyNames.Length))
            {
                config.Scholar.AetherflowStrategy = (AetherflowUsageStrategy)currentStrategy;
                save();
            }

            var strategyDesc = config.Scholar.AetherflowStrategy switch
            {
                AetherflowUsageStrategy.Balanced => "Balance healing and Energy Drain",
                AetherflowUsageStrategy.HealingPriority => "Prioritize healing, minimal DPS",
                AetherflowUsageStrategy.AggressiveDps => "Aggressive Energy Drain when safe",
                _ => ""
            };
            ImGui.TextDisabled(strategyDesc);

            ConfigUIHelpers.Spacing();

            config.Scholar.AetherflowReserve = ConfigUIHelpers.IntSlider("Stack Reserve",
                config.Scholar.AetherflowReserve, 0, 3, "Stacks to keep for emergency healing.", save);

            var enableED = config.Scholar.EnableEnergyDrain;
            if (ImGui.Checkbox("Enable Energy Drain", ref enableED))
            {
                config.Scholar.EnableEnergyDrain = enableED;
                save();
            }

            config.Scholar.AetherflowDumpWindow = ConfigUIHelpers.FloatSlider("Dump Window (sec)",
                config.Scholar.AetherflowDumpWindow, 0f, 15f, "%.1f",
                "Start dumping stacks when Aetherflow CD is below this.", save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Dissipation:");

            var enableDissipation = config.Scholar.EnableDissipation;
            if (ImGui.Checkbox("Enable Dissipation", ref enableDissipation))
            {
                config.Scholar.EnableDissipation = enableDissipation;
                save();
            }
            ImGui.TextDisabled("Sacrifice fairy for 3 Aetherflow + 20% heal boost.");

            if (config.Scholar.EnableDissipation)
            {
                ConfigUIHelpers.BeginIndent();
                config.Scholar.DissipationMaxFairyGauge = ConfigUIHelpers.IntSliderSmall("Max Fairy Gauge",
                    config.Scholar.DissipationMaxFairyGauge, 0, 100,
                    "Only use when gauge is below this (avoid waste).", save);

                config.Scholar.DissipationSafePartyHp = ConfigUIHelpers.ThresholdSliderSmall("Safe Party HP",
                    config.Scholar.DissipationSafePartyHp, 60f, 95f, "Only use when party HP is above this.", save);
                ConfigUIHelpers.EndIndent();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("MP Management:");

            var enableLucid = config.Scholar.EnableLucidDreaming;
            if (ImGui.Checkbox("Enable Lucid Dreaming", ref enableLucid))
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
        if (ConfigUIHelpers.SectionHeader("Damage", "SCH"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Single-Target Damage:");

            var enableSingleTarget = config.Scholar.EnableSingleTargetDamage;
            if (ImGui.Checkbox("Enable Broil/Ruin", ref enableSingleTarget))
            {
                config.Scholar.EnableSingleTargetDamage = enableSingleTarget;
                save();
            }
            ImGui.TextDisabled("Casted single-target damage spells.");

            var enableRuinII = config.Scholar.EnableRuinII;
            if (ImGui.Checkbox("Enable Ruin II", ref enableRuinII))
            {
                config.Scholar.EnableRuinII = enableRuinII;
                save();
            }
            ImGui.TextDisabled("Instant damage while moving.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("DoT:");

            var enableDot = config.Scholar.EnableDot;
            if (ImGui.Checkbox("Enable Bio/Biolysis", ref enableDot))
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
            ConfigUIHelpers.SectionLabel("AoE Damage:");

            var enableAoEDamage = config.Scholar.EnableAoEDamage;
            if (ImGui.Checkbox("Enable Art of War", ref enableAoEDamage))
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
            ConfigUIHelpers.SectionLabel("Aetherflow:");

            var enableAetherflow = config.Scholar.EnableAetherflow;
            if (ImGui.Checkbox("Enable Aetherflow", ref enableAetherflow))
            {
                config.Scholar.EnableAetherflow = enableAetherflow;
                save();
            }
            ImGui.TextDisabled("Use Aetherflow when stacks are empty.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Raid Buff:");

            var enableChain = config.Scholar.EnableChainStratagem;
            if (ImGui.Checkbox("Enable Chain Stratagem", ref enableChain))
            {
                config.Scholar.EnableChainStratagem = enableChain;
                save();
            }
            ImGui.TextDisabled("+10% crit rate on target for party.");

            var enableBaneful = config.Scholar.EnableBanefulImpaction;
            if (ImGui.Checkbox("Enable Baneful Impaction", ref enableBaneful))
            {
                config.Scholar.EnableBanefulImpaction = enableBaneful;
                save();
            }
            ImGui.TextDisabled("AoE follow-up when Impact Imminent is active.");

            ConfigUIHelpers.EndIndent();
        }
    }
}
