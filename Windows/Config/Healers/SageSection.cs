using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;

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
        if (ConfigUIHelpers.SectionHeader("Kardia", "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            var autoKardia = config.Sage.AutoKardia;
            if (ImGui.Checkbox("Auto-Apply Kardia", ref autoKardia))
            {
                config.Sage.AutoKardia = autoKardia;
                save();
            }
            ImGui.TextDisabled("Automatically place Kardia on a party member.");

            var kardiaSwap = config.Sage.KardiaSwapEnabled;
            if (ImGui.Checkbox("Enable Kardia Swapping", ref kardiaSwap))
            {
                config.Sage.KardiaSwapEnabled = kardiaSwap;
                save();
            }
            ImGui.TextDisabled("Allow swapping Kardia target during combat.");

            if (config.Sage.KardiaSwapEnabled)
            {
                config.Sage.KardiaSwapThreshold = ConfigUIHelpers.ThresholdSliderSmall("Swap Threshold",
                    config.Sage.KardiaSwapThreshold, 30f, 80f, "Swap to target below this HP if current target is above it.", save);
            }

            ConfigUIHelpers.Spacing();

            var enableSoteria = config.Sage.EnableSoteria;
            if (ImGui.Checkbox("Enable Soteria", ref enableSoteria))
            {
                config.Sage.EnableSoteria = enableSoteria;
                save();
            }
            ImGui.TextDisabled("Boosts Kardia healing by 70%.");

            if (config.Sage.EnableSoteria)
            {
                config.Sage.SoteriaThreshold = ConfigUIHelpers.ThresholdSliderSmall("Soteria Threshold",
                    config.Sage.SoteriaThreshold, 40f, 85f, "Kardia target HP to trigger Soteria.", save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawAddersgallSection()
    {
        if (ConfigUIHelpers.SectionHeader("Addersgall", "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            config.Sage.AddersgallReserve = ConfigUIHelpers.IntSlider("Stack Reserve",
                config.Sage.AddersgallReserve, 0, 3,
                "Stacks to keep for emergency healing.", save);

            var preventCap = config.Sage.PreventAddersgallCap;
            if (ImGui.Checkbox("Prevent Addersgall Cap", ref preventCap))
            {
                config.Sage.PreventAddersgallCap = preventCap;
                save();
            }
            ImGui.TextDisabled("Spend stacks proactively to avoid capping.");

            if (config.Sage.PreventAddersgallCap)
            {
                config.Sage.AddersgallCapPreventWindow = ConfigUIHelpers.FloatSlider("Cap Prevention Window",
                    config.Sage.AddersgallCapPreventWindow, 0f, 10f, "%.1f sec",
                    "Start spending when new stack would be granted within this time.", save);
            }

            ConfigUIHelpers.Spacing();

            var enableRhizo = config.Sage.EnableRhizomata;
            if (ImGui.Checkbox("Enable Rhizomata", ref enableRhizo))
            {
                config.Sage.EnableRhizomata = enableRhizo;
                save();
            }
            ImGui.TextDisabled("Generate additional Addersgall stacks.");

            if (config.Sage.EnableRhizomata)
            {
                config.Sage.RhizomataMinFreeSlots = ConfigUIHelpers.IntSliderSmall("Min Free Slots",
                    config.Sage.RhizomataMinFreeSlots, 1, 3,
                    "Only use when this many slots are free.", save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawHealingSection()
    {
        if (ConfigUIHelpers.SectionHeader("Healing", "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("GCD Heals:");

            var enableDiag = config.Sage.EnableDiagnosis;
            if (ImGui.Checkbox("Enable Diagnosis", ref enableDiag))
            {
                config.Sage.EnableDiagnosis = enableDiag;
                save();
            }
            ImGui.TextDisabled("Basic GCD heal. Generally avoided in favor of oGCDs.");

            var enableEDiag = config.Sage.EnableEukrasianDiagnosis;
            if (ImGui.Checkbox("Enable Eukrasian Diagnosis", ref enableEDiag))
            {
                config.Sage.EnableEukrasianDiagnosis = enableEDiag;
                save();
            }
            ImGui.TextDisabled("Single-target shield.");

            var enableProg = config.Sage.EnablePrognosis;
            if (ImGui.Checkbox("Enable Prognosis", ref enableProg))
            {
                config.Sage.EnablePrognosis = enableProg;
                save();
            }
            ImGui.TextDisabled("Basic AoE heal.");

            var enableEProg = config.Sage.EnableEukrasianPrognosis;
            if (ImGui.Checkbox("Enable Eukrasian Prognosis", ref enableEProg))
            {
                config.Sage.EnableEukrasianPrognosis = enableEProg;
                save();
            }
            ImGui.TextDisabled("AoE shield.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Addersgall Heals:");

            var enableDruo = config.Sage.EnableDruochole;
            if (ImGui.Checkbox("Enable Druochole", ref enableDruo))
            {
                config.Sage.EnableDruochole = enableDruo;
                save();
            }
            ImGui.TextDisabled("oGCD single-target heal.");

            var enableTauro = config.Sage.EnableTaurochole;
            if (ImGui.Checkbox("Enable Taurochole", ref enableTauro))
            {
                config.Sage.EnableTaurochole = enableTauro;
                save();
            }
            ImGui.TextDisabled("oGCD heal + 10% mitigation.");

            var enableIxo = config.Sage.EnableIxochole;
            if (ImGui.Checkbox("Enable Ixochole", ref enableIxo))
            {
                config.Sage.EnableIxochole = enableIxo;
                save();
            }
            ImGui.TextDisabled("oGCD AoE heal.");

            var enableKera = config.Sage.EnableKerachole;
            if (ImGui.Checkbox("Enable Kerachole", ref enableKera))
            {
                config.Sage.EnableKerachole = enableKera;
                save();
            }
            ImGui.TextDisabled("oGCD AoE HoT + 10% mitigation.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Free oGCDs:");

            var enablePhysis = config.Sage.EnablePhysisII;
            if (ImGui.Checkbox("Enable Physis II", ref enablePhysis))
            {
                config.Sage.EnablePhysisII = enablePhysis;
                save();
            }
            ImGui.TextDisabled("Free AoE HoT + healing received buff.");

            var enableHolos = config.Sage.EnableHolos;
            if (ImGui.Checkbox("Enable Holos", ref enableHolos))
            {
                config.Sage.EnableHolos = enableHolos;
                save();
            }
            ImGui.TextDisabled("Free AoE heal + shield + mitigation.");

            var enablePepsis = config.Sage.EnablePepsis;
            if (ImGui.Checkbox("Enable Pepsis", ref enablePepsis))
            {
                config.Sage.EnablePepsis = enablePepsis;
                save();
            }
            ImGui.TextDisabled("Converts shields to direct healing.");

            var enablePneuma = config.Sage.EnablePneuma;
            if (ImGui.Checkbox("Enable Pneuma", ref enablePneuma))
            {
                config.Sage.EnablePneuma = enablePneuma;
                save();
            }
            ImGui.TextDisabled("Line AoE damage + party heal.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Single-Target Thresholds:");

            config.Sage.DiagnosisThreshold = ConfigUIHelpers.ThresholdSlider("Diagnosis Threshold",
                config.Sage.DiagnosisThreshold, 20f, 60f, null, save);
            config.Sage.EukrasianDiagnosisThreshold = ConfigUIHelpers.ThresholdSlider("E.Diagnosis Threshold",
                config.Sage.EukrasianDiagnosisThreshold, 50f, 95f, null, save);
            config.Sage.DruocholeThreshold = ConfigUIHelpers.ThresholdSlider("Druochole Threshold",
                config.Sage.DruocholeThreshold, 30f, 75f, null, save);
            config.Sage.TaurocholeThreshold = ConfigUIHelpers.ThresholdSlider("Taurochole Threshold",
                config.Sage.TaurocholeThreshold, 30f, 75f, null, save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("AoE Thresholds:");

            config.Sage.AoEHealThreshold = ConfigUIHelpers.ThresholdSlider("AoE HP Threshold",
                config.Sage.AoEHealThreshold, 50f, 90f, null, save);

            config.Sage.AoEHealMinTargets = ConfigUIHelpers.IntSlider("AoE Min Targets",
                config.Sage.AoEHealMinTargets, 1, 8, null, save);

            config.Sage.KeracholeThreshold = ConfigUIHelpers.ThresholdSlider("Kerachole Threshold",
                config.Sage.KeracholeThreshold, 60f, 95f, null, save);
            config.Sage.IxocholeThreshold = ConfigUIHelpers.ThresholdSlider("Ixochole Threshold",
                config.Sage.IxocholeThreshold, 40f, 85f, null, save);
            config.Sage.PhysisIIThreshold = ConfigUIHelpers.ThresholdSlider("Physis II Threshold",
                config.Sage.PhysisIIThreshold, 60f, 95f, null, save);
            config.Sage.HolosThreshold = ConfigUIHelpers.ThresholdSlider("Holos Threshold",
                config.Sage.HolosThreshold, 40f, 80f, null, save);
            config.Sage.PneumaThreshold = ConfigUIHelpers.ThresholdSlider("Pneuma Threshold",
                config.Sage.PneumaThreshold, 40f, 85f, null, save);
            config.Sage.PepsisThreshold = ConfigUIHelpers.ThresholdSlider("Pepsis Threshold",
                config.Sage.PepsisThreshold, 30f, 70f, "Converts shields to healing when party HP drops below this.", save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawShieldSection()
    {
        if (ConfigUIHelpers.SectionHeader("Shields", "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableHaima = config.Sage.EnableHaima;
            if (ImGui.Checkbox("Enable Haima", ref enableHaima))
            {
                config.Sage.EnableHaima = enableHaima;
                save();
            }
            ImGui.TextDisabled("Multi-hit single-target shield (6 stacks).");

            if (config.Sage.EnableHaima)
            {
                config.Sage.HaimaThreshold = ConfigUIHelpers.ThresholdSliderSmall("Haima Threshold",
                    config.Sage.HaimaThreshold, 60f, 95f, "Apply to tank when HP below this.", save);
            }

            ConfigUIHelpers.Spacing();

            var enablePanhaima = config.Sage.EnablePanhaima;
            if (ImGui.Checkbox("Enable Panhaima", ref enablePanhaima))
            {
                config.Sage.EnablePanhaima = enablePanhaima;
                save();
            }
            ImGui.TextDisabled("Multi-hit party shield (5 stacks).");

            if (config.Sage.EnablePanhaima)
            {
                config.Sage.PanhaimaThreshold = ConfigUIHelpers.ThresholdSliderSmall("Panhaima Threshold",
                    config.Sage.PanhaimaThreshold, 65f, 95f, "Use when party average HP below this.", save);
            }

            ConfigUIHelpers.Spacing();

            var avoidOverwrite = config.Sage.AvoidOverwritingShields;
            if (ImGui.Checkbox("Avoid Overwriting Shields", ref avoidOverwrite))
            {
                config.Sage.AvoidOverwritingShields = avoidOverwrite;
                save();
            }
            ImGui.TextDisabled("Don't apply new shields over existing ones.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBuffSection()
    {
        if (ConfigUIHelpers.SectionHeader("Buffs", "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableZoe = config.Sage.EnableZoe;
            if (ImGui.Checkbox("Enable Zoe", ref enableZoe))
            {
                config.Sage.EnableZoe = enableZoe;
                save();
            }
            ImGui.TextDisabled("+50% next GCD heal potency.");

            if (config.Sage.EnableZoe)
            {
                var zoeNames = Enum.GetNames<ZoeUsageStrategy>();
                var currentZoe = (int)config.Sage.ZoeStrategy;
                ImGui.SetNextItemWidth(180);
                if (ImGui.Combo("Zoe Strategy", ref currentZoe, zoeNames, zoeNames.Length))
                {
                    config.Sage.ZoeStrategy = (ZoeUsageStrategy)currentZoe;
                    save();
                }

                var zoeDesc = config.Sage.ZoeStrategy switch
                {
                    ZoeUsageStrategy.WithPneuma => "Save for Pneuma",
                    ZoeUsageStrategy.WithEukrasianPrognosis => "Save for E.Prognosis shield",
                    ZoeUsageStrategy.OnDemand => "Use immediately when healing needed",
                    ZoeUsageStrategy.Manual => "Manual control only",
                    _ => ""
                };
                ImGui.TextDisabled(zoeDesc);
            }

            ConfigUIHelpers.Spacing();

            var enableKrasis = config.Sage.EnableKrasis;
            if (ImGui.Checkbox("Enable Krasis", ref enableKrasis))
            {
                config.Sage.EnableKrasis = enableKrasis;
                save();
            }
            ImGui.TextDisabled("+20% healing received on target.");

            if (config.Sage.EnableKrasis)
            {
                config.Sage.KrasisThreshold = ConfigUIHelpers.ThresholdSliderSmall("Krasis Threshold",
                    config.Sage.KrasisThreshold, 40f, 85f, "Apply when target HP below this.", save);
            }

            ConfigUIHelpers.Spacing();

            var enablePhilo = config.Sage.EnablePhilosophia;
            if (ImGui.Checkbox("Enable Philosophia", ref enablePhilo))
            {
                config.Sage.EnablePhilosophia = enablePhilo;
                save();
            }
            ImGui.TextDisabled("Party-wide Kardia effect.");

            if (config.Sage.EnablePhilosophia)
            {
                config.Sage.PhilosophiaThreshold = ConfigUIHelpers.ThresholdSliderSmall("Philosophia Threshold",
                    config.Sage.PhilosophiaThreshold, 50f, 90f, "Use when party average HP below this.", save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader("Damage", "SGE"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Single-Target:");

            var enableST = config.Sage.EnableSingleTargetDamage;
            if (ImGui.Checkbox("Enable Dosis", ref enableST))
            {
                config.Sage.EnableSingleTargetDamage = enableST;
                save();
            }
            ImGui.TextDisabled("Casted single-target damage. Triggers Kardia healing.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("DoT:");

            var enableDot = config.Sage.EnableDot;
            if (ImGui.Checkbox("Enable Eukrasian Dosis", ref enableDot))
            {
                config.Sage.EnableDot = enableDot;
                save();
            }
            ImGui.TextDisabled("Instant DoT that triggers Kardia.");

            if (config.Sage.EnableDot)
            {
                config.Sage.DotRefreshThreshold = ConfigUIHelpers.FloatSlider("DoT Refresh (sec)",
                    config.Sage.DotRefreshThreshold, 0f, 10f, "%.1f", null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("AoE:");

            var enableAoE = config.Sage.EnableAoEDamage;
            if (ImGui.Checkbox("Enable Dyskrasia", ref enableAoE))
            {
                config.Sage.EnableAoEDamage = enableAoE;
                save();
            }
            ImGui.TextDisabled("Instant AoE damage around self.");

            if (config.Sage.EnableAoEDamage)
            {
                config.Sage.AoEDamageMinTargets = ConfigUIHelpers.IntSlider("Min Enemies",
                    config.Sage.AoEDamageMinTargets, 1, 10, null, save);
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Special Abilities:");

            var enablePhlegma = config.Sage.EnablePhlegma;
            if (ImGui.Checkbox("Enable Phlegma", ref enablePhlegma))
            {
                config.Sage.EnablePhlegma = enablePhlegma;
                save();
            }
            ImGui.TextDisabled("Instant damage with charges.");

            var enableToxikon = config.Sage.EnableToxikon;
            if (ImGui.Checkbox("Enable Toxikon", ref enableToxikon))
            {
                config.Sage.EnableToxikon = enableToxikon;
                save();
            }
            ImGui.TextDisabled("Consumes Addersting (from broken E.Diag shields).");

            var enablePsyche = config.Sage.EnablePsyche;
            if (ImGui.Checkbox("Enable Psyche", ref enablePsyche))
            {
                config.Sage.EnablePsyche = enablePsyche;
                save();
            }
            ImGui.TextDisabled("oGCD damage ability.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("MP Management:");

            var enableLucid = config.Sage.EnableLucidDreaming;
            if (ImGui.Checkbox("Enable Lucid Dreaming", ref enableLucid))
            {
                config.Sage.EnableLucidDreaming = enableLucid;
                save();
            }

            if (config.Sage.EnableLucidDreaming)
            {
                config.Sage.LucidDreamingThreshold = ConfigUIHelpers.ThresholdSlider("Lucid MP Threshold",
                    config.Sage.LucidDreamingThreshold, 40f, 90f, null, save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
