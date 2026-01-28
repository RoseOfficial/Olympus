using System;
using System.Numerics;
using System.Text;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Services.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Performance tab: statistics, uptime, downtime tracking.
/// </summary>
public static class PerformanceTab
{
    public static void Draw(DebugSnapshot snapshot, Configuration config)
    {
        // Statistics Section
        if (IsSectionVisible(config, "Statistics"))
        {
            DrawStatistics(snapshot);
            ImGui.Spacing();
        }

        // Downtime Tracking Section
        if (IsSectionVisible(config, "Downtime"))
        {
            DrawDowntimeTracking(snapshot);
            ImGui.Spacing();
        }

        // Copy Button
        DrawCopyButton(snapshot);
    }

    private static void DrawStatistics(DebugSnapshot snapshot)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Statistics, "Statistics"));
        ImGui.Separator();

        var stats = snapshot.Statistics;
        var gcd = snapshot.GcdState;

        if (ImGui.BeginTable("StatsTable", 3, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Col1", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Col2", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Col3", ImGuiTableColumnFlags.WidthStretch);

            // Row 1: Attempts, Success, Rate
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.AttemptsFormat, "Attempts: {0}", stats.TotalAttempts));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.SuccessFormat, "Success: {0}", stats.SuccessCount));

            ImGui.TableNextColumn();
            var rateColor = DebugColors.GetFFLogsColor(stats.SuccessRate);
            ImGui.TextColored(rateColor, Loc.TFormat(LocalizedStrings.Debug.RateFormat, "Rate: {0:F1}%", stats.SuccessRate));

            // Row 2: GCD Uptime, Avg Gap, Top Failure
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var uptimeColor = DebugColors.GetFFLogsColor(stats.GcdUptime);
            ImGui.TextColored(uptimeColor, Loc.TFormat(LocalizedStrings.Debug.UptimeFormat, "GCD Uptime: {0:F1}%", stats.GcdUptime));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.AvgCastGap, "Avg Gap: {0:F2}s", stats.AverageCastGap));

            ImGui.TableNextColumn();
            if (!string.IsNullOrEmpty(stats.TopFailureReason))
            {
                ImGui.TextColored(DebugColors.Dim, Loc.TFormat(LocalizedStrings.Debug.TopFail, "Top fail: {0} ({1})", stats.TopFailureReason, stats.TopFailureCount));
            }

            ImGui.EndTable();
        }

        // GCD Status Row
        var gcdColor = gcd.DebugGcdReady ? DebugColors.Failure : DebugColors.Success;
        ImGui.TextColored(gcdColor, gcd.DebugGcdReady
            ? Loc.T(LocalizedStrings.Debug.GcdReadyDowntimeLabel, "GCD: READY (downtime)")
            : Loc.T(LocalizedStrings.Debug.GcdActive, "GCD: ACTIVE"));
        ImGui.SameLine();
        ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.GcdStatusRow, "Rem:{0:F2}s Cast:{1} Anim:{2} Act:{3}",
            gcd.GcdRemaining,
            gcd.IsCasting ? "Y" : "N",
            gcd.AnimationLockRemaining > 0 ? "Y" : "N",
            gcd.DebugIsActive ? "Y" : "N"));
    }

    private static void DrawDowntimeTracking(DebugSnapshot snapshot)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.DowntimeTracking, "Downtime Tracking"));
        ImGui.Separator();

        var stats = snapshot.Statistics;

        if (stats.DowntimeEventCount == 0)
        {
            ImGui.TextColored(DebugColors.Success, Loc.T(LocalizedStrings.Debug.NoDowntimeEventsRecorded, "No downtime events recorded"));
            return;
        }

        if (ImGui.BeginTable("DowntimeTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Event count
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DowntimeEvents, "Downtime Events:"));
            ImGui.TableNextColumn();
            ImGui.TextColored(DebugColors.Warning, stats.DowntimeEventCount.ToString());

            // Last occurrence
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.LastOccurrence, "Last Occurrence:"));
            ImGui.TableNextColumn();
            var ago = (DateTime.Now - stats.LastDowntimeTime).TotalSeconds;
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.SecondsAgoFormat, "{0:F1}s ago", ago));

            // Last reason
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.LastReason, "Last Reason:"));
            ImGui.TableNextColumn();
            ImGui.TextColored(DebugColors.Dim, stats.LastDowntimeReason);

            ImGui.EndTable();
        }
    }

    private static void DrawCopyButton(DebugSnapshot snapshot)
    {
        if (ImGui.Button(Loc.T(LocalizedStrings.Debug.CopyDebugInfo, "Copy Debug Info")))
        {
            CopyDebugInfoToClipboard(snapshot);
        }
    }

    private static void CopyDebugInfoToClipboard(DebugSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== Olympus Debug Info ===");
        sb.AppendLine($"Timestamp: {DateTime.Now:HH:mm:ss.fff}");
        sb.AppendLine();

        // Statistics
        var stats = snapshot.Statistics;
        sb.AppendLine("--- Statistics ---");
        sb.AppendLine($"Total Attempts: {stats.TotalAttempts}");
        sb.AppendLine($"Success Count: {stats.SuccessCount}");
        sb.AppendLine($"Success Rate: {stats.SuccessRate:F1}%");
        sb.AppendLine($"GCD Uptime: {stats.GcdUptime:F1}%");
        sb.AppendLine($"Avg Cast Gap: {stats.AverageCastGap:F2}s");
        sb.AppendLine($"Downtime Events: {stats.DowntimeEventCount}");
        sb.AppendLine();

        // GCD State
        var gcd = snapshot.GcdState;
        sb.AppendLine("--- GCD State ---");
        sb.AppendLine($"State: {gcd.State}");
        sb.AppendLine($"GCD Remaining: {gcd.GcdRemaining:F2}s");
        sb.AppendLine($"Animation Lock: {gcd.AnimationLockRemaining:F2}s");
        sb.AppendLine($"Is Casting: {gcd.IsCasting}");
        sb.AppendLine($"Can Execute GCD: {gcd.CanExecuteGcd}");
        sb.AppendLine($"Can Execute oGCD: {gcd.CanExecuteOgcd}");
        sb.AppendLine($"Weave Slots: {gcd.WeaveSlots}");
        sb.AppendLine($"Last Action: {gcd.LastActionName}");
        sb.AppendLine();

        // Rotation State
        var rotation = snapshot.Rotation;
        sb.AppendLine("--- Rotation State ---");
        sb.AppendLine($"Planning State: {rotation.PlanningState}");
        sb.AppendLine($"Planned Action: {rotation.PlannedAction}");
        sb.AppendLine($"DPS State: {rotation.DpsState}");
        sb.AppendLine($"Target Info: {rotation.TargetInfo}");
        sb.AppendLine();

        // Healing State
        var healing = snapshot.Healing;
        sb.AppendLine("--- Healing State ---");
        sb.AppendLine($"AoE Status: {healing.AoEStatus}");
        sb.AppendLine($"Injured Count: {healing.AoEInjuredCount}");
        sb.AppendLine($"Player HP: {healing.PlayerHpPercent:F1}%");
        sb.AppendLine($"Party List: {healing.PartyListCount}");
        sb.AppendLine($"Valid Members: {healing.PartyValidCount}");
        sb.AppendLine($"Pending Heals: {healing.PendingHeals.Count} ({healing.TotalPendingHealAmount} HP)");
        sb.AppendLine();

        // Shadow HP
        if (healing.ShadowHpEntries.Count > 0)
        {
            sb.AppendLine("--- Shadow HP ---");
            foreach (var entry in healing.ShadowHpEntries)
            {
                sb.AppendLine($"  {entry.EntityName}: Game={entry.GameHp} ({entry.GameHpPercent:F0}%), Shadow={entry.ShadowHp} ({entry.ShadowHpPercent:F0}%), Delta={entry.Delta:+#;-#;0}");
            }
        }

        ImGui.SetClipboardText(sb.ToString());
    }

    private static bool IsSectionVisible(Configuration config, string section)
    {
        if (config.Debug.DebugSectionVisibility.TryGetValue(section, out var visible))
            return visible;
        return true;
    }
}
