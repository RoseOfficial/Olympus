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

public class MacrocosmosHandlerTests
{
    private readonly MacrocosmosHandler _handler = new();

    [Fact]
    public void TryExecute_WhenMoving_Skips()
    {
        var context = AstraeaTestContext.Create(canExecuteGcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: true));
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMacrocosmos = false;

        var context = AstraeaTestContext.Create(config: config, canExecuteGcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMacrocosmos = true;
        config.Astrologian.AutoUseMacrocosmos = true;
        config.Astrologian.MacrocosmosMinTargets = 1;
        config.Astrologian.MacrocosmosThreshold = 0.90f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(ASTActions.Macrocosmos.ActionId)).Returns(false);

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 4);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMacrocosmos = true;
        config.Astrologian.AutoUseMacrocosmos = true;
        config.Astrologian.MacrocosmosMinTargets = 1;
        config.Astrologian.MacrocosmosThreshold = 0.60f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(ASTActions.Macrocosmos.ActionId)).Returns(true);

        // All healthy (96% HP) → avgHp > 0.60 threshold
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 8, injuredCount: 0);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenPartyLow_ExecutesMacrocosmos()
    {
        // HasMacrocosmos defaults false (null StatusList)
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableMacrocosmos = true;
        config.Astrologian.AutoUseMacrocosmos = true;
        config.Astrologian.MacrocosmosMinTargets = 1;
        config.Astrologian.MacrocosmosThreshold = 0.90f;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(ASTActions.Macrocosmos.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Macrocosmos.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // Party at 50% HP — below 90% threshold; 4 members in range (at Vector3.Zero)
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 4);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Macrocosmos.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }
}
