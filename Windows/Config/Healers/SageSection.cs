using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
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
                    config.Sage.KardiaSwapThreshold, 30f, 80f, Loc.T(LocalizedStrings.Sage.SwapThresholdDesc, "Swap to target below this HP if current target is above it."), save, v => config.Sage.KardiaSwapThreshold = v);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableSoteria, "Enable Soteria"), () => config.Sage.EnableSoteria, v => config.Sage.EnableSoteria = v,
                null, save, actionId: SGEActions.Soteria.ActionId);

            if (config.Sage.EnableSoteria)
            {
                config.Sage.SoteriaThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Sage.SoteriaThreshold, "Soteria Threshold"),
                    config.Sage.SoteriaThreshold, 40f, 85f, Loc.T(LocalizedStrings.Sage.SoteriaThresholdDesc, "Kardia target HP to trigger Soteria."), save, v => config.Sage.SoteriaThreshold = v);
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
                Loc.T(LocalizedStrings.Sage.StackReserveDesc, "Stacks to keep for emergency healing."), save, v => config.Sage.AddersgallReserve = v);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.PreventAddersgallCap, "Prevent Addersgall Cap"), () => config.Sage.PreventAddersgallCap, v => config.Sage.PreventAddersgallCap = v,
                Loc.T(LocalizedStrings.Sage.PreventAddersgallCapDesc, "Spend stacks proactively to avoid capping."), save);

            if (config.Sage.PreventAddersgallCap)
            {
                config.Sage.AddersgallCapPreventWindow = ConfigUIHelpers.FloatSlider(Loc.T(LocalizedStrings.Sage.CapPreventionWindow, "Cap Prevention Window"),
                    config.Sage.AddersgallCapPreventWindow, 0f, 10f, "%.1f sec",
                    Loc.T(LocalizedStrings.Sage.CapPreventionWindowDesc, "Start spending when new stack would be granted within this time."), save, v => config.Sage.AddersgallCapPreventWindow = v);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableRhizomata, "Enable Rhizomata"), () => config.Sage.EnableRhizomata, v => config.Sage.EnableRhizomata = v,
                null, save, actionId: SGEActions.Rhizomata.ActionId);

            if (config.Sage.EnableRhizomata)
            {
                config.Sage.RhizomataMinFreeSlots = ConfigUIHelpers.IntSliderSmall(Loc.T(LocalizedStrings.Sage.RhizomataMinFreeSlots, "Min Free Slots"),
                    config.Sage.RhizomataMinFreeSlots, 1, 3,
                    Loc.T(LocalizedStrings.Sage.RhizomataMinFreeSlotsDesc, "Only use when this many slots are free."), save, v => config.Sage.RhizomataMinFreeSlots = v);
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
                null, save, actionId: SGEActions.Diagnosis.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableEukrasianDiagnosis, "Enable Eukrasian Diagnosis"), () => config.Sage.EnableEukrasianDiagnosis, v => config.Sage.EnableEukrasianDiagnosis = v,
                null, save, actionId: SGEActions.EukrasianDiagnosis.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePrognosis, "Enable Prognosis"), () => config.Sage.EnablePrognosis, v => config.Sage.EnablePrognosis = v,
                null, save, actionId: SGEActions.Prognosis.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableEukrasianPrognosis, "Enable Eukrasian Prognosis"), () => config.Sage.EnableEukrasianPrognosis, v => config.Sage.EnableEukrasianPrognosis = v,
                null, save, actionId: SGEActions.EukrasianPrognosis.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.AddersgallHeals, "Addersgall Heals:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableDruochole, "Enable Druochole"), () => config.Sage.EnableDruochole, v => config.Sage.EnableDruochole = v,
                null, save, actionId: SGEActions.Druochole.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableTaurochole, "Enable Taurochole"), () => config.Sage.EnableTaurochole, v => config.Sage.EnableTaurochole = v,
                null, save, actionId: SGEActions.Taurochole.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableIxochole, "Enable Ixochole"), () => config.Sage.EnableIxochole, v => config.Sage.EnableIxochole = v,
                null, save, actionId: SGEActions.Ixochole.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableKerachole, "Enable Kerachole"), () => config.Sage.EnableKerachole, v => config.Sage.EnableKerachole = v,
                null, save, actionId: SGEActions.Kerachole.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.FreeOgcds, "Free oGCDs:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePhysisII, "Enable Physis II"), () => config.Sage.EnablePhysisII, v => config.Sage.EnablePhysisII = v,
                null, save, actionId: SGEActions.PhysisII.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableHolos, "Enable Holos"), () => config.Sage.EnableHolos, v => config.Sage.EnableHolos = v,
                null, save, actionId: SGEActions.Holos.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePepsis, "Enable Pepsis"), () => config.Sage.EnablePepsis, v => config.Sage.EnablePepsis = v,
                null, save, actionId: SGEActions.Pepsis.ActionId);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePneuma, "Enable Pneuma"), () => config.Sage.EnablePneuma, v => config.Sage.EnablePneuma = v,
                null, save, actionId: SGEActions.Pneuma.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.SingleTargetThresholds, "Single-Target Thresholds:"));

            config.Sage.DiagnosisThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.DiagnosisThreshold, "Diagnosis Threshold"),
                config.Sage.DiagnosisThreshold, 20f, 60f, null, save, v => config.Sage.DiagnosisThreshold = v);
            config.Sage.EukrasianDiagnosisThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.EukrasianDiagnosisThreshold, "E.Diagnosis Threshold"),
                config.Sage.EukrasianDiagnosisThreshold, 50f, 95f, null, save, v => config.Sage.EukrasianDiagnosisThreshold = v);
            config.Sage.DruocholeThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.DruocholeThreshold, "Druochole Threshold"),
                config.Sage.DruocholeThreshold, 30f, 75f, null, save, v => config.Sage.DruocholeThreshold = v);
            config.Sage.TaurocholeThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.TaurocholeThreshold, "Taurochole Threshold"),
                config.Sage.TaurocholeThreshold, 30f, 75f, null, save, v => config.Sage.TaurocholeThreshold = v);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.AoEThresholds, "AoE Thresholds:"));

            config.Sage.AoEHealThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.AoEHpThreshold, "AoE HP Threshold"),
                config.Sage.AoEHealThreshold, 50f, 90f, null, save, v => config.Sage.AoEHealThreshold = v);

            config.Sage.AoEHealMinTargets = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Sage.AoEMinTargets, "AoE Min Targets"),
                config.Sage.AoEHealMinTargets, 1, 8, null, save, v => config.Sage.AoEHealMinTargets = v);

            config.Sage.KeracholeThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.KeracholeThreshold, "Kerachole Threshold"),
                config.Sage.KeracholeThreshold, 60f, 95f, null, save, v => config.Sage.KeracholeThreshold = v);
            config.Sage.IxocholeThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.IxocholeThreshold, "Ixochole Threshold"),
                config.Sage.IxocholeThreshold, 40f, 85f, null, save, v => config.Sage.IxocholeThreshold = v);
            config.Sage.PhysisIIThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.PhysisIIThreshold, "Physis II Threshold"),
                config.Sage.PhysisIIThreshold, 60f, 95f, null, save, v => config.Sage.PhysisIIThreshold = v);
            config.Sage.HolosThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.HolosThreshold, "Holos Threshold"),
                config.Sage.HolosThreshold, 40f, 80f, null, save, v => config.Sage.HolosThreshold = v);
            config.Sage.PneumaThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.PneumaThreshold, "Pneuma Threshold"),
                config.Sage.PneumaThreshold, 40f, 85f, null, save, v => config.Sage.PneumaThreshold = v);
            config.Sage.PepsisThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.PepsisThreshold, "Pepsis Threshold"),
                config.Sage.PepsisThreshold, 30f, 70f, Loc.T(LocalizedStrings.Sage.PepsisThresholdDesc, "Converts shields to healing when party HP drops below this."), save, v => config.Sage.PepsisThreshold = v);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawShieldSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Sage.ShieldsSection, "Shields"), "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableHaima, "Enable Haima"), () => config.Sage.EnableHaima, v => config.Sage.EnableHaima = v,
                null, save, actionId: SGEActions.Haima.ActionId);

            if (config.Sage.EnableHaima)
            {
                config.Sage.HaimaThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Sage.HaimaThreshold, "Haima Threshold"),
                    config.Sage.HaimaThreshold, 60f, 95f, Loc.T(LocalizedStrings.Sage.HaimaThresholdDesc, "Apply to tank when HP below this."), save, v => config.Sage.HaimaThreshold = v);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePanhaima, "Enable Panhaima"), () => config.Sage.EnablePanhaima, v => config.Sage.EnablePanhaima = v,
                null, save, actionId: SGEActions.Panhaima.ActionId);

            if (config.Sage.EnablePanhaima)
            {
                config.Sage.PanhaimaThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Sage.PanhaimaThreshold, "Panhaima Threshold"),
                    config.Sage.PanhaimaThreshold, 65f, 95f, Loc.T(LocalizedStrings.Sage.PanhaimaThresholdDesc, "Use when party average HP below this."), save, v => config.Sage.PanhaimaThreshold = v);
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
                null, save, actionId: SGEActions.Zoe.ActionId);

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
                null, save, actionId: SGEActions.Krasis.ActionId);

            if (config.Sage.EnableKrasis)
            {
                config.Sage.KrasisThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Sage.KrasisThreshold, "Krasis Threshold"),
                    config.Sage.KrasisThreshold, 40f, 85f, Loc.T(LocalizedStrings.Sage.KrasisThresholdDesc, "Apply when target HP below this."), save, v => config.Sage.KrasisThreshold = v);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePhilosophia, "Enable Philosophia"), () => config.Sage.EnablePhilosophia, v => config.Sage.EnablePhilosophia = v,
                null, save, actionId: SGEActions.Philosophia.ActionId);

            if (config.Sage.EnablePhilosophia)
            {
                config.Sage.PhilosophiaThreshold = ConfigUIHelpers.ThresholdSliderSmall(Loc.T(LocalizedStrings.Sage.PhilosophiaThreshold, "Philosophia Threshold"),
                    config.Sage.PhilosophiaThreshold, 50f, 90f, Loc.T(LocalizedStrings.Sage.PhilosophiaThresholdDesc, "Use when party average HP below this."), save, v => config.Sage.PhilosophiaThreshold = v);
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
                    config.Sage.DotRefreshThreshold, 0f, 10f, "%.1f", null, save, v => config.Sage.DotRefreshThreshold = v);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.AoEDamage, "AoE:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableDyskrasia, "Enable Dyskrasia"), () => config.Sage.EnableAoEDamage, v => config.Sage.EnableAoEDamage = v,
                Loc.T(LocalizedStrings.Sage.DyskrasiaDesc, "Instant AoE damage around self."), save);

            if (config.Sage.EnableAoEDamage)
            {
                config.Sage.AoEDamageMinTargets = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Sage.AoEMinEnemies, "Min Enemies"),
                    config.Sage.AoEDamageMinTargets, 1, 10, null, save, v => config.Sage.AoEDamageMinTargets = v);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.SpecialAbilities, "Special Abilities:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePhlegma, "Enable Phlegma"), () => config.Sage.EnablePhlegma, v => config.Sage.EnablePhlegma = v,
                Loc.T(LocalizedStrings.Sage.PhlegmaDesc, "Instant damage with charges."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableToxikon, "Enable Toxikon"), () => config.Sage.EnableToxikon, v => config.Sage.EnableToxikon = v,
                Loc.T(LocalizedStrings.Sage.ToxikonDesc, "Consumes Addersting (from broken E.Diag shields)."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnablePsyche, "Enable Psyche"), () => config.Sage.EnablePsyche, v => config.Sage.EnablePsyche = v,
                null, save, actionId: SGEActions.Psyche.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Sage.MpManagement, "MP Management:"));

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Sage.EnableLucidDreaming, "Enable Lucid Dreaming"), () => config.Sage.EnableLucidDreaming, v => config.Sage.EnableLucidDreaming = v, null, save);

            if (config.Sage.EnableLucidDreaming)
            {
                config.Sage.LucidDreamingThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Sage.LucidMpThreshold, "Lucid MP Threshold"),
                    config.Sage.LucidDreamingThreshold, 40f, 90f, null, save, v => config.Sage.LucidDreamingThreshold = v);
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
