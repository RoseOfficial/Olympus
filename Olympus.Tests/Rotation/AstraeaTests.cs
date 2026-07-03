using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation;

/// <summary>
/// Integration tests for Astraea (Astrologian) rotation orchestrator.
/// Tests module priority ordering, debug state management, and execution flow.
/// </summary>
public class AstraeaTests
{
    #region Module Priority Tests

    [Fact]
    public void ModulePriorities_CardIsHighestPriority()
    {
        var card = new CardModule();
        var resurrection = new ResurrectionModule();
        var healing = new HealingModule();
        var defensive = new DefensiveModule();
        var buff = new BuffModule();
        var damage = new DamageModule();

        Assert.True(card.Priority < resurrection.Priority);
        Assert.True(card.Priority < healing.Priority);
        Assert.True(card.Priority < defensive.Priority);
        Assert.True(card.Priority < buff.Priority);
        Assert.True(card.Priority < damage.Priority);
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
        Assert.Equal(3, new CardModule().Priority);
        Assert.Equal(5, new ResurrectionModule().Priority);
        Assert.Equal(10, new HealingModule().Priority);
        Assert.Equal(20, new DefensiveModule().Priority);
        Assert.Equal(30, new BuffModule().Priority);
        Assert.Equal(50, new DamageModule().Priority);
    }

    [Fact]
    public void AllModules_HaveExpectedNames()
    {
        Assert.Equal("Card", new CardModule().Name);
        Assert.Equal("Resurrection", new ResurrectionModule().Name);
        Assert.Equal("Healing", new HealingModule().Name);
        Assert.Equal("Defensive", new DefensiveModule().Name);
        Assert.Equal("Buff", new BuffModule().Name);
        Assert.Equal("Damage", new DamageModule().Name);
    }

    #endregion

    #region DebugState Tests

    [Fact]
    public void AstraeaDebugState_DefaultValues_AreInitialized()
    {
        var debugState = new AstraeaDebugState();

        Assert.Equal("Idle", debugState.PlanningState);
        Assert.Equal("None", debugState.PlannedAction);
        Assert.Equal("Idle", debugState.DpsState);
        Assert.Equal("None", debugState.CurrentCardType);
        Assert.Equal("Idle", debugState.CardState);
        Assert.Equal("Idle", debugState.DrawState);
        Assert.Equal("Not Placed", debugState.EarthlyStarState);
    }

    [Fact]
    public void AstraeaDebugState_CanBeModified()
    {
        var debugState = new AstraeaDebugState
        {
            CurrentCardType = "The Balance",
            CardState = "Ready to play",
            SealCount = 3,
            UniqueSealCount = 3
        };

        Assert.Equal("The Balance", debugState.CurrentCardType);
        Assert.Equal("Ready to play", debugState.CardState);
        Assert.Equal(3, debugState.SealCount);
        Assert.Equal(3, debugState.UniqueSealCount);
    }

    [Fact]
    public void AstraeaDebugState_EarthlyStarFields_DefaultToIdle()
    {
        var debugState = new AstraeaDebugState();

        Assert.Equal("Not Placed", debugState.EarthlyStarState);
        Assert.Equal(0f, debugState.StarTimeRemaining);
        Assert.False(debugState.IsStarMature);
        Assert.Equal(0, debugState.StarTargetsInRange);
    }

    #endregion

    #region Context Tests

    [Fact]
    public void AstraeaContext_StoresPlayerReference()
    {
        var context = AstraeaTestContext.Create(level: 90);
        Assert.NotNull(context.Player);
        Assert.Equal((byte)90, context.Player.Level);
    }

    [Fact]
    public void AstraeaContext_TracksCombatState()
    {
        var context = AstraeaTestContext.Create(inCombat: true);
        Assert.True(context.InCombat);

        var context2 = AstraeaTestContext.Create(inCombat: false);
        Assert.False(context2.InCombat);
    }

    [Fact]
    public void AstraeaContext_TracksGcdState()
    {
        var context = AstraeaTestContext.Create(canExecuteGcd: true);
        Assert.True(context.CanExecuteGcd);

        var context2 = AstraeaTestContext.Create(canExecuteGcd: false);
        Assert.False(context2.CanExecuteGcd);
    }

    [Fact]
    public void AstraeaContext_HasDebugState()
    {
        var context = AstraeaTestContext.Create();
        Assert.NotNull(context.Debug);
    }

    [Fact]
    public void AstraeaContext_ConfigurationIsAccessible()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.BeneficThreshold = 0.45f;

        var context = AstraeaTestContext.Create(config: config);

        Assert.Equal(0.45f, context.Configuration.Astrologian.BeneficThreshold);
    }

    [Fact]
    public void AstraeaContext_CurrentCardIsAccessible()
    {
        var cardService = AstraeaTestContext.CreateMockCardService(
            hasCard: true,
            currentCard: Olympus.Data.ASTActions.CardType.TheBalance);

        var context = AstraeaTestContext.Create(cardService: cardService);

        Assert.Equal(Olympus.Data.ASTActions.CardType.TheBalance, context.CurrentCard);
    }

    [Fact]
    public void AstraeaContext_HasCardReflectsCardState()
    {
        var contextWithCard = AstraeaTestContext.Create(hasCard: true);
        Assert.True(contextWithCard.HasCard);

        var contextNoCard = AstraeaTestContext.Create(hasCard: false);
        Assert.False(contextNoCard.HasCard);
    }

    #endregion

    #region Module Integration Tests

    [Fact]
    public void DamageModule_CollectCandidates_NotInCombat_PushesNothing()
    {
        var module = new DamageModule();
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AstraeaTestContext.Create(
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
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableSingleTargetDamage = false;
        config.Astrologian.EnableAoEDamage = false;
        config.Astrologian.EnableDot = false;
        config.Astrologian.EnableOracle = false;
        config.Astrologian.EnableMinorArcana = false;

        var enemy = new Mock<IBattleNpc>();
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: true);
        var context = AstraeaTestContext.Create(
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
    public void DamageModule_CollectCandidates_SingleTargetEnabled_PushesFallMaleficAtPriority330()
    {
        // At level 90, GetDamageGcdForLevel returns FallMalefic (ActionId 25871, MinLevel 82).
        // Oracle blocked (HasDivining=false), LordOfCrowns blocked (HasLord=false by default),
        // DoT blocked (FindEnemyNeedingDot=null), AoE blocked (0 enemies < min).
        var module = new DamageModule();
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();

        var enemy = new Mock<IBattleNpc>();
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(),
            It.IsAny<float>(),
            It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AstraeaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 90,
            inCombat: true,
            canExecuteGcd: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService);
        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcdQueue = scheduler.InspectGcdQueue();
        var stCandidate = Assert.Single(gcdQueue, c => c.Behavior.Action.ActionId == ASTActions.FallMalefic.ActionId);
        Assert.Equal(330, stCandidate.Priority);
    }

    [Fact]
    public void HealingModule_CollectCandidates_HealingMasterDisabled_PushesNothing()
    {
        var module = new HealingModule();
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.EnableHealing = false;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        var context = AstraeaTestContext.Create(
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
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Resurrection.EnableRaise = false;

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 99u, currentHp: 0, maxHp: 50000, isDead: true);
        var partyHelper = new TestableAstraeaPartyHelper(new[] { deadMember.Object });
        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);

        var context = AstraeaTestContext.Create(
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
    public void ResurrectionModule_CollectCandidates_DeadMemberAndHardcastAllowed_PushesAscendAtPriorityOne()
    {
        // Swiftcast and Lightspeed both on cooldown; hardcast Ascend is the only raise path.
        // AST ShouldWaitForPreRaiseBuff checks lightspeedCooldown -- with 60f (> 10f), it returns false.
        var module = new ResurrectionModule();
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();

        var deadMember = MockBuilders.CreateMockBattleChara(entityId: 99u, currentHp: 0, maxHp: 50000, isDead: true);
        var partyHelper = new TestableAstraeaPartyHelper(new[] { deadMember.Object });

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(RoleActions.Swiftcast.ActionId)).Returns(false);
        actionService.Setup(x => x.GetCooldownRemaining(RoleActions.Swiftcast.ActionId)).Returns(60f);
        actionService.Setup(x => x.IsActionReady(ASTActions.Lightspeed.ActionId)).Returns(false);
        actionService.Setup(x => x.GetCooldownRemaining(ASTActions.Lightspeed.ActionId)).Returns(60f);

        var context = AstraeaTestContext.Create(
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
        var ascendCandidate = Assert.Single(gcdQueue, c => c.Behavior.Action.ActionId == RoleActions.Ascend.ActionId);
        Assert.Equal(1, ascendCandidate.Priority);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void Configuration_EnableCards_IsTrue_ByDefault()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        Assert.True(config.Astrologian.EnableCards);
    }

    [Fact]
    public void Configuration_EnableDivination_IsTrue_ByDefault()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        Assert.True(config.Astrologian.EnableDivination);
    }

    [Fact]
    public void Configuration_DefaultAstrologianConfiguration_HasHealingEnabled()
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        Assert.True(config.Astrologian.EnableBenefic);
        Assert.True(config.Astrologian.EnableBeneficII);
        Assert.True(config.Astrologian.EnableEssentialDignity);
    }

    #endregion
}
