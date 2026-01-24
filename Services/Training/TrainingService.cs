namespace Olympus.Services.Training;

using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Plugin.Services;
using Olympus.Config;
using Olympus.Data;
using Olympus.Services.Analytics;

/// <summary>
/// Core implementation of Training Mode - captures rotation decisions and provides explanations.
/// </summary>
public sealed class TrainingService : ITrainingService
{
    private readonly TrainingConfig config;
    private readonly IObjectTable objectTable;
    private readonly IPluginLog? log;

    private readonly List<ActionExplanation> explanations = new();
    private readonly object explanationsLock = new();

    private readonly List<LessonRecommendation> currentRecommendations = new();
    private readonly object recommendationsLock = new();

    private bool wasInCombat;

    public TrainingService(
        TrainingConfig config,
        IObjectTable objectTable,
        IPluginLog? log = null)
    {
        this.config = config;
        this.objectTable = objectTable;
        this.log = log;
    }

    public bool IsTrainingEnabled
    {
        get => config.EnableTraining;
        set => config.EnableTraining = value;
    }

    public bool IsInCombat { get; private set; }

    public IReadOnlyList<ActionExplanation> RecentExplanations
    {
        get
        {
            lock (this.explanationsLock)
            {
                return this.explanations.ToList();
            }
        }
    }

    public ActionExplanation? CurrentExplanation
    {
        get
        {
            lock (this.explanationsLock)
            {
                return this.explanations.FirstOrDefault();
            }
        }
    }

    public void RecordDecision(ActionExplanation explanation)
    {
        if (!this.config.EnableTraining)
            return;

        // Filter by priority
        if (explanation.Priority < this.config.MinimumPriorityToShow)
            return;

        lock (this.explanationsLock)
        {
            // Add to front (most recent first)
            this.explanations.Insert(0, explanation);

            // Trim to max size
            while (this.explanations.Count > this.config.MaxExplanationsToShow)
            {
                this.explanations.RemoveAt(this.explanations.Count - 1);
            }
        }

        // Track concept exposure
        if (!string.IsNullOrEmpty(explanation.ConceptId))
        {
            if (this.config.ConceptExposureCount.TryGetValue(explanation.ConceptId, out var count))
            {
                this.config.ConceptExposureCount[explanation.ConceptId] = count + 1;
            }
            else
            {
                this.config.ConceptExposureCount[explanation.ConceptId] = 1;
            }
        }

        this.log?.Debug("Training: Recorded {ActionName} - {Reason}", explanation.ActionName, explanation.ShortReason);
    }

    public LearningProgress GetProgress()
    {
        var allConcepts = GetAllConcepts();
        var learned = this.config.LearnedConcepts.Count(c => allConcepts.Contains(c));

        // Find concepts with high exposure but not learned (>10 exposures)
        var needingAttention = this.config.ConceptExposureCount
            .Where(kvp => kvp.Value >= 10 && !this.config.LearnedConcepts.Contains(kvp.Key))
            .Select(kvp => kvp.Key)
            .ToArray();

        // Find recently demonstrated concepts (from current explanations)
        string[] recentlyDemonstrated;
        lock (this.explanationsLock)
        {
            recentlyDemonstrated = this.explanations
                .Where(e => !string.IsNullOrEmpty(e.ConceptId))
                .Select(e => e.ConceptId!)
                .Distinct()
                .Take(5)
                .ToArray();
        }

        return new LearningProgress
        {
            TotalConcepts = allConcepts.Length,
            LearnedConcepts = learned,
            ConceptsNeedingAttention = needingAttention,
            RecentlyDemonstratedConcepts = recentlyDemonstrated,
        };
    }

    /// <summary>
    /// Gets all concepts across healers and tanks.
    /// </summary>
    private static string[] GetAllConcepts()
    {
        return WhmConcepts.AllConcepts
            .Concat(SchConcepts.AllConcepts)
            .Concat(AstConcepts.AllConcepts)
            .Concat(SgeConcepts.AllConcepts)
            .Concat(PldConcepts.AllConcepts)
            .Concat(WarConcepts.AllConcepts)
            .Concat(DrkConcepts.AllConcepts)
            .Concat(GnbConcepts.AllConcepts)
            .ToArray();
    }

    /// <summary>
    /// Gets concepts for a specific job based on concept ID prefix.
    /// </summary>
    /// <param name="jobPrefix">The job prefix (e.g., "whm", "sch", "ast", "sge", "pld", "war", "drk", "gnb").</param>
    public static string[] GetConceptsForJob(string jobPrefix)
    {
        return jobPrefix.ToLowerInvariant() switch
        {
            // Healers
            "whm" => WhmConcepts.AllConcepts,
            "sch" => SchConcepts.AllConcepts,
            "ast" => AstConcepts.AllConcepts,
            "sge" => SgeConcepts.AllConcepts,
            // Tanks
            "pld" => PldConcepts.AllConcepts,
            "war" => WarConcepts.AllConcepts,
            "drk" => DrkConcepts.AllConcepts,
            "gnb" => GnbConcepts.AllConcepts,
            _ => Array.Empty<string>(),
        };
    }

    public void MarkConceptLearned(string conceptId)
    {
        this.config.LearnedConcepts.Add(conceptId);
        this.log?.Information("Training: Marked concept as learned: {Concept}", conceptId);
    }

    public void UnmarkConceptLearned(string conceptId)
    {
        this.config.LearnedConcepts.Remove(conceptId);
        this.log?.Information("Training: Unmarked concept: {Concept}", conceptId);
    }

    #region Lesson Management

    public IReadOnlyList<LessonDefinition> GetLessonsForJob(string jobPrefix)
    {
        return LessonRegistry.GetLessonsForJob(jobPrefix);
    }

    public bool IsLessonComplete(string lessonId)
    {
        return this.config.CompletedLessons.Contains(lessonId);
    }

    public void MarkLessonComplete(string lessonId)
    {
        this.config.CompletedLessons.Add(lessonId);
        this.log?.Information("Training: Marked lesson as complete: {Lesson}", lessonId);

        // Also mark all concepts covered by this lesson as learned
        var lesson = LessonRegistry.GetLesson(lessonId);
        if (lesson != null)
        {
            foreach (var concept in lesson.ConceptsCovered)
            {
                if (!this.config.LearnedConcepts.Contains(concept))
                {
                    this.config.LearnedConcepts.Add(concept);
                }
            }
        }
    }

    public bool AreLessonPrerequisitesMet(string lessonId)
    {
        var lesson = LessonRegistry.GetLesson(lessonId);
        if (lesson == null)
            return false;

        if (lesson.Prerequisites.Length == 0)
            return true;

        return lesson.Prerequisites.All(prereq => this.config.CompletedLessons.Contains(prereq));
    }

    #endregion

    #region Skill Quizzes

    public QuizDefinition? GetQuizForLesson(string lessonId)
    {
        return QuizRegistry.GetQuizForLesson(lessonId);
    }

    public bool IsQuizPassed(string quizId)
    {
        return this.config.CompletedQuizzes.Contains(quizId);
    }

    public QuizAttempt? GetBestAttempt(string quizId)
    {
        if (this.config.BestQuizAttempts.TryGetValue(quizId, out var attemptData))
        {
            return new QuizAttempt
            {
                QuizId = quizId,
                AttemptedAt = attemptData.AttemptedAt,
                Score = attemptData.Score,
                Passed = attemptData.Passed,
                SelectedAnswers = Array.Empty<int>(), // Not persisted in config
            };
        }

        return null;
    }

    public void RecordQuizAttempt(QuizAttempt attempt)
    {
        var quiz = QuizRegistry.GetQuiz(attempt.QuizId);
        if (quiz == null)
            return;

        // Only keep best attempt (by score)
        if (!this.config.BestQuizAttempts.TryGetValue(attempt.QuizId, out var existing)
            || attempt.Score > existing.Score)
        {
            this.config.BestQuizAttempts[attempt.QuizId] = new Config.QuizAttemptData
            {
                AttemptedAt = attempt.AttemptedAt,
                Score = attempt.Score,
                TotalQuestions = quiz.Questions.Length,
                Passed = attempt.Passed,
            };
        }

        // Mark as completed if passed
        if (attempt.Passed && !this.config.CompletedQuizzes.Contains(attempt.QuizId))
        {
            this.config.CompletedQuizzes.Add(attempt.QuizId);
            this.log?.Information("Training: Quiz passed: {QuizId} ({Score}/{Total})",
                attempt.QuizId, attempt.Score, quiz.Questions.Length);
        }
    }

    #endregion

    #region Lesson Recommendations

    public IReadOnlyList<LessonRecommendation> GetRecommendations()
    {
        lock (this.recommendationsLock)
        {
            return this.currentRecommendations.ToList();
        }
    }

    public void UpdateRecommendations(FightSession session)
    {
        if (!this.config.EnableRecommendations)
            return;

        if (session.Issues == null || session.Issues.Count == 0)
            return;

        // Get job prefix from job ID
        var jobPrefix = GetJobPrefix(session.JobId);
        if (string.IsNullOrEmpty(jobPrefix))
        {
            this.log?.Debug("Training: No job prefix for job ID {JobId}, skipping recommendations", session.JobId);
            return;
        }

        var lessons = LessonRegistry.GetLessonsForJob(jobPrefix);
        if (lessons.Count == 0)
            return;

        var candidates = new List<(LessonDefinition Lesson, int Priority, string Reason, IssueType[] Issues)>();

        // Build candidate lessons from issues
        foreach (var issue in session.Issues)
        {
            if (!IssueConceptMapping.Mappings.TryGetValue(issue.Type, out var mapping))
                continue;

            var (conceptPatterns, basePriority, reasonTemplate) = mapping;

            // Adjust priority based on issue severity
            var adjustedPriority = basePriority + issue.Severity switch
            {
                IssueSeverity.Error => 10,
                IssueSeverity.Warning => 5,
                _ => 0
            };

            // Find lessons covering matching concepts
            foreach (var lesson in lessons)
            {
                // Skip completed lessons
                if (this.config.CompletedLessons.Contains(lesson.LessonId))
                    continue;

                // Skip dismissed lessons
                if (this.config.DismissedRecommendations.Contains(lesson.LessonId))
                    continue;

                // Check if lesson covers any matching concepts
                var matchingConcepts = lesson.ConceptsCovered
                    .Where(concept => conceptPatterns.Any(pattern => ConceptMatchesPattern(concept, pattern)))
                    .ToArray();

                if (matchingConcepts.Length == 0)
                    continue;

                // Found a match
                var reason = $"{reasonTemplate} - this lesson covers {string.Join(", ", matchingConcepts.Take(2).Select(FormatConceptName))}";
                candidates.Add((lesson, adjustedPriority, reason, new[] { issue.Type }));
            }
        }

        // Deduplicate by lesson (keep highest priority)
        var deduped = candidates
            .GroupBy(c => c.Lesson.LessonId)
            .Select(g =>
            {
                var best = g.OrderByDescending(c => c.Priority).First();
                var allIssues = g.SelectMany(c => c.Issues).Distinct().ToArray();
                return (best.Lesson, best.Priority, best.Reason, allIssues);
            })
            .OrderByDescending(c => c.Priority)
            .Take(this.config.MaxRecommendations)
            .ToList();

        // Build recommendations
        var recommendations = deduped.Select(c => new LessonRecommendation
        {
            Lesson = c.Lesson,
            Priority = c.Priority,
            Reason = c.Reason,
            TriggeringIssues = c.allIssues,
        }).ToList();

        lock (this.recommendationsLock)
        {
            this.currentRecommendations.Clear();
            this.currentRecommendations.AddRange(recommendations);
        }

        this.log?.Information("Training: Generated {Count} recommendations for {Job}", recommendations.Count, jobPrefix);
    }

    public void DismissRecommendation(string lessonId)
    {
        this.config.DismissedRecommendations.Add(lessonId);

        lock (this.recommendationsLock)
        {
            this.currentRecommendations.RemoveAll(r => r.Lesson.LessonId == lessonId);
        }

        this.log?.Debug("Training: Dismissed recommendation for {Lesson}", lessonId);
    }

    public void ClearDismissedRecommendations()
    {
        this.config.DismissedRecommendations.Clear();
        this.log?.Debug("Training: Cleared all dismissed recommendations");
    }

    /// <summary>
    /// Checks if a concept ID matches a pattern using suffix/contains matching.
    /// </summary>
    private static bool ConceptMatchesPattern(string conceptId, string pattern)
    {
        // Pattern should match suffix or be contained in concept
        // e.g., "emergency_healing" matches "whm.emergency_healing", "sch.emergency_healing"
        return conceptId.EndsWith(pattern, StringComparison.OrdinalIgnoreCase) ||
               conceptId.Contains(pattern, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the job prefix for a given job ID.
    /// </summary>
    private static string? GetJobPrefix(uint jobId)
    {
        return jobId switch
        {
            // Healers
            JobRegistry.WhiteMage or JobRegistry.Conjurer => "whm",
            JobRegistry.Scholar or JobRegistry.Arcanist => "sch",
            JobRegistry.Astrologian => "ast",
            JobRegistry.Sage => "sge",
            // Tanks
            JobRegistry.Paladin or JobRegistry.Gladiator => "pld",
            JobRegistry.Warrior or JobRegistry.Marauder => "war",
            JobRegistry.DarkKnight => "drk",
            JobRegistry.Gunbreaker => "gnb",
            _ => null // Only healers and tanks have training mode lessons currently
        };
    }

    /// <summary>
    /// Formats a concept ID for display.
    /// </summary>
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

    #endregion

    public void ClearExplanations()
    {
        lock (this.explanationsLock)
        {
            this.explanations.Clear();
        }

        this.log?.Debug("Training: Cleared all explanations");
    }

    public void Update()
    {
        // Update combat state
        var player = this.objectTable.LocalPlayer;
        this.IsInCombat = player?.StatusFlags.HasFlag(Dalamud.Game.ClientState.Objects.Enums.StatusFlags.InCombat) ?? false;

        // Clear explanations when combat ends
        if (this.wasInCombat && !this.IsInCombat)
        {
            // Keep explanations for a bit after combat for review
            // They'll naturally be cleared when new combat starts and fills the queue
        }

        // Clear when entering new combat
        if (!this.wasInCombat && this.IsInCombat)
        {
            this.ClearExplanations();
        }

        this.wasInCombat = this.IsInCombat;
    }
}
