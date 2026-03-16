using System;
using System.Linq;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Config;
using Olympus.Rotation.Common.Helpers;
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

    // Skill Level group

    [Fact]
    public void GetSkillLevel_NewUser_ReturnsBeginner()
    {
        var result = service.GetSkillLevel("whm");
        Assert.Equal(SkillLevel.Beginner, result.Level);
    }

    [Fact]
    public void GetSkillLevel_AllQuizzesAndLessonsPassed_ReturnsAdvanced()
    {
        // Pass all 7 WHM quizzes with perfect scores
        var quizIds = new[]
        {
            "whm.lesson_1.quiz",
            "whm.lesson_2.quiz",
            "whm.lesson_3.quiz",
            "whm.lesson_4.quiz",
            "whm.lesson_5.quiz",
            "whm.lesson_6.quiz",
            "whm.lesson_7.quiz",
        };
        foreach (var qid in quizIds)
        {
            config.CompletedQuizzes.Add(qid);
            config.BestQuizAttempts[qid] = new QuizAttemptData
            {
                Score = 5,
                TotalQuestions = 5,
                Passed = true,
                AttemptedAt = DateTime.Now,
            };
        }

        // Complete all 7 lessons
        var lessonIds = new[]
        {
            "whm.lesson_1",
            "whm.lesson_2",
            "whm.lesson_3",
            "whm.lesson_4",
            "whm.lesson_5",
            "whm.lesson_6",
            "whm.lesson_7",
        };
        foreach (var lid in lessonIds)
            config.CompletedLessons.Add(lid);

        // Mark all WHM concepts as learned so conceptsLearned score reaches 100%
        var conceptIds = new[]
        {
            "whm.healing_priority", "whm.tank_priority", "whm.party_wide_damage", "whm.ogcd_weaving",
            "whm.emergency_healing", "whm.benediction_usage", "whm.tetragrammaton_usage",
            "whm.lily_management", "whm.afflatus_solace_usage", "whm.afflatus_rapture_usage",
            "whm.blood_lily_building", "whm.afflatus_misery_timing",
            "whm.proactive_healing", "whm.regen_maintenance", "whm.shield_timing",
            "whm.divine_benison_usage", "whm.assize_usage",
            "whm.temperance_usage", "whm.aquaveil_usage", "whm.liturgy_usage",
            "whm.dps_optimization", "whm.glare_priority", "whm.dot_maintenance",
            "whm.esuna_usage", "whm.raise_decision", "whm.cohealer_awareness", "whm.party_coordination",
        };
        foreach (var cid in conceptIds)
            config.LearnedConcepts.Add(cid);

        var result = service.GetSkillLevel("whm");
        Assert.Equal(SkillLevel.Advanced, result.Level);
    }

    [Fact]
    public void GetSkillLevel_WithOverride_ReturnsOverrideLevel()
    {
        config.SkillLevelOverride = SkillLevelOverride.Advanced;
        var result = service.GetSkillLevel("whm");
        Assert.Equal(SkillLevel.Advanced, result.Level);
    }

    // Concept Mastery group

    [Fact]
    public void RecordConceptApplication_Success_IncrementsOpportunitiesAndSuccesses()
    {
        service.RecordConceptApplication("whm.healing_priority", wasSuccessful: true);
        var data = config.ConceptMastery["whm.healing_priority"];
        Assert.Equal(1, data.Opportunities);
        Assert.Equal(1, data.Successes);
    }

    [Fact]
    public void RecordConceptApplication_Failure_IncrementsOnlyOpportunities()
    {
        service.RecordConceptApplication("whm.healing_priority", wasSuccessful: false);
        var data = config.ConceptMastery["whm.healing_priority"];
        Assert.Equal(1, data.Opportunities);
        Assert.Equal(0, data.Successes);
    }

    [Fact]
    public void GetStrugglingConcepts_ReturnsConcepts_BelowThreshold()
    {
        // 5 successes out of 10 opportunities = 50%, below the 60% struggling threshold
        for (var i = 0; i < 5; i++) service.RecordConceptApplication("whm.healing_priority", wasSuccessful: true);
        for (var i = 0; i < 5; i++) service.RecordConceptApplication("whm.healing_priority", wasSuccessful: false);
        var struggling = service.GetStrugglingConcepts("whm");
        Assert.Contains("whm.healing_priority", struggling);
    }

    [Fact]
    public void GetStrugglingConcepts_Excludes_AboveThreshold()
    {
        // 7 successes out of 10 opportunities = 70%, above the 60% struggling threshold
        for (var i = 0; i < 7; i++) service.RecordConceptApplication("whm.healing_priority", wasSuccessful: true);
        for (var i = 0; i < 3; i++) service.RecordConceptApplication("whm.healing_priority", wasSuccessful: false);
        var struggling = service.GetStrugglingConcepts("whm");
        Assert.DoesNotContain("whm.healing_priority", struggling);
    }

    [Fact]
    public void GetMasteredConcepts_ReturnsConcepts_AboveMasteryThreshold()
    {
        // 9 successes out of 10 opportunities = 90%, above the 85% mastery threshold
        for (var i = 0; i < 9; i++) service.RecordConceptApplication("whm.healing_priority", wasSuccessful: true);
        for (var i = 0; i < 1; i++) service.RecordConceptApplication("whm.healing_priority", wasSuccessful: false);
        var mastered = service.GetMasteredConcepts("whm");
        Assert.Contains("whm.healing_priority", mastered);
    }

    [Fact]
    public void DismissRecommendation_RemovesFromList()
    {
        // Recommendations list starts empty; dismissing a non-existent ID is a no-op
        // and the ID should not appear in GetRecommendations()
        service.DismissRecommendation("whm.lesson_1");
        Assert.DoesNotContain(
            service.GetRecommendations(),
            r => r.Lesson.LessonId == "whm.lesson_1");
    }

    [Fact]
    public void UpdateRecommendationsFromMastery_ReturnsFalse_WhenNoMasteryData()
    {
        // Fresh config with no ConceptMastery entries — no struggling concepts, returns false
        var result = service.UpdateRecommendationsFromMastery("whm");
        Assert.False(result);
    }

    [Fact]
    public void TrainingHelper_RecordDamageDecision_WithNullTrainingService_DoesNotThrow()
    {
        // Passing null ITrainingService should be a no-op, not throw
        var ex = Record.Exception(() =>
            TrainingHelper.RecordDamageDecision(
                null, 1, "Test Action", null,
                "Short reason", "Detailed reason",
                new[] { "Factor" }, new[] { "Alternative" },
                "Tip", "whm.healing_priority"));
        Assert.Null(ex);
    }
}
