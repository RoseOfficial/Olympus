using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Scholar tab: Athena-specific debug info including Aetherflow, Fairy, Shields.
/// </summary>
public static class ScholarTab
{
    public static void Draw(AthenaDebugState? athenaState, Configuration config)
    {
        if (athenaState == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Scholar rotation not active.");
            ImGui.TextDisabled("Switch to Scholar to see debug info.");
            return;
        }

        // Resources Section
        DrawResourcesSection(athenaState);
        ImGui.Spacing();

        // Fairy Section
        DrawFairySection(athenaState);
        ImGui.Spacing();

        // Healing Section
        DrawHealingSection(athenaState);
        ImGui.Spacing();

        // DPS Section
        DrawDpsSection(athenaState);
    }

    private static void DrawResourcesSection(AthenaDebugState state)
    {
        ImGui.Text("Resources");
        ImGui.Separator();

        if (ImGui.BeginTable("SchResourcesTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Aetherflow Stacks
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Aetherflow:");
            ImGui.TableNextColumn();
            var stackColor = state.AetherflowStacks switch
            {
                0 => new Vector4(1f, 0.5f, 0.5f, 1f),  // Red - empty
                1 => new Vector4(1f, 1f, 0.5f, 1f),    // Yellow - low
                2 => new Vector4(0.7f, 1f, 0.7f, 1f),  // Light green
                3 => new Vector4(0.5f, 1f, 0.5f, 1f),  // Green - full
                _ => new Vector4(1f, 1f, 1f, 1f)
            };
            ImGui.TextColored(stackColor, $"{state.AetherflowStacks}/3");
            ImGui.SameLine();
            ImGui.TextDisabled($"({state.AetherflowState})");

            // Fairy Gauge
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Fairy Gauge:");
            ImGui.TableNextColumn();
            var gaugePercent = state.FairyGauge / 100f;
            ImGui.ProgressBar(gaugePercent, new Vector2(-1, 0), $"{state.FairyGauge}/100");

            // Player HP
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Player HP:");
            ImGui.TableNextColumn();
            var hpColor = state.PlayerHpPercent < 0.5f ? new Vector4(1f, 0.5f, 0.5f, 1f)
                : state.PlayerHpPercent < 0.8f ? new Vector4(1f, 1f, 0.5f, 1f)
                : new Vector4(0.5f, 1f, 0.5f, 1f);
            ImGui.TextColored(hpColor, $"{state.PlayerHpPercent:P0}");

            // Party Info
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Party:");
            ImGui.TableNextColumn();
            ImGui.Text($"{state.PartyValidCount}/{state.PartyListCount} valid");

            ImGui.EndTable();
        }
    }

    private static void DrawFairySection(AthenaDebugState state)
    {
        ImGui.Text("Fairy");
        ImGui.Separator();

        if (ImGui.BeginTable("SchFairyTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Fairy State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Fairy State:");
            ImGui.TableNextColumn();
            var fairyColor = state.FairyState switch
            {
                "Eos" => new Vector4(0.5f, 1f, 0.5f, 1f),        // Green - active
                "Seraph" => new Vector4(1f, 0.8f, 0.5f, 1f),     // Orange - transformed
                "Seraphism" => new Vector4(1f, 0.6f, 0.8f, 1f),  // Pink - lv100 mode
                "Dissipated" => new Vector4(0.7f, 0.7f, 0.7f, 1f), // Gray - dismissed
                _ => new Vector4(1f, 0.5f, 0.5f, 1f)             // Red - none
            };
            ImGui.TextColored(fairyColor, state.FairyState);

            // Fey Union State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Fey Union:");
            ImGui.TableNextColumn();
            ImGui.Text(state.FeyUnionState);

            // Seraph State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Seraph:");
            ImGui.TableNextColumn();
            ImGui.Text(state.SeraphState);

            // Dissipation State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Dissipation:");
            ImGui.TableNextColumn();
            ImGui.Text(state.DissipationState);

            ImGui.EndTable();
        }
    }

    private static void DrawHealingSection(AthenaDebugState state)
    {
        ImGui.Text("Healing");
        ImGui.Separator();

        if (ImGui.BeginTable("SchHealingTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Single Target Healing
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Single Heal:");
            ImGui.TableNextColumn();
            ImGui.Text(state.SingleHealState);

            // AoE Healing
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("AoE Heal:");
            ImGui.TableNextColumn();
            ImGui.Text($"{state.AoEHealState} ({state.AoEInjuredCount} injured)");

            // Lustrate
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Lustrate:");
            ImGui.TableNextColumn();
            ImGui.Text(state.LustrateState);

            // Indomitability
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Indomitability:");
            ImGui.TableNextColumn();
            ImGui.Text(state.IndomitabilityState);

            // Excogitation
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Excogitation:");
            ImGui.TableNextColumn();
            ImGui.Text(state.ExcogitationState);

            // Sacred Soil
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Sacred Soil:");
            ImGui.TableNextColumn();
            ImGui.Text(state.SacredSoilState);

            // Shields
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Shields:");
            ImGui.TableNextColumn();
            ImGui.Text(state.ShieldState);

            // Emergency Tactics
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Emergency Tactics:");
            ImGui.TableNextColumn();
            ImGui.Text(state.EmergencyTacticsState);

            // Deployment
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Deployment:");
            ImGui.TableNextColumn();
            ImGui.Text(state.DeploymentState);

            // Recitation
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Recitation:");
            ImGui.TableNextColumn();
            ImGui.Text(state.RecitationState);

            // Last Heal
            if (state.LastHealAmount > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Last Heal:");
                ImGui.TableNextColumn();
                ImGui.Text($"{state.LastHealAmount:N0} HP");
                if (!string.IsNullOrEmpty(state.LastHealStats))
                {
                    ImGui.TextDisabled(state.LastHealStats);
                }
            }

            ImGui.EndTable();
        }
    }

    private static void DrawDpsSection(AthenaDebugState state)
    {
        ImGui.Text("DPS");
        ImGui.Separator();

        if (ImGui.BeginTable("SchDpsTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Planned Action
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Planned Action:");
            ImGui.TableNextColumn();
            var actionColor = state.PlannedAction != "None" ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(actionColor, state.PlannedAction);

            // DPS State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("DPS State:");
            ImGui.TableNextColumn();
            ImGui.Text(state.DpsState);

            // DoT State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("DoT:");
            ImGui.TableNextColumn();
            ImGui.Text(state.DotState);

            // AoE DPS
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("AoE DPS:");
            ImGui.TableNextColumn();
            ImGui.Text($"{state.AoEDpsState} ({state.AoEDpsEnemyCount} enemies)");

            // Chain Stratagem
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Chain Stratagem:");
            ImGui.TableNextColumn();
            ImGui.Text(state.ChainStratagemState);

            // Energy Drain
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Energy Drain:");
            ImGui.TableNextColumn();
            ImGui.Text(state.EnergyDrainState);

            // Lucid Dreaming
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Lucid Dreaming:");
            ImGui.TableNextColumn();
            ImGui.Text(state.LucidState);

            ImGui.EndTable();
        }

        // Raise/Esuna at bottom
        ImGui.Spacing();
        if (state.RaiseState != "Idle" || state.RaiseTarget != "None")
        {
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.5f, 1f), $"Raise: {state.RaiseState}");
            if (!string.IsNullOrEmpty(state.RaiseTarget) && state.RaiseTarget != "None")
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"→ {state.RaiseTarget}");
            }
        }

        if (state.EsunaState != "Idle" || state.EsunaTarget != "None")
        {
            ImGui.TextColored(new Vector4(0.5f, 1f, 1f, 1f), $"Esuna: {state.EsunaState}");
            if (!string.IsNullOrEmpty(state.EsunaTarget) && state.EsunaTarget != "None")
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"→ {state.EsunaTarget}");
            }
        }
    }
}
