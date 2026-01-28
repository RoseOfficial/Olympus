using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
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
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.DancerNotActive, "Dancer rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToDancer, "Switch to Dancer to see debug info."));
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
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Dance, "Dance"));
        ImGui.Separator();

        if (ImGui.BeginTable("DncDanceTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Dancing State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Dancing, "Dancing:"));
            ImGui.TableNextColumn();
            if (state.IsDancing)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.8f, 1f), $"Yes (Step {state.StepIndex})");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.No, "No"));
            }

            // Current Step
            if (state.IsDancing)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(Loc.T(LocalizedStrings.Debug.NextStep, "Next Step:"));
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), state.CurrentStep);
            }

            // Standard Finish
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.StandardFinish, "Standard Finish:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasStandardFinish);

            // Technical Finish
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TechnicalFinish, "Technical Finish:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasTechnicalFinish);

            ImGui.EndTable();
        }
    }

    private static void DrawGaugeSection(TerpsichoreDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Gauge, "Gauge"));
        ImGui.Separator();

        if (ImGui.BeginTable("DncGaugeTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Esprit
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Esprit, "Esprit:"));
            ImGui.TableNextColumn();
            var espritPercent = state.Esprit / 100f;
            ImGui.ProgressBar(espritPercent, new Vector2(-1, 0), $"{state.Esprit}/100");

            // Feathers
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Feathers, "Feathers:"));
            ImGui.TableNextColumn();
            var featherColor = state.Feathers >= 4 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.Feathers >= 3 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(featherColor, $"{state.Feathers}/4");

            ImGui.EndTable();
        }
    }

    private static void DrawProcSection(TerpsichoreDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Procs, "Procs"));
        ImGui.Separator();

        if (ImGui.BeginTable("DncProcTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Silken Symmetry
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SilkenSymmetry, "Silken Symmetry:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasSilkenSymmetry);

            // Silken Flow
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SilkenFlow, "Silken Flow:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasSilkenFlow);

            // Threefold Fan Dance
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ThreefoldFan, "Threefold Fan:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasThreefoldFanDance);

            // Fourfold Fan Dance
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.FourfoldFan, "Fourfold Fan:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFourfoldFanDance);

            // Flourishing Finish
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.FlourishingFinish, "Flourishing Finish:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFlourishingFinish);

            // Flourishing Starfall
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.FlourishingStarfall, "Flourishing Starfall:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFlourishingStarfall);

            // Last Dance Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.LastDance, "Last Dance:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasLastDanceReady);

            // Finishing Move Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.FinishingMove, "Finishing Move:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasFinishingMoveReady);

            // Dance of the Dawn Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DanceOfDawn, "Dance of Dawn:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasDanceOfTheDawnReady);

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(TerpsichoreDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs"));
        ImGui.Separator();

        if (ImGui.BeginTable("DncBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Devilment
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Devilment, "Devilment:"));
            ImGui.TableNextColumn();
            if (state.HasDevilment)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.DevilmentRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            ImGui.EndTable();
        }
    }

    private static void DrawTargetSection(TerpsichoreDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.TargetPartner, "Target & Partner"));
        ImGui.Separator();

        if (ImGui.BeginTable("DncTargetTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Dance Partner
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DancePartner, "Dance Partner:"));
            ImGui.TableNextColumn();
            if (state.HasDancePartner)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.8f, 1f), state.DancePartner);
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.NoneLabel, "None"));
            }

            // Current Target
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TargetLabel, "Target:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.CurrentTarget);

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
