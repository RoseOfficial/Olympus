using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
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
    public void DamageModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new DamageModule();
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AthenaTestContext.Create(
            actionService: actionService,
            inCombat: false,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void DamageModule_CollectCandidates_AllDamageDisabled_PushesNothing()
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

        var enemy = new Mock<IBattleNpc>();
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: true);
        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            inCombat: true,
            canExecuteGcd: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void DamageModule_CollectCandidates_ChainStratagemEnabled_PushesAtPriority285()
    {
        // ChainStratagem is an oGCD at priority 285. BanefulImpaction blocked (no ImpactImminent
        // status on player with null StatusList). EnergyDrain may also push (3 stacks, balanced
        // strategy, empty party avg HP 1.0 > 0.8) at priority 290 -- that doesn't affect this assertion.
        var module = new DamageModule();
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();

        var enemy = new Mock<IBattleNpc>();
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true);
        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 100,
            inCombat: true,
            canExecuteOgcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcdQueue = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcdQueue,
            c => c.Behavior.Action.ActionId == SCHActions.ChainStratagem.ActionId && c.Priority == 285);
    }

    [Fact]
    public void HealingModule_CollectCandidates_HealingMasterDisabled_PushesNothing()
    {
        var module = new HealingModule();
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.EnableHealing = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            inCombat: true,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void ResurrectionModule_CollectCandidates_RaiseDisabled_PushesNothing()
    {
        // EnableRaise=false is the master toggle; even with a dead member present, nothing pushes.
        var module = new ResurrectionModule();
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Resurrection.EnableRaise = false;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 99u, currentHp: 0, maxHp: 50000, isDead: true);
        var partyHelper = new TestableAthenaPartyHelper(new[] { deadMember.Object });
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            inCombat: true,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void ResurrectionModule_CollectCandidates_DeadMemberAndHardcastAllowed_PushesResurrectionAtPriorityOne()
    {
        // Swiftcast on cooldown (60s) forces the hardcast path.
        // SCH ShouldWaitForPreRaiseBuff always returns false (base class default -- no pre-raise buff for SCH).
        var module = new ResurrectionModule();
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 99u, currentHp: 0, maxHp: 50000, isDead: true);
        var partyHelper = new TestableAthenaPartyHelper(new[] { deadMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(false);
        actionService.Setup(x => x.GetCooldownRemaining(RoleActions.Swiftcast.ActionId)).Returns(60f);

        var context = AthenaTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 100,
            currentMp: 10000,
            inCombat: true,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcdQueue = scheduler.InspectGcdQueue();
        var resCandidate = Assert.Single(gcdQueue, c => c.Behavior.Action.ActionId == RoleActions.Resurrection.ActionId);
        Assert.Equal(1, resCandidate.Priority);
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
