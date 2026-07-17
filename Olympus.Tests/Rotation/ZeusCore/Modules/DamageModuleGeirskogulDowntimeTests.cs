using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ZeusCore.Abilities;
using Olympus.Rotation.ZeusCore.Context;
using Olympus.Rotation.ZeusCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.ZeusCore.Modules;

/// <summary>
/// Verifies the pre-downtime dump gate for DRG Geirskogul:
/// The burst hold is bypassed when downtime is within 8s, allowing Geirskogul
/// to fire even though the burst window is imminent.
/// </summary>
public class DamageModuleGeirskogulDowntimeTests
{
    // -----------------------------------------------------------------------
    // Hold: burst imminent, eyeCount >= GeirskogulMinEyes, no timeline → held
    // -----------------------------------------------------------------------

    [Fact]
    public void Geirskogul_Hold_WhenBurstImminentAndNoTimeline()
    {
        var burstService = CreateImminent();
        var module = new DamageModule(burstService.Object);

        var config = ZeusTestContext.CreateDefaultDragoonConfiguration();
        config.Dragoon.EnableGeirskogul = true;
        config.Dragoon.EnableBurstPooling = true;
        config.MeleeShared.EnableSecondWind = false;
        config.MeleeShared.EnableBloodbath = false;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRGActions.Geirskogul.ActionId)).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targeting: targeting, timelineService: null, eyeCount: 1);

        module.CollectCandidates(context, scheduler, isMoving: false);

        // eyeCount=1 >= GeirskogulMinEyes=0 AND burst imminent → hold fires → NOT pushed
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ZeusAbilities.Geirskogul);
    }

    // -----------------------------------------------------------------------
    // Dump: burst imminent, 5s downtime → bypass hold, push Geirskogul at priority 3
    // -----------------------------------------------------------------------

    [Fact]
    public void Geirskogul_FiresDespiteBurstHold_WhenDowntimeImminent()
    {
        var burstService = CreateImminent();
        var module = new DamageModule(burstService.Object);

        var config = ZeusTestContext.CreateDefaultDragoonConfiguration();
        config.Dragoon.EnableGeirskogul = true;
        config.Dragoon.EnableBurstPooling = true;
        config.MeleeShared.EnableSecondWind = false;
        config.MeleeShared.EnableBloodbath = false;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DRGActions.Geirskogul.ActionId)).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targeting: targeting, timelineService: timeline, eyeCount: 1);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ZeusAbilities.Geirskogul && c.Priority == 3);
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

    private static Mock<ITargetingService> BuildTargetingWithMeleeEnemy(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
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

    private static IZeusContext CreateContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null,
        Mock<ITimelineService>? timelineService = null,
        int eyeCount = 1,
        bool inCombat = true,
        byte level = 100)
    {
        config ??= ZeusTestContext.CreateDefaultDragoonConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList)
            .Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IZeusContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns(timelineService?.Object);
        mock.Setup(x => x.Debug).Returns(new ZeusDebugState());
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        // DRG-specific defaults that suppress earlier TryPush* paths
        mock.Setup(x => x.EyeCount).Returns(eyeCount);
        mock.Setup(x => x.IsLifeOfDragonActive).Returns(false);  // gate before Geirskogul
        mock.Setup(x => x.HasDiveReady).Returns(false);
        mock.Setup(x => x.HasStarcrossReady).Returns(false);
        mock.Setup(x => x.HasNastrondReady).Returns(false);
        mock.Setup(x => x.HasStardiverReady).Returns(false);
        mock.Setup(x => x.FirstmindsFocus).Returns(0);
        mock.Setup(x => x.HasDraconianFire).Returns(false);
        mock.Setup(x => x.HasFangAndClawBared).Returns(false);
        mock.Setup(x => x.HasWheelInMotion).Returns(false);
        mock.Setup(x => x.HasPowerSurge).Returns(false);
        mock.Setup(x => x.PowerSurgeRemaining).Returns(0f);
        mock.Setup(x => x.HasLanceCharge).Returns(false);
        mock.Setup(x => x.LanceChargeRemaining).Returns(0f);
        mock.Setup(x => x.HasLifeSurge).Returns(false);
        mock.Setup(x => x.HasBattleLitany).Returns(false);
        mock.Setup(x => x.BattleLitanyRemaining).Returns(0f);
        mock.Setup(x => x.HasRightEye).Returns(false);
        mock.Setup(x => x.HasDotOnTarget).Returns(false);
        mock.Setup(x => x.DotRemaining).Returns(0f);
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(0f);
        mock.Setup(x => x.IsInVorpalCombo).Returns(false);
        mock.Setup(x => x.IsInDisembowelCombo).Returns(false);
        mock.Setup(x => x.IsInAoeCombo).Returns(false);
        mock.Setup(x => x.LifeOfDragonRemaining).Returns(0f);

        return mock.Object;
    }
}
