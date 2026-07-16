using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ApolloCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Apollo (WHM) ResurrectionModule.
/// Verifies the Swiftcast Toggle gate (cfg.Resurrection.EnableRaise) routes
/// correctly through a real DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class ResurrectionModuleDispatchTests
{
    private readonly ResurrectionModule _module = new();

    [Fact]
    public void Swiftcast_Dispatches_WhenRaiseEnabled()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Resurrection.EnableRaise = true;

        var dead = MockBuilders.CreateMockBattleChara(entityId: 2u, currentHp: 0, maxHp: 50000, isDead: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper(deadMember: dead.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RoleActions.Swiftcast.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void Swiftcast_DoesNotDispatch_WhenRaiseDisabled()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Resurrection.EnableRaise = false;

        var dead = MockBuilders.CreateMockBattleChara(entityId: 2u, currentHp: 0, maxHp: 50000, isDead: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper(deadMember: dead.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RoleActions.Swiftcast.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryPushSwiftcast_DoesNotPush_WhenMpAboveAbsoluteButBelowPercentThreshold()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.50f;

        var dead = MockBuilders.CreateMockBattleChara(entityId: 2u, currentHp: 0, maxHp: 50000, isDead: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper(deadMember: dead.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        // currentMp 3000 / default maxMp 10000 = 30%, below 50% threshold — Swiftcast must NOT arm
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            currentMp: 3000);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RoleActions.Swiftcast.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryPushSwiftcast_Pushes_WhenMpAboveBothAbsoluteAndPercentThreshold()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.50f;

        var dead = MockBuilders.CreateMockBattleChara(entityId: 2u, currentHp: 0, maxHp: 50000, isDead: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper(deadMember: dead.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        // currentMp 6000 / default maxMp 10000 = 60%, above 50% threshold — Swiftcast must arm
        var context = ApolloTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            currentMp: 6000);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RoleActions.Swiftcast.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }
}
