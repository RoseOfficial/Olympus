namespace Olympus.Training;

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Dalamud.Plugin.Services;
using Olympus.Services.Training;

/// <summary>
/// Registry for training content loaded from embedded JSON resources.
/// Replaces the static QuizRegistry and LessonContent classes.
/// </summary>
public sealed class TrainingDataRegistry
{
    private static readonly string[] JobPrefixes = new[]
    {
        "whm", "sch", "ast", "sge",  // Healers
        "pld", "war", "drk", "gnb",  // Tanks
        "drg", "nin", "sam", "mnk", "rpr", "vpr",  // Melee DPS
        "mch", "brd", "dnc",  // Ranged Physical DPS
        "blm", "smn", "rdm", "pct",  // Casters
    };

    private readonly Dictionary<string, LessonDefinition[]> lessonsByJob = new();
    private readonly Dictionary<string, QuizDefinition[]> quizzesByJob = new();
    private readonly Dictionary<string, LessonDefinition> lessonsById = new();
    private readonly Dictionary<string, QuizDefinition> quizzesById = new();
    private readonly Dictionary<string, QuizDefinition> quizzesByLessonId = new();

    private readonly IPluginLog log;

    /// <summary>
    /// Creates a new TrainingDataRegistry and loads all content from embedded resources.
    /// </summary>
    public TrainingDataRegistry(IPluginLog log)
    {
        this.log = log;
        LoadAllData();
    }

    private void LoadAllData()
    {
        var assembly = Assembly.GetExecutingAssembly();

        foreach (var job in JobPrefixes)
        {
            LoadLessonsForJob(assembly, job);
            LoadQuizzesForJob(assembly, job);
        }

        this.log.Information(
            "TrainingDataRegistry loaded: {LessonCount} lessons, {QuizCount} quizzes across {JobCount} jobs",
            this.lessonsById.Count,
            this.quizzesById.Count,
            JobPrefixes.Length);
    }

    private void LoadLessonsForJob(Assembly assembly, string jobPrefix)
    {
        var resourceName = $"Olympus.Training.Data.Lessons.{jobPrefix}.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            this.log.Warning("Lesson resource not found: {ResourceName}", resourceName);
            this.lessonsByJob[jobPrefix] = Array.Empty<LessonDefinition>();
            return;
        }

        try
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var data = JsonSerializer.Deserialize<LessonFileSchema>(json);

            if (data?.Lessons == null)
            {
                this.log.Warning("Failed to deserialize lessons for {Job}", jobPrefix);
                this.lessonsByJob[jobPrefix] = Array.Empty<LessonDefinition>();
                return;
            }

            var lessons = new List<LessonDefinition>();
            foreach (var lessonJson in data.Lessons)
            {
                var lesson = new LessonDefinition
                {
                    LessonId = lessonJson.LessonId,
                    JobPrefix = data.JobPrefix,
                    Title = lessonJson.Title,
                    Description = lessonJson.Description,
                    LessonNumber = lessonJson.LessonNumber,
                    Prerequisites = lessonJson.Prerequisites,
                    ConceptsCovered = lessonJson.ConceptsCovered,
                    KeyPoints = lessonJson.KeyPoints,
                    RelatedAbilities = lessonJson.RelatedAbilities,
                    Tips = lessonJson.Tips,
                };

                lessons.Add(lesson);
                this.lessonsById[lesson.LessonId] = lesson;
            }

            this.lessonsByJob[jobPrefix] = lessons.ToArray();
        }
        catch (Exception ex)
        {
            this.log.Error(ex, "Error loading lessons for {Job}", jobPrefix);
            this.lessonsByJob[jobPrefix] = Array.Empty<LessonDefinition>();
        }
    }

    private void LoadQuizzesForJob(Assembly assembly, string jobPrefix)
    {
        var resourceName = $"Olympus.Training.Data.Quizzes.{jobPrefix}.json";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream == null)
        {
            this.log.Warning("Quiz resource not found: {ResourceName}", resourceName);
            this.quizzesByJob[jobPrefix] = Array.Empty<QuizDefinition>();
            return;
        }

        try
        {
            using var reader = new StreamReader(stream);
            var json = reader.ReadToEnd();
            var data = JsonSerializer.Deserialize<QuizFileSchema>(json);

            if (data?.Quizzes == null)
            {
                this.log.Warning("Failed to deserialize quizzes for {Job}", jobPrefix);
                this.quizzesByJob[jobPrefix] = Array.Empty<QuizDefinition>();
                return;
            }

            var quizzes = new List<QuizDefinition>();
            foreach (var quizJson in data.Quizzes)
            {
                var questions = new List<QuizQuestion>();
                foreach (var qJson in quizJson.Questions)
                {
                    questions.Add(new QuizQuestion
                    {
                        QuestionId = qJson.QuestionId,
                        ConceptId = qJson.ConceptId,
                        Scenario = qJson.Scenario,
                        Question = qJson.Question,
                        Options = qJson.Options,
                        CorrectIndex = qJson.CorrectIndex,
                        Explanation = qJson.Explanation,
                    });
                }

                var quiz = new QuizDefinition
                {
                    QuizId = quizJson.QuizId,
                    LessonId = quizJson.LessonId,
                    Title = quizJson.Title,
                    PassingScore = quizJson.PassingScore,
                    Questions = questions.ToArray(),
                };

                quizzes.Add(quiz);
                this.quizzesById[quiz.QuizId] = quiz;
                this.quizzesByLessonId[quiz.LessonId] = quiz;
            }

            this.quizzesByJob[jobPrefix] = quizzes.ToArray();
        }
        catch (Exception ex)
        {
            this.log.Error(ex, "Error loading quizzes for {Job}", jobPrefix);
            this.quizzesByJob[jobPrefix] = Array.Empty<QuizDefinition>();
        }
    }

    /// <summary>
    /// Gets a lesson by its ID.
    /// </summary>
    public LessonDefinition? GetLesson(string lessonId)
    {
        return this.lessonsById.TryGetValue(lessonId, out var lesson) ? lesson : null;
    }

    /// <summary>
    /// Gets a quiz by its ID.
    /// </summary>
    public QuizDefinition? GetQuiz(string quizId)
    {
        return this.quizzesById.TryGetValue(quizId, out var quiz) ? quiz : null;
    }

    /// <summary>
    /// Gets the quiz for a specific lesson.
    /// </summary>
    public QuizDefinition? GetQuizForLesson(string lessonId)
    {
        return this.quizzesByLessonId.TryGetValue(lessonId, out var quiz) ? quiz : null;
    }

    /// <summary>
    /// Gets all lessons for a specific job.
    /// </summary>
    public IReadOnlyList<LessonDefinition> GetLessonsForJob(string jobPrefix)
    {
        var key = jobPrefix.ToLowerInvariant();
        return this.lessonsByJob.TryGetValue(key, out var lessons) ? lessons : Array.Empty<LessonDefinition>();
    }

    /// <summary>
    /// Gets all quizzes for a specific job.
    /// </summary>
    public IReadOnlyList<QuizDefinition> GetQuizzesForJob(string jobPrefix)
    {
        var key = jobPrefix.ToLowerInvariant();
        return this.quizzesByJob.TryGetValue(key, out var quizzes) ? quizzes : Array.Empty<QuizDefinition>();
    }

    /// <summary>
    /// Gets all available job prefixes.
    /// </summary>
    public IReadOnlyList<string> GetAllJobPrefixes() => JobPrefixes;

    /// <summary>
    /// Gets all lessons across all jobs.
    /// </summary>
    public IReadOnlyCollection<LessonDefinition> GetAllLessons() => this.lessonsById.Values;

    /// <summary>
    /// Gets all quizzes across all jobs.
    /// </summary>
    public IReadOnlyCollection<QuizDefinition> GetAllQuizzes() => this.quizzesById.Values;
}
