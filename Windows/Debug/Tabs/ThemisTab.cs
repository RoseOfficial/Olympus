using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.ThemisCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Paladin tab: Themis-specific debug info including Oath Gauge, combo state, and cooldowns.
/// </summary>
public static class ThemisTab
{
    public static void Draw(ThemisDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.PaladinNotActive, "Paladin rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToPaladin, "Switch to Paladin to see debug info."));
            return;
        }

        DrawStatusSection(state);
        ImGui.Spacing();

        DrawGaugeSection(state);
        ImGui.Spacing();

        DrawComboSection(state);
        ImGui.Spacing();

        DrawBuffSection(state);
        ImGui.Spacing();

        DrawMitigationSection(state);
        ImGui.Spacing();

        DrawExecutionFlowSection(state);
        ImGui.Spacing();

        DrawModuleStates(state);
    }

    private static void DrawStatusSection(ThemisDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.TankStatus, "Status"));
        ImGui.Separator();

        if (ImGui.BeginTable("ThemisStatusTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.IsMainTankLabel, "Role:"));
            ImGui.TableNextColumn();
            if (state.IsMainTank)
                ImGui.TextColored(new Vector4(0.4f, 0.8f, 1f, 1f), Loc.T(LocalizedStrings.Debug.MainTankValue, "Main Tank"));
            else
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.4f, 1f), Loc.T(LocalizedStrings.Debug.OffTankValue, "Off Tank"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PlannedActionLabel, "Planned Action:"));
            ImGui.TableNextColumn();
            var actionColor = !string.IsNullOrEmpty(state.PlannedAction) && state.PlannedAction != "None"
                ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(actionColor, string.IsNullOrEmpty(state.PlannedAction) ? "—" : state.PlannedAction);

            ImGui.EndTable();
        }
    }

    private static void DrawGaugeSection(ThemisDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Gauge, "Gauge"));
        ImGui.Separator();

        if (ImGui.BeginTable("ThemisGaugeTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.OathGaugeLabel, "Oath Gauge:"));
            ImGui.TableNextColumn();
            ImGui.ProgressBar(state.OathGauge / 100f, new Vector2(-1, 0), $"{state.OathGauge}/100");

            ImGui.EndTable();
        }
    }

    private static void DrawComboSection(ThemisDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Combo, "Combo"));
        ImGui.Separator();

        if (ImGui.BeginTable("ThemisComboTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ComboState, "Combo Step:"));
            ImGui.TableNextColumn();
            ImGui.Text($"{state.ComboStep}");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ComboTimer, "Combo Timer:"));
            ImGui.TableNextColumn();
            var timerColor = state.ComboTimeRemaining < 5f
                ? new Vector4(1f, 0.5f, 0.5f, 1f)
                : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(timerColor, $"{state.ComboTimeRemaining:F1}s");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.AtonementStepLabel, "Atonement Step:"));
            ImGui.TableNextColumn();
            if (state.AtonementStep > 0)
                ImGui.TextColored(new Vector4(0.9f, 0.7f, 0.3f, 1f), $"{state.AtonementStep}");
            else
                ImGui.TextDisabled("—");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ConfiteorStepLabel, "Confiteor Step:"));
            ImGui.TableNextColumn();
            if (state.ConfiteorStep > 0)
                ImGui.TextColored(new Vector4(0.9f, 0.7f, 0.3f, 1f), $"{state.ConfiteorStep}");
            else
                ImGui.TextDisabled("—");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SwordOathStacksLabel, "Sword Oath:"));
            ImGui.TableNextColumn();
            if (state.SwordOathStacks > 0)
                ImGui.TextColored(new Vector4(0.9f, 0.8f, 0.5f, 1f), $"{state.SwordOathStacks}");
            else
                ImGui.TextDisabled("—");

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(ThemisDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs"));
        ImGui.Separator();

        if (ImGui.BeginTable("ThemisBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.FightOrFlight, "Fight or Flight:"));
            ImGui.TableNextColumn();
            if (state.HasFightOrFlight)
                ImGui.TextColored(new Vector4(1f, 0.4f, 0.4f, 1f), $"{state.FightOrFlightRemaining:F1}s");
            else
                ImGui.TextDisabled("—");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Requiescat, "Requiescat:"));
            ImGui.TableNextColumn();
            if (state.HasRequiescat)
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 1f, 1f), Loc.T(LocalizedStrings.Debug.RequiescatStacksLabel, $"{state.RequiescatStacks} stacks"));
            else
                ImGui.TextDisabled("—");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.GoringBlade, "Goring Blade:"));
            ImGui.TableNextColumn();
            if (state.GoringBladeRemaining > 0f)
            {
                var dotColor = state.GoringBladeRemaining < 3f
                    ? new Vector4(1f, 0.5f, 0.5f, 1f)
                    : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(dotColor, $"{state.GoringBladeRemaining:F1}s");
            }
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.NotApplied, "Not applied"));

            ImGui.EndTable();
        }
    }

    private static void DrawMitigationSection(ThemisDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.MitigationHeader, "Mitigation"));
        ImGui.Separator();

        if (ImGui.BeginTable("ThemisMitigationTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ActiveMitigationsLabel, "Active:"));
            ImGui.TableNextColumn();
            if (state.HasActiveMitigation && !string.IsNullOrEmpty(state.ActiveMitigations))
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), state.ActiveMitigations);
            else
                ImGui.TextDisabled("—");

            ImGui.EndTable();
        }
    }

    private static void DrawExecutionFlowSection(ThemisDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.ExecutionFlowHeader, "Execution Flow"));
        ImGui.Separator();

        if (ImGui.BeginTable("ThemisFlowTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.InCombatLabel, "In Combat:"));
            ImGui.TableNextColumn();
            if (state.InCombat)
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.3f, 1f), "Yes");
            else
                ImGui.TextDisabled("No");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.CanExecuteGcdLabel, "Can GCD:"));
            ImGui.TableNextColumn();
            if (state.CanExecuteGcd)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Yes");
            else
                ImGui.TextDisabled("No");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.CanExecuteOgcdLabel, "Can oGCD:"));
            ImGui.TableNextColumn();
            if (state.CanExecuteOgcd)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Yes");
            else
                ImGui.TextDisabled("No");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.GcdStateLabel, "GCD State:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(string.IsNullOrEmpty(state.GcdState) ? "—" : state.GcdState);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.GcdRemainingLabel, "GCD Remaining:"));
            ImGui.TableNextColumn();
            ImGui.Text($"{state.GcdRemaining:F2}s");

            ImGui.EndTable();
        }

        if (!string.IsNullOrEmpty(state.ExecutionFlow))
        {
            ImGui.Spacing();
            ImGui.TextDisabled(state.ExecutionFlow);
        }
    }

    private static void DrawModuleStates(ThemisDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.ModuleStatesHeader, "Module States"));
        ImGui.Separator();

        if (ImGui.BeginTable("ThemisModuleTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DamageStateLabel, "Damage:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(string.IsNullOrEmpty(state.DamageState) ? "—" : state.DamageState);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.MitigationStateLabel, "Mitigation:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(string.IsNullOrEmpty(state.MitigationState) ? "—" : state.MitigationState);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.BuffStateLabel, "Buff:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(string.IsNullOrEmpty(state.BuffState) ? "—" : state.BuffState);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.EnmityStateLabel, "Enmity:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(string.IsNullOrEmpty(state.EnmityState) ? "—" : state.EnmityState);

            ImGui.EndTable();
        }
    }
}
