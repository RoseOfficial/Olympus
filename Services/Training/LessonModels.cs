namespace Olympus.Services.Training;

using System;

/// <summary>
/// Represents a structured lesson for training mode.
/// </summary>
public sealed class LessonDefinition
{
    /// <summary>
    /// Unique lesson identifier (e.g., "whm.lesson_1").
    /// </summary>
    public string LessonId { get; init; } = string.Empty;

    /// <summary>
    /// Job prefix (whm, sch, ast, sge, etc.).
    /// </summary>
    public string JobPrefix { get; init; } = string.Empty;

    /// <summary>
    /// Lesson title (e.g., "Healer Fundamentals").
    /// </summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>
    /// Overview description of the lesson.
    /// </summary>
    public string Description { get; init; } = string.Empty;

    /// <summary>
    /// Lesson number (1-7).
    /// </summary>
    public int LessonNumber { get; init; }

    /// <summary>
    /// LessonIds required to be completed first.
    /// </summary>
    public string[] Prerequisites { get; init; } = Array.Empty<string>();

    /// <summary>
    /// ConceptIds from TrainingModels that this lesson covers.
    /// </summary>
    public string[] ConceptsCovered { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Key educational points for this lesson.
    /// </summary>
    public string[] KeyPoints { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Related ability names.
    /// </summary>
    public string[] RelatedAbilities { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Learning tips for practicing.
    /// </summary>
    public string[] Tips { get; init; } = Array.Empty<string>();
}
