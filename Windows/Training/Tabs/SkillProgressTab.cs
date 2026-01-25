namespace Olympus.Windows.Training.Tabs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Services.Training;

/// <summary>
/// Skill Progress tab: displays skill level detection results and adaptive explanation controls.
/// </summary>
public static class SkillProgressTab
{
    // Colors for UI elements
    private static readonly Vector4 GoodColor = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 WarningColor = new(0.9f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 NeutralColor = new(0.7f, 0.7f, 0.7f, 1.0f);
    private static readonly Vector4 InfoColor = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Vector4 UrgentColor = new(0.9f, 0.4f, 0.3f, 1.0f);

    // Skill level colors
    private static readonly Vector4 BeginnerColor = new(0.4f, 0.7f, 1.0f, 1.0f);
    private static readonly Vector4 IntermediateColor = new(0.9f, 0.7f, 0.3f, 1.0f);
    private static readonly Vector4 AdvancedColor = new(0.3f, 0.9f, 0.3f, 1.0f);

    // State for lesson navigation
    private static string? pendingLessonNavigation;

    // All supported job prefixes
    private static readonly string[] AllJobPrefixes =
    {
        // Healers
        "whm", "sch", "ast", "sge",
        // Tanks
        "pld", "war", "drk", "gnb",
        // Melee DPS
        "drg", "nin", "sam", "mnk", "rpr", "vpr",
        // Ranged Physical DPS
        "mch", "brd", "dnc",
        // Casters
        "blm", "smn", "rdm", "pct",
    };

    private static readonly string[] JobDisplayNames =
    {
        // Healers
        "White Mage", "Scholar", "Astrologian", "Sage",
        // Tanks
        "Paladin", "Warrior", "Dark Knight", "Gunbreaker",
        // Melee DPS
        "Dragoon", "Ninja", "Samurai", "Monk", "Reaper", "Viper",
        // Ranged Physical DPS
        "Machinist", "Bard", "Dancer",
        // Casters
        "Black Mage", "Summoner", "Red Mage", "Pictomancer",
    };

    /// <summary>
    /// Gets the pending lesson navigation request (set by "Study This" button).
    /// Called by TrainingWindow to switch tabs.
    /// </summary>
    public static string? GetPendingLessonNavigation()
    {
        var lesson = pendingLessonNavigation;
        pendingLessonNavigation = null;
        return lesson;
    }

    public static void Draw(ITrainingService trainingService, TrainingConfig config)
    {
        // Adaptive explanations toggle
        DrawAdaptiveSettings(config);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Focus Areas summary (v3.29.0) - show struggling/mastered concepts prominently
        if (config.EnableAdaptiveExplanations)
        {
            DrawFocusAreas(trainingService, config);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

        // Per-job skill level display
        DrawJobSkillLevels(trainingService, config);
    }

    private static void DrawFocusAreas(ITrainingService trainingService, TrainingConfig config)
    {
        // Gather struggling and mastered concepts across all jobs with progress
        var strugglingByJob = new List<(string JobPrefix, string JobName, string ConceptId, float SuccessRate)>();
        var masteredByJob = new List<(string JobPrefix, string JobName, string ConceptId, float SuccessRate)>();

        for (var i = 0; i < AllJobPrefixes.Length; i++)
        {
            var prefix = AllJobPrefixes[i];
            var name = JobDisplayNames[i];
            var mastery = trainingService.GetConceptMastery(prefix);

            // Get mastery data with success rates
            foreach (var conceptId in mastery.StrugglingConcepts)
            {
                var rate = GetConceptSuccessRate(config, conceptId);
                strugglingByJob.Add((prefix, name, conceptId, rate));
            }

            foreach (var conceptId in mastery.MasteredConcepts)
            {
                var rate = GetConceptSuccessRate(config, conceptId);
                masteredByJob.Add((prefix, name, conceptId, rate));
            }
        }

        // Sort struggling by worst success rate first
        strugglingByJob = strugglingByJob.OrderBy(x => x.SuccessRate).ToList();

        // Sort mastered by most recent (highest rate first as proxy)
        masteredByJob = masteredByJob.OrderByDescending(x => x.SuccessRate).ToList();

        // Draw Focus Areas section
        if (strugglingByJob.Count > 0)
        {
            ImGui.TextColored(UrgentColor, $"\u26A0 Focus Areas ({strugglingByJob.Count} concepts need practice)");

            // Show top 5 struggling concepts
            foreach (var (jobPrefix, jobName, conceptId, rate) in strugglingByJob.Take(5))
            {
                ImGui.TextColored(WarningColor, $"  \u251C\u2500 {jobPrefix.ToUpperInvariant()}: {FormatConceptName(conceptId)} ({rate:P0} success)");
                ImGui.SameLine();
                if (ImGui.SmallButton($"Study##{conceptId}"))
                {
                    // Find a lesson that covers this concept
                    var lessons = LessonRegistry.GetLessonsForJob(jobPrefix);
                    var lesson = lessons.FirstOrDefault(l => l.ConceptsCovered.Contains(conceptId));
                    if (lesson != null)
                    {
                        pendingLessonNavigation = lesson.LessonId;
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Open the lesson covering this concept");
                }
            }

            if (strugglingByJob.Count > 5)
            {
                ImGui.TextColored(NeutralColor, $"  \u2514\u2500 ... and {strugglingByJob.Count - 5} more");
            }

            ImGui.Spacing();
        }

        // Draw Recently Mastered section
        if (masteredByJob.Count > 0)
        {
            ImGui.TextColored(GoodColor, $"\u2713 Recently Mastered ({masteredByJob.Count} concepts)");

            // Show top 3 mastered concepts
            foreach (var (jobPrefix, jobName, conceptId, rate) in masteredByJob.Take(3))
            {
                ImGui.TextColored(GoodColor, $"  \u251C\u2500 {jobPrefix.ToUpperInvariant()}: {FormatConceptName(conceptId)} ({rate:P0} success)");
            }

            if (masteredByJob.Count > 3)
            {
                ImGui.TextColored(NeutralColor, $"  \u2514\u2500 ... and {masteredByJob.Count - 3} more");
            }
        }

        if (strugglingByJob.Count == 0 && masteredByJob.Count == 0)
        {
            ImGui.TextColored(NeutralColor, "Play with Training Mode enabled to build mastery data.");
        }
    }

    private static float GetConceptSuccessRate(TrainingConfig config, string conceptId)
    {
        if (config.ConceptMastery.TryGetValue(conceptId, out var data) && data.Opportunities > 0)
        {
            return data.SuccessRate;
        }

        return 0f;
    }

    private static void DrawAdaptiveSettings(TrainingConfig config)
    {
        ImGui.Text("Adaptive Explanations");
        ImGui.Separator();

        var enableAdaptive = config.EnableAdaptiveExplanations;
        if (ImGui.Checkbox("Enable Adaptive Verbosity", ref enableAdaptive))
        {
            config.EnableAdaptiveExplanations = enableAdaptive;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(
                "When enabled, explanation verbosity automatically adjusts based on your skill level:\n" +
                "- Beginners see detailed explanations for all decisions\n" +
                "- Intermediate players see normal detail, with extra detail for unfamiliar concepts\n" +
                "- Advanced players see minimal detail, except for new or critical decisions");
        }

        if (config.EnableAdaptiveExplanations)
        {
            ImGui.Spacing();

            // Override dropdown
            ImGui.Text("Skill Level Override:");
            ImGui.SameLine();

            var overrideOptions = new[] { "Auto-detect", "Beginner", "Intermediate", "Advanced" };
            var currentOverride = config.SkillLevelOverride.HasValue
                ? (int)config.SkillLevelOverride.Value + 1
                : 0;

            ImGui.SetNextItemWidth(150);
            if (ImGui.Combo("##SkillOverride", ref currentOverride, overrideOptions, overrideOptions.Length))
            {
                config.SkillLevelOverride = currentOverride switch
                {
                    1 => SkillLevelOverride.Beginner,
                    2 => SkillLevelOverride.Intermediate,
                    3 => SkillLevelOverride.Advanced,
                    _ => null,
                };
            }

            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(
                    "Override the detected skill level:\n" +
                    "- Auto-detect: Uses quiz and lesson progress to determine level\n" +
                    "- Beginner/Intermediate/Advanced: Force a specific level");
            }
        }
    }

    private static void DrawJobSkillLevels(ITrainingService trainingService, TrainingConfig config)
    {
        ImGui.Text("Skill Levels by Job");
        ImGui.Separator();

        if (!config.EnableAdaptiveExplanations)
        {
            ImGui.TextColored(NeutralColor, "Enable adaptive explanations above to see skill levels.");
            return;
        }

        // Find jobs with any progress (including mastery data)
        var jobsWithProgress = AllJobPrefixes
            .Select((prefix, index) => (
                Prefix: prefix,
                Name: JobDisplayNames[index],
                Result: trainingService.GetSkillLevel(prefix),
                Mastery: trainingService.GetConceptMastery(prefix)))
            .Where(j => j.Result.PassedQuizzes > 0 || j.Result.CompletedLessonsCount > 0 || j.Mastery.TotalConcepts > 0)
            .ToArray();

        if (jobsWithProgress.Length == 0)
        {
            ImGui.TextColored(NeutralColor, "No progress yet. Complete lessons and quizzes to see your skill levels.");
            ImGui.Spacing();
            ImGui.TextColored(NeutralColor, "Go to the Lessons tab to get started!");
            return;
        }

        // Display each job with progress
        foreach (var job in jobsWithProgress)
        {
            DrawJobSkillLevel(job.Prefix, job.Name, job.Result, job.Mastery, config);
            ImGui.Spacing();
        }

        // Show hint for other jobs
        var jobsWithoutProgress = AllJobPrefixes.Length - jobsWithProgress.Length;
        if (jobsWithoutProgress > 0)
        {
            ImGui.Spacing();
            ImGui.TextColored(NeutralColor, $"{jobsWithoutProgress} other jobs with no progress yet.");
        }
    }

    private static void DrawJobSkillLevel(string jobPrefix, string jobName, SkillLevelResult result, ConceptMasteryResult mastery, TrainingConfig config)
    {
        // Job header with level badge
        var levelColor = result.Level switch
        {
            SkillLevel.Beginner => BeginnerColor,
            SkillLevel.Intermediate => IntermediateColor,
            SkillLevel.Advanced => AdvancedColor,
            _ => NeutralColor,
        };

        if (ImGui.TreeNode($"{jobName} ({jobPrefix.ToUpperInvariant()})##{jobPrefix}"))
        {
            // Skill level and score
            ImGui.TextColored(levelColor, $"Level: {result.Level}");
            ImGui.SameLine();
            ImGui.TextColored(NeutralColor, $"(Score: {result.CompositeScore:F0}/100)");

            // Score breakdown
            ImGui.Spacing();
            ImGui.Text("Score Breakdown:");

            // Quiz pass rate (30%)
            DrawScoreComponent("Quiz Pass Rate", result.QuizPassRate, 30,
                $"{result.PassedQuizzes}/{result.TotalQuizzes} quizzes passed");

            // Quiz quality (20%)
            DrawScoreComponent("Quiz Quality", result.QuizQuality, 20,
                "Average score on passed quizzes");

            // Lessons completed (20%)
            DrawScoreComponent("Lessons Completed", result.LessonsCompleted, 20,
                $"{result.CompletedLessonsCount}/{result.TotalLessons} lessons done");

            // Concepts learned (5%)
            DrawScoreComponent("Concepts Learned", result.ConceptsLearned, 5,
                "Marked as learned");

            // Concept Mastery (25%) - NEW in v3.28.0
            DrawScoreComponent("Concept Mastery", result.ConceptMastery, 25,
                "Success rate when applying concepts in combat");

            // Engagement penalty warning
            if (result.EngagementPenaltyApplied)
            {
                ImGui.Spacing();
                ImGui.TextColored(WarningColor, "Note: -25% penalty applied (lessons completed without quizzes)");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Take quizzes to validate your understanding and remove this penalty.");
                }
            }

            // Concept Mastery Details (v3.28.0, enhanced in v3.29.0)
            DrawMasteryDetails(mastery, jobPrefix, config);

            ImGui.TreePop();
        }
        else
        {
            // Compact display when collapsed - now includes struggling indicator (v3.29.0)
            ImGui.SameLine();
            ImGui.TextColored(levelColor, $"[{result.Level}]");
            ImGui.SameLine();
            ImGui.TextColored(NeutralColor, $"Score: {result.CompositeScore:F0}");

            // Show struggling count if any (v3.29.0)
            if (mastery.StrugglingConcepts.Length > 0)
            {
                ImGui.SameLine();
                ImGui.TextColored(WarningColor, $"\u26A0{mastery.StrugglingConcepts.Length} struggling");
            }
            else if (mastery.MasteredConcepts.Length > 0)
            {
                ImGui.SameLine();
                ImGui.TextColored(GoodColor, $"\u2713{mastery.MasteredConcepts.Length} mastered");
            }
        }
    }

    private static void DrawMasteryDetails(ConceptMasteryResult mastery, string jobPrefix, TrainingConfig config)
    {
        if (mastery.TotalConcepts == 0)
            return;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Text("Concept Mastery Details:");

        // Mastered concepts
        if (mastery.MasteredConcepts.Length > 0)
        {
            ImGui.TextColored(GoodColor, $"  Mastered ({mastery.MasteredConcepts.Length}):");
            foreach (var concept in mastery.MasteredConcepts.Take(5))
            {
                var rate = GetConceptSuccessRate(config, concept);
                ImGui.TextColored(GoodColor, $"    \u2713 {FormatConceptName(concept)} ({rate:P0})");
            }

            if (mastery.MasteredConcepts.Length > 5)
            {
                ImGui.TextColored(NeutralColor, $"    ... and {mastery.MasteredConcepts.Length - 5} more");
            }
        }

        // Struggling concepts with "Study This" button (v3.29.0)
        if (mastery.StrugglingConcepts.Length > 0)
        {
            ImGui.TextColored(WarningColor, $"  Needs Practice ({mastery.StrugglingConcepts.Length}):");
            foreach (var concept in mastery.StrugglingConcepts.Take(5))
            {
                var rate = GetConceptSuccessRate(config, concept);
                ImGui.TextColored(WarningColor, $"    \u26A0 {FormatConceptName(concept)} ({rate:P0})");
                ImGui.SameLine();
                if (ImGui.SmallButton($"Study This##{concept}"))
                {
                    // Find a lesson that covers this concept
                    var lessons = LessonRegistry.GetLessonsForJob(jobPrefix);
                    var lesson = lessons.FirstOrDefault(l => l.ConceptsCovered.Contains(concept));
                    if (lesson != null)
                    {
                        pendingLessonNavigation = lesson.LessonId;
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Open the lesson covering this concept");
                }
            }

            if (mastery.StrugglingConcepts.Length > 5)
            {
                ImGui.TextColored(NeutralColor, $"    ... and {mastery.StrugglingConcepts.Length - 5} more");
            }
        }

        // Developing concepts (only show count)
        if (mastery.DevelopingConcepts.Length > 0)
        {
            ImGui.TextColored(InfoColor, $"  Developing: {mastery.DevelopingConcepts.Length} concepts (need more practice)");
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip("These concepts need at least 10 opportunities before mastery can be evaluated.");
            }
        }

        // Summary
        if (mastery.MasteredConcepts.Length == 0 && mastery.StrugglingConcepts.Length == 0)
        {
            ImGui.TextColored(NeutralColor, "  Play more to build mastery data!");
        }
    }

    private static string FormatConceptName(string conceptId)
    {
        // Convert "whm.emergency_healing" to "Emergency Healing"
        var parts = conceptId.Split('.');
        var name = parts.Length > 1 ? parts[^1] : conceptId;
        name = name.Replace("_", " ");
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(name);
    }

    private static void DrawScoreComponent(string label, float value, int weight, string tooltip)
    {
        ImGui.Text($"  {label}:");
        ImGui.SameLine(180);

        // Progress bar
        var barColor = value switch
        {
            >= 75 => GoodColor,
            >= 40 => WarningColor,
            _ => NeutralColor,
        };

        ImGui.PushStyleColor(ImGuiCol.PlotHistogram, barColor);
        ImGui.ProgressBar(value / 100f, new Vector2(100, 0), $"{value:F0}%");
        ImGui.PopStyleColor();

        ImGui.SameLine();
        ImGui.TextColored(NeutralColor, $"({weight}% weight)");

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }
}
