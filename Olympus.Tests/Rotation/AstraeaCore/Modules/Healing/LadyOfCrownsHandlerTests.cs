using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules.Healing;

public class LadyOfCrownsHandlerTests
{
    private readonly LadyOfCrownsHandler _handler = new();

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMinorArcana = false;

        var context = AstraeaTestContext.Create(config: config, canExecuteOgcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        // LadyOfCrowns doesn't check IsActionReady — it's triggered by HasLady
        // and the MinorArcanaStrategy. Test the "no Lady card" guard instead.
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMinorArcana = true;
        config.Astrologian.MinorArcanaStrategy = MinorArcanaUsageStrategy.EmergencyOnly;

        var cardService = AstraeaTestContext.CreateMockCardService(hasLady: false);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenNoLadyCard_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMinorArcana = true;
        config.Astrologian.MinorArcanaStrategy = MinorArcanaUsageStrategy.EmergencyOnly;

        var cardService = AstraeaTestContext.CreateMockCardService(hasLady: false);

        // Party at 40% HP — below threshold, but no Lady card
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 4);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            partyHelper: partyHelper,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMinorArcana = true;
        config.Astrologian.MinorArcanaStrategy = MinorArcanaUsageStrategy.EmergencyOnly;
        config.Astrologian.LadyOfCrownsThreshold = 0.50f;

        var cardService = AstraeaTestContext.CreateMockCardService(hasLady: true);

        // All healthy — avgHp > 0.50 threshold
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 8, injuredCount: 0);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            partyHelper: partyHelper,
            level: 70,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenHasLady_LowParty_ExecutesLadyOfCrowns()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMinorArcana = true;
        config.Astrologian.MinorArcanaStrategy = MinorArcanaUsageStrategy.EmergencyOnly;
        config.Astrologian.LadyOfCrownsThreshold = 0.70f;

        var cardService = AstraeaTestContext.CreateMockCardService(hasLady: true);

        // Party at 50% HP — below 70% threshold; 4 injured (injured >= 2)
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 4);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.LadyOfCrowns.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            cardService: cardService,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 70,
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.LadyOfCrowns.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }
}
