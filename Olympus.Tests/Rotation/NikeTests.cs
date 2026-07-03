using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.NikeCore.Abilities;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Rotation.NikeCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.NikeCore;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Nike (Samurai) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class NikeTests
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
    public void NikeDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new NikeDebugState();

        Assert.Equal("", debugState.PlanningState);
        Assert.Equal("", debugState.PlannedAction);
        Assert.Equal("", debugState.DamageState);
        Assert.Equal("", debugState.BuffState);
    }

    [Fact]
    public void NikeDebugState_CanBeModified()
    {
        var debugState = new NikeDebugState
        {
            PlanningState = "Executing",
            PlannedAction = "Hakaze",
            DamageState = "Combo step 1",
            Kenki = 50,
            HasMeikyoShisui = true
        };

        Assert.Equal("Executing", debugState.PlanningState);
        Assert.Equal("Hakaze", debugState.PlannedAction);
        Assert.Equal("Combo step 1", debugState.DamageState);
        Assert.Equal(50, debugState.Kenki);
        Assert.True(debugState.HasMeikyoShisui);
    }

    [Fact]
    public void NikeDebugState_GetGaugeSummary_ContainsKenki()
    {
        var debugState = new NikeDebugState { Kenki = 75, Meditation = 3 };
        var summary = debugState.GetGaugeSummary();
        Assert.Contains("Kenki:75", summary);
        Assert.Contains("Med:3", summary);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void NikeContext_StoresPlayerReference()
    {
        var context = NikeTestContext.Create(level: 90);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)90, context.Player.Level);
    }

    [Fact]
    public void NikeContext_TracksCombatState()
    {
        var context = NikeTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = NikeTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void NikeContext_TracksGcdState()
    {
        var context = NikeTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = NikeTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void NikeContext_HasDebugState()
    {
        var context = NikeTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void NikeContext_ConfigurationIsAccessible()
    {
        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.EnableMeikyoShisui = false;

        var context = NikeTestContext.Create(config: config);

        Assert.False(context.Configuration.Samurai.EnableMeikyoShisui);
    }

    [Fact]
    public void NikeContext_KenkiIsAccessible()
    {
        var context = NikeTestContext.Create(kenki: 75);
        Assert.Equal(75, context.Kenki);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_PushesNothing_WhenNotInCombat()
    {
        var module = new DamageModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = NikeTestContext.Create(inCombat: false);

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
        var context = NikeTestContext.Create(inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_PushesNothing_WhenNotInCombat()
    {
        var module = new BuffModule();
        var scheduler = SchedulerFactory.CreateForTest();
        var context = NikeTestContext.Create(inCombat: false);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void BuffModule_CollectCandidates_PushesIkishoten_AtPriority2_WhenEnabled()
    {
        // Gate conditions: level >= 68, kenki <= 50, !HasOgiNamikiriReady, IsActionReady.
        // EnableIkishoten defaults to true; burst pooling is a no-op without a service.
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            level: 100,
            inCombat: true,
            kenki: 0,
            hasOgiNamikiriReady: false);

        var module = new BuffModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == NikeAbilities.Ikishoten && c.Priority == 2);
    }

    [Fact]
    public void BuffModule_CollectCandidates_DoesNotPushIkishoten_WhenToggleDisabled()
    {
        // EnableIkishoten = false triggers the module guard inside TryPushIkishoten.
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.EnableIkishoten = false;

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 100,
            inCombat: true,
            kenki: 0);

        var module = new BuffModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == NikeAbilities.Ikishoten);
    }

    [Fact]
    public void DamageModule_CollectCandidates_PushesKaeshiNamikiri_AtPriority1_WhenReady()
    {
        // KaeshiNamikiri: level >= 90, HasKaeshiNamikiriReady, IsActionReady -> GCD priority 1.
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            level: 100,
            inCombat: true,
            hasKaeshiNamikiriReady: true);

        var module = new DamageModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == NikeAbilities.KaeshiNamikiri && c.Priority == 1);
    }

    [Fact]
    public void DamageModule_CollectCandidates_DoesNotPushKaeshiNamikiri_WhenBuffMissing()
    {
        // HasKaeshiNamikiriReady = false (default) means the proc gate blocks the push.
        var enemy = CreateMockEnemy();
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = NikeTestContext.Create(
            actionService: actionService,
            targetingService: targeting,
            level: 100,
            inCombat: true,
            hasKaeshiNamikiriReady: false);

        var module = new DamageModule();
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == NikeAbilities.KaeshiNamikiri);
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
    public void Configuration_MeikyoShisuiEnabled_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Samurai.EnableMeikyoShisui);
    }

    [Fact]
    public void Configuration_DefaultKenkiMinGauge_Is25()
    {
        var config = new Configuration();
        Assert.Equal(25, config.Samurai.KenkiMinGauge);
    }

    #endregion
}
