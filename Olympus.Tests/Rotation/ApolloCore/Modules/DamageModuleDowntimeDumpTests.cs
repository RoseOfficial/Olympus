using System;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Services;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;
using Xunit;
using DamageModule = Olympus.Rotation.ApolloCore.Modules.DamageModule;

namespace Olympus.Tests.Rotation.ApolloCore.Modules;

/// <summary>
/// Verifies that BurstHoldHelper.ShouldDumpForDowntime allows Afflatus Misery to fire
/// when burst hold would otherwise suppress it (B2 interplay rule).
/// </summary>
public sealed class DamageModuleDowntimeDumpTests : IDisposable
{
    public DamageModuleDowntimeDumpTests() => BurstHoldHelper.ModifierKeys = null;

    public void Dispose() => BurstHoldHelper.ModifierKeys = null;

    // -----------------------------------------------------------------------
    // 1. Burst imminent + downtime imminent + blood lily 3 -> fires (dump wins)
    // -----------------------------------------------------------------------

    [Fact]
    public void AfflatusMisery_FiresDespiteBurstHold_WhenDowntimeImminent()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new DamageModule(burstSvc.Object);

        var timeline = CreateDowntimeTimeline(5f);
        var (context, targeting, player) = BuildContext(
            bloodLilyCount: 3,
            lilyCount: 0,
            timelineService: timeline.Object);

        var cluster = MakeEnemy(42u);
        targeting.Setup(t => t.FindBestAoETarget(
                WHMActions.AfflatusMisery.Radius, WHMActions.AfflatusMisery.Range, player))
            .Returns((cluster.Object, 4));

        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.AfflatusMisery);
    }

    // -----------------------------------------------------------------------
    // 2. Burst imminent + no timeline + blood lily 3 -> holds (no dump escape)
    // -----------------------------------------------------------------------

    [Fact]
    public void AfflatusMisery_HeldForBurst_WhenNoTimeline()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new DamageModule(burstSvc.Object);

        var (context, _, _) = BuildContext(bloodLilyCount: 3, lilyCount: 0, timelineService: null);

        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.AfflatusMisery);
    }

    // -----------------------------------------------------------------------
    // 3. Burst imminent + downtime + EnableBurstPooling = false -> fires (pooling off)
    // -----------------------------------------------------------------------

    [Fact]
    public void AfflatusMisery_Fires_WhenBurstPoolingDisabled_EvenWithoutDowntime()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new DamageModule(burstSvc.Object);

        var (context, targeting, player) = BuildContext(bloodLilyCount: 3, lilyCount: 0, timelineService: null);
        context.Configuration.HealerShared.EnableBurstPooling = false;

        var cluster = MakeEnemy(42u);
        targeting.Setup(t => t.FindBestAoETarget(
                WHMActions.AfflatusMisery.Radius, WHMActions.AfflatusMisery.Range, player))
            .Returns((cluster.Object, 4));

        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        // Burst pooling off means hold is never applied -> fires immediately
        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.AfflatusMisery);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<IBurstWindowService> MakeBurst(bool isInBurst, bool isImminent)
    {
        var svc = new Mock<IBurstWindowService>();
        svc.Setup(b => b.IsInBurstWindow).Returns(isInBurst);
        svc.Setup(b => b.IsBurstImminent(It.IsAny<float>())).Returns(isImminent);
        svc.Setup(b => b.SecondsUntilNextBurst).Returns(5f);
        return svc;
    }

    private static Mock<IBattleNpc> MakeEnemy(uint id)
    {
        var m = new Mock<IBattleNpc>();
        m.Setup(x => x.EntityId).Returns(id);
        m.Setup(x => x.GameObjectId).Returns((ulong)id);
        m.Setup(x => x.StatusList).Returns(
            (Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return m;
    }

    private static Mock<ITimelineService> CreateDowntimeTimeline(float secondsUntil)
    {
        var tl = new Mock<ITimelineService>();
        tl.Setup(x => x.Confidence).Returns(1.0f);
        tl.Setup(x => x.SecondsUntilNextUntargetablePhase()).Returns((float?)secondsUntil);
        return tl;
    }

    private static (IApolloContext context, Mock<ITargetingService> targeting, Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter player)
        BuildContext(
            int bloodLilyCount = 3,
            int lilyCount = 0,
            ITimelineService? timelineService = null,
            byte playerLevel = 100)
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = true;
        config.Targeting.SuppressDamageOnForcedMovement = false;
        config.HealerShared.EnableBurstPooling = true;
        config.Damage.EnableAfflatusMisery = true;

        var player = MockBuilders.CreateMockPlayerCharacter(level: playerLevel);

        var targeting = new Mock<ITargetingService>();
        targeting.Setup(t => t.IsDamageTargetingPaused()).Returns(false);
        targeting
            .Setup(t => t.FindBestAoETarget(It.IsAny<float>(), It.IsAny<float>(), player.Object))
            .Returns((MakeEnemy(99u).Object, 1));
        targeting.Setup(t => t.CountEnemiesInRange(It.IsAny<float>(), player.Object)).Returns(1);
        targeting
            .Setup(t => t.FindEnemyNeedingDot(It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(), player.Object))
            .Returns<uint, float, float, Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>((_, _, _, _) => null);

        var ctx = new Mock<IApolloContext>();
        ctx.Setup(x => x.InCombat).Returns(true);
        ctx.Setup(x => x.Configuration).Returns(config);
        ctx.Setup(x => x.Player).Returns(player.Object);
        ctx.Setup(x => x.TargetingService).Returns(targeting.Object);
        ctx.Setup(x => x.Debug).Returns(new DebugState());
        ctx.Setup(x => x.LilyCount).Returns(lilyCount);
        ctx.Setup(x => x.BloodLilyCount).Returns(bloodLilyCount);
        ctx.Setup(x => x.SacredSightStacks).Returns(0);
        ctx.Setup(x => x.SacredSightRemaining).Returns(30f);
        ctx.Setup(x => x.HasSwiftcast).Returns(false);
        ctx.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        ctx.Setup(x => x.TimelineService).Returns(timelineService);

        return (ctx.Object, targeting, player.Object);
    }
}
