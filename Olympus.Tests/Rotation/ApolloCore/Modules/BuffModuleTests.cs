using System.Numerics;
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

        var actionService = new Mock<IActionService>();
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

        var actionService = new Mock<IActionService>();
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

        var actionService = new Mock<IActionService>();
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

        var actionService = new Mock<IActionService>();
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

    #region Asylum Tests

    [Fact]
    public void TryExecute_AsylumDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = true;
        config.Healing.EnableAsylum = false;

        var actionService = new Mock<IActionService>();
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

        var actionService = new Mock<IActionService>();
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

        var actionService = new Mock<IActionService>();
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

        var actionService = new Mock<IActionService>();
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

        var actionService = new Mock<IActionService>();
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

    #endregion

    #region Lucid Dreaming Tests

    [Fact]
    public void TryExecute_LucidDreaming_HighMp_DoesNotExecute()
    {
        // Arrange: MP at 80%, threshold is 70%
        var config = MockBuilders.CreateDefaultConfiguration();

        var actionService = new Mock<IActionService>();
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
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.LucidDreaming.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LucidDreaming_LevelTooLow_DoesNotExecute()
    {
        // Arrange: Low MP but level too low (requires level 24)
        var config = MockBuilders.CreateDefaultConfiguration();

        var actionService = new Mock<IActionService>();
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
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.LucidDreaming.ActionId),
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

        var actionService = new Mock<IActionService>();
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Surecast.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_SurecastManualMode_DoesNotExecute()
    {
        // Arrange: Mode 0 = Manual
        var config = MockBuilders.CreateDefaultConfiguration();
        config.RoleActions.EnableSurecast = true;
        config.RoleActions.SurecastMode = 0;

        var actionService = new Mock<IActionService>();
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            level: 90,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Surecast.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Aetherial Shift Tests

    [Fact]
    public void TryExecute_AetherialShiftDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Buffs.EnableAetherialShift = false;

        var actionService = new Mock<IActionService>();
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

        var actionService = new Mock<IActionService>();
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

    private ApolloContext CreateTestContext(
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
        var objectTable = new Mock<IObjectTable>();
        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        var damageTrendService = MockBuilders.CreateMockDamageTrendService();
        var frameCache = MockBuilders.CreateMockFrameScopedCache();
        var mpForecastService = MockBuilders.CreateMockMpForecastService();
        var partyList = new Mock<IPartyList>();
        var playerStatsService = new Mock<IPlayerStatsService>();
        playerStatsService.Setup(p => p.GetHealingStats(It.IsAny<byte>()))
            .Returns((1000, 1000, 100));

        targetingService ??= new Mock<ITargetingService>();
        var statusHelper = new StatusHelper();

        partyHelper ??= new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((1.0f, 1.0f, 0));
        partyHelper.Setup(p => p.CountPartyMembersNeedingAoEHeal(It.IsAny<IPlayerCharacter>(), It.IsAny<int>()))
            .Returns((0, false, new System.Collections.Generic.List<(uint entityId, string name)>(), 0));

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
            partyHelper.Object);
    }

    #endregion
}
