namespace Olympus.Services.Training;

using System.Collections.Generic;

/// <summary>
/// Service for Training Mode - captures and explains rotation decisions in real-time.
/// </summary>
public interface ITrainingService
{
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
