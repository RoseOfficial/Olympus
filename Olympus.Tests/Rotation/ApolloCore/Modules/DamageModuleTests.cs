using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Debuff;
using Olympus.Services.Healing;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ApolloCore.Modules;

/// <summary>
/// Tests for DamageModule DPS logic.
/// Covers configuration toggles, level requirements, DoT management, and AoE targeting.
/// </summary>
public class DamageModuleTests
{
    private readonly DamageModule _module;

    public DamageModuleTests()
    {
        _module = new DamageModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is50()
    {
        Assert.Equal(50, _module.Priority);
    }

    [Fact]
    public void Name_IsDamage()
    {
        Assert.Equal("Damage", _module.Name);
    }

    #endregion

    #region Combat State Tests

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableDamage = true;

        var context = CreateTestContext(
            config: config,
            inCombat: false);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
        Assert.Equal("Not in combat", context.Debug.DpsState);
    }

    [Fact]
    public void TryExecute_InCombat_AlwaysReturnsFalse()
    {
        // DamageModule always returns false to not block other actions
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableDamage = true;

        var enemyMock = CreateMockEnemy();

        var targetingServiceMock = MockBuilders.CreateMockTargetingService();
        targetingServiceMock.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemyMock.Object);

        var context = CreateTestContext(
            config: config,
            targetingService: targetingServiceMock,
            inCombat: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert - DPS module always returns false
        Assert.False(result);
    }

    #endregion

    #region Configuration Toggle Tests

    [Fact]
    public void TryExecute_DamageDisabled_DoesNotExecuteDamageSpell()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableDamage = false;
        config.Damage.EnableStone = true;

        var enemyMock = CreateMockEnemy();

        var targetingServiceMock = MockBuilders.CreateMockTargetingService();
        targetingServiceMock.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemyMock.Object);

        var actionServiceMock = MockBuilders.CreateMockActionService();

        var context = CreateTestContext(
            config: config,
            targetingService: targetingServiceMock,
            actionService: actionServiceMock,
            inCombat: true,
            level: 90);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert - No damage GCD should be executed
        actionServiceMock.Verify(
            x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_DoTDisabled_DoesNotExecuteDoT()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableDoT = false;
        config.Dot.EnableDia = true;

        var enemyMock = CreateMockEnemy();

        var targetingServiceMock = MockBuilders.CreateMockTargetingService();
        targetingServiceMock.Setup(x => x.FindEnemyNeedingDot(
                It.IsAny<uint>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemyMock.Object);
        targetingServiceMock.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var actionServiceMock = MockBuilders.CreateMockActionService();

        var context = CreateTestContext(
            config: config,
            targetingService: targetingServiceMock,
            actionService: actionServiceMock,
            inCombat: true,
            level: 90);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert - No DoT should be executed (and no damage either since no target)
        actionServiceMock.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Dia.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Level Requirement Tests

    [Fact]
    public void TryExecute_LowLevel_UsesAppropriateSpell()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableDamage = true;
        config.Damage.EnableStone = true;

        var enemyMock = CreateMockEnemy();

        var targetingServiceMock = MockBuilders.CreateMockTargetingService();
        targetingServiceMock.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemyMock.Object);

        var actionServiceMock = MockBuilders.CreateMockActionService();
        actionServiceMock.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateTestContext(
            config: config,
            targetingService: targetingServiceMock,
            actionService: actionServiceMock,
            inCombat: true,
            level: 10); // Low level - should use Stone

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert - Stone should be executed (level 1 spell)
        actionServiceMock.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Stone.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_HighLevel_UsesHighestLevelSpell()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableDamage = true;
        config.Damage.EnableGlareIII = true;

        var enemyMock = CreateMockEnemy();

        var targetingServiceMock = MockBuilders.CreateMockTargetingService();
        targetingServiceMock.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemyMock.Object);

        var actionServiceMock = MockBuilders.CreateMockActionService();
        actionServiceMock.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateTestContext(
            config: config,
            targetingService: targetingServiceMock,
            actionService: actionServiceMock,
            inCombat: true,
            level: 90); // High level - should use Glare III

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert - Glare III should be executed (level 82 spell)
        actionServiceMock.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.GlareIII.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion

    #region AoE Damage Tests

    [Fact]
    public void TryExecute_AoEThresholdMet_UsesHoly()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableDamage = true;
        config.Damage.EnableHoly = true;
        config.Damage.AoEDamageMinTargets = 3;

        var targetingServiceMock = MockBuilders.CreateMockTargetingService();
        targetingServiceMock.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(4); // 4 enemies, above 3 threshold

        // No DoT target, no single target
        targetingServiceMock.Setup(x => x.FindEnemyNeedingDot(
                It.IsAny<uint>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        targetingServiceMock.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var actionServiceMock = MockBuilders.CreateMockActionService();
        actionServiceMock.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateTestContext(
            config: config,
            targetingService: targetingServiceMock,
            actionService: actionServiceMock,
            inCombat: true,
            level: 50); // Level 50 - Holy available

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert - Holy should be executed (self-targeted AoE)
        actionServiceMock.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Holy.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_AoEThresholdNotMet_UsesStandardDamage()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableDamage = true;
        config.Damage.EnableHoly = true;
        config.Damage.EnableStone = true;
        config.Damage.AoEDamageMinTargets = 3;

        var enemyMock = CreateMockEnemy();

        var targetingServiceMock = MockBuilders.CreateMockTargetingService();
        targetingServiceMock.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(2); // Only 2 enemies, below 3 threshold

        targetingServiceMock.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemyMock.Object);

        var actionServiceMock = MockBuilders.CreateMockActionService();
        actionServiceMock.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateTestContext(
            config: config,
            targetingService: targetingServiceMock,
            actionService: actionServiceMock,
            inCombat: true,
            level: 50);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert - Holy should NOT be used, fallback to single target
        actionServiceMock.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Holy.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region No Target Tests

    [Fact]
    public void TryExecute_NoEnemy_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableDamage = true;

        var targetingServiceMock = MockBuilders.CreateMockTargetingService();
        // All targeting methods return null
        targetingServiceMock.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var actionServiceMock = MockBuilders.CreateMockActionService();

        var context = CreateTestContext(
            config: config,
            targetingService: targetingServiceMock,
            actionService: actionServiceMock,
            inCombat: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.Equal("No enemy found", context.Debug.DpsState);
        actionServiceMock.Verify(
            x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Movement Tests

    [Fact]
    public void TryExecute_MovingWithInstantDoT_CanExecuteDoT()
    {
        // Arrange - At level 72+, Dia is instant cast while moving
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableDoT = true;
        config.Dot.EnableDia = true;

        var enemyMock = CreateMockEnemy();

        var targetingServiceMock = MockBuilders.CreateMockTargetingService();
        targetingServiceMock.Setup(x => x.FindEnemyNeedingDot(
                It.IsAny<uint>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemyMock.Object);

        var actionServiceMock = MockBuilders.CreateMockActionService();
        actionServiceMock.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateTestContext(
            config: config,
            targetingService: targetingServiceMock,
            actionService: actionServiceMock,
            inCombat: true,
            level: 72); // Dia level

        // Act
        _module.TryExecute(context, isMoving: true); // Moving!

        // Assert - Dia should be executed
        actionServiceMock.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Dia.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Creates a test ApolloContext with mocked dependencies.
    /// </summary>
    private ApolloContext CreateTestContext(
        Configuration? config = null,
        Mock<ITargetingService>? targetingService = null,
        Mock<IActionService>? actionService = null,
        byte level = 90,
        bool inCombat = false)
    {
        config ??= MockBuilders.CreateDefaultConfiguration();
        targetingService ??= MockBuilders.CreateMockTargetingService();
        actionService ??= MockBuilders.CreateMockActionService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        var combatEventService = MockBuilders.CreateMockCombatEventService();
        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        var hpPredictionService = MockBuilders.CreateMockHpPredictionService();
        var playerStatsService = MockBuilders.CreateMockPlayerStatsService();
        var debuffDetectionService = MockBuilders.CreateMockDebuffDetectionService();
        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();

        var actionTracker = MockBuilders.CreateMockActionTracker(config);
        var statusHelper = new StatusHelper();
        var healingSpellSelector = CreateMockHealingSpellSelector();

        return new ApolloContext(
            player.Object,
            inCombat,
            isMoving: false,
            canExecuteGcd: true,
            canExecuteOgcd: true,
            actionService.Object,
            actionTracker,
            combatEventService.Object,
            damageIntakeService.Object,
            config,
            debuffDetectionService.Object,
            healingSpellSelector.Object,
            hpPredictionService.Object,
            objectTable.Object,
            partyList.Object,
            playerStatsService.Object,
            targetingService.Object,
            statusHelper,
            partyHelper.Object);
    }

    /// <summary>
    /// Creates a mock enemy target.
    /// </summary>
    private static Mock<IBattleNpc> CreateMockEnemy(
        uint entityId = 100,
        uint currentHp = 50000,
        uint maxHp = 50000)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.EntityId).Returns(entityId);
        mock.Setup(x => x.GameObjectId).Returns((ulong)entityId);
        mock.Setup(x => x.CurrentHp).Returns(currentHp);
        mock.Setup(x => x.MaxHp).Returns(maxHp);
        mock.Setup(x => x.Position).Returns(Vector3.Zero);
        return mock;
    }

    /// <summary>
    /// Creates a mock IHealingSpellSelector.
    /// </summary>
    private static Mock<IHealingSpellSelector> CreateMockHealingSpellSelector()
    {
        var mock = new Mock<IHealingSpellSelector>();
        mock.Setup(x => x.SelectBestSingleHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<IBattleChara>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<float>()))
            .Returns(((ActionDefinition?)null, 0));
        mock.Setup(x => x.SelectBestAoEHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<IBattleChara?>()))
            .Returns(((ActionDefinition?)null, 0, null));
        return mock;
    }

    #endregion
}
