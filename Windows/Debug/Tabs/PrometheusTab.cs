using System.Numerics;
using Dalamud.Bindings.ImGui;
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
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Machinist rotation not active.");
            ImGui.TextDisabled("Switch to Machinist to see debug info.");
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
        ImGui.Text("Gauge");
        ImGui.Separator();

        if (ImGui.BeginTable("MchGaugeTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Heat
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Heat:");
            ImGui.TableNextColumn();
            var heatPercent = state.Heat / 100f;
            ImGui.ProgressBar(heatPercent, new Vector2(-1, 0), $"{state.Heat}/100");

            // Battery
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Battery:");
            ImGui.TableNextColumn();
            var batteryPercent = state.Battery / 100f;
            ImGui.ProgressBar(batteryPercent, new Vector2(-1, 0), $"{state.Battery}/100");

            // Combo
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Combo:");
            ImGui.TableNextColumn();
            var comboColor = state.ComboStep > 0 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(comboColor, state.ComboStep > 0 ? $"Step {state.ComboStep}" : "None");

            ImGui.EndTable();
        }
    }

    private static void DrawOverheatSection(PrometheusDebugState state)
    {
        ImGui.Text("Overheat & Queen");
        ImGui.Separator();

        if (ImGui.BeginTable("MchOverheatTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Overheat State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Overheated:");
            ImGui.TableNextColumn();
            if (state.IsOverheated)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.OverheatRemaining:F1}s remaining");
            }
            else
            {
                ImGui.TextDisabled("No");
            }

            // Queen State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Queen Active:");
            ImGui.TableNextColumn();
            if (state.IsQueenActive)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), $"{state.QueenRemaining:F1}s ({state.LastQueenBattery} battery)");
            }
            else
            {
                ImGui.TextDisabled("No");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(PrometheusDebugState state)
    {
        ImGui.Text("Buffs");
        ImGui.Separator();

        if (ImGui.BeginTable("MchBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Reassemble
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Reassemble:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasReassemble);

            // Hypercharged
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Hypercharged:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasHypercharged);

            // Full Metal Machinist
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Full Metal:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFullMetalMachinist);

            // Excavator Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Excavator Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasExcavatorReady);

            ImGui.EndTable();
        }
    }

    private static void DrawCooldownSection(PrometheusDebugState state)
    {
        ImGui.Text("Charges");
        ImGui.Separator();

        if (ImGui.BeginTable("MchCooldownTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Drill Charges
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Drill:");
            ImGui.TableNextColumn();
            var drillColor = state.DrillCharges >= 2 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.DrillCharges >= 1 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(drillColor, $"{state.DrillCharges}/2");

            // Reassemble Charges
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Reassemble:");
            ImGui.TableNextColumn();
            var reassembleColor = state.ReassembleCharges >= 2 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.ReassembleCharges >= 1 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(reassembleColor, $"{state.ReassembleCharges}/2");

            // Gauss Round Charges
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Gauss Round:");
            ImGui.TableNextColumn();
            var gaussColor = state.GaussRoundCharges >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.GaussRoundCharges >= 2 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(gaussColor, $"{state.GaussRoundCharges}/3");

            // Ricochet Charges
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Ricochet:");
            ImGui.TableNextColumn();
            var ricochetColor = state.RicochetCharges >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.RicochetCharges >= 2 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(ricochetColor, $"{state.RicochetCharges}/3");

            ImGui.EndTable();
        }
    }

    private static void DrawTargetSection(PrometheusDebugState state)
    {
        ImGui.Text("Target");
        ImGui.Separator();

        if (ImGui.BeginTable("MchTargetTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Wildfire
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Wildfire:");
            ImGui.TableNextColumn();
            if (state.HasWildfire)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.WildfireRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Not applied");
            }

            // Bioblaster
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Bioblaster:");
            ImGui.TableNextColumn();
            if (state.HasBioblaster)
            {
                var color = state.BioblasterRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.BioblasterRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Not applied");
            }

            // Current Target
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Target:");
            ImGui.TableNextColumn();
            ImGui.Text(state.CurrentTarget);

            // Nearby Enemies
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Nearby Enemies:");
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
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Active");
        }
        else
        {
            ImGui.TextDisabled("No");
        }
    }
}
