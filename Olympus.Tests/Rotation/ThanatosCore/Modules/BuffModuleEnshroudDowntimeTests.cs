using Dalamud.Game.ClientState.Objects.SubKinds;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ThanatosCore.Abilities;
using Olympus.Rotation.ThanatosCore.Context;
using Olympus.Rotation.ThanatosCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.ThanatosCore.Modules;

/// <summary>
/// Verifies the pre-downtime long-commit block for RPR Enshroud:
///   1. Enshroud is blocked when downtime is within 15s and shroud &lt; 90
///      (avoids starting an 11+ GCD sequence that would be cut short).
///   2. Shroud >= 90 is the escape hatch: overcap risk overrides the block.
/// </summary>
public class BuffModuleEnshroudDowntimeTests
{
    // -----------------------------------------------------------------------
    // Block: shroud < 90 + downtime within 15s → Enshroud NOT pushed
    // -----------------------------------------------------------------------

    [Fact]
    public void Enshroud_Blocked_WhenDowntimeWithin15s_AndShroudBelow90()
    {
        var module = new BuffModule(); // no burst service — isolates the downtime block

        var config = ThanatosTestContext.CreateDefaultReaperConfiguration();
        config.Reaper.EnableEnshroud = true;
        config.Reaper.EnableBurstPooling = false; // disable burst hold so only downtime gate is tested

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RPRActions.Enshroud.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(RPRActions.ArcaneCircle.ActionId)).Returns(false);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            timelineService: timeline,
            shroud: 50,
            hasDeathsDesign: true,
            deathsDesignRemaining: 20f);

        module.CollectCandidates(context, scheduler, isMoving: false);

        // shroud=50 < 90 AND downtime=5s within 15s window → block fires → NOT pushed
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ThanatosAbilities.Enshroud);
    }

    // -----------------------------------------------------------------------
    // Escape: shroud >= 90 → overcap risk overrides the 15s block
    // -----------------------------------------------------------------------

    [Fact]
    public void Enshroud_OvercapEscapes_Block_WhenShroud90()
    {
        var module = new BuffModule();

        var config = ThanatosTestContext.CreateDefaultReaperConfiguration();
        config.Reaper.EnableEnshroud = true;
        config.Reaper.EnableBurstPooling = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RPRActions.Enshroud.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(RPRActions.ArcaneCircle.ActionId)).Returns(false);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            timelineService: timeline,
            shroud: 90,      // >= 90 → overcap escape: block condition `shroud < 90` fails
            hasDeathsDesign: true,
            deathsDesignRemaining: 20f);

        module.CollectCandidates(context, scheduler, isMoving: false);

        // shroud=90 → block condition fails → continues → shouldEnshroud=true → pushed at priority 2
        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ThanatosAbilities.Enshroud && c.Priority == 2);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<ITimelineService> CreateDowntimeTimeline(float secondsUntil)
    {
        var tl = new Mock<ITimelineService>();
        tl.Setup(x => x.Confidence).Returns(1.0f);
        tl.Setup(x => x.SecondsUntilNextUntargetablePhase()).Returns((float?)secondsUntil);
        return tl;
    }

    private static IThanatosContext CreateContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITimelineService>? timelineService = null,
        int shroud = 50,
        bool hasDeathsDesign = false,
        float deathsDesignRemaining = 0f,
        bool inCombat = true,
        byte level = 100)
    {
        config ??= ThanatosTestContext.CreateDefaultReaperConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList)
            .Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var targeting = MockBuilders.CreateMockTargetingService();

        var mock = new Mock<IThanatosContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns(timelineService?.Object);
        mock.Setup(x => x.Debug).Returns(new ThanatosDebugState());
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        // RPR-specific defaults
        mock.Setup(x => x.Soul).Returns(0);
        mock.Setup(x => x.Shroud).Returns(shroud);
        mock.Setup(x => x.LemureShroud).Returns(0);
        mock.Setup(x => x.VoidShroud).Returns(0);
        mock.Setup(x => x.IsEnshrouded).Returns(false);
        mock.Setup(x => x.EnshroudTimer).Returns(0f);
        mock.Setup(x => x.HasSoulReaver).Returns(false);
        mock.Setup(x => x.HasArcaneCircle).Returns(false);
        mock.Setup(x => x.ArcaneCircleRemaining).Returns(0f);
        mock.Setup(x => x.HasDeathsDesign).Returns(hasDeathsDesign);
        mock.Setup(x => x.DeathsDesignRemaining).Returns(deathsDesignRemaining);
        mock.Setup(x => x.HasPerfectioParata).Returns(false);
        mock.Setup(x => x.ImmortalSacrificeStacks).Returns(0);

        return mock.Object;
    }
}
