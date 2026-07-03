using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Modules.Healing;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Scheduler-push tests for ProtractionHandler.
/// Key gates: EnableProtraction toggle, level >= 86, IsActionReady,
/// target found, !IsTargetReserved, hpPercent &lt;= ProtractionThreshold.
/// Protraction is a free oGCD (no Aetherflow cost) that increases max HP
/// by 10%, restores the equivalent HP, and buffs healing received for 10s.
/// </summary>
public class ProtractionHandlerTests
{
    private readonly ProtractionHandler _handler = new();

    [Fact]
    public void CollectCandidates_HappyPath_PushesProtractionAtPriority35()
    {
        // Arrange: target at 50% HP (below ProtractionThreshold default 0.70f)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableProtraction = true;
        // ProtractionThreshold defaults to 0.70f from ScholarConfig

        var injured = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Protraction.ActionId)).Returns(true);

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

        // Assert: Protraction pushed to oGCD queue at priority 35
        var ogcd = scheduler.InspectOgcdQueue();
        var candidate = Assert.Single(ogcd, c => c.Behavior == AthenaAbilities.Protraction);
        Assert.Equal(35, candidate.Priority);
        Assert.Equal(injured.Object.GameObjectId, candidate.TargetId);
    }

    [Fact]
    public void CollectCandidates_Disabled_DoesNotPush()
    {
        // Arrange: EnableProtraction = false
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableProtraction = false;

        var injured = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Protraction.ActionId)).Returns(true);

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
            c => c.Behavior == AthenaAbilities.Protraction);
    }

    [Fact]
    public void CollectCandidates_LevelTooLow_DoesNotPush()
    {
        // Arrange: level 85 (Protraction requires level 86)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableProtraction = true;

        var injured = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Protraction.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 85,  // below MinLevel 86
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: level gate blocks push
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Protraction);
    }

    [Fact]
    public void CollectCandidates_TargetHpAboveThreshold_DoesNotPush()
    {
        // Arrange: target at 80% HP > ProtractionThreshold (0.70f) — handler returns early
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableProtraction = true;
        // Default ProtractionThreshold = 0.70f

        // 80% HP: found by FindLowestHpPartyMember (not full) but above threshold
        var member = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 40000, maxHp: 50000);
        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara> { member.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Protraction.ActionId)).Returns(true);

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

        // Assert: HP threshold gate blocks push (80% > 70%)
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Protraction);
    }

    [Fact]
    public void CollectCandidates_NoPartyTarget_DoesNotPush()
    {
        // Arrange: empty party — FindLowestHpPartyMember returns null
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableProtraction = true;

        var partyHelper = new TestableAthenaPartyHelper(new List<IBattleChara>(), config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Protraction.ActionId)).Returns(true);

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

        // Assert: no target found — nothing pushed
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Protraction);
    }
}
