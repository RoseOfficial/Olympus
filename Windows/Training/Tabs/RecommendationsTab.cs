namespace Olympus.Windows.Training.Tabs;

using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;
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
    private static string? generateStatusMessage;
    private static bool generateStatusIsError;

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
            ImGui.TextColored(InfoColor, Loc.T(LocalizedStrings.Training.BasedOnBoth, "Based on fight performance and mastery data:"));
        else if (hasMasteryRecs)
            ImGui.TextColored(InfoColor, Loc.T(LocalizedStrings.Training.BasedOnMastery, "Based on concept mastery data:"));
        else
            ImGui.TextColored(InfoColor, Loc.T(LocalizedStrings.Training.BasedOnPerformance, "Based on your recent fight performance:"));

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
            ImGui.TextColored(NeutralColor, Loc.TFormat(LocalizedStrings.Training.DismissedCountFormat, "{0} dismissed recommendation(s)", config.DismissedRecommendations.Count.ToString()));
            ImGui.SameLine();
            if (ImGui.SmallButton(Loc.T(LocalizedStrings.Training.ClearDismissed, "Clear Dismissed")))
            {
                trainingService.ClearDismissedRecommendations();
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.ClearDismissedTooltip, "Allow dismissed recommendations to appear again."));
            }
        }
    }

    private static void DrawSettings(TrainingConfig config)
    {
        var enabled = config.EnableRecommendations;
        if (ImGui.Checkbox(Loc.T(LocalizedStrings.Training.EnableRecommendations, "Enable Recommendations"), ref enabled))
        {
            config.EnableRecommendations = enabled;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.EnableRecommendationsTooltip, "When enabled, suggests lessons based on fight performance issues."));
        }

        ImGui.SameLine();

        ImGui.SetNextItemWidth(80);
        var max = config.MaxRecommendations;
        if (ImGui.SliderInt(Loc.T(LocalizedStrings.Training.MaxRecommendations, "Max") + "##MaxRec", ref max, 1, 5))
        {
            config.MaxRecommendations = max;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.MaxRecommendationsTooltip, "Maximum number of recommendations to show."));
        }
    }

    private static void DrawEmptyState(TrainingConfig config, ITrainingService trainingService)
    {
        ImGui.Spacing();
        ImGui.Spacing();

        if (!config.EnableRecommendations)
        {
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Training.RecommendationsDisabled, "Recommendations are disabled."));
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Training.EnableAbove, "Enable them above to get personalized lesson suggestions."));
        }
        else
        {
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Training.NoRecommendationsYet, "No recommendations yet."));
            ImGui.Spacing();
            ImGui.TextWrapped(Loc.T(LocalizedStrings.Training.CompleteForRecs, "Complete a fight to receive lesson suggestions based on your performance, or generate suggestions from your mastery data below."));
            ImGui.Spacing();
            ImGui.TextColored(TipColor, Loc.T(LocalizedStrings.Training.RecsTip, "Tip: Recommendations are generated after fights based on detected issues, or from concepts you're struggling with."));

            // Generate from Mastery Data section
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            ImGui.TextColored(InfoColor, Loc.T(LocalizedStrings.Training.GenerateFromMastery, "Generate from Mastery Data"));
            ImGui.Spacing();

            // Job selector
            ImGui.SetNextItemWidth(100);
            if (ImGui.BeginCombo(Loc.T(LocalizedStrings.Training.Job, "Job") + "##MasteryJob", selectedJobPrefix.ToUpperInvariant()))
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

            if (ImGui.Button(Loc.T(LocalizedStrings.Training.Generate, "Generate") + "##FromMastery"))
            {
                if (trainingService.UpdateRecommendationsFromMastery(selectedJobPrefix))
                {
                    generateStatusMessage = null;
                }
                else
                {
                    generateStatusMessage = Loc.T(LocalizedStrings.Training.GenerateNoData, "No mastery data yet — complete some quizzes first.");
                    generateStatusIsError = true;
                }
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.GenerateTooltip, "Generate lesson recommendations based on concepts you're struggling with for this job."));
            }

            if (generateStatusMessage != null)
            {
                ImGui.Spacing();
                var color = generateStatusIsError ? new Vector4(0.9f, 0.5f, 0.3f, 1.0f) : InfoColor;
                ImGui.TextColored(color, generateStatusMessage);
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

        var priorityLabel = rec.PriorityLevel switch
        {
            "HIGH" => Loc.T(LocalizedStrings.Training.PriorityHigh, "HIGH"),
            "MEDIUM" => Loc.T(LocalizedStrings.Training.PriorityMedium, "MEDIUM"),
            _ => Loc.T(LocalizedStrings.Training.PriorityLow, "LOW")
        };

        ImGui.TextColored(priorityColor, $"[{priorityLabel}]");
        ImGui.SameLine();

        // Mastery badge (if mastery-driven)
        if (rec.IsMasteryDriven)
        {
            ImGui.TextColored(MasteryBadgeColor, $"[{Loc.T(LocalizedStrings.Training.MasteryBadge, "MASTERY")}]");
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
            ImGui.TextColored(NeutralColor, Loc.TFormat(LocalizedStrings.Training.IssuesPrefix, "Issues: {0}", issueNames));
        }

        // Struggling concepts (if mastery-driven)
        if (rec.StrugglingConcepts.Length > 0)
        {
            var conceptNames = string.Join(", ", rec.StrugglingConcepts.Select(FormatConceptName));
            ImGui.TextColored(MasteryBadgeColor, Loc.TFormat(LocalizedStrings.Training.StrugglingPrefix, "Struggling: {0}", conceptNames));
        }

        // Action buttons
        ImGui.Spacing();

        // View in Lessons button
        if (ImGui.SmallButton(Loc.T(LocalizedStrings.Training.ViewLesson, "View Lesson") + $"##{rec.Lesson.LessonId}"))
        {
            // Note: This could be enhanced to switch to Lessons tab and select this lesson
            // For now, just mark the lesson as accessed
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Loc.TFormat(LocalizedStrings.Training.LessonTooltipFormat, "Lesson {0}: {1}\n\n{2}", rec.Lesson.LessonNumber.ToString(), rec.Lesson.Title, rec.Lesson.Description));
        }

        ImGui.SameLine();

        // Mark Complete button
        if (ImGui.SmallButton(Loc.T(LocalizedStrings.Training.Complete, "Complete") + $"##{rec.Lesson.LessonId}"))
        {
            trainingService.MarkLessonComplete(rec.Lesson.LessonId);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.CompleteTooltip, "Mark this lesson as completed and remove from recommendations."));
        }

        ImGui.SameLine();

        // Dismiss button
        if (ImGui.SmallButton(Loc.T(LocalizedStrings.Training.Dismiss, "Dismiss") + $"##{rec.Lesson.LessonId}"))
        {
            trainingService.DismissRecommendation(rec.Lesson.LessonId);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.DismissTooltip, "Hide this recommendation. Use 'Clear Dismissed' to show it again."));
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
            Olympus.Services.Analytics.IssueType.PartyDeath => Loc.T(LocalizedStrings.Training.IssueDeaths, "Deaths"),
            Olympus.Services.Analytics.IssueType.NearDeath => Loc.T(LocalizedStrings.Training.IssueNearDeaths, "Near Deaths"),
            Olympus.Services.Analytics.IssueType.AbilityUnused => Loc.T(LocalizedStrings.Training.IssueUnusedAbilities, "Unused Abilities"),
            Olympus.Services.Analytics.IssueType.GcdDowntime => Loc.T(LocalizedStrings.Training.IssueGcdDowntime, "GCD Downtime"),
            Olympus.Services.Analytics.IssueType.CooldownDrift => Loc.T(LocalizedStrings.Training.IssueCooldownDrift, "Cooldown Drift"),
            Olympus.Services.Analytics.IssueType.HighOverheal => Loc.T(LocalizedStrings.Training.IssueHighOverheal, "High Overheal"),
            Olympus.Services.Analytics.IssueType.ResourceCapped => Loc.T(LocalizedStrings.Training.IssueCappedResources, "Capped Resources"),
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
