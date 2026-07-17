using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.KratosCore.Abilities;
using Olympus.Rotation.KratosCore.Context;
using Olympus.Rotation.KratosCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.KratosCore.Modules;

/// <summary>
/// Verifies the pre-downtime dump gate for MNK Chakra spenders:
/// The burst hold is bypassed when downtime is within 8s, allowing TheForbiddenChakra
/// to fire even though the burst window is imminent.
/// </summary>
public class DamageModuleChakraDowntimeTests
{
    // -----------------------------------------------------------------------
    // Hold: burst imminent, chakra < 45, no timeline → held (not pushed)
    // -----------------------------------------------------------------------

    [Fact]
    public void Chakra_Hold_WhenBurstImminentAndNoTimeline()
    {
        var burstService = CreateImminent();
        var module = new DamageModule(burstService.Object);

        var config = KratosTestContext.CreateDefaultMonkConfiguration();
        config.Monk.EnableChakraSpenders = true;
        config.Monk.EnableBurstPooling = true;
        config.MeleeShared.EnableSecondWind = false;
        config.MeleeShared.EnableBloodbath = false;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targeting: targeting, timelineService: null, chakra: 10);

        module.CollectCandidates(context, scheduler, isMoving: false);

        // chakra=10 < 45 AND burst imminent → hold fires → TheForbiddenChakra NOT pushed
        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == KratosAbilities.TheForbiddenChakra);
    }

    // -----------------------------------------------------------------------
    // Dump: burst imminent, 5s downtime → bypass hold, push TheForbiddenChakra
    // -----------------------------------------------------------------------

    [Fact]
    public void Chakra_FiresDespiteBurstHold_WhenDowntimeImminent()
    {
        var burstService = CreateImminent();
        var module = new DamageModule(burstService.Object);

        var config = KratosTestContext.CreateDefaultMonkConfiguration();
        config.Monk.EnableChakraSpenders = true;
        config.Monk.EnableBurstPooling = true;
        config.MeleeShared.EnableSecondWind = false;
        config.MeleeShared.EnableBloodbath = false;

        var enemy = CreateMockEnemy(100ul);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(config: config, actionService: actionService,
            targeting: targeting, timelineService: timeline, chakra: 10);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == KratosAbilities.TheForbiddenChakra && c.Priority == 1);
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
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemyCount);
        return targeting;
    }

    private static IKratosContext CreateContext(
        Configuration? config = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null,
        Mock<ITimelineService>? timelineService = null,
        int chakra = 5,
        bool inCombat = true,
        byte level = 100)
    {
        config ??= KratosTestContext.CreateDefaultMonkConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList)
            .Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IKratosContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns(timelineService?.Object);
        mock.Setup(x => x.Debug).Returns(new KratosDebugState());
        mock.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);

        // MNK-specific defaults
        mock.Setup(x => x.Chakra).Returns(chakra);
        mock.Setup(x => x.CurrentForm).Returns(MonkForm.None);
        mock.Setup(x => x.HasFormlessFist).Returns(false);
        mock.Setup(x => x.HasPerfectBalance).Returns(false);
        mock.Setup(x => x.PerfectBalanceStacks).Returns(0);
        mock.Setup(x => x.BeastChakraCount).Returns(0);
        mock.Setup(x => x.HasBothNadi).Returns(false);
        mock.Setup(x => x.HasLunarNadi).Returns(false);
        mock.Setup(x => x.HasSolarNadi).Returns(false);
        mock.Setup(x => x.HasRiddleOfFire).Returns(false);
        mock.Setup(x => x.HasBrotherhood).Returns(false);
        mock.Setup(x => x.HasDisciplinedFist).Returns(false);
        mock.Setup(x => x.DisciplinedFistRemaining).Returns(0f);
        mock.Setup(x => x.HasLeadenFist).Returns(false);
        mock.Setup(x => x.HasDemolishOnTarget).Returns(false);
        mock.Setup(x => x.DemolishRemaining).Returns(0f);
        mock.Setup(x => x.HasFiresRumination).Returns(false);
        mock.Setup(x => x.HasWindsRumination).Returns(false);
        mock.Setup(x => x.HasRaptorsFury).Returns(false);
        mock.Setup(x => x.HasCoeurlsFury).Returns(false);
        mock.Setup(x => x.HasOpooposFury).Returns(false);
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(0f);

        return mock.Object;
    }
}
