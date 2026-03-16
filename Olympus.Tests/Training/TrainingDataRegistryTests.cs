using System.Linq;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Training;
using Xunit;

namespace Olympus.Tests.Training;

public sealed class TrainingDataRegistryTests
{
    // Shared instance — read-only after construction
    private static readonly TrainingDataRegistry Registry = new(new Mock<IPluginLog>().Object);

    [Fact]
    public void WhmLessons_LoadWithPositiveCount()
    {
        var lessons = Registry.GetLessonsForJob("whm");
        Assert.True(lessons.Count > 0);
    }

    [Fact]
    public void WhmLessons_HaveNoNullRequiredFields()
    {
        var lessons = Registry.GetLessonsForJob("whm");
        foreach (var lesson in lessons)
        {
            Assert.NotNull(lesson.LessonId);
            Assert.NotNull(lesson.Title);
            Assert.NotNull(lesson.Description);
        }
    }

    [Theory]
    [InlineData("whm")]
    [InlineData("sch")]
    [InlineData("ast")]
    [InlineData("sge")]
    [InlineData("war")]
    [InlineData("pld")]
    [InlineData("drk")]
    [InlineData("gnb")]
    [InlineData("drg")]
    [InlineData("nin")]
    [InlineData("sam")]
    [InlineData("mnk")]
    [InlineData("rpr")]
    [InlineData("vpr")]
    [InlineData("mch")]
    [InlineData("brd")]
    [InlineData("dnc")]
    [InlineData("blm")]
    [InlineData("smn")]
    [InlineData("rdm")]
    [InlineData("pct")]
    public void AllJobs_HaveAtLeastOneLesson(string jobPrefix)
    {
        var lessons = Registry.GetLessonsForJob(jobPrefix);
        Assert.True(lessons.Count >= 1, $"Job '{jobPrefix}' should have at least 1 lesson");
    }

    [Theory]
    [InlineData("whm")]
    [InlineData("sch")]
    [InlineData("ast")]
    [InlineData("sge")]
    [InlineData("war")]
    [InlineData("pld")]
    [InlineData("drk")]
    [InlineData("gnb")]
    [InlineData("drg")]
    [InlineData("nin")]
    [InlineData("sam")]
    [InlineData("mnk")]
    [InlineData("rpr")]
    [InlineData("vpr")]
    [InlineData("mch")]
    [InlineData("brd")]
    [InlineData("dnc")]
    [InlineData("blm")]
    [InlineData("smn")]
    [InlineData("rdm")]
    [InlineData("pct")]
    public void AllJobs_HaveAtLeastOneQuiz(string jobPrefix)
    {
        var quizzes = Registry.GetQuizzesForJob(jobPrefix);
        Assert.True(quizzes.Count >= 1, $"Job '{jobPrefix}' should have at least 1 quiz");
    }

    [Fact]
    public void GetLesson_KnownId_ReturnsCorrectLesson()
    {
        var lesson = Registry.GetLesson("whm.lesson_1");
        Assert.NotNull(lesson);
        Assert.Equal("whm.lesson_1", lesson.LessonId);
    }

    [Fact]
    public void GetLesson_UnknownId_ReturnsNull()
    {
        var lesson = Registry.GetLesson("nonexistent.lesson");
        Assert.Null(lesson);
    }

    [Fact]
    public void GetQuizForLesson_KnownLesson_ReturnsQuiz()
    {
        var quiz = Registry.GetQuizForLesson("whm.lesson_1");
        Assert.NotNull(quiz);
    }

    [Fact]
    public void WhmLessonCount_EqualsQuizCount()
    {
        var lessonCount = Registry.GetLessonsForJob("whm").Count;
        var quizCount = Registry.GetQuizzesForJob("whm").Count;
        Assert.Equal(lessonCount, quizCount);
    }

    [Fact]
    public void WhmQuizQuestions_HaveValidCorrectIndex()
    {
        var quizzes = Registry.GetQuizzesForJob("whm");
        foreach (var quiz in quizzes)
        {
            foreach (var question in quiz.Questions)
            {
                Assert.True(
                    question.CorrectIndex >= 0 && question.CorrectIndex < question.Options.Length,
                    $"Quiz {quiz.QuizId}: CorrectIndex {question.CorrectIndex} out of range [0, {question.Options.Length})");
            }
        }
    }
}
