using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.TerpsichoreCore.Abilities;
using Olympus.Rotation.TerpsichoreCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.TerpsichoreCore.Modules;

/// <summary>
/// Verifies that BurstHoldHelper.ShouldDumpForDowntime allows Fan Dance to fire
/// at 3+ feathers before a boss untargetable phase, bypassing SaveFeathersForBurst.
/// </summary>
public class BuffModuleDowntimeDumpTests
{
    // -----------------------------------------------------------------------
    // 1. Downtime imminent + feathers >= 3 + save-for-burst -> fires (dump wins)
    // -----------------------------------------------------------------------

    [Fact]
    public void FanDance_FiresDespiteSaveForBurst_WhenDowntimeImminentAndFeathers3()
    {
        var module = new BuffModule();

        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableFanDance = true;
        config.Dancer.SaveFeathersForBurst = true;
        config.Dancer.FeatherOvercapThreshold = 4; // 3 feathers is below overcap, shouldUse=false
        config.Dancer.FanDanceMinFeathers = 1;

        var enemy = CreateEnemy(42u);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DNCActions.FanDance.ActionId)).Returns(true);

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
            feathers: 3,
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasDancePartner: true,       // suppress ClosedPosition
            hasThreefoldFanDance: false, // suppress FanDanceIII
            hasFourfoldFanDance: false,  // suppress FanDanceIV
            inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == TerpsichoreAbilities.FanDance);
    }

    // -----------------------------------------------------------------------
    // 2. No downtime + feathers = 3 + save-for-burst -> does NOT fire
    // -----------------------------------------------------------------------

    [Fact]
    public void FanDance_NotFired_WhenNoDowntimeAndSaveForBurst()
    {
        var module = new BuffModule();

        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableFanDance = true;
        config.Dancer.SaveFeathersForBurst = true;
        config.Dancer.FeatherOvercapThreshold = 4;
        config.Dancer.FanDanceMinFeathers = 1;

        var enemy = CreateEnemy(42u);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DNCActions.FanDance.ActionId)).Returns(true);

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
            timelineService: null, // no timeline
            feathers: 3,
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasDancePartner: true,
            hasThreefoldFanDance: false,
            hasFourfoldFanDance: false,
            inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == TerpsichoreAbilities.FanDance);
    }

    // -----------------------------------------------------------------------
    // 3. Downtime imminent + feathers < 3 -> does NOT fire (threshold not met)
    // -----------------------------------------------------------------------

    [Fact]
    public void FanDance_NotFired_WhenDowntimeImminentButFeathersBelow3()
    {
        var module = new BuffModule();

        var config = TerpsichoreTestContext.CreateDefaultDancerConfiguration();
        config.Dancer.EnableFanDance = true;
        config.Dancer.SaveFeathersForBurst = true;
        config.Dancer.FeatherOvercapThreshold = 4;
        config.Dancer.FanDanceMinFeathers = 1;

        var enemy = CreateEnemy(42u);
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(DNCActions.FanDance.ActionId)).Returns(true);

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
            feathers: 2, // below dump threshold of 3
            hasDevilment: false,
            hasTechnicalFinish: false,
            hasDancePartner: true,
            hasThreefoldFanDance: false,
            hasFourfoldFanDance: false,
            inCombat: true);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == TerpsichoreAbilities.FanDance);
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
