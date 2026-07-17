using System;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AthenaCore.Modules;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;
using Olympus.Timeline.Models;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules;

/// <summary>
/// Tests for burst-hold behavior in SCH DamageModule.
/// Covers Chain Stratagem hold, Aetherflow hold, and the raidwide-banking
/// override that fires Aetherflow even when burst is imminent.
/// </summary>
public sealed class DamageModuleBurstTests : IDisposable
{
    // ── lifecycle ────────────────────────────────────────────────

    public DamageModuleBurstTests()
    {
        BurstHoldHelper.ModifierKeys = null;
    }

    public void Dispose()
    {
        BurstHoldHelper.ModifierKeys = null;
    }

    // ── helpers ───────────────────────────────────────────────────

    private static Mock<IBurstWindowService> BurstSvc(bool inBurst, bool imminent)
    {
        var m = new Mock<IBurstWindowService>();
        m.Setup(s => s.IsInBurstWindow).Returns(inBurst);
        m.Setup(s => s.IsBurstImminent(It.IsAny<float>())).Returns(imminent);
        return m;
    }

    /// <summary>
    /// Returns a mocked ITimelineService that predicts a raidwide within
    /// <paramref name="secondsUntil"/> seconds at full confidence.
    /// </summary>
    private static Mock<ITimelineService> RaidwideTimeline(float secondsUntil = 3f)
    {
        var prediction = new MechanicPrediction(secondsUntil, TimelineEntryType.Raidwide, "TestRaidwide", 1f);
        var m = new Mock<ITimelineService>();
        m.Setup(s => s.IsActive).Returns(true);
        m.Setup(s => s.Confidence).Returns(1f);
        m.Setup(s => s.NextRaidwide).Returns((MechanicPrediction?)prediction);
        return m;
    }

    private static Mock<IBattleNpc> MakeEnemy()
    {
        var e = new Mock<IBattleNpc>();
        e.Setup(x => x.GameObjectId).Returns(9999ul);
        return e;
    }

    // ── Chain Stratagem hold ──────────────────────────────────────

    [Fact]
    public void ChainStratagem_NotPushed_WhenBurstImminentAndPoolingEnabled()
    {
        var burstSvc = BurstSvc(inBurst: false, imminent: true);
        var module = new DamageModule(burstSvc.Object);

        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.HealerShared.EnableBurstPooling = true;
        config.Scholar.EnableChainStratagem = true;
        config.Scholar.EnableBanefulImpaction = false;
        config.Scholar.EnableEnergyDrain = false;
        config.Scholar.EnableAetherflow = false;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableRuinII = false;

        var actionService = MockBuilders.CreateMockActionService();
        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            aetherflowStacks: 0);

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.ChainStratagem.ActionId);
    }

    [Fact]
    public void ChainStratagem_Pushed_WhenBurstNotImminent()
    {
        var burstSvc = BurstSvc(inBurst: false, imminent: false);
        var module = new DamageModule(burstSvc.Object);

        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.HealerShared.EnableBurstPooling = true;
        config.Scholar.EnableChainStratagem = true;
        config.Scholar.EnableBanefulImpaction = false;
        config.Scholar.EnableEnergyDrain = false;
        config.Scholar.EnableAetherflow = false;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableRuinII = false;

        var enemy = MakeEnemy();
        var targetingService = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();
        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targetingService,
            level: 100,
            inCombat: true,
            aetherflowStacks: 0);

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.ChainStratagem.ActionId
                 && c.Priority == 285);
    }

    // ── Aetherflow hold ───────────────────────────────────────────

    [Fact]
    public void Aetherflow_NotPushed_WhenBurstImminentAndNoRaidwide()
    {
        var burstSvc = BurstSvc(inBurst: false, imminent: true);
        var module = new DamageModule(burstSvc.Object);

        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.HealerShared.EnableBurstPooling = true;
        config.Scholar.EnableAetherflow = true;
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableBanefulImpaction = false;
        config.Scholar.EnableEnergyDrain = false;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableRuinII = false;

        var actionService = MockBuilders.CreateMockActionService();
        // no timelineService: TimelineHelper.IsRaidwideImminent returns false
        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            inCombat: true,
            aetherflowStacks: 0);

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.Aetherflow.ActionId);
    }

    [Fact]
    public void Aetherflow_Pushed_DespiteBurstHold_WhenRaidwideImminent()
    {
        var burstSvc = BurstSvc(inBurst: false, imminent: true);
        var module = new DamageModule(burstSvc.Object);

        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.HealerShared.EnableBurstPooling = true;
        config.Timeline.EnableTimelinePredictions = true;
        config.Timeline.TimelineConfidenceThreshold = 0.8f;
        // RaidwidePreparationWindow defaults to 5f; 3f < 5f so the raidwide fires the override
        config.Scholar.EnableAetherflow = true;
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableBanefulImpaction = false;
        config.Scholar.EnableEnergyDrain = false;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableRuinII = false;

        var actionService = MockBuilders.CreateMockActionService();
        var timelineService = RaidwideTimeline(secondsUntil: 3f);
        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            timelineService: timelineService,
            level: 100,
            inCombat: true,
            aetherflowStacks: 0);

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.Aetherflow.ActionId
                 && c.Priority == 295);
    }
}
