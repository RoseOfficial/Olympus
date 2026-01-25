namespace Olympus.Training;

using System;
using System.Text.Json.Serialization;

/// <summary>
/// JSON schema for lesson file deserialization.
/// </summary>
public sealed class LessonFileSchema
{
    [JsonPropertyName("jobPrefix")]
    public string JobPrefix { get; set; } = string.Empty;

    [JsonPropertyName("jobName")]
    public string JobName { get; set; } = string.Empty;

    [JsonPropertyName("lessons")]
    public LessonJsonSchema[] Lessons { get; set; } = Array.Empty<LessonJsonSchema>();
}

/// <summary>
/// JSON schema for a single lesson.
/// </summary>
public sealed class LessonJsonSchema
{
    [JsonPropertyName("lessonId")]
    public string LessonId { get; set; } = string.Empty;

    [JsonPropertyName("lessonNumber")]
    public int LessonNumber { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("prerequisites")]
    public string[] Prerequisites { get; set; } = Array.Empty<string>();

    [JsonPropertyName("conceptsCovered")]
    public string[] ConceptsCovered { get; set; } = Array.Empty<string>();

    [JsonPropertyName("keyPoints")]
    public string[] KeyPoints { get; set; } = Array.Empty<string>();

    [JsonPropertyName("relatedAbilities")]
    public string[] RelatedAbilities { get; set; } = Array.Empty<string>();

    [JsonPropertyName("tips")]
    public string[] Tips { get; set; } = Array.Empty<string>();
}

/// <summary>
/// JSON schema for quiz file deserialization.
/// </summary>
public sealed class QuizFileSchema
{
    [JsonPropertyName("jobPrefix")]
    public string JobPrefix { get; set; } = string.Empty;

    [JsonPropertyName("jobName")]
    public string JobName { get; set; } = string.Empty;

    [JsonPropertyName("quizzes")]
    public QuizJsonSchema[] Quizzes { get; set; } = Array.Empty<QuizJsonSchema>();
}

/// <summary>
/// JSON schema for a single quiz.
/// </summary>
public sealed class QuizJsonSchema
{
    [JsonPropertyName("quizId")]
    public string QuizId { get; set; } = string.Empty;

    [JsonPropertyName("lessonId")]
    public string LessonId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("passingScore")]
    public int PassingScore { get; set; } = 4;

    [JsonPropertyName("questions")]
    public QuestionJsonSchema[] Questions { get; set; } = Array.Empty<QuestionJsonSchema>();
}

/// <summary>
/// JSON schema for a single quiz question.
/// </summary>
public sealed class QuestionJsonSchema
{
    [JsonPropertyName("questionId")]
    public string QuestionId { get; set; } = string.Empty;

    [JsonPropertyName("conceptId")]
    public string ConceptId { get; set; } = string.Empty;

    [JsonPropertyName("scenario")]
    public string Scenario { get; set; } = string.Empty;

    [JsonPropertyName("question")]
    public string Question { get; set; } = string.Empty;

    [JsonPropertyName("options")]
    public string[] Options { get; set; } = Array.Empty<string>();

    [JsonPropertyName("correctIndex")]
    public int CorrectIndex { get; set; }

    [JsonPropertyName("explanation")]
    public string Explanation { get; set; } = string.Empty;
}
