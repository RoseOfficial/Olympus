using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.NyxCore.Abilities;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Rotation.NyxCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.NyxCore.Modules;

/// <summary>
/// Verifies the pre-downtime dump gates added to DRK DamageModule:
///   1. Shadowbringer: dump gate bypasses burst hold when downtime is within 10s.
///   2. Edge of Shadow MP dump: fires at 6000 MP threshold instead of 9400 when downtime is within 10s.
/// </summary>
public class DamageModuleShadowbringerDowntimeTests
{
    // -----------------------------------------------------------------------
    // Shadowbringer -- hold when burst imminent and no timeline
    // -----------------------------------------------------------------------

    [Fact]
    public void Shadowbringer_Hold_WhenBurstImminentAndOneCharge_NoTimeline()
    {
        var burstService = CreateImminent();
        var module = new DamageModule(burstService.Object);

        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableShadowbringer = true;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Shadowbringer.ActionId)).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(DRKActions.Shadowbringer.ActionId)).Returns(1u);
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targeting: targeting, timelineService: null);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.Shadowbringer);
    }

    // -----------------------------------------------------------------------
    // Shadowbringer -- fires despite burst hold when downtime within 10s
    // -----------------------------------------------------------------------

    [Fact]
    public void Shadowbringer_FiresDespiteBurstHold_WhenDowntimeImminent()
    {
        var burstService = CreateImminent();
        var module = new DamageModule(burstService.Object);

        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableShadowbringer = true;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.Shadowbringer.ActionId)).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(DRKActions.Shadowbringer.ActionId)).Returns(1u);
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targeting: targeting, timelineService: timeline);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.Shadowbringer && c.Priority == 2);
    }

    // -----------------------------------------------------------------------
    // Edge of Shadow MP dump -- fires when MP >= 6000 and downtime within 10s
    // -----------------------------------------------------------------------

    [Fact]
    public void EdgeOfShadow_MpDump_Fires_WhenDowntimeImminentAndMp7000()
    {
        var module = new DamageModule(); // no burst service -- isolate the MP dump gate

        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableShadowbringer = false; // suppress Shadowbringer in this test

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        // Use Edge action (level 100 single-target path)
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(DRKActions.EdgeOfShadow.ActionId)).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targeting: targeting, timelineService: timeline,
            currentMp: 7000,
            hasEnoughMpForEdge: true,
            hasDarkside: true,
            darksideRemaining: 30f); // not expiring, but MP dump threshold lowers

        module.CollectCandidates(context, scheduler, isMoving: false);

        // Downtime within 10s lowers threshold to 6000; MP=7000 >= 6000 → fires
        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.EdgeOfShadow && c.Priority == 5);
    }

    [Fact]
    public void EdgeOfShadow_MpDump_NotFired_WhenNoTimeline_AndMp7000()
    {
        var module = new DamageModule();

        var config = NyxTestContext.CreateDefaultDarkKnightConfiguration();
        config.Tank.EnableShadowbringer = false;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRKActions.HardSlash.ActionId)).Returns(true);
        actionService.Setup(x => x.IsActionReady(DRKActions.EdgeOfShadow.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targeting: targeting, timelineService: null,
            currentMp: 7000,
            hasEnoughMpForEdge: true,
            hasDarkside: true,
            darksideRemaining: 30f);

        module.CollectCandidates(context, scheduler, isMoving: false);

        // No downtime → threshold stays 9400; MP=7000 < 9400 → mpDump=false, not expiring → not pushed
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == NyxAbilities.EdgeOfShadow);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<IBurstWindowService> CreateImminent()
    {
        var svc = new Mock<IBurstWindowService>();
        svc.Setup(x => x.IsInBurstWindow).Returns(false);
        svc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);
        return svc;
    }

    private static Mock<ITimelineService> CreateDowntimeTimeline(float secondsUntil)
    {
        var tl = new Mock<ITimelineService>();
        tl.Setup(x => x.Confidence).Returns(1.0f);
        tl.Setup(x => x.SecondsUntilNextUntargetablePhase()).Returns((float?)secondsUntil);
        return tl;
    }

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 100ul)
    {
        var m = new Mock<IBattleNpc>();
        m.Setup(x => x.GameObjectId).Returns(objectId);
        m.Setup(x => x.CurrentHp).Returns(10000u);
        m.Setup(x => x.MaxHp).Returns(10000u);
        return m;
    }

    private static Mock<ITargetingService> BuildTargetingWithMeleeEnemy(
        Mock<IBattleNpc> enemy, int enemyCount = 1)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemyCount);

        var safety = new Mock<IGapCloserSafetyService>();
        safety.Setup(x => x.ShouldBlockGapCloser(
            It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>())).Returns(false);
        safety.Setup(x => x.LastBlockReason).Returns((string?)null);
        targeting.Setup(x => x.GapCloserSafety).Returns(safety.Object);

        return targeting;
    }

    private static INyxContext CreateContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null,
        Mock<ITimelineService>? timelineService = null,
        bool inCombat = true,
        byte level = 100,
        int currentMp = 0,
        bool hasEnoughMpForEdge = false,
        bool hasDarkside = false,
        float darksideRemaining = 0f)
    {
        config ??= NyxTestContext.CreateDefaultDarkKnightConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList)
            .Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<INyxContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns(timelineService?.Object);
        mock.Setup(x => x.Debug).Returns(new NyxDebugState());

        // DRK-specific defaults that short-circuit paths other than the ones under test
        mock.Setup(x => x.HasDarkArts).Returns(false);
        mock.Setup(x => x.HasSaltedEarth).Returns(true);   // suppress SaltedEarth
        mock.Setup(x => x.HasEnoughMpForEdge).Returns(hasEnoughMpForEdge);
        mock.Setup(x => x.HasDelirium).Returns(false);
        mock.Setup(x => x.DeliriumStacks).Returns(0);
        mock.Setup(x => x.HasScornfulEdge).Returns(false);
        mock.Setup(x => x.BloodGauge).Returns(0);
        mock.Setup(x => x.HasDarkside).Returns(hasDarkside);
        mock.Setup(x => x.DarksideRemaining).Returns(darksideRemaining);
        mock.Setup(x => x.CurrentMp).Returns(currentMp);
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(30f);
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        return mock.Object;
    }
}
