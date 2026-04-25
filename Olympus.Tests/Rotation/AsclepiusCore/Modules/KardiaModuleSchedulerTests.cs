using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Scheduler-push tests for KardiaModule. The distinctive behavior is that Kardia
/// pushes at priority 0 — beating Resurrection (1-2) — so Kardia placement always
/// wins when no Kardia is on the tank.
/// </summary>
public class KardiaModuleSchedulerTests
{
    private readonly KardiaModule _module = new();

    [Fact]
    public void CollectCandidates_KardiaNotPlaced_PushesKardiaAtPriorityZero()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AutoKardia = true;

        var tank = MockBuilders.CreateMockBattleChara(entityId: 1u, currentHp: 100000, maxHp: 100000);
        tank.Setup(x => x.GameObjectId).Returns(0xDEAD0001ul);
        var partyHelper = new Mock<Olympus.Rotation.ApolloCore.Helpers.IPartyHelper>();
        partyHelper.Setup(p => p.FindTankInParty(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>())).Returns(tank.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true,
            hasKardiaPlaced: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        var kardiaCandidate = Assert.Single(queue, c => c.Behavior.Action.ActionId == SGEActions.Kardia.ActionId);
        Assert.Equal(0, kardiaCandidate.Priority);
        Assert.Equal(tank.Object.GameObjectId, kardiaCandidate.TargetId);
    }

    [Fact]
    public void CollectCandidates_KardiaAlreadyOnTank_PushesNothing()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AutoKardia = true;

        var tank = MockBuilders.CreateMockBattleChara(entityId: 1u, currentHp: 100000, maxHp: 100000);
        tank.Setup(x => x.GameObjectId).Returns(0xDEAD0001ul);
        var partyHelper = new Mock<Olympus.Rotation.ApolloCore.Helpers.IPartyHelper>();
        partyHelper.Setup(p => p.FindTankInParty(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>())).Returns(tank.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        // Kardia is already placed on the tank — TryPushPlaceKardia should skip; TryPushEnsureKardiaOnTank
        // should also skip (already on correct target).
        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true,
            hasKardiaPlaced: true,
            kardiaTargetId: tank.Object.GameObjectId);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(queue, c => c.Behavior.Action.ActionId == SGEActions.Kardia.ActionId);
    }

    [Fact]
    public void CollectCandidates_AutoKardiaDisabled_PushesNothing()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AutoKardia = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true,
            hasKardiaPlaced: false);

        var scheduler = SchedulerFactory.CreateForTest(actionService);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(queue, c => c.Behavior.Action.ActionId == SGEActions.Kardia.ActionId);
    }
}
