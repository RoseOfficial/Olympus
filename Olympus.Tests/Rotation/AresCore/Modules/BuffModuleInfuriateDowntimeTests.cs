using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AresCore.Abilities;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.AresCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.AresCore.Modules;

/// <summary>
/// Verifies that BurstHoldHelper.ShouldDumpForDowntime bypasses the Infuriate
/// burst-hold gate when a downtime window is imminent.
/// </summary>
public class BuffModuleInfuriateDowntimeTests
{
    // -----------------------------------------------------------------------
    // 1. Burst imminent + no timeline → hold (do NOT push)
    // -----------------------------------------------------------------------

    [Fact]
    public void Infuriate_Hold_WhenBurstImminentAndNoTimeline()
    {
        var burstService = CreateImminent();
        var module = new BuffModule(burstService.Object);

        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.EnableInfuriate = true;
        config.Tank.EnableInnerRelease = false; // suppress InnerRelease path

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.GetCurrentCharges(WARActions.Infuriate.ActionId)).Returns(1u);
        actionService.Setup(x => x.IsActionReady(WARActions.Infuriate.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            timelineService: null, beastGauge: 0, hasInnerRelease: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == AresAbilities.Infuriate);
    }

    // -----------------------------------------------------------------------
    // 2. Downtime within 15s → dump (bypass burst hold, push Infuriate)
    // -----------------------------------------------------------------------

    [Fact]
    public void Infuriate_FiresDespiteBurstHold_WhenDowntimeImminent()
    {
        var burstService = CreateImminent();
        var module = new BuffModule(burstService.Object);

        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.EnableInfuriate = true;
        config.Tank.EnableInnerRelease = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.GetCurrentCharges(WARActions.Infuriate.ActionId)).Returns(1u);
        actionService.Setup(x => x.IsActionReady(WARActions.Infuriate.ActionId)).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            timelineService: timeline, beastGauge: 0, hasInnerRelease: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AresAbilities.Infuriate && c.Priority == 3);
    }

    // -----------------------------------------------------------------------
    // 3. Both charges capped → charge-cap escape fires even with no timeline
    // -----------------------------------------------------------------------

    [Fact]
    public void Infuriate_ChargeCapEscape_StillFires_WhenTwoCharges()
    {
        var burstService = CreateImminent();
        var module = new BuffModule(burstService.Object);

        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.EnableInfuriate = true;
        config.Tank.EnableInnerRelease = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.GetCurrentCharges(WARActions.Infuriate.ActionId)).Returns(2u);
        actionService.Setup(x => x.IsActionReady(WARActions.Infuriate.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            timelineService: null, beastGauge: 0, hasInnerRelease: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        // charges=2 is not < 2, so the entire hold block is skipped regardless of downtime
        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AresAbilities.Infuriate && c.Priority == 3);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<IBurstWindowService> CreateImminent()
    {
        var svc = new Mock<IBurstWindowService>();
        svc.Setup(x => x.IsInBurstWindow).Returns(false);
        svc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);
        return svc;
    }

    private static Mock<ITimelineService> CreateDowntimeTimeline(float secondsUntil)
    {
        var tl = new Mock<ITimelineService>();
        tl.Setup(x => x.Confidence).Returns(1.0f);
        tl.Setup(x => x.SecondsUntilNextUntargetablePhase()).Returns((float?)secondsUntil);
        return tl;
    }

    private static IAresContext CreateContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITimelineService>? timelineService = null,
        int beastGauge = 0,
        bool hasInnerRelease = false,
        bool inCombat = true,
        byte level = 100)
    {
        config ??= AresTestContext.CreateDefaultWarriorConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList)
            .Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IAresContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(MockBuilders.CreateMockTargetingService().Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns(timelineService?.Object);
        mock.Setup(x => x.Debug).Returns(new Olympus.Rotation.AresCore.Context.AresDebugState());

        // WAR gauge
        mock.Setup(x => x.BeastGauge).Returns(beastGauge);
        mock.Setup(x => x.HasInnerRelease).Returns(hasInnerRelease);
        mock.Setup(x => x.HasNascentChaos).Returns(false);
        mock.Setup(x => x.HasSurgingTempest).Returns(false); // skip InnerRelease early checks
        mock.Setup(x => x.SurgingTempestRemaining).Returns(0f);
        mock.Setup(x => x.HasDefiance).Returns(true);        // suppress TankStance push

        // Suppress defensive paths that read properties
        mock.Setup(x => x.HasHolmgang).Returns(false);
        mock.Setup(x => x.HasVengeance).Returns(false);
        mock.Setup(x => x.HasBloodwhetting).Returns(false);
        mock.Setup(x => x.HasActiveMitigation).Returns(false);
        mock.Setup(x => x.DamageIntakeService)
            .Returns(MockBuilders.CreateMockDamageIntakeService().Object);
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        return mock.Object;
    }
}
