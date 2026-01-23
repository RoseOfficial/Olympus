namespace Olympus.Windows.Training.Tabs;

using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Services.Training;

/// <summary>
/// Live Coaching tab: real-time decision explanations during combat.
/// </summary>
public static class LiveCoachingTab
{
    // Colors for UI elements
    private static readonly Vector4 GoodColor = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 WarningColor = new(0.9f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 BadColor = new(0.9f, 0.3f, 0.3f, 1.0f);
    private static readonly Vector4 NeutralColor = new(0.7f, 0.7f, 0.7f, 1.0f);
    private static readonly Vector4 InfoColor = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Vector4 HealingColor = new(0.4f, 0.9f, 0.4f, 1.0f);
    private static readonly Vector4 DamageColor = new(0.9f, 0.4f, 0.3f, 1.0f);
    private static readonly Vector4 DefensiveColor = new(0.4f, 0.6f, 0.9f, 1.0f);
    private static readonly Vector4 UtilityColor = new(0.9f, 0.7f, 0.3f, 1.0f);
    private static readonly Vector4 TipColor = new(0.6f, 0.4f, 0.9f, 1.0f);

    public static void Draw(ITrainingService trainingService, TrainingConfig config)
    {
        // Combat status
        DrawCombatStatus(trainingService);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Current action (if any)
        var current = trainingService.CurrentExplanation;
        if (current != null && IsSectionVisible(config, "CurrentAction"))
        {
            DrawCurrentAction(current, config);
            ImGui.Spacing();
        }

        // Recent history
        if (IsSectionVisible(config, "RecentHistory"))
        {
            DrawRecentHistory(trainingService, config);
        }
    }

    private static void DrawCombatStatus(ITrainingService trainingService)
    {
        if (ImGui.BeginTable("TrainingStatusTable", 2, ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Status:");
            ImGui.TableNextColumn();
            if (trainingService.IsInCombat)
            {
                ImGui.TextColored(GoodColor, "IN COMBAT - COACHING ACTIVE");
            }
            else
            {
                ImGui.TextColored(NeutralColor, "Waiting for combat...");
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Decisions:");
            ImGui.TableNextColumn();
            ImGui.Text($"{trainingService.RecentExplanations.Count} captured");

            ImGui.EndTable();
        }
    }

    private static void DrawCurrentAction(ActionExplanation explanation, TrainingConfig config)
    {
        ImGui.Text("Current Decision");
        ImGui.Separator();

        // Action name with category color
        var categoryColor = GetCategoryColor(explanation.Category);
        ImGui.TextColored(categoryColor, $"[{explanation.Category}]");
        ImGui.SameLine();
        ImGui.TextColored(InfoColor, explanation.ActionName);
        if (!string.IsNullOrEmpty(explanation.TargetName))
        {
            ImGui.SameLine();
            ImGui.TextColored(NeutralColor, $"-> {explanation.TargetName}");
        }

        ImGui.Spacing();

        // Short reason
        ImGui.TextWrapped(explanation.ShortReason);
        ImGui.Spacing();

        // Decision factors (if detailed verbosity)
        if (IsSectionVisible(config, "DecisionFactors") && explanation.Factors.Length > 0)
        {
            if (config.Verbosity >= ExplanationVerbosity.Normal)
            {
                ImGui.TextColored(NeutralColor, "Decision Factors:");
                foreach (var factor in explanation.Factors)
                {
                    ImGui.TextColored(NeutralColor, $"  - {factor}");
                }
                ImGui.Spacing();
            }
        }

        // Detailed reason (if detailed verbosity)
        if (config.Verbosity >= ExplanationVerbosity.Detailed && !string.IsNullOrEmpty(explanation.DetailedReason))
        {
            ImGui.TextColored(NeutralColor, "Details:");
            ImGui.TextWrapped(explanation.DetailedReason);
            ImGui.Spacing();
        }

        // Alternatives
        if (config.ShowAlternatives && IsSectionVisible(config, "Alternatives") && explanation.Alternatives.Length > 0)
        {
            ImGui.TextColored(WarningColor, "Alternatives Considered:");
            foreach (var alt in explanation.Alternatives)
            {
                ImGui.TextColored(WarningColor, $"  - {alt}");
            }
            ImGui.Spacing();
        }

        // Learning tip
        if (config.ShowTips && IsSectionVisible(config, "Tips") && !string.IsNullOrEmpty(explanation.Tip))
        {
            ImGui.TextColored(TipColor, $"Tip: {explanation.Tip}");
        }
    }

    private static void DrawRecentHistory(ITrainingService trainingService, TrainingConfig config)
    {
        ImGui.Text("Recent Decisions");
        ImGui.Separator();

        var explanations = trainingService.RecentExplanations;
        if (explanations.Count == 0)
        {
            ImGui.TextColored(NeutralColor, "No decisions yet. Enter combat to see explanations.");
            return;
        }

        // Skip first (current) if we already showed it
        var startIndex = IsSectionVisible(config, "CurrentAction") ? 1 : 0;
        if (startIndex >= explanations.Count)
        {
            ImGui.TextColored(NeutralColor, "Waiting for more decisions...");
            return;
        }

        if (ImGui.BeginTable("RecentDecisionsTable", 4, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY, new Vector2(0, 200)))
        {
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 70);
            ImGui.TableSetupColumn("Action", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Reason", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupScrollFreeze(0, 1);
            ImGui.TableHeadersRow();

            for (var i = startIndex; i < explanations.Count; i++)
            {
                var exp = explanations[i];
                ImGui.TableNextRow();

                // Time
                ImGui.TableNextColumn();
                var elapsed = System.DateTime.Now - exp.Timestamp;
                var timeStr = elapsed.TotalSeconds < 60 ? $"{(int)elapsed.TotalSeconds}s ago" : $"{(int)elapsed.TotalMinutes}m ago";
                ImGui.TextColored(NeutralColor, timeStr);

                // Category
                ImGui.TableNextColumn();
                ImGui.TextColored(GetCategoryColor(exp.Category), exp.Category);

                // Action
                ImGui.TableNextColumn();
                ImGui.Text(exp.ActionName);

                // Reason
                ImGui.TableNextColumn();
                ImGui.TextWrapped(exp.ShortReason);
            }

            ImGui.EndTable();
        }
    }

    private static Vector4 GetCategoryColor(string category) => category.ToLowerInvariant() switch
    {
        "healing" or "emergency healing" or "aoe healing" => HealingColor,
        "damage" or "dps" => DamageColor,
        "defensive" or "mitigation" => DefensiveColor,
        "utility" or "buff" => UtilityColor,
        _ => NeutralColor,
    };

    private static bool IsSectionVisible(TrainingConfig config, string section)
    {
        if (config.SectionVisibility.TryGetValue(section, out var visible))
            return visible;
        return true;
    }
}
