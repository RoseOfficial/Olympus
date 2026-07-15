using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.EchidnaCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.EchidnaCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Echidna (VPR) BuffModule.
/// Verifies the SerpentsIre Toggle gate routes correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void SerpentsIre_Dispatches_WhenEnabled()
    {
        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableSerpentsIre = true;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = EchidnaTestContext.Create(
            config: config,
            actionService: actionService,
            hasNoxiousGnash: true,       // bypass: if (!context.HasNoxiousGnash) return;
            hasHuntersInstinct: true,     // bypass: if (!context.HasHuntersInstinct || ...) return;
            hasSwiftscaled: true);        // bypass: if (... || !context.HasSwiftscaled) return;

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SerpentsIre.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void SerpentsIre_DoesNotDispatch_WhenDisabled()
    {
        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableSerpentsIre = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        // All three module guards must pass so the candidate IS pushed.
        // The disabled EnableSerpentsIre Toggle is then the sole reason for rejection.
        var context = EchidnaTestContext.Create(
            config: config,
            actionService: actionService,
            hasNoxiousGnash: true,
            hasHuntersInstinct: true,
            hasSwiftscaled: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == VPRActions.SerpentsIre.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
