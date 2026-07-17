using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.NikeCore.Abilities;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Rotation.NikeCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.NikeCore.Modules;

/// <summary>
/// Verifies the pre-downtime dump gate for SAM Kenki spenders:
/// When kenki is below the overcap threshold (shouldSpend=false), the downtime
/// dump check allows Shinten to fire when downtime is within 10s.
/// Note: with default KenkiMinGauge=25 and KenkiReserveForBurst=25,
/// the burst hold does not fire at kenki=25 (25 < 25 is false). The gate that
/// prevents spending without downtime is the shouldSpend=false guard.
/// </summary>
public class DamageModuleKenkiDowntimeTests
{
    // -----------------------------------------------------------------------
    // No push: kenki < overcap, no downtime → shouldSpend=false, stays silent
    // -----------------------------------------------------------------------

    [Fact]
    public void Kenki_NotPushed_WhenShouldSpendFalseAndNoTimeline()
    {
        var module = new DamageModule();

        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.EnableShinten = true;
        config.Samurai.EnableBurstPooling = true;
        config.MeleeShared.EnableSecondWind = false;
        config.MeleeShared.EnableBloodbath = false;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(SAMActions.Shinten.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        // kenki=25: >= KenkiMinGauge(25), < KenkiOvercapThreshold(80), < 50
        // shouldSpend = 25>=80=false || 25>=50=false = false
        // !false && !dumpForDowntime(false) = true → return → Shinten NOT pushed
        var context = CreateContext(config: config, actionService: actionService,
            targeting: targeting, timelineService: null, kenki: 25);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == NikeAbilities.Shinten);
    }

    // -----------------------------------------------------------------------
    // Push: kenki < overcap, 5s downtime → !false && !true=false → Shinten pushed
    // -----------------------------------------------------------------------

    [Fact]
    public void Kenki_Pushed_WhenDowntimeImminentDespiteLowKenki()
    {
        var module = new DamageModule();

        var config = NikeTestContext.CreateDefaultSamuraiConfiguration();
        config.Samurai.EnableShinten = true;
        config.Samurai.EnableBurstPooling = true;
        config.MeleeShared.EnableSecondWind = false;
        config.MeleeShared.EnableBloodbath = false;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(SAMActions.Shinten.ActionId)).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targeting: targeting, timelineService: timeline, kenki: 25);

        module.CollectCandidates(context, scheduler, isMoving: false);

        // dumpForDowntime=true → !shouldSpend && !true = false → don't return → Shinten pushed
        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == NikeAbilities.Shinten && c.Priority == 5);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

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

    private static Mock<ITargetingService> BuildTargetingWithMeleeEnemy(Mock<IBattleNpc> enemy)
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
            .Returns(1);

        var safety = new Mock<IGapCloserSafetyService>();
        safety.Setup(x => x.ShouldBlockGapCloser(
            It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>())).Returns(false);
        safety.Setup(x => x.LastBlockReason).Returns((string?)null);
        targeting.Setup(x => x.GapCloserSafety).Returns(safety.Object);

        return targeting;
    }

    private static INikeContext CreateContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null,
        Mock<ITimelineService>? timelineService = null,
        int kenki = 25,
        bool inCombat = true,
        byte level = 100)
    {
        config ??= NikeTestContext.CreateDefaultSamuraiConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList)
            .Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<INikeContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns(timelineService?.Object);
        mock.Setup(x => x.Debug).Returns(new NikeDebugState());
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        // SAM-specific defaults
        mock.Setup(x => x.Kenki).Returns(kenki);
        mock.Setup(x => x.Sen).Returns(SAMActions.SenType.None);
        mock.Setup(x => x.SenCount).Returns(0);
        mock.Setup(x => x.HasSetsu).Returns(false);
        mock.Setup(x => x.HasGetsu).Returns(false);
        mock.Setup(x => x.HasKa).Returns(false);
        mock.Setup(x => x.Meditation).Returns(0);
        mock.Setup(x => x.HasFugetsu).Returns(false);
        mock.Setup(x => x.HasFuka).Returns(false);
        mock.Setup(x => x.HasMeikyoShisui).Returns(false);
        mock.Setup(x => x.HasOgiNamikiriReady).Returns(false);
        mock.Setup(x => x.HasKaeshiNamikiriReady).Returns(false);
        mock.Setup(x => x.HasTsubameGaeshiReady).Returns(false);
        mock.Setup(x => x.HasZanshinReady).Returns(false);
        mock.Setup(x => x.HasHiganbanaOnTarget).Returns(false);
        mock.Setup(x => x.HiganbanaRemaining).Returns(0f);
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(0f);

        return mock.Object;
    }
}
