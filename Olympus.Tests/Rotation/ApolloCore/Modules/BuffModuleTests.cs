using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Party;
using Dalamud.Plugin.Services;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Rotation.Common;
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
/// Tests for BuffModule buff and utility oGCD logic.
/// Covers Thin Air, Presence of Mind, Assize, Asylum, Lucid Dreaming, Surecast.
/// </summary>
public class BuffModuleTests
{
    private readonly BuffModule _module;

    public BuffModuleTests()
    {
        _module = new BuffModule();
    }

    #region Module Properties

    [Fact]
    public void Priority_Is30()
    {
        Assert.Equal(30, _module.Priority);
    }

    [Fact]
    public void Name_IsBuffs()
    {
        Assert.Equal("Buffs", _module.Name);
    }

    #endregion

    #region oGCD State Tests

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        // Arrange
        var context = CreateTestContext(canExecuteOgcd: false);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Thin Air Tests

    [Fact]
    public void TryExecute_ThinAirDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Buffs.EnableThinAir = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.ThinAir.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_ThinAirLevelTooLow_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Buffs.EnableThinAir = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Thin Air requires level 58
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 50,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.ThinAir.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Presence of Mind Tests

    [Fact]
    public void TryExecute_PoMDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Buffs.EnablePresenceOfMind = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.PresenceOfMind.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_PoMLevelTooLow_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Buffs.EnablePresenceOfMind = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // PoM requires level 30
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 20,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.PresenceOfMind.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Thin Air Positive Tests

    [Fact]
    public void TryExecute_ThinAirAtMaxCharges_Fires()
    {
        // Arrange: Thin Air at max charges — should fire to avoid wasting charge regen
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Buffs.EnableThinAir = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Set current charges = max charges (2) to trigger charge-cap path
        actionService.Setup(a => a.GetCurrentCharges(WHMActions.ThinAir.ActionId)).Returns(2u);
        actionService.Setup(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.ThinAir.ActionId),
            It.IsAny<ulong>())).Returns(true);

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteOgcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.ThinAir.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Presence of Mind Positive Tests

    [Fact]
    public void TryExecute_PoMEnabled_Fires()
    {
        // Arrange: PoM enabled, level 90 WHM, off cooldown
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Buffs.EnablePresenceOfMind = true;
        config.Buffs.DelayPoMForRaise = false;
        config.Buffs.StackPoMWithAssize = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.PresenceOfMind.ActionId),
            It.IsAny<ulong>())).Returns(true);

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteOgcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.PresenceOfMind.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Asylum Tests

    [Fact]
    public void TryExecute_AsylumDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = true;
        config.Healing.EnableAsylum = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteGroundTargetedOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Asylum.ActionId),
            It.IsAny<Vector3>()), Times.Never);
    }

    [Fact]
    public void TryExecute_AsylumLevelTooLow_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = true;
        config.Healing.EnableAsylum = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Asylum requires level 52
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 45,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteGroundTargetedOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Asylum.ActionId),
            It.IsAny<Vector3>()), Times.Never);
    }

    [Fact]
    public void TryExecute_AsylumHealingDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = false;
        config.Healing.EnableAsylum = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteGroundTargetedOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Asylum.ActionId),
            It.IsAny<Vector3>()), Times.Never);
    }

    #endregion

    #region Assize Tests

    [Fact]
    public void TryExecute_AssizeDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.EnableAssize = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Assize.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_AssizeLevelTooLow_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.EnableAssize = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        // Assize requires level 56
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 50,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Assize.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_AssizeEnabled_Fires()
    {
        // Arrange: Assize enabled, level 90, off cooldown
        // Disable higher-priority abilities so Assize is reached
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.EnableAssize = true;
        config.Buffs.EnableThinAir = false;
        config.Buffs.EnablePresenceOfMind = false;
        config.EnableHealing = false; // disables Asylum path

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Assize.ActionId),
            It.IsAny<ulong>())).Returns(true);

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteOgcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Assize.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Lucid Dreaming Tests

    [Fact]
    public void TryExecute_LucidDreaming_HighMp_DoesNotExecute()
    {
        // Arrange: MP at 80%, threshold is 70%
        var config = MockBuilders.CreateDefaultConfiguration();

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            currentMp: 8000,  // 80% of 10000
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.LucidDreaming.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LucidDreaming_LevelTooLow_DoesNotExecute()
    {
        // Arrange: Low MP but level too low (requires level 24)
        var config = MockBuilders.CreateDefaultConfiguration();

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 20,
            currentMp: 5000,  // 50% of 10000
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.LucidDreaming.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Surecast Tests

    [Fact]
    public void TryExecute_SurecastDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.RoleActions.EnableSurecast = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.Surecast.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_SurecastManualMode_DoesNotExecute()
    {
        // Arrange: Mode 0 = Manual
        var config = MockBuilders.CreateDefaultConfiguration();
        config.RoleActions.EnableSurecast = true;
        config.RoleActions.SurecastMode = 0;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.Surecast.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LucidDreaming_LowMp_Fires()
    {
        // Arrange: MP at 50%, threshold is 70% — should trigger
        var config = MockBuilders.CreateDefaultConfiguration();
        config.HealerShared.EnableLucidDreaming = true;
        config.Buffs.EnablePredictiveLucid = false; // disable predictive to use threshold-based
        // Disable higher-priority abilities so Lucid is reached
        config.Buffs.EnableThinAir = false;
        config.Buffs.EnablePresenceOfMind = false;
        config.EnableHealing = false;
        config.Healing.EnableAssize = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.LucidDreaming.ActionId),
            It.IsAny<ulong>())).Returns(true);

        // Create context with low MP mock forecast service
        var mpForecastService = MockBuilders.CreateMockMpForecastService(
            currentMp: 5000, maxMp: 10000);

        var playerChar = MockBuilders.CreateMockPlayerCharacter(
            level: 90,
            currentHp: 50000,
            maxHp: 50000,
            currentMp: 5000);

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        var debuffDetectionService = MockBuilders.CreateMockDebuffDetectionService();
        var targetingService = MockBuilders.CreateMockTargetingService();
        var combatEventService = MockBuilders.CreateMockCombatEventService();
        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        var damageTrendService = MockBuilders.CreateMockDamageTrendService();
        var frameCache = MockBuilders.CreateMockFrameScopedCache();
        var hpPredictionService = MockBuilders.CreateMockHpPredictionService();
        var playerStatsService = MockBuilders.CreateMockPlayerStatsService();
        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();
        var cooldownPlanner = MockBuilders.CreateMockCooldownPlanner();
        var actionTracker = MockBuilders.CreateMockActionTracker(config);
        var statusHelper = new StatusHelper();
        var healingSpellSelector = ApolloTestContext.CreateDefaultHealingSpellSelector();

        var context = new ApolloContext(
            playerChar.Object,
            inCombat: false,
            isMoving: false,
            canExecuteGcd: true,
            canExecuteOgcd: true,
            actionService.Object,
            actionTracker,
            combatEventService.Object,
            damageIntakeService.Object,
            damageTrendService.Object,
            frameCache.Object,
            config,
            debuffDetectionService.Object,
            hpPredictionService.Object,
            mpForecastService.Object,
            objectTable.Object,
            partyList.Object,
            playerStatsService.Object,
            targetingService.Object,
            healingSpellSelector.Object,
            cooldownPlanner.Object,
            statusHelper,
            partyHelper.Object);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == RoleActions.LucidDreaming.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    #endregion

    #region Aetherial Shift Tests

    [Fact]
    public void TryExecute_AetherialShiftDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Buffs.EnableAetherialShift = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.AetherialShift.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_AetherialShiftMoving_DoesNotExecute()
    {
        // Arrange: Moving prevents Aetherial Shift
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Buffs.EnableAetherialShift = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 100,
            canExecuteOgcd: true);

        // Act - note isMoving: true
        _module.TryExecute(context, isMoving: true);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.AetherialShift.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Helper Methods

    private static ApolloContext CreateTestContext(
        Configuration? config = null,
        Mock<IPartyHelper>? partyHelper = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targetingService = null,
        byte level = 90,
        uint currentHp = 50000,
        uint maxHp = 50000,
        uint currentMp = 10000,
        bool inCombat = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false)
    {
        return ApolloTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            targetingService: targetingService,
            level: level,
            currentHp: currentHp,
            maxHp: maxHp,
            currentMp: currentMp,
            inCombat: inCombat,
            canExecuteGcd: canExecuteGcd,
            canExecuteOgcd: canExecuteOgcd);
    }

    #endregion
}
