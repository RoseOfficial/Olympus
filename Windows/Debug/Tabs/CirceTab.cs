using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Rotation.CirceCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Red Mage tab: Circe-specific debug info including mana balance, Dualcast, and melee combo tracking.
/// </summary>
public static class CirceTab
{
    public static void Draw(CirceDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Red Mage rotation not active.");
            ImGui.TextDisabled("Switch to Red Mage to see debug info.");
            return;
        }

        // Mana Section
        DrawManaSection(state);
        ImGui.Spacing();

        // Melee Combo Section
        DrawMeleeComboSection(state);
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

    private static void DrawManaSection(CirceDebugState state)
    {
        ImGui.Text("Mana");
        ImGui.Separator();

        if (ImGui.BeginTable("RdmManaTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Phase
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Phase:");
            ImGui.TableNextColumn();
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 1f, 1f), state.Phase);

            // Black Mana
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Black Mana:");
            ImGui.TableNextColumn();
            var blackPercent = state.BlackMana / 100f;
            ImGui.ProgressBar(blackPercent, new Vector2(-1, 0), $"{state.BlackMana}/100");

            // White Mana
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("White Mana:");
            ImGui.TableNextColumn();
            var whitePercent = state.WhiteMana / 100f;
            ImGui.ProgressBar(whitePercent, new Vector2(-1, 0), $"{state.WhiteMana}/100");

            // Mana Imbalance
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Imbalance:");
            ImGui.TableNextColumn();
            var imbalanceColor = System.Math.Abs(state.ManaImbalance) >= 30 ? new Vector4(1f, 0.5f, 0.5f, 1f)
                : System.Math.Abs(state.ManaImbalance) >= 20 ? new Vector4(1f, 1f, 0.5f, 1f)
                : new Vector4(0.5f, 1f, 0.5f, 1f);
            var imbalanceText = state.ManaImbalance > 0 ? $"+{state.ManaImbalance} Black" : state.ManaImbalance < 0 ? $"+{-state.ManaImbalance} White" : "Balanced";
            ImGui.TextColored(imbalanceColor, imbalanceText);

            // Mana Stacks
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Mana Stacks:");
            ImGui.TableNextColumn();
            var stackColor = state.ManaStacks >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(stackColor, $"{state.ManaStacks}/3");

            // Can Start Melee
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Melee Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.CanStartMeleeCombo);

            ImGui.EndTable();
        }
    }

    private static void DrawMeleeComboSection(CirceDebugState state)
    {
        ImGui.Text("Melee Combo");
        ImGui.Separator();

        if (ImGui.BeginTable("RdmComboTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // In Melee Combo
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("In Combo:");
            ImGui.TableNextColumn();
            if (state.IsInMeleeCombo)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.8f, 1f), $"Yes - {state.MeleeComboStep}");
            }
            else
            {
                ImGui.TextDisabled("No");
            }

            // Finisher Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Finisher Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.IsFinisherReady);

            // Scorch Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Scorch Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.IsScorchReady);

            // Resolution Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Resolution Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.IsResolutionReady);

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(CirceDebugState state)
    {
        ImGui.Text("Buffs");
        ImGui.Separator();

        if (ImGui.BeginTable("RdmBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Dualcast
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Dualcast:");
            ImGui.TableNextColumn();
            if (state.HasDualcast)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), $"{state.DualcastRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("No");
            }

            // Verfire
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Verfire Ready:");
            ImGui.TableNextColumn();
            if (state.HasVerfire)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.VerfireRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("No");
            }

            // Verstone
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Verstone Ready:");
            ImGui.TableNextColumn();
            if (state.HasVerstone)
            {
                ImGui.TextColored(new Vector4(0.9f, 0.9f, 0.7f, 1f), $"{state.VerstoneRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("No");
            }

            // Embolden
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Embolden:");
            ImGui.TableNextColumn();
            if (state.HasEmbolden)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), $"{state.EmboldenRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Manafication
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Manafication:");
            ImGui.TableNextColumn();
            if (state.HasManafication)
            {
                ImGui.TextColored(new Vector4(0.8f, 0.5f, 1f, 1f), $"{state.ManaficationRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Acceleration
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Acceleration:");
            ImGui.TableNextColumn();
            if (state.HasAcceleration)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.8f, 1f), $"{state.AccelerationRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("No");
            }

            // Special Procs
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Grand Impact:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasGrandImpactReady);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Prefulgence:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasPrefulgenceReady);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Thorned Flourish:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasThornedFlourish);

            ImGui.EndTable();
        }
    }

    private static void DrawCooldownSection(CirceDebugState state)
    {
        ImGui.Text("Cooldowns");
        ImGui.Separator();

        if (ImGui.BeginTable("RdmCooldownTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
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

            // Fleche
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Fleche:");
            ImGui.TableNextColumn();
            DrawReadyStatus(state.FlecheReady);

            // Contre Sixte
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Contre Sixte:");
            ImGui.TableNextColumn();
            DrawReadyStatus(state.ContreSixteReady);

            // Corps-a-corps
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Corps-a-corps:");
            ImGui.TableNextColumn();
            var corpsColor = state.CorpsACorpsCharges >= 2 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.CorpsACorpsCharges >= 1 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(corpsColor, $"{state.CorpsACorpsCharges}/2");

            // Engagement
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Engagement:");
            ImGui.TableNextColumn();
            var engageColor = state.EngagementCharges >= 2 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.EngagementCharges >= 1 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(engageColor, $"{state.EngagementCharges}/2");

            // Acceleration
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Acceleration:");
            ImGui.TableNextColumn();
            var accelColor = state.AccelerationCharges >= 2 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.AccelerationCharges >= 1 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(accelColor, $"{state.AccelerationCharges}/2");

            ImGui.EndTable();
        }
    }

    private static void DrawTargetSection(CirceDebugState state)
    {
        ImGui.Text("Target");
        ImGui.Separator();

        if (ImGui.BeginTable("RdmTargetTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
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
