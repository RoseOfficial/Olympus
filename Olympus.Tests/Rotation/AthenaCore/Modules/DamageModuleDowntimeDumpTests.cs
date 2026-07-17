using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Config;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Services;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;
using Olympus.Rotation.Common.Helpers;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

/// <summary>
/// Verifies that BurstHoldHelper.ShouldDumpForDowntime forces Energy Drain
/// before a boss untargetable phase regardless of AetherflowStrategy, bypassing
/// the conservative (HealingPriority) gate.
/// </summary>
public sealed class DamageModuleDowntimeDumpTests : IDisposable
{
    public DamageModuleDowntimeDumpTests() => BurstHoldHelper.ModifierKeys = null;

    public void Dispose() => BurstHoldHelper.ModifierKeys = null;

    // -----------------------------------------------------------------------
    // 1. Downtime imminent + HealingPriority strategy + 1 stack -> fires
    // -----------------------------------------------------------------------

    [Fact]
    public void EnergyDrain_FiresDespiteConservativeStrategy_WhenDowntimeImminent()
    {
        // HealingPriority strategy would normally not drain at 1 stack (only drains
        // at 3 stacks when Aetherflow is about to cap). Downtime dump overrides.
        var burstSvc = new Mock<IBurstWindowService>();
        burstSvc.Setup(x => x.IsInBurstWindow).Returns(false);
        burstSvc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(false);

        var module = new DamageModule(burstSvc.Object);

        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableEnergyDrain = true;
        config.Scholar.AetherflowStrategy = AetherflowUsageStrategy.HealingPriority;
        // Disable all other damage to isolate Energy Drain
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableBanefulImpaction = false;
        config.Scholar.EnableAetherflow = false;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableRuinII = false;

        var enemy = MakeEnemy(42u);
        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        var actionService = MockBuilders.CreateMockActionService();
        // Energy Drain needs to be ready
        actionService.Setup(x => x.IsActionReady(SCHActions.EnergyDrain.ActionId)).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            timelineService: timeline,
            level: 100,
            inCombat: true,
            aetherflowStacks: 1);

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.EnergyDrain && c.Priority == 290);
    }

    // -----------------------------------------------------------------------
    // 2. No timeline + HealingPriority + 1 stack -> holds (no dump escape)
    // -----------------------------------------------------------------------

    [Fact]
    public void EnergyDrain_NotPushed_WhenNoDowntimeAndConservativeStrategy()
    {
        var burstSvc = new Mock<IBurstWindowService>();
        burstSvc.Setup(x => x.IsInBurstWindow).Returns(false);
        burstSvc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(false);

        var module = new DamageModule(burstSvc.Object);

        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableEnergyDrain = true;
        config.Scholar.AetherflowStrategy = AetherflowUsageStrategy.HealingPriority;
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableBanefulImpaction = false;
        config.Scholar.EnableAetherflow = false;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableRuinII = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(SCHActions.EnergyDrain.ActionId)).Returns(true);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            timelineService: null, // no timeline -> ShouldDumpForDowntime = false
            level: 100,
            inCombat: true,
            aetherflowStacks: 1);

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.EnergyDrain);
    }

    // -----------------------------------------------------------------------
    // 3. Downtime imminent + 0 stacks -> does NOT fire (nothing to drain)
    // -----------------------------------------------------------------------

    [Fact]
    public void EnergyDrain_NotFired_WhenDowntimeImminentButNoStacks()
    {
        var burstSvc = new Mock<IBurstWindowService>();
        burstSvc.Setup(x => x.IsInBurstWindow).Returns(false);
        burstSvc.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(false);

        var module = new DamageModule(burstSvc.Object);

        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableEnergyDrain = true;
        config.Scholar.AetherflowStrategy = AetherflowUsageStrategy.HealingPriority;
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableBanefulImpaction = false;
        config.Scholar.EnableAetherflow = false;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableRuinII = false;

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(SCHActions.EnergyDrain.ActionId)).Returns(true);

        var timeline = CreateDowntimeTimeline(5f);

        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            timelineService: timeline,
            level: 100,
            inCombat: true,
            aetherflowStacks: 0); // no stacks to drain

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.EnergyDrain);
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
