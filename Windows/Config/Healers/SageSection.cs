using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;

namespace Olympus.Windows.Config.Healers;

/// <summary>
/// Renders the Sage (Asclepius) settings section.
/// </summary>
public sealed class SageSection
{
    private readonly Configuration config;
    private readonly Action save;

    public SageSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Sage", "Asclepius", ConfigUIHelpers.SageColor);

        DrawKardiaSection();
        DrawAddersgallSection();
        DrawHealingSection();
        DrawShieldSection();
        DrawBuffSection();
        DrawDamageSection();
    }

    private void DrawKardiaSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Sage.KardiaSection, "Kardia"), "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.AutoApplyKardia, "Auto-Apply Kardia"), () => config.Sage.AutoKardia, v => config.Sage.AutoKardia = v,
                Loc.T(LocalizedStrings.Sage.AutoApplyKardiaDesc, "Automatically place Kardia on a party member."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableKardiaSwapping, "Enable Kardia Swapping"), () => config.Sage.KardiaSwapEnabled, v => config.Sage.KardiaSwapEnabled = v,
                Loc.T(LocalizedStrings.Sage.EnableKardiaSwappingDesc, "Allow swapping Kardia target during combat."), save);

            if (config.Sage.KardiaSwapEnabled)
            {
                config.Sage.KardiaSwapThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Sage.SwapThreshold, "Swap Threshold"),
                    config.Sage.KardiaSwapThreshold, 30f, 80f, Loc.T(LocalizedStrings.Sage.SwapThresholdDesc, "Swap to target below this HP if current target is above it."), save);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableSoteria, "Enable Soteria"), () => config.Sage.EnableSoteria, v => config.Sage.EnableSoteria = v,
                Loc.T(LocalizedStrings.Sage.SoteriaDesc, "Boosts Kardia healing by 70%."), save);

            if (config.Sage.EnableSoteria)
            {
                config.Sage.SoteriaThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Sage.SoteriaThreshold, "Soteria Threshold"),
                    config.Sage.SoteriaThreshold, 40f, 85f, Loc.T(LocalizedStrings.Sage.SoteriaThresholdDesc, "Kardia target HP to trigger Soteria."), save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawAddersgallSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Sage.AddersgallSection, "Addersgall"), "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            config.Sage.AddersgallReserve = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Sage.StackReserve, "Stack Reserve"),
                config.Sage.AddersgallReserve, 0, 3,
                Loc.T(LocalizedStrings.Sage.StackReserveDesc, "Stacks to keep for emergency healing."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.PreventAddersgallCap, "Prevent Addersgall Cap"), () => config.Sage.PreventAddersgallCap, v => config.Sage.PreventAddersgallCap = v,
                Loc.T(LocalizedStrings.Sage.PreventAddersgallCapDesc, "Spend stacks proactively to avoid capping."), save);

            if (config.Sage.PreventAddersgallCap)
            {
                config.Sage.AddersgallCapPreventWindow = ConfigUIHelpers.FloatSlider(Loc.T(LocalizedStrings.Sage.CapPreventionWindow, "Cap Prevention Window"),
                    config.Sage.AddersgallCapPreventWindow, 0f, 10f, "%.1f sec",
                    Loc.T(LocalizedStrings.Sage.CapPreventionWindowDesc, "Start spending when new stack would be granted within this time."), save);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableRhizomata, "Enable Rhizomata"), () => config.Sage.EnableRhizomata, v => config.Sage.EnableRhizomata = v,
                Loc.T(LocalizedStrings.Sage.RhizomataDesc, "Generate additional Addersgall stacks."), save);

            if (config.Sage.EnableRhizomata)
            {
                config.Sage.RhizomataMinFreeSlots = ConfigUIHelpers.IntSliderSmall(Loc.T(LocalizedStrings.Sage.RhizomataMinFreeSlots, "Min Free Slots"),
                    config.Sage.RhizomataMinFreeSlots, 1, 3,
                    Loc.T(LocalizedStrings.Sage.RhizomataMinFreeSlotsDesc, "Only use when this many slots are free."), save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawHealingSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Sage.HealingSection, "Healing"), "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.GcdHeals, "GCD Heals:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableDiagnosis, "Enable Diagnosis"), () => config.Sage.EnableDiagnosis, v => config.Sage.EnableDiagnosis = v,
                Loc.T(LocalizedStrings.Sage.DiagnosisDesc, "Basic GCD heal. Generally avoided in favor of oGCDs."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableEukrasianDiagnosis, "Enable Eukrasian Diagnosis"), () => config.Sage.EnableEukrasianDiagnosis, v => config.Sage.EnableEukrasianDiagnosis = v,
                Loc.T(LocalizedStrings.Sage.EukrasianDiagnosisDesc, "Single-target shield."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePrognosis, "Enable Prognosis"), () => config.Sage.EnablePrognosis, v => config.Sage.EnablePrognosis = v,
                Loc.T(LocalizedStrings.Sage.PrognosisDesc, "Basic AoE heal."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableEukrasianPrognosis, "Enable Eukrasian Prognosis"), () => config.Sage.EnableEukrasianPrognosis, v => config.Sage.EnableEukrasianPrognosis = v,
                Loc.T(LocalizedStrings.Sage.EukrasianPrognosisDesc, "AoE shield."), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.AddersgallHeals, "Addersgall Heals:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableDruochole, "Enable Druochole"), () => config.Sage.EnableDruochole, v => config.Sage.EnableDruochole = v,
                Loc.T(LocalizedStrings.Sage.DruocholeDesc, "oGCD single-target heal."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableTaurochole, "Enable Taurochole"), () => config.Sage.EnableTaurochole, v => config.Sage.EnableTaurochole = v,
                Loc.T(LocalizedStrings.Sage.TaurocholeDesc, "oGCD heal + 10% mitigation."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableIxochole, "Enable Ixochole"), () => config.Sage.EnableIxochole, v => config.Sage.EnableIxochole = v,
                Loc.T(LocalizedStrings.Sage.IxocholeDesc, "oGCD AoE heal."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableKerachole, "Enable Kerachole"), () => config.Sage.EnableKerachole, v => config.Sage.EnableKerachole = v,
                Loc.T(LocalizedStrings.Sage.KeracholeDesc, "oGCD AoE HoT + 10% mitigation."), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.FreeOgcds, "Free oGCDs:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePhysisII, "Enable Physis II"), () => config.Sage.EnablePhysisII, v => config.Sage.EnablePhysisII = v,
                Loc.T(LocalizedStrings.Sage.PhysisIIDesc, "Free AoE HoT + healing received buff."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableHolos, "Enable Holos"), () => config.Sage.EnableHolos, v => config.Sage.EnableHolos = v,
                Loc.T(LocalizedStrings.Sage.HolosDesc, "Free AoE heal + shield + mitigation."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePepsis, "Enable Pepsis"), () => config.Sage.EnablePepsis, v => config.Sage.EnablePepsis = v,
                Loc.T(LocalizedStrings.Sage.PepsisDesc, "Converts shields to direct healing."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePneuma, "Enable Pneuma"), () => config.Sage.EnablePneuma, v => config.Sage.EnablePneuma = v,
                Loc.T(LocalizedStrings.Sage.PneumaDesc, "Line AoE damage + party heal."), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.SingleTargetThresholds, "Single-Target Thresholds:"));

            config.Sage.DiagnosisThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.DiagnosisThreshold, "Diagnosis Threshold"),
                config.Sage.DiagnosisThreshold, 20f, 60f, null, save);
            config.Sage.EukrasianDiagnosisThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.EukrasianDiagnosisThreshold, "E.Diagnosis Threshold"),
                config.Sage.EukrasianDiagnosisThreshold, 50f, 95f, null, save);
            config.Sage.DruocholeThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.DruocholeThreshold, "Druochole Threshold"),
                config.Sage.DruocholeThreshold, 30f, 75f, null, save);
            config.Sage.TaurocholeThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.TaurocholeThreshold, "Taurochole Threshold"),
                config.Sage.TaurocholeThreshold, 30f, 75f, null, save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.AoEThresholds, "AoE Thresholds:"));

            config.Sage.AoEHealThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.AoEHpThreshold, "AoE HP Threshold"),
                config.Sage.AoEHealThreshold, 50f, 90f, null, save);

            config.Sage.AoEHealMinTargets = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Sage.AoEMinTargets, "AoE Min Targets"),
                config.Sage.AoEHealMinTargets, 1, 8, null, save);

            config.Sage.KeracholeThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.KeracholeThreshold, "Kerachole Threshold"),
                config.Sage.KeracholeThreshold, 60f, 95f, null, save);
            config.Sage.IxocholeThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.IxocholeThreshold, "Ixochole Threshold"),
                config.Sage.IxocholeThreshold, 40f, 85f, null, save);
            config.Sage.PhysisIIThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.PhysisIIThreshold, "Physis II Threshold"),
                config.Sage.PhysisIIThreshold, 60f, 95f, null, save);
            config.Sage.HolosThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.HolosThreshold, "Holos Threshold"),
                config.Sage.HolosThreshold, 40f, 80f, null, save);
            config.Sage.PneumaThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.PneumaThreshold, "Pneuma Threshold"),
                config.Sage.PneumaThreshold, 40f, 85f, null, save);
            config.Sage.PepsisThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.PepsisThreshold, "Pepsis Threshold"),
                config.Sage.PepsisThreshold, 30f, 70f, Loc.T(LocalizedStrings.Sage.PepsisThresholdDesc, "Converts shields to healing when party HP drops below this."), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawShieldSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Sage.ShieldsSection, "Shields"), "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableHaima, "Enable Haima"), () => config.Sage.EnableHaima, v => config.Sage.EnableHaima = v,
                Loc.T(LocalizedStrings.Sage.HaimaDesc, "Multi-hit single-target shield (6 stacks)."), save);

            if (config.Sage.EnableHaima)
            {
                config.Sage.HaimaThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Sage.HaimaThreshold, "Haima Threshold"),
                    config.Sage.HaimaThreshold, 60f, 95f, Loc.T(LocalizedStrings.Sage.HaimaThresholdDesc, "Apply to tank when HP below this."), save);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePanhaima, "Enable Panhaima"), () => config.Sage.EnablePanhaima, v => config.Sage.EnablePanhaima = v,
                Loc.T(LocalizedStrings.Sage.PanhaimaDesc, "Multi-hit party shield (5 stacks)."), save);

            if (config.Sage.EnablePanhaima)
            {
                config.Sage.PanhaimaThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Sage.PanhaimaThreshold, "Panhaima Threshold"),
                    config.Sage.PanhaimaThreshold, 65f, 95f, Loc.T(LocalizedStrings.Sage.PanhaimaThresholdDesc, "Use when party average HP below this."), save);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.AvoidOverwritingShields, "Avoid Overwriting Shields"), () => config.Sage.AvoidOverwritingShields, v => config.Sage.AvoidOverwritingShields = v,
                Loc.T(LocalizedStrings.Sage.AvoidOverwritingShieldsDesc, "Don't apply new shields over existing ones."), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBuffSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Sage.BuffsSection, "Buffs"), "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableZoe, "Enable Zoe"), () => config.Sage.EnableZoe, v => config.Sage.EnableZoe = v,
                Loc.T(LocalizedStrings.Sage.ZoeDesc, "+50% next GCD heal potency."), save);

            if (config.Sage.EnableZoe)
            {
                var zoeNames = Enum.GetNames<ZoeUsageStrategy>();
                var currentZoe = (int)config.Sage.ZoeStrategy;
                ImGui.SetNextItemWidth(180);
                if (ImGui.Combo(Loc.T(LocalizedStrings.Sage.ZoeStrategy, "Zoe Strategy"), ref currentZoe, zoeNames, zoeNames.Length))
                {
                    config.Sage.ZoeStrategy = (ZoeUsageStrategy)currentZoe;
                    save();
                }

                var zoeDesc = config.Sage.ZoeStrategy switch
                {
                    ZoeUsageStrategy.WithPneuma => Loc.T(LocalizedStrings.Sage.ZoeWithPneuma, "Save for Pneuma"),
                    ZoeUsageStrategy.WithEukrasianPrognosis => Loc.T(LocalizedStrings.Sage.ZoeWithEukrasianPrognosis, "Save for E.Prognosis shield"),
                    ZoeUsageStrategy.OnDemand => Loc.T(LocalizedStrings.Sage.ZoeOnDemand, "Use immediately when healing needed"),
                    ZoeUsageStrategy.Manual => Loc.T(LocalizedStrings.Sage.ZoeManual, "Manual control only"),
                    _ => ""
                };
                ImGui.TextDisabled(zoeDesc);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableKrasis, "Enable Krasis"), () => config.Sage.EnableKrasis, v => config.Sage.EnableKrasis = v,
                Loc.T(LocalizedStrings.Sage.KrasisDesc, "+20% healing received on target."), save);

            if (config.Sage.EnableKrasis)
            {
                config.Sage.KrasisThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Sage.KrasisThreshold, "Krasis Threshold"),
                    config.Sage.KrasisThreshold, 40f, 85f, Loc.T(LocalizedStrings.Sage.KrasisThresholdDesc, "Apply when target HP below this."), save);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePhilosophia, "Enable Philosophia"), () => config.Sage.EnablePhilosophia, v => config.Sage.EnablePhilosophia = v,
                Loc.T(LocalizedStrings.Sage.PhilosophiaDesc, "Party-wide Kardia effect."), save);

            if (config.Sage.EnablePhilosophia)
            {
                config.Sage.PhilosophiaThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Sage.PhilosophiaThreshold, "Philosophia Threshold"),
                    config.Sage.PhilosophiaThreshold, 50f, 90f, Loc.T(LocalizedStrings.Sage.PhilosophiaThresholdDesc, "Use when party average HP below this."), save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Sage.DamageSection, "Damage"), "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.SingleTargetDamage, "Single-Target:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableDosis, "Enable Dosis"), () => config.Sage.EnableSingleTargetDamage, v => config.Sage.EnableSingleTargetDamage = v,
                Loc.T(LocalizedStrings.Sage.DosisDesc, "Casted single-target damage. Triggers Kardia healing."), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.DotLabel, "DoT:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableEukrasianDosis, "Enable Eukrasian Dosis"), () => config.Sage.EnableDot, v => config.Sage.EnableDot = v,
                Loc.T(LocalizedStrings.Sage.EukrasianDosisDesc, "Instant DoT that triggers Kardia."), save);

            if (config.Sage.EnableDot)
            {
                config.Sage.DotRefreshThreshold = ConfigUIHelpers.FloatSlider(Loc.T(LocalizedStrings.Sage.DotRefreshThreshold, "DoT Refresh (sec)"),
                    config.Sage.DotRefreshThreshold, 0f, 10f, "%.1f", null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.AoEDamage, "AoE:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableDyskrasia, "Enable Dyskrasia"), () => config.Sage.EnableAoEDamage, v => config.Sage.EnableAoEDamage = v,
                Loc.T(LocalizedStrings.Sage.DyskrasiaDesc, "Instant AoE damage around self."), save);

            if (config.Sage.EnableAoEDamage)
            {
                config.Sage.AoEDamageMinTargets = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Sage.AoEMinEnemies, "Min Enemies"),
                    config.Sage.AoEDamageMinTargets, 1, 10, null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.SpecialAbilities, "Special Abilities:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePhlegma, "Enable Phlegma"), () => config.Sage.EnablePhlegma, v => config.Sage.EnablePhlegma = v,
                Loc.T(LocalizedStrings.Sage.PhlegmaDesc, "Instant damage with charges."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableToxikon, "Enable Toxikon"), () => config.Sage.EnableToxikon, v => config.Sage.EnableToxikon = v,
                Loc.T(LocalizedStrings.Sage.ToxikonDesc, "Consumes Addersting (from broken E.Diag shields)."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePsyche, "Enable Psyche"), () => config.Sage.EnablePsyche, v => config.Sage.EnablePsyche = v,
                Loc.T(LocalizedStrings.Sage.PsycheDesc, "oGCD damage ability."), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.MpManagement, "MP Management:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableLucidDreaming, "Enable Lucid Dreaming"), () => config.Sage.EnableLucidDreaming, v => config.Sage.EnableLucidDreaming = v, null, save);

            if (config.Sage.EnableLucidDreaming)
            {
                config.Sage.LucidDreamingThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.LucidMpThreshold, "Lucid MP Threshold"),
                    config.Sage.LucidDreamingThreshold, 40f, 90f, null, save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
