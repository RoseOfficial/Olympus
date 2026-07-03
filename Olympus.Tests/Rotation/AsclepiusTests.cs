using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Asclepius (Sage) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class AsclepiusTests
{
    #region Module Priority Tests

    [Fact]
    public void ModulePriorities_KardiaIsHighestPriority()
    {
        var kardia = new KardiaModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var damage = new DamageModule();

        Assert.True(kardia.Priority < resurrection.Priority);
        Assert.True(kardia.Priority < healing.Priority);
        Assert.True(kardia.Priority < defensive.Priority);
        Assert.True(kardia.Priority < damage.Priority);
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
    public void ModulePriorities_DefensiveBeforeDamage()
    {
        var defensive = new DefensiveModule();
        var damage = new DamageModule();

        Assert.True(defensive.Priority < damage.Priority);
    }

    [Fact]
    public void AllModules_HaveCorrectExpectedPriorities()
    {
        Assert.Equal(3, new KardiaModule().Priority);
        Assert.Equal(5, new ResurrectionModule().Priority);
        Assert.Equal(10, new HealingModule().Priority);
        Assert.Equal(20, new DefensiveModule().Priority);
        Assert.Equal(50, new DamageModule().Priority);
    }

    [Fact]
    public void AllModules_HaveExpectedNames()
    {
        Assert.Equal("Kardia", new KardiaModule().Name);
        Assert.Equal("Resurrection", new ResurrectionModule().Name);
        Assert.Equal("Healing", new HealingModule().Name);
        Assert.Equal("Defensive", new DefensiveModule().Name);
        Assert.Equal("Damage", new DamageModule().Name);
    }

    #endregion

    #region DebugState Tests

    [Fact]
    public void AsclepiusDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new AsclepiusDebugState();

        Assert.Equal("Idle", debugState.PlanningState);
        Assert.Equal("None", debugState.PlannedAction);
        Assert.Equal("Idle", debugState.DpsState);
        Assert.Equal("Idle", debugState.KardiaState);
        Assert.Equal("None", debugState.KardiaTarget);
        Assert.Equal("Idle", debugState.EukrasiaState);
        Assert.Equal("Idle", debugState.DoTState);
    }

    [Fact]
    public void AsclepiusDebugState_CanBeModified()
    {
        var debugState = new AsclepiusDebugState
        {
            KardiaState = "Active",
            KardiaTarget = "Tank",
            EukrasiaActive = true,
            AddersgallStacks = 3
        };

        Assert.Equal("Active", debugState.KardiaState);
        Assert.Equal("Tank", debugState.KardiaTarget);
        Assert.True(debugState.EukrasiaActive);
        Assert.Equal(3, debugState.AddersgallStacks);
    }

    [Fact]
    public void AsclepiusDebugState_ResourceFields_DefaultToZero()
    {
        var debugState = new AsclepiusDebugState();

        Assert.Equal(0, debugState.AddersgallStacks);
        Assert.Equal(0f, debugState.AddersgallTimer);
        Assert.Equal(0, debugState.AdderstingStacks);
        Assert.Equal(0, debugState.SoteriaStacks);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void AsclepiusContext_StoresPlayerReference()
    {
        var context = AsclepiusTestContext.Create(level: 90);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)90, context.Player.Level);
    }

    [Fact]
    public void AsclepiusContext_TracksCombatState()
    {
        var context = AsclepiusTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = AsclepiusTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void AsclepiusContext_TracksGcdState()
    {
        var context = AsclepiusTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = AsclepiusTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void AsclepiusContext_HasDebugState()
    {
        var context = AsclepiusTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void AsclepiusContext_ConfigurationIsAccessible()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.DruocholeThreshold = 0.50f;

        var context = AsclepiusTestContext.Create(config: config);

        Assert.Equal(0.50f, context.Configuration.Sage.DruocholeThreshold);
    }

    [Fact]
    public void AsclepiusContext_AddersgallCountIsAccessible()
    {
        var context = AsclepiusTestContext.Create(addersgallStacks: 2);
        Assert.Equal(2, context.AddersgallStacks);
    }

    [Fact]
    public void AsclepiusContext_AdderstingCountIsAccessible()
    {
        var context = AsclepiusTestContext.Create(adderstingStacks: 1);
        Assert.Equal(1, context.AdderstingStacks);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new DamageModule();
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AsclepiusTestContext.Create(
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
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableDamage = false;
        config.EnableDoT = false;
        config.Sage.EnablePhlegma = false;
        config.Sage.EnablePsyche = false;
        config.Sage.EnableToxikon = false;

        var enemy = new Mock<IBattleNpc>();
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: true);
        var context = AsclepiusTestContext.Create(
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
    public void DamageModule_CollectCandidates_SingleTargetEnabled_PushesDosisIIIAtPriority330()
    {
        // At level 90, GetDamageGcdForLevel returns DosisIII (ActionId 24312, MinLevel 82).
        // Psyche blocked (MinLevel 92 > 90), Phlegma blocked (0 charges by default),
        // DoT blocked (HasEukrasia=false), AoE blocked (0 enemies < min).
        var module = new DamageModule();
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();

        var enemy = new Mock<IBattleNpc>();
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 90,
            inCombat: true,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcdQueue = scheduler.InspectGcdQueue();
        var stCandidate = Assert.Single(gcdQueue, c => c.Behavior.Action.ActionId == SGEActions.DosisIII.ActionId);
        Assert.Equal(330, stCandidate.Priority);
    }

    [Fact]
    public void HealingModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new HealingModule();
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AsclepiusTestContext.Create(
            actionService: actionService,
            inCombat: false,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
    }

    [Fact]
    public void HealingModule_CollectCandidates_HealingMasterDisabled_PushesNothing()
    {
        var module = new HealingModule();
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.EnableHealing = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AsclepiusTestContext.Create(
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
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Resurrection.EnableRaise = false;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 99u, currentHp: 0, maxHp: 50000);
        var partyHelper = MockBuilders.CreateMockPartyHelper(deadMember: deadMember.Object);
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AsclepiusTestContext.Create(
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
    public void ResurrectionModule_CollectCandidates_DeadMemberAndHardcastAllowed_PushesEgeiroAtPriorityOne()
    {
        // Swiftcast on cooldown (60s) forces the hardcast path.
        // SGE ShouldWaitForPreRaiseBuff always returns false, so hardcast proceeds immediately.
        var module = new ResurrectionModule();
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 99u, currentHp: 0, maxHp: 50000);
        var partyHelper = MockBuilders.CreateMockPartyHelper(deadMember: deadMember.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(false);
        actionService.Setup(x => x.GetCooldownRemaining(RoleActions.Swiftcast.ActionId)).Returns(60f);

        var context = AsclepiusTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            actionService: actionService,
            level: 90,
            currentMp: 10000,
            inCombat: true,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcdQueue = scheduler.InspectGcdQueue();
        var egeiroCandidate = Assert.Single(gcdQueue, c => c.Behavior.Action.ActionId == RoleActions.Egeiro.ActionId);
        Assert.Equal(1, egeiroCandidate.Priority);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_EnableKardia_IsTrue_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Sage.AutoKardia);
    }

    [Fact]
    public void Configuration_EnableAddersgallHealing_IsTrue_ByDefault()
    {
        var config = new Configuration();
        Assert.True(config.Sage.EnableDruochole);
    }

    [Fact]
    public void Configuration_DefaultSageConfiguration_HasKardiaAndHealingEnabled()
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        Assert.True(config.Sage.AutoKardia);
        Assert.True(config.Sage.EnableDruochole);
        Assert.True(config.Sage.EnableTaurochole);
        Assert.True(config.Sage.EnableIxochole);
    }

    #endregion
}
