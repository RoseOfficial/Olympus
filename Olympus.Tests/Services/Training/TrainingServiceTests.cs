using System;
using System.Linq;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Config;
using Olympus.Services.Training;
using Olympus.Training;
using Xunit;

namespace Olympus.Tests.Services.Training;

public sealed class TrainingServiceTests
{
    private readonly TrainingConfig config;
    private readonly TrainingDataRegistry registry;
    private readonly TrainingService service;

    public TrainingServiceTests()
    {
        config = new TrainingConfig { EnableTraining = true };
        var log = new Mock<IPluginLog>();
        registry = new TrainingDataRegistry(log.Object);
        var objectTable = new Mock<IObjectTable>();
        service = new TrainingService(config, objectTable.Object, registry);
        // Do NOT call SetSpacedRepetitionService — keep service isolated
    }

    private static ActionExplanation MakeExplanation(
        string conceptId = "whm.healing_priority",
        ExplanationPriority priority = ExplanationPriority.Normal)
        => new ActionExplanation
        {
            ActionId = 1,
            ActionName = "Test Action",
            Category = "Healing",
            ShortReason = "Test",
            DetailedReason = "Test detailed",
            ConceptId = conceptId,
            Priority = priority,
        };

    [Fact]
    public void RecordDecision_WhenDisabled_DoesNotStore()
    {
        config.EnableTraining = false;
        service.RecordDecision(MakeExplanation());
        Assert.Empty(service.RecentExplanations);
    }

    [Fact]
    public void RecordDecision_WhenEnabled_StoresExplanation()
    {
        service.RecordDecision(MakeExplanation());
        Assert.Single(service.RecentExplanations);
    }

    [Fact]
    public void RecordDecision_MostRecentFirst()
    {
        var first = MakeExplanation("whm.healing_priority");
        var second = MakeExplanation("whm.ogcd_weaving");
        service.RecordDecision(first);
        service.RecordDecision(second);
        Assert.Equal("whm.ogcd_weaving", service.RecentExplanations[0].ConceptId);
        Assert.Equal("whm.healing_priority", service.RecentExplanations[1].ConceptId);
    }

    [Fact]
    public void RecordDecision_CapsAtMaxExplanations()
    {
        config.MaxExplanationsToShow = 5;
        for (var i = 0; i < 7; i++)
            service.RecordDecision(MakeExplanation($"concept.{i}"));
        Assert.Equal(5, service.RecentExplanations.Count);
    }

    [Fact]
    public void RecordDecision_FiltersByMinPriority()
    {
        config.MinimumPriorityToShow = ExplanationPriority.High;
        service.RecordDecision(MakeExplanation(priority: ExplanationPriority.Normal));
        Assert.Empty(service.RecentExplanations);
    }

    [Fact]
    public void RecordDecision_TracksConceptExposureCount()
    {
        service.RecordDecision(MakeExplanation("whm.healing_priority"));
        service.RecordDecision(MakeExplanation("whm.healing_priority"));
        Assert.Equal(2, service.GetConceptExposureCount("whm.healing_priority"));
    }

    [Fact]
    public void ClearExplanations_RemovesAll()
    {
        service.RecordDecision(MakeExplanation());
        service.RecordDecision(MakeExplanation());
        service.ClearExplanations();
        Assert.Empty(service.RecentExplanations);
    }

    [Fact]
    public void MarkConceptLearned_AddsToLearnedSet()
    {
        service.MarkConceptLearned("whm.healing_priority");
        Assert.Contains("whm.healing_priority", config.LearnedConcepts);
    }

    [Fact]
    public void UnmarkConceptLearned_RemovesFromLearnedSet()
    {
        service.MarkConceptLearned("whm.healing_priority");
        service.UnmarkConceptLearned("whm.healing_priority");
        Assert.DoesNotContain("whm.healing_priority", config.LearnedConcepts);
    }

    [Fact]
    public void GetProgress_TotalConceptsIsPositive()
    {
        var progress = service.GetProgress();
        Assert.True(progress.TotalConcepts > 0);
    }

    [Fact]
    public void GetProgress_CountsLearnedConcepts()
    {
        service.MarkConceptLearned("whm.healing_priority");
        service.MarkConceptLearned("whm.ogcd_weaving");
        var progress = service.GetProgress();
        Assert.Equal(2, progress.LearnedConcepts);
    }

    [Fact]
    public void GetProgress_HighExposureUnlearned_AppearsInNeedsAttention()
    {
        config.ConceptExposureCount["whm.healing_priority"] = 10;
        var progress = service.GetProgress();
        Assert.Contains("whm.healing_priority", progress.ConceptsNeedingAttention);
    }

    [Fact]
    public void GetProgress_LowExposureUnlearned_NotInNeedsAttention()
    {
        config.ConceptExposureCount["whm.healing_priority"] = 5;
        var progress = service.GetProgress();
        Assert.DoesNotContain("whm.healing_priority", progress.ConceptsNeedingAttention);
    }

    [Fact]
    public void GetProgress_RecentExplanations_AppearsInRecentlySeen()
    {
        service.RecordDecision(MakeExplanation("whm.healing_priority"));
        var progress = service.GetProgress();
        Assert.Contains("whm.healing_priority", progress.RecentlyDemonstratedConcepts);
    }

    // Note: uses real WHM lesson IDs from embedded JSON.
    // whm.lesson_1 has no prerequisites; whm.lesson_2 requires whm.lesson_1.

    [Fact]
    public void IsLessonComplete_ReturnsFalse_WhenNotCompleted()
    {
        Assert.False(service.IsLessonComplete("whm.lesson_1"));
    }

    [Fact]
    public void MarkLessonComplete_SetsCompleted()
    {
        service.MarkLessonComplete("whm.lesson_1");
        Assert.True(service.IsLessonComplete("whm.lesson_1"));
    }

    [Fact]
    public void MarkLessonComplete_AlsoMarksConceptsCovered()
    {
        // whm.lesson_1 covers concepts — after completing it, at least one concept should be learned
        service.MarkLessonComplete("whm.lesson_1");
        Assert.NotEmpty(config.LearnedConcepts);
    }

    [Fact]
    public void AreLessonPrerequisitesMet_NoPrereqs_ReturnsTrue()
    {
        // whm.lesson_1 has no prerequisites
        Assert.True(service.AreLessonPrerequisitesMet("whm.lesson_1"));
    }

    [Fact]
    public void AreLessonPrerequisitesMet_UnmetPrereqs_ReturnsFalse()
    {
        // whm.lesson_2 requires whm.lesson_1 — do NOT complete lesson_1 first
        Assert.False(service.AreLessonPrerequisitesMet("whm.lesson_2"));
    }

    [Fact]
    public void AreLessonPrerequisitesMet_MetPrereqs_ReturnsTrue()
    {
        service.MarkLessonComplete("whm.lesson_1");
        Assert.True(service.AreLessonPrerequisitesMet("whm.lesson_2"));
    }

    [Fact]
    public void IsQuizPassed_ReturnsFalse_WhenNeverAttempted()
    {
        Assert.False(service.IsQuizPassed("whm.lesson_1.quiz"));
    }

    [Fact]
    public void RecordQuizAttempt_PassingScore_SetsIsQuizPassed()
    {
        var quiz = service.GetQuizForLesson("whm.lesson_1");
        Assert.NotNull(quiz);

        service.RecordQuizAttempt(new QuizAttempt
        {
            QuizId = quiz.QuizId,
            Score = quiz.PassingScore,
            Passed = true,
            AttemptedAt = DateTime.Now,
        });

        Assert.True(service.IsQuizPassed(quiz.QuizId));
    }

    [Fact]
    public void RecordQuizAttempt_FailingScore_DoesNotSetPassed()
    {
        var quiz = service.GetQuizForLesson("whm.lesson_1");
        Assert.NotNull(quiz);

        service.RecordQuizAttempt(new QuizAttempt
        {
            QuizId = quiz.QuizId,
            Score = quiz.PassingScore - 1,
            Passed = false,
            AttemptedAt = DateTime.Now,
        });

        Assert.False(service.IsQuizPassed(quiz.QuizId));
    }

    [Fact]
    public void GetBestAttempt_ReturnsHighestScore()
    {
        var quiz = service.GetQuizForLesson("whm.lesson_1");
        Assert.NotNull(quiz);

        service.RecordQuizAttempt(new QuizAttempt
        {
            QuizId = quiz.QuizId,
            Score = 2,
            Passed = false,
            AttemptedAt = DateTime.Now.AddMinutes(-5),
        });

        service.RecordQuizAttempt(new QuizAttempt
        {
            QuizId = quiz.QuizId,
            Score = 4,
            Passed = true,
            AttemptedAt = DateTime.Now,
        });

        var best = service.GetBestAttempt(quiz.QuizId);
        Assert.NotNull(best);
        Assert.Equal(4, best.Score);
    }
}
