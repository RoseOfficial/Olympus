using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Data;
using Olympus.Localization;
using Olympus.Services.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Checklist tab: shows every spell a job should be casting at the current level,
/// with a per-action cast count and a reset button.
/// </summary>
public static class ChecklistTab
{
    private static readonly Vector4 _green = new(0.0f, 1.0f, 0.0f, 1.0f);
    private static readonly Vector4 _red   = new(1.0f, 0.3f, 0.3f, 1.0f);
    private static readonly Vector4 _dim   = new(0.6f, 0.6f, 0.6f, 1.0f);

    public static void Draw(DebugSnapshot snapshot, DebugService debugService)
    {
        var jobId       = debugService.GetJobId();
        var playerLevel = debugService.GetPlayerLevel();

        // Build O(1) lookup from snapshot (rebuilt once per frame)
        var usageLookup = snapshot.Actions.SpellUsage
            .ToDictionary(s => s.ActionId, s => s.Count);

        // Get checklist for current job
        var groups = SpellChecklistRegistry.GetChecklist(jobId);

        if (groups.Length == 0)
        {
            ImGui.TextColored(_dim, "No checklist available for this job.");
            return;
        }

        // Header: job name, level, reset button
        var jobName = JobRegistry.GetJobName(jobId);
        ImGui.Text($"Job: {jobName} (Lv.{playerLevel})");
        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 100f);
        if (ImGui.Button(Loc.T(LocalizedStrings.Debug.ResetCounts, "Reset Counts")))
            debugService.ClearSpellUsageCounts();

        ImGui.Separator();

        // Track totals for footer
        int usedCount = 0;
        int totalCount = 0;

        foreach (var group in groups)
        {
            var actions = group.GetActions(playerLevel);
            if (actions.Length == 0)
                continue;

            // Section header
            ImGui.TextColored(_dim, group.Name);

            foreach (var action in actions)
            {
                usageLookup.TryGetValue(action.ActionId, out var count);

                // Colored dot
                var dotColor = count > 0 ? _green : _red;
                ImGui.TextColored(dotColor, "●");
                ImGui.SameLine();

                // Action name
                ImGui.Text(action.Name);

                // Cast count (right-aligned)
                var countText = count.ToString();
                var countWidth = ImGui.CalcTextSize(countText).X;
                ImGui.SameLine(ImGui.GetContentRegionAvail().X - countWidth + ImGui.GetCursorPosX());
                ImGui.TextColored(count > 0 ? _green : _dim, countText);

                totalCount++;
                if (count > 0)
                    usedCount++;
            }

            ImGui.Spacing();
        }

        // Footer
        ImGui.Separator();
        ImGui.TextColored(_dim, $"{usedCount} / {totalCount} spells used this session");
    }
}
