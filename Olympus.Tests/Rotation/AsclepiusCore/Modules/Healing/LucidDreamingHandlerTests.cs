using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

public class LucidDreamingHandlerTests
{
    private readonly LucidDreamingHandler _handler = new();

    [Fact]
    public void TryExecute_WhenMpLowAndReady_ExecutesLucidDreaming()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.LucidDreamingThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(RoleActions.LucidDreaming.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == RoleActions.LucidDreaming.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // currentMp=5000, maxMp=10000 → 50% < threshold 80%
        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            level: 14, // LucidDreaming min level
            canExecuteOgcd: true,
            currentMp: 5000);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == RoleActions.LucidDreaming.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(RoleActions.LucidDreaming.ActionId)).Returns(false);

        var context = AsclepiusTestContext.Create(
            actionService: actionService,
            canExecuteOgcd: true,
            level: 14,
            currentMp: 5000);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenMpAboveThreshold_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.LucidDreamingThreshold = 0.70f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(RoleActions.LucidDreaming.ActionId)).Returns(true);

        // currentMp=9000, maxMp=10000 → 90% > threshold 70%
        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 14,
            currentMp: 9000);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
