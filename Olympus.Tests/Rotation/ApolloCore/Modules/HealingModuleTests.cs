using System.Collections.Generic;
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
/// Tests for HealingModule healing logic.
/// Covers configuration toggles, level requirements, priority order, and threshold behavior.
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

    #region Configuration Toggles - EnableHealing

    [Fact]
    public void TryExecute_HealingDisabled_ReturnsFalse()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = false;

        var context = CreateTestContext(config: config);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void TryExecute_HealingEnabled_NoTargetsNeedHealing_ReturnsFalse()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableHealing = true;

        // No one needs healing (all at full HP)
        var partyHelperMock = MockBuilders.CreateMockPartyHelper();
        partyHelperMock.Setup(x => x.FindLowestHpPartyMember(It.IsAny<IPlayerCharacter>(), It.IsAny<int>()))
            .Returns((IBattleChara?)null);

        var healingSpellSelectorMock = CreateMockHealingSpellSelector();
        healingSpellSelectorMock.Setup(x => x.SelectBestSingleHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<IBattleChara>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<float>()))
            .Returns(((ActionDefinition?)null, 0));

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            healingSpellSelector: healingSpellSelectorMock.Object);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Benediction Tests

    [Fact]
    public void TryExecute_BenedictionDisabled_SkipsBenediction()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableBenediction = false;

        var lowHpTarget = MockBuilders.CreateMockBattleChara(
            currentHp: 5000, maxHp: 50000); // 10% HP

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowHpTarget.Object);
        partyHelperMock.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>()))
            .Returns(0.10f);

        var actionServiceMock = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            actionService: actionServiceMock,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert - Benediction should NOT be executed
        actionServiceMock.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Benediction.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_BenedictionEnabled_TargetAboveThreshold_SkipsBenediction()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableBenediction = true;
        config.BenedictionEmergencyThreshold = 0.30f;

        var target = MockBuilders.CreateMockBattleChara(
            currentHp: 25000, maxHp: 50000); // 50% HP, above 30% threshold

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);
        partyHelperMock.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>()))
            .Returns(0.50f);

        var actionServiceMock = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            actionService: actionServiceMock,
            level: 90,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert - Benediction should NOT be executed (target not critical)
        actionServiceMock.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Benediction.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_BenedictionEnabled_TargetBelowThreshold_ExecutesBenediction()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableBenediction = true;
        config.BenedictionEmergencyThreshold = 0.30f;

        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2,
            currentHp: 10000,
            maxHp: 50000); // 20% HP, below 30% threshold

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);
        partyHelperMock.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>()))
            .Returns(0.20f);

        var actionServiceMock = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionServiceMock.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            actionService: actionServiceMock,
            level: 90,
            canExecuteOgcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionServiceMock.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Benediction.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_Benediction_LevelTooLow_SkipsBenediction()
    {
        // Arrange - Benediction requires level 50
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableBenediction = true;
        config.BenedictionEmergencyThreshold = 0.30f;

        var target = MockBuilders.CreateMockBattleChara(
            currentHp: 5000, maxHp: 50000); // 10% HP

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);
        partyHelperMock.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>()))
            .Returns(0.10f);

        var actionServiceMock = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            actionService: actionServiceMock,
            level: 45, // Below Benediction's level 50 requirement
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionServiceMock.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Benediction.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Benediction_CannotExecuteOgcd_SkipsBenediction()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableBenediction = true;

        var target = MockBuilders.CreateMockBattleChara(
            currentHp: 5000, maxHp: 50000);

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);
        partyHelperMock.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>()))
            .Returns(0.10f);

        var actionServiceMock = MockBuilders.CreateMockActionService(canExecuteOgcd: false);

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            actionService: actionServiceMock,
            level: 90,
            canExecuteOgcd: false); // Cannot weave oGCD

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionServiceMock.Verify(
            x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Esuna Tests

    [Fact]
    public void TryExecute_EsunaDisabled_SkipsEsuna()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableEsuna = false;

        var context = CreateTestContext(config: config, inCombat: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.Equal("Disabled", context.Debug.EsunaState);
    }

    [Fact]
    public void TryExecute_Esuna_NotInCombat_SkipsEsuna()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableEsuna = true;

        var debuffServiceMock = MockBuilders.CreateMockDebuffDetectionService(
            target => (100u, DebuffPriority.Lethal, 10f));

        var context = CreateTestContext(
            config: config,
            debuffDetectionService: debuffServiceMock,
            inCombat: false); // Not in combat

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert - Esuna check is only done when InCombat
        // The debug state won't be "Disabled" because we never entered that path
    }

    [Fact]
    public void TryExecute_Esuna_LevelTooLow_SkipsEsuna()
    {
        // Arrange - Esuna requires level 10
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableEsuna = true;

        var context = CreateTestContext(
            config: config,
            level: 5, // Below Esuna's level 10 requirement
            inCombat: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.Contains("Level 5 < 10", context.Debug.EsunaState);
    }

    [Fact]
    public void TryExecute_Esuna_NotEnoughMp_SkipsEsuna()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableEsuna = true;

        var context = CreateTestContext(
            config: config,
            level: 90,
            currentMp: 100, // Not enough MP (Esuna costs 400)
            inCombat: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.Contains("MP 100 < 400", context.Debug.EsunaState);
    }

    [Fact]
    public void TryExecute_Esuna_Moving_SkipsEsuna()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableEsuna = true;

        var debuffedMember = MockBuilders.CreateMockBattleChara(
            entityId: 2, name: "DebuffedPlayer", currentHp: 40000, maxHp: 50000);

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(
            partyMembers: new List<IBattleChara> { debuffedMember.Object });

        var debuffServiceMock = MockBuilders.CreateMockDebuffDetectionService(
            target => (100u, DebuffPriority.Lethal, 10f));

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            debuffDetectionService: debuffServiceMock,
            level: 90,
            inCombat: true);

        // Act
        _module.TryExecute(context, isMoving: true); // Moving!

        // Assert
        Assert.Equal("Moving", context.Debug.EsunaState);
    }

    #endregion

    #region Regen Tests

    [Fact]
    public void TryExecute_RegenDisabled_SkipsRegen()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableRegen = false;

        var context = CreateTestContext(config: config, inCombat: true);

        // Act - Regen should not be attempted
        _module.TryExecute(context, isMoving: false);

        // No direct assertion - just verify no exception and module continues
    }

    [Fact]
    public void TryExecute_Regen_NotInCombat_SkipsRegen()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableRegen = true;

        var target = MockBuilders.CreateMockBattleChara(
            currentHp: 40000, maxHp: 50000); // 80% HP, needs regen

        var partyHelperMock = MockBuilders.CreateMockPartyHelper();
        partyHelperMock.Setup(x => x.FindRegenTarget(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<float>(),
                It.IsAny<float>()))
            .Returns(target.Object);

        var actionServiceMock = MockBuilders.CreateMockActionService();

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            actionService: actionServiceMock,
            level: 90,
            inCombat: false); // Not in combat

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert - Regen GCD should NOT be executed (not in combat)
        actionServiceMock.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Regen.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Regen_LevelTooLow_SkipsRegen()
    {
        // Arrange - Regen requires level 35
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableRegen = true;

        var target = MockBuilders.CreateMockBattleChara(
            currentHp: 40000, maxHp: 50000);

        var partyHelperMock = MockBuilders.CreateMockPartyHelper();
        partyHelperMock.Setup(x => x.FindRegenTarget(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<float>(),
                It.IsAny<float>()))
            .Returns(target.Object);

        var actionServiceMock = MockBuilders.CreateMockActionService();

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            actionService: actionServiceMock,
            level: 30, // Below Regen's level 35 requirement
            inCombat: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionServiceMock.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Regen.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Tetragrammaton Tests

    [Fact]
    public void TryExecute_TetragrammatonDisabled_SkipsTetragrammaton()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableTetragrammaton = false;

        var target = MockBuilders.CreateMockBattleChara(
            currentHp: 30000, maxHp: 50000); // 60% HP

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);

        var actionServiceMock = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            actionService: actionServiceMock,
            level: 90,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionServiceMock.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Tetragrammaton.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Tetragrammaton_LevelTooLow_SkipsTetragrammaton()
    {
        // Arrange - Tetragrammaton requires level 60
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableTetragrammaton = true;

        var target = MockBuilders.CreateMockBattleChara(
            currentHp: 30000, maxHp: 50000);

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);

        var actionServiceMock = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            actionService: actionServiceMock,
            level: 55, // Below Tetragrammaton's level 60 requirement
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionServiceMock.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Tetragrammaton.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Tetragrammaton_CannotExecuteOgcd_SkipsTetragrammaton()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableTetragrammaton = true;

        var target = MockBuilders.CreateMockBattleChara(
            currentHp: 30000, maxHp: 50000);

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);

        var actionServiceMock = MockBuilders.CreateMockActionService(canExecuteOgcd: false);

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            actionService: actionServiceMock,
            level: 90,
            canExecuteOgcd: false);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionServiceMock.Verify(
            x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region AoE Healing Tests

    [Fact]
    public void TryExecute_AoEHeal_LevelTooLow_SkipsAoEHeal()
    {
        // Arrange - Medica requires level 10
        var config = MockBuilders.CreateDefaultConfiguration();
        config.AoEHealMinTargets = 3;

        var context = CreateTestContext(
            config: config,
            level: 5); // Below Medica's level 10 requirement

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.Contains("Level 5 < 10", context.Debug.AoEStatus);
    }

    [Fact]
    public void TryExecute_AoEHeal_NotEnoughTargets_SkipsAoEHeal()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.AoEHealMinTargets = 3;

        var partyHelperMock = MockBuilders.CreateMockPartyHelper();
        partyHelperMock.Setup(x => x.CountPartyMembersNeedingAoEHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<int>()))
            .Returns((2, false, new List<(uint, string)>(), 5000)); // Only 2 injured, need 3

        partyHelperMock.Setup(x => x.FindBestCureIIITarget(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<int>()))
            .Returns(((IBattleChara?)null, 1, new List<uint>()));

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            level: 90);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.Contains("Injured 2", context.Debug.AoEStatus);
        Assert.Contains("< min 3", context.Debug.AoEStatus);
    }

    [Fact]
    public void TryExecute_AoEHeal_Moving_SkipsAoEHeal()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();
        config.AoEHealMinTargets = 3;

        var partyHelperMock = MockBuilders.CreateMockPartyHelper();
        partyHelperMock.Setup(x => x.CountPartyMembersNeedingAoEHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<int>()))
            .Returns((4, false, new List<(uint, string)> { (1, "A"), (2, "B"), (3, "C"), (4, "D") }, 5000));

        partyHelperMock.Setup(x => x.FindBestCureIIITarget(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<int>()))
            .Returns(((IBattleChara?)null, 0, new List<uint>()));

        var healingSpellSelectorMock = CreateMockHealingSpellSelector();
        healingSpellSelectorMock.Setup(x => x.SelectBestAoEHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<IBattleChara?>()))
            .Returns((WHMActions.Medica, 4000, null)); // Returns a casted spell

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            healingSpellSelector: healingSpellSelectorMock.Object,
            level: 90);

        // Act
        _module.TryExecute(context, isMoving: true); // Moving!

        // Assert
        Assert.Equal("Moving", context.Debug.AoEStatus);
    }

    #endregion

    #region Single Heal Tests

    [Fact]
    public void TryExecute_SingleHeal_LevelTooLow_SkipsSingleHeal()
    {
        // Arrange - Cure requires level 2
        var config = MockBuilders.CreateDefaultConfiguration();

        var target = MockBuilders.CreateMockBattleChara(
            currentHp: 30000, maxHp: 50000);

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            level: 1); // Below Cure's level 2 requirement

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert - should return false (level too low for any healing)
        Assert.False(result);
    }

    [Fact]
    public void TryExecute_SingleHeal_Moving_SkipsCastedHeal()
    {
        // Arrange
        var config = MockBuilders.CreateDefaultConfiguration();

        var target = MockBuilders.CreateMockBattleChara(
            currentHp: 30000, maxHp: 50000);

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);

        var healingSpellSelectorMock = CreateMockHealingSpellSelector();
        healingSpellSelectorMock.Setup(x => x.SelectBestSingleHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<IBattleChara>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<float>()))
            .Returns((WHMActions.Cure, 5000)); // Returns a casted spell

        var actionServiceMock = MockBuilders.CreateMockActionService();

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            healingSpellSelector: healingSpellSelectorMock.Object,
            actionService: actionServiceMock,
            level: 90);

        // Act
        _module.TryExecute(context, isMoving: true); // Moving!

        // Assert - ExecuteGcd should NOT be called (moving blocks casted spells)
        actionServiceMock.Verify(
            x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Priority Order Tests

    [Fact]
    public void TryExecute_Benediction_HasHighestPriority()
    {
        // Arrange - Set up scenario where both Benediction and single heal could trigger
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableBenediction = true;
        config.BenedictionEmergencyThreshold = 0.30f;

        var target = MockBuilders.CreateMockBattleChara(
            entityId: 2,
            currentHp: 10000,
            maxHp: 50000); // 20% HP

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);
        partyHelperMock.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>()))
            .Returns(0.20f);

        var actionServiceMock = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionServiceMock.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var healingSpellSelectorMock = CreateMockHealingSpellSelector();
        healingSpellSelectorMock.Setup(x => x.SelectBestSingleHeal(
                It.IsAny<IPlayerCharacter>(),
                It.IsAny<IBattleChara>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<float>()))
            .Returns((WHMActions.CureII, 8000));

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            actionService: actionServiceMock,
            healingSpellSelector: healingSpellSelectorMock.Object,
            level: 90,
            canExecuteOgcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert - Benediction should be executed first (highest priority)
        Assert.True(result);
        actionServiceMock.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Benediction.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
        // Single heal GCD should NOT be executed (Benediction already succeeded)
        actionServiceMock.Verify(
            x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Esuna_HasSecondHighestPriority_AfterBenediction()
    {
        // Arrange - Benediction not triggered (target not low enough), but Esuna needed
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableBenediction = true;
        config.BenedictionEmergencyThreshold = 0.30f;
        config.EnableEsuna = true;
        config.EsunaPriorityThreshold = 5;

        var debuffedMember = MockBuilders.CreateMockBattleChara(
            entityId: 2, name: "DebuffedPlayer", currentHp: 40000, maxHp: 50000);

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(
            partyMembers: new List<IBattleChara> { debuffedMember.Object },
            lowestHpMember: debuffedMember.Object);
        partyHelperMock.Setup(x => x.GetHpPercent(It.IsAny<IBattleChara>()))
            .Returns(0.80f); // 80% HP - above Benediction threshold

        var debuffServiceMock = MockBuilders.CreateMockDebuffDetectionService(
            target => (100u, DebuffPriority.Lethal, 10f));

        var actionServiceMock = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionServiceMock.Setup(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()))
            .Returns(true);

        var context = CreateTestContext(
            config: config,
            partyHelper: partyHelperMock,
            debuffDetectionService: debuffServiceMock,
            actionService: actionServiceMock,
            level: 90,
            inCombat: true,
            canExecuteOgcd: true);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert - Esuna should be executed (Benediction skipped due to HP threshold)
        Assert.True(result);
        actionServiceMock.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Esuna.ActionId),
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
        Mock<IPartyHelper>? partyHelper = null,
        Mock<IActionService>? actionService = null,
        Mock<IDebuffDetectionService>? debuffDetectionService = null,
        IHealingSpellSelector? healingSpellSelector = null,
        byte level = 90,
        uint currentHp = 50000,
        uint maxHp = 50000,
        uint currentMp = 10000,
        bool inCombat = false,
        bool canExecuteGcd = true,
        bool canExecuteOgcd = false)
    {
        config ??= MockBuilders.CreateDefaultConfiguration();
        partyHelper ??= MockBuilders.CreateMockPartyHelper();
        actionService ??= MockBuilders.CreateMockActionService(canExecuteGcd: canExecuteGcd, canExecuteOgcd: canExecuteOgcd);
        debuffDetectionService ??= MockBuilders.CreateMockDebuffDetectionService();

        var player = MockBuilders.CreateMockPlayerCharacter(
            level: level,
            currentHp: currentHp,
            maxHp: maxHp,
            currentMp: currentMp);

        var combatEventService = MockBuilders.CreateMockCombatEventService();
        var hpPredictionService = MockBuilders.CreateMockHpPredictionService();
        var playerStatsService = MockBuilders.CreateMockPlayerStatsService();
        var targetingService = MockBuilders.CreateMockTargetingService();
        var objectTable = MockBuilders.CreateMockObjectTable();
        var partyList = MockBuilders.CreateMockPartyList();

        // Create real ActionTracker and StatusHelper with mocked dependencies
        var actionTracker = MockBuilders.CreateMockActionTracker(config);
        var statusHelper = new StatusHelper();

        // Use provided or create default mock HealingSpellSelector
        healingSpellSelector ??= CreateMockHealingSpellSelector().Object;

        return new ApolloContext(
            player.Object,
            inCombat,
            isMoving: false,
            canExecuteGcd,
            canExecuteOgcd,
            actionService.Object,
            actionTracker,
            combatEventService.Object,
            config,
            debuffDetectionService.Object,
            healingSpellSelector,
            hpPredictionService.Object,
            objectTable.Object,
            partyList.Object,
            playerStatsService.Object,
            targetingService.Object,
            statusHelper,
            partyHelper.Object);
    }

    /// <summary>
    /// Creates a mock IHealingSpellSelector that returns no heals by default.
    /// </summary>
    private static Mock<IHealingSpellSelector> CreateMockHealingSpellSelector()
    {
        var mock = new Mock<IHealingSpellSelector>();

        // Default: return no heal
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
