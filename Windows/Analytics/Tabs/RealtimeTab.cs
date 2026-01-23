using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Services.Analytics;

namespace Olympus.Windows.Analytics.Tabs;

/// <summary>
/// Realtime tab: live combat metrics display.
/// </summary>
public static class RealtimeTab
{
    // Colors for metrics
    private static readonly Vector4 GoodColor = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 WarningColor = new(0.9f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 BadColor = new(0.9f, 0.3f, 0.3f, 1.0f);
    private static readonly Vector4 NeutralColor = new(0.7f, 0.7f, 0.7f, 1.0f);
    private static readonly Vector4 InfoColor = new(0.4f, 0.7f, 1.0f, 1.0f);

    public static void Draw(IPerformanceTracker tracker, AnalyticsConfig config)
    {
        var snapshot = tracker.GetCurrentSnapshot();

        // Combat Status Section
        if (IsSectionVisible(config, "RealtimeCombatStatus"))
        {
            DrawCombatStatus(tracker, snapshot);
            ImGui.Spacing();
        }

        if (snapshot == null)
        {
            ImGui.TextColored(NeutralColor, "Not in combat. Metrics will appear when combat starts.");
            return;
        }

        // Metrics Section
        if (IsSectionVisible(config, "RealtimeMetrics"))
        {
            DrawMetrics(snapshot);
            ImGui.Spacing();
        }

        // Cooldowns Section
        if (IsSectionVisible(config, "RealtimeCooldowns"))
        {
            DrawCooldowns(snapshot);
        }
    }

    private static void DrawCombatStatus(IPerformanceTracker tracker, CombatMetricsSnapshot? snapshot)
    {
        var isTracking = tracker.IsTracking;
        var duration = tracker.CombatDuration;

        if (ImGui.BeginTable("CombatStatusTable", 2, ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Status:");
            ImGui.TableNextColumn();
            if (isTracking)
            {
                ImGui.TextColored(GoodColor, "TRACKING");
            }
            else
            {
                ImGui.TextColored(NeutralColor, "IDLE");
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Combat:");
            ImGui.TableNextColumn();
            if (duration > 0)
            {
                var minutes = (int)(duration / 60);
                var seconds = (int)(duration % 60);
                ImGui.Text($"{minutes}:{seconds:D2}");
            }
            else
            {
                ImGui.TextColored(NeutralColor, "0:00");
            }

            if (snapshot != null)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("GCD Uptime:");
                ImGui.TableNextColumn();
                var uptimeColor = GetUptimeColor(snapshot.GcdUptime);
                ImGui.TextColored(uptimeColor, $"{snapshot.GcdUptime:F1}%");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawMetrics(CombatMetricsSnapshot snapshot)
    {
        ImGui.Text("Metrics");
        ImGui.Separator();

        if (ImGui.BeginTable("MetricsTable", 2, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Metric", ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // DPS row (placeholder until damage tracking is implemented)
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("DPS:");
            ImGui.TableNextColumn();
            if (snapshot.PersonalDps > 0)
            {
                ImGui.TextColored(InfoColor, $"{snapshot.PersonalDps:N0}");
            }
            else
            {
                ImGui.TextColored(NeutralColor, "N/A");
            }

            // Healing row
            if (snapshot.TotalHealing > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Healing:");
                ImGui.TableNextColumn();
                var overhealColor = GetOverhealColor(snapshot.OverhealPercent);
                ImGui.TextColored(InfoColor, $"{snapshot.TotalHealing:N0}");
                ImGui.SameLine();
                ImGui.TextColored(overhealColor, $"({snapshot.OverhealPercent:F0}% overheal)");
            }

            // Deaths row
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Deaths:");
            ImGui.TableNextColumn();
            var deathColor = snapshot.Deaths > 0 ? BadColor : GoodColor;
            ImGui.TextColored(deathColor, snapshot.Deaths.ToString());

            // Near-deaths row
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Near-Deaths:");
            ImGui.TableNextColumn();
            var nearDeathColor = snapshot.NearDeaths > 2 ? WarningColor : (snapshot.NearDeaths > 0 ? NeutralColor : GoodColor);
            ImGui.TextColored(nearDeathColor, snapshot.NearDeaths.ToString());

            ImGui.EndTable();
        }
    }

    private static void DrawCooldowns(CombatMetricsSnapshot snapshot)
    {
        ImGui.Text("Cooldowns");
        ImGui.Separator();

        if (snapshot.Cooldowns.Count == 0)
        {
            ImGui.TextColored(NeutralColor, "No cooldowns tracked for this job.");
            return;
        }

        if (ImGui.BeginTable("CooldownsTable", 4, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Ability", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Uses", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Efficiency", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Drift", ImGuiTableColumnFlags.WidthFixed, 60);

            ImGui.TableHeadersRow();

            foreach (var cooldown in snapshot.Cooldowns)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(cooldown.Name);

                ImGui.TableNextColumn();
                ImGui.Text($"{cooldown.TimesUsed}/{cooldown.OptimalUses}");

                ImGui.TableNextColumn();
                var effColor = GetEfficiencyColor(cooldown.Efficiency);
                ImGui.TextColored(effColor, $"{cooldown.Efficiency:F0}%");

                ImGui.TableNextColumn();
                var driftColor = GetDriftColor(cooldown.AverageDrift);
                ImGui.TextColored(driftColor, $"{cooldown.AverageDrift:F1}s");
            }

            ImGui.EndTable();
        }
    }

    private static Vector4 GetUptimeColor(float uptime) => uptime switch
    {
        >= 95f => GoodColor,
        >= 85f => WarningColor,
        _ => BadColor
    };

    private static Vector4 GetOverhealColor(float overheal) => overheal switch
    {
        <= 20f => GoodColor,
        <= 35f => WarningColor,
        _ => BadColor
    };

    private static Vector4 GetEfficiencyColor(float efficiency) => efficiency switch
    {
        >= 80f => GoodColor,
        >= 60f => WarningColor,
        _ => BadColor
    };

    private static Vector4 GetDriftColor(float drift) => drift switch
    {
        <= 3f => GoodColor,
        <= 8f => WarningColor,
        _ => BadColor
    };

    private static bool IsSectionVisible(AnalyticsConfig config, string section)
    {
        if (config.SectionVisibility.TryGetValue(section, out var visible))
            return visible;
        return true;
    }
}
