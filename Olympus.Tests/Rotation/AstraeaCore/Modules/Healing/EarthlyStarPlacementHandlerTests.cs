using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
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

public class EarthlyStarPlacementHandlerTests
{
    private readonly EarthlyStarPlacementHandler _handler = new();

    [Fact]
    public void TryExecute_WhenStarAlreadyPlaced_Skips()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEarthlyStar = true;
        config.Astrologian.StarPlacement = EarthlyStarPlacementStrategy.OnSelf;

        // IsStarPlaced = true → skip
        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: true);

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

        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: false);

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
        config.Astrologian.StarPlacement = EarthlyStarPlacementStrategy.OnSelf;
        config.Astrologian.EarthlyStarDetonateThreshold = 0.80f;

        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: false);
        // Party low so reactive placement check passes
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 4);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.EarthlyStar.ActionId)).Returns(false);

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
    public void TryExecute_WhenStarNotPlaced_PartyLow_ExecutesPlacement()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEarthlyStar = true;
        config.Astrologian.StarPlacement = EarthlyStarPlacementStrategy.OnSelf;
        config.Astrologian.EarthlyStarDetonateThreshold = 0.80f;

        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService(isStarPlaced: false);
        earthlyStarService.Setup(x => x.OnStarPlaced(It.IsAny<System.Numerics.Vector3>()));

        // Party at 50% HP — below EarthlyStarDetonateThreshold (0.80)
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(healthyCount: 0, injuredCount: 4);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(ASTActions.EarthlyStar.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGroundTargetedOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == ASTActions.EarthlyStar.ActionId),
                It.IsAny<System.Numerics.Vector3>()))
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
        actionService.Verify(x => x.ExecuteGroundTargetedOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == ASTActions.EarthlyStar.ActionId),
            It.IsAny<System.Numerics.Vector3>()), Times.Once);
        earthlyStarService.Verify(x => x.OnStarPlaced(It.IsAny<System.Numerics.Vector3>()), Times.Once);
    }
}
