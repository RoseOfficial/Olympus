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

public class SingleTargetHandlerTests
{
    private readonly SingleTargetHandler _handler = new();

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
        config.Astrologian.EnableBenefic = false;
        config.Astrologian.EnableBeneficII = false;

        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 15000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object });

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteGcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenNoTargetNeedsHealing_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableBenefic = true;
        config.Astrologian.EnableBeneficII = true;
        config.Astrologian.BeneficThreshold = 0.50f;
        config.Astrologian.BeneficIIThreshold = 0.60f;

        // All party members above threshold — empty party means no target
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
        config.Astrologian.EnableBenefic = true;
        config.Astrologian.EnableBeneficII = false;
        config.Astrologian.BeneficThreshold = 0.40f;

        // Member at 80% HP — above BeneficThreshold (0.40)
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
    public void TryExecute_WhenTargetBelowBeneficIIThreshold_ExecutesBeneficII()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableBeneficII = true;
        config.Astrologian.BeneficIIThreshold = 0.60f;

        // Member at 40% HP — below BeneficII threshold
        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 20000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.BeneficII.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 26,
            canExecuteGcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.BeneficII.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }
}
