using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.HecateCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.HecateCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Hecate (BLM) BuffModule.
/// Verifies the Triplecast Toggle gate routes correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void Triplecast_Dispatches_WhenEnabled()
    {
        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.BlackMage.EnableTriplecast = true;
        config.BlackMage.EnableAmplifier = false; // block Amplifier (priority 1) so Triplecast (priority 3) wins
        config.BlackMage.EnableLeyLines = false;  // LeyLines blocked by isMoving=true anyway, but be explicit

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = HecateTestContext.Create(
            config: config,
            actionService: actionService,
            isMoving: true,
            triplecastCharges: 1, // charges available
            hasTriplecast: false,  // triplecastStacks=0 (no active Triplecast buff)
            hasInstantCast: false, // no instant cast → isMoving path activates
            amplifierReady: false);

        _module.CollectCandidates(context, scheduler, isMoving: true);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Triplecast.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void Triplecast_DoesNotDispatch_WhenDisabled()
    {
        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.BlackMage.EnableTriplecast = false;
        config.BlackMage.EnableAmplifier = false;
        config.BlackMage.EnableLeyLines = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = HecateTestContext.Create(
            config: config,
            actionService: actionService,
            isMoving: true,
            triplecastCharges: 1,
            hasTriplecast: false,
            hasInstantCast: false,
            amplifierReady: false);

        _module.CollectCandidates(context, scheduler, isMoving: true);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Triplecast.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
