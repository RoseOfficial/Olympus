using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Services.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Overview tab: rotation state, target info, quick stats.
/// </summary>
public static class OverviewTab
{
    public static void Draw(DebugSnapshot snapshot, Configuration config)
    {
        // GCD Planning Section
        if (IsSectionVisible(config, "GcdPlanning"))
        {
            DrawGcdPlanning(snapshot);
            ImGui.Spacing();
        }

        // Quick Stats Section
        if (IsSectionVisible(config, "QuickStats"))
        {
            DrawQuickStats(snapshot);
        }
    }

    private static void DrawGcdPlanning(DebugSnapshot snapshot)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.GcdPlanning, "GCD Planning"));
        ImGui.Separator();

        var rotation = snapshot.Rotation;

        if (ImGui.BeginTable("GcdPlanningTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Planning state
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.State, "State:"));
            ImGui.TableNextColumn();
            ImGui.TextColored(DebugColors.GetPlanningStateColor(rotation.PlanningState), rotation.PlanningState);

            // Planned action
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PlannedAction, "Planned Action:"));
            ImGui.TableNextColumn();
            var actionColor = rotation.PlannedAction != "None" && rotation.PlannedAction != "No target"
                ? DebugColors.Success : DebugColors.Failure;
            ImGui.TextColored(actionColor, rotation.PlannedAction);

            // DPS state
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Dps, "DPS:"));
            ImGui.TableNextColumn();
            var dpsColor = GetDpsStateColor(rotation.DpsState);
            ImGui.TextColored(dpsColor, rotation.DpsState);

            // Target info
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Target, "Target:"));
            ImGui.TableNextColumn();
            ImGui.Text(rotation.TargetInfo);

            ImGui.EndTable();
        }
    }

    private static void DrawQuickStats(DebugSnapshot snapshot)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.QuickStats, "Quick Stats"));
        ImGui.Separator();

        var stats = snapshot.Statistics;
        var gcd = snapshot.GcdState;

        if (ImGui.BeginTable("QuickStatsTable", 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Stat1", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Stat2", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Stat3", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Stat4", ImGuiTableColumnFlags.WidthStretch);

            // Row 1: Attempts, Success, Rate, GCD State
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.AttemptsFormat, "Attempts: {0}", stats.TotalAttempts));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.SuccessFormat, "Success: {0}", stats.SuccessCount));

            ImGui.TableNextColumn();
            var rateColor = DebugColors.GetFFLogsColor(stats.SuccessRate);
            ImGui.TextColored(rateColor, Loc.TFormat(LocalizedStrings.Debug.RateFormat, "Rate: {0:F1}%", stats.SuccessRate));

            ImGui.TableNextColumn();
            var gcdColor = DebugColors.GetGcdStateColor(gcd.State);
            ImGui.TextColored(gcdColor, Loc.TFormat(LocalizedStrings.Debug.GcdFormat, "GCD: {0}", gcd.State));

            // Row 2: GCD Uptime, Avg Gap, Weave, Last Action
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var uptimeColor = DebugColors.GetFFLogsColor(stats.GcdUptime);
            ImGui.TextColored(uptimeColor, Loc.TFormat(LocalizedStrings.Debug.UptimeFormat, "Uptime: {0:F1}%", stats.GcdUptime));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.GapFormat, "Gap: {0:F2}s", stats.AverageCastGap));

            ImGui.TableNextColumn();
            var weaveColor = gcd.CanExecuteOgcd ? DebugColors.Heal : DebugColors.Dim;
            var weaveText = gcd.CanExecuteOgcd
                ? Loc.T(LocalizedStrings.Debug.Yes, "Yes")
                : Loc.T(LocalizedStrings.Debug.No, "No");
            ImGui.TextColored(weaveColor, Loc.TFormat(LocalizedStrings.Debug.WeaveFormat, "Weave: {0} ({1})", weaveText, gcd.WeaveSlots));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.LastFormat, "Last: {0}", gcd.LastActionName));

            ImGui.EndTable();
        }
    }

    private static Vector4 GetDpsStateColor(string dpsState)
    {
        if (dpsState.StartsWith("Planned:"))
            return DebugColors.Success;
        if (dpsState.Contains("disabled"))
            return DebugColors.Failure;
        if (dpsState == "No enemy found" || dpsState == "No target")
            return DebugColors.Warning;
        return DebugColors.Dim;
    }

    private static bool IsSectionVisible(Configuration config, string section)
    {
        if (config.Debug.DebugSectionVisibility.TryGetValue(section, out var visible))
            return visible;
        return true; // Default to visible
    }
}
