using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Rotation.ThanatosCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Reaper tab: Thanatos-specific debug info including Soul/Shroud gauge, Enshroud, and procs.
/// </summary>
public static class ThanatosTab
{
    public static void Draw(ThanatosDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Reaper rotation not active.");
            ImGui.TextDisabled("Switch to Reaper to see debug info.");
            return;
        }

        // Gauge Section
        DrawGaugeSection(state);
        ImGui.Spacing();

        // Enshroud Section
        DrawEnshroudSection(state);
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

    private static void DrawGaugeSection(ThanatosDebugState state)
    {
        ImGui.Text("Gauge");
        ImGui.Separator();

        if (ImGui.BeginTable("RprGaugeTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Soul
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Soul:");
            ImGui.TableNextColumn();
            var soulPercent = state.Soul / 100f;
            ImGui.ProgressBar(soulPercent, new Vector2(-1, 0), $"{state.Soul}/100");

            // Shroud
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Shroud:");
            ImGui.TableNextColumn();
            var shroudPercent = state.Shroud / 100f;
            ImGui.ProgressBar(shroudPercent, new Vector2(-1, 0), $"{state.Shroud}/100");

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

    private static void DrawEnshroudSection(ThanatosDebugState state)
    {
        ImGui.Text("Enshroud");
        ImGui.Separator();

        if (ImGui.BeginTable("RprEnshroudTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Enshroud State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Enshroud:");
            ImGui.TableNextColumn();
            var enshroudColor = state.IsEnshrouded ? new Vector4(0.8f, 0.5f, 1f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(enshroudColor, state.GetEnshroudState());

            if (state.IsEnshrouded)
            {
                // Lemure Shroud
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Lemure Shroud:");
                ImGui.TableNextColumn();
                var lemureColor = state.LemureShroud > 0 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
                ImGui.TextColored(lemureColor, $"{state.LemureShroud}");

                // Void Shroud
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Void Shroud:");
                ImGui.TableNextColumn();
                var voidColor = state.VoidShroud >= 2 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
                ImGui.TextColored(voidColor, $"{state.VoidShroud}");
            }

            // Soul Reaver
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Soul Reaver:");
            ImGui.TableNextColumn();
            if (state.HasSoulReaver)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), $"{state.SoulReaverStacks} stacks");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(ThanatosDebugState state)
    {
        ImGui.Text("Buffs");
        ImGui.Separator();

        if (ImGui.BeginTable("RprBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Death's Design
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Death's Design:");
            ImGui.TableNextColumn();
            if (state.HasDeathsDesign)
            {
                var color = state.DeathsDesignRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.DeathsDesignRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Not applied");
            }

            // Arcane Circle
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Arcane Circle:");
            ImGui.TableNextColumn();
            if (state.HasArcaneCircle)
            {
                ImGui.TextColored(new Vector4(0.8f, 0.5f, 1f, 1f), "Active");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Immortal Sacrifice
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Immortal Sacrifice:");
            ImGui.TableNextColumn();
            var sacrificeColor = state.ImmortalSacrificeStacks > 0 ? new Vector4(1f, 0.8f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(sacrificeColor, $"{state.ImmortalSacrificeStacks} stacks");

            // Bloodsown Circle
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Bloodsown Circle:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasBloodsownCircle);

            ImGui.EndTable();
        }
    }

    private static void DrawProcSection(ThanatosDebugState state)
    {
        ImGui.Text("Procs");
        ImGui.Separator();

        if (ImGui.BeginTable("RprProcTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Enhanced Gibbet
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Enhanced Gibbet:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasEnhancedGibbet);

            // Enhanced Gallows
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Enhanced Gallows:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasEnhancedGallows);

            // Enhanced Void Reaping
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Enhanced Void:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasEnhancedVoidReaping);

            // Enhanced Cross Reaping
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Enhanced Cross:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasEnhancedCrossReaping);

            // Perfectio Parata
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Perfectio Parata:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasPerfectioParata);

            // Oblatio
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Oblatio:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasOblatio);

            // Soulsow
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Soulsow:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasSoulsow);

            ImGui.EndTable();
        }
    }

    private static void DrawPositionalSection(ThanatosDebugState state)
    {
        ImGui.Text("Positional");
        ImGui.Separator();

        if (ImGui.BeginTable("RprPositionalTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
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
