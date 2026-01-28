using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
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
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.MonkNotActive, "Monk rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToMonk, "Switch to Monk to see debug info."));
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
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Form, "Form"));
        ImGui.Separator();

        if (ImGui.BeginTable("MnkFormTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Current Form
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.CurrentForm, "Current Form:"));
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
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PerfectBalance, "Perfect Balance:"));
            ImGui.TableNextColumn();
            if (state.HasPerfectBalance)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), Loc.TFormat(LocalizedStrings.Debug.StacksFormat, "{0} stacks", state.PerfectBalanceStacks));
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            // Formless Fist
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.FormlessFist, "Formless Fist:"));
            ImGui.TableNextColumn();
            if (state.HasFormlessFist)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            ImGui.EndTable();
        }
    }

    private static void DrawChakraSection(KratosDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.ChakraLabel, "Chakra"));
        ImGui.Separator();

        if (ImGui.BeginTable("MnkChakraTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Chakra
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ChakraLabel, "Chakra:"));
            ImGui.TableNextColumn();
            var chakraColor = state.Chakra >= 5 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(chakraColor, $"{state.Chakra}/5");

            // Beast Chakra
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.BeastChakra, "Beast Chakra:"));
            ImGui.TableNextColumn();
            var beastColor = state.BeastChakraCount >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(beastColor, state.BeastChakraState);
            ImGui.SameLine();
            ImGui.TextDisabled($"({state.BeastChakraCount}/3)");

            // Lunar Nadi
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.LunarNadi, "Lunar Nadi:"));
            ImGui.TableNextColumn();
            if (state.HasLunarNadi)
            {
                ImGui.TextColored(new Vector4(0.6f, 0.6f, 1f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            // Solar Nadi
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SolarNadi, "Solar Nadi:"));
            ImGui.TableNextColumn();
            if (state.HasSolarNadi)
            {
                ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(KratosDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs"));
        ImGui.Separator();

        if (ImGui.BeginTable("MnkBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Disciplined Fist
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DisciplinedFist, "Disciplined Fist:"));
            ImGui.TableNextColumn();
            if (state.HasDisciplinedFist)
            {
                var color = state.DisciplinedFistRemaining < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.DisciplinedFistRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            // Leaden Fist
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.LeadenFist, "Leaden Fist:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasLeadenFist);

            // Riddle of Fire
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.RiddleOfFire, "Riddle of Fire:"));
            ImGui.TableNextColumn();
            if (state.HasRiddleOfFire)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.RiddleOfFireRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            // Brotherhood
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Brotherhood, "Brotherhood:"));
            ImGui.TableNextColumn();
            if (state.HasBrotherhood)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            // Riddle of Wind
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.RiddleOfWind, "Riddle of Wind:"));
            ImGui.TableNextColumn();
            if (state.HasRiddleOfWind)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.8f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            ImGui.EndTable();
        }
    }

    private static void DrawProcSection(KratosDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Procs, "Procs"));
        ImGui.Separator();

        if (ImGui.BeginTable("MnkProcTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Raptor's Fury
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.RaptorsFury, "Raptor's Fury:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasRaptorsFury);

            // Coeurl's Fury
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.CoeurlsFury, "Coeurl's Fury:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasCoeurlsFury);

            // Opo-opo's Fury
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.OpooposFury, "Opo-opo's Fury:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasOpooposFury);

            // Fire's Rumination
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.FiresRumination, "Fire's Rumination:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFiresRumination);

            // Wind's Rumination
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.WindsRumination, "Wind's Rumination:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasWindsRumination);

            // DoT
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Demolish, "Demolish:"));
            ImGui.TableNextColumn();
            if (state.HasDemolishOnTarget)
            {
                var color = state.DemolishRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.DemolishRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.NotApplied, "Not applied"));
            }

            ImGui.EndTable();
        }
    }

    private static void DrawPositionalSection(KratosDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Positional, "Positional"));
        ImGui.Separator();

        if (ImGui.BeginTable("MnkPositionalTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Current Position
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Position, "Position:"));
            ImGui.TableNextColumn();
            if (state.TargetHasPositionalImmunity)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), Loc.T(LocalizedStrings.Debug.ImmuneOmni, "Immune (omni)"));
            }
            else if (state.IsAtRear)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Rear, "Rear"));
            }
            else if (state.IsAtFlank)
            {
                ImGui.TextColored(new Vector4(1f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Flank, "Flank"));
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Front, "Front"));
            }

            // True North
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TrueNorth, "True North:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasTrueNorth);

            // Target
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TargetLabel, "Target:"));
            ImGui.TableNextColumn();
            ImGui.Text(string.IsNullOrEmpty(state.CurrentTarget) ? Loc.T(LocalizedStrings.Debug.NoneLabel, "None") : state.CurrentTarget);

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
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Ready, "Ready"));
        }
        else
        {
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.No, "No"));
        }
    }
}
