using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Rotation.EchidnaCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Viper tab: Echidna-specific debug info including Serpent Offerings, Reawaken, and venom tracking.
/// </summary>
public static class EchidnaTab
{
    public static void Draw(EchidnaDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Viper rotation not active.");
            ImGui.TextDisabled("Switch to Viper to see debug info.");
            return;
        }

        // Gauge Section
        DrawGaugeSection(state);
        ImGui.Spacing();

        // Reawaken Section
        DrawReawakenSection(state);
        ImGui.Spacing();

        // Buffs Section
        DrawBuffSection(state);
        ImGui.Spacing();

        // Venom Section
        DrawVenomSection(state);
        ImGui.Spacing();

        // Positional Section
        DrawPositionalSection(state);
    }

    private static void DrawGaugeSection(EchidnaDebugState state)
    {
        ImGui.Text("Gauge");
        ImGui.Separator();

        if (ImGui.BeginTable("VprGaugeTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Serpent Offering
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Serpent Offering:");
            ImGui.TableNextColumn();
            var offeringPercent = state.SerpentOffering / 100f;
            ImGui.ProgressBar(offeringPercent, new Vector2(-1, 0), $"{state.SerpentOffering}/100");

            // Rattling Coils
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Rattling Coils:");
            ImGui.TableNextColumn();
            var coilColor = state.RattlingCoils >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(coilColor, $"{state.RattlingCoils}/3");

            // Combo
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Combo:");
            ImGui.TableNextColumn();
            var comboColor = state.ComboStep > 0 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(comboColor, state.ComboStep > 0 ? $"Step {state.ComboStep}" : "None");

            // Dread Combo
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Dread Combo:");
            ImGui.TableNextColumn();
            var dreadColor = state.DreadCombo != 0 ? new Vector4(0.8f, 0.5f, 1f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(dreadColor, state.DreadCombo.ToString());

            ImGui.EndTable();
        }
    }

    private static void DrawReawakenSection(EchidnaDebugState state)
    {
        ImGui.Text("Reawaken");
        ImGui.Separator();

        if (ImGui.BeginTable("VprReawakenTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Reawaken State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Reawaken:");
            ImGui.TableNextColumn();
            var reawakenColor = state.IsReawakened ? new Vector4(0.5f, 1f, 0.8f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(reawakenColor, state.GetReawakenState());

            // Anguine Tribute
            if (state.IsReawakened)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Anguine Tribute:");
                ImGui.TableNextColumn();
                var tributeColor = state.AnguineTribute > 0 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
                ImGui.TextColored(tributeColor, $"{state.AnguineTribute}");
            }

            // Ready to Reawaken
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Ready to Reawaken:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasReadyToReawaken);

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(EchidnaDebugState state)
    {
        ImGui.Text("Buffs");
        ImGui.Separator();

        if (ImGui.BeginTable("VprBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Hunter's Instinct
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Hunter's Instinct:");
            ImGui.TableNextColumn();
            if (state.HasHuntersInstinct)
            {
                var color = state.HuntersInstinctRemaining < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.HuntersInstinctRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Inactive");
            }

            // Swiftscaled
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Swiftscaled:");
            ImGui.TableNextColumn();
            if (state.HasSwiftscaled)
            {
                var color = state.SwiftscaledRemaining < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.SwiftscaledRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Inactive");
            }

            // Noxious Gnash
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Noxious Gnash:");
            ImGui.TableNextColumn();
            if (state.HasNoxiousGnash)
            {
                var color = state.NoxiousGnashRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.NoxiousGnashRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Not applied");
            }

            // Honed Steel/Reavers
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Honed Steel:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasHonedSteel);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Honed Reavers:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasHonedReavers);

            ImGui.EndTable();
        }
    }

    private static void DrawVenomSection(EchidnaDebugState state)
    {
        ImGui.Text("Venom");
        ImGui.Separator();

        if (ImGui.BeginTable("VprVenomTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Current Venom
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Active Venom:");
            ImGui.TableNextColumn();
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.8f, 1f), state.GetVenomState());

            // Twinfang/Twinblood Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Twinfang Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasPoisedForTwinfang);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Twinblood Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasPoisedForTwinblood);

            ImGui.EndTable();
        }
    }

    private static void DrawPositionalSection(EchidnaDebugState state)
    {
        ImGui.Text("Positional");
        ImGui.Separator();

        if (ImGui.BeginTable("VprPositionalTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
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
