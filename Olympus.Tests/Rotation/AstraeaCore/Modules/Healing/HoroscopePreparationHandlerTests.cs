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

public class HoroscopePreparationHandlerTests
{
    private readonly HoroscopePreparationHandler _handler = new();

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHoroscope = false;

        var context = AstraeaTestContext.Create(config: config, canExecuteOgcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAutoCastDisabled_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHoroscope = true;
        config.Astrologian.AutoCastHoroscope = false;

        var context = AstraeaTestContext.Create(config: config, canExecuteOgcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHoroscope = true;
        config.Astrologian.AutoCastHoroscope = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Horoscope.ActionId)).Returns(false);

        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 76,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenHoroscopeAlreadyActive_Skips()
    {
        // HasHoroscope = true → skip (already prepared)
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHoroscope = true;
        config.Astrologian.AutoCastHoroscope = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Horoscope.ActionId)).Returns(true);

        var player = MockBuilders.CreateMockPlayerCharacter(level: 76, currentHp: 25000, maxHp: 50000);

        var context = new Mock<IAstraeaContext>();
        context.Setup(x => x.Configuration).Returns(config);
        context.Setup(x => x.Player).Returns(player.Object);
        context.Setup(x => x.ActionService).Returns(actionService.Object);
        context.Setup(x => x.HasHoroscope).Returns(true);
        context.Setup(x => x.HasHoroscopeHelios).Returns(false);
        context.Setup(x => x.PartyHealthMetrics).Returns((0.50f, 0.30f, 3));
        context.Setup(x => x.HealingCoordination).Returns(new HealingCoordinationState());
        context.Setup(x => x.Debug).Returns(new AstraeaDebugState());
        context.Setup(x => x.TrainingService).Returns((Olympus.Services.Training.ITrainingService?)null);

        Assert.False(_handler.TryExecute(context.Object, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        // avgHp > 0.85f and no raidwide → skip
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHoroscope = true;
        config.Astrologian.AutoCastHoroscope = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Horoscope.ActionId)).Returns(true);

        // All healthy (96% HP) → above 0.85f threshold
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 8, injuredCount: 0);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 76,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenPartyLow_Executes()
    {
        // HasHoroscope = false (default), party low → prepare Horoscope
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHoroscope = true;
        config.Astrologian.AutoCastHoroscope = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Horoscope.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Horoscope.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // Party at 50% HP — below 0.85f threshold
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 4);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 76,
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Horoscope.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }
}
