using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Config;
using Olympus.Services.Targeting;

namespace Olympus.Windows;

public sealed class ConfigWindow : Window
{
    private readonly Configuration configuration;
    private readonly Action saveConfiguration;

    public ConfigWindow(Configuration configuration, Action saveConfiguration)
        : base("Olympus Settings", ImGuiWindowFlags.NoCollapse)
    {
        this.configuration = configuration;
        this.saveConfiguration = saveConfiguration;

        Size = new System.Numerics.Vector2(400, 550);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var enabled = configuration.Enabled;
        if (ImGui.Checkbox("Enable Rotation", ref enabled))
        {
            configuration.Enabled = enabled;
            saveConfiguration();
        }

        ImGui.TextDisabled("When enabled, Apollo will automatically cast spells.");

        ImGui.Separator();

        DrawTargetingSection();
        DrawHealingSection();
        DrawDefensiveSection();
        DrawResurrectionSection();
        DrawDamageSection();
        DrawDoTSection();
        DrawRoleActionsSection();

        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button("Reset to Defaults"))
        {
            ImGui.OpenPopup("Reset Confirmation");
        }

        if (ImGui.BeginPopupModal("Reset Confirmation", ref resetPopupOpen, ImGuiWindowFlags.AlwaysAutoResize))
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

    private bool resetPopupOpen = true;

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

            ImGui.EndDisabled();
            ImGui.Unindent();
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
}
