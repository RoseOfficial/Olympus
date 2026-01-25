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

    // Spaced repetition integration (v4.0.0)
    private SpacedRepetitionService? spacedRepetitionService;

    public TrainingService(
        TrainingConfig config,
        IObjectTable objectTable,
        IPluginLog? log = null)
    {
        this.config = config;
        this.objectTable = objectTable;
        this.log = log;
    }

    /// <summary>
    /// Sets the spaced repetition service for retention tracking.
    /// Called after construction to avoid circular dependencies.
    /// </summary>
    public void SetSpacedRepetitionService(SpacedRepetitionService? service)
    {
        this.spacedRepetitionService = service;
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
    /// Gets all concepts across healers, tanks, and DPS.
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
            .Concat(DrgConcepts.AllConcepts)
            .Concat(NinConcepts.AllConcepts)
            .Concat(SamConcepts.AllConcepts)
            .Concat(MnkConcepts.AllConcepts)
            .Concat(RprConcepts.AllConcepts)
            .Concat(VprConcepts.AllConcepts)
            .Concat(MchConcepts.AllConcepts)
            .Concat(BrdConcepts.AllConcepts)
            .Concat(DncConcepts.AllConcepts)
            .Concat(BlmConcepts.AllConcepts)
            .Concat(SmnConcepts.AllConcepts)
            .Concat(RdmConcepts.AllConcepts)
            .Concat(PctConcepts.AllConcepts)
            .ToArray();
    }

    /// <summary>
    /// Gets concepts for a specific job based on concept ID prefix.
    /// </summary>
    /// <param name="jobPrefix">The job prefix (e.g., "whm", "sch", "ast", "sge", "pld", "war", "drk", "gnb", "drg", "nin", "rpr", "vpr", "mch", "brd").</param>
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
            // Melee DPS
            "drg" => DrgConcepts.AllConcepts,
            "nin" => NinConcepts.AllConcepts,
            "sam" => SamConcepts.AllConcepts,
            "mnk" => MnkConcepts.AllConcepts,
            "rpr" => RprConcepts.AllConcepts,
            "vpr" => VprConcepts.AllConcepts,
            // Ranged Physical DPS
            "mch" => MchConcepts.AllConcepts,
            "brd" => BrdConcepts.AllConcepts,
            "dnc" => DncConcepts.AllConcepts,
            // Casters
            "blm" => BlmConcepts.AllConcepts,
            "smn" => SmnConcepts.AllConcepts,
            "rdm" => RdmConcepts.AllConcepts,
            "pct" => PctConcepts.AllConcepts,
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

    public LearningPathRecommendation GetNextRecommendedLesson(string jobPrefix)
    {
        var lessons = LessonRegistry.GetLessonsForJob(jobPrefix);
        var skillLevelResult = GetSkillLevel(jobPrefix);
        var mastery = GetConceptMastery(jobPrefix);

        // Count completed lessons
        var completedLessons = lessons.Where(l => this.config.CompletedLessons.Contains(l.LessonId)).ToList();
        var incompleteLessons = lessons.Where(l => !this.config.CompletedLessons.Contains(l.LessonId)).ToList();

        var baseResult = new LearningPathRecommendation
        {
            CompletedLessons = completedLessons.Count,
            TotalLessons = lessons.Count,
            SkillLevel = skillLevelResult.Level,
            StrugglingConcepts = mastery.StrugglingConcepts,
        };

        // All complete?
        if (incompleteLessons.Count == 0)
        {
            return baseResult with
            {
                RecommendedLessonId = null,
                Reason = skillLevelResult.Level == SkillLevel.Advanced
                    ? "Consider retaking quizzes for mastery"
                    : "Review previous lessons or take quizzes",
                ReasonType = LearningPathReason.AllComplete,
            };
        }

        // Priority 1: Address struggling concepts (if any with 10+ opportunities)
        if (mastery.StrugglingConcepts.Length > 0)
        {
            foreach (var lesson in incompleteLessons)
            {
                // Check if this lesson covers any struggling concept
                var coveredStruggling = lesson.ConceptsCovered
                    .Intersect(mastery.StrugglingConcepts)
                    .ToArray();

                if (coveredStruggling.Length > 0)
                {
                    // Get success rate for most struggling concept
                    var worstConcept = coveredStruggling
                        .Select(c => (Concept: c, Rate: GetConceptSuccessRate(c)))
                        .OrderBy(x => x.Rate)
                        .First();

                    return baseResult with
                    {
                        RecommendedLessonId = lesson.LessonId,
                        Reason = $"Covers: {FormatConceptName(worstConcept.Concept)} ({worstConcept.Rate:P0} success)",
                        ReasonType = LearningPathReason.AddressStrugglingConcept,
                    };
                }
            }
        }

        // Priority 2: Skill-appropriate progression
        var nextLesson = GetSkillAppropriateLesson(skillLevelResult.Level, lessons.ToList(), incompleteLessons);

        if (nextLesson == null)
        {
            // Fallback to first incomplete
            nextLesson = incompleteLessons.First();
        }

        var reason = (skillLevelResult.Level, completedLessons.Count) switch
        {
            (_, 0) => "Start here to build your foundation",
            (SkillLevel.Advanced, _) => "Review optimization techniques",
            _ => "Continue where you left off",
        };

        var reasonType = completedLessons.Count == 0
            ? LearningPathReason.StartFromBeginning
            : skillLevelResult.Level == SkillLevel.Advanced
                ? LearningPathReason.ReviewForMastery
                : LearningPathReason.ContinueProgress;

        return baseResult with
        {
            RecommendedLessonId = nextLesson.LessonId,
            Reason = reason,
            ReasonType = reasonType,
        };
    }

    /// <summary>
    /// Gets the appropriate lesson based on skill level.
    /// </summary>
    private LessonDefinition? GetSkillAppropriateLesson(
        SkillLevel skillLevel,
        List<LessonDefinition> allLessons,
        List<LessonDefinition> incompleteLessons)
    {
        return skillLevel switch
        {
            // Beginners: Always start from the beginning, first incomplete lesson
            SkillLevel.Beginner => incompleteLessons.FirstOrDefault(),

            // Intermediate: If lessons 1-2 are done, skip to lesson 3-4, else continue from current
            SkillLevel.Intermediate => GetIntermediateLesson(allLessons, incompleteLessons),

            // Advanced: Focus on lessons 5-7 (advanced topics), or first incomplete
            SkillLevel.Advanced => GetAdvancedLesson(allLessons, incompleteLessons),

            _ => incompleteLessons.FirstOrDefault(),
        };
    }

    private static LessonDefinition? GetIntermediateLesson(
        List<LessonDefinition> allLessons,
        List<LessonDefinition> incompleteLessons)
    {
        // If basic lessons (1-2) are done, prioritize intermediate lessons (3-4)
        var basicLessons = allLessons.Where(l => l.LessonNumber <= 2).ToList();
        var basicComplete = basicLessons.All(l => !incompleteLessons.Contains(l));

        if (basicComplete)
        {
            // Try to find an intermediate lesson (3-4) that's incomplete
            var intermediateLesson = incompleteLessons
                .FirstOrDefault(l => l.LessonNumber >= 3 && l.LessonNumber <= 4);

            if (intermediateLesson != null)
                return intermediateLesson;
        }

        // Otherwise continue linearly
        return incompleteLessons.FirstOrDefault();
    }

    private static LessonDefinition? GetAdvancedLesson(
        List<LessonDefinition> allLessons,
        List<LessonDefinition> incompleteLessons)
    {
        // If intermediate lessons (1-4) are done, prioritize advanced lessons (5-7)
        var intermediateLessons = allLessons.Where(l => l.LessonNumber <= 4).ToList();
        var intermediateComplete = intermediateLessons.All(l => !incompleteLessons.Contains(l));

        if (intermediateComplete)
        {
            // Try to find an advanced lesson (5-7) that's incomplete
            var advancedLesson = incompleteLessons
                .FirstOrDefault(l => l.LessonNumber >= 5);

            if (advancedLesson != null)
                return advancedLesson;
        }

        // Otherwise continue linearly
        return incompleteLessons.FirstOrDefault();
    }

    /// <summary>
    /// Gets the success rate for a specific concept.
    /// </summary>
    private float GetConceptSuccessRate(string conceptId)
    {
        if (this.config.ConceptMastery.TryGetValue(conceptId, out var data))
        {
            return data.SuccessRate;
        }

        return 0f;
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

        var candidates = new List<(LessonDefinition Lesson, int Priority, string Reason, IssueType[] Issues, string[] StrugglingConcepts, bool IsMasteryDriven)>();

        // Build candidate lessons from issues (if any)
        if (session.Issues != null && session.Issues.Count > 0)
        {
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
                    candidates.Add((lesson, adjustedPriority, reason, new[] { issue.Type }, Array.Empty<string>(), false));
                }
            }
        }

        // Add mastery-driven recommendations
        AddMasteryDrivenCandidates(jobPrefix, lessons, candidates);

        // Deduplicate by lesson (keep highest priority, merge data)
        var deduped = candidates
            .GroupBy(c => c.Lesson.LessonId)
            .Select(g =>
            {
                var best = g.OrderByDescending(c => c.Priority).First();
                var allIssues = g.SelectMany(c => c.Issues).Distinct().ToArray();
                var allStrugglingConcepts = g.SelectMany(c => c.StrugglingConcepts).Distinct().ToArray();
                var hasMastery = g.Any(c => c.IsMasteryDriven);

                // If both issue and mastery driven, combine reasons
                var reason = best.Reason;
                if (hasMastery && allIssues.Length > 0 && !best.IsMasteryDriven)
                {
                    var masteryEntry = g.FirstOrDefault(c => c.IsMasteryDriven);
                    if (!string.IsNullOrEmpty(masteryEntry.Reason))
                        reason = $"{best.Reason} (also: {masteryEntry.Reason})";
                }

                return (best.Lesson, best.Priority, reason, allIssues, allStrugglingConcepts, hasMastery);
            })
            .OrderByDescending(c => c.Priority)
            .Take(this.config.MaxRecommendations)
            .ToList();

        // Build recommendations
        var recommendations = deduped.Select(c => new LessonRecommendation
        {
            Lesson = c.Lesson,
            Priority = c.Priority,
            Reason = c.reason,
            TriggeringIssues = c.allIssues,
            StrugglingConcepts = c.allStrugglingConcepts,
            IsMasteryDriven = c.hasMastery,
        }).ToList();

        lock (this.recommendationsLock)
        {
            this.currentRecommendations.Clear();
            this.currentRecommendations.AddRange(recommendations);
        }

        this.log?.Information("Training: Generated {Count} recommendations for {Job} (mastery-driven: {MasteryCount})",
            recommendations.Count, jobPrefix, recommendations.Count(r => r.IsMasteryDriven));
    }

    public void UpdateRecommendationsFromMastery(string jobPrefix)
    {
        if (!this.config.EnableRecommendations)
            return;

        if (string.IsNullOrEmpty(jobPrefix))
            return;

        var lessons = LessonRegistry.GetLessonsForJob(jobPrefix);
        if (lessons.Count == 0)
            return;

        var candidates = new List<(LessonDefinition Lesson, int Priority, string Reason, IssueType[] Issues, string[] StrugglingConcepts, bool IsMasteryDriven)>();

        // Only add mastery-driven candidates
        AddMasteryDrivenCandidates(jobPrefix, lessons, candidates);

        if (candidates.Count == 0)
        {
            this.log?.Debug("Training: No mastery-driven recommendations for {Job}", jobPrefix);
            return;
        }

        // Deduplicate by lesson (keep highest priority)
        var deduped = candidates
            .GroupBy(c => c.Lesson.LessonId)
            .Select(g =>
            {
                var best = g.OrderByDescending(c => c.Priority).First();
                var allStrugglingConcepts = g.SelectMany(c => c.StrugglingConcepts).Distinct().ToArray();
                return (best.Lesson, best.Priority, best.Reason, Array.Empty<IssueType>(), allStrugglingConcepts, true);
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
            TriggeringIssues = Array.Empty<IssueType>(),
            StrugglingConcepts = c.allStrugglingConcepts,
            IsMasteryDriven = true,
        }).ToList();

        lock (this.recommendationsLock)
        {
            this.currentRecommendations.Clear();
            this.currentRecommendations.AddRange(recommendations);
        }

        this.log?.Information("Training: Generated {Count} mastery-driven recommendations for {Job}", recommendations.Count, jobPrefix);
    }

    /// <summary>
    /// Adds mastery-driven recommendation candidates based on struggling concepts.
    /// </summary>
    private void AddMasteryDrivenCandidates(
        string jobPrefix,
        IReadOnlyList<LessonDefinition> lessons,
        List<(LessonDefinition Lesson, int Priority, string Reason, IssueType[] Issues, string[] StrugglingConcepts, bool IsMasteryDriven)> candidates)
    {
        var strugglingConcepts = GetStrugglingConcepts(jobPrefix);
        if (strugglingConcepts.Length == 0)
            return;

        foreach (var conceptId in strugglingConcepts)
        {
            // Get mastery data for this concept
            var successRate = this.config.ConceptMastery.TryGetValue(conceptId, out var data)
                ? data.SuccessRate
                : 0f;

            foreach (var lesson in lessons)
            {
                // Skip completed lessons
                if (this.config.CompletedLessons.Contains(lesson.LessonId))
                    continue;

                // Skip dismissed lessons
                if (this.config.DismissedRecommendations.Contains(lesson.LessonId))
                    continue;

                // Check if lesson covers this struggling concept
                if (!lesson.ConceptsCovered.Contains(conceptId))
                    continue;

                // Priority: Lower success = higher priority
                // Success 0% -> priority 100, Success 60% -> priority 70
                var priority = 70 + (int)((1f - successRate) * 30f);

                var reason = $"Struggling with {FormatConceptName(conceptId)} ({successRate:P0} success rate)";
                candidates.Add((lesson, priority, reason, Array.Empty<IssueType>(), new[] { conceptId }, true));
            }
        }
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
            // Melee DPS
            JobRegistry.Dragoon or JobRegistry.Lancer => "drg",
            JobRegistry.Ninja or JobRegistry.Rogue => "nin",
            JobRegistry.Samurai => "sam",
            JobRegistry.Monk or JobRegistry.Pugilist => "mnk",
            JobRegistry.Reaper => "rpr",
            JobRegistry.Viper => "vpr",
            // Ranged Physical DPS
            JobRegistry.Machinist => "mch",
            JobRegistry.Bard or JobRegistry.Archer => "brd",
            JobRegistry.Dancer => "dnc",
            // Casters
            JobRegistry.BlackMage or JobRegistry.Thaumaturge => "blm",
            JobRegistry.Summoner => "smn",
            JobRegistry.RedMage => "rdm",
            JobRegistry.Pictomancer => "pct",
            _ => null
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

    #region Skill Level Detection (v3.27.0) + Concept Mastery (v3.28.0)

    // Constants for skill level calculation (updated in v3.28.0 to include mastery)
    // Total weights: 30% + 20% + 20% + 5% + 25% = 100%
    private const float QuizPassRateWeight = 0.30f;      // Was 40%, reduced to make room for mastery
    private const float QuizQualityWeight = 0.20f;       // Was 25%, reduced to make room for mastery
    private const float LessonsCompletedWeight = 0.20f;  // Was 25%, reduced to make room for mastery
    private const float ConceptsLearnedWeight = 0.05f;   // Was 10%, reduced to make room for mastery
    private const float ConceptMasteryWeight = 0.25f;    // NEW in v3.28.0
    private const float EngagementPenalty = 0.25f;

    // Mastery thresholds
    private const int MasteryMinOpportunities = 10;     // Minimum opportunities to evaluate mastery
    private const float MasteredSuccessRate = 0.85f;    // >85% success rate = mastered
    private const float StrugglingSuccessRate = 0.60f;  // <60% success rate = struggling

    // Thresholds for skill levels
    private const float IntermediateThreshold = 40f;
    private const float AdvancedThreshold = 75f;

    // Concept familiarity thresholds
    private const int NewConceptThreshold = 2;
    private const int MasteredConceptThreshold = 10;

    public SkillLevelResult GetSkillLevel(string jobPrefix)
    {
        // Check for manual override first
        if (this.config.SkillLevelOverride.HasValue)
        {
            var overrideLevel = this.config.SkillLevelOverride.Value switch
            {
                SkillLevelOverride.Beginner => SkillLevel.Beginner,
                SkillLevelOverride.Intermediate => SkillLevel.Intermediate,
                SkillLevelOverride.Advanced => SkillLevel.Advanced,
                _ => SkillLevel.Beginner,
            };

            return new SkillLevelResult
            {
                Level = overrideLevel,
                CompositeScore = overrideLevel switch
                {
                    SkillLevel.Beginner => 20f,
                    SkillLevel.Intermediate => 60f,
                    SkillLevel.Advanced => 90f,
                    _ => 0f,
                },
                QuizPassRate = 0f,
                QuizQuality = 0f,
                LessonsCompleted = 0f,
                ConceptsLearned = 0f,
                ConceptMastery = 0f,
                EngagementPenaltyApplied = false,
                TotalQuizzes = 0,
                PassedQuizzes = 0,
                TotalLessons = 0,
                CompletedLessonsCount = 0,
            };
        }

        var lessons = LessonRegistry.GetLessonsForJob(jobPrefix);
        var concepts = GetConceptsForJob(jobPrefix);

        if (lessons.Count == 0)
        {
            return new SkillLevelResult
            {
                Level = SkillLevel.Beginner,
                CompositeScore = 0f,
                ConceptMastery = 0f,
            };
        }

        // Count quizzes and passes for this job
        var quizIds = lessons.Select(l => $"quiz.{l.LessonId}").ToArray();
        var totalQuizzes = quizIds.Length;
        var passedQuizzes = quizIds.Count(qid => this.config.CompletedQuizzes.Contains(qid));
        var quizPassRate = totalQuizzes > 0 ? (float)passedQuizzes / totalQuizzes * 100f : 0f;

        // Calculate average quiz quality (score on passed quizzes)
        var quizScores = quizIds
            .Where(qid => this.config.BestQuizAttempts.TryGetValue(qid, out var attempt) && attempt.Passed)
            .Select(qid =>
            {
                var attempt = this.config.BestQuizAttempts[qid];
                return attempt.TotalQuestions > 0 ? (float)attempt.Score / attempt.TotalQuestions * 100f : 0f;
            })
            .ToArray();
        var quizQuality = quizScores.Length > 0 ? quizScores.Average() : 0f;

        // Count completed lessons for this job
        var completedLessons = lessons.Count(l => this.config.CompletedLessons.Contains(l.LessonId));
        var lessonsCompletedRate = lessons.Count > 0 ? (float)completedLessons / lessons.Count * 100f : 0f;

        // Count learned concepts for this job
        var learnedConcepts = concepts.Count(c => this.config.LearnedConcepts.Contains(c));
        var conceptsLearnedRate = concepts.Length > 0 ? (float)learnedConcepts / concepts.Length * 100f : 0f;

        // Calculate concept mastery score (v3.28.0)
        var masteryResult = GetConceptMastery(jobPrefix);
        var conceptMasteryRate = masteryResult.MasteryScore;

        // Calculate composite score (updated weights in v3.28.0)
        var compositeScore =
            (quizPassRate * QuizPassRateWeight) +
            (quizQuality * QuizQualityWeight) +
            (lessonsCompletedRate * LessonsCompletedWeight) +
            (conceptsLearnedRate * ConceptsLearnedWeight) +
            (conceptMasteryRate * ConceptMasteryWeight);

        // Apply engagement penalty if lessons completed without taking quizzes
        var engagementPenaltyApplied = false;
        if (completedLessons > 0 && passedQuizzes == 0)
        {
            compositeScore *= (1f - EngagementPenalty);
            engagementPenaltyApplied = true;
        }

        // Determine skill level from composite score
        var level = compositeScore switch
        {
            >= AdvancedThreshold => SkillLevel.Advanced,
            >= IntermediateThreshold => SkillLevel.Intermediate,
            _ => SkillLevel.Beginner,
        };

        return new SkillLevelResult
        {
            Level = level,
            CompositeScore = compositeScore,
            QuizPassRate = quizPassRate,
            QuizQuality = (float)quizQuality,
            LessonsCompleted = lessonsCompletedRate,
            ConceptsLearned = conceptsLearnedRate,
            ConceptMastery = conceptMasteryRate,
            EngagementPenaltyApplied = engagementPenaltyApplied,
            TotalQuizzes = totalQuizzes,
            PassedQuizzes = passedQuizzes,
            TotalLessons = lessons.Count,
            CompletedLessonsCount = completedLessons,
        };
    }

    public ExplanationVerbosity GetEffectiveVerbosity(ActionExplanation explanation, string jobPrefix)
    {
        // If adaptive explanations are disabled, use the configured verbosity
        if (!this.config.EnableAdaptiveExplanations)
        {
            return this.config.Verbosity;
        }

        // Critical priority always gets detailed explanations (emergency override)
        if (explanation.Priority == ExplanationPriority.Critical)
        {
            return ExplanationVerbosity.Detailed;
        }

        var skillLevel = GetSkillLevel(jobPrefix);
        var baseVerbosity = skillLevel.Level switch
        {
            SkillLevel.Beginner => ExplanationVerbosity.Detailed,
            SkillLevel.Intermediate => ExplanationVerbosity.Normal,
            SkillLevel.Advanced => ExplanationVerbosity.Minimal,
            _ => this.config.Verbosity,
        };

        // Apply concept familiarity adjustment
        if (!string.IsNullOrEmpty(explanation.ConceptId))
        {
            var exposureCount = GetConceptExposureCount(explanation.ConceptId);

            // New concept (0-2 exposures): boost verbosity +1 level
            if (exposureCount <= NewConceptThreshold)
            {
                baseVerbosity = BoostVerbosity(baseVerbosity);
            }
            // Mastered concept (10+ exposures) for Advanced players: reduce verbosity -1 level
            else if (exposureCount >= MasteredConceptThreshold && skillLevel.Level == SkillLevel.Advanced)
            {
                baseVerbosity = ReduceVerbosity(baseVerbosity);
            }
        }

        return baseVerbosity;
    }

    public int GetConceptExposureCount(string conceptId)
    {
        if (string.IsNullOrEmpty(conceptId))
            return 0;

        return this.config.ConceptExposureCount.TryGetValue(conceptId, out var count) ? count : 0;
    }

    private static ExplanationVerbosity BoostVerbosity(ExplanationVerbosity verbosity)
    {
        return verbosity switch
        {
            ExplanationVerbosity.Minimal => ExplanationVerbosity.Normal,
            ExplanationVerbosity.Normal => ExplanationVerbosity.Detailed,
            ExplanationVerbosity.Detailed => ExplanationVerbosity.Detailed, // Can't go higher
            _ => verbosity,
        };
    }

    private static ExplanationVerbosity ReduceVerbosity(ExplanationVerbosity verbosity)
    {
        return verbosity switch
        {
            ExplanationVerbosity.Detailed => ExplanationVerbosity.Normal,
            ExplanationVerbosity.Normal => ExplanationVerbosity.Minimal,
            ExplanationVerbosity.Minimal => ExplanationVerbosity.Minimal, // Can't go lower
            _ => verbosity,
        };
    }

    #endregion

    #region Concept Mastery (v3.28.0)

    public void RecordConceptApplication(string conceptId, bool wasSuccessful, string? reason = null)
    {
        if (string.IsNullOrEmpty(conceptId))
            return;

        if (!this.config.ConceptMastery.TryGetValue(conceptId, out var data))
        {
            data = new Config.ConceptMasteryData();
            this.config.ConceptMastery[conceptId] = data;
        }

        data.Opportunities++;
        if (wasSuccessful)
        {
            data.Successes++;

            // Update spaced repetition retention (v4.0.0)
            this.spacedRepetitionService?.RecordSuccess(conceptId);
        }

        data.LastApplied = DateTime.Now;

        var outcomeStr = wasSuccessful ? "success" : "failure";
        var reasonStr = string.IsNullOrEmpty(reason) ? "" : $" - {reason}";
        this.log?.Debug("Training: Concept {ConceptId} application: {Outcome}{Reason} ({Successes}/{Opportunities})",
            conceptId, outcomeStr, reasonStr, data.Successes, data.Opportunities);
    }

    public ConceptMasteryResult GetConceptMastery(string jobPrefix)
    {
        var concepts = GetConceptsForJob(jobPrefix);
        if (concepts.Length == 0)
        {
            return new ConceptMasteryResult
            {
                MasteryScore = 0f,
                TotalConcepts = 0,
            };
        }

        var mastered = new List<string>();
        var struggling = new List<string>();
        var developing = new List<string>();
        var totalSuccessRate = 0f;
        var conceptsWithData = 0;

        foreach (var conceptId in concepts)
        {
            if (!this.config.ConceptMastery.TryGetValue(conceptId, out var data) || data.Opportunities == 0)
            {
                developing.Add(conceptId);
                continue;
            }

            if (data.Opportunities < MasteryMinOpportunities)
            {
                developing.Add(conceptId);
                continue;
            }

            // Has enough opportunities to evaluate
            conceptsWithData++;
            totalSuccessRate += data.SuccessRate;

            if (data.SuccessRate >= MasteredSuccessRate)
            {
                mastered.Add(conceptId);
            }
            else if (data.SuccessRate < StrugglingSuccessRate)
            {
                struggling.Add(conceptId);
            }
            // Concepts between struggling and mastered are neither category
        }

        // Calculate mastery score:
        // - Base: Average success rate of concepts with enough data (0-100 scale)
        // - Bonus: +10 for each mastered concept (capped at 30)
        // - Penalty: -5 for each struggling concept (capped at -15)
        var masteryScore = 0f;
        if (conceptsWithData > 0)
        {
            masteryScore = totalSuccessRate / conceptsWithData * 100f;
            masteryScore += Math.Min(mastered.Count * 10f, 30f);
            masteryScore -= Math.Min(struggling.Count * 5f, 15f);
            masteryScore = Math.Clamp(masteryScore, 0f, 100f);
        }

        return new ConceptMasteryResult
        {
            MasteredConcepts = mastered.ToArray(),
            StrugglingConcepts = struggling.ToArray(),
            DevelopingConcepts = developing.ToArray(),
            MasteryScore = masteryScore,
            TotalConcepts = concepts.Length,
        };
    }

    public string[] GetStrugglingConcepts(string jobPrefix)
    {
        return GetConceptMastery(jobPrefix).StrugglingConcepts;
    }

    public string[] GetMasteredConcepts(string jobPrefix)
    {
        return GetConceptMastery(jobPrefix).MasteredConcepts;
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
