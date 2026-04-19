using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.HecateCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Timeline;
using Olympus.Timeline.Models;

namespace Olympus.Tests.Rotation.HecateCore.Modules;

public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    [Fact]
    public void TryExecute_NotInCombat_ReturnsFalse()
    {
        var context = HecateTestContext.Create(inCombat: false, canExecuteGcd: true);
        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_GcdNotReady_ReturnsFalse()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteGcd: false,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_NoTarget_ReturnsFalse()
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            targetingService: targeting);

        Assert.False(_module.TryExecute(context, isMoving: false));
    }

    [Fact]
    public void TryExecute_FlareStar_At6AstralSoulStacks_InAstralFire_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(BLMActions.FlareStar.ActionId)).Returns(true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BLMActions.FlareStar.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            inAstralFire: true,
            astralSoulStacks: 6,
            currentMp: 10000,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.FlareStar.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Xenoglossy_WithPolyglot_MovingInFirePhase_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Xenoglossy.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            inAstralFire: true,
            polyglotStacks: 1,
            hasInstantCast: false,
            actionService: actionService,
            targetingService: targeting);

        // Moving with polyglot available — should fire Xenoglossy
        var result = _module.TryExecute(context, isMoving: true);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Xenoglossy.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_FireIV_InAstralFire3_WithMp_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Fire4.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            inAstralFire: true,
            astralFireStacks: 3,
            umbralHearts: 3,
            currentMp: 8000,       // Enough for Fire IV (800 cost)
            elementTimer: 15f,     // Not about to drop
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Fire4.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Despair_InAstralFire_LowMp_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Despair.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            inAstralFire: true,
            astralFireStacks: 3,
            currentMp: 900,        // Between DespairMpCost(800) and Fire4MpCost*2(1600)
            elementTimer: 15f,
            hasFirestarter: false, // No proc to use first
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Despair.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Paradox_InFirePhase_LowTimer_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Paradox.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            inAstralFire: true,
            hasParadox: true,
            elementTimer: 4f,      // Below ElementRefreshThreshold (6f) — triggers Paradox
            currentMp: 8000,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Paradox.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_Thunder_WhenThunderheadActive_DoTExpired_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // Thunder action at Lv.100 = HighThunder (ActionId 36986)
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BLMActions.HighThunder.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            hasThunderhead: true,
            thunderheadRemaining: 20f,
            hasThunderDoT: false,
            thunderDoTRemaining: 0f,  // DoT expired
            inAstralFire: false,
            inUmbralIce: false,       // Neutral state to reach TryProcs
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.HighThunder.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_BlizzardIII_TransitionToIce_InAstralFire_LowMp_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        // Blizzard III at Lv.100 is the transition spell
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Blizzard3.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            inAstralFire: true,
            astralFireStacks: 3,
            currentMp: 100,        // Below DespairMpCost (800) — triggers Ice transition
            hasFirestarter: false,
            hasParadox: false,
            elementTimer: 15f,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Blizzard3.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void TryExecute_FireIII_WithFirestarter_InUmbralIce_Fires()
    {
        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.ExecuteGcd(
                It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Fire3.ActionId),
                It.IsAny<ulong>()))
            .Returns(true);

        // In Ice phase with Firestarter proc — should use it to enter Fire
        var context = HecateTestContext.Create(
            inCombat: true,
            canExecuteGcd: true,
            inAstralFire: false,
            inUmbralIce: true,
            umbralIceStacks: 3,
            umbralHearts: 3,
            hasFirestarter: true,
            firestarterRemaining: 2f,  // About to expire
            currentMp: 10000,
            actionService: actionService,
            targetingService: targeting);

        var result = _module.TryExecute(context, isMoving: false);

        Assert.True(result);
        actionService.Verify(x => x.ExecuteGcd(
            It.Is<ActionDefinition>(a => a.ActionId == BLMActions.Fire3.ActionId),
            It.IsAny<ulong>()), Times.Once);
    }

    [Fact]
    public void FireIV_BlockedWhenRaidwideImminent()
    {
        // Arrange: raidwide in 1.5s, Fire IV cast = 2.8s → deadline 3.3s → should block
        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.Timeline.EnableMechanicAwareCasting = true;
        config.Timeline.EnableTimelinePredictions = true;
        config.Timeline.TimelineConfidenceThreshold = 0.8f;

        var enemy = CreateMockEnemy();
        var targeting = CreateTargetingWithEnemy(enemy);

        var timelineMock = new Mock<ITimelineService>();
        timelineMock.Setup(x => x.IsActive).Returns(true);
        timelineMock.Setup(x => x.Confidence).Returns(0.9f);
        timelineMock.Setup(x => x.NextRaidwide).Returns(
            new MechanicPrediction(1.5f, TimelineEntryType.Raidwide, "Exaflare", 0.9f));

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.IsActionReady(BLMActions.FlareStar.ActionId)).Returns(false);

        var context = HecateTestContext.Create(
            config: config,
            inCombat: true,
            canExecuteGcd: true,
            inAstralFire: true,
            astralFireStacks: 3,
            umbralHearts: 3,
            currentMp: 8000,
            elementTimer: 15f,
            astralSoulStacks: 0,  // No Flare Star
            hasFirestarter: false,
            hasParadox: false,
            hasInstantCast: false,
            hasSwiftcast: false,
            actionService: actionService,
            targetingService: targeting,
            timelineService: timelineMock.Object);

        // Act
        _module.TryExecute(context, isMoving: false);

        // Assert: no GCD should fire — gate should block
        actionService.Verify(
            x => x.ExecuteGcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>()),
            Times.Never);
    }

    #region Helpers

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static Mock<ITargetingService> CreateTargetingWithEnemy(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);
        return targeting;
    }

    #endregion
}
