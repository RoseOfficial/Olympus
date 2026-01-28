using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.PrometheusCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Machinist tab: Prometheus-specific debug info including Heat, Battery, and Overheat tracking.
/// </summary>
public static class PrometheusTab
{
    public static void Draw(PrometheusDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.MachinistNotActive, "Machinist rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToMachinist, "Switch to Machinist to see debug info."));
            return;
        }

        // Gauge Section
        DrawGaugeSection(state);
        ImGui.Spacing();

        // Overheat Section
        DrawOverheatSection(state);
        ImGui.Spacing();

        // Buffs Section
        DrawBuffSection(state);
        ImGui.Spacing();

        // Cooldowns Section
        DrawCooldownSection(state);
        ImGui.Spacing();

        // Target Section
        DrawTargetSection(state);
    }

    private static void DrawGaugeSection(PrometheusDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Gauge, "Gauge"));
        ImGui.Separator();

        if (ImGui.BeginTable("MchGaugeTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Heat
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Heat, "Heat:"));
            ImGui.TableNextColumn();
            var heatPercent = state.Heat / 100f;
            ImGui.ProgressBar(heatPercent, new Vector2(-1, 0), $"{state.Heat}/100");

            // Battery
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Battery, "Battery:"));
            ImGui.TableNextColumn();
            var batteryPercent = state.Battery / 100f;
            ImGui.ProgressBar(batteryPercent, new Vector2(-1, 0), $"{state.Battery}/100");

            // Combo
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Combo, "Combo:"));
            ImGui.TableNextColumn();
            var comboColor = state.ComboStep > 0 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(comboColor, state.ComboStep > 0 ? $"Step {state.ComboStep}" : Loc.T(LocalizedStrings.Debug.NoneLabel, "None"));

            ImGui.EndTable();
        }
    }

    private static void DrawOverheatSection(PrometheusDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.OverheatQueen, "Overheat & Queen"));
        ImGui.Separator();

        if (ImGui.BeginTable("MchOverheatTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Overheat State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Overheated, "Overheated:"));
            ImGui.TableNextColumn();
            if (state.IsOverheated)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.OverheatRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.No, "No"));
            }

            // Queen State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.QueenActive, "Queen Active:"));
            ImGui.TableNextColumn();
            if (state.IsQueenActive)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), $"{state.QueenRemaining:F1}s ({state.LastQueenBattery} battery)");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.No, "No"));
            }

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(PrometheusDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs"));
        ImGui.Separator();

        if (ImGui.BeginTable("MchBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Reassemble
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Reassemble, "Reassemble:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasReassemble);

            // Hypercharged
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Hypercharged, "Hypercharged:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasHypercharged);

            // Full Metal Machinist
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.FullMetal, "Full Metal:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFullMetalMachinist);

            // Excavator Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ExcavatorReady, "Excavator Ready:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasExcavatorReady);

            ImGui.EndTable();
        }
    }

    private static void DrawCooldownSection(PrometheusDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Charges, "Charges"));
        ImGui.Separator();

        if (ImGui.BeginTable("MchCooldownTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Drill Charges
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Drill, "Drill:"));
            ImGui.TableNextColumn();
            var drillColor = state.DrillCharges >= 2 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.DrillCharges >= 1 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(drillColor, $"{state.DrillCharges}/2");

            // Reassemble Charges
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Reassemble, "Reassemble:"));
            ImGui.TableNextColumn();
            var reassembleColor = state.ReassembleCharges >= 2 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.ReassembleCharges >= 1 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(reassembleColor, $"{state.ReassembleCharges}/2");

            // Gauss Round Charges
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.GaussRound, "Gauss Round:"));
            ImGui.TableNextColumn();
            var gaussColor = state.GaussRoundCharges >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.GaussRoundCharges >= 2 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(gaussColor, $"{state.GaussRoundCharges}/3");

            // Ricochet Charges
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Ricochet, "Ricochet:"));
            ImGui.TableNextColumn();
            var ricochetColor = state.RicochetCharges >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.RicochetCharges >= 2 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(ricochetColor, $"{state.RicochetCharges}/3");

            ImGui.EndTable();
        }
    }

    private static void DrawTargetSection(PrometheusDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.TargetLabel, "Target"));
        ImGui.Separator();

        if (ImGui.BeginTable("MchTargetTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Wildfire
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Wildfire, "Wildfire:"));
            ImGui.TableNextColumn();
            if (state.HasWildfire)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.WildfireRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.NotApplied, "Not applied"));
            }

            // Bioblaster
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Bioblaster, "Bioblaster:"));
            ImGui.TableNextColumn();
            if (state.HasBioblaster)
            {
                var color = state.BioblasterRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.BioblasterRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.NotApplied, "Not applied"));
            }

            // Current Target
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TargetLabel, "Target:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.CurrentTarget);

            // Nearby Enemies
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.NearbyEnemies, "Nearby Enemies:"));
            ImGui.TableNextColumn();
            var aoeColor = state.NearbyEnemies >= 3 ? new Vector4(1f, 0.6f, 0.2f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(aoeColor, $"{state.NearbyEnemies}");

            ImGui.EndTable();
        }
    }

    private static void DrawProcStatus(bool hasProc)
    {
        if (hasProc)
        {
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
        }
        else
        {
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.No, "No"));
        }
    }
}
