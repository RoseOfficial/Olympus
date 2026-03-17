using Moq;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Athena (Scholar) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class AthenaTests
{
    #region Module Priority Tests

    [Fact]
    public void ModulePriorities_FairyIsHighestPriority()
    {
        var fairy = new FairyModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(fairy.Priority < resurrection.Priority);
        Assert.True(fairy.Priority < healing.Priority);
        Assert.True(fairy.Priority < defensive.Priority);
        Assert.True(fairy.Priority < buff.Priority);
        Assert.True(fairy.Priority < damage.Priority);
    }

    [Fact]
    public void ModulePriorities_ResurrectionBeforeHealing()
    {
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();

        Assert.True(resurrection.Priority < healing.Priority);
    }

    [Fact]
    public void ModulePriorities_HealingBeforeDefensive()
    {
        var healing = new HealingModule();
        var defensive = new DefensiveModule();

        Assert.True(healing.Priority < defensive.Priority);
    }

    [Fact]
    public void ModulePriorities_DefensiveBeforeBuff()
    {
        var defensive = new DefensiveModule();
        var buff = new BuffModule();

        Assert.True(defensive.Priority < buff.Priority);
    }

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
        Assert.Equal(3, new FairyModule().Priority);
        Assert.Equal(5, new ResurrectionModule().Priority);
        Assert.Equal(10, new HealingModule().Priority);
        Assert.Equal(20, new DefensiveModule().Priority);
        Assert.Equal(30, new BuffModule().Priority);
        Assert.Equal(50, new DamageModule().Priority);
    }

    [Fact]
    public void AllModules_HaveExpectedNames()
    {
        Assert.Equal("Fairy", new FairyModule().Name);
        Assert.Equal("Resurrection", new ResurrectionModule().Name);
        Assert.Equal("Healing", new HealingModule().Name);
        Assert.Equal("Defensive", new DefensiveModule().Name);
        Assert.Equal("Buff", new BuffModule().Name);
        Assert.Equal("Damage", new DamageModule().Name);
    }

    #endregion

    #region DebugState Tests

    [Fact]
    public void AthenaDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new AthenaDebugState();

        Assert.Equal("Idle", debugState.PlanningState);
        Assert.Equal("None", debugState.PlannedAction);
        Assert.Equal("Idle", debugState.DpsState);
        Assert.Equal("Idle", debugState.AetherflowState);
        Assert.Equal("None", debugState.FairyState);
        Assert.Equal("Idle", debugState.LustrateState);
        Assert.Equal("Idle", debugState.ChainStratagemState);
    }

    [Fact]
    public void AthenaDebugState_CanBeModified()
    {
        var debugState = new AthenaDebugState
        {
            AetherflowStacks = 3,
            AetherflowState = "3/3",
            FairyGauge = 100,
            FairyState = "Eos"
        };

        Assert.Equal(3, debugState.AetherflowStacks);
        Assert.Equal("3/3", debugState.AetherflowState);
        Assert.Equal(100, debugState.FairyGauge);
        Assert.Equal("Eos", debugState.FairyState);
    }

    [Fact]
    public void AthenaDebugState_ShieldFields_DefaultToIdle()
    {
        var debugState = new AthenaDebugState();

        Assert.Equal("Idle", debugState.ShieldState);
        Assert.Equal("Idle", debugState.DeploymentState);
        Assert.Equal("Idle", debugState.EmergencyTacticsState);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void AthenaContext_StoresPlayerReference()
    {
        var context = AthenaTestContext.Create(level: 100);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)100, context.Player.Level);
    }

    [Fact]
    public void AthenaContext_TracksCombatState()
    {
        var context = AthenaTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = AthenaTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void AthenaContext_TracksGcdState()
    {
        var context = AthenaTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = AthenaTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void AthenaContext_HasDebugState()
    {
        var context = AthenaTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void AthenaContext_ConfigurationIsAccessible()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.LustrateThreshold = 0.40f;

        var context = AthenaTestContext.Create(config: config);

        Assert.Equal(0.40f, context.Configuration.Scholar.LustrateThreshold);
    }

    [Fact]
    public void AthenaContext_AetherflowStacksIsAccessible()
    {
        var context = AthenaTestContext.Create(aetherflowStacks: 2);
        Assert.Equal(2, context.AetherflowStacks);
    }

    [Fact]
    public void AthenaContext_FairyGaugeIsAccessible()
    {
        var context = AthenaTestContext.Create(fairyGauge: 80);
        Assert.Equal(80, context.FairyGauge);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_ReturnsFalse_WhenNotInCombat()
    {
        var module = new DamageModule();
        var context = AthenaTestContext.Create(inCombat: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void DamageModule_ReturnsFalse_WhenDamageDisabled()
    {
        var module = new DamageModule();
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableBanefulImpaction = false;
        config.Scholar.EnableEnergyDrain = false;
        config.Scholar.EnableAetherflow = false;
        config.Scholar.EnableRuinII = false;

        var enemy = new Moq.Mock<Dalamud.Game.ClientState.Objects.Types.IBattleNpc>();
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var context = AthenaTestContext.Create(
            config: config,
            targetingService: targetingService,
            inCombat: true,
            canExecuteGcd: true);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void HealingModule_ReturnsFalse_WhenNoGcdOrOgcdAvailable()
    {
        var module = new HealingModule();
        var context = AthenaTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            canExecuteOgcd: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void ResurrectionModule_ReturnsFalse_WhenRaiseDisabled()
    {
        var module = new ResurrectionModule();
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Resurrection.EnableRaise = false;

        var context = AthenaTestContext.Create(
            config: config,
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    [Fact]
    public void FairyModule_ReturnsFalse_WhenNoOgcdAvailable()
    {
        var module = new FairyModule();
        var context = AthenaTestContext.Create(
            canExecuteOgcd: false,
            canExecuteGcd: false);

        var result = module.TryExecute(context, isMoving: false);

        Assert.False(result);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_EnableAetherflow_IsTrue_ByDefault()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        Assert.True(config.Scholar.EnableAetherflow);
    }

    [Fact]
    public void Configuration_EnableFairy_IsTrue_ByDefault()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        Assert.True(config.Scholar.AutoSummonFairy);
        Assert.True(config.Scholar.EnableFairyAbilities);
    }

    [Fact]
    public void Configuration_DefaultScholarConfiguration_HasHealingEnabled()
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        Assert.True(config.Scholar.EnableLustrate);
        Assert.True(config.Scholar.EnableExcogitation);
        Assert.True(config.Scholar.EnableIndomitability);
    }

    #endregion
}
