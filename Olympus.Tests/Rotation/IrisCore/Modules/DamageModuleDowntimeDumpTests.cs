using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.IrisCore.Abilities;
using Olympus.Rotation.IrisCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;

namespace Olympus.Tests.Rotation.IrisCore.Modules;

/// <summary>
/// Verifies that BurstHoldHelper.ShouldDumpForDowntime allows Holy in White to fire
/// at 4 paint before a boss untargetable phase, bypassing the PaletteGauge threshold.
/// </summary>
public class DamageModuleDowntimeDumpTests
{
    // -----------------------------------------------------------------------
    // 1. Downtime imminent + paint >= 4 + low palette gauge -> fires (dump wins)
    // -----------------------------------------------------------------------

    [Fact]
    public void HolyInWhite_FiresDespitePaletteThreshold_WhenDowntimeImminentAndPaint4()
    {
        var module = new DamageModule();

        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.EnableHolyInWhite = true;
        config.Pictomancer.HolyMinPalette = 50; // palette 0 < 50, would normally gate

        var enemy = CreateEnemy(42u);
        var targeting = CreateTargetingWith(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = IrisTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            timelineService: timeline.Object,
            whitePaint: 4,
            hasWhitePaint: true,
            paletteGauge: 0,           // below HolyMinPalette (50)
            isInBurstWindow: false,
            hasStarstruck: false,       // suppress StarPrism
            hasRainbowBright: false,    // suppress RainbowDrip
            isInHammerCombo: false,     // suppress HammerCombo
            hasBlackPaint: false,       // suppress CometInBlack
            hasInspiration: false,      // suppress MotifDuringInspiration
            inCombat: true,
            level: 100);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == IrisAbilities.HolyInWhite);
    }

    // -----------------------------------------------------------------------
    // 2. No downtime + paint = 4 + palette gauge = 0 -> does NOT fire
    // -----------------------------------------------------------------------

    [Fact]
    public void HolyInWhite_NotFired_WhenNoDowntimeAndPaletteBelowThreshold()
    {
        var module = new DamageModule();

        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.EnableHolyInWhite = true;
        config.Pictomancer.HolyMinPalette = 50;

        var enemy = CreateEnemy(42u);
        var targeting = CreateTargetingWith(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = IrisTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            timelineService: null, // no timeline
            whitePaint: 4,
            hasWhitePaint: true,
            paletteGauge: 0,
            isInBurstWindow: false,
            hasStarstruck: false,
            hasRainbowBright: false,
            isInHammerCombo: false,
            hasBlackPaint: false,
            hasInspiration: false,
            inCombat: true,
            level: 100);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == IrisAbilities.HolyInWhite);
    }

    // -----------------------------------------------------------------------
    // 3. Downtime imminent + paint < 4 -> does NOT fire (dump threshold not met)
    // -----------------------------------------------------------------------

    [Fact]
    public void HolyInWhite_NotFired_WhenDowntimeImminentButPaintBelow4()
    {
        var module = new DamageModule();

        var config = IrisTestContext.CreateDefaultPctConfiguration();
        config.Pictomancer.EnableHolyInWhite = true;
        config.Pictomancer.HolyMinPalette = 50;

        var enemy = CreateEnemy(42u);
        var targeting = CreateTargetingWith(enemy.Object);
        var actionService = MockBuilders.CreateMockActionService();

        var timeline = CreateDowntimeTimeline(5f);
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = IrisTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            timelineService: timeline.Object,
            whitePaint: 3, // below dump threshold of 4
            hasWhitePaint: true,
            paletteGauge: 0,
            isInBurstWindow: false,
            hasStarstruck: false,
            hasRainbowBright: false,
            isInHammerCombo: false,
            hasBlackPaint: false,
            hasInspiration: false,
            inCombat: true,
            level: 100);

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == IrisAbilities.HolyInWhite);
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
        return t;
    }
}
