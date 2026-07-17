using Moq;
using Olympus.Data;
using Olympus.Rotation.AstraeaCore.Modules.Healing;
using Olympus.Services.Prediction;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules.Healing;

/// <summary>
/// Scheduler-push tests for MacrocosmosHandler.
/// Proactive branch: fires when a raidwide is imminent even if party HP is healthy.
/// Reactive branch: fires when party HP falls below the configured threshold.
/// </summary>
public class MacrocosmosHandlerSchedulerTests
{
    private readonly MacrocosmosHandler _handler = new();

    [Fact]
    public void CollectCandidates_RaidwideImminent_HealthyParty_PushesGcdCandidate()
    {
        // Arrange: party at 96% HP (above threshold) but a raidwide is predicted.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMacrocosmos = true;
        config.Astrologian.MacrocosmosThreshold = 0.80f;
        config.Astrologian.MacrocosmosMinTargets = 4;
        config.Healing.EnableMechanicAwareness = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Macrocosmos.ActionId)).Returns(true);

        // 4 healthy members at 96% HP -- average HP well above 80% threshold.
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 4, injuredCount: 0);

        var bossMechanicDetector = new Mock<IBossMechanicDetector>();
        bossMechanicDetector.SetupGet(x => x.IsRaidwideImminent).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            bossMechanicDetector: bossMechanicDetector,
            level: 90,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: proactive push present
        Assert.Single(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == ASTActions.Macrocosmos.ActionId);
    }

    [Fact]
    public void CollectCandidates_NoRaidwide_HealthyParty_PushesNothing()
    {
        // Arrange: party at 96% HP, no raidwide detected -- should not push.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMacrocosmos = true;
        config.Astrologian.MacrocosmosThreshold = 0.80f;
        config.Astrologian.MacrocosmosMinTargets = 4;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Macrocosmos.ActionId)).Returns(true);

        // 4 healthy members -- average HP well above threshold; bossMechanicDetector is null.
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 4, injuredCount: 0);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: nothing pushed
        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == ASTActions.Macrocosmos.ActionId);
    }

    [Fact]
    public void CollectCandidates_ReactivePathUnchanged_LowHp_NoRaidwide_PushesGcdCandidate()
    {
        // Arrange: party at 50% HP (below threshold), no raidwide -- reactive path fires.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMacrocosmos = true;
        config.Astrologian.MacrocosmosThreshold = 0.80f;
        config.Astrologian.MacrocosmosMinTargets = 4;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Macrocosmos.ActionId)).Returns(true);

        // 4 injured members at 50% HP -- average HP below the 80% threshold.
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 4);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: reactive push present
        Assert.Single(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == ASTActions.Macrocosmos.ActionId);
    }
}
