using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Rotation.KratosCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Monk tab: Kratos-specific debug info including forms, chakra, and Nadi tracking.
/// </summary>
public static class KratosTab
{
    public static void Draw(KratosDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Monk rotation not active.");
            ImGui.TextDisabled("Switch to Monk to see debug info.");
            return;
        }

        // Form Section
        DrawFormSection(state);
        ImGui.Spacing();

        // Chakra Section
        DrawChakraSection(state);
        ImGui.Spacing();

        // Buffs Section
        DrawBuffSection(state);
        ImGui.Spacing();

        // Procs Section
        DrawProcSection(state);
        ImGui.Spacing();

        // Positional Section
        DrawPositionalSection(state);
    }

    private static void DrawFormSection(KratosDebugState state)
    {
        ImGui.Text("Form");
        ImGui.Separator();

        if (ImGui.BeginTable("MnkFormTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Current Form
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Current Form:");
            ImGui.TableNextColumn();
            var formColor = state.CurrentForm switch
            {
                MonkForm.OpoOpo => new Vector4(1f, 0.8f, 0.5f, 1f),
                MonkForm.Raptor => new Vector4(0.5f, 1f, 0.5f, 1f),
                MonkForm.Coeurl => new Vector4(0.5f, 0.8f, 1f, 1f),
                _ => new Vector4(0.7f, 0.7f, 0.7f, 1f)
            };
            ImGui.TextColored(formColor, state.CurrentForm.ToString());

            // Perfect Balance
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Perfect Balance:");
            ImGui.TableNextColumn();
            if (state.HasPerfectBalance)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), $"{state.PerfectBalanceStacks} stacks");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Formless Fist
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Formless Fist:");
            ImGui.TableNextColumn();
            if (state.HasFormlessFist)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Active");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawChakraSection(KratosDebugState state)
    {
        ImGui.Text("Chakra");
        ImGui.Separator();

        if (ImGui.BeginTable("MnkChakraTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Chakra
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Chakra:");
            ImGui.TableNextColumn();
            var chakraColor = state.Chakra >= 5 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(chakraColor, $"{state.Chakra}/5");

            // Beast Chakra
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Beast Chakra:");
            ImGui.TableNextColumn();
            var beastColor = state.BeastChakraCount >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(beastColor, state.BeastChakraState);
            ImGui.SameLine();
            ImGui.TextDisabled($"({state.BeastChakraCount}/3)");

            // Lunar Nadi
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Lunar Nadi:");
            ImGui.TableNextColumn();
            if (state.HasLunarNadi)
            {
                ImGui.TextColored(new Vector4(0.6f, 0.6f, 1f, 1f), "Active");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Solar Nadi
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Solar Nadi:");
            ImGui.TableNextColumn();
            if (state.HasSolarNadi)
            {
                ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), "Active");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(KratosDebugState state)
    {
        ImGui.Text("Buffs");
        ImGui.Separator();

        if (ImGui.BeginTable("MnkBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Disciplined Fist
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Disciplined Fist:");
            ImGui.TableNextColumn();
            if (state.HasDisciplinedFist)
            {
                var color = state.DisciplinedFistRemaining < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.DisciplinedFistRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Inactive");
            }

            // Leaden Fist
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Leaden Fist:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasLeadenFist);

            // Riddle of Fire
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Riddle of Fire:");
            ImGui.TableNextColumn();
            if (state.HasRiddleOfFire)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.RiddleOfFireRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Brotherhood
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Brotherhood:");
            ImGui.TableNextColumn();
            if (state.HasBrotherhood)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "Active");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Riddle of Wind
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Riddle of Wind:");
            ImGui.TableNextColumn();
            if (state.HasRiddleOfWind)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.8f, 1f), "Active");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawProcSection(KratosDebugState state)
    {
        ImGui.Text("Procs");
        ImGui.Separator();

        if (ImGui.BeginTable("MnkProcTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Raptor's Fury
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Raptor's Fury:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasRaptorsFury);

            // Coeurl's Fury
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Coeurl's Fury:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasCoeurlsFury);

            // Opo-opo's Fury
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Opo-opo's Fury:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasOpooposFury);

            // Fire's Rumination
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Fire's Rumination:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFiresRumination);

            // Wind's Rumination
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Wind's Rumination:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasWindsRumination);

            // DoT
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Demolish:");
            ImGui.TableNextColumn();
            if (state.HasDemolishOnTarget)
            {
                var color = state.DemolishRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.DemolishRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Not applied");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawPositionalSection(KratosDebugState state)
    {
        ImGui.Text("Positional");
        ImGui.Separator();

        if (ImGui.BeginTable("MnkPositionalTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Current Position
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Position:");
            ImGui.TableNextColumn();
            if (state.TargetHasPositionalImmunity)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), "Immune (omni)");
            }
            else if (state.IsAtRear)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Rear");
            }
            else if (state.IsAtFlank)
            {
                ImGui.TextColored(new Vector4(1f, 1f, 0.5f, 1f), "Flank");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Front");
            }

            // True North
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("True North:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasTrueNorth);

            // Target
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Target:");
            ImGui.TableNextColumn();
            ImGui.Text(string.IsNullOrEmpty(state.CurrentTarget) ? "None" : state.CurrentTarget);

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
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Ready");
        }
        else
        {
            ImGui.TextDisabled("No");
        }
    }
}
