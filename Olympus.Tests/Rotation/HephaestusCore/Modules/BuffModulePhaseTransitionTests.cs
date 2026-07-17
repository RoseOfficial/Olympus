using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HephaestusCore.Abilities;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.HephaestusCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Timeline;
using Olympus.Timeline.Models;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.HephaestusCore.Modules;

/// <summary>
/// Verifies that GNB No Mercy is held when a high-confidence phase
/// transition is imminent, and fires normally when no transition is detected.
/// </summary>
public class BuffModulePhaseTransitionTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void NoMercy_NotPushed_WhenPhaseTransitionImminent()
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
    public void NoMercy_Pushed_WhenNoPhaseTransition()
    {
        var context = CreateContext(timelineService: null);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Single(queue);
        Assert.Equal(GnbAbilities.NoMercy, queue[0].Behavior);
    }

    private static IHephaestusContext CreateContext(ITimelineService? timelineService)
    {
        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        config.Tank.EnableNoMercy = true;
        config.Tank.EnableBloodfest = false; // isolate: only No Mercy can push

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(GNBActions.NoMercy.ActionId)).Returns(true);
        actionService.Setup(x => x.GetCooldownRemaining(GNBActions.NoMercy.ActionId)).Returns(20f);

        var player = MockBuilders.CreateMockPlayerCharacter(level: 100);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IHephaestusContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(true);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TimelineService).Returns(timelineService);
        mock.Setup(x => x.HasNoMercy).Returns(false);
        mock.Setup(x => x.Cartridges).Returns(2); // bypass: level>=90 and Cartridges<1 gate
        mock.Setup(x => x.Debug).Returns(new HephaestusDebugState());
        return mock.Object;
    }
}
