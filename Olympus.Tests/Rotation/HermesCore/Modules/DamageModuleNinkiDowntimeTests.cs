using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.HermesCore.Abilities;
using Olympus.Rotation.HermesCore.Context;
using Olympus.Rotation.HermesCore.Modules;
using Olympus.Rotation.HermesCore.Helpers;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.HermesCore.Modules;

/// <summary>
/// Verifies the pre-downtime dump gate for NIN Ninki spenders:
/// The burst hold is bypassed when downtime is within 8s, allowing Bhavacakra
/// to fire even though the burst window is imminent.
/// </summary>
public class DamageModuleNinkiDowntimeTests
{
    // -----------------------------------------------------------------------
    // Hold: burst imminent, ninki < ninkiOvercapThreshold, no timeline → held
    // -----------------------------------------------------------------------

    [Fact]
    public void Ninki_Hold_WhenBurstImminentAndNoTimeline()
    {
        var burstService = CreateImminent();
        var module = new DamageModule(burstService.Object);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.EnableBurstPooling = true;
        config.Ninja.SaveNinkiForBurst = true;
        config.Ninja.EnableBhavacakra = true;
        config.MeleeShared.EnableSecondWind = false;
        config.MeleeShared.EnableBloodbath = false;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targeting: targeting, timelineService: null, ninki: 60);

        module.CollectCandidates(context, scheduler, isMoving: false);

        // ninki=60 >= NinkiMinGauge(50) AND ninki < NinkiOvercapThreshold(80) AND burst imminent → held
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == HermesAbilities.Bhavacakra);
    }

    // -----------------------------------------------------------------------
    // Dump: burst imminent, 5s downtime → bypass hold, push Bhavacakra at priority 1
    // -----------------------------------------------------------------------

    [Fact]
    public void Ninki_FiresDespiteBurstHold_WhenDowntimeImminent()
    {
        var burstService = CreateImminent();
        var module = new DamageModule(burstService.Object);

        var config = HermesTestContext.CreateDefaultNinjaConfiguration();
        config.Ninja.EnableBurstPooling = true;
        config.Ninja.SaveNinkiForBurst = true;
        config.Ninja.EnableBhavacakra = true;
        config.MeleeShared.EnableSecondWind = false;
        config.MeleeShared.EnableBloodbath = false;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targeting: targeting, timelineService: timeline, ninki: 60);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == HermesAbilities.Bhavacakra && c.Priority == 1);
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
        targeting.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);
        return targeting;
    }

    private static IHermesContext CreateContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null,
        Mock<ITimelineService>? timelineService = null,
        int ninki = 60,
        bool inCombat = true,
        byte level = 100)
    {
        config ??= HermesTestContext.CreateDefaultNinjaConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList)
            .Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var helper = new MudraHelper();

        var mock = new Mock<IHermesContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns(timelineService?.Object);
        mock.Setup(x => x.Debug).Returns(new HermesDebugState());
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);
        mock.Setup(x => x.MudraHelper).Returns(helper);

        // NIN-specific defaults
        mock.Setup(x => x.Ninki).Returns(ninki);
        mock.Setup(x => x.Kazematoi).Returns(0);
        mock.Setup(x => x.IsMudraActive).Returns(false);
        mock.Setup(x => x.HasMeisui).Returns(false);
        mock.Setup(x => x.HasKassatsu).Returns(false);
        mock.Setup(x => x.HasTenChiJin).Returns(false);
        mock.Setup(x => x.HasBunshin).Returns(false);
        mock.Setup(x => x.HasPhantomKamaitachiReady).Returns(false);
        mock.Setup(x => x.HasRaijuReady).Returns(false);
        mock.Setup(x => x.RaijuStacks).Returns(0);
        mock.Setup(x => x.HasTenriJindoReady).Returns(false);
        mock.Setup(x => x.HasDokumoriOnTarget).Returns(false);
        mock.Setup(x => x.HasKunaisBaneOnTarget).Returns(false);
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(0f);

        return mock.Object;
    }
}
