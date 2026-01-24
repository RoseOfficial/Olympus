namespace Olympus.Config;

using System;
using System.Collections.Generic;

/// <summary>
/// Configuration for Training Mode - intelligent coaching and decision explanations.
/// </summary>
public sealed class TrainingConfig
{
    /// <summary>
    /// Master toggle for training mode. When enabled, captures decision explanations.
    /// </summary>
    public bool EnableTraining { get; set; } = false;

    /// <summary>
    /// Whether the training window is currently visible.
    /// </summary>
    public bool TrainingWindowVisible { get; set; } = false;

    /// <summary>
    /// Maximum number of explanations to keep in memory (oldest are discarded).
    /// </summary>
    private int _maxExplanationsToShow = 15;
    public int MaxExplanationsToShow
    {
        get => _maxExplanationsToShow;
        set => _maxExplanationsToShow = Math.Clamp(value, 5, 50);
    }

    /// <summary>
    /// Show alternative actions that were considered but not chosen.
    /// </summary>
    public bool ShowAlternatives { get; set; } = true;

    /// <summary>
    /// Show learning tips with explanations.
    /// </summary>
    public bool ShowTips { get; set; } = true;

    /// <summary>
    /// How detailed explanations should be.
    /// </summary>
    public ExplanationVerbosity Verbosity { get; set; } = ExplanationVerbosity.Normal;

    /// <summary>
    /// Filter explanations by minimum priority level.
    /// </summary>
    public ExplanationPriority MinimumPriorityToShow { get; set; } = ExplanationPriority.Low;

    /// <summary>
    /// Concepts the user has marked as "learned" (shown checkmark in UI).
    /// </summary>
    public HashSet<string> LearnedConcepts { get; set; } = new();

    /// <summary>
    /// Lessons the user has marked as "completed" (shown in Lessons tab).
    /// </summary>
    public HashSet<string> CompletedLessons { get; set; } = new();

    /// <summary>
    /// How many times each concept has been shown to the user.
    /// Used to identify concepts that may need more attention.
    /// </summary>
    public Dictionary<string, int> ConceptExposureCount { get; set; } = new();

    /// <summary>
    /// Section visibility toggles for the training window.
    /// </summary>
    public Dictionary<string, bool> SectionVisibility { get; set; } = new()
    {
        { "CurrentAction", true },
        { "DecisionFactors", true },
        { "Alternatives", true },
        { "Tips", true },
        { "RecentHistory", true },
    };

    #region Skill Quizzes (v3.11.0)

    /// <summary>
    /// Quiz IDs that have been passed.
    /// </summary>
    public HashSet<string> CompletedQuizzes { get; set; } = new();

    /// <summary>
    /// Best attempt for each quiz (keyed by quiz ID).
    /// </summary>
    public Dictionary<string, QuizAttemptData> BestQuizAttempts { get; set; } = new();

    /// <summary>
    /// Whether to require passing the quiz to progress to the next lesson.
    /// When false, quizzes are optional but encouraged.
    /// </summary>
    public bool RequireQuizToProgress { get; set; } = false;

    #endregion

    #region Adaptive Explanations (v3.27.0)

    /// <summary>
    /// Whether to automatically adjust explanation verbosity based on skill level.
    /// When enabled, beginners see detailed explanations while advanced players see minimal.
    /// </summary>
    public bool EnableAdaptiveExplanations { get; set; } = true;

    /// <summary>
    /// Optional override for skill level detection. When set, uses this level instead of calculating.
    /// </summary>
    public SkillLevelOverride? SkillLevelOverride { get; set; }

    #endregion

    #region Concept Mastery (v3.28.0)

    /// <summary>
    /// Tracks mastery data for each concept (opportunities, successes).
    /// Key is the concept ID (e.g., "whm.emergency_healing").
    /// </summary>
    public Dictionary<string, ConceptMasteryData> ConceptMastery { get; set; } = new();

    #endregion

    #region Lesson Recommendations (v3.10.0)

    /// <summary>
    /// Whether to generate lesson recommendations based on fight performance.
    /// </summary>
    public bool EnableRecommendations { get; set; } = true;

    /// <summary>
    /// Maximum number of recommendations to show at once.
    /// </summary>
    private int _maxRecommendations = 3;
    public int MaxRecommendations
    {
        get => _maxRecommendations;
        set => _maxRecommendations = Math.Clamp(value, 1, 5);
    }

    /// <summary>
    /// Lesson IDs that the user has dismissed (won't show as recommendations).
    /// </summary>
    public HashSet<string> DismissedRecommendations { get; set; } = new();

    #endregion
}

/// <summary>
/// Controls how detailed the explanations are.
/// </summary>
public enum ExplanationVerbosity
{
    /// <summary>
    /// Brief explanations - just the essential info.
    /// </summary>
    Minimal,

    /// <summary>
    /// Standard explanations with key factors.
    /// </summary>
    Normal,

    /// <summary>
    /// Detailed explanations with all decision factors and numbers.
    /// </summary>
    Detailed,
}

/// <summary>
/// Serializable quiz attempt data for config persistence.
/// </summary>
public sealed class QuizAttemptData
{
    /// <summary>
    /// When this attempt was made.
    /// </summary>
    public DateTime AttemptedAt { get; set; }

    /// <summary>
    /// Number of correct answers.
    /// </summary>
    public int Score { get; set; }

    /// <summary>
    /// Total number of questions.
    /// </summary>
    public int TotalQuestions { get; set; }

    /// <summary>
    /// Whether the quiz was passed.
    /// </summary>
    public bool Passed { get; set; }
}

/// <summary>
/// How important an explanation is to show to the user.
/// </summary>
public enum ExplanationPriority
{
    /// <summary>
    /// Routine actions (basic DPS, simple heals).
    /// </summary>
    Low = 0,

    /// <summary>
    /// Standard priority actions (most healing decisions).
    /// </summary>
    Normal = 1,

    /// <summary>
    /// Important decisions (cooldown usage, emergency heals).
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical decisions (emergency response, life-saving actions).
    /// </summary>
    Critical = 3,
}

/// <summary>
/// Manual override for skill level (if user wants to force a level).
/// </summary>
public enum SkillLevelOverride
{
    /// <summary>
    /// Force beginner level explanations.
    /// </summary>
    Beginner,

    /// <summary>
    /// Force intermediate level explanations.
    /// </summary>
    Intermediate,

    /// <summary>
    /// Force advanced level explanations.
    /// </summary>
    Advanced,
}

/// <summary>
/// Tracks mastery through opportunities and successes for a concept.
/// Mastery is demonstrated by successful application in combat, not just exposure.
/// </summary>
public sealed class ConceptMasteryData
{
    /// <summary>
    /// Number of times an opportunity to apply this concept occurred.
    /// </summary>
    public int Opportunities { get; set; }

    /// <summary>
    /// Number of times the concept was successfully applied.
    /// </summary>
    public int Successes { get; set; }

    /// <summary>
    /// When the concept was last applied (success or failure).
    /// </summary>
    public DateTime LastApplied { get; set; }

    /// <summary>
    /// Success rate as a percentage (0.0 to 1.0).
    /// Returns 0 if no opportunities have occurred.
    /// </summary>
    public float SuccessRate => Opportunities > 0 ? (float)Successes / Opportunities : 0f;
}
