using System.Collections.Generic;
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

public class ExaltationHandlerTests
{
    private readonly ExaltationHandler _handler = new();

    [Fact]
    public void TryExecute_WhenTargetBelowThreshold_ExecutesExaltation()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        // ExaltationThreshold = 0.75f (set in CreateDefaultAstrologianConfiguration)
        // FindExaltationTarget uses hardcoded 0.7f for the fallback search

        // Member at 50% HP — below both 0.7f (FindExaltation) and 0.75f (config threshold)
        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Exaltation.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Exaltation.ActionId),
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
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.Exaltation.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableExaltation = false;

        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object });

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Exaltation.ActionId)).Returns(false);

        var context = AstraeaTestContext.Create(
            actionService: actionService,
            partyHelper: partyHelper,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        // Set threshold to 0.60f — so a member at 65% HP is above the threshold
        config.Astrologian.ExaltationThreshold = 0.60f;

        // Member at 65% HP: below 0.7f (FindExaltationTarget finds them),
        // but 0.65 > 0.60 threshold → TryExaltation returns false
        var member = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 32500, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { member.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Exaltation.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenTargetReserved_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();

        // Member at 50% HP — FindExaltationTarget will find them
        const uint memberId = 42u;
        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: memberId, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.Exaltation.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        // Pre-reserve the target so IsTargetReserved returns true
        context.HealingCoordination.TryReserveTarget(memberId);

        Assert.False(_handler.TryExecute(context, isMoving: false));
        actionService.Verify(x => x.ExecuteOgcd(
            It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }
}
