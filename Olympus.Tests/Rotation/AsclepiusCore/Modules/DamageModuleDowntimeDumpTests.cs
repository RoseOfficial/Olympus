using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Modules;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;
using Olympus.Rotation.Common.Helpers;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules;

/// <summary>
/// Verifies that BurstHoldHelper.ShouldDumpForDowntime allows Phlegma to fire
/// with 1 charge when a boss untargetable phase is imminent, bypassing both the
/// burst-alignment hold and charge conservation (B2 interplay rule).
/// </summary>
public sealed class DamageModuleDowntimeDumpTests : IDisposable
{
    public DamageModuleDowntimeDumpTests() => BurstHoldHelper.ModifierKeys = null;

    public void Dispose() => BurstHoldHelper.ModifierKeys = null;

    // -----------------------------------------------------------------------
    // 1. Downtime imminent + 1 charge + burst imminent -> fires (dump wins)
    // -----------------------------------------------------------------------

    [Fact]
    public void Phlegma_FiresDespiteBurstHoldAndChargeConservation_WhenDowntimeImminent()
    {
        // Without downtime: burst hold fires (burst imminent, pooling on) AND
        // charge conservation fires (rechargingTime >= 5f). The downtime dump
        // must bypass BOTH gates and push Phlegma regardless.
        var burstSvc = new Mock<IBurstWindowService>();
        burstSvc.Setup(x => x.IsInBurstWindow).Returns(false);
        burstSvc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);

        var module = new DamageModule(burstSvc.Object);
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePhlegma = true;
        config.HealerShared.EnableBurstPooling = true;

        var enemy = MakeEnemy(42u);
        var targetingService = MockBuilders.CreateMockTargetingService();
        targetingService.Setup(x => x.FindEnemy(
                It.IsAny<EnemyTargetingStrategy>(),
                It.IsAny<float>(),
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(enemy.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.GetCurrentCharges(It.IsAny<uint>())).Returns(1u);
        // rechargingTime >= 5f = charge-conservation would fire without downtime dump
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(30f);

        var timeline = CreateDowntimeTimeline(5f);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            timelineService: timeline,
            level: 100,
            inCombat: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == SGEActions.PhlegmaIII.ActionId);
    }

    // -----------------------------------------------------------------------
    // 2. No timeline + burst imminent -> holds (charge conservation fires)
    // -----------------------------------------------------------------------

    [Fact]
    public void Phlegma_HeldByChargeConservation_WhenNoDowntimeAndBurstImminent()
    {
        // No timeline means ShouldDumpForDowntime = false. Burst hold fires.
        var burstSvc = new Mock<IBurstWindowService>();
        burstSvc.Setup(x => x.IsInBurstWindow).Returns(false);
        burstSvc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);

        var module = new DamageModule(burstSvc.Object);
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePhlegma = true;
        config.HealerShared.EnableBurstPooling = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.GetCurrentCharges(It.IsAny<uint>())).Returns(1u);
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(30f);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            timelineService: null, // no timeline
            level: 100,
            inCombat: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == SGEActions.PhlegmaIII.ActionId);
        // Burst hold fires first (before charge conservation)
        Assert.Equal("Phlegma held: burst imminent", context.Debug.PhlegmaState);
    }

    // -----------------------------------------------------------------------
    // 3. Downtime imminent + 0 charges -> does NOT fire (no charges)
    // -----------------------------------------------------------------------

    [Fact]
    public void Phlegma_NotFired_WhenDowntimeImminentButNoCharges()
    {
        var burstSvc = new Mock<IBurstWindowService>();
        burstSvc.Setup(x => x.IsInBurstWindow).Returns(false);
        burstSvc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(false);

        var module = new DamageModule(burstSvc.Object);
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnablePhlegma = true;
        config.HealerShared.EnableBurstPooling = true;

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true);
        actionService.Setup(x => x.GetCurrentCharges(It.IsAny<uint>())).Returns(0u); // no charges
        actionService.Setup(x => x.GetCooldownRemaining(It.IsAny<uint>())).Returns(20f);

        var timeline = CreateDowntimeTimeline(5f);

        var context = AsclepiusTestContext.Create(
            config: config,
            actionService: actionService,
            timelineService: timeline,
            level: 100,
            inCombat: true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior.Action.ActionId == SGEActions.PhlegmaIII.ActionId);
        Assert.Equal("No charges", context.Debug.PhlegmaState);
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

    private static Mock<IBattleNpc> MakeEnemy(uint id)
    {
        var m = new Mock<IBattleNpc>();
        m.Setup(x => x.GameObjectId).Returns((ulong)id);
        return m;
    }
}
