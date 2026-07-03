using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.EchidnaCore.Abilities;
using Olympus.Rotation.EchidnaCore.Context;
using Olympus.Rotation.EchidnaCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.EchidnaCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Echidna (Viper) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class EchidnaTests
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
    public void EchidnaDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new EchidnaDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void EchidnaDebugState_CanBeModified()
    {
        var debugState = new EchidnaDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Steel Fangs",
            DamageState = "Combo step 1",
            SerpentOffering = 50,
            IsReawakened = true
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Steel Fangs", debugState.PlannedAction);
        Assert.Equal("Combo step 1", debugState.DamageState);
        Assert.Equal(50, debugState.SerpentOffering);
        Assert.True(debugState.IsReawakened);
    }

    [Fact]
    public void EchidnaDebugState_GetReawakenState_ReturnsNotReawakened_WhenInactive()
    {
        var debugState = new EchidnaDebugState();
        Assert.Equal("Not Reawakened", debugState.GetReawakenState());
    }

    [Fact]
    public void EchidnaDebugState_GetGaugeState_ContainsOfferingsAndCoils()
    {
        var debugState = new EchidnaDebugState { SerpentOffering = 70, RattlingCoils = 2 };
        var state = debugState.GetGaugeState();
        Assert.Contains("Offerings: 70", state);
        Assert.Contains("Coils: 2", state);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void EchidnaContext_StoresPlayerReference()
    {
        var context = EchidnaTestContext.Create(level: 100);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)100, context.Player.Level);
    }

    [Fact]
    public void EchidnaContext_TracksCombatState()
    {
        var context = EchidnaTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = EchidnaTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void EchidnaContext_TracksGcdState()
    {
        var context = EchidnaTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = EchidnaTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void EchidnaContext_HasDebugState()
    {
        var context = EchidnaTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void EchidnaContext_ConfigurationIsAccessible()
    {
        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableReawaken = false;

        var context = EchidnaTestContext.Create(config: config);

        Assert.False(context.Configuration.Viper.EnableReawaken);
    }

    [Fact]
    public void EchidnaContext_SerpentOfferingIsAccessible()
    {
        var context = EchidnaTestContext.Create(serpentOffering: 70);
        Assert.Equal(70, context.SerpentOffering);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_PushesNothing_WhenNotInCombat()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = EchidnaTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void DamageModule_CollectCandidates_PushesNothing_WhenNoTarget()
    {
        // Default targeting mock returns null for FindEnemyForAction.
        // The module gates all pushes behind the target check.
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = EchidnaTestContext.Create(inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_PushesNothing_WhenNotInCombat()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = EchidnaTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_PushesSerpentsIre_AtPriority1_WhenConditionsMet()
    {
        // Gate conditions: EnableSerpentsIre (default true), level >= 86, IsActionReady,
        // !ShouldHoldForPhaseTransition (null timeline), !ShouldHoldForBurst (null service),
        // HasNoxiousGnash, HasHuntersInstinct, HasSwiftscaled.
        // BuffModule uses player.GameObjectId as target; no enemy target needed.
        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            actionService: actionService,
            level: 100,
            inCombat: true,
            hasNoxiousGnash: true,
            hasHuntersInstinct: true,
            hasSwiftscaled: true);

        var module = new BuffModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == EchidnaAbilities.SerpentsIre && c.Priority == 1);
    }

    [Fact]
    public void BuffModule_CollectCandidates_DoesNotPushSerpentsIre_WhenToggleDisabled()
    {
        // EnableSerpentsIre = false triggers the module guard inside TryPushSerpentsIre.
        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableSerpentsIre = false;

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            hasNoxiousGnash: true,
            hasHuntersInstinct: true,
            hasSwiftscaled: true);

        var module = new BuffModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == EchidnaAbilities.SerpentsIre);
    }

    [Fact]
    public void BuffModule_CollectCandidates_DoesNotPushSerpentsIre_WhenHuntersInstinctMissing()
    {
        // The buff-gate blocks when HasHuntersInstinct is false, even if HasNoxiousGnash passes.
        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            actionService: actionService,
            level: 100,
            inCombat: true,
            hasNoxiousGnash: true,
            hasHuntersInstinct: false,
            hasSwiftscaled: true);

        var module = new BuffModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == EchidnaAbilities.SerpentsIre);
    }

    [Fact]
    public void DamageModule_CollectCandidates_PushesSteelFangs_AtPriority5_WhenNoCombo()
    {
        // With ComboStep = 0 and HasHuntersInstinct = false, GetStarterAction returns SteelFangs.
        // SteelFangs is pushed as the ST dual-wield combo starter at GCD priority 5.
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            level: 100,
            inCombat: true,
            comboStep: 0,
            hasHuntersInstinct: false);

        var module = new DamageModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == EchidnaAbilities.SteelFangs && c.Priority == 5);
    }

    [Fact]
    public void DamageModule_CollectCandidates_DoesNotPushReawaken_WhenToggleDisabled()
    {
        // EnableReawaken = false triggers the module guard inside TryPushReawaken.
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var config = EchidnaTestContext.CreateDefaultViperConfiguration();
        config.Viper.EnableReawaken = false;

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = EchidnaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 100,
            inCombat: true);

        var module = new DamageModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == EchidnaAbilities.Reawaken);
    }

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_ReawakenEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Viper.EnableReawaken);
    }

    [Fact]
    public void Configuration_DefaultRattlingCoilMinStacks_Is1()
    {
        var config = new Configuration();
        Assert.Equal(1, config.Viper.RattlingCoilMinStacks);
    }

    #endregion
}
