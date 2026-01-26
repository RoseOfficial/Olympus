using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Rotation.HermesCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Ninja tab: Hermes-specific debug info including mudra, Ninki, and buff tracking.
/// </summary>
public static class HermesTab
{
    public static void Draw(HermesDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Ninja rotation not active.");
            ImGui.TextDisabled("Switch to Ninja to see debug info.");
            return;
        }

        // Mudra Section
        DrawMudraSection(state);
        ImGui.Spacing();

        // Gauge Section
        DrawGaugeSection(state);
        ImGui.Spacing();

        // Buffs Section
        DrawBuffSection(state);
        ImGui.Spacing();

        // Debuffs Section
        DrawDebuffSection(state);
        ImGui.Spacing();

        // Positional Section
        DrawPositionalSection(state);
    }

    private static void DrawMudraSection(HermesDebugState state)
    {
        ImGui.Text("Mudra");
        ImGui.Separator();

        if (ImGui.BeginTable("NinMudraTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Mudra Active
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Mudra Active:");
            ImGui.TableNextColumn();
            if (state.IsMudraActive)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), $"Yes ({state.MudraCount} in sequence)");
            }
            else
            {
                ImGui.TextDisabled("No");
            }

            // Mudra Sequence
            if (state.IsMudraActive && !string.IsNullOrEmpty(state.MudraSequence))
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Sequence:");
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(1f, 0.8f, 0.5f, 1f), state.MudraSequence);
            }

            // Pending Ninjutsu
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Pending Ninjutsu:");
            ImGui.TableNextColumn();
            var ninjutsuColor = state.PendingNinjutsu != 0 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(ninjutsuColor, state.PendingNinjutsu.ToString());

            // Kassatsu
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Kassatsu:");
            ImGui.TableNextColumn();
            if (state.HasKassatsu)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.8f, 1f), "Active");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Ten Chi Jin
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Ten Chi Jin:");
            ImGui.TableNextColumn();
            if (state.HasTenChiJin)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), $"Active ({state.TenChiJinStacks} stacks)");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawGaugeSection(HermesDebugState state)
    {
        ImGui.Text("Gauge");
        ImGui.Separator();

        if (ImGui.BeginTable("NinGaugeTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Ninki
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Ninki:");
            ImGui.TableNextColumn();
            var ninkiPercent = state.Ninki / 100f;
            ImGui.ProgressBar(ninkiPercent, new Vector2(-1, 0), $"{state.Ninki}/100");

            // Kazematoi
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Kazematoi:");
            ImGui.TableNextColumn();
            var kazColor = state.Kazematoi >= 4 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(kazColor, $"{state.Kazematoi}/5");

            // Combo Step
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Combo:");
            ImGui.TableNextColumn();
            var comboColor = state.ComboStep > 0 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(comboColor, state.ComboStep > 0 ? $"Step {state.ComboStep} ({state.ComboTimeRemaining:F1}s)" : "None");

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(HermesDebugState state)
    {
        ImGui.Text("Buffs");
        ImGui.Separator();

        if (ImGui.BeginTable("NinBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Suiton
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Suiton:");
            ImGui.TableNextColumn();
            if (state.HasSuiton)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), $"{state.SuitonRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Bunshin
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Bunshin:");
            ImGui.TableNextColumn();
            if (state.HasBunshin)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), $"{state.BunshinStacks} stacks");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Phantom Kamaitachi
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Phantom Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasPhantomKamaitachiReady);

            // Raiju Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Raiju Ready:");
            ImGui.TableNextColumn();
            if (state.HasRaijuReady)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), $"{state.RaijuStacks} stacks");
            }
            else
            {
                ImGui.TextDisabled("No");
            }

            // Tenri Jindo
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Tenri Jindo Ready:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasTenriJindoReady);

            ImGui.EndTable();
        }
    }

    private static void DrawDebuffSection(HermesDebugState state)
    {
        ImGui.Text("Target Debuffs");
        ImGui.Separator();

        if (ImGui.BeginTable("NinDebuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Kunai's Bane
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Kunai's Bane:");
            ImGui.TableNextColumn();
            if (state.HasKunaisBaneOnTarget)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), $"{state.KunaisBaneRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Not applied");
            }

            // Dokumori
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Dokumori:");
            ImGui.TableNextColumn();
            if (state.HasDokumoriOnTarget)
            {
                ImGui.TextColored(new Vector4(0.8f, 0.5f, 1f, 1f), $"{state.DokumoriRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Not applied");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawPositionalSection(HermesDebugState state)
    {
        ImGui.Text("Positional");
        ImGui.Separator();

        if (ImGui.BeginTable("NinPositionalTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
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
