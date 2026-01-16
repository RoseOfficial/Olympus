using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;

namespace Olympus.Windows.Config.Healers;

/// <summary>
/// Renders the White Mage (Apollo) settings section.
/// </summary>
public sealed class WhiteMageSection
{
    private readonly Configuration config;
    private readonly Action save;

    private static readonly string[] DpsPriorityNames = ["Heal First", "Balanced", "DPS First"];

    public WhiteMageSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("White Mage", "Apollo", ConfigUIHelpers.WhiteMageColor);

        DrawHealingSection();
        DrawDefensiveSection();
        DrawDamageSection();
        DrawDoTSection();
    }

    private void DrawHealingSection()
    {
        if (ConfigUIHelpers.SectionHeader("Healing", "WHM"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableHealing = config.EnableHealing;
            if (ImGui.Checkbox("Enable Healing", ref enableHealing))
            {
                config.EnableHealing = enableHealing;
                save();
            }

            ConfigUIHelpers.BeginDisabledGroup(!config.EnableHealing);

            ConfigUIHelpers.SectionLabel("Single-Target:");
            var enableCure = config.Healing.EnableCure;
            if (ImGui.Checkbox("Cure", ref enableCure))
            {
                config.Healing.EnableCure = enableCure;
                save();
            }

            ImGui.SameLine();
            var enableCureII = config.Healing.EnableCureII;
            if (ImGui.Checkbox("Cure II", ref enableCureII))
            {
                config.Healing.EnableCureII = enableCureII;
                save();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("AoE Healing:");

            var enableMedica = config.Healing.EnableMedica;
            if (ImGui.Checkbox("Medica", ref enableMedica))
            {
                config.Healing.EnableMedica = enableMedica;
                save();
            }

            ImGui.SameLine();
            var enableMedicaII = config.Healing.EnableMedicaII;
            if (ImGui.Checkbox("Medica II", ref enableMedicaII))
            {
                config.Healing.EnableMedicaII = enableMedicaII;
                save();
            }

            ImGui.SameLine();
            var enableMedicaIII = config.Healing.EnableMedicaIII;
            if (ImGui.Checkbox("Medica III", ref enableMedicaIII))
            {
                config.Healing.EnableMedicaIII = enableMedicaIII;
                save();
            }

            var enableCureIII = config.Healing.EnableCureIII;
            if (ImGui.Checkbox("Cure III", ref enableCureIII))
            {
                config.Healing.EnableCureIII = enableCureIII;
                save();
            }
            ImGui.TextDisabled("Targeted AoE heal (10y radius around target). Best when stacked.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Lily Heals:");

            var enableAfflatusSolace = config.Healing.EnableAfflatusSolace;
            if (ImGui.Checkbox("Afflatus Solace", ref enableAfflatusSolace))
            {
                config.Healing.EnableAfflatusSolace = enableAfflatusSolace;
                save();
            }

            ImGui.SameLine();
            var enableAfflatusRapture = config.Healing.EnableAfflatusRapture;
            if (ImGui.Checkbox("Afflatus Rapture", ref enableAfflatusRapture))
            {
                config.Healing.EnableAfflatusRapture = enableAfflatusRapture;
                save();
            }
            ImGui.TextDisabled("Free heals that consume Lily gauge.");

            // Blood Lily Optimization Strategy
            ConfigUIHelpers.Spacing();
            var strategyNames = Enum.GetNames<LilyGenerationStrategy>();
            var currentIndex = (int)config.Healing.LilyStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Lily Strategy", ref currentIndex, strategyNames, strategyNames.Length))
            {
                config.Healing.LilyStrategy = (LilyGenerationStrategy)currentIndex;
                save();
            }
            var strategyDescription = config.Healing.LilyStrategy switch
            {
                LilyGenerationStrategy.Aggressive => "Always prefer lily heals when available",
                LilyGenerationStrategy.Balanced => "Prefer lily heals until Blood Lily is full (3/3)",
                LilyGenerationStrategy.Conservative => "Only use lily heals below HP threshold",
                LilyGenerationStrategy.Disabled => "Use normal heal priority (no lily preference)",
                _ => ""
            };
            ImGui.TextDisabled(strategyDescription);

            // Conservative HP threshold (only show when Conservative mode is selected)
            if (config.Healing.LilyStrategy == LilyGenerationStrategy.Conservative)
            {
                config.Healing.ConservativeLilyHpThreshold = ConfigUIHelpers.ThresholdSliderSmall(
                    "Conservative HP Threshold", config.Healing.ConservativeLilyHpThreshold, 50f, 90f,
                    "Only use lily heals when target is below this HP%.", save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("oGCD Heals:");

            var enableTetragrammaton = config.Healing.EnableTetragrammaton;
            if (ImGui.Checkbox("Tetragrammaton", ref enableTetragrammaton))
            {
                config.Healing.EnableTetragrammaton = enableTetragrammaton;
                save();
            }

            ImGui.SameLine();
            var enableBenediction = config.Healing.EnableBenediction;
            if (ImGui.Checkbox("Benediction", ref enableBenediction))
            {
                config.Healing.EnableBenediction = enableBenediction;
                save();
            }

            ImGui.SameLine();
            var enableAssize = config.Healing.EnableAssize;
            if (ImGui.Checkbox("Assize", ref enableAssize))
            {
                config.Healing.EnableAssize = enableAssize;
                save();
            }
            ImGui.TextDisabled("Instant heals used during weave windows.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Healing HoTs:");

            var enableRegen = config.Healing.EnableRegen;
            if (ImGui.Checkbox("Regen", ref enableRegen))
            {
                config.Healing.EnableRegen = enableRegen;
                save();
            }

            ImGui.SameLine();
            var enableAsylum = config.Healing.EnableAsylum;
            if (ImGui.Checkbox("Asylum", ref enableAsylum))
            {
                config.Healing.EnableAsylum = enableAsylum;
                save();
            }
            ImGui.TextDisabled("Regen (single-target) and Asylum (ground AoE HoT).");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Buffs:");

            var enablePoM = config.Buffs.EnablePresenceOfMind;
            if (ImGui.Checkbox("Presence of Mind", ref enablePoM))
            {
                config.Buffs.EnablePresenceOfMind = enablePoM;
                save();
            }

            ImGui.SameLine();
            var enableThinAir = config.Buffs.EnableThinAir;
            if (ImGui.Checkbox("Thin Air", ref enableThinAir))
            {
                config.Buffs.EnableThinAir = enableThinAir;
                save();
            }
            ImGui.TextDisabled("Speed buff and MP cost reduction.");

            var enableAetherialShift = config.Buffs.EnableAetherialShift;
            if (ImGui.Checkbox("Aetherial Shift", ref enableAetherialShift))
            {
                config.Buffs.EnableAetherialShift = enableAetherialShift;
                save();
            }
            ImGui.TextDisabled("Gap closer (15y dash) when out of spell range.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Emergency Thresholds:");

            config.Healing.OgcdEmergencyThreshold = ConfigUIHelpers.ThresholdSlider("oGCD Emergency",
                config.Healing.OgcdEmergencyThreshold, 30f, 70f, "Use emergency oGCD heals (Tetra) when below this HP%.", save);

            config.Healing.GcdEmergencyThreshold = ConfigUIHelpers.ThresholdSlider("GCD Emergency",
                config.Healing.GcdEmergencyThreshold, 20f, 60f, "Interrupt DPS to heal when below this HP%.", save);

            config.Healing.BenedictionEmergencyThreshold = ConfigUIHelpers.ThresholdSlider("Benediction Threshold",
                config.Healing.BenedictionEmergencyThreshold, 10f, 50f, "Only use Benediction when target HP is below this %.", save);

            ConfigUIHelpers.Spacing();

            config.Healing.AoEHealMinTargets = ConfigUIHelpers.IntSlider("AoE Min Targets",
                config.Healing.AoEHealMinTargets, 2, 8, "Use AoE heal when this many party members need healing.", save);

            DrawAdvancedHealingSection();

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawAdvancedHealingSection()
    {
        ConfigUIHelpers.Spacing();
        ConfigUIHelpers.Separator();

        if (ConfigUIHelpers.BeginTreeNode("Advanced Healing Settings"))
        {
            // Triage Settings
            ConfigUIHelpers.SectionLabel("Healing Triage:");

            var useTriage = config.Healing.UseDamageIntakeTriage;
            if (ImGui.Checkbox("Use Damage-Based Triage", ref useTriage))
            {
                config.Healing.UseDamageIntakeTriage = useTriage;
                save();
            }
            ImGui.TextDisabled("Prioritize healing targets taking active damage.");

            if (config.Healing.UseDamageIntakeTriage)
            {
                ConfigUIHelpers.BeginIndent();
                var presetNames = Enum.GetNames<TriagePreset>();
                var currentPreset = (int)config.Healing.TriagePreset;
                ImGui.SetNextItemWidth(150);
                if (ImGui.Combo("Triage Preset", ref currentPreset, presetNames, presetNames.Length))
                {
                    config.Healing.TriagePreset = (TriagePreset)currentPreset;
                    save();
                }

                var presetDesc = config.Healing.TriagePreset switch
                {
                    TriagePreset.Balanced => "Balanced weights across all factors",
                    TriagePreset.TankFocus => "Prioritize tanks over DPS",
                    TriagePreset.SpreadDamage => "React to highest damage intake",
                    TriagePreset.RaidWide => "Focus on lowest HP members",
                    TriagePreset.Custom => "Use custom weight values below",
                    _ => ""
                };
                ImGui.TextDisabled(presetDesc);

                // Show custom weights only when Custom is selected
                if (config.Healing.TriagePreset == TriagePreset.Custom)
                {
                    config.Healing.CustomTriageWeights.DamageRate = ConfigUIHelpers.ThresholdSliderSmall(
                        "Damage Rate", config.Healing.CustomTriageWeights.DamageRate, 0f, 60f, null, save);
                    config.Healing.CustomTriageWeights.TankBonus = ConfigUIHelpers.ThresholdSliderSmall(
                        "Tank Bonus", config.Healing.CustomTriageWeights.TankBonus, 0f, 60f, null, save);
                    config.Healing.CustomTriageWeights.MissingHp = ConfigUIHelpers.ThresholdSliderSmall(
                        "Missing HP", config.Healing.CustomTriageWeights.MissingHp, 0f, 60f, null, save);
                    config.Healing.CustomTriageWeights.DamageAcceleration = ConfigUIHelpers.ThresholdSliderSmall(
                        "Acceleration", config.Healing.CustomTriageWeights.DamageAcceleration, 0f, 30f, null, save);
                }
                ConfigUIHelpers.EndIndent();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Assize Healing:");

            var enableAssizeHealing = config.Healing.EnableAssizeHealing;
            if (ImGui.Checkbox("Enable Assize for Healing", ref enableAssizeHealing))
            {
                config.Healing.EnableAssizeHealing = enableAssizeHealing;
                save();
            }
            ImGui.TextDisabled("Use Assize as a healing oGCD when party needs it.");

            if (config.Healing.EnableAssizeHealing)
            {
                ConfigUIHelpers.BeginIndent();
                config.Healing.AssizeHealingMinTargets = ConfigUIHelpers.IntSliderSmall("Min Injured",
                    config.Healing.AssizeHealingMinTargets, 1, 8, null, save);
                config.Healing.AssizeHealingHpThreshold = ConfigUIHelpers.ThresholdSliderSmall("HP Threshold",
                    config.Healing.AssizeHealingHpThreshold, 50f, 95f, "Prioritize Assize healing when avg HP below threshold.", save);
                ConfigUIHelpers.EndIndent();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Preemptive Healing:");

            var enablePreemptive = config.Healing.EnablePreemptiveHealing;
            if (ImGui.Checkbox("Enable Preemptive Healing", ref enablePreemptive))
            {
                config.Healing.EnablePreemptiveHealing = enablePreemptive;
                save();
            }
            ImGui.TextDisabled("Heal before damage spikes land based on pattern detection.");

            if (config.Healing.EnablePreemptiveHealing)
            {
                ConfigUIHelpers.BeginIndent();
                config.Healing.PreemptiveHealingThreshold = ConfigUIHelpers.ThresholdSliderSmall("HP Trigger",
                    config.Healing.PreemptiveHealingThreshold, 10f, 80f, "Heal if projected HP would drop below this.", save);
                config.Healing.SpikePatternConfidenceThreshold = ConfigUIHelpers.ThresholdSliderSmall("Pattern Confidence",
                    config.Healing.SpikePatternConfidenceThreshold, 30f, 95f, "Minimum confidence for spike pattern prediction.", save);

                config.Healing.SpikePredictionLookahead = ConfigUIHelpers.FloatSlider("Lookahead (sec)",
                    config.Healing.SpikePredictionLookahead, 0.5f, 5f, "%.1f", "How far ahead to predict spikes.", save);
                ConfigUIHelpers.EndIndent();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Experimental:");

            var enableScored = config.Healing.EnableScoredHealSelection;
            if (ImGui.Checkbox("Enable Scored Heal Selection", ref enableScored))
            {
                config.Healing.EnableScoredHealSelection = enableScored;
                save();
            }
            ImGui.TextDisabled("Use multi-factor scoring instead of tier-based selection.");
            ConfigUIHelpers.WarningText("EXPERIMENTAL");

            ConfigUIHelpers.EndTreeNode();
        }
    }

    private void DrawDefensiveSection()
    {
        if (ConfigUIHelpers.SectionHeader("Defensive Cooldowns", "WHM"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Shields:");

            var enableBenison = config.Defensive.EnableDivineBenison;
            if (ImGui.Checkbox("Divine Benison", ref enableBenison))
            {
                config.Defensive.EnableDivineBenison = enableBenison;
                save();
            }

            ImGui.SameLine();
            var enableAquaveil = config.Defensive.EnableAquaveil;
            if (ImGui.Checkbox("Aquaveil", ref enableAquaveil))
            {
                config.Defensive.EnableAquaveil = enableAquaveil;
                save();
            }
            ImGui.TextDisabled("Single-target shields. Applied to tank when HP < 90%.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Party Mitigation:");

            var enablePlenary = config.Defensive.EnablePlenaryIndulgence;
            if (ImGui.Checkbox("Plenary Indulgence", ref enablePlenary))
            {
                config.Defensive.EnablePlenaryIndulgence = enablePlenary;
                save();
            }

            ImGui.SameLine();
            var enableTemperance = config.Defensive.EnableTemperance;
            if (ImGui.Checkbox("Temperance", ref enableTemperance))
            {
                config.Defensive.EnableTemperance = enableTemperance;
                save();
            }
            ImGui.TextDisabled("Party-wide mitigation. Used when 3+ injured or avg HP low.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Advanced:");

            var enableBell = config.Defensive.EnableLiturgyOfTheBell;
            if (ImGui.Checkbox("Liturgy of the Bell", ref enableBell))
            {
                config.Defensive.EnableLiturgyOfTheBell = enableBell;
                save();
            }

            ImGui.SameLine();
            var enableCaress = config.Defensive.EnableDivineCaress;
            if (ImGui.Checkbox("Divine Caress", ref enableCaress))
            {
                config.Defensive.EnableDivineCaress = enableCaress;
                save();
            }
            ImGui.TextDisabled("Bell: Ground AoE reactive heal. Caress: AoE shield after Temperance.");

            ConfigUIHelpers.Spacing();

            config.Defensive.DefensiveCooldownThreshold = ConfigUIHelpers.ThresholdSlider("Defensive Threshold",
                config.Defensive.DefensiveCooldownThreshold, 50f, 95f, "Use defensives when party avg HP falls below this %.", save);

            var useWithAoE = config.Defensive.UseDefensivesWithAoEHeals;
            if (ImGui.Checkbox("Use with AoE Heals", ref useWithAoE))
            {
                config.Defensive.UseDefensivesWithAoEHeals = useWithAoE;
                save();
            }
            ImGui.TextDisabled("Sync Plenary Indulgence with AoE healing for bonus potency.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader("Damage", "WHM"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableDamage = config.EnableDamage;
            if (ImGui.Checkbox("Enable Damage", ref enableDamage))
            {
                config.EnableDamage = enableDamage;
                save();
            }

            ConfigUIHelpers.BeginDisabledGroup(!config.EnableDamage);

            // DPS Priority Mode
            var currentPriority = (int)config.Damage.DpsPriority;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("DPS Priority", ref currentPriority, DpsPriorityNames, DpsPriorityNames.Length))
            {
                config.Damage.DpsPriority = (DpsPriorityMode)currentPriority;
                save();
            }
            var priorityDesc = config.Damage.DpsPriority switch
            {
                DpsPriorityMode.HealFirst => "Safest - only DPS when party is healthy",
                DpsPriorityMode.Balanced => "Moderate - more aggressive DPS while healing",
                DpsPriorityMode.DpsFirst => "Maximum DPS - minimal proactive healing",
                _ => ""
            };
            ImGui.TextDisabled(priorityDesc);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Stone Progression:");

            var enableStone = config.Damage.EnableStone;
            if (ImGui.Checkbox("Stone", ref enableStone))
            {
                config.Damage.EnableStone = enableStone;
                save();
            }

            ImGui.SameLine();
            var enableStoneII = config.Damage.EnableStoneII;
            if (ImGui.Checkbox("Stone II", ref enableStoneII))
            {
                config.Damage.EnableStoneII = enableStoneII;
                save();
            }

            ImGui.SameLine();
            var enableStoneIII = config.Damage.EnableStoneIII;
            if (ImGui.Checkbox("Stone III", ref enableStoneIII))
            {
                config.Damage.EnableStoneIII = enableStoneIII;
                save();
            }

            var enableStoneIV = config.Damage.EnableStoneIV;
            if (ImGui.Checkbox("Stone IV", ref enableStoneIV))
            {
                config.Damage.EnableStoneIV = enableStoneIV;
                save();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Glare Progression:");

            var enableGlare = config.Damage.EnableGlare;
            if (ImGui.Checkbox("Glare", ref enableGlare))
            {
                config.Damage.EnableGlare = enableGlare;
                save();
            }

            ImGui.SameLine();
            var enableGlareIII = config.Damage.EnableGlareIII;
            if (ImGui.Checkbox("Glare III", ref enableGlareIII))
            {
                config.Damage.EnableGlareIII = enableGlareIII;
                save();
            }

            ImGui.SameLine();
            var enableGlareIV = config.Damage.EnableGlareIV;
            if (ImGui.Checkbox("Glare IV", ref enableGlareIV))
            {
                config.Damage.EnableGlareIV = enableGlareIV;
                save();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("AoE Damage:");

            var enableHoly = config.Damage.EnableHoly;
            if (ImGui.Checkbox("Holy", ref enableHoly))
            {
                config.Damage.EnableHoly = enableHoly;
                save();
            }

            ImGui.SameLine();
            var enableHolyIII = config.Damage.EnableHolyIII;
            if (ImGui.Checkbox("Holy III", ref enableHolyIII))
            {
                config.Damage.EnableHolyIII = enableHolyIII;
                save();
            }
            ImGui.TextDisabled("Self-centered AoE (8y radius). Use when enemies are stacked.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Blood Lily:");

            var enableMisery = config.Damage.EnableAfflatusMisery;
            if (ImGui.Checkbox("Afflatus Misery", ref enableMisery))
            {
                config.Damage.EnableAfflatusMisery = enableMisery;
                save();
            }
            ImGui.TextDisabled("1240p AoE damage (costs 3 Blood Lilies). Use at 3 stacks.");

            ConfigUIHelpers.Spacing();

            config.Damage.AoEDamageMinTargets = ConfigUIHelpers.IntSlider("AoE Min Enemies",
                config.Damage.AoEDamageMinTargets, 2, 8, "Use Holy when this many enemies are within range.", save);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDoTSection()
    {
        if (ConfigUIHelpers.SectionHeader("DoT", "WHM"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableDoT = config.EnableDoT;
            if (ImGui.Checkbox("Enable DoT", ref enableDoT))
            {
                config.EnableDoT = enableDoT;
                save();
            }

            ConfigUIHelpers.BeginDisabledGroup(!config.EnableDoT);

            var enableAero = config.Dot.EnableAero;
            if (ImGui.Checkbox("Aero", ref enableAero))
            {
                config.Dot.EnableAero = enableAero;
                save();
            }

            ImGui.SameLine();
            var enableAeroII = config.Dot.EnableAeroII;
            if (ImGui.Checkbox("Aero II", ref enableAeroII))
            {
                config.Dot.EnableAeroII = enableAeroII;
                save();
            }

            ImGui.SameLine();
            var enableDia = config.Dot.EnableDia;
            if (ImGui.Checkbox("Dia", ref enableDia))
            {
                config.Dot.EnableDia = enableDia;
                save();
            }

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }
}
