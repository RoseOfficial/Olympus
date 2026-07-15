using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ThanatosCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ThanatosCore.Modules;

/// <summary>
/// Push-through-dispatch tests for Thanatos (RPR) BuffModule.
/// Verifies the ArcaneCircle Toggle gate routes correctly through a real
/// DispatchOgcd pass. Catches wrong config-field wiring.
/// </summary>
public class BuffModuleDispatchTests
{
    private readonly BuffModule _module = new();

    [Fact]
    public void ArcaneCircle_Dispatches_WhenEnabled()
    {
        var config = ThanatosTestContext.CreateDefaultReaperConfiguration();
        config.Reaper.EnableArcaneCircle = true;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        var context = ThanatosTestContext.Create(
            config: config,
            actionService: actionService,
            hasArcaneCircle: false,
            hasDeathsDesign: true);   // bypass: if (!context.HasDeathsDesign) return;

        _module.CollectCandidates(context, scheduler, isMoving: false);
        var result = scheduler.DispatchOgcd(context);

        Assert.True(result.Dispatched);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.ArcaneCircle.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void ArcaneCircle_DoesNotDispatch_WhenDisabled()
    {
        var config = ThanatosTestContext.CreateDefaultReaperConfiguration();
        config.Reaper.EnableArcaneCircle = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        // hasDeathsDesign=true so the module guard passes and the candidate IS pushed.
        // The disabled EnableArcaneCircle Toggle is then the sole reason for rejection.
        var context = ThanatosTestContext.Create(
            config: config,
            actionService: actionService,
            hasArcaneCircle: false,
            hasDeathsDesign: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);
        scheduler.DispatchOgcd(context);

        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RPRActions.ArcaneCircle.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }
}
