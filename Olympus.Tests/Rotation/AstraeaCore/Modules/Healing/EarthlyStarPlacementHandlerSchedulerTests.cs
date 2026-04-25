using System.Numerics;
using Olympus.Data;
using Olympus.Rotation.AstraeaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules.Healing;

/// <summary>
/// Scheduler-push tests for EarthlyStarPlacementHandler — the canonical ground-targeted
/// push case. The candidate must have GroundPosition set (TargetId = 0) so the scheduler
/// dispatches via ExecuteGroundTargetedOgcd.
/// </summary>
public class EarthlyStarPlacementHandlerSchedulerTests
{
    private readonly EarthlyStarPlacementHandler _handler = new();

    [Fact]
    public void CollectCandidates_PartyLowHp_PushesGroundTargetedCandidate()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEarthlyStar = true;
        config.Astrologian.StarPlacement = Olympus.Config.EarthlyStarPlacementStrategy.OnSelf;
        config.Astrologian.EarthlyStarDetonateThreshold = 0.85f;

        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: false);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.EarthlyStar.ActionId)).Returns(true);

        // Party at 60% HP — well below the 85% threshold so reactive placement triggers.
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 4);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            earthlyStarService: earthlyStarService,
            level: 100,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        var starCandidate = Assert.Single(queue, c => c.Behavior.Action.ActionId == ASTActions.EarthlyStar.ActionId);

        // Ground-targeted: GroundPosition set, TargetId = 0
        Assert.NotNull(starCandidate.GroundPosition);
        Assert.Equal(0ul, starCandidate.TargetId);
    }

    [Fact]
    public void CollectCandidates_StarAlreadyPlaced_PushesNothing()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEarthlyStar = true;

        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: true);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            earthlyStarService: earthlyStarService,
            level: 100,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == ASTActions.EarthlyStar.ActionId);
    }

    [Fact]
    public void CollectCandidates_ManualPlacementStrategy_PushesNothing()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEarthlyStar = true;
        config.Astrologian.StarPlacement = Olympus.Config.EarthlyStarPlacementStrategy.Manual;

        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: false);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.EarthlyStar.ActionId)).Returns(true);

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 4);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            earthlyStarService: earthlyStarService,
            level: 100,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == ASTActions.EarthlyStar.ActionId);
    }

    [Fact]
    public void CollectCandidates_PartyHealthy_NoRaidwide_PushesNothing()
    {
        // Without raidwide/burst proactive trigger and party HP above threshold, don't reactively place.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEarthlyStar = true;
        config.Astrologian.StarPlacement = Olympus.Config.EarthlyStarPlacementStrategy.OnSelf;
        config.Astrologian.EarthlyStarDetonateThreshold = 0.50f; // Strict — party at 100% won't trigger.

        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: false);
        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.EarthlyStar.ActionId)).Returns(true);

        // Party at 100% HP — no need to place reactively.
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 4, injuredCount: 0);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            earthlyStarService: earthlyStarService,
            level: 100,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == ASTActions.EarthlyStar.ActionId);
    }
}
