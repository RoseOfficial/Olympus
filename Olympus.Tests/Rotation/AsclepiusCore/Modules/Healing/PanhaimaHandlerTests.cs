using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

public class PanhaimaHandlerTests
{
    private readonly PanhaimaHandler _handler = new();

    [Fact]
    public void TryExecute_WhenPartyLowAndReady_ExecutesPanhaima()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.PanhaimaThreshold = 0.80f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.55f, 0.40f, 4));

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Panhaima.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Panhaima.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 80, // Panhaima min level
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Panhaima.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePanhaima = false;

        var context = AsclepiusTestContext.Create(config: config, canExecuteOgcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.55f, 0.40f, 4));

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Panhaima.ActionId)).Returns(false);

        var context = AsclepiusTestContext.Create(
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 80);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenPartyAboveThreshold_Skips()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.PanhaimaThreshold = 0.60f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((0.85f, 0.80f, 2)); // avg 85% > threshold 60%

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Panhaima.ActionId)).Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 80);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
