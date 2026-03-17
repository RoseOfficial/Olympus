using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.HecateCore.Context;
using Olympus.Rotation.HecateCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.HecateCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Hecate (Black Mage) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class HecateTests
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
        Assert.Equal(20, new BuffModule().Priority);
        Assert.Equal(30, new DamageModule().Priority);
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
    public void HecateDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new HecateDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void HecateDebugState_CanBeModified()
    {
        var debugState = new HecateDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Fire IV",
            DamageState = "Astral Fire phase",
            InAstralFire = true,
            ElementStacks = 3,
            UmbralHearts = 3,
            PolyglotStacks = 1
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Fire IV", debugState.PlannedAction);
        Assert.Equal("Astral Fire phase", debugState.DamageState);
        Assert.True(debugState.InAstralFire);
        Assert.Equal(3, debugState.ElementStacks);
        Assert.Equal(3, debugState.UmbralHearts);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void HecateContext_StoresPlayerReference()
    {
        var context = HecateTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void HecateContext_TracksCombatState()
    {
        var context = HecateTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = HecateTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void HecateContext_TracksGcdState()
    {
        var context = HecateTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = HecateTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void HecateContext_HasDebugState()
    {
        var context = HecateTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void HecateContext_ConfigurationIsAccessible()
    {
        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.BlackMage.FireIVsBeforeDespair = 3;

        var context = HecateTestContext.Create(config: config);

        Assert.Equal(3, context.Configuration.BlackMage.FireIVsBeforeDespair);
    }

    [Fact]
    public void HecateContext_ElementStateIsAccessible()
    {
        var context = HecateTestContext.Create(inAstralFire: true, astralFireStacks: 3, elementStacks: 3);
        Assert.True(context.InAstralFire);
        Assert.Equal(3, context.AstralFireStacks);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = HecateTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
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

        var context = HecateTestContext.Create(
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
        var context = HecateTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_LeyLinesEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.BlackMage.EnableLeyLines);
    }

    [Fact]
    public void Configuration_FireIVsBeforeDespair_DefaultIs4()
    {
        var config = new Configuration();
        Assert.Equal(4, config.BlackMage.FireIVsBeforeDespair);
    }

    #endregion
}
