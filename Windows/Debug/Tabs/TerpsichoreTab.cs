using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Rotation.TerpsichoreCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Dancer tab: Terpsichore-specific debug info including dance steps, Esprit, and partner tracking.
/// </summary>
public static class TerpsichoreTab
{
    public static void Draw(TerpsichoreDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Dancer rotation not active.");
            ImGui.TextDisabled("Switch to Dancer to see debug info.");
            return;
        }

        // Dance Section
        DrawDanceSection(state);
        ImGui.Spacing();

        // Gauge Section
        DrawGaugeSection(state);
        ImGui.Spacing();

        // Procs Section
        DrawProcSection(state);
        ImGui.Spacing();

        // Buffs Section
        DrawBuffSection(state);
        ImGui.Spacing();

        // Target Section
        DrawTargetSection(state);
    }

    private static void DrawDanceSection(TerpsichoreDebugState state)
    {
        ImGui.Text("Dance");
        ImGui.Separator();

        if (ImGui.BeginTable("DncDanceTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Dancing State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Dancing:");
            ImGui.TableNextColumn();
            if (state.IsDancing)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.8f, 1f), $"Yes (Step {state.StepIndex})");
            }
            else
            {
                ImGui.TextDisabled("No");
            }

            // Current Step
            if (state.IsDancing)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Next Step:");
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), state.CurrentStep);
            }

            // Standard Finish
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Standard Finish:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasStandardFinish);

            // Technical Finish
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Technical Finish:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasTechnicalFinish);

            ImGui.EndTable();
        }
    }

    private static void DrawGaugeSection(TerpsichoreDebugState state)
    {
        ImGui.Text("Gauge");
        ImGui.Separator();

        if (ImGui.BeginTable("DncGaugeTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Esprit
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Esprit:");
            ImGui.TableNextColumn();
            var espritPercent = state.Esprit / 100f;
            ImGui.ProgressBar(espritPercent, new Vector2(-1, 0), $"{state.Esprit}/100");

            // Feathers
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Feathers:");
            ImGui.TableNextColumn();
            var featherColor = state.Feathers >= 4 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.Feathers >= 3 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(featherColor, $"{state.Feathers}/4");

            ImGui.EndTable();
        }
    }

    private static void DrawProcSection(TerpsichoreDebugState state)
    {
        ImGui.Text("Procs");
        ImGui.Separator();

        if (ImGui.BeginTable("DncProcTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Silken Symmetry
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Silken Symmetry:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasSilkenSymmetry);

            // Silken Flow
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Silken Flow:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasSilkenFlow);

            // Threefold Fan Dance
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Threefold Fan:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasThreefoldFanDance);

            // Fourfold Fan Dance
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Fourfold Fan:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFourfoldFanDance);

            // Flourishing Finish
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Flourishing Finish:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFlourishingFinish);

            // Flourishing Starfall
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Flourishing Starfall:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFlourishingStarfall);

            // Last Dance Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Last Dance:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasLastDanceReady);

            // Finishing Move Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Finishing Move:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFinishingMoveReady);

            // Dance of the Dawn Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Dance of Dawn:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasDanceOfTheDawnReady);

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(TerpsichoreDebugState state)
    {
        ImGui.Text("Buffs");
        ImGui.Separator();

        if (ImGui.BeginTable("DncBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Devilment
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Devilment:");
            ImGui.TableNextColumn();
            if (state.HasDevilment)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.DevilmentRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawTargetSection(TerpsichoreDebugState state)
    {
        ImGui.Text("Target & Partner");
        ImGui.Separator();

        if (ImGui.BeginTable("DncTargetTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Dance Partner
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Dance Partner:");
            ImGui.TableNextColumn();
            if (state.HasDancePartner)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.8f, 1f), state.DancePartner);
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "None");
            }

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
}
