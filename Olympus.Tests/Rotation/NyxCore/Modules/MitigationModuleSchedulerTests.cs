using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.NyxCore.Abilities;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Helpers;
using Olympus.Rotation.NyxCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.NyxCore.Modules;

/// <summary>
/// Scheduler-push tests for Nyx MitigationModule (DRK).
/// Verifies each ability is pushed with the correct behavior reference and priority,
/// and that config toggles, HP thresholds, MP gates, and IPC overlap checks block
/// the push when they should.
/// </summary>
public class MitigationModuleSchedulerTests
{
    private readonly MitigationModule _module = new();

    // -----------------------------------------------------------------------
    // Early-exit guards
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_MitigationDisabled_PushesNothing()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableMitigation = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(config: config, inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Equal("Disabled", context.Debug.MitigationState);
    }

    [Fact]
    public void CollectCandidates_NotInCombat_PushesNothing()
    {
        var scheduler = SchedulerFactory.CreateForTest();
        var context = CreateContext(inCombat: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Equal("Not in combat", context.Debug.MitigationState);
    }

    // -----------------------------------------------------------------------
    // The Blackest Night
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_TBN_PushedAtPriority3_WhenHpBelowThreshold()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableTheBlackestNight = true;
        config.Tank.TBNThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.TheBlackestNight.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // 70% HP is below the 80% threshold; currentMp=5000 >= TbnMpCost(3000)
        var context = CreateContext(
            config: config,
            actionService: actionService,
            currentHp: 35000, maxHp: 50000,
            currentMp: 5000,
            hasTheBlackestNight: false,
            hasWalkingDead: false,
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.TheBlackestNight && c.Priority == 3);
    }

    [Fact]
    public void CollectCandidates_TBN_NotPushed_WhenDisabled()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableTheBlackestNight = false;
        config.Tank.TBNThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.TheBlackestNight.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            currentHp: 35000, maxHp: 50000,
            currentMp: 5000,
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.TheBlackestNight);
    }

    [Fact]
    public void CollectCandidates_TBN_NotPushed_WhenAlreadyActive()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableTheBlackestNight = true;
        config.Tank.TBNThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.TheBlackestNight.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            currentHp: 35000, maxHp: 50000,
            currentMp: 5000,
            hasTheBlackestNight: true,  // shield already applied
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.TheBlackestNight);
    }

    [Fact]
    public void CollectCandidates_TBN_NotPushed_WhenHpAboveThreshold()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableTheBlackestNight = true;
        config.Tank.TBNThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.TheBlackestNight.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // 96% HP is above the 80% threshold
        var context = CreateContext(
            config: config,
            actionService: actionService,
            currentHp: 48000, maxHp: 50000,
            currentMp: 5000,
            hasTheBlackestNight: false,
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.TheBlackestNight);
    }

    [Fact]
    public void CollectCandidates_TBN_NotPushed_WhenInsufficientMp()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableTheBlackestNight = true;
        config.Tank.TBNThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.TheBlackestNight.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // 2000 MP is below the 3000 TBN cost
        var context = CreateContext(
            config: config,
            actionService: actionService,
            currentHp: 35000, maxHp: 50000,
            currentMp: 2000,
            hasTheBlackestNight: false,
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.TheBlackestNight);
    }

    // -----------------------------------------------------------------------
    // Oblation
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Oblation_PushedAtPriority3_WhenHpBelowMitigationThreshold()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableOblation = true;
        config.Tank.MitigationThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Oblation.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // 70% HP is below the 80% mitigation threshold
        var context = CreateContext(
            config: config,
            actionService: actionService,
            currentHp: 35000, maxHp: 50000,
            hasOblation: false,
            hasWalkingDead: false,
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.Oblation && c.Priority == 3);
    }

    [Fact]
    public void CollectCandidates_Oblation_NotPushed_WhenDisabled()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableOblation = false;
        config.Tank.MitigationThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Oblation.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config, actionService: actionService,
            currentHp: 35000, maxHp: 50000,
            inCombat: true, level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.Oblation);
    }

    [Fact]
    public void CollectCandidates_Oblation_NotPushed_WhenLevelTooLow()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableOblation = true;
        config.Tank.MitigationThreshold = 0.80f;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Oblation.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // Oblation is MinLevel 82; level 81 is below
        var context = CreateContext(
            config: config, actionService: actionService,
            currentHp: 35000, maxHp: 50000,
            inCombat: true,
            level: (byte)(DRKActions.Oblation.MinLevel - 1));

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.Oblation);
    }

    // -----------------------------------------------------------------------
    // Dark Missionary
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_DarkMissionary_PushedAtPriority5_WhenPartyInjured()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableDarkMissionary = true;
        config.PartyCoordination.EnableCooldownCoordination = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.DarkMissionary.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // 3 injured members, average HP 75% — triggers the gate (injuredCount >= 3 OR avgHp <= 0.85)
        var context = CreateContext(
            config: config,
            actionService: actionService,
            partyHealthMetrics: (0.75f, 0.60f, 3),
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.DarkMissionary && c.Priority == 5);
    }

    [Fact]
    public void CollectCandidates_DarkMissionary_NotPushed_WhenPartyHealthy()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableDarkMissionary = true;
        config.PartyCoordination.EnableCooldownCoordination = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.DarkMissionary.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // Only 1 injured, 95% avg HP — gate: injuredCount < 3 && avgHp > 0.85 → skip
        var context = CreateContext(
            config: config,
            actionService: actionService,
            partyHealthMetrics: (0.95f, 0.90f, 1),
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.DarkMissionary);
    }

    [Fact]
    public void CollectCandidates_DarkMissionary_NotPushed_WhenRemoteMitActive()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableDarkMissionary = true;
        config.PartyCoordination.EnableCooldownCoordination = true;
        config.PartyCoordination.CooldownOverlapWindowSeconds = 10f;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.DarkMissionary.ActionId)).Returns(true);

        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.WasPartyMitigationUsedRecently(It.IsAny<float>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            partyCoordService: partyCoord.Object,
            partyHealthMetrics: (0.70f, 0.55f, 4),
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.DarkMissionary);
        Assert.Equal("Dark Missionary skipped (remote mit)", context.Debug.MitigationState);
    }

    // -----------------------------------------------------------------------
    // Reprisal
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Reprisal_PushedAtPriority4_WhenPartyInjured()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.PartyCoordination.EnableCooldownCoordination = false;

        var enemy = CreateMockEnemy(99ul);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Reprisal.ActionId)).Returns(true);

        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>())).Returns(1);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            currentTarget: enemy.Object,
            partyHealthMetrics: (0.70f, 0.50f, 4),
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.Reprisal && c.Priority == 4);
    }

    [Fact]
    public void CollectCandidates_Reprisal_NotPushed_WhenRemoteReprisalActive()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.PartyCoordination.EnableCooldownCoordination = true;

        var enemy = CreateMockEnemy(99ul);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Reprisal.ActionId)).Returns(true);

        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>())).Returns(1);

        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.WasActionUsedByOther(RoleActions.Reprisal.ActionId, 10f)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            targeting: targeting,
            currentTarget: enemy.Object,
            partyCoordService: partyCoord.Object,
            partyHealthMetrics: (0.70f, 0.50f, 4),
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.Reprisal);
        Assert.Equal("Reprisal skipped (remote Reprisal up)", context.Debug.MitigationState);
    }

    // -----------------------------------------------------------------------
    // Interrupt (Interject / Low Blow)
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Interrupt_Interject_PushedAtPriority1_WhenTargetCastingInterruptible()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableInterject = true;
        config.PartyCoordination.EnableInterruptCoordination = false;

        // currentCastTime=1f > interruptDelay (which is at most 0.7f), so delay passes
        var target = CreateMockCastingEnemy(interruptible: true, totalCastTime: 3f, currentCastTime: 1f);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Interject.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            currentTarget: target.Object,
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(
            scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.Interject && c.Priority == 1);
    }

    [Fact]
    public void CollectCandidates_Interrupt_NotPushed_WhenTargetNotCasting()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableInterject = true;

        var target = new Mock<IBattleChara>();
        target.Setup(x => x.IsCasting).Returns(false);
        target.Setup(x => x.EntityId).Returns(55u);
        target.Setup(x => x.GameObjectId).Returns(55ul);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Interject.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            currentTarget: target.Object,
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.Interject);
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.LowBlow);
    }

    [Fact]
    public void CollectCandidates_Interrupt_NotPushed_WhenIpcReservedByOther()
    {
        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableInterject = true;
        config.PartyCoordination.EnableInterruptCoordination = true;

        var target = CreateMockCastingEnemy(interruptible: true, totalCastTime: 3f, currentCastTime: 1f);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Interject.ActionId)).Returns(true);

        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.IsInterruptTargetReservedByOther(target.Object.EntityId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            config: config,
            actionService: actionService,
            currentTarget: target.Object,
            partyCoordService: partyCoord.Object,
            inCombat: true,
            level: 100);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == NyxAbilities.Interject);
        Assert.Equal("Interrupt reserved by other", context.Debug.MitigationState);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<IBattleChara> CreateMockEnemy(ulong gameObjectId = 99ul)
    {
        var m = new Mock<IBattleChara>();
        m.Setup(x => x.GameObjectId).Returns(gameObjectId);
        m.Setup(x => x.EntityId).Returns((uint)gameObjectId);
        m.Setup(x => x.IsCasting).Returns(false);
        m.Setup(x => x.IsCastInterruptible).Returns(false);
        return m;
    }

    private static Mock<IBattleChara> CreateMockCastingEnemy(
        bool interruptible, float totalCastTime, float currentCastTime,
        ulong gameObjectId = 77ul)
    {
        var m = new Mock<IBattleChara>();
        m.Setup(x => x.GameObjectId).Returns(gameObjectId);
        m.Setup(x => x.EntityId).Returns((uint)gameObjectId);
        m.Setup(x => x.IsCasting).Returns(true);
        m.Setup(x => x.IsCastInterruptible).Returns(interruptible);
        m.Setup(x => x.TotalCastTime).Returns(totalCastTime);
        m.Setup(x => x.CurrentCastTime).Returns(currentCastTime);
        return m;
    }

    /// <summary>
    /// Builds a Mock&lt;INyxContext&gt; with safe defaults for all properties
    /// read by MitigationModule. Individual tests override as needed.
    /// </summary>
    private static INyxContext CreateContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null,
        IBattleChara? currentTarget = null,
        IPartyCoordinationService? partyCoordService = null,
        (float avg, float lowest, int injured)? partyHealthMetrics = null,
        bool inCombat = true,
        byte level = 100,
        uint currentHp = 50000,
        uint maxHp = 50000,
        uint currentMp = 10000,
        bool hasTheBlackestNight = false,
        bool hasOblation = false,
        bool hasWalkingDead = false,
        bool hasLivingDead = false,
        bool hasDarkMind = false,
        bool hasShadowWall = false,
        bool hasActiveMitigation = false)
    {
        config ??= NyxTestContext.CreateDefaultDarkKnightConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(
            level: level, currentHp: currentHp, maxHp: maxHp, currentMp: currentMp);

        var tankCooldown = new Mock<ITankCooldownService>();
        tankCooldown.Setup(x => x.ShouldUseMitigation(
            It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>())).Returns(false);
        tankCooldown.Setup(x => x.ShouldUseMajorCooldown(
            It.IsAny<float>(), It.IsAny<float>())).Returns(false);

        var damageIntake = MockBuilders.CreateMockDamageIntakeService();

        var metrics = partyHealthMetrics ?? (1f, 1f, 0);

        var debugState = new NyxDebugState();
        var mock = new Mock<INyxContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TankCooldownService).Returns(tankCooldown.Object);
        mock.Setup(x => x.DamageIntakeService).Returns(damageIntake.Object);
        mock.Setup(x => x.PartyCoordinationService).Returns(partyCoordService);
        mock.Setup(x => x.CurrentTarget).Returns(currentTarget);
        mock.Setup(x => x.PartyHealthMetrics).Returns(metrics);
        mock.Setup(x => x.TimelineService).Returns((Olympus.Timeline.ITimelineService?)null);
        mock.Setup(x => x.TrainingService).Returns((Olympus.Services.Training.ITrainingService?)null);
        mock.Setup(x => x.StatusHelper).Returns(new NyxStatusHelper());
        mock.Setup(x => x.Debug).Returns(debugState);

        // Defensive state — all false by default; tests override as needed
        mock.Setup(x => x.HasTheBlackestNight).Returns(hasTheBlackestNight);
        mock.Setup(x => x.HasOblation).Returns(hasOblation);
        mock.Setup(x => x.HasWalkingDead).Returns(hasWalkingDead);
        mock.Setup(x => x.HasLivingDead).Returns(hasLivingDead);
        mock.Setup(x => x.HasDarkMind).Returns(hasDarkMind);
        mock.Setup(x => x.HasShadowWall).Returns(hasShadowWall);
        mock.Setup(x => x.HasActiveMitigation).Returns(hasActiveMitigation);

        // MP-derived gates
        mock.Setup(x => x.HasEnoughMpForTbn).Returns(currentMp >= DRKActions.TbnMpCost);
        mock.Setup(x => x.HasEnoughMpForEdge).Returns(currentMp >= DRKActions.EdgeFloodMpCost);
        mock.Setup(x => x.CurrentMp).Returns((int)currentMp);

        // Not relevant for mitigation — safe defaults
        mock.Setup(x => x.HasDelirium).Returns(false);
        mock.Setup(x => x.HasDarkside).Returns(false);
        mock.Setup(x => x.HasDarkArts).Returns(false);
        mock.Setup(x => x.BloodGauge).Returns(0);

        return mock.Object;
    }
}
