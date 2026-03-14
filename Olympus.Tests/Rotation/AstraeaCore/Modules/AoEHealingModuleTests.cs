using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules;

/// <summary>
/// Tests for AoEHealingModule: Celestial Opposition and Helios/AspectedHelios/HeliosConjunction.
/// Sub-modules do not check CanExecuteOgcd/CanExecuteGcd — those checks stay in the coordinator.
/// The coordinator guards TryGcd with !isMoving before calling this module.
/// </summary>
public class AoEHealingModuleTests
{
    private readonly AoEHealingModule _module;

    public AoEHealingModuleTests()
    {
        _module = new AoEHealingModule();
    }

    #region TryGcd — Helios AoE heal

    [Fact]
    public void TryGcd_AoEHealDisabled_ReturnsFalse()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHelios = false;
        config.Astrologian.EnableAspectedHelios = false;

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 5, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryGcd(context);

        Assert.False(result);
    }

    [Fact]
    public void TryGcd_InsufficientInjured_BelowThreshold_ReturnsFalse()
    {
        // Only 2 injured below AoEHealMinTargets = 3 — should not fire
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHelios = true;
        config.Astrologian.AoEHealMinTargets = 3;
        config.Astrologian.AoEHealThreshold = 0.80f;

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 6, injuredCount: 2, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>())).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryGcd(context);

        Assert.False(result);
    }

    [Fact]
    public void TryGcd_SufficientInjured_AtThreshold_ReturnsTrue()
    {
        // 3 injured members at AoEHealMinTargets = 3 — should fire
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHelios = true;
        config.Astrologian.AoEHealMinTargets = 3;
        config.Astrologian.AoEHealThreshold = 0.80f;

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 5, injuredCount: 3, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(a => a.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryGcd(context);

        Assert.True(result);
    }

    #endregion

    #region TryOgcd — Celestial Opposition

    [Fact]
    public void TryOgcd_CelestialOppositionDisabled_ReturnsFalse()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableCelestialOpposition = false;

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 5, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryOgcd(context);

        Assert.False(result);
    }

    #endregion
}
