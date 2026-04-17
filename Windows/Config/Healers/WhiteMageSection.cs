using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.Healers;

/// <summary>
/// Renders the White Mage (Apollo) settings section.
/// </summary>
public sealed class WhiteMageSection
{
    private readonly Configuration config;
    private readonly Action save;

    private static string[] GetDpsPriorityNames() => new[]
    {
        Loc.T(LocalizedStrings.WhiteMage.DpsPriorityHealFirst, "Heal First"),
        Loc.T(LocalizedStrings.WhiteMage.DpsPriorityBalanced, "Balanced"),
        Loc.T(LocalizedStrings.WhiteMage.DpsPriorityDpsFirst, "DPS First"),
    };

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
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.WhiteMage.HealingSection, "Healing"), "WHM"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.General.EnableHealing, "Enable Healing"), () => this.config.EnableHealing, v => this.config.EnableHealing = v, null, this.save);

            ConfigUIHelpers.BeginDisabledGroup(!this.config.EnableHealing);

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.SingleTarget, "Single-Target:"));
            ConfigUIHelpers.Toggle("Cure", () => config.Healing.EnableCure, v => config.Healing.EnableCure = v, null, save,
                actionId: WHMActions.Cure.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Cure II", () => config.Healing.EnableCureII, v => config.Healing.EnableCureII = v, null, save,
                actionId: WHMActions.CureII.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.AoEHealing, "AoE Healing:"));

            ConfigUIHelpers.Toggle("Medica", () => config.Healing.EnableMedica, v => config.Healing.EnableMedica = v, null, save,
                actionId: WHMActions.Medica.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Medica II", () => config.Healing.EnableMedicaII, v => config.Healing.EnableMedicaII = v, null, save,
                actionId: WHMActions.MedicaII.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Medica III", () => config.Healing.EnableMedicaIII, v => config.Healing.EnableMedicaIII = v, null, save,
                actionId: WHMActions.MedicaIII.ActionId);

            ConfigUIHelpers.Toggle("Cure III", () => config.Healing.EnableCureIII, v => config.Healing.EnableCureIII = v,
                null, save, actionId: WHMActions.CureIII.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.LilyHeals, "Lily Heals:"));

            ConfigUIHelpers.Toggle("Afflatus Solace", () => config.Healing.EnableAfflatusSolace, v => config.Healing.EnableAfflatusSolace = v, null, save,
                actionId: WHMActions.AfflatusSolace.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Afflatus Rapture", () => config.Healing.EnableAfflatusRapture, v => config.Healing.EnableAfflatusRapture = v,
                null, save, actionId: WHMActions.AfflatusRapture.ActionId);

            // Blood Lily Optimization Strategy
            ConfigUIHelpers.Spacing();
            var strategyNames = Enum.GetNames<LilyGenerationStrategy>();
            var currentIndex = (int)config.Healing.LilyStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo(Loc.T(LocalizedStrings.WhiteMage.LilyStrategyLabel, "Lily Strategy"), ref currentIndex, strategyNames, strategyNames.Length))
            {
                config.Healing.LilyStrategy = (LilyGenerationStrategy)currentIndex;
                save();
            }
            var strategyDescription = config.Healing.LilyStrategy switch
            {
                LilyGenerationStrategy.Aggressive => Loc.T(LocalizedStrings.WhiteMage.LilyStrategyAggressive, "Always prefer lily heals when available"),
                LilyGenerationStrategy.Balanced => Loc.T(LocalizedStrings.WhiteMage.LilyStrategyBalanced, "Prefer lily heals until Blood Lily is full (3/3)"),
                LilyGenerationStrategy.Conservative => Loc.T(LocalizedStrings.WhiteMage.LilyStrategyConservative, "Only use lily heals below HP threshold"),
                LilyGenerationStrategy.Disabled => Loc.T(LocalizedStrings.WhiteMage.LilyStrategyDisabled, "Use normal heal priority (no lily preference)"),
                _ => ""
            };
            ImGui.TextDisabled(strategyDescription);

            // Conservative HP threshold (only show when Conservative mode is selected)
            if (config.Healing.LilyStrategy == LilyGenerationStrategy.Conservative)
            {
                config.Healing.ConservativeLilyHpThreshold = ConfigUIHelpers.ThresholdSliderSmall(
                    Loc.T(LocalizedStrings.WhiteMage.ConservativeHpThreshold, "Conservative HP Threshold"), config.Healing.ConservativeLilyHpThreshold, 50f, 90f,
                    "Only use lily heals when target is below this HP%.", save, v => config.Healing.ConservativeLilyHpThreshold = v);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.OgcdHeals, "oGCD Heals:"));

            ConfigUIHelpers.Toggle("Tetragrammaton", () => config.Healing.EnableTetragrammaton, v => config.Healing.EnableTetragrammaton = v, null, save,
                actionId: WHMActions.Tetragrammaton.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Benediction", () => config.Healing.EnableBenediction, v => config.Healing.EnableBenediction = v, null, save,
                actionId: WHMActions.Benediction.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Assize", () => config.Healing.EnableAssize, v => config.Healing.EnableAssize = v,
                null, save, actionId: WHMActions.Assize.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.HealingHots, "Healing HoTs:"));

            ConfigUIHelpers.Toggle("Regen", () => config.Healing.EnableRegen, v => config.Healing.EnableRegen = v, null, save,
                actionId: WHMActions.Regen.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Asylum", () => config.Healing.EnableAsylum, v => config.Healing.EnableAsylum = v,
                null, save, actionId: WHMActions.Asylum.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.Buffs, "Buffs:"));

            ConfigUIHelpers.Toggle("Presence of Mind", () => config.Buffs.EnablePresenceOfMind, v => config.Buffs.EnablePresenceOfMind = v, null, save,
                actionId: WHMActions.PresenceOfMind.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Thin Air", () => config.Buffs.EnableThinAir, v => config.Buffs.EnableThinAir = v,
                null, save, actionId: WHMActions.ThinAir.ActionId);

            ConfigUIHelpers.Toggle("Aetherial Shift", () => config.Buffs.EnableAetherialShift, v => config.Buffs.EnableAetherialShift = v,
                null, save, actionId: WHMActions.AetherialShift.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.EmergencyThresholds, "Emergency Thresholds:"));

            config.Healing.OgcdEmergencyThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.WhiteMage.OgcdEmergencyLabel, "oGCD Emergency"),
                config.Healing.OgcdEmergencyThreshold, 30f, 70f, "Use emergency oGCD heals (Tetra) when below this HP%.", save, v => config.Healing.OgcdEmergencyThreshold = v);

            config.Healing.GcdEmergencyThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.WhiteMage.GcdEmergencyLabel, "GCD Emergency"),
                config.Healing.GcdEmergencyThreshold, 20f, 60f, "Interrupt DPS to heal when below this HP%.", save, v => config.Healing.GcdEmergencyThreshold = v);

            config.Healing.BenedictionEmergencyThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.WhiteMage.BenedictionThresholdLabel, "Benediction Threshold"),
                config.Healing.BenedictionEmergencyThreshold, 10f, 50f, "Only use Benediction when target HP is below this %.", save, v => config.Healing.BenedictionEmergencyThreshold = v);

            ConfigUIHelpers.Spacing();

            config.Healing.AoEHealMinTargets = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.WhiteMage.AoEMinTargetsLabel, "AoE Min Targets"),
                config.Healing.AoEHealMinTargets, 2, 8, "Use AoE heal when this many party members need healing.", save, v => config.Healing.AoEHealMinTargets = v);

            config.Healing.AoEHealHpThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.WhiteMage.AoEHpThresholdLabel, "AoE HP Threshold"),
                config.Healing.AoEHealHpThreshold, 50f, 95f, "Count a party member as needing AoE healing when below this HP %.", save, v => config.Healing.AoEHealHpThreshold = v);

            DrawAdvancedHealingSection();

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawAdvancedHealingSection()
    {
        ConfigUIHelpers.Spacing();
        ConfigUIHelpers.Separator();

        if (ConfigUIHelpers.BeginTreeNode(Loc.T(LocalizedStrings.WhiteMage.AdvancedHealingSettings, "Advanced Healing Settings")))
        {
            // Triage Settings
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.HealingTriageLabel, "Healing Triage:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.WhiteMage.UseDamageBasedTriage, "Use Damage-Based Triage"), () => config.Healing.UseDamageIntakeTriage, v => config.Healing.UseDamageIntakeTriage = v,
                Loc.T(LocalizedStrings.WhiteMage.UseDamageBasedTriageDesc, "Prioritize healing targets taking active damage."), save);

            if (config.Healing.UseDamageIntakeTriage)
            {
                ConfigUIHelpers.BeginIndent();
                var presetNames = Enum.GetNames<TriagePreset>();
                var currentPreset = (int)config.Healing.TriagePreset;
                ImGui.SetNextItemWidth(150);
                if (ImGui.Combo(Loc.T(LocalizedStrings.WhiteMage.TriagePreset, "Triage Preset"), ref currentPreset, presetNames, presetNames.Length))
                {
                    config.Healing.TriagePreset = (TriagePreset)currentPreset;
                    save();
                }

                var presetDesc = config.Healing.TriagePreset switch
                {
                    TriagePreset.Balanced => Loc.T(LocalizedStrings.WhiteMage.TriagePresetBalanced, "Balanced weights across all factors"),
                    TriagePreset.TankFocus => Loc.T(LocalizedStrings.WhiteMage.TriagePresetTankFocus, "Prioritize tanks over DPS"),
                    TriagePreset.SpreadDamage => Loc.T(LocalizedStrings.WhiteMage.TriagePresetSpreadDamage, "React to highest damage intake"),
                    TriagePreset.RaidWide => Loc.T(LocalizedStrings.WhiteMage.TriagePresetRaidWide, "Focus on lowest HP members"),
                    TriagePreset.Custom => Loc.T(LocalizedStrings.WhiteMage.TriagePresetCustom, "Use custom weight values below"),
                    _ => ""
                };
                ImGui.TextDisabled(presetDesc);

                // Show custom weights only when Custom is selected
                if (config.Healing.TriagePreset == TriagePreset.Custom)
                {
                    config.Healing.CustomTriageWeights.DamageRate = ConfigUIHelpers.ThresholdSliderSmall(
                        "Damage Rate", config.Healing.CustomTriageWeights.DamageRate, 0f, 60f, null, save, v => config.Healing.CustomTriageWeights.DamageRate = v);
                    config.Healing.CustomTriageWeights.TankBonus = ConfigUIHelpers.ThresholdSliderSmall(
                        "Tank Bonus", config.Healing.CustomTriageWeights.TankBonus, 0f, 60f, null, save, v => config.Healing.CustomTriageWeights.TankBonus = v);
                    config.Healing.CustomTriageWeights.MissingHp = ConfigUIHelpers.ThresholdSliderSmall(
                        "Missing HP", config.Healing.CustomTriageWeights.MissingHp, 0f, 60f, null, save, v => config.Healing.CustomTriageWeights.MissingHp = v);
                    config.Healing.CustomTriageWeights.DamageAcceleration = ConfigUIHelpers.ThresholdSliderSmall(
                        "Acceleration", config.Healing.CustomTriageWeights.DamageAcceleration, 0f, 30f, null, save, v => config.Healing.CustomTriageWeights.DamageAcceleration = v);
                }
                ConfigUIHelpers.EndIndent();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.AssizeHealingLabel, "Assize Healing:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.WhiteMage.EnableAssizeForHealing, "Enable Assize for Healing"), () => config.Healing.EnableAssizeHealing, v => config.Healing.EnableAssizeHealing = v,
                Loc.T(LocalizedStrings.WhiteMage.EnableAssizeForHealingDesc, "Use Assize as a healing oGCD when party needs it."), save);

            if (config.Healing.EnableAssizeHealing)
            {
                ConfigUIHelpers.BeginIndent();
                config.Healing.AssizeHealingMinTargets = ConfigUIHelpers.IntSliderSmall("Min Injured",
                    config.Healing.AssizeHealingMinTargets, 1, 8, null, save, v => config.Healing.AssizeHealingMinTargets = v);
                config.Healing.AssizeHealingHpThreshold = ConfigUIHelpers.ThresholdSliderSmall("HP Threshold",
                    config.Healing.AssizeHealingHpThreshold, 50f, 95f, "Prioritize Assize healing when avg HP below threshold.", save, v => config.Healing.AssizeHealingHpThreshold = v);
                ConfigUIHelpers.EndIndent();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.PreemptiveHealingLabel, "Preemptive Healing:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.WhiteMage.EnablePreemptiveHealing, "Enable Preemptive Healing"), () => config.Healing.EnablePreemptiveHealing, v => config.Healing.EnablePreemptiveHealing = v,
                Loc.T(LocalizedStrings.WhiteMage.EnablePreemptiveHealingDesc, "Heal before damage spikes land based on pattern detection."), save);

            if (config.Healing.EnablePreemptiveHealing)
            {
                ConfigUIHelpers.BeginIndent();
                config.Healing.PreemptiveHealingThreshold = ConfigUIHelpers.ThresholdSliderSmall("HP Trigger",
                    config.Healing.PreemptiveHealingThreshold, 10f, 80f, "Heal if projected HP would drop below this.", save, v => config.Healing.PreemptiveHealingThreshold = v);
                config.Healing.SpikePatternConfidenceThreshold = ConfigUIHelpers.ThresholdSliderSmall("Pattern Confidence",
                    config.Healing.SpikePatternConfidenceThreshold, 30f, 95f, "Minimum confidence for spike pattern prediction.", save, v => config.Healing.SpikePatternConfidenceThreshold = v);

                config.Healing.SpikePredictionLookahead = ConfigUIHelpers.FloatSlider("Lookahead (sec)",
                    config.Healing.SpikePredictionLookahead, 0.5f, 5f, "%.1f", "How far ahead to predict spikes.", save, v => config.Healing.SpikePredictionLookahead = v);
                ConfigUIHelpers.EndIndent();
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.ExperimentalLabel, "Experimental:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.WhiteMage.EnableScoredHealSelection, "Enable Scored Heal Selection"), () => config.Healing.EnableScoredHealSelection, v => config.Healing.EnableScoredHealSelection = v,
                Loc.T(LocalizedStrings.WhiteMage.EnableScoredHealSelectionDesc, "Use multi-factor scoring instead of tier-based selection."), save);
            ConfigUIHelpers.WarningText(Loc.T(LocalizedStrings.WhiteMage.ExperimentalWarning, "EXPERIMENTAL"));

            ConfigUIHelpers.EndTreeNode();
        }
    }

    private void DrawDefensiveSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.WhiteMage.DefensiveSection, "Defensive Cooldowns"), "WHM"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.Shields, "Shields:"));

            ConfigUIHelpers.Toggle("Divine Benison", () => config.Defensive.EnableDivineBenison, v => config.Defensive.EnableDivineBenison = v, null, save,
                actionId: WHMActions.DivineBenison.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Aquaveil", () => config.Defensive.EnableAquaveil, v => config.Defensive.EnableAquaveil = v,
                null, save, actionId: WHMActions.Aquaveil.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.PartyMitigation, "Party Mitigation:"));

            ConfigUIHelpers.Toggle("Plenary Indulgence", () => config.Defensive.EnablePlenaryIndulgence, v => config.Defensive.EnablePlenaryIndulgence = v, null, save,
                actionId: WHMActions.PlenaryIndulgence.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Temperance", () => config.Defensive.EnableTemperance, v => config.Defensive.EnableTemperance = v,
                null, save, actionId: WHMActions.Temperance.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.Advanced, "Advanced:"));

            ConfigUIHelpers.Toggle("Liturgy of the Bell", () => config.Defensive.EnableLiturgyOfTheBell, v => config.Defensive.EnableLiturgyOfTheBell = v, null, save,
                actionId: WHMActions.LiturgyOfTheBell.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Divine Caress", () => config.Defensive.EnableDivineCaress, v => config.Defensive.EnableDivineCaress = v,
                null, save, actionId: WHMActions.DivineCaress.ActionId);

            ConfigUIHelpers.Spacing();

            config.Defensive.DefensiveCooldownThreshold = ConfigUIHelpers.ThresholdSlider("Defensive Threshold",
                config.Defensive.DefensiveCooldownThreshold, 50f, 95f, "Use defensives when party avg HP falls below this %.", save, v => config.Defensive.DefensiveCooldownThreshold = v);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.WhiteMage.UseWithAoEHeals, "Use with AoE Heals"), () => config.Defensive.UseDefensivesWithAoEHeals, v => config.Defensive.UseDefensivesWithAoEHeals = v,
                Loc.T(LocalizedStrings.WhiteMage.UseWithAoEHealsDesc, "Sync Plenary Indulgence with AoE healing for bonus potency."), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.WhiteMage.DamageSection, "Damage"), "WHM"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.WhiteMage.EnableDamage, "Enable Damage"), () => config.EnableDamage, v => config.EnableDamage = v, null, save);

            ConfigUIHelpers.BeginDisabledGroup(!config.EnableDamage);

            // DPS Priority Mode
            var currentPriority = (int)config.Damage.DpsPriority;
            ImGui.SetNextItemWidth(150);
            var dpsPriorityNames = GetDpsPriorityNames();
            if (ImGui.Combo(Loc.T(LocalizedStrings.WhiteMage.DpsPriority, "DPS Priority"), ref currentPriority, dpsPriorityNames, dpsPriorityNames.Length))
            {
                config.Damage.DpsPriority = (DpsPriorityMode)currentPriority;
                save();
            }
            var priorityDesc = config.Damage.DpsPriority switch
            {
                DpsPriorityMode.HealFirst => Loc.T(LocalizedStrings.WhiteMage.DpsPriorityHealFirstDesc, "Safest - only DPS when party is healthy"),
                DpsPriorityMode.Balanced => Loc.T(LocalizedStrings.WhiteMage.DpsPriorityBalancedDesc, "Moderate - more aggressive DPS while healing"),
                DpsPriorityMode.DpsFirst => Loc.T(LocalizedStrings.WhiteMage.DpsPriorityDpsFirstDesc, "Maximum DPS - minimal proactive healing"),
                _ => ""
            };
            ImGui.TextDisabled(priorityDesc);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.StoneProgression, "Stone Progression:"));

            ConfigUIHelpers.Toggle("Stone", () => config.Damage.EnableStone, v => config.Damage.EnableStone = v, null, save,
                actionId: WHMActions.Stone.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Stone II", () => config.Damage.EnableStoneII, v => config.Damage.EnableStoneII = v, null, save,
                actionId: WHMActions.StoneII.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Stone III", () => config.Damage.EnableStoneIII, v => config.Damage.EnableStoneIII = v, null, save,
                actionId: WHMActions.StoneIII.ActionId);

            ConfigUIHelpers.Toggle("Stone IV", () => config.Damage.EnableStoneIV, v => config.Damage.EnableStoneIV = v, null, save,
                actionId: WHMActions.StoneIV.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.GlareProgression, "Glare Progression:"));

            ConfigUIHelpers.Toggle("Glare", () => config.Damage.EnableGlare, v => config.Damage.EnableGlare = v, null, save,
                actionId: WHMActions.Glare.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Glare III", () => config.Damage.EnableGlareIII, v => config.Damage.EnableGlareIII = v, null, save,
                actionId: WHMActions.GlareIII.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Glare IV", () => config.Damage.EnableGlareIV, v => config.Damage.EnableGlareIV = v, null, save,
                actionId: WHMActions.GlareIV.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.AoEDamage, "AoE Damage:"));

            ConfigUIHelpers.Toggle("Holy", () => config.Damage.EnableHoly, v => config.Damage.EnableHoly = v, null, save,
                actionId: WHMActions.Holy.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Holy III", () => config.Damage.EnableHolyIII, v => config.Damage.EnableHolyIII = v,
                null, save, actionId: WHMActions.HolyIII.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.WhiteMage.BloodLily, "Blood Lily:"));

            ConfigUIHelpers.Toggle("Afflatus Misery", () => config.Damage.EnableAfflatusMisery, v => config.Damage.EnableAfflatusMisery = v,
                Loc.T(LocalizedStrings.WhiteMage.MiseryDesc, "1240p AoE damage (costs 3 Blood Lilies). Use at 3 stacks."), save);

            ConfigUIHelpers.Spacing();

            config.Damage.AoEDamageMinTargets = ConfigUIHelpers.IntSlider("AoE Min Enemies",
                config.Damage.AoEDamageMinTargets, 2, 8, "Use Holy when this many enemies are within range.", save, v => config.Damage.AoEDamageMinTargets = v);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDoTSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.WhiteMage.DoTSection, "DoT"), "WHM"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.WhiteMage.EnableDoT, "Enable DoT"), () => config.EnableDoT, v => config.EnableDoT = v, null, save);

            ConfigUIHelpers.BeginDisabledGroup(!config.EnableDoT);

            ConfigUIHelpers.Toggle("Aero", () => config.Dot.EnableAero, v => config.Dot.EnableAero = v, null, save,
                actionId: WHMActions.Aero.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Aero II", () => config.Dot.EnableAeroII, v => config.Dot.EnableAeroII = v, null, save,
                actionId: WHMActions.AeroII.ActionId);

            ImGui.SameLine();
            ConfigUIHelpers.Toggle("Dia", () => config.Dot.EnableDia, v => config.Dot.EnableDia = v, null, save,
                actionId: WHMActions.Dia.ActionId);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }
}
