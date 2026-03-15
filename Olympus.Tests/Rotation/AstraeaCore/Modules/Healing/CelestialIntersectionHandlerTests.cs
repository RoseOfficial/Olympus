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

public class CelestialIntersectionHandlerTests
{
    private readonly CelestialIntersectionHandler _handler = new();

    [Fact]
    public void TryExecute_WhenTargetBelowThreshold_ExecutesCelestialIntersection()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.CelestialIntersectionThreshold = 0.70f;

        // Member at 50% HP — below the 70% threshold
        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.CelestialIntersection.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.CelestialIntersection.ActionId),
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
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.CelestialIntersection.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableCelestialIntersection = false;

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
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.CelestialIntersection.ActionId)).Returns(false);

        var injuredMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 25000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injuredMember.Object });

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
        config.Astrologian.CelestialIntersectionThreshold = 0.70f;

        // Member at 90% HP — above the 70% threshold
        var healthyMember = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 45000, maxHp: 50000);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { healthyMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.CelestialIntersection.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenNoPartyMembers_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        var partyHelper = new TestableAstraeaPartyHelper(new List<IBattleChara>());

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.CelestialIntersection.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }
}
