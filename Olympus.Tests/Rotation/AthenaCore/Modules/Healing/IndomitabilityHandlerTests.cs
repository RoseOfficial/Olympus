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
/// Scheduler-push tests for IndomitabilityHandler.
/// Key gates: EnableIndomitability toggle, level >= 52, IsActionReady,
/// AetherflowStacks > AetherflowReserve (unless Recitation active),
/// avgHp &lt;= AoEHealThreshold AND injuredCount >= AoEHealMinTargets,
/// TryReserveAoEHeal succeeds (frame + remote coordination).
/// </summary>
public class IndomitabilityHandlerTests
{
    private readonly IndomitabilityHandler _handler = new();

    [Fact]
    public void CollectCandidates_HappyPath_PushesIndomitabilityAtPriority25()
    {
        // Arrange: 3 members at 50% HP — avgHp=0.50 <= threshold(0.70), injuredCount=3 >= min(3)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableIndomitability = true;
        config.Scholar.AoEHealThreshold = 0.70f;
        config.Scholar.AoEHealMinTargets = 3;
        config.Scholar.AetherflowReserve = 1;

        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 12u, currentHp: 25000, maxHp: 50000).Object,
        };
        var partyHelper = new TestableAthenaPartyHelper(members, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Indomitability.ActionId)).Returns(true);

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

        // Assert: Indomitability pushed to oGCD queue at priority 25
        var ogcd = scheduler.InspectOgcdQueue();
        var candidate = Assert.Single(ogcd, c => c.Behavior == AthenaAbilities.Indomitability);
        Assert.Equal(25, candidate.Priority);
    }

    [Fact]
    public void CollectCandidates_Disabled_DoesNotPush()
    {
        // Arrange: EnableIndomitability = false
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableIndomitability = false;

        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 12u, currentHp: 25000, maxHp: 50000).Object,
        };
        var partyHelper = new TestableAthenaPartyHelper(members, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Indomitability.ActionId)).Returns(true);

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
            c => c.Behavior == AthenaAbilities.Indomitability);
    }

    [Fact]
    public void CollectCandidates_LevelTooLow_DoesNotPush()
    {
        // Arrange: level 51 (Indomitability requires level 52)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableIndomitability = true;

        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 12u, currentHp: 25000, maxHp: 50000).Object,
        };
        var partyHelper = new TestableAthenaPartyHelper(members, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Indomitability.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 51,  // below MinLevel 52
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        // Act
        _handler.CollectCandidates(context, scheduler, isMoving: false);

        // Assert: level gate blocks push
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Indomitability);
    }

    [Fact]
    public void CollectCandidates_InsufficientAetherflowNoRecitation_DoesNotPush()
    {
        // Arrange: stacks == reserve (1 == 1), no Recitation (StatusList null => false)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableIndomitability = true;
        config.Scholar.AoEHealThreshold = 0.70f;
        config.Scholar.AoEHealMinTargets = 3;
        config.Scholar.AetherflowReserve = 1;

        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 12u, currentHp: 25000, maxHp: 50000).Object,
        };
        var partyHelper = new TestableAthenaPartyHelper(members, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Indomitability.ActionId)).Returns(true);

        // stacks=1, reserve=1: 1 <= 1 and no Recitation — handler returns early
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
            c => c.Behavior == AthenaAbilities.Indomitability);
    }

    [Fact]
    public void CollectCandidates_NotEnoughInjuredMembers_DoesNotPush()
    {
        // Arrange: only 2 members injured (< AoEHealMinTargets=3)
        // avgHp is low (0.50) but injuredCount=2 fails the gate
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableIndomitability = true;
        config.Scholar.AoEHealThreshold = 0.70f;
        config.Scholar.AoEHealMinTargets = 3;
        config.Scholar.AetherflowReserve = 1;

        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 25000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 25000, maxHp: 50000).Object,
            // only 2 injured members
        };
        var partyHelper = new TestableAthenaPartyHelper(members, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Indomitability.ActionId)).Returns(true);

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

        // Assert: injured count gate blocks push (2 < 3)
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Indomitability);
    }

    [Fact]
    public void CollectCandidates_PartyHpAboveThreshold_DoesNotPush()
    {
        // Arrange: 4 healthy members at 96% HP — avgHp=0.96 > threshold(0.70)
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableIndomitability = true;
        config.Scholar.AoEHealThreshold = 0.70f;
        config.Scholar.AoEHealMinTargets = 3;
        config.Scholar.AetherflowReserve = 1;

        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 48000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 11u, currentHp: 48000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 12u, currentHp: 48000, maxHp: 50000).Object,
            MockBuilders.CreateMockBattleChara(entityId: 13u, currentHp: 48000, maxHp: 50000).Object,
        };
        var partyHelper = new TestableAthenaPartyHelper(members, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Indomitability.ActionId)).Returns(true);

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

        // Assert: party HP above threshold — gate blocks push
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Indomitability);
    }
}
