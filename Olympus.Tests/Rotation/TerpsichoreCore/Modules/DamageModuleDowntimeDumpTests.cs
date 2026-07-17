using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.TerpsichoreCore.Abilities;
using Olympus.Rotation.TerpsichoreCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Rotation.Common.Helpers;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.TerpsichoreCore.Modules;

/// <summary>
/// Verifies that BurstHoldHelper.ShouldDumpForDowntime allows Saber Dance to fire
/// at 50+ Esprit before a boss untargetable phase, bypassing the overcap threshold.
/// </summary>
public sealed class DamageModuleDowntimeDumpTests : IDisposable
{
    public DamageModuleDowntimeDumpTests() => BurstHoldHelper.ModifierKeys = null;

    public void Dispose() => BurstHoldHelper.ModifierKeys = null;

    // -----------------------------------------------------------------------
    // 1. Downtime imminent + Esprit >= 50 + overcap threshold not met -> fires
    // -----------------------------------------------------------------------

    [Fact]
    public void SaberDance_FiresDespiteOvercapThreshold_WhenDowntimeImminentAndEsprit50()
    {
        var module = new DamageModule();

        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableSaberDance = true;
        config.Dancer.EspritOvercapThreshold = 80; // 50 Esprit < 80, shouldUse=false normally
        config.Dancer.SaveEspritForBurst = false;

        var enemy = CreateEnemy(42u);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DNCActions.SaberDance.ActionId)).Returns(true);

        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(t => t.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(t => t.CountEnemiesInRange(It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(1);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            timelineService: timeline.Object,
            esprit: 50,
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasDanceOfTheDawnReady: false, // suppress DanceOfTheDawn
            hasFlourishingStarfall: false,  // suppress StarfallDance
            hasFinishingMoveReady: false,   // suppress FinishingMove
            hasLastDanceReady: false,       // suppress LastDance
            hasSilkenSymmetry: false,
            hasSilkenFlow: false,
            inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == TerpsichoreAbilities.SaberDance);
    }

    // -----------------------------------------------------------------------
    // 2. No downtime + Esprit = 50 + overcap threshold 80 -> does NOT fire
    // -----------------------------------------------------------------------

    [Fact]
    public void SaberDance_NotFired_WhenNoDowntimeAndEspritBelowOvercap()
    {
        var module = new DamageModule();

        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableSaberDance = true;
        config.Dancer.EspritOvercapThreshold = 80;
        config.Dancer.SaveEspritForBurst = false;

        var enemy = CreateEnemy(42u);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DNCActions.SaberDance.ActionId)).Returns(true);

        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(t => t.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(t => t.CountEnemiesInRange(It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(1);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            timelineService: null,
            esprit: 50,
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasDanceOfTheDawnReady: false,
            hasFlourishingStarfall: false,
            hasFinishingMoveReady: false,
            hasLastDanceReady: false,
            hasSilkenSymmetry: false,
            hasSilkenFlow: false,
            inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == TerpsichoreAbilities.SaberDance);
    }

    // -----------------------------------------------------------------------
    // 3. Downtime imminent + Esprit < 50 -> does NOT fire (below minimum)
    // -----------------------------------------------------------------------

    [Fact]
    public void SaberDance_NotFired_WhenDowntimeImminentButEspritBelow50()
    {
        var module = new DamageModule();

        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableSaberDance = true;
        config.Dancer.EspritOvercapThreshold = 80;
        config.Dancer.SaveEspritForBurst = false;

        var enemy = CreateEnemy(42u);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DNCActions.SaberDance.ActionId)).Returns(true);

        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(t => t.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(t => t.CountEnemiesInRange(It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(1);

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = TerpsichoreTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            timelineService: timeline.Object,
            esprit: 49, // below the hard 50 floor
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasDanceOfTheDawnReady: false,
            hasFlourishingStarfall: false,
            hasFinishingMoveReady: false,
            hasLastDanceReady: false,
            hasSilkenSymmetry: false,
            hasSilkenFlow: false,
            inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == TerpsichoreAbilities.SaberDance);
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

    private static Mock<IBattleNpc> CreateEnemy(uint id)
    {
        var m = new Mock<IBattleNpc>();
        m.Setup(x => x.EntityId).Returns(id);
        m.Setup(x => x.GameObjectId).Returns((ulong)id);
        m.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return m;
    }
}
