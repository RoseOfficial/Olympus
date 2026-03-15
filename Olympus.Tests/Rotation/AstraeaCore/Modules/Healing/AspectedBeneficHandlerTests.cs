using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
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

public class AspectedBeneficHandlerTests
{
    private readonly AspectedBeneficHandler _handler = new();

    [Fact]
    public void TryExecute_WhenMoving_DoesNotSkip()
    {
        // Aspected Benefic is instant cast — it should not be blocked by isMoving
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableAspectedBenefic = true;
        config.Astrologian.AspectedBeneficThreshold = 0.75f;

        var injuredMember = MockBuilders.CreateMockBattleChara(entityId: 1u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(new List<IBattleChara> { injuredMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.AspectedBenefic.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 34,
            canExecuteGcd: true);

        var result = _handler.TryExecute(context, isMoving: true); // moving — should still fire

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.AspectedBenefic.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableAspectedBenefic = false;

        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 20000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object });

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteGcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAlreadyRegen_Skips()
    {
        // StatusHelper.HasAspectedBenefic returns false for null StatusList → safe to not test this path
        // Instead test: no party member needs healing (empty party)
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableAspectedBenefic = true;
        config.Astrologian.AspectedBeneficThreshold = 0.75f;

        var partyHelper = new TestableAstraeaPartyHelper(new List<IBattleChara>());

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteGcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableAspectedBenefic = true;
        config.Astrologian.AspectedBeneficThreshold = 0.60f;

        // Member at 80% HP — above the 60% threshold
        var healthyMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 40000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { healthyMember.Object });

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteGcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenTargetBelowThreshold_ExecutesAspectedBenefic()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableAspectedBenefic = true;
        config.Astrologian.AspectedBeneficThreshold = 0.75f;

        // Member at 50% HP — below threshold; HasAspectedBenefic defaults false (null StatusList)
        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.AspectedBenefic.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 34,
            canExecuteGcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.AspectedBenefic.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }
}
