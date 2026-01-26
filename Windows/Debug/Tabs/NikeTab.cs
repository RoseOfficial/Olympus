using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Rotation.NikeCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Samurai tab: Nike-specific debug info including Sen, Kenki, and Iaijutsu tracking.
/// </summary>
public static class NikeTab
{
    public static void Draw(NikeDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Samurai rotation not active.");
            ImGui.TextDisabled("Switch to Samurai to see debug info.");
            return;
        }

        // Gauge Section
        DrawGaugeSection(state);
        ImGui.Spacing();

        // Buffs Section
        DrawBuffSection(state);
        ImGui.Spacing();

        // Procs Section
        DrawProcSection(state);
        ImGui.Spacing();

        // DoT Section
        DrawDotSection(state);
        ImGui.Spacing();

        // Positional Section
        DrawPositionalSection(state);
    }

    private static void DrawGaugeSection(NikeDebugState state)
    {
        ImGui.Text("Gauge");
        ImGui.Separator();

        if (ImGui.BeginTable("SamGaugeTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Sen
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Sen:");
            ImGui.TableNextColumn();
            var senColor = state.SenCount switch
            {
                3 => new Vector4(0.5f, 1f, 0.5f, 1f),
                2 => new Vector4(1f, 1f, 0.5f, 1f),
                1 => new Vector4(1f, 0.8f, 0.5f, 1f),
                _ => new Vector4(0.7f, 0.7f, 0.7f, 1f)
            };
            ImGui.TextColored(senColor, NikeDebugState.FormatSen(state.Sen));
            ImGui.SameLine();
            ImGui.TextDisabled($"({state.SenCount}/3)");

            // Kenki
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Kenki:");
            ImGui.TableNextColumn();
            var kenkiPercent = state.Kenki / 100f;
            ImGui.ProgressBar(kenkiPercent, new Vector2(-1, 0), $"{state.Kenki}/100");

            // Meditation
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Meditation:");
            ImGui.TableNextColumn();
            var medColor = state.Meditation >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(medColor, $"{state.Meditation}/3");

            // Combo
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Combo:");
            ImGui.TableNextColumn();
            var comboColor = state.ComboStep > 0 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(comboColor, state.ComboStep > 0 ? $"Step {state.ComboStep} ({state.ComboTimeRemaining:F1}s)" : "None");

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(NikeDebugState state)
    {
        ImGui.Text("Buffs");
        ImGui.Separator();

        if (ImGui.BeginTable("SamBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Fugetsu (damage up)
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Fugetsu:");
            ImGui.TableNextColumn();
            if (state.HasFugetsu)
            {
                var color = state.FugetsuRemaining < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.FugetsuRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Inactive");
            }

            // Fuka (speed up)
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Fuka:");
            ImGui.TableNextColumn();
            if (state.HasFuka)
            {
                var color = state.FukaRemaining < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.FukaRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Inactive");
            }

            // Meikyo Shisui
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Meikyo Shisui:");
            ImGui.TableNextColumn();
            if (state.HasMeikyoShisui)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), $"{state.MeikyoStacks} stacks");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawProcSection(NikeDebugState state)
    {
        ImGui.Text("Procs");
        ImGui.Separator();

        if (ImGui.BeginTable("SamProcTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Last Iaijutsu
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Last Iaijutsu:");
            ImGui.TableNextColumn();
            ImGui.Text(state.LastIaijutsu.ToString());

            // Tsubame-gaeshi Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Tsubame Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasTsubameGaeshiReady);

            // Ogi Namikiri Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Ogi Namikiri Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasOgiNamikiriReady);

            // Kaeshi Namikiri Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Kaeshi Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasKaeshiNamikiriReady);

            // Zanshin Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Zanshin Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasZanshinReady);

            ImGui.EndTable();
        }
    }

    private static void DrawDotSection(NikeDebugState state)
    {
        ImGui.Text("DoT");
        ImGui.Separator();

        if (ImGui.BeginTable("SamDotTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Higanbana
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Higanbana:");
            ImGui.TableNextColumn();
            if (state.HasHiganbanaOnTarget)
            {
                var color = state.HiganbanaRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.HiganbanaRemaining:F1}s remaining");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Not applied");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawPositionalSection(NikeDebugState state)
    {
        ImGui.Text("Positional");
        ImGui.Separator();

        if (ImGui.BeginTable("SamPositionalTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
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
