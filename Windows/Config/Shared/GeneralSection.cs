using System;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;
using Olympus.Services.Targeting;

namespace Olympus.Windows.Config.Shared;

/// <summary>
/// Renders the General settings section including targeting, resurrection, and privacy.
/// </summary>
public sealed class GeneralSection
{
    private readonly Configuration config;
    private readonly Action save;

    private string[] GetStrategyNames() =>
    [
        Loc.T(LocalizedStrings.Targeting.StrategyLowestHp, "Lowest HP"),
        Loc.T(LocalizedStrings.Targeting.StrategyHighestHp, "Highest HP"),
        Loc.T(LocalizedStrings.Targeting.StrategyNearest, "Nearest"),
        Loc.T(LocalizedStrings.Targeting.StrategyTankAssist, "Tank Assist"),
        Loc.T(LocalizedStrings.Targeting.StrategyCurrentTarget, "Current Target"),
        Loc.T(LocalizedStrings.Targeting.StrategyFocusTarget, "Focus Target")
    ];

    private string[] GetStrategyDescriptions() =>
    [
        Loc.T(LocalizedStrings.Targeting.StrategyDescLowestHp, "Target enemy with lowest HP (finish off weak enemies)"),
        Loc.T(LocalizedStrings.Targeting.StrategyDescHighestHp, "Target enemy with highest HP (for cleave/AoE)"),
        Loc.T(LocalizedStrings.Targeting.StrategyDescNearest, "Target closest enemy"),
        Loc.T(LocalizedStrings.Targeting.StrategyDescTankAssist, "Attack what the party tank is targeting"),
        Loc.T(LocalizedStrings.Targeting.StrategyDescCurrentTarget, "Use your current hard target if valid"),
        Loc.T(LocalizedStrings.Targeting.StrategyDescFocusTarget, "Use your focus target if valid")
    ];

    private string[] GetRaiseModeNames() =>
    [
        Loc.T(LocalizedStrings.Resurrection.RaiseModeFirst, "Raise First"),
        Loc.T(LocalizedStrings.Resurrection.RaiseModeBalanced, "Balanced"),
        Loc.T(LocalizedStrings.Resurrection.RaiseModeHealFirst, "Heal First")
    ];

    private string[] GetSurecastModes() =>
    [
        Loc.T(LocalizedStrings.RoleActions.SurecastModeManual, "Manual Only"),
        Loc.T(LocalizedStrings.RoleActions.SurecastModeAuto, "Use on Cooldown")
    ];

    public GeneralSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void DrawGeneral()
    {
        DrawCombatBehaviorSection();
        DrawWindowBehaviorSection();
        DrawResurrectionSection();
        DrawLanguageSection();
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

        var strategyNames = this.GetStrategyNames();
        var strategyDescriptions = this.GetStrategyDescriptions();
        var currentStrategy = (int)this.config.Targeting.EnemyStrategy;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo(Loc.T(LocalizedStrings.Targeting.EnemyStrategy, "Enemy Strategy"), ref currentStrategy, strategyNames, strategyNames.Length))
        {
            this.config.Targeting.EnemyStrategy = (EnemyTargetingStrategy)currentStrategy;
            this.save();
        }
        ImGui.TextDisabled(strategyDescriptions[currentStrategy]);

        ConfigUIHelpers.Spacing();

        // Only show tank assist fallback when tank assist is selected
        if (this.config.Targeting.EnemyStrategy == EnemyTargetingStrategy.TankAssist)
        {
            var useFallback = this.config.Targeting.UseTankAssistFallback;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Targeting.FallbackToLowestHp, "Fallback to Lowest HP"), ref useFallback))
            {
                this.config.Targeting.UseTankAssistFallback = useFallback;
                this.save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Targeting.FallbackToLowestHpDesc, "If no tank target found, use Lowest HP instead."));
        }

        ConfigUIHelpers.Spacing();

        // Movement tolerance
        var moveTolerance = this.config.MovementTolerance * 1000f; // Convert to ms
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderFloat(Loc.T(LocalizedStrings.Targeting.MovementTolerance, "Movement Tolerance"), ref moveTolerance, 0f, 500f, "%.0f ms"))
        {
            this.config.MovementTolerance = moveTolerance / 1000f;
            this.save();
        }
        ImGui.TextDisabled(Loc.T(LocalizedStrings.Targeting.MovementToleranceDesc, "Delay after stopping before casting. Lower = faster, higher = safer."));

        ConfigUIHelpers.EndIndent();
    }

    private void DrawCombatBehaviorSection()
    {
        if (ConfigUIHelpers.SectionHeader("Combat Behavior"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableOnAutoAttack = this.config.EnableOnAutoAttack;
            if (ImGui.Checkbox("Start rotation when weapon is drawn", ref enableOnAutoAttack))
            {
                this.config.EnableOnAutoAttack = enableOnAutoAttack;
                this.save();
            }
            ImGui.TextDisabled("When enabled, Olympus starts executing the rotation as soon as you draw your weapon, before the server sets the in-combat flag.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawWindowBehaviorSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T("config.window.section", "Window Behavior")))
        {
            ConfigUIHelpers.BeginIndent();

            var preventEscape = this.config.PreventEscapeClose;
            if (ImGui.Checkbox(Loc.T("config.window.prevent_escape_close", "Prevent closing with Escape key"), ref preventEscape))
            {
                this.config.PreventEscapeClose = preventEscape;
                this.save();
            }
            ImGui.TextDisabled(Loc.T("config.window.prevent_escape_close_desc", "When enabled, pressing Escape will not close the Olympus window."));

            ConfigUIHelpers.Spacing();

            var showCutscenes = this.config.ShowDuringCutscenes;
            if (ImGui.Checkbox(Loc.T("config.window.show_during_cutscenes", "Show window during cutscenes"), ref showCutscenes))
            {
                this.config.ShowDuringCutscenes = showCutscenes;
                this.save();
            }
            ImGui.TextDisabled(Loc.T("config.window.show_during_cutscenes_desc", "When enabled, Olympus windows stay visible during in-game cutscenes."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawResurrectionSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Resurrection.Section, "Resurrection")))
        {
            ConfigUIHelpers.BeginIndent();

            var enableRaise = this.config.Resurrection.EnableRaise;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Resurrection.EnableRaise, "Enable Raise"), ref enableRaise))
            {
                this.config.Resurrection.EnableRaise = enableRaise;
                this.save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Resurrection.EnableRaiseDesc, "Automatically resurrect dead party members."));

            ConfigUIHelpers.BeginDisabledGroup(!this.config.Resurrection.EnableRaise);

            // Raise Execution Mode
            var raiseModeNames = this.GetRaiseModeNames();
            var currentMode = (int)this.config.Resurrection.RaiseMode;
            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo(Loc.T(LocalizedStrings.Resurrection.RaisePriority, "Raise Priority"), ref currentMode, raiseModeNames, raiseModeNames.Length))
            {
                this.config.Resurrection.RaiseMode = (RaiseExecutionMode)currentMode;
                this.save();
            }
            var modeDesc = this.config.Resurrection.RaiseMode switch
            {
                RaiseExecutionMode.RaiseFirst => Loc.T(LocalizedStrings.Resurrection.RaiseModeDescFirst, "Prioritize raising over other actions"),
                RaiseExecutionMode.Balanced => Loc.T(LocalizedStrings.Resurrection.RaiseModeDescBalanced, "Raise in weave windows, don't interrupt healing"),
                RaiseExecutionMode.HealFirst => Loc.T(LocalizedStrings.Resurrection.RaiseModeDescHealFirst, "Only raise when party HP is stable"),
                _ => ""
            };
            ImGui.TextDisabled(modeDesc);

            ConfigUIHelpers.Spacing();
            var allowHardcast = this.config.Resurrection.AllowHardcastRaise;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Resurrection.AllowHardcast, "Allow Hardcast Raise"), ref allowHardcast))
            {
                this.config.Resurrection.AllowHardcastRaise = allowHardcast;
                this.save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Resurrection.AllowHardcastDesc, "Cast Raise without Swiftcast (8s cast). Use with caution."));

            this.config.Resurrection.RaiseMpThreshold = ConfigUIHelpers.ThresholdSlider(
                Loc.T(LocalizedStrings.Resurrection.MinMpForRaise, "Min MP for Raise"),
                this.config.Resurrection.RaiseMpThreshold, 10f, 50f,
                Loc.T("config.resurrection.min_mp_for_raise_desc", "Minimum MP percentage before attempting to raise."),
                this.save);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawLanguageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Language.Section, "Language")))
        {
            ConfigUIHelpers.BeginIndent();

            // Available languages with display names
            // Show native name + English name for clarity
            var displayNames = new[]
            {
                Loc.T(LocalizedStrings.Language.Auto, "Auto (Game Client)"),
                "English",
                "日本語 (Japanese)",
                "简体中文 (Chinese)",
                "한국어 (Korean)",
                "Deutsch (German)",
                "Français (French)",
            };
            var languageCodes = new[] { "", "en", "ja", "zh", "ko", "de", "fr" };

            // Find current selection index
            var currentOverride = this.config.LanguageOverride ?? "";
            var selectedIndex = currentOverride switch
            {
                "" => 0,
                "en" => 1,
                "ja" => 2,
                "zh" => 3,
                "ko" => 4,
                "de" => 5,
                "fr" => 6,
                _ => 0,
            };

            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo(
                Loc.T(LocalizedStrings.Language.Select, "Language"),
                ref selectedIndex,
                displayNames,
                displayNames.Length))
            {
                var chosen = languageCodes[selectedIndex];
                this.config.LanguageOverride = chosen;
                this.save();

                // Apply language change immediately using the chosen code directly.
                // When Auto ("") is selected, fall back to ReloadLanguage so it can
                // determine the effective language from the game client.
                if (!string.IsNullOrEmpty(chosen))
                    OlympusLocalization.Instance?.SetLanguage(chosen);
                else
                    OlympusLocalization.Instance?.ReloadLanguage();
            }

            ImGui.TextDisabled(Loc.T(
                LocalizedStrings.Language.SelectDesc,
                "Select your preferred language. Auto uses the game client language."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawPrivacySection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Privacy.Section, "Privacy"), false))
        {
            ConfigUIHelpers.BeginIndent();

            var telemetryEnabled = this.config.TelemetryEnabled;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Privacy.Telemetry, "Send anonymous usage statistics"), ref telemetryEnabled))
            {
                this.config.TelemetryEnabled = telemetryEnabled;
                this.save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Privacy.TelemetryDesc, "Only sends plugin version. No personal data."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawRoleActionsSection()
    {
        ConfigUIHelpers.BeginIndent();

        // Esuna
        ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.RoleActions.EsunaSection, "Esuna (Cleanse):"));

        var enableEsuna = this.config.RoleActions.EnableEsuna;
        if (ImGui.Checkbox(Loc.T(LocalizedStrings.RoleActions.EnableEsuna, "Enable Esuna"), ref enableEsuna))
        {
            this.config.RoleActions.EnableEsuna = enableEsuna;
            this.save();
        }
        ImGui.TextDisabled(Loc.T(LocalizedStrings.RoleActions.EnableEsunaDesc, "Automatically cleanse dispellable debuffs from party."));

        ConfigUIHelpers.BeginDisabledGroup(!this.config.RoleActions.EnableEsuna);

        var priorityThreshold = this.config.RoleActions.EsunaPriorityThreshold;
        ImGui.SetNextItemWidth(200);
        if (ImGui.SliderInt(Loc.T(LocalizedStrings.RoleActions.EsunaPriorityThreshold, "Priority Threshold"), ref priorityThreshold, 0, 3))
        {
            this.config.RoleActions.EsunaPriorityThreshold = priorityThreshold;
            this.save();
        }

        var priorityDesc = priorityThreshold switch
        {
            0 => Loc.T(LocalizedStrings.RoleActions.EsunaPriorityLethal, "Lethal only (Doom, Throttle)"),
            1 => Loc.T(LocalizedStrings.RoleActions.EsunaPriorityHigh, "High+ (also Vulnerability Up)"),
            2 => Loc.T(LocalizedStrings.RoleActions.EsunaPriorityMedium, "Medium+ (also Paralysis, Silence)"),
            3 => Loc.T(LocalizedStrings.RoleActions.EsunaPriorityAll, "All dispellable debuffs"),
            _ => "Unknown"
        };
        ImGui.TextDisabled(priorityDesc);

        ConfigUIHelpers.EndDisabledGroup();

        ConfigUIHelpers.Spacing();

        // Surecast
        ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.RoleActions.SurecastSection, "Surecast (Knockback Immunity):"));

        var enableSurecast = this.config.RoleActions.EnableSurecast;
        if (ImGui.Checkbox(Loc.T(LocalizedStrings.RoleActions.EnableSurecast, "Enable Surecast"), ref enableSurecast))
        {
            this.config.RoleActions.EnableSurecast = enableSurecast;
            this.save();
        }
        ImGui.TextDisabled(Loc.T(LocalizedStrings.RoleActions.EnableSurecastDesc, "6s immunity to knockback/draw-in. 120s cooldown."));

        ConfigUIHelpers.BeginDisabledGroup(!this.config.RoleActions.EnableSurecast);

        var surecastModes = this.GetSurecastModes();
        var surecastMode = this.config.RoleActions.SurecastMode;
        ImGui.SetNextItemWidth(200);
        if (ImGui.Combo(Loc.T(LocalizedStrings.RoleActions.SurecastMode, "Surecast Mode"), ref surecastMode, surecastModes, surecastModes.Length))
        {
            this.config.RoleActions.SurecastMode = surecastMode;
            this.save();
        }
        ImGui.TextDisabled(Loc.T(LocalizedStrings.RoleActions.SurecastModeDesc, "Knockbacks are content-specific. Manual recommended."));

        ConfigUIHelpers.EndDisabledGroup();

        ConfigUIHelpers.Spacing();

        // Rescue
        ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.RoleActions.RescueSection, "Rescue (Pull Party Member):"));

        var enableRescue = this.config.RoleActions.EnableRescue;
        if (ImGui.Checkbox(Loc.T(LocalizedStrings.RoleActions.EnableRescue, "Enable Rescue"), ref enableRescue))
        {
            this.config.RoleActions.EnableRescue = enableRescue;
            this.save();
        }

        if (this.config.RoleActions.EnableRescue)
        {
            ConfigUIHelpers.DangerText(Loc.T(LocalizedStrings.RoleActions.RescueWarning, "WARNING: Rescue can kill party members if misused!"));
        }
        ImGui.TextDisabled(Loc.T(LocalizedStrings.RoleActions.EnableRescueDesc, "Pulls party member to your position. Manual use only."));

        ConfigUIHelpers.EndIndent();
    }
}
