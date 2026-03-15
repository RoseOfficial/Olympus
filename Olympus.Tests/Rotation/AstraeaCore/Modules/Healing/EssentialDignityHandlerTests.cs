using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules.Healing;

public class EssentialDignityHandlerTests
{
    private readonly EssentialDignityHandler _handler = new();

    [Fact]
    public void TryExecute_WhenTankLow_ExecutesEssentialDignity()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EssentialDignityThreshold = 0.75f;

        // Tank at 30% HP — below the 75% threshold; FindEssentialDignityTarget will find them
        var tank = MockBuilders.CreateMockBattleChara(currentHp: 30000, maxHp: 100000);
        var partyHelper = new TestableAstraeaPartyHelper(new IBattleChara[] { tank.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.EssentialDignity.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.EssentialDignity.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.EssentialDignity.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EssentialDignityThreshold = 0.75f;

        // Tank at 90% HP — above the 75% threshold; FindEssentialDignityTarget returns null
        var tank = MockBuilders.CreateMockBattleChara(currentHp: 90000, maxHp: 100000);
        var partyHelper = new TestableAstraeaPartyHelper(new IBattleChara[] { tank.Object });

        // Explicitly mark action as ready so the test exercises the HP threshold guard, not the cooldown guard
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.EssentialDignity.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.EssentialDignity.ActionId)).Returns(false);

        var context = AstraeaTestContext.Create(actionService: actionService, canExecuteOgcd: true);
        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEssentialDignity = false;

        var tank = MockBuilders.CreateMockBattleChara(currentHp: 15000, maxHp: 100000);
        var partyHelper = new TestableAstraeaPartyHelper(new IBattleChara[] { tank.Object });

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
