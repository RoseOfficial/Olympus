using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Tests.Rotation.ApolloCore;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Rotation.Common;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Tests.Mocks;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Apollo rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class ApolloTests
{
    #region Module Priority Tests

    [Fact]
    public void ModulePriorities_ResurrectionIsHighestPriority()
    {
        // Resurrection should have the highest priority (lowest number)
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(resurrection.Priority < healing.Priority);
        Assert.True(resurrection.Priority < defensive.Priority);
        Assert.True(resurrection.Priority < buff.Priority);
        Assert.True(resurrection.Priority < damage.Priority);
    }

    [Fact]
    public void ModulePriorities_HealingBeforeDefensive()
    {
        var healing = new HealingModule();
        var defensive = new DefensiveModule();

        Assert.True(healing.Priority < defensive.Priority);
    }

    [Fact]
    public void ModulePriorities_DefensiveBeforeBuffs()
    {
        var defensive = new DefensiveModule();
        var buff = new BuffModule();

        Assert.True(defensive.Priority < buff.Priority);
    }

    [Fact]
    public void ModulePriorities_BuffsBeforeDamage()
    {
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(buff.Priority < damage.Priority);
    }

    [Fact]
    public void ModulePriorities_DamageIsLowestPriority()
    {
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(damage.Priority > resurrection.Priority);
        Assert.True(damage.Priority > healing.Priority);
        Assert.True(damage.Priority > defensive.Priority);
        Assert.True(damage.Priority > buff.Priority);
    }

    [Fact]
    public void AllModules_HaveCorrectExpectedPriorities()
    {
        // Document the expected priority values
        Assert.Equal(5, new ResurrectionModule().Priority);
        Assert.Equal(10, new HealingModule().Priority);
        Assert.Equal(20, new DefensiveModule().Priority);
        Assert.Equal(30, new BuffModule().Priority);
        Assert.Equal(50, new DamageModule().Priority);
    }

    [Fact]
    public void AllModules_HaveExpectedNames()
    {
        Assert.Equal("Resurrection", new ResurrectionModule().Name);
        Assert.Equal("Healing", new HealingModule().Name);
        Assert.Equal("Defensive", new DefensiveModule().Name);
        Assert.Equal("Buffs", new BuffModule().Name);
        Assert.Equal("Damage", new DamageModule().Name);
    }

    #endregion

    #region ApolloContext Tests

    [Fact]
    public void ApolloContext_StoresPlayerReference()
    {
        var player = MockBuilders.CreateMockPlayerCharacter(level: 90);
        var context = ApolloTestContext.Create(player: player.Object);

        Assert.Same(player.Object, context.Player);
    }

    [Fact]
    public void ApolloContext_TracksMovementState()
    {
        var context = ApolloTestContext.Create(isMoving: true);
        Assert.True(context.IsMoving);

        var context2 = ApolloTestContext.Create(isMoving: false);
        Assert.False(context2.IsMoving);
    }

    [Fact]
    public void ApolloContext_TracksCombatState()
    {
        var context = ApolloTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = ApolloTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void ApolloContext_TracksGcdState()
    {
        var context = ApolloTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = ApolloTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void ApolloContext_TracksOgcdState()
    {
        var context = ApolloTestContext.Create(canExecuteOgcd: true);
        Assert.True(context.CanExecuteOgcd);

        var context2 = ApolloTestContext.Create(canExecuteOgcd: false);
        Assert.False(context2.CanExecuteOgcd);
    }

    [Fact]
    public void ApolloContext_HasDebugState()
    {
        var context = ApolloTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void ApolloContext_SharesDebugState()
    {
        var debugState = new DebugState { PlanningState = "Test" };
        var context = ApolloTestContext.Create(debugState: debugState);

        Assert.Same(debugState, context.Debug);
        Assert.Equal("Test", context.Debug.PlanningState);
    }

    [Fact]
    public void ApolloContext_ConfigurationIsAccessible()
    {
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.BenedictionEmergencyThreshold = 0.25f;

        var context = ApolloTestContext.Create(config: config);

        Assert.Equal(0.25f, context.Configuration.Healing.BenedictionEmergencyThreshold);
    }

    [Fact]
    public void ApolloContext_ServicesAreAccessible()
    {
        var context = ApolloTestContext.Create();

        Assert.NotNull(context.ActionService);
        Assert.NotNull(context.HealingSpellSelector);
        Assert.NotNull(context.HpPredictionService);
        Assert.NotNull(context.PartyHelper);
        Assert.NotNull(context.StatusHelper);
        Assert.NotNull(context.TargetingService);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void HealingModule_ReturnsTrue_WhenSuccessfullyHeals()
    {
        var module = new HealingModule();
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.EnableBenediction = true;
        config.Healing.BenedictionEmergencyThreshold = 0.30f;

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

        var context = ApolloTestContext.Create(
            config: config,
            partyHelper: partyHelperMock,
            actionService: actionServiceMock,
            level: 90,
            canExecuteOgcd: true);

        var result = module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionServiceMock.Verify(x => x.ExecuteOgcd(
            It.Is<ActionDefinition>(a => a.ActionId == WHMActions.Benediction.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenDamageDisabled()
    {
        var module = new DamageModule();
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableDamage = false;

        var context = ApolloTestContext.Create(config: config, inCombat: true);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var config = MockBuilders.CreateDefaultConfiguration();
        config.EnableDamage = true;

        var context = ApolloTestContext.Create(config: config, inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void ResurrectionModule_ReturnsFalse_WhenNoDeadMembers()
    {
        var module = new ResurrectionModule();
        var config = MockBuilders.CreateDefaultConfiguration();

        var partyHelperMock = MockBuilders.CreateMockPartyHelper(deadMember: null);

        var context = ApolloTestContext.Create(
            config: config,
            partyHelper: partyHelperMock,
            inCombat: true);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DefensiveModule_ReturnsFalse_WhenNoDefensivesNeeded()
    {
        var module = new DefensiveModule();
        var config = MockBuilders.CreateDefaultConfiguration();

        // All party members at full HP
        var partyHelperMock = MockBuilders.CreateMockPartyHelper();
        partyHelperMock.Setup(x => x.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((1.0f, 1.0f, 0));

        var context = ApolloTestContext.Create(
            config: config,
            partyHelper: partyHelperMock,
            canExecuteOgcd: true);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void BuffModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new BuffModule();
        var config = MockBuilders.CreateDefaultConfiguration();

        var context = ApolloTestContext.Create(config: config, inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_DefaultBenedictionThreshold_Is30Percent()
    {
        var config = new Configuration();
        Assert.Equal(0.30f, config.Healing.BenedictionEmergencyThreshold);
    }

    [Fact]
    public void Configuration_DefaultEmergencyThresholds()
    {
        var config = new Configuration();
        Assert.Equal(0.50f, config.Healing.OgcdEmergencyThreshold);
        Assert.Equal(0.40f, config.Healing.GcdEmergencyThreshold);
    }

    [Fact]
    public void Configuration_AllSpellsEnabledByDefault()
    {
        var config = new Configuration();

        // Healing spells
        Assert.True(config.Healing.EnableCure);
        Assert.True(config.Healing.EnableCureII);
        Assert.True(config.Healing.EnableCureIII);
        Assert.True(config.Healing.EnableRegen);
        Assert.True(config.Healing.EnableMedica);
        Assert.True(config.Healing.EnableMedicaII);
        Assert.True(config.Healing.EnableAfflatusSolace);
        Assert.True(config.Healing.EnableAfflatusRapture);
        Assert.True(config.Healing.EnableTetragrammaton);
        Assert.True(config.Healing.EnableBenediction);
        Assert.True(config.Healing.EnableAssize);
        Assert.True(config.Healing.EnableAsylum);
    }

    [Fact]
    public void Configuration_MasterSwitchesEnabledByDefault()
    {
        var config = new Configuration();

        Assert.True(config.EnableHealing);
        Assert.True(config.EnableDamage);
        Assert.True(config.EnableDoT);
    }

    [Fact]
    public void Configuration_AoEHealMinTargets_Default()
    {
        var config = new Configuration();
        Assert.Equal(3, config.Healing.AoEHealMinTargets);
    }

    [Fact]
    public void Configuration_DefensiveCooldownThreshold_Default()
    {
        var config = new Configuration();
        Assert.Equal(0.80f, config.Defensive.DefensiveCooldownThreshold);
    }

    #endregion

}
