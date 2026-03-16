using System;
using System.Linq;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Config;
using Olympus.Services.Training;
using Xunit;

namespace Olympus.Tests.Services.Training;

public sealed class SpacedRepetitionServiceTests
{
    private readonly TrainingConfig config;
    private readonly SpacedRepetitionService service;

    public SpacedRepetitionServiceTests()
    {
        config = new TrainingConfig { EnableTraining = true, EnableSpacedRepetition = true };
        var trainingService = new Mock<ITrainingService>();
        var log = new Mock<IPluginLog>();
        service = new SpacedRepetitionService(config, trainingService.Object, log.Object);
    }

    // IsEnabled tests

    [Fact]
    public void IsEnabled_BothTrue_ReturnsTrue()
    {
        Assert.True(service.IsEnabled);
    }

    [Fact]
    public void IsEnabled_EnableTrainingFalse_ReturnsFalse()
    {
        config.EnableTraining = false;
        Assert.False(service.IsEnabled);
    }

    [Fact]
    public void IsEnabled_EnableSpacedRepetitionFalse_ReturnsFalse()
    {
        config.EnableSpacedRepetition = false;
        Assert.False(service.IsEnabled);
    }

    // GetRetentionData tests

    [Fact]
    public void GetRetentionData_UnknownConcept_ReturnsNull()
    {
        var result = service.GetRetentionData("unknown.concept");
        Assert.Null(result);
    }

    [Fact]
    public void RecordSuccess_CreatesRetentionData_WithHasBeenPracticed()
    {
        service.RecordSuccess("whm.healing_priority");
        var data = service.GetRetentionData("whm.healing_priority");
        Assert.NotNull(data);
        Assert.True(data.HasBeenPracticed);
    }

    // Forgetting curve / review detection tests

    [Fact]
    public void GetConceptsNeedingReview_Empty_WhenNothingPracticed()
    {
        var result = service.GetConceptsNeedingReview();
        Assert.Empty(result);
    }

    [Fact]
    public void GetConceptsNeedingReview_ExcludesRecentlyPracticed()
    {
        service.RecordSuccess("whm.healing_priority");
        var result = service.GetConceptsNeedingReview();
        Assert.DoesNotContain(result, d => d.ConceptId == "whm.healing_priority");
    }

    [Fact]
    public void GetConceptsNeedingReview_Includes_OldConcept()
    {
        config.ConceptRetention["whm.healing_priority"] = new ConceptRetentionData
        {
            ConceptId = "whm.healing_priority",
            LastPracticed = DateTime.Now.AddDays(-31),
            SuccessfulDemonstrations = 0,
        };

        var result = service.GetConceptsNeedingReview();
        Assert.Contains(result, d => d.ConceptId == "whm.healing_priority");
    }

    [Fact]
    public void GetConceptsNeedingRelearning_Includes_OldConcept()
    {
        config.ConceptRetention["whm.healing_priority"] = new ConceptRetentionData
        {
            ConceptId = "whm.healing_priority",
            LastPracticed = DateTime.Now.AddDays(-31),
            SuccessfulDemonstrations = 0,
        };

        var result = service.GetConceptsNeedingRelearning();
        Assert.Contains(result, d => d.ConceptId == "whm.healing_priority");
    }

    [Fact]
    public void GetConceptsNeedingRelearning_Excludes_MediumRetentionConcept()
    {
        // 5 days old with 0 demonstrations: effectiveDays = 5 / 1.0 = 5
        // Retention = 0.80 - ((5 - 3) * 0.05) = 0.80 - 0.10 = 0.70 (~70%) — above relearning threshold
        config.ConceptRetention["whm.ogcd_weaving"] = new ConceptRetentionData
        {
            ConceptId = "whm.ogcd_weaving",
            LastPracticed = DateTime.Now.AddDays(-5),
            SuccessfulDemonstrations = 0,
        };

        var result = service.GetConceptsNeedingRelearning();
        Assert.DoesNotContain(result, d => d.ConceptId == "whm.ogcd_weaving");
    }

    // Review recording tests

    [Fact]
    public void RecordReview_IncrementsReviewCount()
    {
        config.ConceptRetention["whm.healing_priority"] = new ConceptRetentionData
        {
            ConceptId = "whm.healing_priority",
            LastPracticed = DateTime.Now.AddDays(-31),
            SuccessfulDemonstrations = 0,
            ReviewCount = 0,
        };

        service.RecordReview("whm.healing_priority");
        var data = service.GetRetentionData("whm.healing_priority");
        Assert.NotNull(data);
        Assert.Equal(1, data.ReviewCount);
    }

    [Fact]
    public void RecordReview_AdvancesLastPracticed()
    {
        config.ConceptRetention["whm.healing_priority"] = new ConceptRetentionData
        {
            ConceptId = "whm.healing_priority",
            LastPracticed = DateTime.Now.AddDays(-31),
            SuccessfulDemonstrations = 0,
        };

        var dataBefore = service.GetRetentionData("whm.healing_priority");
        Assert.NotNull(dataBefore);
        var capturedLastPracticed = dataBefore.LastPracticed;

        service.RecordReview("whm.healing_priority");

        var dataAfter = service.GetRetentionData("whm.healing_priority");
        Assert.NotNull(dataAfter);
        Assert.True(dataAfter.LastPracticed > capturedLastPracticed);
    }
}
