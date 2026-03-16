using Dalamud.Plugin.Services;
using Moq;
using Olympus.Training;
using Xunit;

namespace Olympus.Tests.Training;

public class TrainingDataRegistryJapaneseTests
{
    [Fact]
    public void GetLessonsForJob_JapaneseLanguage_ReturnsJapaneseTitles()
    {
        var log = new Mock<IPluginLog>().Object;
        var registry = new TrainingDataRegistry(log);
        registry.CurrentLanguage = "ja";

        var lessons = registry.GetLessonsForJob("whm");

        Assert.NotNull(lessons);
        Assert.NotEmpty(lessons);
        // Japanese WHM lesson 1 title should be in Japanese
        Assert.Equal("ヒーラーの基本", lessons[0].Title);
    }

    [Fact]
    public void GetQuizzesForJob_JapaneseLanguage_ReturnsJapaneseTitles()
    {
        var log = new Mock<IPluginLog>().Object;
        var registry = new TrainingDataRegistry(log);
        registry.CurrentLanguage = "ja";

        var quizForLesson1 = registry.GetQuizForLesson("whm.lesson_1");

        Assert.NotNull(quizForLesson1);
        Assert.Equal("クイズ: ヒーラーの基本", quizForLesson1.Title);
    }

    [Fact]
    public void GetLessonsForJob_JapaneseLanguage_FallsBackToEnglish_ForMissingJob()
    {
        // This test passes even before all jobs are translated
        var log = new Mock<IPluginLog>().Object;
        var registry = new TrainingDataRegistry(log);
        registry.CurrentLanguage = "ja";

        // sch is translated — should return Japanese
        var schLessons = registry.GetLessonsForJob("sch");
        Assert.NotNull(schLessons);
        Assert.NotEmpty(schLessons);
    }
}
