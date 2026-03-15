using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Helpers;
using Olympus.Rotation.AstraeaCore.Modules.Healing;
using Olympus.Rotation.Common;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules.Healing;

public class HoroscopeDetonationHandlerTests
{
    private readonly HoroscopeDetonationHandler _handler = new();

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHoroscope = false;

        var context = AstraeaTestContext.Create(config: config, canExecuteOgcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHoroscope = true;

        // HasHoroscope defaults false (null StatusList), so this also tests HoroscopeNotActive,
        // but we stub action ready to verify the cooldown path specifically
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.HoroscopeEnd.ActionId)).Returns(false);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenHoroscopeNotActive_Skips()
    {
        // With null StatusList, HasHoroscope and HasHoroscopeHelios both return false
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHoroscope = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.HoroscopeEnd.ActionId)).Returns(true);

        // Low HP so threshold guard wouldn't block, but HasHoroscope is false → skip
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 5);
        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHoroscope = true;
        config.Astrologian.HoroscopeThreshold = 0.60f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.HoroscopeEnd.ActionId)).Returns(true);

        // Party at 90% HP — above 60% threshold
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 8, injuredCount: 0);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenHoroscopeActive_LowParty_Executes()
    {
        // Use Mock<IAstraeaContext> directly to inject HasHoroscope = true
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHoroscope = true;
        config.Astrologian.HoroscopeThreshold = 0.80f;
        config.Astrologian.HoroscopeMinTargets = 1;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.HoroscopeEnd.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.HoroscopeEnd.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = new Mock<IAstraeaContext>();
        var player = MockBuilders.CreateMockPlayerCharacter(level: 76);

        context.Setup(x => x.Configuration).Returns(config);
        context.Setup(x => x.Player).Returns(player.Object);
        context.Setup(x => x.ActionService).Returns(actionService.Object);
        context.Setup(x => x.HasHoroscope).Returns(true);
        context.Setup(x => x.HasHoroscopeHelios).Returns(false);
        context.Setup(x => x.PartyHealthMetrics).Returns((0.50f, 0.30f, 3));
        context.Setup(x => x.HealingCoordination).Returns(new HealingCoordinationState());
        context.Setup(x => x.Debug).Returns(new AstraeaDebugState());
        context.Setup(x => x.TrainingService).Returns((Olympus.Services.Training.ITrainingService?)null);

        var result = _handler.TryExecute(context.Object, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.HoroscopeEnd.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }
}
