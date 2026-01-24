namespace Olympus.Services.Training;

using System.Collections.Generic;
using Olympus.Config;
using Olympus.Services.Analytics;

/// <summary>
/// Service for Training Mode - captures and explains rotation decisions in real-time.
/// </summary>
public interface ITrainingService
{
    #region Skill Level Detection (v3.27.0)

    /// <summary>
    /// Gets the detected skill level for a specific job.
    /// </summary>
    /// <param name="jobPrefix">The job prefix (e.g., "whm", "sch", "drg").</param>
    SkillLevelResult GetSkillLevel(string jobPrefix);

    /// <summary>
    /// Gets the effective verbosity for an explanation based on skill level and concept familiarity.
    /// </summary>
    /// <param name="explanation">The explanation to evaluate.</param>
    /// <param name="jobPrefix">The job prefix for skill level lookup.</param>
    ExplanationVerbosity GetEffectiveVerbosity(ActionExplanation explanation, string jobPrefix);

    /// <summary>
    /// Gets the number of times a concept has been seen by the user.
    /// </summary>
    /// <param name="conceptId">The concept ID to check.</param>
    int GetConceptExposureCount(string conceptId);

    #endregion

    #region Lesson Management

    /// <summary>
    /// Gets all lessons for a specific job.
    /// </summary>
    /// <param name="jobPrefix">The job prefix (e.g., "whm", "sch", "ast", "sge").</param>
    IReadOnlyList<LessonDefinition> GetLessonsForJob(string jobPrefix);

    /// <summary>
    /// Checks if a lesson has been completed.
    /// </summary>
    /// <param name="lessonId">The lesson ID to check.</param>
    bool IsLessonComplete(string lessonId);

    /// <summary>
    /// Marks a lesson as completed.
    /// </summary>
    /// <param name="lessonId">The lesson ID to mark as complete.</param>
    void MarkLessonComplete(string lessonId);

    /// <summary>
    /// Checks if all prerequisites for a lesson have been completed.
    /// </summary>
    /// <param name="lessonId">The lesson ID to check prerequisites for.</param>
    bool AreLessonPrerequisitesMet(string lessonId);

    #endregion

    #region Lesson Recommendations

    /// <summary>
    /// Gets the current lesson recommendations based on recent fight performance.
    /// </summary>
    IReadOnlyList<LessonRecommendation> GetRecommendations();

    /// <summary>
    /// Updates recommendations based on a completed fight session.
    /// </summary>
    /// <param name="session">The completed fight session to analyze.</param>
    void UpdateRecommendations(FightSession session);

    /// <summary>
    /// Dismisses a recommendation so it won't appear again.
    /// </summary>
    /// <param name="lessonId">The lesson ID to dismiss.</param>
    void DismissRecommendation(string lessonId);

    /// <summary>
    /// Clears all dismissed recommendations, allowing them to appear again.
    /// </summary>
    void ClearDismissedRecommendations();

    #endregion

    #region Skill Quizzes

    /// <summary>
    /// Gets the quiz definition for a lesson.
    /// </summary>
    /// <param name="lessonId">The lesson ID to get the quiz for.</param>
    QuizDefinition? GetQuizForLesson(string lessonId);

    /// <summary>
    /// Checks if a quiz has been passed.
    /// </summary>
    /// <param name="quizId">The quiz ID to check.</param>
    bool IsQuizPassed(string quizId);

    /// <summary>
    /// Gets the best attempt for a quiz.
    /// </summary>
    /// <param name="quizId">The quiz ID to get the best attempt for.</param>
    QuizAttempt? GetBestAttempt(string quizId);

    /// <summary>
    /// Records a quiz attempt.
    /// </summary>
    /// <param name="attempt">The attempt to record.</param>
    void RecordQuizAttempt(QuizAttempt attempt);

    #endregion

    /// <summary>
    /// Whether training mode is currently enabled.
    /// </summary>
    bool IsTrainingEnabled { get; set; }

    /// <summary>
    /// Whether the player is currently in combat.
    /// </summary>
    bool IsInCombat { get; }

    /// <summary>
    /// Recent action explanations (most recent first), capped at configured max.
    /// </summary>
    IReadOnlyList<ActionExplanation> RecentExplanations { get; }

    /// <summary>
    /// The most recent explanation (if any).
    /// </summary>
    ActionExplanation? CurrentExplanation { get; }

    /// <summary>
    /// Records a decision with its explanation.
    /// Called from handlers after successfully executing an action.
    /// </summary>
    /// <param name="explanation">The explanation to record.</param>
    void RecordDecision(ActionExplanation explanation);

    /// <summary>
    /// Gets the current learning progress.
    /// </summary>
    LearningProgress GetProgress();

    /// <summary>
    /// Marks a concept as learned by the user.
    /// </summary>
    /// <param name="conceptId">The concept ID to mark as learned.</param>
    void MarkConceptLearned(string conceptId);

    /// <summary>
    /// Unmarks a concept as learned.
    /// </summary>
    /// <param name="conceptId">The concept ID to unmark.</param>
    void UnmarkConceptLearned(string conceptId);

    /// <summary>
    /// Clears all recorded explanations (but not learned concepts).
    /// </summary>
    void ClearExplanations();

    /// <summary>
    /// Called each frame to update combat state.
    /// </summary>
    void Update();
}
