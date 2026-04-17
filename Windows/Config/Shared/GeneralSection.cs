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
        DrawCoachingSummarySection();
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
            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Targeting.FallbackToLowestHp, "Fallback to Lowest HP"), () => this.config.Targeting.UseTankAssistFallback, v => this.config.Targeting.UseTankAssistFallback = v,
                Loc.T(LocalizedStrings.Targeting.FallbackToLowestHpDesc, "If no tank target found, use Lowest HP instead."), this.save);
        }

        ConfigUIHelpers.Spacing();

        // Safety toggles — protect against gaze mechanics and unintentional target retargeting.
        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.Targeting.PauseWhenNoTarget, "Pause damage when no target"),
            () => this.config.Targeting.PauseWhenNoTarget,
            v => this.config.Targeting.PauseWhenNoTarget = v,
            Loc.T(LocalizedStrings.Targeting.PauseWhenNoTargetDesc,
                "Stop attacking when you drop your target. Lets you look away for gaze mechanics or disengage without Olympus picking a new enemy."),
            this.save);

        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.Targeting.SuppressDamageOnForcedMovement, "Pause damage during forced movement"),
            () => this.config.Targeting.SuppressDamageOnForcedMovement,
            v => this.config.Targeting.SuppressDamageOnForcedMovement = v,
            Loc.T(LocalizedStrings.Targeting.SuppressDamageOnForcedMovementDesc,
                "Stop firing damage actions while a Forced March or Confusion debuff is active. Prevents cast-time GCDs from failing repeatedly while your character moves involuntarily."),
            this.save);

        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.Targeting.PauseAllOnStandStillPunisher, "Pause everything during Pyretic"),
            () => this.config.Targeting.PauseAllOnStandStillPunisher,
            v => this.config.Targeting.PauseAllOnStandStillPunisher = v,
            Loc.T(LocalizedStrings.Targeting.PauseAllOnStandStillPunisherDesc,
                "Halt the entire rotation — including healing, mitigation, and oGCDs — while a Pyretic-style 'any action kills you' debuff is active. Resumes automatically the frame it falls off."),
            this.save);

        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.Targeting.StrictCurrentTargetStrategy, "Strict explicit-target mode"),
            () => this.config.Targeting.StrictCurrentTargetStrategy,
            v => this.config.Targeting.StrictCurrentTargetStrategy = v,
            Loc.T(LocalizedStrings.Targeting.StrictCurrentTargetStrategyDesc,
                "When using Current Target or Focus Target strategy, never fall back to another enemy if yours is gone."),
            this.save);

        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.Targeting.SafeGapCloser, "Safe gap closers"),
            () => this.config.Targeting.SafeGapCloser,
            v => this.config.Targeting.SafeGapCloser = v,
            Loc.T(LocalizedStrings.Targeting.SafeGapCloserDesc,
                "Only use gap closers on your explicitly selected target, and block them when you are moving away from the target (spread markers, ground AoE at the end point)."),
            this.save);

        ConfigUIHelpers.Toggle(
            Loc.T(LocalizedStrings.Targeting.InvulnerabilityFiltering, "Skip invulnerable enemies"),
            () => this.config.Targeting.EnableInvulnerabilityFiltering,
            v => this.config.Targeting.EnableInvulnerabilityFiltering = v,
            Loc.T(LocalizedStrings.Targeting.InvulnerabilityFilteringDesc,
                "Auto-targeting ignores enemies with invulnerability effects (boss phase transitions, immune adds, invulnerable objects). Prevents wasting actions on targets that take no damage."),
            this.save);

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
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.General.CombatBehaviorHeader, "Combat Behavior")))
        {
            ConfigUIHelpers.BeginIndent();

            var enableOnAutoAttack = this.config.EnableOnAutoAttack;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.General.StartOnAutoAttack, "Start rotation on auto-attack"), ref enableOnAutoAttack))
            {
                this.config.EnableOnAutoAttack = enableOnAutoAttack;
                this.save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.General.StartOnAutoAttackDesc, "When enabled, Olympus starts executing the rotation as soon as auto-attack is active on a target, before the server sets the in-combat flag."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawWindowBehaviorSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Window.Section, "Window Behavior")))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Window.PreventEscapeClose, "Prevent closing with Escape key"), () => this.config.PreventEscapeClose, v => this.config.PreventEscapeClose = v,
                Loc.T(LocalizedStrings.Window.PreventEscapeCloseDesc, "When enabled, pressing Escape will not close the Olympus window."), this.save);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Window.ShowDuringCutscenes, "Show window during cutscenes"), () => this.config.ShowDuringCutscenes, v => this.config.ShowDuringCutscenes = v,
                Loc.T(LocalizedStrings.Window.ShowDuringCutsceneDesc, "When enabled, Olympus windows stay visible during in-game cutscenes."), this.save);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Overlay.ShowMechanicsForecast, "Show mechanic forecast in overlay"), () => this.config.Overlay.ShowMechanicsForecast, v => this.config.Overlay.ShowMechanicsForecast = v,
                Loc.T(LocalizedStrings.Overlay.ShowMechanicsForecastDesc, "Displays upcoming fight mechanics (raidwides, tank busters, phases) with countdown timers in the overlay. Only visible when a fight timeline is loaded."), this.save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawCoachingSummarySection()
    {
        if (ConfigUIHelpers.SectionHeader(
            Loc.T(LocalizedStrings.FightSummary.WindowTitle, "Coaching Summary"), "CoachingSummary"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.FightSummary.ShowOnCombatEnd, "Show coaching summary after combat"),
                () => config.Analytics.ShowSummaryOnCombatEnd,
                v => config.Analytics.ShowSummaryOnCombatEnd = v,
                null, save);

            ConfigUIHelpers.Spacing();

            config.Analytics.SummaryMinimumDurationSeconds = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.FightSummary.MinDuration, "Minimum Fight Duration (s)"),
                config.Analytics.SummaryMinimumDurationSeconds, 30, 300,
                null, save, v => config.Analytics.SummaryMinimumDurationSeconds = v);

            ConfigUIHelpers.Spacing();

            config.Analytics.SummaryPopupDelaySeconds = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.FightSummary.PopupDelay, "Popup Delay (s)"),
                config.Analytics.SummaryPopupDelaySeconds, 0f, 10f, "%.1f",
                null, save, v => config.Analytics.SummaryPopupDelaySeconds = v);

            ConfigUIHelpers.Spacing();

            config.Analytics.MaxStoredSummaries = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.FightSummary.MaxStored, "Max Stored Summaries"),
                config.Analytics.MaxStoredSummaries, 5, 25,
                null, save, v => config.Analytics.MaxStoredSummaries = v);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawResurrectionSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Resurrection.Section, "Resurrection")))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Resurrection.EnableRaise, "Enable Raise"), () => this.config.Resurrection.EnableRaise, v => this.config.Resurrection.EnableRaise = v,
                Loc.T(LocalizedStrings.Resurrection.EnableRaiseDesc, "Automatically resurrect dead party members."), this.save);

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
            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Resurrection.AllowHardcast, "Allow Hardcast Raise"), () => this.config.Resurrection.AllowHardcastRaise, v => this.config.Resurrection.AllowHardcastRaise = v,
                Loc.T(LocalizedStrings.Resurrection.AllowHardcastDesc, "Cast Raise without Swiftcast (8s cast). Use with caution."), this.save);

            this.config.Resurrection.RaiseMpThreshold = ConfigUIHelpers.ThresholdSlider(
                Loc.T(LocalizedStrings.Resurrection.MinMpForRaise, "Min MP for Raise"),
                this.config.Resurrection.RaiseMpThreshold, 10f, 50f,
                Loc.T(LocalizedStrings.Resurrection.MinMpForRaiseDesc, "Minimum MP percentage before attempting to raise."),
                this.save, v => this.config.Resurrection.RaiseMpThreshold = v);

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
                "Español (Spanish)",
                "Português (Portuguese)",
                "Русский (Russian)",
            };
            var languageCodes = new[] { "", "en", "ja", "zh", "ko", "de", "fr", "es", "pt", "ru" };

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
                "es" => 7,
                "pt" => 8,
                "ru" => 9,
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

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Privacy.Telemetry, "Send anonymous usage statistics"), () => this.config.TelemetryEnabled, v => this.config.TelemetryEnabled = v,
                Loc.T(LocalizedStrings.Privacy.TelemetryDesc, "Only sends plugin version. No personal data."), this.save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawRoleActionsSection()
    {
        ConfigUIHelpers.BeginIndent();

        // Esuna
        ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.RoleActions.EsunaSection, "Esuna (Cleanse):"));

        ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.RoleActions.EnableEsuna, "Enable Esuna"), () => this.config.RoleActions.EnableEsuna, v => this.config.RoleActions.EnableEsuna = v,
            Loc.T(LocalizedStrings.RoleActions.EnableEsunaDesc, "Automatically cleanse dispellable debuffs from party."), this.save);

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

        ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.RoleActions.EnableSurecast, "Enable Surecast"), () => this.config.RoleActions.EnableSurecast, v => this.config.RoleActions.EnableSurecast = v,
            Loc.T(LocalizedStrings.RoleActions.EnableSurecastDesc, "6s immunity to knockback/draw-in. 120s cooldown."), this.save);

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

        ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.RoleActions.EnableRescue, "Enable Rescue"), () => this.config.RoleActions.EnableRescue, v => this.config.RoleActions.EnableRescue = v, null, this.save);

        if (this.config.RoleActions.EnableRescue)
        {
            ConfigUIHelpers.DangerText(Loc.T(LocalizedStrings.RoleActions.RescueWarning, "WARNING: Rescue can kill party members if misused!"));
        }
        ImGui.TextDisabled(Loc.T(LocalizedStrings.RoleActions.EnableRescueDesc, "Pulls party member to your position. Manual use only."));

        ConfigUIHelpers.EndIndent();
    }
}
