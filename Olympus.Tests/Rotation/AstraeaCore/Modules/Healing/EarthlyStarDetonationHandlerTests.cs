using System.Collections.Generic;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Services.Astrologian;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules.Healing;

public class EarthlyStarDetonationHandlerTests
{
    private readonly EarthlyStarDetonationHandler _handler = new();

    [Fact]
    public void TryExecute_WhenStarNotPlaced_Skips()
    {
        // IsStarPlaced = false (default) → skip immediately
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEarthlyStar = true;

        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: false);

        var context = AstraeaTestContext.Create(
            config: config,
            earthlyStarService: earthlyStarService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenDisabled_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEarthlyStar = false;

        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: true, isStarMature: true);

        var context = AstraeaTestContext.Create(
            config: config,
            earthlyStarService: earthlyStarService,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenOnCooldown_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEarthlyStar = true;
        config.Astrologian.EarthlyStarDetonateThreshold = 0.80f;
        config.Astrologian.EarthlyStarMinTargets = 2;
        config.Astrologian.WaitForGiantDominance = false;

        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: true, isStarMature: true);
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 4);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.StellarDetonation.ActionId)).Returns(false);

        var context = AstraeaTestContext.Create(
            config: config,
            earthlyStarService: earthlyStarService,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 62,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenAboveThreshold_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEarthlyStar = true;
        config.Astrologian.EarthlyStarDetonateThreshold = 0.60f;
        config.Astrologian.EarthlyStarMinTargets = 5;
        config.Astrologian.WaitForGiantDominance = true;
        config.Astrologian.EarthlyStarEmergencyThreshold = 0.30f;

        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: true, isStarMature: true);
        // All healthy (96% HP) — avgHp > 0.60 and injured = 0, no emergency
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 8, injuredCount: 0);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.StellarDetonation.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            earthlyStarService: earthlyStarService,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 62,
            canExecuteOgcd: true);

        Assert.False(_handler.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_WhenStarPlaced_MatureAndPartyLow_Executes()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEarthlyStar = true;
        config.Astrologian.EarthlyStarDetonateThreshold = 0.80f;
        config.Astrologian.EarthlyStarMinTargets = 2;
        config.Astrologian.WaitForGiantDominance = false;

        // Star is placed and mature
        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: true, isStarMature: true);
        earthlyStarService.Setup(x => x.OnStarDetonated());

        // Party at 50% HP — below EarthlyStarDetonateThreshold (0.80)
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 4);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.StellarDetonation.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.StellarDetonation.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            earthlyStarService: earthlyStarService,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 62,
            canExecuteOgcd: true);

        var result = _handler.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.StellarDetonation.ActionId),
            It.IsAny<ulong>()), Times.Once);
        earthlyStarService.Verify(x => x.OnStarDetonated(), Times.Once);
    }
}
