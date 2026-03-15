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
/// Tests for the HealingModule coordinator.
/// Covers module properties, priority ordering, isMoving guards, and AoE regression.
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
    public void TryExecute_NoHealingNeeded_ReturnsFalse()
    {
        // Arrange: player at full HP, no party members injured, cannot weave.
        // HealingModule has no EnableHealing master-switch guard — it returns false
        // when no healing condition is met.
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindLowestHpPartyMember(It.IsAny<IPlayerCharacter>(), It.IsAny<int>()))
            .Returns((IBattleChara?)null);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((1.0f, 1.0f, 0));
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleChara?)null);

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: false,
            canExecuteOgcd: false);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            canExecuteGcd: false,
            canExecuteOgcd: false,
            currentHp: 50000,
            maxHp: 50000);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert: nothing to heal, module returns false
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

    #region Priority Coordination Tests

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

    #region isMoving Guard Tests (coordinator responsibility)

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

    #region Routing Tests

    [Fact]
    public void TryExecute_WhenCanExecuteOgcdOnly_DoesNotRunGcdHandlers()
    {
        // canExecuteOgcd=true, canExecuteGcd=false
        // All oGCD handlers return false → result is false
        // GCD handlers must not have been called (no action executed)
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: false, canExecuteOgcd: true);
        var context = AsclepiusTestContext.Create(actionService: actionService,
            canExecuteGcd: false, canExecuteOgcd: true);
        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);
        actionService.Verify(x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void TryExecute_WhenCanExecuteGcdOnly_DoesNotRunOgcdHandlers()
    {
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        var context = AsclepiusTestContext.Create(actionService: actionService,
            canExecuteGcd: true, canExecuteOgcd: false);
        var result = _module.TryExecute(context, isMoving: false);
        Assert.False(result);
        actionService.Verify(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    #endregion

    #region AoE Regression Tests

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

    /// <summary>
    /// Regression test for v4.10.7 bug class — uses real party counting logic, not mocked.
    /// An 8-man raid with 3 members below the injured threshold (0.95f) and party avg below
    /// AoEHealThreshold (0.80f) must cause Prognosis to fire. A mocked CalculatePartyHealthMetrics
    /// would not catch regressions in the counting logic itself.
    /// </summary>
    [Fact]
    public void TryExecute_AoEHeal_8ManRaid_RealPartyHelper_3MembersInjured_Level100_FiresAoEHeal()
    {
        // Regression test for v4.10.7 bug class — uses real party counting logic, not mocked

        // Arrange: 8-man party — 5 healthy members (96% HP = not injured) + 3 low-HP members (50% = injured)
        // InjuredHpThreshold = 0.95f, so 96% is NOT injured; 50% IS injured.
        // Average HP = (5 * 0.96 + 3 * 0.50) / 8 = (4.80 + 1.50) / 8 = 0.7875 — below AoEHealThreshold(0.80)
        var members = new List<IBattleChara>
        {
            MockBuilders.CreateMockBattleChara(entityId: 1u, currentHp: 48000, maxHp: 50000).Object, // 96%
            MockBuilders.CreateMockBattleChara(entityId: 2u, currentHp: 48000, maxHp: 50000).Object, // 96%
            MockBuilders.CreateMockBattleChara(entityId: 3u, currentHp: 48000, maxHp: 50000).Object, // 96%
            MockBuilders.CreateMockBattleChara(entityId: 4u, currentHp: 48000, maxHp: 50000).Object, // 96%
            MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 48000, maxHp: 50000).Object, // 96%
            MockBuilders.CreateMockBattleChara(entityId: 6u, currentHp: 25000, maxHp: 50000).Object, // 50% — injured
            MockBuilders.CreateMockBattleChara(entityId: 7u, currentHp: 25000, maxHp: 50000).Object, // 50% — injured
            MockBuilders.CreateMockBattleChara(entityId: 8u, currentHp: 25000, maxHp: 50000).Object, // 50% — injured
        };

        var realPartyHelper = new TestableApolloPartyHelper(members);

        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 3;
        config.Sage.AoEHealThreshold = 0.80f;
        // Disable all higher-priority heals so only Prognosis can fire
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

        var actionService = MockBuilders.CreateMockActionService(
            canExecuteGcd: true,
            canExecuteOgcd: false);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.CreateWithRealPartyHelper(
            realPartyHelper: realPartyHelper,
            config: config,
            actionService: actionService,
            addersgallStacks: 0,
            level: 100,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        // Act
        var result = _module.TryExecute(context, isMoving: false);

        // Assert: real party counting must identify 3 injured members and trigger AoE heal
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Prognosis.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion
}
