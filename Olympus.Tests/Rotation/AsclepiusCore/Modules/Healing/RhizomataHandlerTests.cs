using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

public class RhizomataHandlerTests
{
    private readonly RhizomataHandler _handler = new();

    [Fact]
    public void TryExecute_WhenAddersgallEmpty_ExecutesRhizomata()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Rhizomata.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Rhizomata.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            actionService: actionService,
            level: 74, // Rhizomata min level
            canExecuteOgcd: true,
            addersgallStacks: 0); // empty

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Rhizomata.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Rhizomata.ActionId)).Returns(false);

        var context = AsclepiusTestContext.Create(
            actionService: actionService,
            canExecuteOgcd: true,
            level: 74,
            addersgallStacks: 0);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAddersgallFull_Skips()
    {
        // At max stacks (3) — should not use Rhizomata to avoid overcap
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Rhizomata.ActionId)).Returns(true);

        var context = AsclepiusTestContext.Create(
            actionService: actionService,
            canExecuteOgcd: true,
            level: 74,
            addersgallStacks: 3); // full

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAtTwoStacksAndNotAboutToCap_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.PreventAddersgallCap = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Rhizomata.ActionId)).Returns(true);

        // 2 stacks, but timer is 15 seconds away (not about to cap)
        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 74,
            addersgallStacks: 2,
            addersgallTimer: 15f); // not about to cap (> 5f threshold)

        // Should skip since not about to cap and stacks != 0
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
