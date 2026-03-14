using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules;

/// <summary>
/// Tests for Astrologian DefensiveModule logic.
/// Covers Neutral Sect strategies, SunSign gating on Neutral Sect,
/// and Collective Unconscious disabled-by-default behavior.
/// </summary>
public class DefensiveModuleTests
{
    private readonly DefensiveModule _module;

    public DefensiveModuleTests()
    {
        _module = new DefensiveModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is20()
    {
        Assert.Equal(20, _module.Priority);
    }

    [Fact]
    public void Name_IsDefensive()
    {
        Assert.Equal("Defensive", _module.Name);
    }

    #endregion

    #region Neutral Sect — Disabled

    [Fact]
    public void TryExecute_NeutralSectDisabled_ReturnsFalse()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableNeutralSect = false;
        config.Astrologian.EnableCollectiveUnconscious = false;
        config.Astrologian.EnableSunSign = false;

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 2, injuredCount: 4, config: config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(It.IsAny<uint>())).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.NeutralSect.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Neutral Sect — OnCooldown Strategy

    [Fact]
    public void TryExecute_NeutralSect_OnCooldownStrategy_InCombat_Fires()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableNeutralSect = true;
        config.Astrologian.NeutralSectStrategy = NeutralSectUsageStrategy.OnCooldown;
        config.Astrologian.EnableSunSign = false;
        config.Astrologian.EnableCollectiveUnconscious = false;

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 4, injuredCount: 0, config: config);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteOgcd: true,
            canExecuteGcd: false);
        actionService.Setup(a => a.IsActionReady(ASTActions.NeutralSect.ActionId)).Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.NeutralSect.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.NeutralSect.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Neutral Sect — SaveForDamage Strategy

    [Fact]
    public void TryExecute_NeutralSect_SaveForDamage_PartyHealthy_DoesNotFire()
    {
        // Party is healthy — SaveForDamage strategy should hold Neutral Sect
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableNeutralSect = true;
        config.Astrologian.NeutralSectStrategy = NeutralSectUsageStrategy.SaveForDamage;
        config.Astrologian.NeutralSectThreshold = 0.65f;
        config.Astrologian.EnableSunSign = false;
        config.Astrologian.EnableCollectiveUnconscious = false;

        // All 4 members at 96% HP — well above the 65% threshold
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 4, injuredCount: 0, config: config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.NeutralSect.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.NeutralSect.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_NeutralSect_SaveForDamage_PartyInjured_Fires()
    {
        // 4 of 6 members injured (50% HP) — average drops below 65% threshold
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableNeutralSect = true;
        config.Astrologian.NeutralSectStrategy = NeutralSectUsageStrategy.SaveForDamage;
        config.Astrologian.NeutralSectThreshold = 0.65f;
        config.Astrologian.EnableSunSign = false;
        config.Astrologian.EnableCollectiveUnconscious = false;

        // 2 healthy (96%), 4 injured (50%) → avg ≈ 64.7% < 65% threshold
        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 2, injuredCount: 4, config: config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.NeutralSect.ActionId)).Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.NeutralSect.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.NeutralSect.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Neutral Sect — Manual Strategy

    [Fact]
    public void TryExecute_NeutralSect_ManualStrategy_NeverFires()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableNeutralSect = true;
        config.Astrologian.NeutralSectStrategy = NeutralSectUsageStrategy.Manual;
        config.Astrologian.EnableSunSign = false;
        config.Astrologian.EnableCollectiveUnconscious = false;

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 0, injuredCount: 6, config: config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.NeutralSect.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.NeutralSect.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Neutral Sect — Level Guard

    [Fact]
    public void TryExecute_NeutralSect_LevelTooLow_DoesNotFire()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableNeutralSect = true;
        config.Astrologian.NeutralSectStrategy = NeutralSectUsageStrategy.OnCooldown;
        config.Astrologian.EnableSunSign = false;
        config.Astrologian.EnableCollectiveUnconscious = false;

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 4, injuredCount: 0, config: config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.NeutralSect.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: (byte)(ASTActions.NeutralSect.MinLevel - 1), // Below minimum
            inCombat: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region SunSign — Requires Neutral Sect Active

    [Fact]
    public void TryExecute_SunSign_WithoutNeutralSect_DoesNotFire()
    {
        // Sun Sign requires Neutral Sect to be active — context has null StatusList
        // so HasNeutralSect returns false
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableNeutralSect = false; // Neutral Sect won't fire
        config.Astrologian.EnableSunSign = true;
        config.Astrologian.EnableCollectiveUnconscious = false;

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 2, injuredCount: 4, config: config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.SunSign.ActionId)).Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.SunSign.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Collective Unconscious — Disabled by Default

    [Fact]
    public void TryExecute_CollectiveUnconscious_DisabledByDefault_DoesNotFire()
    {
        // Default config has EnableCollectiveUnconscious = false
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        Assert.False(config.Astrologian.EnableCollectiveUnconscious);

        config.Astrologian.EnableNeutralSect = false;
        config.Astrologian.EnableSunSign = false;

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 1, injuredCount: 5, config: config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.CollectiveUnconscious.ActionId))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.CollectiveUnconscious.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_CollectiveUnconscious_WhileMoving_DoesNotFire()
    {
        // Collective Unconscious is blocked when player is moving (channeled ability)
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableNeutralSect = false;
        config.Astrologian.EnableSunSign = false;
        config.Astrologian.EnableCollectiveUnconscious = true;
        config.Astrologian.CollectiveUnconsciousThreshold = 0.90f; // Fire at 90% avg HP

        var partyHelper = AstraeaTestContext.CreatePartyWithInjured(
            healthyCount: 0, injuredCount: 6, config: config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(ASTActions.CollectiveUnconscious.ActionId))
            .Returns(true);

        var context = AstraeaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        // isMoving = true blocks Collective Unconscious
        var result = _module.TryExecute(context, isMoving: true);

        Assert.False(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == ASTActions.CollectiveUnconscious.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion
}
