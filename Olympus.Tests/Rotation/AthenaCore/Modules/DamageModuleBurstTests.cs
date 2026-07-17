using System;
using System.Collections.Generic;
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
/// Covers Chain Stratagem hold, Aetherflow hold, the raidwide-banking
/// override, tank-buster banking, and the emergency-HP escape.
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

    /// <summary>
    /// Returns a mocked ITimelineService that predicts a tank buster within
    /// <paramref name="secondsUntil"/> seconds at full confidence.
    /// </summary>
    private static Mock<ITimelineService> TankBusterTimeline(float secondsUntil = 2f)
    {
        var prediction = new MechanicPrediction(secondsUntil, TimelineEntryType.TankBuster, "TestTankBuster", 1f);
        var m = new Mock<ITimelineService>();
        m.Setup(s => s.IsActive).Returns(true);
        m.Setup(s => s.Confidence).Returns(1f);
        m.Setup(s => s.NextTankBuster).Returns((MechanicPrediction?)prediction);
        return m;
    }

    private static Mock<IBattleNpc> MakeEnemy()
    {
        var e = new Mock<IBattleNpc>();
        e.Setup(x => x.GameObjectId).Returns(9999ul);
        return e;
    }

    /// <summary>
    /// A party helper containing one member at 40% HP — below the default
    /// OgcdEmergencyThreshold of 0.50f, so CalculatePartyHealthMetrics
    /// returns lowestHpPercent = 0.40f and triggers the emergency escape.
    /// </summary>
    private static TestableAthenaPartyHelper LowHpParty(Configuration? config = null)
    {
        var member = MockBuilders.CreateMockBattleChara(
            entityId: 1u, currentHp: 20000, maxHp: 50000); // 40%
        return new TestableAthenaPartyHelper(new List<IBattleChara> { member.Object }, config);
    }

    // ── Chain Stratagem hold ──────────────────────────────────────

    [Fact]
    public void ChainStratagem_NotPushed_WhenBurstImminentAndPoolingEnabled()
    {
        // Enemy is provided so FindEnemy returns non-null: the hold is the
        // only variable discriminating this test from the positive case below.
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

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.ChainStratagem.ActionId);
    }

    [Fact]
    public void ChainStratagem_Pushed_WhenPoolingDisabled()
    {
        // Same setup as the negative test above, but pooling is off:
        // Chain Stratagem must be pushed despite burst being imminent.
        var burstSvc = BurstSvc(inBurst: false, imminent: true);
        var module = new DamageModule(burstSvc.Object);

        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.HealerShared.EnableBurstPooling = false;
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

    // ── Aetherflow hold and banking ───────────────────────────────

    [Fact]
    public void Aetherflow_NotPushed_WhenBurstImminentAndNoRaidwide()
    {
        // Stacks == 0, burst imminent, no threat, healthy party: hold fires.
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
        // no timelineService: TimelineHelper.IsRaidwideImminent and
        // IsTankBusterImminent both return false
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
        // Stacks == 0, burst imminent, raidwide imminent: banking branch pushes.
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

    [Fact]
    public void Aetherflow_Pushed_WhenRaidwideImminentAtOneStack()
    {
        // Stacks == 1 + raidwide imminent: banking fires so SCH goes into the
        // raidwide with 3 stacks instead of 1. RED against the old code
        // that exited at "stacks > 0" before reaching the banking check.
        var burstSvc = BurstSvc(inBurst: false, imminent: true);
        var module = new DamageModule(burstSvc.Object);

        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.HealerShared.EnableBurstPooling = true;
        config.Timeline.EnableTimelinePredictions = true;
        config.Timeline.TimelineConfidenceThreshold = 0.8f;
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
            aetherflowStacks: 1); // <-- the key: 1 stack in hand

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.Aetherflow.ActionId
                 && c.Priority == 295);
    }

    [Fact]
    public void Aetherflow_Pushed_WhenTankBusterImminentAtOneStack()
    {
        // Stacks == 1 + tank buster imminent: banking fires so SCH goes into the
        // tank buster with 3 stacks instead of 1. RED against the old code.
        var burstSvc = BurstSvc(inBurst: false, imminent: true);
        var module = new DamageModule(burstSvc.Object);

        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.HealerShared.EnableBurstPooling = true;
        config.Timeline.EnableTimelinePredictions = true;
        config.Timeline.TimelineConfidenceThreshold = 0.8f;
        // TankBusterPreparationWindow defaults to 3f; 2f < 3f so it fires
        config.Scholar.EnableAetherflow = true;
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableBanefulImpaction = false;
        config.Scholar.EnableEnergyDrain = false;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableRuinII = false;

        var actionService = MockBuilders.CreateMockActionService();
        var timelineService = TankBusterTimeline(secondsUntil: 2f);
        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            timelineService: timelineService,
            level: 100,
            inCombat: true,
            aetherflowStacks: 1); // <-- the key: 1 stack in hand

        var scheduler = SchedulerFactory.CreateForTest(actionService, config: config);
        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior.Action.ActionId == SCHActions.Aetherflow.ActionId
                 && c.Priority == 295);
    }

    [Fact]
    public void Aetherflow_Pushed_WhenBurstImminentAndTankBusterImminent()
    {
        // Stacks == 0 + burst imminent + tank buster imminent:
        // banking branch fires (stacks < 2 && tankBuster) so the hold is bypassed.
        var burstSvc = BurstSvc(inBurst: false, imminent: true);
        var module = new DamageModule(burstSvc.Object);

        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.HealerShared.EnableBurstPooling = true;
        config.Timeline.EnableTimelinePredictions = true;
        config.Timeline.TimelineConfidenceThreshold = 0.8f;
        config.Scholar.EnableAetherflow = true;
        config.Scholar.EnableChainStratagem = false;
        config.Scholar.EnableBanefulImpaction = false;
        config.Scholar.EnableEnergyDrain = false;
        config.Scholar.EnableSingleTargetDamage = false;
        config.Scholar.EnableAoEDamage = false;
        config.Scholar.EnableDot = false;
        config.Scholar.EnableRuinII = false;

        var actionService = MockBuilders.CreateMockActionService();
        var timelineService = TankBusterTimeline(secondsUntil: 2f);
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

    [Fact]
    public void Aetherflow_Pushed_WhenBurstImminentAndEmergencyLowHp()
    {
        // Stacks == 0 + burst imminent + no timeline threats + party member at
        // 40% HP (below default OgcdEmergencyThreshold 0.50f): emergency escape
        // bypasses the hold so Aetherflow is available for immediate healing.
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
        var partyHelper = LowHpParty(config);
        // no timelineService: raidwideImminent and tankBusterImminent are false
        var context = AthenaTestContext.Create(
            config: config,
            actionService: actionService,
            partyHelper: partyHelper,
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
