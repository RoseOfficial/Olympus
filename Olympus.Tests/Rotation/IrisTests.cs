using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.IrisCore.Context;
using Olympus.Rotation.IrisCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.IrisCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Iris (Pictomancer) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class IrisTests
{
    #region Module Priority Tests

    [Fact]
    public void ModulePriorities_BuffBeforeDamage()
    {
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(buff.Priority < damage.Priority);
    }

    [Fact]
    public void AllModules_HaveCorrectExpectedPriorities()
    {
        // Iris (PCT) has different priorities: Buff=30, Damage=50
        Assert.Equal(30, new BuffModule().Priority);
        Assert.Equal(50, new DamageModule().Priority);
    }

    [Fact]
    public void AllModules_HaveExpectedNames()
    {
        Assert.Equal("Buff", new BuffModule().Name);
        Assert.Equal("Damage", new DamageModule().Name);
    }

    #endregion

    #region DebugState Tests

    [Fact]
    public void IrisDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new IrisDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void IrisDebugState_CanBeModified()
    {
        var debugState = new IrisDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Holy in White",
            DamageState = "Paint spender",
            PaletteGauge = 75,
            HasCreatureCanvas = true,
            IsInHammerCombo = false
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Holy in White", debugState.PlannedAction);
        Assert.Equal("Paint spender", debugState.DamageState);
        Assert.Equal(75, debugState.PaletteGauge);
        Assert.True(debugState.HasCreatureCanvas);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void IrisContext_StoresPlayerReference()
    {
        var context = IrisTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void IrisContext_TracksCombatState()
    {
        var context = IrisTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = IrisTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void IrisContext_TracksGcdState()
    {
        var context = IrisTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = IrisTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void IrisContext_HasDebugState()
    {
        var context = IrisTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void IrisContext_ConfigurationIsAccessible()
    {
        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.HolyMinPalette = 75;

        var context = IrisTestContext.Create(config: config);

        Assert.Equal(75, context.Configuration.Pictomancer.HolyMinPalette);
    }

    [Fact]
    public void IrisContext_PaletteGaugeIsAccessible()
    {
        var context = IrisTestContext.Create(paletteGauge: 80, canUseSubtractivePalette: true);
        Assert.Equal(80, context.PaletteGauge);
        Assert.True(context.CanUseSubtractivePalette);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        // PCT's DamageModule may try prepaint when not in combat.
        // With all motif needs set to false (default), prepaint does nothing and returns false.
        var module = new DamageModule();
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = IrisTestContext.Create(
            inCombat: false,
            canExecuteGcd: true,
            actionService: actionService,
            needsCreatureMotif: false,
            needsWeaponMotif: false,
            needsLandscapeMotif: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
        actionService.Verify(x => x.ExecuteGcd(It.IsAny<Olympus.Models.Action.ActionDefinition>(), It.IsAny<ulong>()), Times.Never);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenGcdNotReady()
    {
        var module = new DamageModule();

        var enemy = new Mock<IBattleNpc>();
        enemy.Setup(x => x.GameObjectId).Returns(99999UL);
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targetingService.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);

        var context = IrisTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: false,
            targetingService: targetingService);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void BuffModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new BuffModule();
        var context = IrisTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_StarryMuseEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Pictomancer.EnableStarryMuse);
    }

    [Fact]
    public void Configuration_HolyMinPalette_DefaultIs50()
    {
        var config = new Configuration();
        Assert.Equal(50, config.Pictomancer.HolyMinPalette);
    }

    #endregion
}
