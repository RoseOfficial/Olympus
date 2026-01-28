namespace Olympus.Windows.Training.Tabs;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;
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

    // Retention status colors (v3.52.0)
    private static readonly Vector4 FreshColor = new(0.3f, 0.9f, 0.3f, 1.0f);
    private static readonly Vector4 DecayingColor = new(0.9f, 0.6f, 0.2f, 1.0f);
    private static readonly Vector4 NeedsReviewColor = new(0.9f, 0.3f, 0.3f, 1.0f);

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
        Draw(trainingService, config, null);
    }

    public static void Draw(ITrainingService trainingService, TrainingConfig config, SpacedRepetitionService? spacedRepetition)
    {
        // Adaptive explanations toggle
        DrawAdaptiveSettings(config);

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        // Spaced repetition / retention section (v3.52.0)
        if (spacedRepetition?.IsEnabled == true)
        {
            DrawRetentionSection(spacedRepetition, trainingService, config);
            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();
        }

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
            ImGui.TextColored(UrgentColor, Loc.TFormat(LocalizedStrings.Training.FocusAreasFormat, "\u26A0 Focus Areas ({0} concepts need practice)", strugglingByJob.Count.ToString()));

            // Show top 5 struggling concepts
            foreach (var (jobPrefix, jobName, conceptId, rate) in strugglingByJob.Take(5))
            {
                ImGui.TextColored(WarningColor, Loc.TFormat(LocalizedStrings.Training.ConceptSuccessFormat, "  \u251C\u2500 {0}: {1} ({2} success)", jobPrefix.ToUpperInvariant(), FormatConceptName(conceptId), rate.ToString("P0")));
                ImGui.SameLine();
                if (ImGui.SmallButton(Loc.T(LocalizedStrings.Training.Study, "Study") + $"##{conceptId}"))
                {
                    // Find a lesson that covers this concept
                    var lessons = trainingService.GetLessonsForJob(jobPrefix);
                    var lesson = lessons.FirstOrDefault(l => l.ConceptsCovered.Contains(conceptId));
                    if (lesson != null)
                    {
                        pendingLessonNavigation = lesson.LessonId;
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.OpenLessonTooltip, "Open the lesson covering this concept"));
                }
            }

            if (strugglingByJob.Count > 5)
            {
                ImGui.TextColored(NeutralColor, Loc.TFormat(LocalizedStrings.Training.AndMoreFormat, "  \u2514\u2500 ... and {0} more", (strugglingByJob.Count - 5).ToString()));
            }

            ImGui.Spacing();
        }

        // Draw Recently Mastered section
        if (masteredByJob.Count > 0)
        {
            ImGui.TextColored(GoodColor, Loc.TFormat(LocalizedStrings.Training.RecentlyMasteredFormat, "\u2713 Recently Mastered ({0} concepts)", masteredByJob.Count.ToString()));

            // Show top 3 mastered concepts
            foreach (var (jobPrefix, jobName, conceptId, rate) in masteredByJob.Take(3))
            {
                ImGui.TextColored(GoodColor, Loc.TFormat(LocalizedStrings.Training.ConceptSuccessFormat, "  \u251C\u2500 {0}: {1} ({2} success)", jobPrefix.ToUpperInvariant(), FormatConceptName(conceptId), rate.ToString("P0")));
            }

            if (masteredByJob.Count > 3)
            {
                ImGui.TextColored(NeutralColor, Loc.TFormat(LocalizedStrings.Training.AndMoreFormat, "  \u2514\u2500 ... and {0} more", (masteredByJob.Count - 3).ToString()));
            }
        }

        if (strugglingByJob.Count == 0 && masteredByJob.Count == 0)
        {
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Training.PlayToBuildMastery, "Play with Training Mode enabled to build mastery data."));
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
        ImGui.Text(Loc.T(LocalizedStrings.Training.AdaptiveExplanations, "Adaptive Explanations"));
        ImGui.Separator();

        var enableAdaptive = config.EnableAdaptiveExplanations;
        if (ImGui.Checkbox(Loc.T(LocalizedStrings.Training.EnableAdaptiveVerbosity, "Enable Adaptive Verbosity"), ref enableAdaptive))
        {
            config.EnableAdaptiveExplanations = enableAdaptive;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.AdaptiveVerbosityTooltip,
                "When enabled, explanation verbosity automatically adjusts based on your skill level:\n" +
                "- Beginners see detailed explanations for all decisions\n" +
                "- Intermediate players see normal detail, with extra detail for unfamiliar concepts\n" +
                "- Advanced players see minimal detail, except for new or critical decisions"));
        }

        if (config.EnableAdaptiveExplanations)
        {
            ImGui.Spacing();

            // Override dropdown
            ImGui.Text(Loc.T(LocalizedStrings.Training.SkillLevelOverride, "Skill Level Override:"));
            ImGui.SameLine();

            var overrideOptions = new[]
            {
                Loc.T(LocalizedStrings.Training.AutoDetect, "Auto-detect"),
                Loc.T(LocalizedStrings.Training.Beginner, "Beginner"),
                Loc.T(LocalizedStrings.Training.Intermediate, "Intermediate"),
                Loc.T(LocalizedStrings.Training.Advanced, "Advanced"),
            };
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
                ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.SkillOverrideTooltip,
                    "Override the detected skill level:\n" +
                    "- Auto-detect: Uses quiz and lesson progress to determine level\n" +
                    "- Beginner/Intermediate/Advanced: Force a specific level"));
            }
        }
    }

    private static void DrawJobSkillLevels(ITrainingService trainingService, TrainingConfig config)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Training.SkillLevelsByJob, "Skill Levels by Job"));
        ImGui.Separator();

        if (!config.EnableAdaptiveExplanations)
        {
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Training.EnableAdaptiveToSeeSkills, "Enable adaptive explanations above to see skill levels."));
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
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Training.NoProgressYet, "No progress yet. Complete lessons and quizzes to see your skill levels."));
            ImGui.Spacing();
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Training.GoToLessonsTab, "Go to the Lessons tab to get started!"));
            return;
        }

        // Display each job with progress
        foreach (var job in jobsWithProgress)
        {
            DrawJobSkillLevel(job.Prefix, job.Name, job.Result, job.Mastery, trainingService, config);
            ImGui.Spacing();
        }

        // Show hint for other jobs
        var jobsWithoutProgress = AllJobPrefixes.Length - jobsWithProgress.Length;
        if (jobsWithoutProgress > 0)
        {
            ImGui.Spacing();
            ImGui.TextColored(NeutralColor, Loc.TFormat(LocalizedStrings.Training.OtherJobsNoProgress, "{0} other jobs with no progress yet.", jobsWithoutProgress.ToString()));
        }
    }

    private static void DrawJobSkillLevel(string jobPrefix, string jobName, SkillLevelResult result, ConceptMasteryResult mastery, ITrainingService trainingService, TrainingConfig config)
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
            ImGui.TextColored(levelColor, Loc.TFormat(LocalizedStrings.Training.LevelFormat, "Level: {0}", result.Level.ToString()));
            ImGui.SameLine();
            ImGui.TextColored(NeutralColor, Loc.TFormat(LocalizedStrings.Training.ScoreValueFormat, "(Score: {0}/100)", result.CompositeScore.ToString("F0")));

            // Score breakdown
            ImGui.Spacing();
            ImGui.Text(Loc.T(LocalizedStrings.Training.ScoreBreakdown, "Score Breakdown:"));

            // Quiz pass rate (30%)
            DrawScoreComponent(Loc.T(LocalizedStrings.Training.QuizPassRate, "Quiz Pass Rate"), result.QuizPassRate, 30,
                Loc.TFormat(LocalizedStrings.Training.QuizPassRateTooltip, "{0}/{1} quizzes passed", result.PassedQuizzes.ToString(), result.TotalQuizzes.ToString()));

            // Quiz quality (20%)
            DrawScoreComponent(Loc.T(LocalizedStrings.Training.QuizQuality, "Quiz Quality"), result.QuizQuality, 20,
                Loc.T(LocalizedStrings.Training.QuizQualityTooltip, "Average score on passed quizzes"));

            // Lessons completed (20%)
            DrawScoreComponent(Loc.T(LocalizedStrings.Training.LessonsCompletedScore, "Lessons Completed"), result.LessonsCompleted, 20,
                Loc.TFormat(LocalizedStrings.Training.LessonsCompletedTooltip, "{0}/{1} lessons done", result.CompletedLessonsCount.ToString(), result.TotalLessons.ToString()));

            // Concepts learned (5%)
            DrawScoreComponent(Loc.T(LocalizedStrings.Training.ConceptsLearnedScore, "Concepts Learned"), result.ConceptsLearned, 5,
                Loc.T(LocalizedStrings.Training.ConceptsLearnedTooltip, "Marked as learned"));

            // Concept Mastery (25%) - NEW in v3.28.0
            DrawScoreComponent(Loc.T(LocalizedStrings.Training.ConceptMasteryScore, "Concept Mastery"), result.ConceptMastery, 25,
                Loc.T(LocalizedStrings.Training.ConceptMasteryTooltip, "Success rate when applying concepts in combat"));

            // Engagement penalty warning
            if (result.EngagementPenaltyApplied)
            {
                ImGui.Spacing();
                ImGui.TextColored(WarningColor, Loc.T(LocalizedStrings.Training.EngagementPenalty, "Note: -25% penalty applied (lessons completed without quizzes)"));
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.EngagementPenaltyTooltip, "Take quizzes to validate your understanding and remove this penalty."));
                }
            }

            // Concept Mastery Details (v3.28.0, enhanced in v3.29.0)
            DrawMasteryDetails(mastery, jobPrefix, trainingService, config);

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
                ImGui.TextColored(WarningColor, Loc.TFormat(LocalizedStrings.Training.StrugglingFormat, "\u26A0{0} struggling", mastery.StrugglingConcepts.Length.ToString()));
            }
            else if (mastery.MasteredConcepts.Length > 0)
            {
                ImGui.SameLine();
                ImGui.TextColored(GoodColor, Loc.TFormat(LocalizedStrings.Training.MasteredFormat, "\u2713{0} mastered", mastery.MasteredConcepts.Length.ToString()));
            }
        }
    }

    private static void DrawMasteryDetails(ConceptMasteryResult mastery, string jobPrefix, ITrainingService trainingService, TrainingConfig config)
    {
        if (mastery.TotalConcepts == 0)
            return;

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Text(Loc.T(LocalizedStrings.Training.ConceptMasteryDetails, "Concept Mastery Details:"));

        // Mastered concepts
        if (mastery.MasteredConcepts.Length > 0)
        {
            ImGui.TextColored(GoodColor, Loc.TFormat(LocalizedStrings.Training.MasteredCountFormat, "  Mastered ({0}):", mastery.MasteredConcepts.Length.ToString()));
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
            ImGui.TextColored(WarningColor, Loc.TFormat(LocalizedStrings.Training.NeedsPracticeCountFormat, "  Needs Practice ({0}):", mastery.StrugglingConcepts.Length.ToString()));
            foreach (var concept in mastery.StrugglingConcepts.Take(5))
            {
                var rate = GetConceptSuccessRate(config, concept);
                ImGui.TextColored(WarningColor, $"    \u26A0 {FormatConceptName(concept)} ({rate:P0})");
                ImGui.SameLine();
                if (ImGui.SmallButton(Loc.T(LocalizedStrings.Training.StudyThis, "Study This") + $"##{concept}"))
                {
                    // Find a lesson that covers this concept
                    var lessons = trainingService.GetLessonsForJob(jobPrefix);
                    var lesson = lessons.FirstOrDefault(l => l.ConceptsCovered.Contains(concept));
                    if (lesson != null)
                    {
                        pendingLessonNavigation = lesson.LessonId;
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.OpenLessonTooltip, "Open the lesson covering this concept"));
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
            ImGui.TextColored(InfoColor, Loc.TFormat(LocalizedStrings.Training.DevelopingFormat, "  Developing: {0} concepts (need more practice)", mastery.DevelopingConcepts.Length.ToString()));
            if (ImGui.IsItemHovered())
            {
                ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.DevelopingTooltip, "These concepts need at least 10 opportunities before mastery can be evaluated."));
            }
        }

        // Summary
        if (mastery.MasteredConcepts.Length == 0 && mastery.StrugglingConcepts.Length == 0)
        {
            ImGui.TextColored(NeutralColor, Loc.T(LocalizedStrings.Training.PlayMoreToBuildMastery, "  Play more to build mastery data!"));
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

    #region Spaced Repetition / Retention (v3.52.0)

    private static void DrawRetentionSection(SpacedRepetitionService spacedRepetition, ITrainingService trainingService, TrainingConfig config)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Training.KnowledgeRetention, "Knowledge Retention"));
        ImGui.Separator();

        // Retention toggle
        var enableRetention = config.EnableSpacedRepetition;
        if (ImGui.Checkbox(Loc.T(LocalizedStrings.Training.TrackKnowledgeRetention, "Track Knowledge Retention"), ref enableRetention))
        {
            config.EnableSpacedRepetition = enableRetention;
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.RetentionTooltip,
                "Track how well you remember concepts over time.\n" +
                "Concepts decay without practice - review suggested when retention drops below 40%."));
        }

        if (!config.EnableSpacedRepetition)
            return;

        ImGui.Spacing();

        // Overall status message
        var statusMessage = spacedRepetition.GetReviewStatusMessage();
        var hasUrgent = spacedRepetition.HasUrgentReviews();
        var statusColor = hasUrgent ? NeedsReviewColor : (statusMessage.Contains("due") ? DecayingColor : FreshColor);
        ImGui.TextColored(statusColor, statusMessage);

        // Show concepts needing review
        var needsReview = spacedRepetition.GetConceptsNeedingReview().ToList();
        var needsRelearning = spacedRepetition.GetConceptsNeedingRelearning().ToList();

        if (needsRelearning.Count > 0)
        {
            ImGui.Spacing();
            ImGui.TextColored(NeedsReviewColor, Loc.TFormat(LocalizedStrings.Training.NeedsRelearningFormat, "\u26A0 Needs Re-learning ({0}):", needsRelearning.Count.ToString()));

            foreach (var data in needsRelearning.Take(3))
            {
                ImGui.TextColored(NeedsReviewColor, Loc.TFormat(LocalizedStrings.Training.RetentionFormat, "  \u2718 {0} ({1} retention)", FormatConceptName(data.ConceptId), data.RetentionScore.ToString("P0")));
                ImGui.SameLine();

                // Find and suggest quiz
                var jobPrefix = data.ConceptId.Split('.').FirstOrDefault() ?? string.Empty;
                if (!string.IsNullOrEmpty(jobPrefix) && ImGui.SmallButton(Loc.T(LocalizedStrings.Training.Review, "Review") + $"##{data.ConceptId}"))
                {
                    var lessons = trainingService.GetLessonsForJob(jobPrefix);
                    var lesson = lessons.FirstOrDefault(l => l.ConceptsCovered.Contains(data.ConceptId));
                    if (lesson != null)
                    {
                        pendingLessonNavigation = lesson.LessonId;
                    }
                }

                if (ImGui.IsItemHovered())
                {
                    var daysSince = data.DaysSinceLastPractice;
                    ImGui.SetTooltip(Loc.TFormat(LocalizedStrings.Training.LastPracticedTooltip, "Last practiced: {0} days ago\nOpen the lesson to refresh your knowledge.", daysSince.ToString("F0")));
                }
            }

            if (needsRelearning.Count > 3)
            {
                ImGui.TextColored(NeutralColor, $"  ... and {needsRelearning.Count - 3} more");
            }
        }

        if (needsReview.Count > 0 && needsReview.Count != needsRelearning.Count)
        {
            // Filter out the ones already shown in relearning
            var reviewOnly = needsReview.Where(r => !needsRelearning.Any(rl => rl.ConceptId == r.ConceptId)).ToList();
            if (reviewOnly.Count > 0)
            {
                ImGui.Spacing();
                ImGui.TextColored(DecayingColor, Loc.TFormat(LocalizedStrings.Training.DueForReviewFormat, "\u23F0 Due for Review ({0}):", reviewOnly.Count.ToString()));

                foreach (var data in reviewOnly.Take(3))
                {
                    ImGui.TextColored(DecayingColor, Loc.TFormat(LocalizedStrings.Training.RetentionFormat, "  \u25CB {0} ({1} retention)", FormatConceptName(data.ConceptId), data.RetentionScore.ToString("P0")));
                    ImGui.SameLine();

                    var jobPrefix = data.ConceptId.Split('.').FirstOrDefault() ?? string.Empty;
                    if (!string.IsNullOrEmpty(jobPrefix) && ImGui.SmallButton(Loc.T(LocalizedStrings.Training.Review, "Review") + $"##{data.ConceptId}"))
                    {
                        var lessons = trainingService.GetLessonsForJob(jobPrefix);
                        var lesson = lessons.FirstOrDefault(l => l.ConceptsCovered.Contains(data.ConceptId));
                        if (lesson != null)
                        {
                            pendingLessonNavigation = lesson.LessonId;
                        }
                    }

                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip(Loc.T(LocalizedStrings.Training.RetentionDecliningTooltip, "Retention declining.\nReview recommended to maintain knowledge."));
                    }
                }

                if (reviewOnly.Count > 3)
                {
                    ImGui.TextColored(NeutralColor, $"  ... and {reviewOnly.Count - 3} more");
                }
            }
        }

        // Show strongest concepts (fresh)
        var strongestConcepts = spacedRepetition.GetStrongestConcepts(count: 3).ToList();
        if (strongestConcepts.Count > 0)
        {
            ImGui.Spacing();
            ImGui.TextColored(FreshColor, Loc.T(LocalizedStrings.Training.FreshInMemory, "\u2713 Fresh in Memory:"));

            foreach (var data in strongestConcepts)
            {
                var daysUntilReview = data.DaysUntilReviewNeeded;
                var daysText = daysUntilReview > 1
                    ? Loc.TFormat(LocalizedStrings.Training.DaysUntilReview, "{0}d until review", daysUntilReview.ToString("F0"))
                    : Loc.T(LocalizedStrings.Training.ReviewSoon, "review soon");
                ImGui.TextColored(FreshColor, Loc.TFormat(LocalizedStrings.Training.FreshConceptFormat, "  \u2713 {0} ({1}, {2})", FormatConceptName(data.ConceptId), data.RetentionScore.ToString("P0"), daysText));
            }
        }

        // Suggested quizzes
        var suggestedQuizzes = spacedRepetition.SuggestReviewQuizzes(maxQuizzes: 2).ToList();
        if (suggestedQuizzes.Count > 0)
        {
            ImGui.Spacing();
            ImGui.TextColored(InfoColor, Loc.T(LocalizedStrings.Training.SuggestedReviewQuizzes, "Suggested Review Quizzes:"));

            foreach (var quizId in suggestedQuizzes)
            {
                var parts = quizId.Split('.');
                var jobPrefix = parts.Length > 0 ? parts[0] : string.Empty;
                var quizNum = parts.Length > 2 ? parts[2] : "?";
                var jobIndex = Array.IndexOf(AllJobPrefixes, jobPrefix);
                var jobName = jobIndex >= 0 ? JobDisplayNames[jobIndex] : jobPrefix.ToUpperInvariant();

                ImGui.Text(Loc.TFormat(LocalizedStrings.Training.JobQuizFormat, "  \u2022 {0} Quiz {1}", jobName, quizNum));
                ImGui.SameLine();
                if (ImGui.SmallButton(Loc.T(LocalizedStrings.Training.TakeQuiz, "Take Quiz") + $"##{quizId}"))
                {
                    // Navigate to quiz - set pending navigation
                    pendingLessonNavigation = quizId;
                }
            }
        }
    }

    #endregion

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
        ImGui.TextColored(NeutralColor, Loc.TFormat(LocalizedStrings.Training.WeightFormat, "({0}% weight)", weight.ToString()));

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(tooltip);
        }
    }
}
