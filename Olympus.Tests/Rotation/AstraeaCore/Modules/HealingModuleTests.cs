using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
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
/// Tests for the Astrologian HealingModule coordinator.
/// Covers module properties, general guards, and AoE regression tests.
/// Handler-specific logic is tested in the individual handler test files under Healing/.
/// </summary>
public class HealingModuleTests
{
    private readonly HealingModule _module;

    public HealingModuleTests()
    {
        _module = new HealingModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is10()
    {
        Assert.Equal(10, _module.Priority);
    }

    [Fact]
    public void Name_IsHealing()
    {
        Assert.Equal("Healing", _module.Name);
    }

    #endregion

    #region General Healing State Tests

    [Fact]
    public void TryExecute_HealingDisabled_ReturnsFalse()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.EnableHealing = false;

        var context = AstraeaTestContext.Create(
            config: config,
            canExecuteGcd: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void TryExecute_NoWindowAvailable_ReturnsFalse()
    {
        // Neither GCD nor oGCD window — nothing can fire
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();

        var context = AstraeaTestContext.Create(
            config: config,
            canExecuteGcd: false,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region AoE Regression — Real Party Counting (v4.10.7 class fix)

    /// <summary>
    /// Regression test: confirms AoE heal fires correctly when enough members are injured.
    /// Uses a real TestableAstraeaPartyHelper so actual counting logic is exercised.
    /// This test class prevents v4.10.7-class bugs where mocked counts bypassed real logic.
    /// </summary>
    [Fact]
    public void AoEHeal_RealPartyHelper_8Members3Injured_ThresholdMet_TriesAoEHeal()
    {
        // 8-person raid: 5 healthy (96% HP), 3 injured (50% HP)
        // AoEHealMinTargets = 3 → threshold is met, module should attempt AoE heal
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHelios = true;
        config.Astrologian.AoEHealMinTargets = 3;
        config.Astrologian.AoEHealThreshold = 0.80f; // 80% threshold

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 5, injuredCount: 3, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>()))
            .Returns(true);
        actionService.Setup(a => a.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        // With real counting, 3 injured members meets the AoEHealMinTargets = 3 threshold
        Assert.True(result);
    }

    [Fact]
    public void AoEHeal_RealPartyHelper_4ManDungeon_3Injured_ThresholdMet_TriesAoEHeal()
    {
        // 4-man dungeon: 1 healthy (96% HP), 3 injured (50% HP)
        // AoEHealMinTargets = 3 → threshold met, module should attempt AoE heal
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHelios = true;
        config.Astrologian.AoEHealMinTargets = 3;
        config.Astrologian.AoEHealThreshold = 0.80f;

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 3, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>()))
            .Returns(true);
        actionService.Setup(a => a.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        // Real party counting: 3 injured meets the AoEHealMinTargets = 3 threshold
        Assert.True(result);
    }

    [Fact]
    public void AoEHeal_RealPartyHelper_8Members2Injured_BelowThreshold_DoesNotAoEHeal()
    {
        // Only 2 injured members — below AoEHealMinTargets = 3, should not fire AoE heal
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableHelios = true;
        config.Astrologian.AoEHealMinTargets = 3;
        config.Astrologian.AoEHealThreshold = 0.80f;

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 6, injuredCount: 2, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = _module.TryExecute(context, isMoving: false);

        // With real counting, 2 injured is below the minimum of 3
        Assert.False(result);
    }

    #endregion

    #region Routing Tests

    [Fact]
    public void TryExecute_WhenCanExecuteOgcdOnly_DoesNotRunGcdHandlers()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        var context = AstraeaTestContext.Create(actionService: actionService,
            canExecuteGcd: false, canExecuteOgcd: true);
        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);
        actionService.Verify(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_WhenCanExecuteGcdOnly_DoesNotRunOgcdHandlers()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        var context = AstraeaTestContext.Create(actionService: actionService,
            canExecuteGcd: true, canExecuteOgcd: false);
        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);
        actionService.Verify(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    #endregion
}
