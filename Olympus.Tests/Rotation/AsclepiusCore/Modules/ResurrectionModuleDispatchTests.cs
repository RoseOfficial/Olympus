using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Asclepius (SGE) ResurrectionModule.
/// Verifies the Swiftcast Toggle gate (cfg.Resurrection.EnableRaise) routes
/// correctly through a real DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class ResurrectionModuleDispatchTests
{
    private readonly ResurrectionModule _module = new();

    [Fact]
    public void Swiftcast_Dispatches_WhenRaiseEnabled()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = true;

        var dead = MockBuilders.CreateMockBattleChara(entityId: 2u, currentHp: 0, maxHp: 50000, isDead: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper(deadMember: dead.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = AsclepiusTestContext.Create(
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
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = false;

        var dead = MockBuilders.CreateMockBattleChara(entityId: 2u, currentHp: 0, maxHp: 50000, isDead: true);
        var partyHelper = MockBuilders.CreateMockPartyHelper(deadMember: dead.Object);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RoleActions.Swiftcast.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
