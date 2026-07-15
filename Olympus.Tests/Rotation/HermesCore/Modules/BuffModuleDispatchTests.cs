using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.HermesCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.HermesCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Hermes (NIN) BuffModule.
/// Verifies the KunaisBane Toggle gate routes correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void KunaisBane_Dispatches_WhenEnabled()
    {
        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.EnableKunaisBane = true;
        config.Ninja.EnableTenriJindo = false; // block priority-1 TenriJindo so KunaisBane wins

        var enemy = new Mock<IBattleNpc>();
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(),
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = HermesTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            hasSuiton: true,
            hasTenriJindoReady: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.KunaisBane.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void KunaisBane_DoesNotDispatch_WhenDisabled()
    {
        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.EnableKunaisBane = false;
        config.Ninja.EnableTenriJindo = false;

        var enemy = new Mock<IBattleNpc>();
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(),
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = HermesTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            hasSuiton: true,
            hasTenriJindoReady: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == NINActions.KunaisBane.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
