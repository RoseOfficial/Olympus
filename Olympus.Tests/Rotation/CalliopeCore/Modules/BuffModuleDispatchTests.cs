using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.CalliopeCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.CalliopeCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Calliope (BRD) BuffModule.
/// Verifies the PitchPerfect Toggle gate routes correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void PitchPerfect_Dispatches_WhenEnabled()
    {
        var config = CalliopeTestContext.CreateDefaultBardConfiguration();
        config.Bard.EnablePitchPerfect = true;

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
        var context = CalliopeTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            isWanderersMinuetActive: true,
            repertoire: 3); // at PitchPerfectMinStacks threshold

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.PitchPerfect.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void PitchPerfect_DoesNotDispatch_WhenDisabled()
    {
        var config = CalliopeTestContext.CreateDefaultBardConfiguration();
        config.Bard.EnablePitchPerfect = false;

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
        var context = CalliopeTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            isWanderersMinuetActive: true,
            repertoire: 3);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BRDActions.PitchPerfect.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
