using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Dalamud.Utility;
using Olympus.Config;
using Olympus.Services.Targeting;

namespace Olympus.Windows;

public sealed class ConfigWindow : Window
{
    private readonly Configuration configuration;
    private readonly Action saveConfiguration;
    private ConfigurationPreset selectedPreset;

    public ConfigWindow(Configuration configuration, Action saveConfiguration)
        : base("Olympus Settings", ImGuiWindowFlags.NoCollapse)
    {
        this.configuration = configuration;
        this.saveConfiguration = saveConfiguration;

        Size = new System.Numerics.Vector2(450, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        // Discord community button
        var discordColor = new Vector4(88f / 255f, 101f / 255f, 242f / 255f, 1.0f);
        ImGui.PushStyleColor(ImGuiCol.Button, discordColor);
        ImGui.PushStyleColor(ImGuiCol.ButtonHovered, discordColor * 1.1f);
        ImGui.PushStyleColor(ImGuiCol.ButtonActive, discordColor * 0.9f);
        if (ImGui.Button("Join Discord", new Vector2(100, 0)))
        {
            Util.OpenLink("https://discord.gg/3gXYyqbdaU");
        }
        ImGui.PopStyleColor(3);

        ImGui.SameLine();

        var enabled = configuration.Enabled;
        if (ImGui.Checkbox("Enable Rotation", ref enabled))
        {
            configuration.Enabled = enabled;
            saveConfiguration();
        }

        ImGui.TextDisabled("When enabled, the rotation will automatically cast spells.");

        ImGui.Spacing();

        // Configuration Preset selector
        DrawPresetSelector();

        ImGui.Separator();

        // Tab bar for job selection
        if (ImGui.BeginTabBar("JobConfigTabs"))
        {
            if (ImGui.BeginTabItem("General"))
            {
                DrawGeneralTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("White Mage"))
            {
                DrawWhiteMageTab();
                ImGui.EndTabItem();
            }

            if (ImGui.BeginTabItem("Scholar"))
            {
                DrawScholarTab();
                ImGui.EndTabItem();
            }

            ImGui.EndTabBar();
        }

        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Reset to Defaults"))
        {
            ImGui.OpenPopup("Reset Confirmation");
        }

        // Local variable for popup close button state - must be true to show close button
        var popupOpen = true;
        if (ImGui.BeginPopupModal("Reset Confirmation", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text("Reset all settings to default values?");
            ImGui.Text("This cannot be undone.");
            ImGui.Spacing();

            if (ImGui.Button("Yes, Reset", new System.Numerics.Vector2(120, 0)))
            {
                configuration.ResetToDefaults();
                saveConfiguration();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new System.Numerics.Vector2(120, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private static readonly string[] PresetNames = Enum.GetNames<ConfigurationPreset>();

    private void DrawPresetSelector()
    {
        ImGui.Text("Configuration Preset");
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            ImGui.BeginTooltip();
            ImGui.Text("Presets quickly configure settings for different content types.");
            ImGui.Text("Raid: Co-healer aware, balanced DPS");
            ImGui.Text("Dungeon: Solo healer, aggressive DPS");
            ImGui.Text("Casual: Safe mode, healing priority");
            ImGui.EndTooltip();
        }

        var currentPreset = (int)configuration.ActivePreset;
        ImGui.SetNextItemWidth(150);
        if (ImGui.Combo("##PresetCombo", ref currentPreset, PresetNames, PresetNames.Length))
        {
            selectedPreset = (ConfigurationPreset)currentPreset;
            if (selectedPreset != ConfigurationPreset.Custom)
            {
                ImGui.OpenPopup("Apply Preset Confirmation");
            }
        }

        ImGui.SameLine();
        ImGui.TextDisabled(ConfigurationPresets.GetDescription(configuration.ActivePreset));

        DrawPresetConfirmationPopup();
    }

    private void DrawPresetConfirmationPopup()
    {
        var popupOpen = true;
        if (ImGui.BeginPopupModal("Apply Preset Confirmation", ref popupOpen, ImGuiWindowFlags.AlwaysAutoResize))
        {
            ImGui.Text($"Apply {selectedPreset} preset?");
            ImGui.Spacing();
            ImGui.TextWrapped(ConfigurationPresets.GetDescription(selectedPreset));
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), "This will overwrite behavior settings.");
            ImGui.TextDisabled("Spell toggles and targeting preferences are preserved.");
            ImGui.Spacing();

            if (ImGui.Button("Apply", new Vector2(100, 0)))
            {
                ConfigurationPresets.ApplyPreset(configuration, selectedPreset);
                saveConfiguration();
                ImGui.CloseCurrentPopup();
            }

            ImGui.SameLine();

            if (ImGui.Button("Cancel", new Vector2(100, 0)))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }

    private record StrategyInfo(string Name, string Description);

    private static readonly StrategyInfo[] Strategies =
    [
        new("Lowest HP", "Target enemy with lowest HP (finish off weak enemies)"),
        new("Highest HP", "Target enemy with highest HP (for cleave/AoE)"),
        new("Nearest", "Target closest enemy"),
        new("Tank Assist", "Attack what the party tank is targeting"),
        new("Current Target", "Use your current hard target if valid"),
        new("Focus Target", "Use your focus target if valid")
    ];

    private static readonly string[] StrategyNames = Strategies.Select(s => s.Name).ToArray();

    private void DrawTargetingSection()
    {
        if (ImGui.CollapsingHeader("Targeting", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            var currentStrategy = (int)configuration.Targeting.EnemyStrategy;
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("Enemy Strategy", ref currentStrategy, StrategyNames, StrategyNames.Length))
            {
                configuration.Targeting.EnemyStrategy = (EnemyTargetingStrategy)currentStrategy;
                saveConfiguration();
            }
            ImGui.TextDisabled(Strategies[currentStrategy].Description);

            ImGui.Spacing();

            // Only show tank assist fallback when tank assist is selected
            if (configuration.Targeting.EnemyStrategy == EnemyTargetingStrategy.TankAssist)
            {
                var useFallback = configuration.Targeting.UseTankAssistFallback;
                if (ImGui.Checkbox("Fallback to Lowest HP", ref useFallback))
                {
                    configuration.Targeting.UseTankAssistFallback = useFallback;
                    saveConfiguration();
                }
                ImGui.TextDisabled("If no tank target found, use Lowest HP instead.");
            }

            ImGui.Spacing();

            // Movement tolerance
            var moveTolerance = configuration.MovementTolerance * 1000f; // Convert to ms
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Movement Tolerance", ref moveTolerance, 0f, 500f, "%.0f ms"))
            {
                configuration.MovementTolerance = moveTolerance / 1000f;
                saveConfiguration();
            }
            ImGui.TextDisabled("Delay after stopping before casting. Lower = faster, higher = safer.");

            ImGui.Unindent();
        }
    }

    private void DrawHealingSection()
    {
        if (ImGui.CollapsingHeader("Healing", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            var enableHealing = configuration.EnableHealing;
            if (ImGui.Checkbox("Enable Healing", ref enableHealing))
            {
                configuration.EnableHealing = enableHealing;
                saveConfiguration();
            }

            ImGui.BeginDisabled(!configuration.EnableHealing);

            ImGui.TextDisabled("Single-Target:");
            var enableCure = configuration.Healing.EnableCure;
            if (ImGui.Checkbox("Cure", ref enableCure))
            {
                configuration.Healing.EnableCure = enableCure;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableCureII = configuration.Healing.EnableCureII;
            if (ImGui.Checkbox("Cure II", ref enableCureII))
            {
                configuration.Healing.EnableCureII = enableCureII;
                saveConfiguration();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("AoE Healing:");

            var enableMedica = configuration.Healing.EnableMedica;
            if (ImGui.Checkbox("Medica", ref enableMedica))
            {
                configuration.Healing.EnableMedica = enableMedica;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableMedicaII = configuration.Healing.EnableMedicaII;
            if (ImGui.Checkbox("Medica II", ref enableMedicaII))
            {
                configuration.Healing.EnableMedicaII = enableMedicaII;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableMedicaIII = configuration.Healing.EnableMedicaIII;
            if (ImGui.Checkbox("Medica III", ref enableMedicaIII))
            {
                configuration.Healing.EnableMedicaIII = enableMedicaIII;
                saveConfiguration();
            }

            var enableCureIII = configuration.Healing.EnableCureIII;
            if (ImGui.Checkbox("Cure III", ref enableCureIII))
            {
                configuration.Healing.EnableCureIII = enableCureIII;
                saveConfiguration();
            }
            ImGui.TextDisabled("Targeted AoE heal (10y radius around target). Best when stacked.");

            ImGui.Spacing();
            ImGui.TextDisabled("Lily Heals:");

            var enableAfflatusSolace = configuration.Healing.EnableAfflatusSolace;
            if (ImGui.Checkbox("Afflatus Solace", ref enableAfflatusSolace))
            {
                configuration.Healing.EnableAfflatusSolace = enableAfflatusSolace;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableAfflatusRapture = configuration.Healing.EnableAfflatusRapture;
            if (ImGui.Checkbox("Afflatus Rapture", ref enableAfflatusRapture))
            {
                configuration.Healing.EnableAfflatusRapture = enableAfflatusRapture;
                saveConfiguration();
            }
            ImGui.TextDisabled("Free heals that consume Lily gauge.");

            // Blood Lily Optimization Strategy
            ImGui.Spacing();
            var strategyNames = Enum.GetNames<LilyGenerationStrategy>();
            var currentIndex = (int)configuration.Healing.LilyStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Lily Strategy", ref currentIndex, strategyNames, strategyNames.Length))
            {
                configuration.Healing.LilyStrategy = (LilyGenerationStrategy)currentIndex;
                saveConfiguration();
            }
            var strategyDescription = configuration.Healing.LilyStrategy switch
            {
                LilyGenerationStrategy.Aggressive => "Always prefer lily heals when available",
                LilyGenerationStrategy.Balanced => "Prefer lily heals until Blood Lily is full (3/3)",
                LilyGenerationStrategy.Conservative => "Only use lily heals below HP threshold",
                LilyGenerationStrategy.Disabled => "Use normal heal priority (no lily preference)",
                _ => ""
            };
            ImGui.TextDisabled(strategyDescription);

            // Conservative HP threshold (only show when Conservative mode is selected)
            if (configuration.Healing.LilyStrategy == LilyGenerationStrategy.Conservative)
            {
                var threshold = configuration.Healing.ConservativeLilyHpThreshold * 100f;
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Conservative HP Threshold", ref threshold, 50f, 90f, "%.0f%%"))
                {
                    configuration.Healing.ConservativeLilyHpThreshold = threshold / 100f;
                    saveConfiguration();
                }
                ImGui.TextDisabled("Only use lily heals when target is below this HP%.");
            }

            ImGui.Spacing();
            ImGui.TextDisabled("oGCD Heals:");

            var enableTetragrammaton = configuration.Healing.EnableTetragrammaton;
            if (ImGui.Checkbox("Tetragrammaton", ref enableTetragrammaton))
            {
                configuration.Healing.EnableTetragrammaton = enableTetragrammaton;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableBenediction = configuration.Healing.EnableBenediction;
            if (ImGui.Checkbox("Benediction", ref enableBenediction))
            {
                configuration.Healing.EnableBenediction = enableBenediction;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableAssize = configuration.Healing.EnableAssize;
            if (ImGui.Checkbox("Assize", ref enableAssize))
            {
                configuration.Healing.EnableAssize = enableAssize;
                saveConfiguration();
            }
            ImGui.TextDisabled("Instant heals used during weave windows.");

            ImGui.Spacing();
            ImGui.TextDisabled("Healing HoTs:");

            var enableRegen = configuration.Healing.EnableRegen;
            if (ImGui.Checkbox("Regen", ref enableRegen))
            {
                configuration.Healing.EnableRegen = enableRegen;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableAsylum = configuration.Healing.EnableAsylum;
            if (ImGui.Checkbox("Asylum", ref enableAsylum))
            {
                configuration.Healing.EnableAsylum = enableAsylum;
                saveConfiguration();
            }
            ImGui.TextDisabled("Regen (single-target) and Asylum (ground AoE HoT).");

            ImGui.Spacing();
            ImGui.TextDisabled("Buffs:");

            var enablePoM = configuration.Buffs.EnablePresenceOfMind;
            if (ImGui.Checkbox("Presence of Mind", ref enablePoM))
            {
                configuration.Buffs.EnablePresenceOfMind = enablePoM;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableThinAir = configuration.Buffs.EnableThinAir;
            if (ImGui.Checkbox("Thin Air", ref enableThinAir))
            {
                configuration.Buffs.EnableThinAir = enableThinAir;
                saveConfiguration();
            }
            ImGui.TextDisabled("Speed buff and MP cost reduction.");

            var enableAetherialShift = configuration.Buffs.EnableAetherialShift;
            if (ImGui.Checkbox("Aetherial Shift", ref enableAetherialShift))
            {
                configuration.Buffs.EnableAetherialShift = enableAetherialShift;
                saveConfiguration();
            }
            ImGui.TextDisabled("Gap closer (15y dash) when out of spell range.");

            ImGui.Spacing();
            ImGui.TextDisabled("Emergency Thresholds:");

            var ogcdThreshold = configuration.Healing.OgcdEmergencyThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("oGCD Emergency", ref ogcdThreshold, 30f, 70f, "%.0f%%"))
            {
                configuration.Healing.OgcdEmergencyThreshold = ogcdThreshold / 100f;
                saveConfiguration();
            }
            ImGui.TextDisabled("Use emergency oGCD heals (Tetra) when below this HP%.");

            var gcdThreshold = configuration.Healing.GcdEmergencyThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("GCD Emergency", ref gcdThreshold, 20f, 60f, "%.0f%%"))
            {
                configuration.Healing.GcdEmergencyThreshold = gcdThreshold / 100f;
                saveConfiguration();
            }
            ImGui.TextDisabled("Interrupt DPS to heal when below this HP%.");

            var beneThreshold = configuration.Healing.BenedictionEmergencyThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Benediction Threshold", ref beneThreshold, 10f, 50f, "%.0f%%"))
            {
                configuration.Healing.BenedictionEmergencyThreshold = beneThreshold / 100f;
                saveConfiguration();
            }
            ImGui.TextDisabled("Only use Benediction when target HP is below this %.");

            ImGui.Spacing();

            var aoeMinTargets = configuration.Healing.AoEHealMinTargets;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("AoE Min Targets", ref aoeMinTargets, 2, 8))
            {
                configuration.Healing.AoEHealMinTargets = aoeMinTargets;
                saveConfiguration();
            }
            ImGui.TextDisabled("Use AoE heal when this many party members need healing.");

            DrawAdvancedHealingSection();

            ImGui.EndDisabled();
            ImGui.Unindent();
        }
    }

    private void DrawAdvancedHealingSection()
    {
        ImGui.Spacing();
        ImGui.Separator();

        if (ImGui.TreeNode("Advanced Healing Settings"))
        {
            // Triage Settings
            ImGui.TextDisabled("Healing Triage:");

            var useTriage = configuration.Healing.UseDamageIntakeTriage;
            if (ImGui.Checkbox("Use Damage-Based Triage", ref useTriage))
            {
                configuration.Healing.UseDamageIntakeTriage = useTriage;
                saveConfiguration();
            }
            ImGui.TextDisabled("Prioritize healing targets taking active damage.");

            if (configuration.Healing.UseDamageIntakeTriage)
            {
                ImGui.Indent();
                var presetNames = Enum.GetNames<TriagePreset>();
                var currentPreset = (int)configuration.Healing.TriagePreset;
                ImGui.SetNextItemWidth(150);
                if (ImGui.Combo("Triage Preset", ref currentPreset, presetNames, presetNames.Length))
                {
                    configuration.Healing.TriagePreset = (TriagePreset)currentPreset;
                    saveConfiguration();
                }

                var presetDesc = configuration.Healing.TriagePreset switch
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
                if (configuration.Healing.TriagePreset == TriagePreset.Custom)
                {
                    var damageRate = configuration.Healing.CustomTriageWeights.DamageRate * 100f;
                    ImGui.SetNextItemWidth(120);
                    if (ImGui.SliderFloat("Damage Rate", ref damageRate, 0f, 60f, "%.0f%%"))
                    {
                        configuration.Healing.CustomTriageWeights.DamageRate = damageRate / 100f;
                        saveConfiguration();
                    }

                    var tankBonus = configuration.Healing.CustomTriageWeights.TankBonus * 100f;
                    ImGui.SetNextItemWidth(120);
                    if (ImGui.SliderFloat("Tank Bonus", ref tankBonus, 0f, 60f, "%.0f%%"))
                    {
                        configuration.Healing.CustomTriageWeights.TankBonus = tankBonus / 100f;
                        saveConfiguration();
                    }

                    var missingHp = configuration.Healing.CustomTriageWeights.MissingHp * 100f;
                    ImGui.SetNextItemWidth(120);
                    if (ImGui.SliderFloat("Missing HP", ref missingHp, 0f, 60f, "%.0f%%"))
                    {
                        configuration.Healing.CustomTriageWeights.MissingHp = missingHp / 100f;
                        saveConfiguration();
                    }

                    var accel = configuration.Healing.CustomTriageWeights.DamageAcceleration * 100f;
                    ImGui.SetNextItemWidth(120);
                    if (ImGui.SliderFloat("Acceleration", ref accel, 0f, 30f, "%.0f%%"))
                    {
                        configuration.Healing.CustomTriageWeights.DamageAcceleration = accel / 100f;
                        saveConfiguration();
                    }
                }
                ImGui.Unindent();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Assize Healing:");

            var enableAssizeHealing = configuration.Healing.EnableAssizeHealing;
            if (ImGui.Checkbox("Enable Assize for Healing", ref enableAssizeHealing))
            {
                configuration.Healing.EnableAssizeHealing = enableAssizeHealing;
                saveConfiguration();
            }
            ImGui.TextDisabled("Use Assize as a healing oGCD when party needs it.");

            if (configuration.Healing.EnableAssizeHealing)
            {
                ImGui.Indent();
                var assizeMinTargets = configuration.Healing.AssizeHealingMinTargets;
                ImGui.SetNextItemWidth(120);
                if (ImGui.SliderInt("Min Injured", ref assizeMinTargets, 1, 8))
                {
                    configuration.Healing.AssizeHealingMinTargets = assizeMinTargets;
                    saveConfiguration();
                }

                var assizeThreshold = configuration.Healing.AssizeHealingHpThreshold * 100f;
                ImGui.SetNextItemWidth(120);
                if (ImGui.SliderFloat("HP Threshold", ref assizeThreshold, 50f, 95f, "%.0f%%"))
                {
                    configuration.Healing.AssizeHealingHpThreshold = assizeThreshold / 100f;
                    saveConfiguration();
                }
                ImGui.TextDisabled("Prioritize Assize healing when avg HP below threshold.");
                ImGui.Unindent();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Preemptive Healing:");

            var enablePreemptive = configuration.Healing.EnablePreemptiveHealing;
            if (ImGui.Checkbox("Enable Preemptive Healing", ref enablePreemptive))
            {
                configuration.Healing.EnablePreemptiveHealing = enablePreemptive;
                saveConfiguration();
            }
            ImGui.TextDisabled("Heal before damage spikes land based on pattern detection.");

            if (configuration.Healing.EnablePreemptiveHealing)
            {
                ImGui.Indent();
                var preemptiveThreshold = configuration.Healing.PreemptiveHealingThreshold * 100f;
                ImGui.SetNextItemWidth(120);
                if (ImGui.SliderFloat("HP Trigger", ref preemptiveThreshold, 10f, 80f, "%.0f%%"))
                {
                    configuration.Healing.PreemptiveHealingThreshold = preemptiveThreshold / 100f;
                    saveConfiguration();
                }
                ImGui.TextDisabled("Heal if projected HP would drop below this.");

                var spikeConfidence = configuration.Healing.SpikePatternConfidenceThreshold * 100f;
                ImGui.SetNextItemWidth(120);
                if (ImGui.SliderFloat("Pattern Confidence", ref spikeConfidence, 30f, 95f, "%.0f%%"))
                {
                    configuration.Healing.SpikePatternConfidenceThreshold = spikeConfidence / 100f;
                    saveConfiguration();
                }
                ImGui.TextDisabled("Minimum confidence for spike pattern prediction.");

                var spikeLookahead = configuration.Healing.SpikePredictionLookahead;
                ImGui.SetNextItemWidth(120);
                if (ImGui.SliderFloat("Lookahead (sec)", ref spikeLookahead, 0.5f, 5f, "%.1f"))
                {
                    configuration.Healing.SpikePredictionLookahead = spikeLookahead;
                    saveConfiguration();
                }
                ImGui.TextDisabled("How far ahead to predict spikes.");
                ImGui.Unindent();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Experimental:");

            var enableScored = configuration.Healing.EnableScoredHealSelection;
            if (ImGui.Checkbox("Enable Scored Heal Selection", ref enableScored))
            {
                configuration.Healing.EnableScoredHealSelection = enableScored;
                saveConfiguration();
            }
            ImGui.TextDisabled("Use multi-factor scoring instead of tier-based selection.");
            ImGui.TextColored(new System.Numerics.Vector4(1f, 0.7f, 0.3f, 1f), "EXPERIMENTAL");

            ImGui.TreePop();
        }
    }

    private static readonly string[] RaiseModeNames = ["Raise First", "Balanced", "Heal First"];

    private void DrawResurrectionSection()
    {
        if (ImGui.CollapsingHeader("Resurrection", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            var enableRaise = configuration.Resurrection.EnableRaise;
            if (ImGui.Checkbox("Enable Raise", ref enableRaise))
            {
                configuration.Resurrection.EnableRaise = enableRaise;
                saveConfiguration();
            }
            ImGui.TextDisabled("Automatically resurrect dead party members.");

            ImGui.BeginDisabled(!configuration.Resurrection.EnableRaise);

            // Raise Execution Mode
            var currentMode = (int)configuration.Resurrection.RaiseMode;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Raise Priority", ref currentMode, RaiseModeNames, RaiseModeNames.Length))
            {
                configuration.Resurrection.RaiseMode = (RaiseExecutionMode)currentMode;
                saveConfiguration();
            }
            var modeDesc = configuration.Resurrection.RaiseMode switch
            {
                RaiseExecutionMode.RaiseFirst => "Prioritize raising over other actions",
                RaiseExecutionMode.Balanced => "Raise in weave windows, don't interrupt healing",
                RaiseExecutionMode.HealFirst => "Only raise when party HP is stable",
                _ => ""
            };
            ImGui.TextDisabled(modeDesc);

            ImGui.Spacing();
            var allowHardcast = configuration.Resurrection.AllowHardcastRaise;
            if (ImGui.Checkbox("Allow Hardcast Raise", ref allowHardcast))
            {
                configuration.Resurrection.AllowHardcastRaise = allowHardcast;
                saveConfiguration();
            }
            ImGui.TextDisabled("Cast Raise without Swiftcast (8s cast). Use with caution.");

            var mpThreshold = configuration.Resurrection.RaiseMpThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Min MP for Raise", ref mpThreshold, 10f, 50f, "%.0f%%"))
            {
                configuration.Resurrection.RaiseMpThreshold = mpThreshold / 100f;
                saveConfiguration();
            }
            ImGui.TextDisabled("Minimum MP percentage before attempting to raise.");

            ImGui.EndDisabled();
            ImGui.Unindent();
        }
    }

    private void DrawPrivacySection()
    {
        if (ImGui.CollapsingHeader("Privacy"))
        {
            ImGui.Indent();

            var telemetryEnabled = configuration.TelemetryEnabled;
            if (ImGui.Checkbox("Send anonymous usage statistics", ref telemetryEnabled))
            {
                configuration.TelemetryEnabled = telemetryEnabled;
                saveConfiguration();
            }
            ImGui.TextDisabled("Only sends plugin version. No personal data.");

            ImGui.Unindent();
        }
    }

    private static readonly string[] DpsPriorityNames = ["Heal First", "Balanced", "DPS First"];

    private void DrawDamageSection()
    {
        if (ImGui.CollapsingHeader("Damage", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            var enableDamage = configuration.EnableDamage;
            if (ImGui.Checkbox("Enable Damage", ref enableDamage))
            {
                configuration.EnableDamage = enableDamage;
                saveConfiguration();
            }

            ImGui.BeginDisabled(!configuration.EnableDamage);

            // DPS Priority Mode
            var currentPriority = (int)configuration.Damage.DpsPriority;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("DPS Priority", ref currentPriority, DpsPriorityNames, DpsPriorityNames.Length))
            {
                configuration.Damage.DpsPriority = (DpsPriorityMode)currentPriority;
                saveConfiguration();
            }
            var priorityDesc = configuration.Damage.DpsPriority switch
            {
                DpsPriorityMode.HealFirst => "Safest - only DPS when party is healthy",
                DpsPriorityMode.Balanced => "Moderate - more aggressive DPS while healing",
                DpsPriorityMode.DpsFirst => "Maximum DPS - minimal proactive healing",
                _ => ""
            };
            ImGui.TextDisabled(priorityDesc);

            ImGui.Spacing();
            ImGui.TextDisabled("Stone Progression:");

            var enableStone = configuration.Damage.EnableStone;
            if (ImGui.Checkbox("Stone", ref enableStone))
            {
                configuration.Damage.EnableStone = enableStone;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableStoneII = configuration.Damage.EnableStoneII;
            if (ImGui.Checkbox("Stone II", ref enableStoneII))
            {
                configuration.Damage.EnableStoneII = enableStoneII;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableStoneIII = configuration.Damage.EnableStoneIII;
            if (ImGui.Checkbox("Stone III", ref enableStoneIII))
            {
                configuration.Damage.EnableStoneIII = enableStoneIII;
                saveConfiguration();
            }

            var enableStoneIV = configuration.Damage.EnableStoneIV;
            if (ImGui.Checkbox("Stone IV", ref enableStoneIV))
            {
                configuration.Damage.EnableStoneIV = enableStoneIV;
                saveConfiguration();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Glare Progression:");

            var enableGlare = configuration.Damage.EnableGlare;
            if (ImGui.Checkbox("Glare", ref enableGlare))
            {
                configuration.Damage.EnableGlare = enableGlare;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableGlareIII = configuration.Damage.EnableGlareIII;
            if (ImGui.Checkbox("Glare III", ref enableGlareIII))
            {
                configuration.Damage.EnableGlareIII = enableGlareIII;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableGlareIV = configuration.Damage.EnableGlareIV;
            if (ImGui.Checkbox("Glare IV", ref enableGlareIV))
            {
                configuration.Damage.EnableGlareIV = enableGlareIV;
                saveConfiguration();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("AoE Damage:");

            var enableHoly = configuration.Damage.EnableHoly;
            if (ImGui.Checkbox("Holy", ref enableHoly))
            {
                configuration.Damage.EnableHoly = enableHoly;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableHolyIII = configuration.Damage.EnableHolyIII;
            if (ImGui.Checkbox("Holy III", ref enableHolyIII))
            {
                configuration.Damage.EnableHolyIII = enableHolyIII;
                saveConfiguration();
            }
            ImGui.TextDisabled("Self-centered AoE (8y radius). Use when enemies are stacked.");

            ImGui.Spacing();
            ImGui.TextDisabled("Blood Lily:");

            var enableMisery = configuration.Damage.EnableAfflatusMisery;
            if (ImGui.Checkbox("Afflatus Misery", ref enableMisery))
            {
                configuration.Damage.EnableAfflatusMisery = enableMisery;
                saveConfiguration();
            }
            ImGui.TextDisabled("1240p AoE damage (costs 3 Blood Lilies). Use at 3 stacks.");

            ImGui.Spacing();

            var aoeMinEnemies = configuration.Damage.AoEDamageMinTargets;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("AoE Min Enemies", ref aoeMinEnemies, 2, 8))
            {
                configuration.Damage.AoEDamageMinTargets = aoeMinEnemies;
                saveConfiguration();
            }
            ImGui.TextDisabled("Use Holy when this many enemies are within range.");

            ImGui.EndDisabled();
            ImGui.Unindent();
        }
    }

    private void DrawDefensiveSection()
    {
        if (ImGui.CollapsingHeader("Defensive Cooldowns", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            ImGui.TextDisabled("Shields:");

            var enableBenison = configuration.Defensive.EnableDivineBenison;
            if (ImGui.Checkbox("Divine Benison", ref enableBenison))
            {
                configuration.Defensive.EnableDivineBenison = enableBenison;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableAquaveil = configuration.Defensive.EnableAquaveil;
            if (ImGui.Checkbox("Aquaveil", ref enableAquaveil))
            {
                configuration.Defensive.EnableAquaveil = enableAquaveil;
                saveConfiguration();
            }
            ImGui.TextDisabled("Single-target shields. Applied to tank when HP < 90%.");

            ImGui.Spacing();
            ImGui.TextDisabled("Party Mitigation:");

            var enablePlenary = configuration.Defensive.EnablePlenaryIndulgence;
            if (ImGui.Checkbox("Plenary Indulgence", ref enablePlenary))
            {
                configuration.Defensive.EnablePlenaryIndulgence = enablePlenary;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableTemperance = configuration.Defensive.EnableTemperance;
            if (ImGui.Checkbox("Temperance", ref enableTemperance))
            {
                configuration.Defensive.EnableTemperance = enableTemperance;
                saveConfiguration();
            }
            ImGui.TextDisabled("Party-wide mitigation. Used when 3+ injured or avg HP low.");

            ImGui.Spacing();
            ImGui.TextDisabled("Advanced:");

            var enableBell = configuration.Defensive.EnableLiturgyOfTheBell;
            if (ImGui.Checkbox("Liturgy of the Bell", ref enableBell))
            {
                configuration.Defensive.EnableLiturgyOfTheBell = enableBell;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableCaress = configuration.Defensive.EnableDivineCaress;
            if (ImGui.Checkbox("Divine Caress", ref enableCaress))
            {
                configuration.Defensive.EnableDivineCaress = enableCaress;
                saveConfiguration();
            }
            ImGui.TextDisabled("Bell: Ground AoE reactive heal. Caress: AoE shield after Temperance.");

            ImGui.Spacing();

            var threshold = configuration.Defensive.DefensiveCooldownThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Defensive Threshold", ref threshold, 50f, 95f, "%.0f%%"))
            {
                configuration.Defensive.DefensiveCooldownThreshold = threshold / 100f;
                saveConfiguration();
            }
            ImGui.TextDisabled("Use defensives when party avg HP falls below this %.");

            var useWithAoE = configuration.Defensive.UseDefensivesWithAoEHeals;
            if (ImGui.Checkbox("Use with AoE Heals", ref useWithAoE))
            {
                configuration.Defensive.UseDefensivesWithAoEHeals = useWithAoE;
                saveConfiguration();
            }
            ImGui.TextDisabled("Sync Plenary Indulgence with AoE healing for bonus potency.");

            ImGui.Unindent();
        }
    }

    private void DrawDoTSection()
    {
        if (ImGui.CollapsingHeader("DoT", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            var enableDoT = configuration.EnableDoT;
            if (ImGui.Checkbox("Enable DoT", ref enableDoT))
            {
                configuration.EnableDoT = enableDoT;
                saveConfiguration();
            }

            ImGui.BeginDisabled(!configuration.EnableDoT);

            var enableAero = configuration.Dot.EnableAero;
            if (ImGui.Checkbox("Aero", ref enableAero))
            {
                configuration.Dot.EnableAero = enableAero;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableAeroII = configuration.Dot.EnableAeroII;
            if (ImGui.Checkbox("Aero II", ref enableAeroII))
            {
                configuration.Dot.EnableAeroII = enableAeroII;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableDia = configuration.Dot.EnableDia;
            if (ImGui.Checkbox("Dia", ref enableDia))
            {
                configuration.Dot.EnableDia = enableDia;
                saveConfiguration();
            }

            ImGui.EndDisabled();
            ImGui.Unindent();
        }
    }

    private static readonly string[] SurecastModes = ["Manual Only", "Use on Cooldown"];

    private void DrawRoleActionsSection()
    {
        if (ImGui.CollapsingHeader("Role Actions", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            // Esuna
            ImGui.TextDisabled("Esuna (Cleanse):");

            var enableEsuna = configuration.RoleActions.EnableEsuna;
            if (ImGui.Checkbox("Enable Esuna", ref enableEsuna))
            {
                configuration.RoleActions.EnableEsuna = enableEsuna;
                saveConfiguration();
            }
            ImGui.TextDisabled("Automatically cleanse dispellable debuffs from party.");

            ImGui.BeginDisabled(!configuration.RoleActions.EnableEsuna);

            var priorityThreshold = configuration.RoleActions.EsunaPriorityThreshold;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Priority Threshold", ref priorityThreshold, 0, 3))
            {
                configuration.RoleActions.EsunaPriorityThreshold = priorityThreshold;
                saveConfiguration();
            }

            var priorityDesc = priorityThreshold switch
            {
                0 => "Lethal only (Doom, Throttle)",
                1 => "High+ (also Vulnerability Up)",
                2 => "Medium+ (also Paralysis, Silence)",
                3 => "All dispellable debuffs",
                _ => "Unknown"
            };
            ImGui.TextDisabled(priorityDesc);

            ImGui.EndDisabled();

            ImGui.Spacing();

            // Surecast
            ImGui.TextDisabled("Surecast (Knockback Immunity):");

            var enableSurecast = configuration.RoleActions.EnableSurecast;
            if (ImGui.Checkbox("Enable Surecast", ref enableSurecast))
            {
                configuration.RoleActions.EnableSurecast = enableSurecast;
                saveConfiguration();
            }
            ImGui.TextDisabled("6s immunity to knockback/draw-in. 120s cooldown.");

            ImGui.BeginDisabled(!configuration.RoleActions.EnableSurecast);

            var surecastMode = configuration.RoleActions.SurecastMode;
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("Surecast Mode", ref surecastMode, SurecastModes, SurecastModes.Length))
            {
                configuration.RoleActions.SurecastMode = surecastMode;
                saveConfiguration();
            }
            ImGui.TextDisabled("Knockbacks are content-specific. Manual recommended.");

            ImGui.EndDisabled();

            ImGui.Spacing();

            // Rescue
            ImGui.TextDisabled("Rescue (Pull Party Member):");

            var enableRescue = configuration.RoleActions.EnableRescue;
            if (ImGui.Checkbox("Enable Rescue", ref enableRescue))
            {
                configuration.RoleActions.EnableRescue = enableRescue;
                saveConfiguration();
            }

            if (configuration.RoleActions.EnableRescue)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0.5f, 0, 1),
                    "WARNING: Rescue can kill party members if misused!");
            }
            ImGui.TextDisabled("Pulls party member to your position. Manual use only.");

            ImGui.Unindent();
        }
    }

    #region Tab Methods

    private void DrawGeneralTab()
    {
        DrawTargetingSection();
        DrawRoleActionsSection();
        DrawResurrectionSection();
        DrawPrivacySection();
    }

    private void DrawWhiteMageTab()
    {
        ImGui.TextColored(new System.Numerics.Vector4(1f, 1f, 0.8f, 1f), "Apollo (White Mage) Settings");
        ImGui.Spacing();

        DrawHealingSection();
        DrawDefensiveSection();
        DrawDamageSection();
        DrawDoTSection();
    }

    private void DrawScholarTab()
    {
        ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.9f, 1f, 1f), "Athena (Scholar) Settings");
        ImGui.Spacing();

        DrawScholarHealingSection();
        DrawScholarFairySection();
        DrawScholarShieldSection();
        DrawScholarAetherflowSection();
        DrawScholarDamageSection();
    }

    #endregion

    #region Scholar Sections

    private void DrawScholarHealingSection()
    {
        if (ImGui.CollapsingHeader("Healing##SCH", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            ImGui.TextDisabled("GCD Heals:");

            var enablePhysick = configuration.Scholar.EnablePhysick;
            if (ImGui.Checkbox("Enable Physick", ref enablePhysick))
            {
                configuration.Scholar.EnablePhysick = enablePhysick;
                saveConfiguration();
            }

            var enableAdlo = configuration.Scholar.EnableAdloquium;
            if (ImGui.Checkbox("Enable Adloquium", ref enableAdlo))
            {
                configuration.Scholar.EnableAdloquium = enableAdlo;
                saveConfiguration();
            }

            var enableSuccor = configuration.Scholar.EnableSuccor;
            if (ImGui.Checkbox("Enable Succor", ref enableSuccor))
            {
                configuration.Scholar.EnableSuccor = enableSuccor;
                saveConfiguration();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("oGCD Heals:");

            var enableLustrate = configuration.Scholar.EnableLustrate;
            if (ImGui.Checkbox("Enable Lustrate", ref enableLustrate))
            {
                configuration.Scholar.EnableLustrate = enableLustrate;
                saveConfiguration();
            }

            var enableExcog = configuration.Scholar.EnableExcogitation;
            if (ImGui.Checkbox("Enable Excogitation", ref enableExcog))
            {
                configuration.Scholar.EnableExcogitation = enableExcog;
                saveConfiguration();
            }

            var enableIndom = configuration.Scholar.EnableIndomitability;
            if (ImGui.Checkbox("Enable Indomitability", ref enableIndom))
            {
                configuration.Scholar.EnableIndomitability = enableIndom;
                saveConfiguration();
            }

            var enableProtraction = configuration.Scholar.EnableProtraction;
            if (ImGui.Checkbox("Enable Protraction", ref enableProtraction))
            {
                configuration.Scholar.EnableProtraction = enableProtraction;
                saveConfiguration();
            }

            var enableRecitation = configuration.Scholar.EnableRecitation;
            if (ImGui.Checkbox("Enable Recitation", ref enableRecitation))
            {
                configuration.Scholar.EnableRecitation = enableRecitation;
                saveConfiguration();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Single-Target Thresholds:");

            var physickThreshold = configuration.Scholar.PhysickThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Physick", ref physickThreshold, 20f, 80f, "%.0f%%"))
            {
                configuration.Scholar.PhysickThreshold = physickThreshold / 100f;
                saveConfiguration();
            }

            var adloThreshold = configuration.Scholar.AdloquiumThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Adloquium", ref adloThreshold, 40f, 90f, "%.0f%%"))
            {
                configuration.Scholar.AdloquiumThreshold = adloThreshold / 100f;
                saveConfiguration();
            }

            var lustrateThreshold = configuration.Scholar.LustrateThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Lustrate", ref lustrateThreshold, 30f, 80f, "%.0f%%"))
            {
                configuration.Scholar.LustrateThreshold = lustrateThreshold / 100f;
                saveConfiguration();
            }

            var excogThreshold = configuration.Scholar.ExcogitationThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Excogitation", ref excogThreshold, 60f, 95f, "%.0f%%"))
            {
                configuration.Scholar.ExcogitationThreshold = excogThreshold / 100f;
                saveConfiguration();
            }
            ImGui.TextDisabled("Apply Excogitation proactively at this HP%.");

            ImGui.Spacing();
            ImGui.TextDisabled("AoE Healing:");

            var aoeThreshold = configuration.Scholar.AoEHealThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("AoE HP Threshold", ref aoeThreshold, 50f, 90f, "%.0f%%"))
            {
                configuration.Scholar.AoEHealThreshold = aoeThreshold / 100f;
                saveConfiguration();
            }

            var aoeMinTargets = configuration.Scholar.AoEHealMinTargets;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("AoE Min Targets##SCH", ref aoeMinTargets, 2, 8))
            {
                configuration.Scholar.AoEHealMinTargets = aoeMinTargets;
                saveConfiguration();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Recitation Priority:");

            var recitationNames = Enum.GetNames<RecitationPriority>();
            var currentRecitation = (int)configuration.Scholar.RecitationPriority;
            ImGui.SetNextItemWidth(180);
            if (ImGui.Combo("Recitation Target", ref currentRecitation, recitationNames, recitationNames.Length))
            {
                configuration.Scholar.RecitationPriority = (RecitationPriority)currentRecitation;
                saveConfiguration();
            }
            ImGui.TextDisabled("Which ability to use with Recitation (guaranteed crit, free).");

            ImGui.Spacing();
            ImGui.TextDisabled("Sacred Soil:");

            var enableSoil = configuration.Scholar.EnableSacredSoil;
            if (ImGui.Checkbox("Enable Sacred Soil", ref enableSoil))
            {
                configuration.Scholar.EnableSacredSoil = enableSoil;
                saveConfiguration();
            }

            if (configuration.Scholar.EnableSacredSoil)
            {
                ImGui.Indent();
                var soilThreshold = configuration.Scholar.SacredSoilThreshold * 100f;
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Soil HP Threshold", ref soilThreshold, 50f, 90f, "%.0f%%"))
                {
                    configuration.Scholar.SacredSoilThreshold = soilThreshold / 100f;
                    saveConfiguration();
                }

                var soilMinTargets = configuration.Scholar.SacredSoilMinTargets;
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderInt("Soil Min Targets", ref soilMinTargets, 2, 8))
                {
                    configuration.Scholar.SacredSoilMinTargets = soilMinTargets;
                    saveConfiguration();
                }
                ImGui.Unindent();
            }

            ImGui.Unindent();
        }
    }

    private void DrawScholarFairySection()
    {
        if (ImGui.CollapsingHeader("Fairy##SCH", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            var autoSummon = configuration.Scholar.AutoSummonFairy;
            if (ImGui.Checkbox("Auto-Summon Fairy", ref autoSummon))
            {
                configuration.Scholar.AutoSummonFairy = autoSummon;
                saveConfiguration();
            }
            ImGui.TextDisabled("Automatically summon Eos if not present.");

            var enableAbilities = configuration.Scholar.EnableFairyAbilities;
            if (ImGui.Checkbox("Enable Fairy Abilities", ref enableAbilities))
            {
                configuration.Scholar.EnableFairyAbilities = enableAbilities;
                saveConfiguration();
            }
            ImGui.TextDisabled("Automatically use Whispering Dawn, Fey Blessing, etc.");

            ImGui.BeginDisabled(!configuration.Scholar.EnableFairyAbilities);

            ImGui.Spacing();
            ImGui.TextDisabled("Whispering Dawn:");

            var wdThreshold = configuration.Scholar.WhisperingDawnThreshold * 100f;
            ImGui.SetNextItemWidth(180);
            if (ImGui.SliderFloat("WD HP Threshold", ref wdThreshold, 50f, 95f, "%.0f%%"))
            {
                configuration.Scholar.WhisperingDawnThreshold = wdThreshold / 100f;
                saveConfiguration();
            }

            var wdMinTargets = configuration.Scholar.WhisperingDawnMinTargets;
            ImGui.SetNextItemWidth(180);
            if (ImGui.SliderInt("WD Min Targets", ref wdMinTargets, 1, 8))
            {
                configuration.Scholar.WhisperingDawnMinTargets = wdMinTargets;
                saveConfiguration();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Fey Blessing:");

            var fbThreshold = configuration.Scholar.FeyBlessingThreshold * 100f;
            ImGui.SetNextItemWidth(180);
            if (ImGui.SliderFloat("FB HP Threshold", ref fbThreshold, 50f, 90f, "%.0f%%"))
            {
                configuration.Scholar.FeyBlessingThreshold = fbThreshold / 100f;
                saveConfiguration();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Fey Union:");

            var fuThreshold = configuration.Scholar.FeyUnionThreshold * 100f;
            ImGui.SetNextItemWidth(180);
            if (ImGui.SliderFloat("FU HP Threshold", ref fuThreshold, 40f, 80f, "%.0f%%"))
            {
                configuration.Scholar.FeyUnionThreshold = fuThreshold / 100f;
                saveConfiguration();
            }

            var fuMinGauge = configuration.Scholar.FeyUnionMinGauge;
            ImGui.SetNextItemWidth(180);
            if (ImGui.SliderInt("FU Min Gauge", ref fuMinGauge, 10, 100))
            {
                configuration.Scholar.FeyUnionMinGauge = fuMinGauge;
                saveConfiguration();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Seraph:");

            var seraphNames = Enum.GetNames<SeraphUsageStrategy>();
            var currentSeraph = (int)configuration.Scholar.SeraphStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Seraph Strategy", ref currentSeraph, seraphNames, seraphNames.Length))
            {
                configuration.Scholar.SeraphStrategy = (SeraphUsageStrategy)currentSeraph;
                saveConfiguration();
            }

            var seraphThreshold = configuration.Scholar.SeraphPartyHpThreshold * 100f;
            ImGui.SetNextItemWidth(180);
            if (ImGui.SliderFloat("Seraph HP Trigger", ref seraphThreshold, 50f, 90f, "%.0f%%"))
            {
                configuration.Scholar.SeraphPartyHpThreshold = seraphThreshold / 100f;
                saveConfiguration();
            }

            var enableConsolation = configuration.Scholar.EnableConsolation;
            if (ImGui.Checkbox("Enable Consolation", ref enableConsolation))
            {
                configuration.Scholar.EnableConsolation = enableConsolation;
                saveConfiguration();
            }
            ImGui.TextDisabled("Seraph AoE heal + shield ability.");

            ImGui.Spacing();
            ImGui.TextDisabled("Seraphism (Lv100):");

            var seraphismNames = Enum.GetNames<SeraphismUsageStrategy>();
            var currentSeraphism = (int)configuration.Scholar.SeraphismStrategy;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Seraphism Strategy", ref currentSeraphism, seraphismNames, seraphismNames.Length))
            {
                configuration.Scholar.SeraphismStrategy = (SeraphismUsageStrategy)currentSeraphism;
                saveConfiguration();
            }

            ImGui.EndDisabled();
            ImGui.Unindent();
        }
    }

    private void DrawScholarShieldSection()
    {
        if (ImGui.CollapsingHeader("Shields##SCH", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            var enableET = configuration.Scholar.EnableEmergencyTactics;
            if (ImGui.Checkbox("Emergency Tactics", ref enableET))
            {
                configuration.Scholar.EnableEmergencyTactics = enableET;
                saveConfiguration();
            }
            ImGui.TextDisabled("Convert next shield to direct healing.");

            if (configuration.Scholar.EnableEmergencyTactics)
            {
                var etThreshold = configuration.Scholar.EmergencyTacticsThreshold * 100f;
                ImGui.SetNextItemWidth(180);
                if (ImGui.SliderFloat("ET HP Threshold", ref etThreshold, 20f, 60f, "%.0f%%"))
                {
                    configuration.Scholar.EmergencyTacticsThreshold = etThreshold / 100f;
                    saveConfiguration();
                }
            }

            ImGui.Spacing();

            var enableDT = configuration.Scholar.EnableDeploymentTactics;
            if (ImGui.Checkbox("Deployment Tactics", ref enableDT))
            {
                configuration.Scholar.EnableDeploymentTactics = enableDT;
                saveConfiguration();
            }
            ImGui.TextDisabled("Spread Galvanize shield to party.");

            if (configuration.Scholar.EnableDeploymentTactics)
            {
                var dtMinTargets = configuration.Scholar.DeploymentMinTargets;
                ImGui.SetNextItemWidth(180);
                if (ImGui.SliderInt("Deploy Min Targets", ref dtMinTargets, 2, 8))
                {
                    configuration.Scholar.DeploymentMinTargets = dtMinTargets;
                    saveConfiguration();
                }
            }

            ImGui.Spacing();

            var avoidSage = configuration.Scholar.AvoidOverwritingSageShields;
            if (ImGui.Checkbox("Avoid Sage Shield Overwrite", ref avoidSage))
            {
                configuration.Scholar.AvoidOverwritingSageShields = avoidSage;
                saveConfiguration();
            }
            ImGui.TextDisabled("Don't apply Galvanize if target has Sage shields.");

            ImGui.Spacing();
            ImGui.TextDisabled("Expedient:");

            var enableExp = configuration.Scholar.EnableExpedient;
            if (ImGui.Checkbox("Enable Expedient", ref enableExp))
            {
                configuration.Scholar.EnableExpedient = enableExp;
                saveConfiguration();
            }

            if (configuration.Scholar.EnableExpedient)
            {
                var expThreshold = configuration.Scholar.ExpedientThreshold * 100f;
                ImGui.SetNextItemWidth(180);
                if (ImGui.SliderFloat("Expedient HP Trigger", ref expThreshold, 40f, 80f, "%.0f%%"))
                {
                    configuration.Scholar.ExpedientThreshold = expThreshold / 100f;
                    saveConfiguration();
                }
            }

            ImGui.Unindent();
        }
    }

    private void DrawScholarAetherflowSection()
    {
        if (ImGui.CollapsingHeader("Aetherflow##SCH", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            var strategyNames = Enum.GetNames<AetherflowUsageStrategy>();
            var currentStrategy = (int)configuration.Scholar.AetherflowStrategy;
            ImGui.SetNextItemWidth(180);
            if (ImGui.Combo("Aetherflow Strategy", ref currentStrategy, strategyNames, strategyNames.Length))
            {
                configuration.Scholar.AetherflowStrategy = (AetherflowUsageStrategy)currentStrategy;
                saveConfiguration();
            }

            var strategyDesc = configuration.Scholar.AetherflowStrategy switch
            {
                AetherflowUsageStrategy.Balanced => "Balance healing and Energy Drain",
                AetherflowUsageStrategy.HealingPriority => "Prioritize healing, minimal DPS",
                AetherflowUsageStrategy.AggressiveDps => "Aggressive Energy Drain when safe",
                _ => ""
            };
            ImGui.TextDisabled(strategyDesc);

            ImGui.Spacing();

            var reserve = configuration.Scholar.AetherflowReserve;
            ImGui.SetNextItemWidth(180);
            if (ImGui.SliderInt("Stack Reserve", ref reserve, 0, 3))
            {
                configuration.Scholar.AetherflowReserve = reserve;
                saveConfiguration();
            }
            ImGui.TextDisabled("Stacks to keep for emergency healing.");

            var enableED = configuration.Scholar.EnableEnergyDrain;
            if (ImGui.Checkbox("Enable Energy Drain", ref enableED))
            {
                configuration.Scholar.EnableEnergyDrain = enableED;
                saveConfiguration();
            }

            var dumpWindow = configuration.Scholar.AetherflowDumpWindow;
            ImGui.SetNextItemWidth(180);
            if (ImGui.SliderFloat("Dump Window (sec)", ref dumpWindow, 0f, 15f, "%.1f"))
            {
                configuration.Scholar.AetherflowDumpWindow = dumpWindow;
                saveConfiguration();
            }
            ImGui.TextDisabled("Start dumping stacks when Aetherflow CD is below this.");

            ImGui.Spacing();
            ImGui.TextDisabled("Dissipation:");

            var enableDissipation = configuration.Scholar.EnableDissipation;
            if (ImGui.Checkbox("Enable Dissipation", ref enableDissipation))
            {
                configuration.Scholar.EnableDissipation = enableDissipation;
                saveConfiguration();
            }
            ImGui.TextDisabled("Sacrifice fairy for 3 Aetherflow + 20% heal boost.");

            if (configuration.Scholar.EnableDissipation)
            {
                ImGui.Indent();
                var maxGauge = configuration.Scholar.DissipationMaxFairyGauge;
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderInt("Max Fairy Gauge", ref maxGauge, 0, 100))
                {
                    configuration.Scholar.DissipationMaxFairyGauge = maxGauge;
                    saveConfiguration();
                }
                ImGui.TextDisabled("Only use when gauge is below this (avoid waste).");

                var safeHp = configuration.Scholar.DissipationSafePartyHp * 100f;
                ImGui.SetNextItemWidth(150);
                if (ImGui.SliderFloat("Safe Party HP", ref safeHp, 60f, 95f, "%.0f%%"))
                {
                    configuration.Scholar.DissipationSafePartyHp = safeHp / 100f;
                    saveConfiguration();
                }
                ImGui.TextDisabled("Only use when party HP is above this.");
                ImGui.Unindent();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("MP Management:");

            var enableLucid = configuration.Scholar.EnableLucidDreaming;
            if (ImGui.Checkbox("Enable Lucid Dreaming", ref enableLucid))
            {
                configuration.Scholar.EnableLucidDreaming = enableLucid;
                saveConfiguration();
            }

            if (configuration.Scholar.EnableLucidDreaming)
            {
                var lucidThreshold = configuration.Scholar.LucidDreamingThreshold * 100f;
                ImGui.SetNextItemWidth(180);
                if (ImGui.SliderFloat("Lucid MP Threshold", ref lucidThreshold, 40f, 90f, "%.0f%%"))
                {
                    configuration.Scholar.LucidDreamingThreshold = lucidThreshold / 100f;
                    saveConfiguration();
                }
            }

            ImGui.Unindent();
        }
    }

    private void DrawScholarDamageSection()
    {
        if (ImGui.CollapsingHeader("Damage##SCH", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            ImGui.TextDisabled("Single-Target Damage:");

            var enableSingleTarget = configuration.Scholar.EnableSingleTargetDamage;
            if (ImGui.Checkbox("Enable Broil/Ruin", ref enableSingleTarget))
            {
                configuration.Scholar.EnableSingleTargetDamage = enableSingleTarget;
                saveConfiguration();
            }
            ImGui.TextDisabled("Casted single-target damage spells.");

            var enableRuinII = configuration.Scholar.EnableRuinII;
            if (ImGui.Checkbox("Enable Ruin II", ref enableRuinII))
            {
                configuration.Scholar.EnableRuinII = enableRuinII;
                saveConfiguration();
            }
            ImGui.TextDisabled("Instant damage while moving.");

            ImGui.Spacing();
            ImGui.TextDisabled("DoT:");

            var enableDot = configuration.Scholar.EnableDot;
            if (ImGui.Checkbox("Enable Bio/Biolysis", ref enableDot))
            {
                configuration.Scholar.EnableDot = enableDot;
                saveConfiguration();
            }

            if (configuration.Scholar.EnableDot)
            {
                var dotRefresh = configuration.Scholar.DotRefreshThreshold;
                ImGui.SetNextItemWidth(180);
                if (ImGui.SliderFloat("DoT Refresh (sec)", ref dotRefresh, 0f, 10f, "%.1f"))
                {
                    configuration.Scholar.DotRefreshThreshold = dotRefresh;
                    saveConfiguration();
                }
            }

            ImGui.Spacing();
            ImGui.TextDisabled("AoE Damage:");

            var enableAoEDamage = configuration.Scholar.EnableAoEDamage;
            if (ImGui.Checkbox("Enable Art of War", ref enableAoEDamage))
            {
                configuration.Scholar.EnableAoEDamage = enableAoEDamage;
                saveConfiguration();
            }

            if (configuration.Scholar.EnableAoEDamage)
            {
                var aoeDamageMinTargets = configuration.Scholar.AoEDamageMinTargets;
                ImGui.SetNextItemWidth(180);
                if (ImGui.SliderInt("Art of War Min Enemies", ref aoeDamageMinTargets, 2, 10))
                {
                    configuration.Scholar.AoEDamageMinTargets = aoeDamageMinTargets;
                    saveConfiguration();
                }
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Aetherflow:");

            var enableAetherflow = configuration.Scholar.EnableAetherflow;
            if (ImGui.Checkbox("Enable Aetherflow", ref enableAetherflow))
            {
                configuration.Scholar.EnableAetherflow = enableAetherflow;
                saveConfiguration();
            }
            ImGui.TextDisabled("Use Aetherflow when stacks are empty.");

            ImGui.Spacing();
            ImGui.TextDisabled("Raid Buff:");

            var enableChain = configuration.Scholar.EnableChainStratagem;
            if (ImGui.Checkbox("Enable Chain Stratagem", ref enableChain))
            {
                configuration.Scholar.EnableChainStratagem = enableChain;
                saveConfiguration();
            }
            ImGui.TextDisabled("+10% crit rate on target for party.");

            var enableBaneful = configuration.Scholar.EnableBanefulImpaction;
            if (ImGui.Checkbox("Enable Baneful Impaction", ref enableBaneful))
            {
                configuration.Scholar.EnableBanefulImpaction = enableBaneful;
                saveConfiguration();
            }
            ImGui.TextDisabled("AoE follow-up when Impact Imminent is active.");

            ImGui.Unindent();
        }
    }

    #endregion
}
