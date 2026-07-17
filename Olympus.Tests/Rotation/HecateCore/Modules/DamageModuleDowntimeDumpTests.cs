using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.HecateCore.Abilities;
using Olympus.Rotation.HecateCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.HecateCore.Modules;

/// <summary>
/// Verifies that BurstHoldHelper.ShouldDumpForDowntime allows Xenoglossy to fire
/// when burst hold is active but a boss untargetable phase is imminent (B2 interplay rule).
/// Also covers the stationary downtime-dump push added to TryPushPolyglot.
/// </summary>
public class DamageModuleDowntimeDumpTests
{
    // -----------------------------------------------------------------------
    // 1. Burst imminent + downtime imminent + 1 Polyglot + stationary -> fires
    // -----------------------------------------------------------------------

    [Fact]
    public void Xenoglossy_FiresDespiteBurstHold_WhenDowntimeImminentAndStationary()
    {
        var burstService = CreateImminent();
        var module = new DamageModule(burstService.Object);

        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.BlackMage.EnableXenoglossy = true;
        config.BlackMage.EnableFoul = false; // force Xenoglossy selection
        config.BlackMage.EnableBurstPooling = true;
        config.BlackMage.PolyglotMinStacks = 1;

        var enemy = CreateEnemy(42u);
        var targeting = CreateTargetingWith(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = HecateTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            timelineService: timeline.Object,
            polyglotStacks: 1,   // < 2, would normally hold for burst
            inCombat: true,
            level: 100);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == HecateAbilities.Xenoglossy && c.Priority == 3);
    }

    // -----------------------------------------------------------------------
    // 2. Burst imminent + no timeline + 1 Polyglot -> holds (no dump push)
    // -----------------------------------------------------------------------

    [Fact]
    public void Xenoglossy_HeldForBurst_WhenNoTimeline()
    {
        var burstService = CreateImminent();
        var module = new DamageModule(burstService.Object);

        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.BlackMage.EnableXenoglossy = true;
        config.BlackMage.EnableFoul = false;
        config.BlackMage.EnableBurstPooling = true;
        config.BlackMage.PolyglotMinStacks = 1;

        var enemy = CreateEnemy(42u);
        var targeting = CreateTargetingWith(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = HecateTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            timelineService: null, // no timeline -> ShouldDumpForDowntime returns false
            polyglotStacks: 1,
            inCombat: true,
            level: 100);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == HecateAbilities.Xenoglossy && c.Priority == 3);
    }

    // -----------------------------------------------------------------------
    // 3. No burst hold + 2 Polyglot stacks -> fires via cap-avoidance path
    // -----------------------------------------------------------------------

    [Fact]
    public void Xenoglossy_Fires_When2PolyglotStacks_CapAvoidance()
    {
        // At cap (2 stacks at Lv.<98) -> cap-avoidance push fires regardless of burst
        var burstService = CreateImminent();
        var module = new DamageModule(burstService.Object);

        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.BlackMage.EnableXenoglossy = true;
        config.BlackMage.EnableFoul = false;
        config.BlackMage.EnableBurstPooling = true;

        var enemy = CreateEnemy(42u);
        var targeting = CreateTargetingWith(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = HecateTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            timelineService: null,
            polyglotStacks: 2, // >= maxPolyglot(2 at Lv.<98) -> cap path pushes at priority 3
            inCombat: true,
            level: 90); // below 98 so maxPolyglot=2

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == HecateAbilities.Xenoglossy && c.Priority == 3);
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

    private static Mock<IBattleNpc> CreateEnemy(uint id)
    {
        var m = new Mock<IBattleNpc>();
        m.Setup(x => x.EntityId).Returns(id);
        m.Setup(x => x.GameObjectId).Returns((ulong)id);
        m.Setup(x => x.MaxHp).Returns(100000u);
        m.Setup(x => x.CurrentHp).Returns(100000u);
        m.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return m;
    }

    private static Mock<ITargetingService> CreateTargetingWith(IBattleNpc enemy)
    {
        var t = new Mock<ITargetingService>();
        t.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        t.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy);
        t.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(1);
        t.Setup(x => x.FindEnemyNeedingDot(
                It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        return t;
    }
}
