using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Tests for Sage HealingModule logic.
/// Covers Addersgall heals, free oGCD heals, AoE healing, and AoE regression.
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
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableHealing = false;

        var context = AsclepiusTestContext.Create(config: config);

        // Act — healing disabled entirely
        // HealingModule doesn't have an explicit EnableHealing master switch check,
        // but config.EnableHealing affects multiple downstream decisions.
        // Verify it does not throw and returns gracefully.
        var result = _module.TryExecute(context, isMoving: false);

        // The module itself does not check EnableHealing at the top-level;
        // this tests the overall module handles edge cases without exception.
        Assert.False(result);
    }

    [Fact]
    public void TryExecute_NoHealingNeeded_AllHealthy_ReturnsFalse()
    {
        // Arrange: all party at full HP, no Addersgall healing thresholds met
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.55f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        // No lowest HP member (everyone healthy)
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<IPlayerCharacter>(), It.IsAny<int>()))
            .Returns((IBattleChara?)null);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((1.0f, 1.0f, 0)); // 100% avg, 100% lowest, 0 injured
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleChara?)null);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: false); // Can't weave anything

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: false,
            canExecuteOgcd: false);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
    }

    #endregion

    #region Druochole Tests (Addersgall single-target heal)

    [Fact]
    public void TryExecute_Druochole_NoAddersgall_SkipsDruochole()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.75f;
        config.Sage.AddersgallReserve = 0;

        var lowHpTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 20000, maxHp: 50000); // 40% HP

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowHpTarget.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 0); // Empty

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 0,
            canExecuteOgcd: true);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Druochole_TargetAboveThreshold_SkipsDruochole()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.55f; // 55% threshold

        var healthyTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 40000, maxHp: 50000); // 80% HP

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: healthyTarget.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallStacks: 3,
            canExecuteOgcd: true,
            level: 90);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Druochole_TargetBelowThreshold_ExecutesDruochole()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.75f;
        config.Sage.AddersgallReserve = 0;
        // Disable other oGCD heals to isolate Druochole
        config.Sage.EnableTaurochole = false;
        config.Sage.EnableIxochole = false;
        config.Sage.EnableKerachole = false;
        config.Sage.EnablePhysisII = false;
        config.Sage.EnableHolos = false;
        config.Sage.EnableHaima = false;
        config.Sage.EnablePanhaima = false;
        config.Sage.EnablePepsis = false;
        config.Sage.EnableRhizomata = false;
        config.Sage.EnableKrasis = false;
        config.Sage.EnableZoe = false;

        var lowHpTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 20000, maxHp: 50000); // 40% HP

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowHpTarget.Object);
        partyHelper.Setup(x => x.GetHpPercent(lowHpTarget.Object)).Returns(0.40f);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.40f, 0.40f, 1));
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleChara?)null);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                lowHpTarget.Object.GameObjectId))
            .Returns(true);

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 2);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 2,
            canExecuteOgcd: true,
            level: 90);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                lowHpTarget.Object.GameObjectId),
            Times.Once);
    }

    [Fact]
    public void TryExecute_Druochole_LevelTooLow_SkipsDruochole()
    {
        // Arrange: Druochole requires level 45
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.75f;

        var lowHpTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 20000, maxHp: 50000);

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowHpTarget.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallStacks: 3,
            canExecuteOgcd: true,
            level: 40); // Below Druochole level 45

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region AoE Healing Tests

    [Fact]
    public void TryExecute_Prognosis_NotEnoughInjured_SkipsPrognosis()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 3;
        config.Sage.AoEHealThreshold = 0.80f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.70f, 0.50f, 2)); // Only 2 injured, need 3

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 90);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Prognosis_EnoughInjured_ExecutesPrognosis()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 3;
        config.Sage.AoEHealThreshold = 0.80f;
        // Disable all higher-priority options so Prognosis fires
        config.Sage.AddersgallReserve = 0;
        config.Sage.EnablePhysisII = false;
        config.Sage.EnableHolos = false;
        config.Sage.EnableHaima = false;
        config.Sage.EnablePanhaima = false;
        config.Sage.EnablePepsis = false;
        config.Sage.EnableRhizomata = false;
        config.Sage.EnableKrasis = false;
        config.Sage.EnableZoe = false;
        config.Sage.EnableIxochole = false;
        config.Sage.EnableKerachole = false;
        config.Sage.EnablePneuma = false;
        config.Sage.EnableEukrasianPrognosis = false;
        config.Sage.EnableEukrasianDiagnosis = false;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.65f, 0.50f, 4)); // 4 injured at 65% avg — triggers Prognosis
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<IPlayerCharacter>(), It.IsAny<int>()))
            .Returns((IBattleChara?)null); // No single-target needed
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleChara?)null);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 0); // No stacks for Addersgall heals

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 0,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 90);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void TryExecute_Prognosis_Moving_SkipsPrognosis()
    {
        // Arrange: Prognosis has a cast time, cannot use while moving
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 3;
        config.Sage.AoEHealThreshold = 0.80f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 4));

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: true,
            level: 90);

        // Act: moving prevents cast spells
        _module.TryExecute(context, isMoving: true);

        // Assert
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    /// <summary>
    /// REGRESSION TEST: v4.10.7 bug class — AoE heal fails to fire in 8-man raid
    /// with 3+ members below threshold at level 100.
    /// Ensures Prognosis fires when 3+ party members need healing.
    /// </summary>
    [Fact]
    public void TryExecute_AoEHeal_8ManRaid_3MembersInjured_Level100_FiresAoEHeal()
    {
        // Arrange: simulate 8-man raid scenario at level 100
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 3;
        config.Sage.AoEHealThreshold = 0.80f;
        // Disable all higher-priority heals
        config.Sage.AddersgallReserve = 0;
        config.Sage.EnablePhysisII = false;
        config.Sage.EnableHolos = false;
        config.Sage.EnableHaima = false;
        config.Sage.EnablePanhaima = false;
        config.Sage.EnablePepsis = false;
        config.Sage.EnableRhizomata = false;
        config.Sage.EnableKrasis = false;
        config.Sage.EnableZoe = false;
        config.Sage.EnableIxochole = false;
        config.Sage.EnableKerachole = false;
        config.Sage.EnablePneuma = false;
        config.Sage.EnableEukrasianPrognosis = false;
        config.Sage.EnableEukrasianDiagnosis = false;
        config.Sage.EnableTaurochole = false;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 5)); // 5 of 8 injured at 60% avg
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<IPlayerCharacter>(), It.IsAny<int>()))
            .Returns((IBattleChara?)null);
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleChara?)null);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 0);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 0,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 100); // Level 100

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert: AoE heal must fire
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion

    #region Addersgall Resource Spending Tests

    [Fact]
    public void TryExecute_AddersgallAvailable_PrefersAddersgallHealOverGcd()
    {
        // Arrange: 3 Addersgall available with tank below threshold
        // Druochole (oGCD, Addersgall) should fire before Diagnosis (GCD)
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.75f;
        config.Sage.AddersgallReserve = 0;
        // Disable Taurochole and everything else higher priority
        config.Sage.EnableTaurochole = false;
        config.Sage.EnableIxochole = false;
        config.Sage.EnableKerachole = false;
        config.Sage.EnablePhysisII = false;
        config.Sage.EnableHolos = false;
        config.Sage.EnableHaima = false;
        config.Sage.EnablePanhaima = false;
        config.Sage.EnablePepsis = false;
        config.Sage.EnableRhizomata = false;
        config.Sage.EnableKrasis = false;
        config.Sage.EnableZoe = false;

        var lowHpTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 25000, maxHp: 50000); // 50% HP

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowHpTarget.Object);
        partyHelper.Setup(x => x.GetHpPercent(lowHpTarget.Object)).Returns(0.50f);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.50f, 0.50f, 1));
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleChara?)null);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                lowHpTarget.Object.GameObjectId))
            .Returns(true);

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 3);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 3,
            canExecuteGcd: true,
            canExecuteOgcd: true,
            level: 90);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert: oGCD Druochole fired, not GCD Diagnosis
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Druochole.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
        // GCD Diagnosis should not have been used
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Diagnosis.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_IxocholeFires_WhenMultipleInjured_AndAddersgallAvailable()
    {
        // Arrange: 3 injured party members, Ixochole available
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 3;
        config.Sage.IxocholeThreshold = 0.70f;
        config.Sage.AddersgallReserve = 0;
        // Disable higher-priority Druochole and Taurochole
        config.Sage.DruocholeThreshold = 0.20f; // Very low threshold so it doesn't fire
        config.Sage.EnableTaurochole = false;
        config.Sage.EnableKerachole = false;
        config.Sage.EnablePhysisII = false;
        config.Sage.EnableHolos = false;
        config.Sage.EnableHaima = false;
        config.Sage.EnablePanhaima = false;
        config.Sage.EnablePepsis = false;
        config.Sage.EnableRhizomata = false;
        config.Sage.EnableKrasis = false;
        config.Sage.EnableZoe = false;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 4)); // 4 injured at 60% avg
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<IPlayerCharacter>(), It.IsAny<int>()))
            .Returns((IBattleChara?)null);
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleChara?)null);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Ixochole.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Ixochole.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 2);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 2,
            canExecuteOgcd: true,
            level: 90);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Ixochole.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion

    #region PhysisII Tests (Free oGCD)

    [Fact]
    public void TryExecute_PhysisII_Disabled_SkipsPhysisII()
    {
        // Arrange
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePhysisII = false;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 4));

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 90);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.PhysisII.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_PhysisII_LevelTooLow_SkipsPhysisII()
    {
        // Arrange: PhysisII requires level 60
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePhysisII = true;
        config.Sage.PhysisIIThreshold = 0.80f;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.60f, 0.40f, 4));

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.PhysisII.ActionId)).Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteOgcd: true,
            level: 55); // Below PhysisII level 60

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.PhysisII.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Diagnosis Tests (GCD single-target)

    [Fact]
    public void TryExecute_Diagnosis_TargetAboveThreshold_SkipsDiagnosis()
    {
        // Arrange: TryDiagnosis skips when target HP is above DruocholeThreshold.
        // Note: Diagnosis has no explicit level check in the module — the game engine
        // enforces that. Module-level gating for Diagnosis is by HP threshold only.
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.75f;
        config.Sage.EnableEukrasianDiagnosis = false;
        config.Sage.EnableEukrasianPrognosis = false;

        // Target at 90% HP — above 75% threshold
        var highHpTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 45000, maxHp: 50000);

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: highHpTarget.Object);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.90f, 0.90f, 0)); // Everyone healthy

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 0);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 0,
            canExecuteGcd: true,
            level: 90);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert: Diagnosis skipped — target HP is above threshold
        Assert.False(result);
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Diagnosis.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    [Fact]
    public void TryExecute_Diagnosis_Moving_SkipsDiagnosis()
    {
        // Arrange: Diagnosis has cast time, can't use while moving
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DiagnosisThreshold = 0.75f;
        config.Sage.EnableEukrasianDiagnosis = false;
        config.Sage.EnableEukrasianPrognosis = false;

        var lowHpTarget = MockBuilders.CreateMockBattleChara(
            entityId: 2u, currentHp: 20000, maxHp: 50000);

        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowHpTarget.Object);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.40f, 0.40f, 1));

        var addersgall = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 0);
        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgall,
            addersgallStacks: 0,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            level: 90);

        // Act: moving prevents cast heals
        _module.TryExecute(context, isMoving: true);

        // Assert
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Diagnosis.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion
}
