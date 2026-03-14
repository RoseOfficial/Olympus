using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Tests for Sage ResurrectionModule (Egeiro) logic.
/// Covers raise configuration, MP thresholds, Swiftcast, and hardcast rules.
/// </summary>
public class ResurrectionModuleTests
{
    private readonly ResurrectionModule _module;

    public ResurrectionModuleTests()
    {
        _module = new ResurrectionModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is5()
    {
        Assert.Equal(5, _module.Priority);
    }

    [Fact]
    public void Name_IsResurrection()
    {
        Assert.Equal("Resurrection", _module.Name);
    }

    #endregion

    #region Raise Configuration Tests

    [Fact]
    public void TryExecute_RaiseDisabled_ReturnsFalse()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteGcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SGEActions.Egeiro.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_RaiseLevelTooLow_ReturnsFalse()
    {
        // Arrange: Egeiro requires level 12
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = true;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 10, // Below Egeiro level 12
            currentMp: 10000,
            canExecuteGcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SGEActions.Egeiro.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region MP Threshold Tests

    [Fact]
    public void TryExecute_InsufficientMpForEgeiro_ReturnsFalse()
    {
        // Arrange: MP below the 2400 cost for Egeiro
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.10f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 2000, // Less than 2400 cost
            canExecuteGcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryExecute_MpBelowThreshold_ReturnsFalse()
    {
        // Arrange: MP above cost but below threshold percentage
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.50f; // 50% threshold

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 4000, // 40% of 10000, below 50% threshold
            canExecuteGcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region No Target Tests

    [Fact]
    public void TryExecute_NoDeadPartyMember_ReturnsFalse()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = true;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleChara?)null);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Hardcast Tests

    [Fact]
    public void TryExecute_HardcastDisabled_NoSwiftcast_ReturnsFalse()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = false;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // No Swiftcast ready
        actionService.Setup(a => a.IsActionReady(SGEActions.Swiftcast.ActionId))
            .Returns(false);
        actionService.Setup(a => a.GetCooldownRemaining(SGEActions.Swiftcast.ActionId))
            .Returns(15f);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SGEActions.Egeiro.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_HardcastEnabled_Moving_ReturnsFalse()
    {
        // Arrange: hardcast enabled but player is moving
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = true;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(a => a.IsActionReady(SGEActions.Swiftcast.ActionId))
            .Returns(false);
        actionService.Setup(a => a.GetCooldownRemaining(SGEActions.Swiftcast.ActionId))
            .Returns(15f);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: true);

        // Act - moving prevents hardcast raise
        var result = _module.TryExecute(context, isMoving: true);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Swiftcast Tests

    [Fact]
    public void TryExecute_SwiftcastAvailable_OgcdWindow_ExecutesSwiftcast()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = true;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);
        actionService.Setup(a => a.IsActionReady(SGEActions.Swiftcast.ActionId))
            .Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(SGEActions.Swiftcast, It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(SGEActions.Swiftcast, It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SwiftcastLevelTooLow_DoesNotExecuteSwiftcast()
    {
        // Arrange: Swiftcast requires level 18
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = true;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 15, // Below Swiftcast level 18
            currentMp: 10000,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(SGEActions.Swiftcast, It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region SGE-specific: No pre-raise buff (unlike WHM's Thin Air)

    [Fact]
    public void TryExecute_SGE_DoesNotWaitForPreRaiseBuff()
    {
        // SGE has no Thin Air equivalent — no pre-raise buff delay.
        // With a dead member, MP sufficient, hardcast enabled and GCD available,
        // the module should attempt the raise immediately.
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.0f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10u, currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(a => a.IsActionReady(SGEActions.Swiftcast.ActionId))
            .Returns(false);
        actionService.Setup(a => a.GetCooldownRemaining(SGEActions.Swiftcast.ActionId))
            .Returns(60f); // Swiftcast on long cooldown
        actionService.Setup(a => a.ExecuteGcd(
                It.Is<ActionDefinition>(ad => ad.ActionId == SGEActions.Egeiro.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert - should execute raise without waiting for any pre-buff
        Assert.True(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == SGEActions.Egeiro.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion
}
