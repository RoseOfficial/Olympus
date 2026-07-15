using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.TerpsichoreCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.TerpsichoreCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Terpsichore (DNC) BuffModule.
/// Verifies the Devilment Toggle gate routes correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void Devilment_Dispatches_WhenEnabled()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableDevilment = true;
        config.Dancer.UseDevilmentAfterTechnical = true;
        config.Dancer.EnableTechnicalStep = false; // block TechnicalStep (priority 2) so Devilment (priority 3) wins

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
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            hasDancePartner: true,     // skip ClosedPosition push
            hasStandardFinish: true,   // skip first StandardStep push
            hasTechnicalFinish: true,  // satisfies Devilment shouldUse condition
            hasDevilment: false);      // allow Devilment push

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Devilment.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void Devilment_DoesNotDispatch_WhenDisabled()
    {
        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableDevilment = false;
        config.Dancer.EnableTechnicalStep = false;

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
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            hasDancePartner: true,
            hasStandardFinish: true,
            hasTechnicalFinish: true,
            hasDevilment: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == DNCActions.Devilment.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
