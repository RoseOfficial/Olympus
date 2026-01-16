using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Services.Targeting;

namespace Olympus.Windows.Config.Shared;

/// <summary>
/// Renders the General settings section including targeting, resurrection, and privacy.
/// </summary>
public sealed class GeneralSection
{
    private readonly Configuration config;
    private readonly Action save;

    private static readonly string[] StrategyNames =
    [
        "Lowest HP",
        "Highest HP",
        "Nearest",
        "Tank Assist",
        "Current Target",
        "Focus Target"
    ];

    private static readonly string[] StrategyDescriptions =
    [
        "Target enemy with lowest HP (finish off weak enemies)",
        "Target enemy with highest HP (for cleave/AoE)",
        "Target closest enemy",
        "Attack what the party tank is targeting",
        "Use your current hard target if valid",
        "Use your focus target if valid"
    ];

    private static readonly string[] RaiseModeNames = ["Raise First", "Balanced", "Heal First"];
    private static readonly string[] SurecastModes = ["Manual Only", "Use on Cooldown"];

    public GeneralSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void DrawGeneral()
    {
        DrawResurrectionSection();
        DrawPrivacySection();
    }

    public void DrawTargeting()
    {
        DrawTargetingSection();
    }

    public void DrawRoleActions()
    {
        DrawRoleActionsSection();
    }

    private void DrawTargetingSection()
    {
        ConfigUIHelpers.BeginIndent();

        var currentStrategy = (int)config.Targeting.EnemyStrategy;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Enemy Strategy", ref currentStrategy, StrategyNames, StrategyNames.Length))
        {
            config.Targeting.EnemyStrategy = (EnemyTargetingStrategy)currentStrategy;
            save();
        }
        ImGui.TextDisabled(StrategyDescriptions[currentStrategy]);

        ConfigUIHelpers.Spacing();

        // Only show tank assist fallback when tank assist is selected
        if (config.Targeting.EnemyStrategy == EnemyTargetingStrategy.TankAssist)
        {
            var useFallback = config.Targeting.UseTankAssistFallback;
            if (ImGui.Checkbox("Fallback to Lowest HP", ref useFallback))
            {
                config.Targeting.UseTankAssistFallback = useFallback;
                save();
            }
            ImGui.TextDisabled("If no tank target found, use Lowest HP instead.");
        }

        ConfigUIHelpers.Spacing();

        // Movement tolerance
        var moveTolerance = config.MovementTolerance * 1000f; // Convert to ms
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat("Movement Tolerance", ref moveTolerance, 0f, 500f, "%.0f ms"))
        {
            config.MovementTolerance = moveTolerance / 1000f;
            save();
        }
        ImGui.TextDisabled("Delay after stopping before casting. Lower = faster, higher = safer.");

        ConfigUIHelpers.EndIndent();
    }

    private void DrawResurrectionSection()
    {
        if (ConfigUIHelpers.SectionHeader("Resurrection"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableRaise = config.Resurrection.EnableRaise;
            if (ImGui.Checkbox("Enable Raise", ref enableRaise))
            {
                config.Resurrection.EnableRaise = enableRaise;
                save();
            }
            ImGui.TextDisabled("Automatically resurrect dead party members.");

            ConfigUIHelpers.BeginDisabledGroup(!config.Resurrection.EnableRaise);

            // Raise Execution Mode
            var currentMode = (int)config.Resurrection.RaiseMode;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("Raise Priority", ref currentMode, RaiseModeNames, RaiseModeNames.Length))
            {
                config.Resurrection.RaiseMode = (RaiseExecutionMode)currentMode;
                save();
            }
            var modeDesc = config.Resurrection.RaiseMode switch
            {
                RaiseExecutionMode.RaiseFirst => "Prioritize raising over other actions",
                RaiseExecutionMode.Balanced => "Raise in weave windows, don't interrupt healing",
                RaiseExecutionMode.HealFirst => "Only raise when party HP is stable",
                _ => ""
            };
            ImGui.TextDisabled(modeDesc);

            ConfigUIHelpers.Spacing();
            var allowHardcast = config.Resurrection.AllowHardcastRaise;
            if (ImGui.Checkbox("Allow Hardcast Raise", ref allowHardcast))
            {
                config.Resurrection.AllowHardcastRaise = allowHardcast;
                save();
            }
            ImGui.TextDisabled("Cast Raise without Swiftcast (8s cast). Use with caution.");

            config.Resurrection.RaiseMpThreshold = ConfigUIHelpers.ThresholdSlider("Min MP for Raise",
                config.Resurrection.RaiseMpThreshold, 10f, 50f, "Minimum MP percentage before attempting to raise.", save);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawPrivacySection()
    {
        if (ConfigUIHelpers.SectionHeader("Privacy", false))
        {
            ConfigUIHelpers.BeginIndent();

            var telemetryEnabled = config.TelemetryEnabled;
            if (ImGui.Checkbox("Send anonymous usage statistics", ref telemetryEnabled))
            {
                config.TelemetryEnabled = telemetryEnabled;
                save();
            }
            ImGui.TextDisabled("Only sends plugin version. No personal data.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawRoleActionsSection()
    {
        ConfigUIHelpers.BeginIndent();

        // Esuna
        ConfigUIHelpers.SectionLabel("Esuna (Cleanse):");

        var enableEsuna = config.RoleActions.EnableEsuna;
        if (ImGui.Checkbox("Enable Esuna", ref enableEsuna))
        {
            config.RoleActions.EnableEsuna = enableEsuna;
            save();
        }
        ImGui.TextDisabled("Automatically cleanse dispellable debuffs from party.");

        ConfigUIHelpers.BeginDisabledGroup(!config.RoleActions.EnableEsuna);

        var priorityThreshold = config.RoleActions.EsunaPriorityThreshold;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt("Priority Threshold", ref priorityThreshold, 0, 3))
        {
            config.RoleActions.EsunaPriorityThreshold = priorityThreshold;
            save();
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

        ConfigUIHelpers.EndDisabledGroup();

        ConfigUIHelpers.Spacing();

        // Surecast
        ConfigUIHelpers.SectionLabel("Surecast (Knockback Immunity):");

        var enableSurecast = config.RoleActions.EnableSurecast;
        if (ImGui.Checkbox("Enable Surecast", ref enableSurecast))
        {
            config.RoleActions.EnableSurecast = enableSurecast;
            save();
        }
        ImGui.TextDisabled("6s immunity to knockback/draw-in. 120s cooldown.");

        ConfigUIHelpers.BeginDisabledGroup(!config.RoleActions.EnableSurecast);

        var surecastMode = config.RoleActions.SurecastMode;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo("Surecast Mode", ref surecastMode, SurecastModes, SurecastModes.Length))
        {
            config.RoleActions.SurecastMode = surecastMode;
            save();
        }
        ImGui.TextDisabled("Knockbacks are content-specific. Manual recommended.");

        ConfigUIHelpers.EndDisabledGroup();

        ConfigUIHelpers.Spacing();

        // Rescue
        ConfigUIHelpers.SectionLabel("Rescue (Pull Party Member):");

        var enableRescue = config.RoleActions.EnableRescue;
        if (ImGui.Checkbox("Enable Rescue", ref enableRescue))
        {
            config.RoleActions.EnableRescue = enableRescue;
            save();
        }

        if (config.RoleActions.EnableRescue)
        {
            ConfigUIHelpers.DangerText("WARNING: Rescue can kill party members if misused!");
        }
        ImGui.TextDisabled("Pulls party member to your position. Manual use only.");

        ConfigUIHelpers.EndIndent();
    }
}
