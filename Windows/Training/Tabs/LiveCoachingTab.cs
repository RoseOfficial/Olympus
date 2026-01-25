namespace Olympus.Windows.Training.Tabs;

using System.Linq;
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
    private static readonly Vector4 AdaptiveColor = new(0.5f, 0.8f, 0.9f, 1.0f);

    // Validation outcome colors
    private static readonly Vector4 OptimalColor = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 AcceptableColor = new(0.9f, 0.7f, 0.2f, 1.0f);
    private static readonly Vector4 SuboptimalColor = new(0.9f, 0.3f, 0.3f, 1.0f);

    // Skill level colors
    private static readonly Vector4 BeginnerColor = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Vector4 IntermediateColor = new(0.9f, 0.7f, 0.3f, 1.0f);
    private static readonly Vector4 AdvancedColor = new(0.3f, 0.9f, 0.3f, 1.0f);

    // Current job prefix for skill level lookup (updated from player job)
    private static string currentJobPrefix = "whm";

    /// <summary>
    /// Sets the current job prefix for skill level lookup.
    /// Called from the rotation modules when the job changes.
    /// </summary>
    public static void SetCurrentJob(string jobPrefix)
    {
        if (!string.IsNullOrEmpty(jobPrefix))
        {
            currentJobPrefix = jobPrefix.ToLowerInvariant();
        }
    }

    public static void Draw(ITrainingService trainingService, TrainingConfig config)
    {
        Draw(trainingService, config, null);
    }

    public static void Draw(ITrainingService trainingService, TrainingConfig config, DecisionValidationService? validationService)
    {
        // Combat status with skill level indicator
        DrawCombatStatus(trainingService, config, validationService);
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Current action (if any)
        var current = trainingService.CurrentExplanation;
        if (current != null && IsSectionVisible(config, "CurrentAction"))
        {
            DrawCurrentAction(current, config, trainingService, validationService);
            ImGui.Spacing();
        }

        // Recent history
        if (IsSectionVisible(config, "RecentHistory"))
        {
            DrawRecentHistory(trainingService, config, validationService);
        }
    }

    private static void DrawCombatStatus(ITrainingService trainingService, TrainingConfig config, DecisionValidationService? validationService)
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

            // Show decision validation summary if available
            if (validationService != null)
            {
                var summary = validationService.GetSummary();
                if (summary.TotalDecisions > 0)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text("Validation:");
                    ImGui.TableNextColumn();
                    ImGui.TextColored(OptimalColor, $"{summary.Optimal}");
                    ImGui.SameLine(0, 2);
                    ImGui.TextColored(OptimalColor, "✓");
                    ImGui.SameLine();
                    ImGui.TextColored(AcceptableColor, $"{summary.Acceptable}");
                    ImGui.SameLine(0, 2);
                    ImGui.TextColored(AcceptableColor, "≈");
                    ImGui.SameLine();
                    ImGui.TextColored(SuboptimalColor, $"{summary.Suboptimal}");
                    ImGui.SameLine(0, 2);
                    ImGui.TextColored(SuboptimalColor, "✗");
                    if (ImGui.IsItemHovered())
                    {
                        var optimalRate = validationService.GetOverallOptimalRate();
                        ImGui.SetTooltip($"Optimal decision rate: {optimalRate:P0}\n✓ Optimal | ≈ Acceptable | ✗ Suboptimal");
                    }
                }
            }

            // Show skill level if adaptive explanations are enabled
            if (config.EnableAdaptiveExplanations)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Skill Level:");
                ImGui.TableNextColumn();
                var skillLevel = trainingService.GetSkillLevel(currentJobPrefix);
                var levelColor = skillLevel.Level switch
                {
                    SkillLevel.Beginner => BeginnerColor,
                    SkillLevel.Intermediate => IntermediateColor,
                    SkillLevel.Advanced => AdvancedColor,
                    _ => NeutralColor,
                };
                ImGui.TextColored(levelColor, $"{skillLevel.Level} ({currentJobPrefix.ToUpperInvariant()})");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"Score: {skillLevel.CompositeScore:F0}/100\nExplanation verbosity adapts to your skill level.");
                }
            }

            ImGui.EndTable();
        }
    }

    private static void DrawCurrentAction(ActionExplanation explanation, TrainingConfig config, ITrainingService trainingService, DecisionValidationService? validationService)
    {
        ImGui.Text("Current Decision");

        // Show validation result if available
        var currentValidation = validationService?.CurrentValidation;
        if (currentValidation != null && currentValidation.ActualActionId == explanation.ActionId)
        {
            ImGui.SameLine();
            var validationColor = GetValidationColor(currentValidation.Outcome);
            ImGui.TextColored(validationColor, currentValidation.Symbol);
            if (ImGui.IsItemHovered())
            {
                var tooltip = currentValidation.Outcome switch
                {
                    ValidationOutcome.Optimal => "Optimal decision!",
                    ValidationOutcome.Acceptable => $"Acceptable. {currentValidation.WhatWouldBeBetter}",
                    ValidationOutcome.Suboptimal => $"Suboptimal. {currentValidation.WhatWouldBeBetter}",
                    _ => currentValidation.ShortExplanation,
                };
                ImGui.SetTooltip(tooltip);
            }
        }

        // Show adaptive indicator if verbosity was adjusted
        if (config.EnableAdaptiveExplanations)
        {
            var effectiveVerbosity = trainingService.GetEffectiveVerbosity(explanation, currentJobPrefix);
            if (effectiveVerbosity != config.Verbosity)
            {
                ImGui.SameLine();
                ImGui.TextColored(AdaptiveColor, "[Adaptive]");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip($"Verbosity adjusted from {config.Verbosity} to {effectiveVerbosity} based on your skill level.");
                }
            }
        }

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

        // Get effective verbosity (adaptive or configured)
        var verbosity = config.EnableAdaptiveExplanations
            ? trainingService.GetEffectiveVerbosity(explanation, currentJobPrefix)
            : config.Verbosity;

        // Short reason
        ImGui.TextWrapped(explanation.ShortReason);
        ImGui.Spacing();

        // Show "what would be better" for non-optimal decisions
        if (currentValidation != null && currentValidation.Outcome != ValidationOutcome.Optimal && !string.IsNullOrEmpty(currentValidation.WhatWouldBeBetter))
        {
            var betterColor = currentValidation.Outcome == ValidationOutcome.Acceptable ? AcceptableColor : SuboptimalColor;
            ImGui.TextColored(betterColor, $"Better: {currentValidation.WhatWouldBeBetter}");
            ImGui.Spacing();
        }

        // Decision factors (if normal+ verbosity)
        if (IsSectionVisible(config, "DecisionFactors") && explanation.Factors.Length > 0)
        {
            if (verbosity >= ExplanationVerbosity.Normal)
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
        if (verbosity >= ExplanationVerbosity.Detailed && !string.IsNullOrEmpty(explanation.DetailedReason))
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

    private static void DrawRecentHistory(ITrainingService trainingService, TrainingConfig config, DecisionValidationService? validationService)
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

        // Get validations for lookup
        var validations = validationService?.RecentValidations;

        var columnCount = validationService != null ? 5 : 4;
        if (ImGui.BeginTable("RecentDecisionsTable", columnCount, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.ScrollY, new Vector2(0, 200)))
        {
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 60);
            if (validationService != null)
            {
                ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, 20); // Validation symbol
            }
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

                // Validation symbol (if validation service is available)
                if (validationService != null)
                {
                    ImGui.TableNextColumn();
                    var validation = validations?.FirstOrDefault(v => v.ActualActionId == exp.ActionId && System.Math.Abs((v.Timestamp - exp.Timestamp).TotalSeconds) < 2);
                    if (validation != null)
                    {
                        var validationColor = GetValidationColor(validation.Outcome);
                        ImGui.TextColored(validationColor, validation.Symbol);
                        if (ImGui.IsItemHovered() && !string.IsNullOrEmpty(validation.WhatWouldBeBetter))
                        {
                            ImGui.SetTooltip(validation.WhatWouldBeBetter);
                        }
                    }
                }

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

    private static Vector4 GetValidationColor(ValidationOutcome outcome) => outcome switch
    {
        ValidationOutcome.Optimal => OptimalColor,
        ValidationOutcome.Acceptable => AcceptableColor,
        ValidationOutcome.Suboptimal => SuboptimalColor,
        _ => NeutralColor,
    };

    private static bool IsSectionVisible(TrainingConfig config, string section)
    {
        if (config.SectionVisibility.TryGetValue(section, out var visible))
            return visible;
        return true;
    }
}
