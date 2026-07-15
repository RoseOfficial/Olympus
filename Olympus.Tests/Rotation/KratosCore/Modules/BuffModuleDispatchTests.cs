using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.KratosCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.KratosCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Kratos (MNK) BuffModule.
/// Verifies the RiddleOfFire Toggle gate routes correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void RiddleOfFire_Dispatches_WhenEnabled()
    {
        var config = KratosTestContext.CreateDefaultMonkConfiguration();
        config.Monk.EnableRiddleOfFire = true;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = KratosTestContext.Create(
            config: config,
            actionService: actionService,
            hasDisciplinedFist: true,
            hasRiddleOfFire: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.RiddleOfFire.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void RiddleOfFire_DoesNotDispatch_WhenDisabled()
    {
        var config = KratosTestContext.CreateDefaultMonkConfiguration();
        config.Monk.EnableRiddleOfFire = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = KratosTestContext.Create(
            config: config,
            actionService: actionService,
            hasDisciplinedFist: true,
            hasRiddleOfFire: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == MNKActions.RiddleOfFire.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
