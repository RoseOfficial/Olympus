using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Helpers;
using Olympus.Rotation.AstraeaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules.Healing;

public class MicrocosmosHandlerTests
{
    private readonly MicrocosmosHandler _handler = new();

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMacrocosmos = false;

        var context = AstraeaTestContext.Create(config: config, canExecuteOgcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMacrocosmos = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Microcosmos.ActionId)).Returns(false);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        // avgHp > 0.70f && injured < 3 → skip
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMacrocosmos = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Microcosmos.ActionId)).Returns(true);

        // All healthy (96% HP), so avgHp > 0.70 and injured = 0
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 8, injuredCount: 0);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenMacrocosmosActive_LowParty_Executes()
    {
        // Use Mock<IAstraeaContext> to inject HasMacrocosmos = true
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMacrocosmos = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Microcosmos.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Microcosmos.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var player = MockBuilders.CreateMockPlayerCharacter(level: 100);
        var context = new Mock<IAstraeaContext>();

        context.Setup(x => x.Configuration).Returns(config);
        context.Setup(x => x.Player).Returns(player.Object);
        context.Setup(x => x.ActionService).Returns(actionService.Object);
        context.Setup(x => x.HasMacrocosmos).Returns(true);
        // avgHp <= 0.70 → injured condition passes
        context.Setup(x => x.PartyHealthMetrics).Returns((0.50f, 0.30f, 4));
        context.Setup(x => x.HealingCoordination).Returns(new HealingCoordinationState());
        context.Setup(x => x.Debug).Returns(new AstraeaDebugState());
        context.Setup(x => x.TrainingService).Returns((Olympus.Services.Training.ITrainingService?)null);

        var result = _handler.TryExecute(context.Object, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Microcosmos.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }
}
