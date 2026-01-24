namespace Olympus.Services.Training;

using System;

/// <summary>
/// Represents a quiz that tests understanding of a lesson's concepts.
/// </summary>
public sealed class QuizDefinition
{
    /// <summary>
    /// Unique quiz identifier (e.g., "whm.lesson_1.quiz").
    /// </summary>
    public string QuizId { get; init; } = string.Empty;

    /// <summary>
    /// The lesson ID this quiz tests (e.g., "whm.lesson_1").
    /// </summary>
    public string LessonId { get; init; } = string.Empty;

    /// <summary>
    /// Quiz title (e.g., "Quiz: Healer Fundamentals").
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Number of correct answers required to pass (default: 4 out of 5).
    /// </summary>
    public int PassingScore { get; init; } = 4;

    /// <summary>
    /// The questions in this quiz.
    /// </summary>
    public QuizQuestion[] Questions { get; init; } = Array.Empty<QuizQuestion>();
}

/// <summary>
/// Represents a single quiz question with scenario-based context.
/// </summary>
public sealed class QuizQuestion
{
    /// <summary>
    /// Unique question identifier (e.g., "whm.lesson_1.q1").
    /// </summary>
    public string QuestionId { get; init; } = string.Empty;

    /// <summary>
    /// The concept being tested (e.g., "whm.emergency_healing").
    /// </summary>
    public string ConceptId { get; init; } = string.Empty;

    /// <summary>
    /// The scenario description setting up the question.
    /// </summary>
    public string Scenario { get; init; } = string.Empty;

    /// <summary>
    /// The actual question being asked.
    /// </summary>
    public string Question { get; init; } = string.Empty;

    /// <summary>
    /// The available answer options (typically 4).
    /// </summary>
    public string[] Options { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Index of the correct answer (0-based).
    /// </summary>
    public int CorrectIndex { get; init; }

    /// <summary>
    /// Explanation of why the correct answer is correct.
    /// </summary>
    public string Explanation { get; init; } = string.Empty;
}

/// <summary>
/// Represents a single attempt at completing a quiz.
/// </summary>
public sealed class QuizAttempt
{
    /// <summary>
    /// The quiz that was attempted.
    /// </summary>
    public string QuizId { get; init; } = string.Empty;

    /// <summary>
    /// When this attempt was made.
    /// </summary>
    public DateTime AttemptedAt { get; init; }

    /// <summary>
    /// The answers selected for each question (-1 = not answered).
    /// </summary>
    public int[] SelectedAnswers { get; init; } = Array.Empty<int>();

    /// <summary>
    /// Number of correct answers.
    /// </summary>
    public int Score { get; init; }

    /// <summary>
    /// Whether the quiz was passed.
    /// </summary>
    public bool Passed { get; init; }
}
