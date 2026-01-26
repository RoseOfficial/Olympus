using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Rotation.ZeusCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Dragoon tab: Zeus-specific debug info including Eye gauge, Life of the Dragon, and combo state.
/// </summary>
public static class ZeusTab
{
    public static void Draw(ZeusDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Dragoon rotation not active.");
            ImGui.TextDisabled("Switch to Dragoon to see debug info.");
            return;
        }

        // Gauge Section
        DrawGaugeSection(state);
        ImGui.Spacing();

        // Combo Section
        DrawComboSection(state);
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

    private static void DrawGaugeSection(ZeusDebugState state)
    {
        ImGui.Text("Gauge");
        ImGui.Separator();

        if (ImGui.BeginTable("DrgGaugeTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Life of the Dragon
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Dragon State:");
            ImGui.TableNextColumn();
            var lotdColor = state.IsLifeOfDragonActive ? new Vector4(1f, 0.6f, 0.2f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(lotdColor, state.FormatLifeState());

            // Eye Count
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Dragon Eyes:");
            ImGui.TableNextColumn();
            var eyeColor = state.EyeCount switch
            {
                2 => new Vector4(0.5f, 1f, 0.5f, 1f),
                1 => new Vector4(1f, 1f, 0.5f, 1f),
                _ => new Vector4(0.7f, 0.7f, 0.7f, 1f)
            };
            ImGui.TextColored(eyeColor, $"{state.EyeCount}/2");

            // Firstmind's Focus
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Firstmind's Focus:");
            ImGui.TableNextColumn();
            var focusColor = state.FirstmindsFocus >= 2 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(focusColor, $"{state.FirstmindsFocus}/2");

            ImGui.EndTable();
        }
    }

    private static void DrawComboSection(ZeusDebugState state)
    {
        ImGui.Text("Combo");
        ImGui.Separator();

        if (ImGui.BeginTable("DrgComboTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Combo Step
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Combo State:");
            ImGui.TableNextColumn();
            var comboText = ZeusDebugState.FormatComboState(state.LastComboAction, state.ComboStep);
            var comboColor = state.ComboStep > 0 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(comboColor, comboText);

            // Combo Timer
            if (state.ComboTimeRemaining > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Combo Timer:");
                ImGui.TableNextColumn();
                var timerColor = state.ComboTimeRemaining < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
                ImGui.TextColored(timerColor, $"{state.ComboTimeRemaining:F1}s");
            }

            // DoT
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("DoT:");
            ImGui.TableNextColumn();
            if (state.HasDotOnTarget)
            {
                var dotColor = state.DotRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(dotColor, $"{state.DotRemaining:F1}s remaining");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Not applied");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(ZeusDebugState state)
    {
        ImGui.Text("Buffs");
        ImGui.Separator();

        if (ImGui.BeginTable("DrgBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Power Surge
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Power Surge:");
            ImGui.TableNextColumn();
            if (state.HasPowerSurge)
            {
                var color = state.PowerSurgeRemaining < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.PowerSurgeRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Lance Charge
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Lance Charge:");
            ImGui.TableNextColumn();
            if (state.HasLanceCharge)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), $"{state.LanceChargeRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Life Surge
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Life Surge:");
            ImGui.TableNextColumn();
            if (state.HasLifeSurge)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Active");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Battle Litany
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Battle Litany:");
            ImGui.TableNextColumn();
            if (state.HasBattleLitany)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), $"{state.BattleLitanyRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Right Eye
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Right Eye:");
            ImGui.TableNextColumn();
            if (state.HasRightEye)
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

    private static void DrawProcSection(ZeusDebugState state)
    {
        ImGui.Text("Procs");
        ImGui.Separator();

        if (ImGui.BeginTable("DrgProcTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Dive Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Dive Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasDiveReady);

            // Nastrond Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Nastrond Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasNastrondReady);

            // Stardiver Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Stardiver Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasStardiverReady);

            // Starcross Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Starcross Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasStarcrossReady);

            // Fang and Claw
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Fang and Claw:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFangAndClawBared);

            // Wheeling Thrust
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Wheel in Motion:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasWheelInMotion);

            // Draconian Fire
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Draconian Fire:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasDraconianFire);

            ImGui.EndTable();
        }
    }

    private static void DrawPositionalSection(ZeusDebugState state)
    {
        ImGui.Text("Positional");
        ImGui.Separator();

        if (ImGui.BeginTable("DrgPositionalTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
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
            if (state.HasTrueNorth)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Active");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

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
