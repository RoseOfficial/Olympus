namespace Olympus.Windows.Training.Tabs;

using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Services.Training;

/// <summary>
/// Recommendations tab: displays personalized lesson suggestions based on fight performance.
/// </summary>
public static class RecommendationsTab
{
    // Colors for UI elements
    private static readonly Vector4 HighPriorityColor = new(0.9f, 0.3f, 0.3f, 1.0f);
    private static readonly Vector4 MediumPriorityColor = new(0.9f, 0.7f, 0.3f, 1.0f);
    private static readonly Vector4 LowPriorityColor = new(0.5f, 0.7f, 0.9f, 1.0f);
    private static readonly Vector4 NeutralColor = new(0.7f, 0.7f, 0.7f, 1.0f);
    private static readonly Vector4 InfoColor = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Vector4 TipColor = new(0.6f, 0.4f, 0.9f, 1.0f);
    private static readonly Vector4 MasteryBadgeColor = new(0.4f, 0.8f, 0.6f, 1.0f);

    // Track selected job for mastery-based generation
    private static string selectedJobPrefix = "whm";

    public static void Draw(ITrainingService trainingService, TrainingConfig config)
    {
        // Settings row
        DrawSettings(config);

        ImGui.Separator();
        ImGui.Spacing();

        // Get recommendations
        var recommendations = trainingService.GetRecommendations();

        if (recommendations.Count == 0)
        {
            DrawEmptyState(config, trainingService);
            return;
        }

        // Display header based on recommendation sources
        var hasMasteryRecs = recommendations.Any(r => r.IsMasteryDriven);
        var hasIssueRecs = recommendations.Any(r => r.TriggeringIssues.Length > 0);

        if (hasMasteryRecs && hasIssueRecs)
            ImGui.TextColored(InfoColor, "Based on fight performance and mastery data:");
        else if (hasMasteryRecs)
            ImGui.TextColored(InfoColor, "Based on concept mastery data:");
        else
            ImGui.TextColored(InfoColor, "Based on your recent fight performance:");

        ImGui.Spacing();

        foreach (var rec in recommendations)
        {
            DrawRecommendation(rec, trainingService, config);
            ImGui.Spacing();
        }

        // Footer
        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Clear dismissed button
        if (config.DismissedRecommendations.Count > 0)
        {
            ImGui.TextColored(NeutralColor, $"{config.DismissedRecommendations.Count} dismissed recommendation(s)");
            ImGui.SameLine();
            if (ImGui.SmallButton("Clear Dismissed"))
            {
                trainingService.ClearDismissedRecommendations();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Allow dismissed recommendations to appear again.");
            }
        }
    }

    private static void DrawSettings(TrainingConfig config)
    {
        var enabled = config.EnableRecommendations;
        if (ImGui.Checkbox("Enable Recommendations", ref enabled))
        {
            config.EnableRecommendations = enabled;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("When enabled, suggests lessons based on fight performance issues.");
        }

        ImGui.SameLine();

        ImGui.SetNextItemWidth(80);
        var max = config.MaxRecommendations;
        if (ImGui.SliderInt("Max##MaxRec", ref max, 1, 5))
        {
            config.MaxRecommendations = max;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Maximum number of recommendations to show.");
        }
    }

    private static void DrawEmptyState(TrainingConfig config, ITrainingService trainingService)
    {
        ImGui.Spacing();
        ImGui.Spacing();

        if (!config.EnableRecommendations)
        {
            ImGui.TextColored(NeutralColor, "Recommendations are disabled.");
            ImGui.TextColored(NeutralColor, "Enable them above to get personalized lesson suggestions.");
        }
        else
        {
            ImGui.TextColored(NeutralColor, "No recommendations yet.");
            ImGui.Spacing();
            ImGui.TextWrapped("Complete a fight to receive lesson suggestions based on your performance, or generate suggestions from your mastery data below.");
            ImGui.Spacing();
            ImGui.TextColored(TipColor, "Tip: Recommendations are generated after fights based on detected issues, or from concepts you're struggling with.");

            // Generate from Mastery Data section
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(InfoColor, "Generate from Mastery Data");
            ImGui.Spacing();

            // Job selector
            ImGui.SetNextItemWidth(100);
            if (ImGui.BeginCombo("Job##MasteryJob", selectedJobPrefix.ToUpperInvariant()))
            {
                foreach (var job in new[] { "whm", "sch", "ast", "sge", "pld", "war", "drk", "gnb", "drg", "nin", "sam", "mnk", "rpr", "vpr", "mch", "brd", "dnc", "blm", "smn", "rdm", "pct" })
                {
                    if (ImGui.Selectable(job.ToUpperInvariant(), selectedJobPrefix == job))
                    {
                        selectedJobPrefix = job;
                    }
                }

                ImGui.EndCombo();
            }

            ImGui.SameLine();

            if (ImGui.Button("Generate##FromMastery"))
            {
                trainingService.UpdateRecommendationsFromMastery(selectedJobPrefix);
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("Generate lesson recommendations based on concepts you're struggling with for this job.");
            }
        }
    }

    private static void DrawRecommendation(LessonRecommendation rec, ITrainingService trainingService, TrainingConfig config)
    {
        // Priority badge
        var priorityColor = rec.PriorityLevel switch
        {
            "HIGH" => HighPriorityColor,
            "MEDIUM" => MediumPriorityColor,
            _ => LowPriorityColor
        };

        ImGui.TextColored(priorityColor, $"[{rec.PriorityLevel}]");
        ImGui.SameLine();

        // Mastery badge (if mastery-driven)
        if (rec.IsMasteryDriven)
        {
            ImGui.TextColored(MasteryBadgeColor, "[MASTERY]");
            ImGui.SameLine();
        }

        // Lesson title (as selectable)
        ImGui.TextColored(InfoColor, rec.Lesson.Title);

        // Job badge
        ImGui.SameLine();
        ImGui.TextColored(NeutralColor, $"({rec.Lesson.JobPrefix.ToUpperInvariant()})");

        // Reason
        ImGui.Indent();
        ImGui.TextWrapped(rec.Reason);

        // Triggering issues (if any)
        if (rec.TriggeringIssues.Length > 0)
        {
            var issueNames = string.Join(", ", rec.TriggeringIssues.Select(FormatIssueType));
            ImGui.TextColored(NeutralColor, $"Issues: {issueNames}");
        }

        // Struggling concepts (if mastery-driven)
        if (rec.StrugglingConcepts.Length > 0)
        {
            var conceptNames = string.Join(", ", rec.StrugglingConcepts.Select(FormatConceptName));
            ImGui.TextColored(MasteryBadgeColor, $"Struggling: {conceptNames}");
        }

        // Action buttons
        ImGui.Spacing();

        // View in Lessons button
        if (ImGui.SmallButton($"View Lesson##{rec.Lesson.LessonId}"))
        {
            // Note: This could be enhanced to switch to Lessons tab and select this lesson
            // For now, just mark the lesson as accessed
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip($"Lesson {rec.Lesson.LessonNumber}: {rec.Lesson.Title}\n\n{rec.Lesson.Description}");
        }

        ImGui.SameLine();

        // Mark Complete button
        if (ImGui.SmallButton($"Complete##{rec.Lesson.LessonId}"))
        {
            trainingService.MarkLessonComplete(rec.Lesson.LessonId);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Mark this lesson as completed and remove from recommendations.");
        }

        ImGui.SameLine();

        // Dismiss button
        if (ImGui.SmallButton($"Dismiss##{rec.Lesson.LessonId}"))
        {
            trainingService.DismissRecommendation(rec.Lesson.LessonId);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip("Hide this recommendation. Use 'Clear Dismissed' to show it again.");
        }

        ImGui.Unindent();

        // Separator between recommendations
        ImGui.Spacing();
        ImGui.Separator();
    }

    private static string FormatIssueType(Olympus.Services.Analytics.IssueType issueType)
    {
        return issueType switch
        {
            Olympus.Services.Analytics.IssueType.PartyDeath => "Deaths",
            Olympus.Services.Analytics.IssueType.NearDeath => "Near Deaths",
            Olympus.Services.Analytics.IssueType.AbilityUnused => "Unused Abilities",
            Olympus.Services.Analytics.IssueType.GcdDowntime => "GCD Downtime",
            Olympus.Services.Analytics.IssueType.CooldownDrift => "Cooldown Drift",
            Olympus.Services.Analytics.IssueType.HighOverheal => "High Overheal",
            Olympus.Services.Analytics.IssueType.ResourceCapped => "Capped Resources",
            _ => issueType.ToString()
        };
    }

    private static string FormatConceptName(string conceptId)
    {
        var parts = conceptId.Split('.');
        if (parts.Length > 1)
        {
            var name = parts[^1].Replace("_", " ");
            return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
        }

        return conceptId;
    }
}
