using System;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Services.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Overheal tab: detailed overheal statistics with per-spell and per-target breakdowns.
/// </summary>
public static class OverhealTab
{
    public static void Draw(DebugSnapshot snapshot, Configuration config, DebugService debugService)
    {
        var stats = snapshot.OverhealStats;

        // Summary Section
        if (IsSectionVisible(config, "OverhealSummary"))
        {
            DrawSummary(stats);
            ImGui.Spacing();
        }

        // By Spell Section
        if (IsSectionVisible(config, "OverhealBySpell"))
        {
            DrawBySpell(stats);
            ImGui.Spacing();
        }

        // By Target Section
        if (IsSectionVisible(config, "OverhealByTarget"))
        {
            DrawByTarget(stats);
            ImGui.Spacing();
        }

        // Recent Overheals Timeline
        if (IsSectionVisible(config, "OverhealTimeline"))
        {
            DrawTimeline(stats);
            ImGui.Spacing();
        }

        // Session Controls
        if (IsSectionVisible(config, "OverhealControls"))
        {
            DrawControls(debugService);
        }
    }

    private static void DrawSummary(DebugOverhealStats stats)
    {
        ImGui.Text("Summary");
        ImGui.Separator();

        var effectiveHealing = stats.EffectiveHealing;
        var overhealpct = stats.OverhealPercent;

        if (ImGui.BeginTable("OverhealSummaryTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Session Duration
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Session Duration:");
            ImGui.TableNextColumn();
            ImGui.Text(FormatDuration(stats.SessionDuration));

            // Total Healing
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Total Healing:");
            ImGui.TableNextColumn();
            ImGui.TextColored(DebugColors.Heal, $"{stats.TotalHealing:N0}");

            // Effective Healing
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Effective Healing:");
            ImGui.TableNextColumn();
            ImGui.TextColored(DebugColors.Success, $"{effectiveHealing:N0}");

            // Total Overheal
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Total Overheal:");
            ImGui.TableNextColumn();
            ImGui.TextColored(GetOverhealColor(overhealpct), $"{stats.TotalOverheal:N0}");

            // Overheal %
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Overheal %:");
            ImGui.TableNextColumn();
            ImGui.TextColored(GetOverhealColor(overhealpct), $"{overhealpct:F1}%");

            ImGui.EndTable();
        }

        // Visual indicator bar
        if (stats.TotalHealing > 0)
        {
            ImGui.Spacing();
            DrawOverhealBar(stats.TotalHealing, effectiveHealing, stats.TotalOverheal);
        }
    }

    private static void DrawOverhealBar(int total, int effective, int overheal)
    {
        var effectiveRatio = (float)effective / total;
        var barSize = new Vector2(ImGui.GetContentRegionAvail().X, 20);

        var drawList = ImGui.GetWindowDrawList();
        var cursorPos = ImGui.GetCursorScreenPos();

        // Background
        drawList.AddRectFilled(
            cursorPos,
            cursorPos + barSize,
            ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 1.0f)));

        // Effective healing (green)
        var effectiveWidth = barSize.X * effectiveRatio;
        drawList.AddRectFilled(
            cursorPos,
            cursorPos + new Vector2(effectiveWidth, barSize.Y),
            ImGui.GetColorU32(DebugColors.Success));

        // Overheal (red)
        drawList.AddRectFilled(
            cursorPos + new Vector2(effectiveWidth, 0),
            cursorPos + barSize,
            ImGui.GetColorU32(DebugColors.Failure));

        // Labels
        var effectiveLabel = $"Effective: {effectiveRatio:P0}";
        var overhealLabel = $"Overheal: {1 - effectiveRatio:P0}";

        // Only show labels if they fit
        if (effectiveWidth > 80)
        {
            var textPos = cursorPos + new Vector2(4, 2);
            drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(0, 0, 0, 1)), effectiveLabel);
        }

        if (barSize.X - effectiveWidth > 70)
        {
            var textPos = cursorPos + new Vector2(effectiveWidth + 4, 2);
            drawList.AddText(textPos, ImGui.GetColorU32(new Vector4(1, 1, 1, 1)), overhealLabel);
        }

        ImGui.Dummy(barSize);
    }

    private static void DrawBySpell(DebugOverhealStats stats)
    {
        ImGui.Text("By Spell");
        ImGui.Separator();

        if (stats.BySpell.Count == 0)
        {
            ImGui.TextColored(DebugColors.Dim, "No healing data yet");
            return;
        }

        if (ImGui.BeginTable("OverhealBySpellTable", 5,
            ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.Sortable))
        {
            ImGui.TableSetupColumn("Spell", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Casts", ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn("Total Heal", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Overheal", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Overheal %", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var spell in stats.BySpell)
            {
                ImGui.TableNextRow();

                // Spell name
                ImGui.TableNextColumn();
                ImGui.Text(spell.SpellName);

                // Cast count
                ImGui.TableNextColumn();
                ImGui.Text($"{spell.CastCount}");

                // Total healing
                ImGui.TableNextColumn();
                ImGui.TextColored(DebugColors.Heal, $"{spell.TotalHealing:N0}");

                // Overheal
                ImGui.TableNextColumn();
                var overhealColor = GetOverhealColor(spell.OverhealPercent);
                ImGui.TextColored(overhealColor, $"{spell.TotalOverheal:N0}");

                // Overheal %
                ImGui.TableNextColumn();
                ImGui.TextColored(overhealColor, $"{spell.OverhealPercent:F1}%");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawByTarget(DebugOverhealStats stats)
    {
        ImGui.Text("By Target");
        ImGui.Separator();

        if (stats.ByTarget.Count == 0)
        {
            ImGui.TextColored(DebugColors.Dim, "No healing data yet");
            return;
        }

        if (ImGui.BeginTable("OverhealByTargetTable", 5,
            ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Target", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Heals", ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn("Total Heal", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Overheal", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Overheal %", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var target in stats.ByTarget)
            {
                ImGui.TableNextRow();

                // Target name
                ImGui.TableNextColumn();
                ImGui.Text(target.TargetName);

                // Heal count
                ImGui.TableNextColumn();
                ImGui.Text($"{target.HealCount}");

                // Total healing
                ImGui.TableNextColumn();
                ImGui.TextColored(DebugColors.Heal, $"{target.TotalHealing:N0}");

                // Overheal
                ImGui.TableNextColumn();
                var overhealColor = GetOverhealColor(target.OverhealPercent);
                ImGui.TextColored(overhealColor, $"{target.TotalOverheal:N0}");

                // Overheal %
                ImGui.TableNextColumn();
                ImGui.TextColored(overhealColor, $"{target.OverhealPercent:F1}%");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawTimeline(DebugOverhealStats stats)
    {
        var headerText = $"Recent Overheals ({stats.RecentOverheals.Count})";
        if (!ImGui.CollapsingHeader(headerText))
            return;

        if (stats.RecentOverheals.Count == 0)
        {
            ImGui.TextColored(DebugColors.Dim, "No overheals recorded yet");
            return;
        }

        if (ImGui.BeginTable("OverhealTimelineTable", 5,
            ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Spell", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Target", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Heal", ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("Overheal", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var evt in stats.RecentOverheals)
            {
                ImGui.TableNextRow();

                // Time ago
                ImGui.TableNextColumn();
                var ageColor = evt.SecondsAgo < 5f ? DebugColors.Warning : DebugColors.Dim;
                ImGui.TextColored(ageColor, $"{evt.SecondsAgo:F0}s ago");

                // Spell name
                ImGui.TableNextColumn();
                ImGui.Text(evt.SpellName);

                // Target name
                ImGui.TableNextColumn();
                ImGui.Text(evt.TargetName);

                // Heal amount
                ImGui.TableNextColumn();
                ImGui.TextColored(DebugColors.Heal, $"{evt.HealAmount:N0}");

                // Overheal amount and %
                ImGui.TableNextColumn();
                var overhealColor = GetOverhealColor(evt.OverhealPercent);
                ImGui.TextColored(overhealColor, $"{evt.OverhealAmount:N0} ({evt.OverhealPercent:F0}%)");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawControls(DebugService debugService)
    {
        ImGui.Separator();

        if (ImGui.Button("Reset Statistics"))
        {
            debugService.ResetOverhealStatistics();
        }

        ImGui.SameLine();
        ImGui.TextColored(DebugColors.Dim, "Clears all overheal data and starts a new session");
    }

    /// <summary>
    /// Gets color based on overheal percentage.
    /// Green (&lt;25%): Efficient healing
    /// Yellow (25-50%): Moderate overheal
    /// Red (&gt;50%): Excessive overheal
    /// </summary>
    private static Vector4 GetOverhealColor(float percent) => percent switch
    {
        < 25f => DebugColors.Success,
        < 50f => DebugColors.Warning,
        _ => DebugColors.Failure
    };

    private static string FormatDuration(TimeSpan duration)
    {
        if (duration.TotalHours >= 1)
            return $"{(int)duration.TotalHours}h {duration.Minutes}m {duration.Seconds}s";
        if (duration.TotalMinutes >= 1)
            return $"{duration.Minutes}m {duration.Seconds}s";
        return $"{duration.Seconds}s";
    }

    private static bool IsSectionVisible(Configuration config, string section)
    {
        if (config.Debug.DebugSectionVisibility.TryGetValue(section, out var visible))
            return visible;
        return true;
    }
}
