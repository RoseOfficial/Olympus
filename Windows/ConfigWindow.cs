using System;
using System.Linq;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
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

            var currentStrategy = (int)configuration.EnemyStrategy;
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("Enemy Strategy", ref currentStrategy, StrategyNames, StrategyNames.Length))
            {
                configuration.EnemyStrategy = (EnemyTargetingStrategy)currentStrategy;
                saveConfiguration();
            }
            ImGui.TextDisabled(Strategies[currentStrategy].Description);

            ImGui.Spacing();

            // Only show tank assist fallback when tank assist is selected
            if (configuration.EnemyStrategy == EnemyTargetingStrategy.TankAssist)
            {
                var useFallback = configuration.UseTankAssistFallback;
                if (ImGui.Checkbox("Fallback to Lowest HP", ref useFallback))
                {
                    configuration.UseTankAssistFallback = useFallback;
                    saveConfiguration();
                }
                ImGui.TextDisabled("If no tank target found, use Lowest HP instead.");
            }

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
            var enableCure = configuration.EnableCure;
            if (ImGui.Checkbox("Cure", ref enableCure))
            {
                configuration.EnableCure = enableCure;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableCureII = configuration.EnableCureII;
            if (ImGui.Checkbox("Cure II", ref enableCureII))
            {
                configuration.EnableCureII = enableCureII;
                saveConfiguration();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("AoE Healing:");

            var enableMedica = configuration.EnableMedica;
            if (ImGui.Checkbox("Medica", ref enableMedica))
            {
                configuration.EnableMedica = enableMedica;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableMedicaII = configuration.EnableMedicaII;
            if (ImGui.Checkbox("Medica II", ref enableMedicaII))
            {
                configuration.EnableMedicaII = enableMedicaII;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableMedicaIII = configuration.EnableMedicaIII;
            if (ImGui.Checkbox("Medica III", ref enableMedicaIII))
            {
                configuration.EnableMedicaIII = enableMedicaIII;
                saveConfiguration();
            }

            var enableCureIII = configuration.EnableCureIII;
            if (ImGui.Checkbox("Cure III", ref enableCureIII))
            {
                configuration.EnableCureIII = enableCureIII;
                saveConfiguration();
            }
            ImGui.TextDisabled("Targeted AoE heal (10y radius around target). Best when stacked.");

            ImGui.Spacing();
            ImGui.TextDisabled("Lily Heals:");

            var enableAfflatusSolace = configuration.EnableAfflatusSolace;
            if (ImGui.Checkbox("Afflatus Solace", ref enableAfflatusSolace))
            {
                configuration.EnableAfflatusSolace = enableAfflatusSolace;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableAfflatusRapture = configuration.EnableAfflatusRapture;
            if (ImGui.Checkbox("Afflatus Rapture", ref enableAfflatusRapture))
            {
                configuration.EnableAfflatusRapture = enableAfflatusRapture;
                saveConfiguration();
            }
            ImGui.TextDisabled("Free heals that consume Lily gauge.");

            ImGui.Spacing();
            ImGui.TextDisabled("oGCD Heals:");

            var enableTetragrammaton = configuration.EnableTetragrammaton;
            if (ImGui.Checkbox("Tetragrammaton", ref enableTetragrammaton))
            {
                configuration.EnableTetragrammaton = enableTetragrammaton;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableBenediction = configuration.EnableBenediction;
            if (ImGui.Checkbox("Benediction", ref enableBenediction))
            {
                configuration.EnableBenediction = enableBenediction;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableAssize = configuration.EnableAssize;
            if (ImGui.Checkbox("Assize", ref enableAssize))
            {
                configuration.EnableAssize = enableAssize;
                saveConfiguration();
            }
            ImGui.TextDisabled("Instant heals used during weave windows.");

            ImGui.Spacing();
            ImGui.TextDisabled("Healing HoTs:");

            var enableRegen = configuration.EnableRegen;
            if (ImGui.Checkbox("Regen", ref enableRegen))
            {
                configuration.EnableRegen = enableRegen;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableAsylum = configuration.EnableAsylum;
            if (ImGui.Checkbox("Asylum", ref enableAsylum))
            {
                configuration.EnableAsylum = enableAsylum;
                saveConfiguration();
            }
            ImGui.TextDisabled("Regen (single-target) and Asylum (ground AoE HoT).");

            ImGui.Spacing();
            ImGui.TextDisabled("Buffs:");

            var enablePoM = configuration.EnablePresenceOfMind;
            if (ImGui.Checkbox("Presence of Mind", ref enablePoM))
            {
                configuration.EnablePresenceOfMind = enablePoM;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableThinAir = configuration.EnableThinAir;
            if (ImGui.Checkbox("Thin Air", ref enableThinAir))
            {
                configuration.EnableThinAir = enableThinAir;
                saveConfiguration();
            }
            ImGui.TextDisabled("Speed buff and MP cost reduction.");

            var enableAetherialShift = configuration.EnableAetherialShift;
            if (ImGui.Checkbox("Aetherial Shift", ref enableAetherialShift))
            {
                configuration.EnableAetherialShift = enableAetherialShift;
                saveConfiguration();
            }
            ImGui.TextDisabled("Gap closer (15y dash) when out of spell range.");

            ImGui.Spacing();
            var beneThreshold = configuration.BenedictionEmergencyThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Benediction Threshold", ref beneThreshold, 10f, 50f, "%.0f%%"))
            {
                configuration.BenedictionEmergencyThreshold = beneThreshold / 100f;
                saveConfiguration();
            }
            ImGui.TextDisabled("Only use Benediction when target HP is below this %.");

            ImGui.Spacing();

            var aoeMinTargets = configuration.AoEHealMinTargets;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("AoE Min Targets", ref aoeMinTargets, 2, 8))
            {
                configuration.AoEHealMinTargets = aoeMinTargets;
                saveConfiguration();
            }
            ImGui.TextDisabled("Use AoE heal when this many party members need healing.");

            ImGui.EndDisabled();
            ImGui.Unindent();
        }
    }

    private void DrawResurrectionSection()
    {
        if (ImGui.CollapsingHeader("Resurrection", ImGuiTreeNodeFlags.DefaultOpen))
        {
            ImGui.Indent();

            var enableRaise = configuration.EnableRaise;
            if (ImGui.Checkbox("Enable Raise", ref enableRaise))
            {
                configuration.EnableRaise = enableRaise;
                saveConfiguration();
            }
            ImGui.TextDisabled("Automatically resurrect dead party members.");

            ImGui.BeginDisabled(!configuration.EnableRaise);

            var allowHardcast = configuration.AllowHardcastRaise;
            if (ImGui.Checkbox("Allow Hardcast Raise", ref allowHardcast))
            {
                configuration.AllowHardcastRaise = allowHardcast;
                saveConfiguration();
            }
            ImGui.TextDisabled("Cast Raise without Swiftcast (8s cast). Use with caution.");

            var mpThreshold = configuration.RaiseMpThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Min MP for Raise", ref mpThreshold, 10f, 50f, "%.0f%%"))
            {
                configuration.RaiseMpThreshold = mpThreshold / 100f;
                saveConfiguration();
            }
            ImGui.TextDisabled("Minimum MP percentage before attempting to raise.");

            ImGui.EndDisabled();
            ImGui.Unindent();
        }
    }

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

            ImGui.TextDisabled("Stone Progression:");

            var enableStone = configuration.EnableStone;
            if (ImGui.Checkbox("Stone", ref enableStone))
            {
                configuration.EnableStone = enableStone;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableStoneII = configuration.EnableStoneII;
            if (ImGui.Checkbox("Stone II", ref enableStoneII))
            {
                configuration.EnableStoneII = enableStoneII;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableStoneIII = configuration.EnableStoneIII;
            if (ImGui.Checkbox("Stone III", ref enableStoneIII))
            {
                configuration.EnableStoneIII = enableStoneIII;
                saveConfiguration();
            }

            var enableStoneIV = configuration.EnableStoneIV;
            if (ImGui.Checkbox("Stone IV", ref enableStoneIV))
            {
                configuration.EnableStoneIV = enableStoneIV;
                saveConfiguration();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("Glare Progression:");

            var enableGlare = configuration.EnableGlare;
            if (ImGui.Checkbox("Glare", ref enableGlare))
            {
                configuration.EnableGlare = enableGlare;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableGlareIII = configuration.EnableGlareIII;
            if (ImGui.Checkbox("Glare III", ref enableGlareIII))
            {
                configuration.EnableGlareIII = enableGlareIII;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableGlareIV = configuration.EnableGlareIV;
            if (ImGui.Checkbox("Glare IV", ref enableGlareIV))
            {
                configuration.EnableGlareIV = enableGlareIV;
                saveConfiguration();
            }

            ImGui.Spacing();
            ImGui.TextDisabled("AoE Damage:");

            var enableHoly = configuration.EnableHoly;
            if (ImGui.Checkbox("Holy", ref enableHoly))
            {
                configuration.EnableHoly = enableHoly;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableHolyIII = configuration.EnableHolyIII;
            if (ImGui.Checkbox("Holy III", ref enableHolyIII))
            {
                configuration.EnableHolyIII = enableHolyIII;
                saveConfiguration();
            }
            ImGui.TextDisabled("Self-centered AoE (8y radius). Use when enemies are stacked.");

            ImGui.Spacing();
            ImGui.TextDisabled("Blood Lily:");

            var enableMisery = configuration.EnableAfflatusMisery;
            if (ImGui.Checkbox("Afflatus Misery", ref enableMisery))
            {
                configuration.EnableAfflatusMisery = enableMisery;
                saveConfiguration();
            }
            ImGui.TextDisabled("1240p AoE damage (costs 3 Blood Lilies). Use at 3 stacks.");

            ImGui.Spacing();

            var aoeMinEnemies = configuration.AoEDamageMinTargets;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("AoE Min Enemies", ref aoeMinEnemies, 2, 8))
            {
                configuration.AoEDamageMinTargets = aoeMinEnemies;
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

            var enableBenison = configuration.EnableDivineBenison;
            if (ImGui.Checkbox("Divine Benison", ref enableBenison))
            {
                configuration.EnableDivineBenison = enableBenison;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableAquaveil = configuration.EnableAquaveil;
            if (ImGui.Checkbox("Aquaveil", ref enableAquaveil))
            {
                configuration.EnableAquaveil = enableAquaveil;
                saveConfiguration();
            }
            ImGui.TextDisabled("Single-target shields. Applied to tank when HP < 90%.");

            ImGui.Spacing();
            ImGui.TextDisabled("Party Mitigation:");

            var enablePlenary = configuration.EnablePlenaryIndulgence;
            if (ImGui.Checkbox("Plenary Indulgence", ref enablePlenary))
            {
                configuration.EnablePlenaryIndulgence = enablePlenary;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableTemperance = configuration.EnableTemperance;
            if (ImGui.Checkbox("Temperance", ref enableTemperance))
            {
                configuration.EnableTemperance = enableTemperance;
                saveConfiguration();
            }
            ImGui.TextDisabled("Party-wide mitigation. Used when 3+ injured or avg HP low.");

            ImGui.Spacing();
            ImGui.TextDisabled("Advanced:");

            var enableBell = configuration.EnableLiturgyOfTheBell;
            if (ImGui.Checkbox("Liturgy of the Bell", ref enableBell))
            {
                configuration.EnableLiturgyOfTheBell = enableBell;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableCaress = configuration.EnableDivineCaress;
            if (ImGui.Checkbox("Divine Caress", ref enableCaress))
            {
                configuration.EnableDivineCaress = enableCaress;
                saveConfiguration();
            }
            ImGui.TextDisabled("Bell: Ground AoE reactive heal. Caress: AoE shield after Temperance.");

            ImGui.Spacing();

            var threshold = configuration.DefensiveCooldownThreshold * 100f;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderFloat("Defensive Threshold", ref threshold, 50f, 95f, "%.0f%%"))
            {
                configuration.DefensiveCooldownThreshold = threshold / 100f;
                saveConfiguration();
            }
            ImGui.TextDisabled("Use defensives when party avg HP falls below this %.");

            var useWithAoE = configuration.UseDefensivesWithAoEHeals;
            if (ImGui.Checkbox("Use with AoE Heals", ref useWithAoE))
            {
                configuration.UseDefensivesWithAoEHeals = useWithAoE;
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

            var enableAero = configuration.EnableAero;
            if (ImGui.Checkbox("Aero", ref enableAero))
            {
                configuration.EnableAero = enableAero;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableAeroII = configuration.EnableAeroII;
            if (ImGui.Checkbox("Aero II", ref enableAeroII))
            {
                configuration.EnableAeroII = enableAeroII;
                saveConfiguration();
            }

            ImGui.SameLine();
            var enableDia = configuration.EnableDia;
            if (ImGui.Checkbox("Dia", ref enableDia))
            {
                configuration.EnableDia = enableDia;
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

            var enableEsuna = configuration.EnableEsuna;
            if (ImGui.Checkbox("Enable Esuna", ref enableEsuna))
            {
                configuration.EnableEsuna = enableEsuna;
                saveConfiguration();
            }
            ImGui.TextDisabled("Automatically cleanse dispellable debuffs from party.");

            ImGui.BeginDisabled(!configuration.EnableEsuna);

            var priorityThreshold = configuration.EsunaPriorityThreshold;
            ImGui.SetNextItemWidth(200);
            if (ImGui.SliderInt("Priority Threshold", ref priorityThreshold, 0, 3))
            {
                configuration.EsunaPriorityThreshold = priorityThreshold;
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

            var enableSurecast = configuration.EnableSurecast;
            if (ImGui.Checkbox("Enable Surecast", ref enableSurecast))
            {
                configuration.EnableSurecast = enableSurecast;
                saveConfiguration();
            }
            ImGui.TextDisabled("6s immunity to knockback/draw-in. 120s cooldown.");

            ImGui.BeginDisabled(!configuration.EnableSurecast);

            var surecastMode = configuration.SurecastMode;
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo("Surecast Mode", ref surecastMode, SurecastModes, SurecastModes.Length))
            {
                configuration.SurecastMode = surecastMode;
                saveConfiguration();
            }
            ImGui.TextDisabled("Knockbacks are content-specific. Manual recommended.");

            ImGui.EndDisabled();

            ImGui.Spacing();

            // Rescue
            ImGui.TextDisabled("Rescue (Pull Party Member):");

            var enableRescue = configuration.EnableRescue;
            if (ImGui.Checkbox("Enable Rescue", ref enableRescue))
            {
                configuration.EnableRescue = enableRescue;
                saveConfiguration();
            }

            if (configuration.EnableRescue)
            {
                ImGui.TextColored(new System.Numerics.Vector4(1, 0.5f, 0, 1),
                    "WARNING: Rescue can kill party members if misused!");
            }
            ImGui.TextDisabled("Pulls party member to your position. Manual use only.");

            ImGui.Unindent();
        }
    }
}
