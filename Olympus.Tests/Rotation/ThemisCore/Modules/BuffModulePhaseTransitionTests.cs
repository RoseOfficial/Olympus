using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.ThemisCore.Abilities;
using Olympus.Rotation.ThemisCore.Context;
using Olympus.Rotation.ThemisCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Timeline;
using Olympus.Timeline.Models;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ThemisCore.Modules;

/// <summary>
/// Verifies that PLD Fight or Flight is held when a high-confidence phase
/// transition is imminent, and fires normally when no transition is detected.
/// </summary>
public class BuffModulePhaseTransitionTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void FightOrFlight_NotPushed_WhenPhaseTransitionImminent()
    {
        var timeline = new Mock<ITimelineService>();
        timeline.Setup(x => x.GetNextMechanic(TimelineEntryType.Phase))
            .Returns(new MechanicPrediction(5f, TimelineEntryType.Phase, "Phase 2", 0.9f));

        var enemy = new Mock<IBattleNpc>();
        var context = CreateContext(timelineService: timeline.Object, enemy: enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void FightOrFlight_Pushed_WhenNoPhaseTransition()
    {
        var enemy = new Mock<IBattleNpc>();
        var context = CreateContext(timelineService: null, enemy: enemy.Object);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Single(queue);
        Assert.Equal(ThemisAbilities.FightOrFlight, queue[0].Behavior);
    }

    private static IThemisContext CreateContext(ITimelineService? timelineService, IBattleNpc? enemy)
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.AutoTankStance = false;
        config.Tank.EnableFightOrFlight = true;
        config.Tank.EnableRequiescat = false; // isolate: only Fight or Flight can push

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(PLDActions.FightOrFlight.ActionId)).Returns(true);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemyForAction(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<uint>(),
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy);

        var player = MockBuilders.CreateMockPlayerCharacter(level: 100);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IThemisContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(true);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targetingService.Object);
        mock.Setup(x => x.TimelineService).Returns(timelineService);
        mock.Setup(x => x.HasFightOrFlight).Returns(false);
        mock.Setup(x => x.HasRequiescat).Returns(false);
        mock.Setup(x => x.ComboStep).Returns(0); // goodTiming = ComboStep <= 1 = true
        mock.Setup(x => x.HasSwordOath).Returns(false);
        mock.Setup(x => x.Debug).Returns(new ThemisDebugState());
        return mock.Object;
    }
}
