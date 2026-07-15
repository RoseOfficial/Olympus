using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.CirceCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.CirceCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Circe (RDM) BuffModule.
/// Verifies the Embolden Toggle gate routes correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void Embolden_Dispatches_WhenEnabled()
    {
        var config = CirceTestContext.CreateDefaultRdmConfiguration();
        config.RedMage.EnableEmbolden = true;
        // Leave FindEnemy returning null (default) so target-requiring abilities
        // (Fleche, ContreSixte, ViceOfThorns, Prefulgence at priority 1) return
        // early, letting Embolden (priority 2, self-targeted) dispatch.

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = CirceTestContext.Create(
            config: config,
            actionService: actionService,
            emboldenReady: true,
            hasEmbolden: false,
            canStartMeleeCombo: true); // satisfies the melee-combo gate in TryPushEmbolden

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Embolden.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void Embolden_DoesNotDispatch_WhenDisabled()
    {
        var config = CirceTestContext.CreateDefaultRdmConfiguration();
        config.RedMage.EnableEmbolden = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = CirceTestContext.Create(
            config: config,
            actionService: actionService,
            emboldenReady: true,
            hasEmbolden: false,
            canStartMeleeCombo: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RDMActions.Embolden.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
