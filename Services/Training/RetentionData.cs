namespace Olympus.Services.Training;

using System;

/// <summary>
/// Retention status indicating how urgently a concept needs review.
/// </summary>
public enum RetentionStatus
{
    /// <summary>
    /// Fresh in memory (>80% retention).
    /// </summary>
    Fresh,

    /// <summary>
    /// Good retention (60-80%).
    /// </summary>
    Good,

    /// <summary>
    /// Retention declining, review recommended (40-60%).
    /// </summary>
    ReviewRecommended,

    /// <summary>
    /// Low retention, needs review soon (20-40%).
    /// </summary>
    NeedsReview,

    /// <summary>
    /// Very low retention, needs re-learning (<20%).
    /// </summary>
    NeedsRelearning,

    /// <summary>
    /// Never practiced.
    /// </summary>
    NotPracticed,
}

/// <summary>
/// Tracks retention data for a single concept using spaced repetition principles.
/// Applies a forgetting curve to estimate current knowledge retention.
/// </summary>
public sealed class ConceptRetentionData
{
    /// <summary>
    /// The concept ID this retention data tracks.
    /// </summary>
    public string ConceptId { get; set; } = string.Empty;

    /// <summary>
    /// When the concept was last successfully demonstrated in combat.
    /// </summary>
    public DateTime LastPracticed { get; set; } = DateTime.MinValue;

    /// <summary>
    /// Number of successful demonstrations.
    /// Higher values slow the decay rate (more reinforcement = longer retention).
    /// </summary>
    public int SuccessfulDemonstrations { get; set; }

    /// <summary>
    /// Number of times the concept was reviewed (quiz taken).
    /// Reviews also reinforce retention.
    /// </summary>
    public int ReviewCount { get; set; }

    /// <summary>
    /// Whether the concept has ever been practiced.
    /// </summary>
    public bool HasBeenPracticed => LastPracticed > DateTime.MinValue;

    /// <summary>
    /// Days since last practice.
    /// </summary>
    public double DaysSinceLastPractice => HasBeenPracticed
        ? (DateTime.Now - LastPracticed).TotalDays
        : double.MaxValue;

    /// <summary>
    /// Current retention score (0.0 to 1.0) based on forgetting curve.
    /// </summary>
    public float RetentionScore => CalculateRetention();

    /// <summary>
    /// Current retention status based on score.
    /// </summary>
    public RetentionStatus Status => GetStatus();

    /// <summary>
    /// Whether this concept needs review (below 40% retention threshold).
    /// </summary>
    public bool NeedsReview => Status >= RetentionStatus.NeedsReview;

    /// <summary>
    /// Whether this concept needs re-learning (below 20% retention).
    /// </summary>
    public bool NeedsRelearning => Status == RetentionStatus.NeedsRelearning;

    /// <summary>
    /// Calculates current retention using Ebbinghaus forgetting curve.
    /// The curve is modified by the number of successful demonstrations.
    /// </summary>
    /// <remarks>
    /// Base forgetting curve:
    /// - Day 1: 100% retention
    /// - Day 3: 80% retention
    /// - Day 7: 60% retention
    /// - Day 14: 40% retention
    /// - Day 30+: 20% retention
    ///
    /// Each successful demonstration adds a stability bonus that slows decay.
    /// </remarks>
    private float CalculateRetention()
    {
        if (!HasBeenPracticed)
            return 0f;

        var days = DaysSinceLastPractice;

        // Stability factor: each demonstration slows decay
        // More practice = slower forgetting
        var stabilityFactor = 1.0 + (Math.Min(SuccessfulDemonstrations, 20) * 0.1);

        // Apply stability to slow the effective day count
        var effectiveDays = days / stabilityFactor;

        // Ebbinghaus-inspired forgetting curve
        // R = e^(-t/S) where t is time and S is stability
        // Simplified to match the specified decay points
        var retention = effectiveDays switch
        {
            <= 1 => 1.0f,
            <= 3 => (float)(1.0 - ((effectiveDays - 1) * 0.1)), // 100% -> 80% over days 1-3
            <= 7 => (float)(0.80 - ((effectiveDays - 3) * 0.05)), // 80% -> 60% over days 3-7
            <= 14 => (float)(0.60 - ((effectiveDays - 7) * 0.0286)), // 60% -> 40% over days 7-14
            <= 30 => (float)(0.40 - ((effectiveDays - 14) * 0.0125)), // 40% -> 20% over days 14-30
            _ => 0.20f, // Floor at 20%
        };

        return Math.Clamp(retention, 0f, 1f);
    }

    private RetentionStatus GetStatus()
    {
        if (!HasBeenPracticed)
            return RetentionStatus.NotPracticed;

        var retention = RetentionScore;
        return retention switch
        {
            > 0.80f => RetentionStatus.Fresh,
            > 0.60f => RetentionStatus.Good,
            > 0.40f => RetentionStatus.ReviewRecommended,
            > 0.20f => RetentionStatus.NeedsReview,
            _ => RetentionStatus.NeedsRelearning,
        };
    }

    /// <summary>
    /// Records a successful demonstration of this concept.
    /// </summary>
    public void RecordSuccess()
    {
        LastPracticed = DateTime.Now;
        SuccessfulDemonstrations++;
    }

    /// <summary>
    /// Records a review (quiz taken) for this concept.
    /// Reviews provide partial reinforcement.
    /// </summary>
    public void RecordReview()
    {
        ReviewCount++;
        // Partial credit: reviews refresh retention but not as much as practice
        if (HasBeenPracticed)
        {
            // Move last practiced forward by half the elapsed time
            var daysSincePractice = DaysSinceLastPractice;
            var refreshDays = Math.Min(daysSincePractice * 0.5, 7);
            LastPracticed = LastPracticed.AddDays(refreshDays);
        }
    }

    /// <summary>
    /// Estimated days until retention drops below the review threshold (40%).
    /// </summary>
    public double DaysUntilReviewNeeded
    {
        get
        {
            if (!HasBeenPracticed)
                return 0;

            var retention = RetentionScore;
            if (retention <= 0.40f)
                return 0;

            // Estimate based on current decay rate
            var stabilityFactor = 1.0 + (Math.Min(SuccessfulDemonstrations, 20) * 0.1);
            var targetDays = 14 * stabilityFactor; // Day 14 is when retention hits 40%
            var daysRemaining = targetDays - DaysSinceLastPractice;
            return Math.Max(0, daysRemaining);
        }
    }

    /// <summary>
    /// Gets a human-readable description of the retention status.
    /// </summary>
    public string GetStatusDescription() => Status switch
    {
        RetentionStatus.Fresh => "Fresh in memory",
        RetentionStatus.Good => "Good retention",
        RetentionStatus.ReviewRecommended => "Review recommended",
        RetentionStatus.NeedsReview => "Needs review soon",
        RetentionStatus.NeedsRelearning => "Needs re-learning",
        RetentionStatus.NotPracticed => "Not yet practiced",
        _ => "Unknown",
    };

    /// <summary>
    /// Gets a short label for display.
    /// </summary>
    public string GetShortLabel() => Status switch
    {
        RetentionStatus.Fresh => "Fresh",
        RetentionStatus.Good => "Good",
        RetentionStatus.ReviewRecommended => "Review",
        RetentionStatus.NeedsReview => "Low",
        RetentionStatus.NeedsRelearning => "Relearn",
        RetentionStatus.NotPracticed => "New",
        _ => "?",
    };
}
