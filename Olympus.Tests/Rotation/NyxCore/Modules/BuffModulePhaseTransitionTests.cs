using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.NyxCore.Abilities;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Timeline;
using Olympus.Timeline.Models;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.NyxCore.Modules;

/// <summary>
/// Verifies that DRK Delirium is held when a high-confidence phase
/// transition is imminent, and fires normally when no transition is detected.
/// </summary>
public class BuffModulePhaseTransitionTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void Delirium_NotPushed_WhenPhaseTransitionImminent()
    {
        var timeline = new Mock<ITimelineService>();
        timeline.Setup(x => x.GetNextMechanic(TimelineEntryType.Phase))
            .Returns(new MechanicPrediction(5f, TimelineEntryType.Phase, "Phase 2", 0.9f));

        var context = CreateContext(timelineService: timeline.Object);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void Delirium_Pushed_WhenNoPhaseTransition()
    {
        var context = CreateContext(timelineService: null);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Single(queue);
        Assert.Equal(NyxAbilities.Delirium, queue[0].Behavior);
    }

    private static INyxContext CreateContext(ITimelineService? timelineService)
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.AutoTankStance = false;
        config.Tank.EnableBloodWeapon = false; // isolate: only Delirium can push
        config.Tank.EnableDelirium = true;
        config.Tank.EnableLivingShadow = false; // isolate

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Delirium.ActionId)).Returns(true);

        var player = MockBuilders.CreateMockPlayerCharacter(level: 100);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<INyxContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(true);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TimelineService).Returns(timelineService);
        mock.Setup(x => x.HasDelirium).Returns(false);
        mock.Setup(x => x.HasDarkside).Returns(true); // required: TryPushDelirium gates on HasDarkside
        mock.Setup(x => x.BloodGauge).Returns(0);     // level 100: the gauge<30 gate only fires below lv.96
        mock.Setup(x => x.Debug).Returns(new NyxDebugState());
        return mock.Object;
    }
}
