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
/// Tests for DefensiveModule defensive cooldown logic.
/// Covers configuration toggles, level requirements, and cooldown triggers.
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

    #region Combat State Tests

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        // Arrange
        var context = CreateTestContext(inCombat: false, canExecuteOgcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryExecute_CannotExecuteOgcd_ReturnsFalse()
    {
        // Arrange
        var context = CreateTestContext(inCombat: true, canExecuteOgcd: false);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Divine Benison Tests

    [Fact]
    public void TryExecute_DivineBenisonDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableDivineBenison = false;

        var actionService = new Mock<IActionService>();
        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.8f, 0.7f, 2));

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.DivineBenison.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_DivineBenisonLevelTooLow_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableDivineBenison = true;

        var actionService = new Mock<IActionService>();
        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.8f, 0.7f, 2));

        // DivineBenison requires level 66
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 50,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.DivineBenison.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Aquaveil Tests

    [Fact]
    public void TryExecute_AquaveilDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Defensive.EnableAquaveil = false;

        var actionService = new Mock<IActionService>();
        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.8f, 0.7f, 2));

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Aquaveil.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_AquaveilLevelTooLow_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Defensive.EnableAquaveil = true;

        var actionService = new Mock<IActionService>();
        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.8f, 0.7f, 2));

        // Aquaveil requires level 86
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 80,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Aquaveil.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Temperance Tests

    [Fact]
    public void TryExecute_TemperanceDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableTemperance = false;

        var actionService = new Mock<IActionService>();
        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.5f, 0.3f, 4)); // Low HP to trigger

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Temperance.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_TemperanceLevelTooLow_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableTemperance = true;

        var actionService = new Mock<IActionService>();
        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.5f, 0.3f, 4)); // Low HP to trigger

        // Temperance requires level 80
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 70,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.Temperance.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Liturgy of the Bell Tests

    [Fact]
    public void TryExecute_LiturgyDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Defensive.EnableLiturgyOfTheBell = false;

        var actionService = new Mock<IActionService>();
        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.5f, 0.3f, 3)); // Multiple injured

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteGroundTargetedOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.LiturgyOfTheBell.ActionId),
            It.IsAny<Vector3>()), Times.Never);
    }

    [Fact]
    public void TryExecute_LiturgyNotEnoughInjured_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Defensive.EnableLiturgyOfTheBell = true;

        var actionService = new Mock<IActionService>();
        actionService.Setup(a => a.IsActionReady(WHMActions.LiturgyOfTheBell.ActionId))
            .Returns(true);

        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.9f, 0.8f, 1)); // Only 1 injured, need >= 2

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteGroundTargetedOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.LiturgyOfTheBell.ActionId),
            It.IsAny<Vector3>()), Times.Never);
    }

    #endregion

    #region Plenary Indulgence Tests

    [Fact]
    public void TryExecute_PlenaryDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnablePlenaryIndulgence = false;

        var actionService = new Mock<IActionService>();
        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.6f, 0.4f, 3));

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.PlenaryIndulgence.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_PlenaryLevelTooLow_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnablePlenaryIndulgence = true;

        var actionService = new Mock<IActionService>();
        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.6f, 0.4f, 3));

        // Plenary Indulgence requires level 70
        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 60,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.PlenaryIndulgence.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Divine Caress Tests

    [Fact]
    public void TryExecute_DivineCaressDisabled_DoesNotExecute()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = true;
        config.Defensive.EnableDivineCaress = false;

        var actionService = new Mock<IActionService>();
        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.8f, 0.7f, 2));

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad => ad.ActionId == WHMActions.DivineCaress.ActionId),
            It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region Healing Master Toggle Tests

    [Fact]
    public void TryExecute_HealingDisabled_DoesNotExecuteDefensives()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = false;
        config.Defensive.EnableTemperance = true;
        config.Defensive.EnableDivineBenison = true;
        config.Defensive.EnablePlenaryIndulgence = true;

        var actionService = new Mock<IActionService>();
        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.5f, 0.3f, 4)); // Low HP to trigger

        var context = CreateTestContext(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert - None of the defensive actions that require EnableHealing should fire
        actionService.Verify(a => a.ExecuteOgcd(
            It.Is<ActionDefinition>(ad =>
                ad.ActionId == WHMActions.Temperance.ActionId ||
                ad.ActionId == WHMActions.DivineBenison.ActionId ||
                ad.ActionId == WHMActions.PlenaryIndulgence.ActionId ||
                ad.ActionId == WHMActions.DivineCaress.ActionId),
            It.IsAny<ulong>()), Times.Never);
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

        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        var hpPredictionService = new Mock<IHpPredictionService>();
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

    #endregion
}
