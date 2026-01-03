using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Models;
using Olympus.Services.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Actions tab: action history, spell usage, GCD state details.
/// </summary>
public static class ActionsTab
{
    // Filter state (persists across frames)
    private static bool _showSuccess = true;
    private static bool _showFailures = true;
    private static bool _showSkips = true;

    public static void Draw(DebugSnapshot snapshot, Configuration config, DebugService debugService)
    {
        // GCD State Details Section
        if (IsSectionVisible(config, "GcdDetails"))
        {
            DrawGcdStateDetails(snapshot);
            ImGui.Spacing();
        }

        // Spell Usage Section
        if (IsSectionVisible(config, "SpellUsage"))
        {
            DrawSpellUsage(snapshot);
            ImGui.Spacing();
        }

        // Action History Section
        if (IsSectionVisible(config, "ActionHistory"))
        {
            DrawFilters(debugService);
            ImGui.Separator();
            DrawActionHistory(snapshot);
        }
    }

    private static void DrawGcdStateDetails(DebugSnapshot snapshot)
    {
        ImGui.Text("GCD State Details");
        ImGui.Separator();

        var gcd = snapshot.GcdState;

        if (ImGui.BeginTable("GcdDetailsTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Property", ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            DrawTableRow("Current State:", gcd.State.ToString(), DebugColors.GetGcdStateColor(gcd.State));
            DrawTableRow("GCD Remaining:", $"{gcd.GcdRemaining:F3}s");
            DrawTableRow("Animation Lock:", $"{gcd.AnimationLockRemaining:F3}s");
            DrawTableRow("Is Casting:", gcd.IsCasting ? "Yes" : "No");
            DrawTableRow("Can Execute GCD:", gcd.CanExecuteGcd ? "Yes" : "No", gcd.CanExecuteGcd ? DebugColors.Success : DebugColors.Dim);
            DrawTableRow("Can Execute oGCD:", gcd.CanExecuteOgcd ? "Yes" : "No", gcd.CanExecuteOgcd ? DebugColors.Heal : DebugColors.Dim);
            DrawTableRow("Weave Slots:", gcd.WeaveSlots.ToString());
            DrawTableRow("Last Action:", gcd.LastActionName);

            // Debug flags from ActionTracker
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextColored(DebugColors.Dim, "--- Debug Flags ---");
            ImGui.TableNextColumn();

            var readyColor = gcd.DebugGcdReady ? DebugColors.Failure : DebugColors.Success;
            DrawTableRow("GCD Ready (downtime):", gcd.DebugGcdReady ? "YES" : "No", readyColor);
            DrawTableRow("Is Active:", gcd.DebugIsActive ? "Yes" : "No");

            ImGui.EndTable();
        }
    }

    private static void DrawTableRow(string label, string value, Vector4? color = null)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(label);
        ImGui.TableNextColumn();
        if (color.HasValue)
            ImGui.TextColored(color.Value, value);
        else
            ImGui.Text(value);
    }

    private static void DrawSpellUsage(DebugSnapshot snapshot)
    {
        ImGui.Text("Spell Usage");
        ImGui.Separator();

        var actions = snapshot.Actions;

        if (actions.SpellUsage.Count == 0)
        {
            ImGui.TextColored(DebugColors.Dim, "No spells cast yet");
            return;
        }

        // Calculate columns based on available width
        var columnWidth = 160f;
        var columns = (int)(ImGui.GetContentRegionAvail().X / columnWidth);
        if (columns < 1) columns = 1;

        var i = 0;
        foreach (var spell in actions.SpellUsage)
        {
            if (i > 0 && i % columns != 0)
                ImGui.SameLine(columnWidth * (i % columns));

            ImGui.TextColored(DebugColors.Success, $"{spell.Count}");
            ImGui.SameLine();
            ImGui.Text(spell.Name);

            i++;
        }
    }

    private static void DrawFilters(DebugService debugService)
    {
        ImGui.Text("Filters:");
        ImGui.SameLine();
        ImGui.Checkbox("Success", ref _showSuccess);
        ImGui.SameLine();
        ImGui.Checkbox("Failures", ref _showFailures);
        ImGui.SameLine();
        ImGui.Checkbox("Skips", ref _showSkips);

        ImGui.SameLine(ImGui.GetContentRegionAvail().X - 100);
        if (ImGui.Button("Clear"))
        {
            debugService.ClearHistory();
        }
    }

    private static void DrawActionHistory(DebugSnapshot snapshot)
    {
        ImGui.Text("Action History");

        var childSize = new Vector2(0, -1);
        if (!ImGui.BeginChild("ActionLog", childSize, true, ImGuiWindowFlags.HorizontalScrollbar))
            return;

        var history = snapshot.Actions.History;

        foreach (var attempt in history)
        {
            if (!ShouldShowAttempt(attempt))
                continue;

            DrawAttemptLine(attempt);
        }

        ImGui.EndChild();
    }

    private static bool ShouldShowAttempt(ActionAttempt attempt)
    {
        return attempt.Result switch
        {
            ActionResult.Success => _showSuccess,
            ActionResult.NoTarget => _showSkips,
            ActionResult.ActionNotReady or ActionResult.OnCooldown => _showSkips,
            _ => _showFailures
        };
    }

    private static void DrawAttemptLine(ActionAttempt attempt)
    {
        var color = DebugColors.GetResultColor(attempt.Result);
        var timestamp = attempt.Timestamp.ToString("HH:mm:ss.fff");
        var resultIcon = DebugColors.GetResultIcon(attempt.Result);

        // Timestamp
        ImGui.TextColored(DebugColors.Dim, timestamp);
        ImGui.SameLine();

        // Result icon and spell name
        ImGui.TextColored(color, $"{resultIcon} [{attempt.SpellName}]");
        ImGui.SameLine();

        // Target or failure reason
        if (attempt.Result == ActionResult.Success)
        {
            ImGui.TextColored(color, $"-> {attempt.TargetName} (HP: {attempt.TargetHp})");

            // Show time since last cast if significant
            if (attempt.TimeSinceLastCast > 0.1f)
            {
                ImGui.SameLine();
                var gapColor = attempt.TimeSinceLastCast > 3.0f ? DebugColors.Failure : DebugColors.Dim;
                ImGui.TextColored(gapColor, $"[+{attempt.TimeSinceLastCast:F2}s]");
            }
        }
        else
        {
            ImGui.TextColored(color, attempt.FailureReason ?? attempt.Result.ToString());

            // Show status code if available
            if (attempt.StatusCode.HasValue && attempt.StatusCode.Value != 0)
            {
                ImGui.SameLine();
                ImGui.TextColored(DebugColors.Dim, $"(code: {attempt.StatusCode})");
            }
        }
    }

    private static bool IsSectionVisible(Configuration config, string section)
    {
        if (config.DebugSectionVisibility.TryGetValue(section, out var visible))
            return visible;
        return true;
    }
}
