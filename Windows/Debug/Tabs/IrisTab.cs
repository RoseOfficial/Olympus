using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Rotation.IrisCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Pictomancer tab: Iris-specific debug info including palette, canvas, and hammer combo tracking.
/// </summary>
public static class IrisTab
{
    public static void Draw(IrisDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Pictomancer rotation not active.");
            ImGui.TextDisabled("Switch to Pictomancer to see debug info.");
            return;
        }

        // Canvas Section
        DrawCanvasSection(state);
        ImGui.Spacing();

        // Palette Section
        DrawPaletteSection(state);
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

    private static void DrawCanvasSection(IrisDebugState state)
    {
        ImGui.Text("Canvas");
        ImGui.Separator();

        if (ImGui.BeginTable("PctCanvasTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Phase
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Phase:");
            ImGui.TableNextColumn();
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 1f, 1f), state.Phase);

            // Creature Motif
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Creature Motif:");
            ImGui.TableNextColumn();
            var creatureColor = state.HasCreatureCanvas ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(creatureColor, state.CreatureMotif);

            // Creature Canvas
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Creature Canvas:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasCreatureCanvas);

            // Weapon Canvas
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Weapon Canvas:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasWeaponCanvas);

            // Landscape Canvas
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Landscape Canvas:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasLandscapeCanvas);

            // Portraits
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Mog Portrait:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.MogReady);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Madeen Portrait:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.MadeenReady);

            ImGui.EndTable();
        }
    }

    private static void DrawPaletteSection(IrisDebugState state)
    {
        ImGui.Text("Palette & Paint");
        ImGui.Separator();

        if (ImGui.BeginTable("PctPaletteTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Palette Gauge
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Palette Gauge:");
            ImGui.TableNextColumn();
            var palettePercent = state.PaletteGauge / 100f;
            ImGui.ProgressBar(palettePercent, new Vector2(-1, 0), $"{state.PaletteGauge}/100");

            // White Paint
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("White Paint:");
            ImGui.TableNextColumn();
            var whiteColor = state.WhitePaint >= 5 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.WhitePaint >= 3 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(whiteColor, $"{state.WhitePaint}/5");

            // Black Paint
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Black Paint:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasBlackPaint);

            // Can Use Subtractive
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Subtractive Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.CanUseSubtractive);

            // Hammer Combo
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Hammer Combo:");
            ImGui.TableNextColumn();
            if (state.IsInHammerCombo)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), $"{state.HammerComboStepName} (Step {state.HammerComboStep})");
            }
            else
            {
                ImGui.TextDisabled("Not in combo");
            }

            // Base Combo
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Base Combo:");
            ImGui.TableNextColumn();
            if (state.IsInSubtractiveCombo)
            {
                ImGui.TextColored(new Vector4(0.8f, 0.5f, 1f, 1f), $"Subtractive (Step {state.BaseComboStep})");
            }
            else if (state.BaseComboStep > 0)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), $"Step {state.BaseComboStep}");
            }
            else
            {
                ImGui.TextDisabled("None");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(IrisDebugState state)
    {
        ImGui.Text("Buffs");
        ImGui.Separator();

        if (ImGui.BeginTable("PctBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Starry Muse
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Starry Muse:");
            ImGui.TableNextColumn();
            if (state.HasStarryMuse)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), $"{state.StarryMuseRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Hyperphantasia
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Hyperphantasia:");
            ImGui.TableNextColumn();
            if (state.HasHyperphantasia)
            {
                ImGui.TextColored(new Vector4(1f, 0.8f, 0.5f, 1f), $"{state.HyperphantasiaStacks} stacks");
            }
            else
            {
                ImGui.TextDisabled("No");
            }

            // Inspiration
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Inspiration:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasInspiration);

            // Subtractive Spectrum
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Subtractive Spectrum:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasSubtractiveSpectrum);

            // Rainbow Bright
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Rainbow Bright:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasRainbowBright);

            // Starstruck
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Starstruck:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasStarstruck);

            // Hammer Time
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Hammer Time:");
            ImGui.TableNextColumn();
            if (state.HasHammerTime)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), $"{state.HammerTimeStacks} stacks");
            }
            else
            {
                ImGui.TextDisabled("No");
            }

            // Swiftcast
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Swiftcast:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasSwiftcast);

            ImGui.EndTable();
        }
    }

    private static void DrawCooldownSection(IrisDebugState state)
    {
        ImGui.Text("Cooldowns");
        ImGui.Separator();

        if (ImGui.BeginTable("PctCooldownTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // MP
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("MP:");
            ImGui.TableNextColumn();
            var mpPercent = state.MaxMp > 0 ? (float)state.CurrentMp / state.MaxMp : 0;
            ImGui.ProgressBar(mpPercent, new Vector2(-1, 0), $"{state.CurrentMp:N0}/{state.MaxMp:N0}");

            // Starry Muse
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Starry Muse CD:");
            ImGui.TableNextColumn();
            DrawReadyStatus(state.StarryMuseReady);

            // Living Muse
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Living Muse:");
            ImGui.TableNextColumn();
            var livingColor = state.LivingMuseCharges >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.LivingMuseCharges >= 2 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(livingColor, $"{state.LivingMuseCharges}/3");

            // Striking Muse
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Striking Muse:");
            ImGui.TableNextColumn();
            DrawReadyStatus(state.StrikingMuseReady);

            // Subtractive Palette
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Subtractive Palette:");
            ImGui.TableNextColumn();
            DrawReadyStatus(state.SubtractivePaletteReady);

            // Tempera Coat
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Tempera Coat:");
            ImGui.TableNextColumn();
            DrawReadyStatus(state.TemperaCoatReady);

            // Smudge
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Smudge:");
            ImGui.TableNextColumn();
            DrawReadyStatus(state.SmudgeReady);

            ImGui.EndTable();
        }
    }

    private static void DrawTargetSection(IrisDebugState state)
    {
        ImGui.Text("Target");
        ImGui.Separator();

        if (ImGui.BeginTable("PctTargetTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

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
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Ready");
        }
        else
        {
            ImGui.TextDisabled("No");
        }
    }

    private static void DrawReadyStatus(bool isReady)
    {
        if (isReady)
        {
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Ready");
        }
        else
        {
            ImGui.TextDisabled("On CD");
        }
    }
}
