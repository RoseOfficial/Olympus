namespace Olympus.Services.Training;

using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Plugin.Services;
using Olympus.Config;

/// <summary>
/// Service for managing spaced repetition and knowledge retention tracking.
/// Applies forgetting curves to identify concepts that need review.
/// </summary>
public sealed class SpacedRepetitionService
{
    private readonly TrainingConfig config;
    private readonly ITrainingService trainingService;
    private readonly IPluginLog? log;

    private readonly object retentionLock = new();

    /// <summary>
    /// Default threshold below which review is recommended (40%).
    /// </summary>
    public const float DefaultReviewThreshold = 0.40f;

    public SpacedRepetitionService(
        TrainingConfig config,
        ITrainingService trainingService,
        IPluginLog? log = null)
    {
        this.config = config;
        this.trainingService = trainingService;
        this.log = log;
    }

    /// <summary>
    /// Whether spaced repetition tracking is enabled.
    /// </summary>
    public bool IsEnabled => config.EnableSpacedRepetition && config.EnableTraining;

    /// <summary>
    /// Gets the current review threshold (retention level that triggers review suggestion).
    /// </summary>
    public float ReviewThreshold => config.SpacedRepetitionReviewThreshold;

    /// <summary>
    /// Gets retention data for all tracked concepts.
    /// </summary>
    public IReadOnlyDictionary<string, ConceptRetentionData> AllRetentionData
    {
        get
        {
            lock (this.retentionLock)
            {
                return new Dictionary<string, ConceptRetentionData>(config.ConceptRetention);
            }
        }
    }

    /// <summary>
    /// Gets retention data for a specific concept.
    /// Returns null if the concept hasn't been tracked.
    /// </summary>
    public ConceptRetentionData? GetRetentionData(string conceptId)
    {
        if (string.IsNullOrEmpty(conceptId))
            return null;

        lock (this.retentionLock)
        {
            return config.ConceptRetention.TryGetValue(conceptId, out var data) ? data : null;
        }
    }

    /// <summary>
    /// Gets or creates retention data for a concept.
    /// </summary>
    private ConceptRetentionData GetOrCreateRetentionData(string conceptId)
    {
        lock (this.retentionLock)
        {
            if (!config.ConceptRetention.TryGetValue(conceptId, out var data))
            {
                data = new ConceptRetentionData { ConceptId = conceptId };
                config.ConceptRetention[conceptId] = data;
            }

            return data;
        }
    }

    /// <summary>
    /// Records a successful demonstration of a concept (used in combat correctly).
    /// </summary>
    public void RecordSuccess(string conceptId)
    {
        if (!IsEnabled || string.IsNullOrEmpty(conceptId))
            return;

        var data = GetOrCreateRetentionData(conceptId);
        data.RecordSuccess();

        this.log?.Debug(
            "Spaced repetition: Recorded success for {ConceptId}, retention now {Retention:P0}",
            conceptId,
            data.RetentionScore);
    }

    /// <summary>
    /// Records that a concept was reviewed (quiz taken).
    /// </summary>
    public void RecordReview(string conceptId)
    {
        if (!IsEnabled || string.IsNullOrEmpty(conceptId))
            return;

        var data = GetOrCreateRetentionData(conceptId);
        data.RecordReview();

        this.log?.Debug(
            "Spaced repetition: Recorded review for {ConceptId}, retention now {Retention:P0}",
            conceptId,
            data.RetentionScore);
    }

    /// <summary>
    /// Gets concepts that need review (retention below threshold).
    /// </summary>
    /// <param name="jobPrefix">Optional job prefix to filter by (e.g., "whm").</param>
    /// <returns>Concepts needing review, ordered by urgency (lowest retention first).</returns>
    public IEnumerable<ConceptRetentionData> GetConceptsNeedingReview(string? jobPrefix = null)
    {
        if (!IsEnabled)
            return Enumerable.Empty<ConceptRetentionData>();

        lock (this.retentionLock)
        {
            var query = config.ConceptRetention.Values
                .Where(d => d.HasBeenPracticed && d.RetentionScore < ReviewThreshold);

            if (!string.IsNullOrEmpty(jobPrefix))
            {
                var prefix = jobPrefix.ToLowerInvariant() + ".";
                query = query.Where(d => d.ConceptId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }

            return query
                .OrderBy(d => d.RetentionScore)
                .ToList();
        }
    }

    /// <summary>
    /// Gets concepts that need re-learning (very low retention, below 20%).
    /// </summary>
    public IEnumerable<ConceptRetentionData> GetConceptsNeedingRelearning(string? jobPrefix = null)
    {
        if (!IsEnabled)
            return Enumerable.Empty<ConceptRetentionData>();

        lock (this.retentionLock)
        {
            var query = config.ConceptRetention.Values
                .Where(d => d.NeedsRelearning);

            if (!string.IsNullOrEmpty(jobPrefix))
            {
                var prefix = jobPrefix.ToLowerInvariant() + ".";
                query = query.Where(d => d.ConceptId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }

            return query
                .OrderBy(d => d.RetentionScore)
                .ToList();
        }
    }

    /// <summary>
    /// Gets concepts with the best retention (for encouragement/celebration).
    /// </summary>
    public IEnumerable<ConceptRetentionData> GetStrongestConcepts(string? jobPrefix = null, int count = 5)
    {
        if (!IsEnabled)
            return Enumerable.Empty<ConceptRetentionData>();

        lock (this.retentionLock)
        {
            var query = config.ConceptRetention.Values
                .Where(d => d.HasBeenPracticed);

            if (!string.IsNullOrEmpty(jobPrefix))
            {
                var prefix = jobPrefix.ToLowerInvariant() + ".";
                query = query.Where(d => d.ConceptId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }

            return query
                .OrderByDescending(d => d.RetentionScore)
                .Take(count)
                .ToList();
        }
    }

    /// <summary>
    /// Gets a summary of retention status for a job.
    /// </summary>
    public (int Fresh, int Good, int ReviewRecommended, int NeedsReview, int NeedsRelearning, int NotPracticed)
        GetRetentionSummary(string? jobPrefix = null)
    {
        if (!IsEnabled)
            return (0, 0, 0, 0, 0, 0);

        lock (this.retentionLock)
        {
            var concepts = config.ConceptRetention.Values.AsEnumerable();

            if (!string.IsNullOrEmpty(jobPrefix))
            {
                var prefix = jobPrefix.ToLowerInvariant() + ".";
                concepts = concepts.Where(d => d.ConceptId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }

            var grouped = concepts.GroupBy(d => d.Status).ToDictionary(g => g.Key, g => g.Count());

            return (
                grouped.GetValueOrDefault(RetentionStatus.Fresh, 0),
                grouped.GetValueOrDefault(RetentionStatus.Good, 0),
                grouped.GetValueOrDefault(RetentionStatus.ReviewRecommended, 0),
                grouped.GetValueOrDefault(RetentionStatus.NeedsReview, 0),
                grouped.GetValueOrDefault(RetentionStatus.NeedsRelearning, 0),
                grouped.GetValueOrDefault(RetentionStatus.NotPracticed, 0));
        }
    }

    /// <summary>
    /// Gets the overall retention score for a job (average of all practiced concepts).
    /// </summary>
    public float GetOverallRetention(string? jobPrefix = null)
    {
        if (!IsEnabled)
            return 0f;

        lock (this.retentionLock)
        {
            var concepts = config.ConceptRetention.Values
                .Where(d => d.HasBeenPracticed)
                .AsEnumerable();

            if (!string.IsNullOrEmpty(jobPrefix))
            {
                var prefix = jobPrefix.ToLowerInvariant() + ".";
                concepts = concepts.Where(d => d.ConceptId.StartsWith(prefix, StringComparison.OrdinalIgnoreCase));
            }

            var list = concepts.ToList();
            if (list.Count == 0)
                return 0f;

            return list.Average(d => d.RetentionScore);
        }
    }

    /// <summary>
    /// Suggests quiz IDs for concepts needing review.
    /// Maps concept IDs to their associated quiz IDs.
    /// </summary>
    public IEnumerable<string> SuggestReviewQuizzes(string? jobPrefix = null, int maxQuizzes = 3)
    {
        var needingReview = GetConceptsNeedingReview(jobPrefix);

        // Map concepts to quizzes
        // Quiz IDs typically follow the pattern "{job}.quiz.{number}"
        // We'll suggest quizzes for the job that has concepts needing review
        var conceptsByJob = needingReview
            .GroupBy(d => d.ConceptId.Split('.').FirstOrDefault() ?? string.Empty)
            .OrderByDescending(g => g.Count());

        var suggestedQuizzes = new List<string>();

        foreach (var jobGroup in conceptsByJob)
        {
            if (suggestedQuizzes.Count >= maxQuizzes)
                break;

            var job = jobGroup.Key;
            if (string.IsNullOrEmpty(job))
                continue;

            // Suggest quizzes for this job
            // Start with basic quizzes (1-3), then intermediate (4-5), then advanced (6-7)
            for (var i = 1; i <= 7 && suggestedQuizzes.Count < maxQuizzes; i++)
            {
                var quizId = $"{job}.quiz.{i}";

                // Only suggest if not already completed recently
                if (!config.CompletedQuizzes.Contains(quizId) ||
                    ShouldRetakeQuiz(quizId))
                {
                    suggestedQuizzes.Add(quizId);
                }
            }
        }

        return suggestedQuizzes;
    }

    /// <summary>
    /// Determines if a completed quiz should be retaken based on time elapsed.
    /// </summary>
    private bool ShouldRetakeQuiz(string quizId)
    {
        if (!config.BestQuizAttempts.TryGetValue(quizId, out var attempt))
            return true;

        // Suggest retaking if more than 14 days have passed
        var daysSinceAttempt = (DateTime.Now - attempt.AttemptedAt).TotalDays;
        return daysSinceAttempt >= 14;
    }

    /// <summary>
    /// Gets the number of concepts currently needing attention for a job.
    /// </summary>
    public int GetConceptsNeedingAttentionCount(string? jobPrefix = null)
    {
        return GetConceptsNeedingReview(jobPrefix).Count() +
               GetConceptsNeedingRelearning(jobPrefix).Count();
    }

    /// <summary>
    /// Checks if there are any concepts urgently needing review.
    /// </summary>
    public bool HasUrgentReviews(string? jobPrefix = null)
    {
        return GetConceptsNeedingRelearning(jobPrefix).Any();
    }

    /// <summary>
    /// Gets a message summarizing the current review status.
    /// </summary>
    public string GetReviewStatusMessage(string? jobPrefix = null)
    {
        var needsRelearning = GetConceptsNeedingRelearning(jobPrefix).Count();
        var needsReview = GetConceptsNeedingReview(jobPrefix).Count();

        if (needsRelearning > 0)
        {
            return $"{needsRelearning} concept{(needsRelearning != 1 ? "s" : "")} need re-learning!";
        }

        if (needsReview > 0)
        {
            return $"{needsReview} concept{(needsReview != 1 ? "s" : "")} due for review.";
        }

        return "All concepts are fresh!";
    }
}
