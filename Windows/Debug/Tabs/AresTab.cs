using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.AresCore.Context;
using Olympus.Windows.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Warrior tab: Ares-specific debug info including Beast Gauge, combo state, and buffs.
/// </summary>
public static class AresTab
{
    public static void Draw(AresDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.WarriorNotActive, "Warrior rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToWarrior, "Switch to Warrior to see debug info."));
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

        DrawModuleStates(state);
    }

    private static void DrawStatusSection(AresDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.TankStatus, "Status"), "AresStatusTable", () =>
        {
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
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TankStance, "Stance:"));
            ImGui.TableNextColumn();
            if (state.HasDefiance)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Defiance, "Defiance"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.Defiance, "Defiance"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.NearbyEnemies, "Nearby Enemies:"));
            ImGui.TableNextColumn();
            ImGui.Text($"{state.NearbyEnemies}");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PlannedActionLabel, "Planned Action:"));
            ImGui.TableNextColumn();
            var actionColor = !string.IsNullOrEmpty(state.PlannedAction) && state.PlannedAction != "None"
                ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(actionColor, string.IsNullOrEmpty(state.PlannedAction) ? "—" : state.PlannedAction);
        });
    }

    private static void DrawGaugeSection(AresDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Gauge, "Gauge"), "AresGaugeTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.BeastGaugeLabel, "Beast Gauge:"));
            ImGui.TableNextColumn();
            ImGui.ProgressBar(state.BeastGauge / 100f, new Vector2(-1, 0), $"{state.BeastGauge}/100");
        });
    }

    private static void DrawComboSection(AresDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Combo, "Combo"), "AresComboTable", () =>
        {
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

            if (!string.IsNullOrEmpty(state.LastComboAction))
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Last Action:");
                ImGui.TableNextColumn();
                ImGui.TextDisabled(state.LastComboAction);
            }
        });
    }

    private static void DrawBuffSection(AresDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs"), "AresBuffTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SurgingTempest, "Surging Tempest:"));
            ImGui.TableNextColumn();
            if (state.HasSurgingTempest)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), $"{state.SurgingTempestRemaining:F1}s");
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.InnerRelease, "Inner Release:"));
            ImGui.TableNextColumn();
            if (state.HasInnerRelease)
                ImGui.TextColored(new Vector4(1f, 0.7f, 0.3f, 1f), Loc.T(LocalizedStrings.Debug.InnerReleaseStacksLabel, $"{state.InnerReleaseStacks} stacks"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.NascentChaos, "Nascent Chaos:"));
            ImGui.TableNextColumn();
            if (state.HasNascentChaos)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PrimalRendReady, "Primal Rend:"));
            ImGui.TableNextColumn();
            if (state.HasPrimalRendReady)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Ready, "Ready"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "—"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PrimalRuinationReady, "Primal Ruination:"));
            ImGui.TableNextColumn();
            if (state.HasPrimalRuinationReady)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Ready, "Ready"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "—"));
        });
    }

    private static void DrawMitigationSection(AresDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.MitigationHeader, "Mitigation"), "AresMitigationTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ActiveMitigationsLabel, "Active:"));
            ImGui.TableNextColumn();
            if (state.HasActiveMitigation && !string.IsNullOrEmpty(state.ActiveMitigations))
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), state.ActiveMitigations);
            else
                ImGui.TextDisabled("—");
        });
    }

    private static void DrawModuleStates(AresDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.ModuleStatesHeader, "Module States"), "AresModuleTable", () =>
        {
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
        });
    }
}
