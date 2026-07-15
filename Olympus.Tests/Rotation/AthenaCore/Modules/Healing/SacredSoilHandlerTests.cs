using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.AthenaCore.Helpers;
using Olympus.Rotation.AthenaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Scheduler-push tests for SacredSoilHandler.
/// Key gates: EnableSacredSoil toggle, level >= 50, IsActionReady,
/// AetherflowStacks > AetherflowReserve, avgHp &lt;= SacredSoilThreshold OR raidwideImminent,
/// membersInRange >= SacredSoilMinTargets,
/// WasActionUsedByOther(SacredSoil, 15f) == false (remote mit overlap window).
/// </summary>
public class SacredSoilHandlerTests
{
    private readonly SacredSoilHandler _handler = new();

    // -----------------------------------------------------------------------
    // Basic gate tests using AthenaTestContext.Create
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_HappyPath_PushesSacredSoilAtPriority30()
    {
        // Arrange: 4 members at 50% HP all at origin (within radius 10)
        // avgHp=0.50 < threshold(0.75), membersInRange=4 >= min(3)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSacredSoil = true;
        config.Scholar.SacredSoilThreshold = 0.75f;
        config.Scholar.SacredSoilMinTargets = 3;
        config.Scholar.AetherflowReserve = 1;

        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000, position: Vector3.Zero).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 25000, maxHp: 50000, position: Vector3.Zero).Object,
            MockBuilders.CreateMockBattleChara(entityId: 12u, currentHp: 25000, maxHp: 50000, position: Vector3.Zero).Object,
            MockBuilders.CreateMockBattleChara(entityId: 13u, currentHp: 25000, maxHp: 50000, position: Vector3.Zero).Object,
        };
        var partyHelper = new TestableAthenaPartyHelper(members, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.SacredSoil.ActionId)).Returns(true);

        var aetherflow = AthenaTestContext.CreateMockAetherflowService(currentStacks: 3);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflow,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: SacredSoil pushed to oGCD queue at priority 30 via ground-targeted path
        var ogcd = scheduler.InspectOgcdQueue();
        var candidate = Assert.Single(ogcd, c => c.Behavior == AthenaAbilities.SacredSoil);
        Assert.Equal(30, candidate.Priority);
        Assert.True(candidate.GroundPosition.HasValue, "Sacred Soil must be pushed via PushGroundTargetedOgcd (GroundPosition must be set)");
    }

    [Fact]
    public void CollectCandidates_Disabled_DoesNotPush()
    {
        // Arrange: EnableSacredSoil = false
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSacredSoil = false;

        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 12u, currentHp: 25000, maxHp: 50000).Object,
        };
        var partyHelper = new TestableAthenaPartyHelper(members, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.SacredSoil.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: toggle gate blocks push
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.SacredSoil);
    }

    [Fact]
    public void CollectCandidates_InsufficientAetherflow_DoesNotPush()
    {
        // Arrange: stacks == reserve (1 == 1)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSacredSoil = true;
        config.Scholar.SacredSoilThreshold = 0.75f;
        config.Scholar.SacredSoilMinTargets = 3;
        config.Scholar.AetherflowReserve = 1;

        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 12u, currentHp: 25000, maxHp: 50000).Object,
        };
        var partyHelper = new TestableAthenaPartyHelper(members, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.SacredSoil.ActionId)).Returns(true);

        // stacks=1, reserve=1: 1 <= 1 — handler returns early
        var aetherflow = AthenaTestContext.CreateMockAetherflowService(currentStacks: 1);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflow,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: aetherflow gate blocks push
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.SacredSoil);
    }

    [Fact]
    public void CollectCandidates_PartyHpHealthy_NoRaidwide_DoesNotPush()
    {
        // Arrange: party at 96% HP — avgHp=0.96 > SacredSoilThreshold(0.75), no raidwide
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSacredSoil = true;
        config.Scholar.SacredSoilThreshold = 0.75f;
        config.Scholar.SacredSoilMinTargets = 3;
        config.Scholar.AetherflowReserve = 1;

        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 48000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 48000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 12u, currentHp: 48000, maxHp: 50000).Object,
        };
        var partyHelper = new TestableAthenaPartyHelper(members, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.SacredSoil.ActionId)).Returns(true);

        var aetherflow = AthenaTestContext.CreateMockAetherflowService(currentStacks: 3);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflow,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: party HP above threshold and no raidwide — gate blocks push
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.SacredSoil);
    }

    [Fact]
    public void CollectCandidates_NotEnoughMembersInRange_DoesNotPush()
    {
        // Arrange: empty party — membersInRange=0 < SacredSoilMinTargets(3)
        // avgHp passes (1f default for empty party), but no members in range
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSacredSoil = true;
        config.Scholar.SacredSoilThreshold = 0.75f;
        config.Scholar.SacredSoilMinTargets = 3;
        config.Scholar.AetherflowReserve = 1;

        // Empty party: CalculatePartyHealthMetrics returns (1f, 1f, 0) for empty
        // which fails the avgHp gate already. To specifically hit the membersInRange gate,
        // provide injured members but force threshold to fail... or use 2 injured members:
        var twoMembers = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000, position: Vector3.Zero).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 25000, maxHp: 50000, position: Vector3.Zero).Object,
            // Only 2 members — below SacredSoilMinTargets = 3
        };
        var partyHelper = new TestableAthenaPartyHelper(twoMembers, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.SacredSoil.ActionId)).Returns(true);

        var aetherflow = AthenaTestContext.CreateMockAetherflowService(currentStacks: 3);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowService: aetherflow,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: member count gate blocks push (2 < 3)
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.SacredSoil);
    }

    // -----------------------------------------------------------------------
    // Coordination gate tests — require partyCoordinationService injection,
    // so they construct AthenaContext directly via a private helper.
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_RemoteSacredSoilStillActive_SkipsAndSetsDebugState()
    {
        // Arrange: WasActionUsedByOther(SacredSoil, 15f) = true (remote instance's buff still up)
        // Handler should skip and set PlanningState = "Sacred Soil skipped (remote mit)"
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSacredSoil = true;
        config.Scholar.SacredSoilThreshold = 0.75f;
        config.Scholar.SacredSoilMinTargets = 3;
        config.Scholar.AetherflowReserve = 1;
        config.PartyCoordination.EnableCooldownCoordination = true;

        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000, position: Vector3.Zero).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 25000, maxHp: 50000, position: Vector3.Zero).Object,
            MockBuilders.CreateMockBattleChara(entityId: 12u, currentHp: 25000, maxHp: 50000, position: Vector3.Zero).Object,
        };
        var partyHelper = new TestableAthenaPartyHelper(members, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.SacredSoil.ActionId)).Returns(true);

        // Remote instance placed SacredSoil within the last 15s — WasActionUsedByOther = true
        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.WasActionUsedByOther(SCHActions.SacredSoil.ActionId, 15f)).Returns(true);
        partyCoord.Setup(x => x.WouldOverlapWithRemoteGroundEffect(
            It.IsAny<Vector3>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(false);

        var context = CreateContextWithCoordination(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowStacks: 3,
            partyCoordService: partyCoord.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: coordination gate blocks push and debug state is set
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.SacredSoil);
        Assert.Equal("Sacred Soil skipped (remote mit)", context.Debug.PlanningState);
    }

    [Fact]
    public void CollectCandidates_RemoteSacredSoilExpired_DoesPush()
    {
        // Arrange: WasActionUsedByOther(SacredSoil, 15f) = false (remote buff has worn off)
        // Handler should push normally since the overlap window has expired
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSacredSoil = true;
        config.Scholar.SacredSoilThreshold = 0.75f;
        config.Scholar.SacredSoilMinTargets = 3;
        config.Scholar.AetherflowReserve = 1;
        config.PartyCoordination.EnableCooldownCoordination = true;

        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000, position: Vector3.Zero).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 25000, maxHp: 50000, position: Vector3.Zero).Object,
            MockBuilders.CreateMockBattleChara(entityId: 12u, currentHp: 25000, maxHp: 50000, position: Vector3.Zero).Object,
        };
        var partyHelper = new TestableAthenaPartyHelper(members, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.SacredSoil.ActionId)).Returns(true);

        // Remote buff expired — WasActionUsedByOther = false, no ground overlap
        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.WasActionUsedByOther(SCHActions.SacredSoil.ActionId, 15f)).Returns(false);
        partyCoord.Setup(x => x.WouldOverlapWithRemoteGroundEffect(
            It.IsAny<Vector3>(), It.IsAny<uint>(), It.IsAny<float>())).Returns(false);
        partyCoord.Setup(x => x.IsAoEHealReservedByOther()).Returns(false);

        var context = CreateContextWithCoordination(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            aetherflowStacks: 3,
            partyCoordService: partyCoord.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: coordination window expired — SacredSoil pushed via ground-targeted path
        var ogcd = scheduler.InspectOgcdQueue();
        var candidate = Assert.Single(ogcd, c => c.Behavior == AthenaAbilities.SacredSoil);
        Assert.Equal(30, candidate.Priority);
        Assert.True(candidate.GroundPosition.HasValue, "Sacred Soil must be dispatched via ground-targeted path");
    }

    // -----------------------------------------------------------------------
    // Helper: constructs AthenaContext directly with an injected
    // IPartyCoordinationService (not exposed by AthenaTestContext.Create).
    // -----------------------------------------------------------------------
    private static AthenaContext CreateContextWithCoordination(
        Configuration config,
        TestableAthenaPartyHelper partyHelper,
        Mock<IActionService> actionService,
        int aetherflowStacks,
        IPartyCoordinationService? partyCoordService)
    {
        var player = MockBuilders.CreateMockPlayerCharacter(
            level: 100, currentHp: 50000, maxHp: 50000, currentMp: 10000);

        var aetherflow = AthenaTestContext.CreateMockAetherflowService(aetherflowStacks);
        var fairyGauge = AthenaTestContext.CreateMockFairyGaugeService(50);
        var fairyState = AthenaTestContext.CreateMockFairyStateManager();

        // SacredSoilHandler does not call IHealingSpellSelector — bare mock suffices.
        var healingSpellSelector = new Mock<IHealingSpellSelector>();

        return new AthenaContext(
            player: player.Object,
            inCombat: true,
            isMoving: false,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            actionService: actionService.Object,
            actionTracker: MockBuilders.CreateMockActionTracker(config),
            combatEventService: MockBuilders.CreateMockCombatEventService().Object,
            damageIntakeService: MockBuilders.CreateMockDamageIntakeService().Object,
            damageTrendService: MockBuilders.CreateMockDamageTrendService().Object,
            configuration: config,
            debuffDetectionService: MockBuilders.CreateMockDebuffDetectionService().Object,
            hpPredictionService: MockBuilders.CreateMockHpPredictionService().Object,
            mpForecastService: MockBuilders.CreateMockMpForecastService().Object,
            objectTable: MockBuilders.CreateMockObjectTable().Object,
            partyList: MockBuilders.CreateMockPartyList().Object,
            playerStatsService: MockBuilders.CreateMockPlayerStatsService().Object,
            targetingService: MockBuilders.CreateMockTargetingService().Object,
            aetherflowService: aetherflow.Object,
            fairyGaugeService: fairyGauge.Object,
            fairyStateManager: fairyState.Object,
            statusHelper: new AthenaStatusHelper(),
            partyHelper: partyHelper,
            cooldownPlanner: MockBuilders.CreateMockCooldownPlanner().Object,
            healingSpellSelector: healingSpellSelector.Object,
            partyCoordinationService: partyCoordService,
            debugState: new AthenaDebugState());
    }
}
