using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Rotation.CalliopeCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.CalliopeCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Calliope (Bard) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class CalliopeTests
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
    public void CalliopeDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new CalliopeDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void CalliopeDebugState_CanBeModified()
    {
        var debugState = new CalliopeDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Burst Shot",
            DamageState = "Filler GCD",
            SoulVoice = 80,
            Repertoire = 2
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Burst Shot", debugState.PlannedAction);
        Assert.Equal("Filler GCD", debugState.DamageState);
        Assert.Equal(80, debugState.SoulVoice);
        Assert.Equal(2, debugState.Repertoire);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void CalliopeContext_StoresPlayerReference()
    {
        var context = CalliopeTestContext.Create(level: 95);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)95, context.Player.Level);
    }

    [Fact]
    public void CalliopeContext_TracksCombatState()
    {
        var context = CalliopeTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = CalliopeTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void CalliopeContext_TracksGcdState()
    {
        var context = CalliopeTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = CalliopeTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void CalliopeContext_HasDebugState()
    {
        var context = CalliopeTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void CalliopeContext_ConfigurationIsAccessible()
    {
        var config = CalliopeTestContext.CreateDefaultBardConfiguration();
        config.Bard.ApexArrowMinGauge = 90;

        var context = CalliopeTestContext.Create(config: config);

        Assert.Equal(90, context.Configuration.Bard.ApexArrowMinGauge);
    }

    [Fact]
    public void CalliopeContext_SoulVoiceIsAccessible()
    {
        var context = CalliopeTestContext.Create(soulVoice: 75);
        Assert.Equal(75, context.SoulVoice);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = CalliopeTestContext.Create(inCombat: false);

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

        var context = CalliopeTestContext.Create(
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
        var context = CalliopeTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_AoERotationEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Bard.EnableAoERotation);
    }

    [Fact]
    public void Configuration_ApexArrowMinGauge_DefaultIs80()
    {
        var config = new Configuration();
        Assert.Equal(80, config.Bard.ApexArrowMinGauge);
    }

    #endregion
}
