using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ApolloCore.Modules;

/// <summary>
/// Tests for ResurrectionModule raise logic.
/// Covers Raise configuration, MP thresholds, Swiftcast usage, and hardcast rules.
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
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Resurrection.EnableRaise = false;

        var actionService = new Mock<IActionService>();
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteGcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Raise.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_RaiseLevelTooLow_ReturnsFalse()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Resurrection.EnableRaise = true;

        var partyHelper = new Mock<IPartyHelper>();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10, name: "DeadMember", currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = new Mock<IActionService>();
        // Raise requires level 12
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 10,
            canExecuteGcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Raise.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region MP Threshold Tests

    [Fact]
    public void TryExecute_InsufficientMp_ReturnsFalse()
    {
        // Arrange: MP below the 2400 cost for Raise
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.25f;

        var partyHelper = new Mock<IPartyHelper>();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10, name: "DeadMember", currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = new Mock<IActionService>();
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            currentMp: 2000,  // Less than 2400 cost
            canExecuteGcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Raise.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_MpBelowThreshold_ReturnsFalse()
    {
        // Arrange: MP above cost but below threshold percentage
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.RaiseMpThreshold = 0.50f;  // 50% threshold

        var partyHelper = new Mock<IPartyHelper>();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10, name: "DeadMember", currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = new Mock<IActionService>();
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            currentMp: 4000,  // 40% of 10000, below 50% threshold
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
        // Arrange: No dead party members
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Resurrection.EnableRaise = true;

        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleChara?)null);

        var actionService = new Mock<IActionService>();
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
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
        // Arrange: Hardcast disabled, no Swiftcast available
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = false;

        var partyHelper = new Mock<IPartyHelper>();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10, name: "DeadMember", currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = new Mock<IActionService>();
        // No Swiftcast ready
        actionService.Setup(a => a.IsActionReady(WHMActions.Swiftcast.ActionId))
            .Returns(false);
        actionService.Setup(a => a.GetCooldownRemaining(WHMActions.Swiftcast.ActionId))
            .Returns(5f);  // Short cooldown, would normally wait

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
        actionService.Verify(a => a.ExecuteGcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Raise.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_HardcastEnabled_Moving_ReturnsFalse()
    {
        // Arrange: Hardcast enabled but moving
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Resurrection.EnableRaise = true;
        config.Resurrection.AllowHardcastRaise = true;

        var partyHelper = new Mock<IPartyHelper>();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10, name: "DeadMember", currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = new Mock<IActionService>();
        // No Swiftcast
        actionService.Setup(a => a.IsActionReady(WHMActions.Swiftcast.ActionId))
            .Returns(false);
        actionService.Setup(a => a.GetCooldownRemaining(WHMActions.Swiftcast.ActionId))
            .Returns(15f);  // Long cooldown

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: true);

        // Act - moving prevents hardcast
        var result = _module.TryExecute(context, isMoving: true);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Swiftcast Tests

    [Fact]
    public void TryExecute_SwiftcastOgcdWithDeadMember_ExecutesSwiftcast()
    {
        // Arrange: oGCD window with dead member
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Resurrection.EnableRaise = true;

        var partyHelper = new Mock<IPartyHelper>();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10, name: "DeadMember", currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = new Mock<IActionService>();
        actionService.Setup(a => a.IsActionReady(WHMActions.Swiftcast.ActionId))
            .Returns(true);
        actionService.Setup(a => a.ExecuteOgcd(WHMActions.Swiftcast, It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            currentMp: 10000,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(WHMActions.Swiftcast, It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_SwiftcastLevelTooLow_DoesNotExecuteSwiftcast()
    {
        // Arrange: Level too low for Swiftcast (requires level 18)
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Resurrection.EnableRaise = true;

        var partyHelper = new Mock<IPartyHelper>();
        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 10, name: "DeadMember", currentHp: 0, isDead: true);
        partyHelper.Setup(p => p.FindDeadPartyMemberNeedingRaise(It.IsAny<IPlayerCharacter>()))
            .Returns(deadMember.Object);

        var actionService = new Mock<IActionService>();

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 15,  // Below Swiftcast level 18
            currentMp: 10000,
            canExecuteGcd: false,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(WHMActions.Swiftcast, It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private ApolloContext CreateTestContext(
        Configuration? config = null,
        Mock<IPartyHelper>? partyHelper = null,
        Mock<IActionService>? actionService = null,
        byte level = 90,
        uint currentHp = 50000,
        uint maxHp = 50000,
        uint currentMp = 10000,
        bool inCombat = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false)
    {
        config ??= MockBuilders.CreateDefaultConfiguration();

        var player = MockBuilders.CreateMockPlayerCharacter(level, currentHp, maxHp, currentMp);

        actionService ??= new Mock<IActionService>();
        var actionTracker = MockBuilders.CreateMockActionTracker(config);

        var combatEventService = new Mock<ICombatEventService>();
        var debuffDetectionService = new Mock<IDebuffDetectionService>();
        var healingSpellSelector = new Mock<IHealingSpellSelector>();
        healingSpellSelector.Setup(h => h.SelectBestSingleHeal(
            It.IsAny<IPlayerCharacter>(),
            It.IsAny<IBattleChara>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<float>()))
            .Returns(((ActionDefinition?)null, 0));
        healingSpellSelector.Setup(h => h.SelectBestAoEHeal(
            It.IsAny<IPlayerCharacter>(),
            It.IsAny<int>(),
            It.IsAny<int>(),
            It.IsAny<bool>(),
            It.IsAny<bool>(),
            It.IsAny<int>(),
            It.IsAny<IBattleChara>()))
            .Returns(((ActionDefinition?)null, 0, (IBattleChara?)null));

        var hpPredictionService = new Mock<IHpPredictionService>();
        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        var damageTrendService = MockBuilders.CreateMockDamageTrendService();
        var frameCache = MockBuilders.CreateMockFrameScopedCache();
        var mpForecastService = MockBuilders.CreateMockMpForecastService();
        var objectTable = new Mock<IObjectTable>();
        var partyList = new Mock<IPartyList>();
        var playerStatsService = new Mock<IPlayerStatsService>();
        playerStatsService.Setup(p => p.GetHealingStats(It.IsAny<byte>()))
            .Returns((1000, 1000, 100));

        var targetingService = new Mock<ITargetingService>();
        var statusHelper = new StatusHelper();

        partyHelper ??= new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((1.0f, 1.0f, 0));

        return new ApolloContext(
            player.Object,
            inCombat,
            isMoving: false,
            canExecuteGcd,
            canExecuteOgcd,
            actionService.Object,
            actionTracker,
            combatEventService.Object,
            damageIntakeService.Object,
            damageTrendService.Object,
            frameCache.Object,
            config,
            debuffDetectionService.Object,
            healingSpellSelector.Object,
            hpPredictionService.Object,
            mpForecastService.Object,
            objectTable.Object,
            partyList.Object,
            playerStatsService.Object,
            targetingService.Object,
            statusHelper,
            partyHelper.Object,
            MockBuilders.CreateMockCooldownPlanner().Object);
    }

    #endregion
}
