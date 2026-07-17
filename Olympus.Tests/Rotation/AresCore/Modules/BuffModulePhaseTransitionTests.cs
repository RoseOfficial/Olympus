using Moq;
using Olympus.Data;
using Olympus.Rotation.AresCore.Abilities;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.AresCore.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Timeline;
using Olympus.Timeline.Models;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AresCore.Modules;

/// <summary>
/// Verifies that WAR Inner Release is held when a high-confidence phase
/// transition is imminent, and fires normally when no transition is detected.
/// </summary>
public class BuffModulePhaseTransitionTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void InnerRelease_NotPushed_WhenPhaseTransitionImminent()
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
    public void InnerRelease_Pushed_WhenNoPhaseTransition()
    {
        var context = CreateContext(timelineService: null);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Single(queue);
        Assert.Equal(AresAbilities.InnerRelease, queue[0].Behavior);
    }

    private static IAresContext CreateContext(ITimelineService? timelineService)
    {
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.AutoTankStance = false;
        config.Tank.EnableInnerRelease = true;
        config.Tank.EnableInfuriate = false; // isolate: only Inner Release can push

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(WARActions.InnerRelease.ActionId)).Returns(true);

        var player = MockBuilders.CreateMockPlayerCharacter(level: 100);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IAresContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(true);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TimelineService).Returns(timelineService);
        mock.Setup(x => x.HasInnerRelease).Returns(false);
        mock.Setup(x => x.HasSurgingTempest).Returns(true);
        mock.Setup(x => x.SurgingTempestRemaining).Returns(30f);
        mock.Setup(x => x.BeastGauge).Returns(50); // bypass: if (BeastGauge < 50 && SurgingTempestRemaining > 15f) return
        mock.Setup(x => x.Debug).Returns(new AresDebugState());
        return mock.Object;
    }
}
