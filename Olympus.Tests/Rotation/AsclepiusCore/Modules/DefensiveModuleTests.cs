using Dalamud.Game.ClientState.Objects.SubKinds;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Tests for Sage DefensiveModule logic.
/// Covers Taurochole, Kerachole, Holos, Panhaima, and Haima.
/// </summary>
public class DefensiveModuleTests
{
    #region Module Properties

    [Fact]
    public void Priority_Is20()
    {
        var module = new DefensiveModule();
        Assert.Equal(20, module.Priority);
    }

    [Fact]
    public void Name_IsDefensive()
    {
        var module = new DefensiveModule();
        Assert.Equal("Defensive", module.Name);
    }

    #endregion

    #region Combat Guard

    [Fact]
    public void DefensiveModule_DoesNotFire_WhenNotInCombat()
    {
        // Arrange: not in combat — base class returns false before any defensive logic
        var module = new DefensiveModule();
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: false,           // not in combat
            canExecuteOgcd: true,
            addersgallStacks: 3,
            level: 90);

        // Act
        var result = module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
        actionService.Verify(
            x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Taurochole Tests

    [Fact]
    public void Taurochole_Fires_WhenTankBelowThreshold_AndHasAddersgall()
    {
        // Arrange: tank at 50% HP, threshold 0.55, 2 Addersgall stacks
        var module = new DefensiveModule();
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.TaurocholeThreshold = 0.55f;
        // Disable other defensives so Taurochole fires first
        config.Sage.EnableKerachole = false;

        var tank = MockBuilders.CreateMockBattleChara(
            entityId: 10u,
            currentHp: 50000,
            maxHp: 100000); // 50% HP

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns(tank.Object);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.75f, 0.50f, 1));

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Taurochole.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Taurochole.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var addersgallService = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 2);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgallService,
            addersgallStacks: 2,
            inCombat: true,
            canExecuteOgcd: true,
            level: 90);

        // Act
        var result = module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Taurochole.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    [Fact]
    public void Taurochole_DoesNotFire_WhenNoAddersgall()
    {
        // Arrange: tank at 50% HP, threshold 0.55, 0 stacks
        var module = new DefensiveModule();
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.TaurocholeThreshold = 0.55f;
        config.Sage.EnableKerachole = false;
        config.Sage.EnableHolos = false;
        config.Sage.EnablePanhaima = false;
        config.Sage.EnableHaima = false;

        var tank = MockBuilders.CreateMockBattleChara(
            entityId: 10u,
            currentHp: 50000,
            maxHp: 100000); // 50% HP

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns(tank.Object);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.75f, 0.50f, 1));

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Taurochole.ActionId)).Returns(true);

        var addersgallService = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 0);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgallService,
            addersgallStacks: 0,
            inCombat: true,
            canExecuteOgcd: true,
            level: 90);

        // Act
        var result = module.TryExecute(context, isMoving: false);

        // Assert
        Assert.False(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Taurochole.ActionId),
                It.IsAny<ulong>()),
            Times.Never);
    }

    #endregion

    #region Kerachole Tests

    [Fact]
    public void Kerachole_Fires_WhenPartyInjured()
    {
        // Arrange: 4 injured members (>= AoEHealMinTargets 3), 2 Addersgall stacks
        var module = new DefensiveModule();
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.AoEHealMinTargets = 3;
        config.Sage.KeracholeThreshold = 0.80f;
        // Disable higher-priority defensives that also need addersgall
        config.Sage.EnableTaurochole = false;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((Dalamud.Game.ClientState.Objects.Types.IBattleChara?)null);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.85f, 0.60f, 4)); // 4 injured — triggers AoE threshold

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Kerachole.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Kerachole.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var addersgallService = AsclepiusTestContext.CreateMockAddersgallService(currentStacks: 2);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            addersgallService: addersgallService,
            addersgallStacks: 2,
            inCombat: true,
            canExecuteOgcd: true,
            level: 90);

        // Act
        var result = module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Kerachole.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion

    #region Holos Tests

    [Fact]
    public void Holos_Fires_WhenAvgHpBelowThreshold()
    {
        // Arrange: avg HP 55%, threshold 0.60
        var module = new DefensiveModule();
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.HolosThreshold = 0.60f;
        // Disable higher-priority defensives
        config.Sage.EnableTaurochole = false;
        config.Sage.EnableKerachole = false;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((Dalamud.Game.ClientState.Objects.Types.IBattleChara?)null);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.55f, 0.40f, 2)); // avg 55% < 60% threshold

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Holos.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Holos.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            inCombat: true,
            canExecuteOgcd: true,
            level: 90);

        // Act
        var result = module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Holos.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion

    #region Panhaima Tests

    [Fact]
    public void Panhaima_Fires_WhenAvgHpBelowThreshold()
    {
        // Arrange: avg HP 80%, threshold 0.85
        var module = new DefensiveModule();
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.PanhaimaThreshold = 0.85f;
        // Disable higher-priority defensives
        config.Sage.EnableTaurochole = false;
        config.Sage.EnableKerachole = false;
        config.Sage.EnableHolos = false;

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper.Setup(x => x.FindTankInParty(It.IsAny<IPlayerCharacter>()))
            .Returns((Dalamud.Game.ClientState.Objects.Types.IBattleChara?)null);
        partyHelper.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.80f, 0.65f, 2)); // avg 80% < 85% threshold

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        actionService.Setup(x => x.IsActionReady(SGEActions.Panhaima.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Panhaima.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            inCombat: true,
            canExecuteOgcd: true,
            level: 90);

        // Act
        var result = module.TryExecute(context, isMoving: false);

        // Assert
        Assert.True(result);
        actionService.Verify(
            x => x.ExecuteOgcd(
                It.Is<ActionDefinition>(a => a.ActionId == SGEActions.Panhaima.ActionId),
                It.IsAny<ulong>()),
            Times.Once);
    }

    #endregion

}
