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
}
