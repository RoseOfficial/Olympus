using System;
using Dalamud.Game.ClientState.Objects.SubKinds;
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
using Xunit;
using DamageModule = Olympus.Rotation.ApolloCore.Modules.DamageModule;

namespace Olympus.Tests.Rotation.ApolloCore.Modules;

/// <summary>
/// Burst-alignment tests for WHM DamageModule:
///   Misery burst pooling, movement/lily-overcap escapes, dynamic priority,
///   and Glare IV expiry escape.
/// </summary>
public class DamageModuleBurstTests : IDisposable
{
    public void Dispose() => BurstHoldHelper.ModifierKeys = null;

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

    /// <summary>
    /// Builds a minimal IApolloContext mock for DamageModule burst tests.
    /// bloodLilyCount: must be 3 for Misery to pass the lily guard.
    /// lilyCount: white lily count; >= 3 triggers the overcap escape.
    /// sacredSightStacks/sacredSightRemaining: controls Glare IV path.
    /// </summary>
    private static (IApolloContext context, Mock<ITargetingService> targeting, IPlayerCharacter player)
        BuildContext(
            int bloodLilyCount = 3,
            int lilyCount = 0,
            int sacredSightStacks = 0,
            float sacredSightRemaining = 30f,
            byte playerLevel = 100)
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = true;
        config.Targeting.SuppressDamageOnForcedMovement = false;

        var player = MockBuilders.CreateMockPlayerCharacter(level: playerLevel);

        var targeting = new Mock<ITargetingService>();
        targeting.Setup(t => t.IsDamageTargetingPaused()).Returns(false);
        // Wire up a generic enemy for the AoE target calls.
        targeting
            .Setup(t => t.FindBestAoETarget(It.IsAny<float>(), It.IsAny<float>(), player.Object))
            .Returns((MakeEnemy(99u).Object, 1));
        targeting.Setup(t => t.CountEnemiesInRange(It.IsAny<float>(), player.Object)).Returns(1);
        targeting
            .Setup(t => t.FindEnemyNeedingDot(It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(), player.Object))
            .Returns<uint, float, float, IPlayerCharacter>((_, _, _, _) => null);

        var ctx = new Mock<IApolloContext>();
        ctx.Setup(x => x.InCombat).Returns(true);
        ctx.Setup(x => x.Configuration).Returns(config);
        ctx.Setup(x => x.Player).Returns(player.Object);
        ctx.Setup(x => x.TargetingService).Returns(targeting.Object);
        ctx.Setup(x => x.Debug).Returns(new DebugState());
        ctx.Setup(x => x.LilyCount).Returns(lilyCount);
        ctx.Setup(x => x.BloodLilyCount).Returns(bloodLilyCount);
        ctx.Setup(x => x.SacredSightStacks).Returns(sacredSightStacks);
        ctx.Setup(x => x.SacredSightRemaining).Returns(sacredSightRemaining);
        ctx.Setup(x => x.HasSwiftcast).Returns(false);
        ctx.Setup(x => x.TrainingService).Returns((ITrainingService?)null);

        return (ctx.Object, targeting, player.Object);
    }

    // -----------------------------------------------------------------------
    // Misery: in burst -> priority 295
    // -----------------------------------------------------------------------
    [Fact]
    public void Misery_InBurst_Priority295()
    {
        var burstSvc = MakeBurst(isInBurst: true, isImminent: false);
        var module = new DamageModule(burstSvc.Object);
        var (context, targeting, player) = BuildContext(bloodLilyCount: 3, lilyCount: 0);
        var cluster = MakeEnemy(42u);
        targeting
            .Setup(t => t.FindBestAoETarget(
                WHMActions.AfflatusMisery.Radius, WHMActions.AfflatusMisery.Range, player))
            .Returns((cluster.Object, 4));

        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c =>
            c.Behavior == ApolloAbilities.AfflatusMisery && c.Priority == 295);
    }

    // -----------------------------------------------------------------------
    // Misery: burst imminent, 0 white lilies, not moving -> held
    // -----------------------------------------------------------------------
    [Fact]
    public void Misery_BurstImminent_ZeroWhiteLilies_Held()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new DamageModule(burstSvc.Object);
        var (context, _, _) = BuildContext(bloodLilyCount: 3, lilyCount: 0);

        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.AfflatusMisery);
    }

    // -----------------------------------------------------------------------
    // Misery: burst imminent BUT 3 white lilies (overcap risk) -> fires at 300
    // -----------------------------------------------------------------------
    [Fact]
    public void Misery_BurstImminent_ThreeWhiteLilies_Fires()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new DamageModule(burstSvc.Object);
        var (context, targeting, player) = BuildContext(bloodLilyCount: 3, lilyCount: 3);
        var cluster = MakeEnemy(42u);
        targeting
            .Setup(t => t.FindBestAoETarget(
                WHMActions.AfflatusMisery.Radius, WHMActions.AfflatusMisery.Range, player))
            .Returns((cluster.Object, 4));

        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c =>
            c.Behavior == ApolloAbilities.AfflatusMisery && c.Priority == 300);
    }

    // -----------------------------------------------------------------------
    // Misery: burst imminent AND moving -> fires at 300 (instant; use while moving)
    // -----------------------------------------------------------------------
    [Fact]
    public void Misery_BurstImminent_Moving_Fires()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new DamageModule(burstSvc.Object);
        var (context, targeting, player) = BuildContext(bloodLilyCount: 3, lilyCount: 0);
        var cluster = MakeEnemy(42u);
        targeting
            .Setup(t => t.FindBestAoETarget(
                WHMActions.AfflatusMisery.Radius, WHMActions.AfflatusMisery.Range, player))
            .Returns((cluster.Object, 4));

        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: true);

        Assert.Contains(scheduler.InspectGcdQueue(), c =>
            c.Behavior == ApolloAbilities.AfflatusMisery && c.Priority == 300);
    }

    // -----------------------------------------------------------------------
    // Misery: no burst service -> fires at 300 (null service, backward compat)
    // -----------------------------------------------------------------------
    [Fact]
    public void Misery_NoBurstService_FiresAt300()
    {
        var module = new DamageModule(); // no burst service
        var (context, targeting, player) = BuildContext(bloodLilyCount: 3, lilyCount: 0);
        var cluster = MakeEnemy(42u);
        targeting
            .Setup(t => t.FindBestAoETarget(
                WHMActions.AfflatusMisery.Radius, WHMActions.AfflatusMisery.Range, player))
            .Returns((cluster.Object, 4));

        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c =>
            c.Behavior == ApolloAbilities.AfflatusMisery && c.Priority == 300);
    }

    // -----------------------------------------------------------------------
    // Misery: EnableBurstPooling = false -> fires even when burst imminent
    // -----------------------------------------------------------------------
    [Fact]
    public void Misery_BurstImminent_BurstPoolingDisabled_Fires()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new DamageModule(burstSvc.Object);
        var (context, targeting, player) = BuildContext(bloodLilyCount: 3, lilyCount: 0);
        var cluster = MakeEnemy(42u);
        targeting
            .Setup(t => t.FindBestAoETarget(
                WHMActions.AfflatusMisery.Radius, WHMActions.AfflatusMisery.Range, player))
            .Returns((cluster.Object, 4));
        // Disable burst pooling on the config the context returns
        context.Configuration.HealerShared.EnableBurstPooling = false;

        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(), c =>
            c.Behavior == ApolloAbilities.AfflatusMisery);
    }

    // -----------------------------------------------------------------------
    // Glare IV: burst imminent BUT Sacred Sight almost expired -> fires anyway
    // -----------------------------------------------------------------------
    [Fact]
    public void GlareIV_BurstImminent_SacredSightExpiring_Fires()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new DamageModule(burstSvc.Object);
        // sacredSightRemaining < 2.5f threshold -> expiry escape triggers
        var (context, targeting, player) = BuildContext(
            bloodLilyCount: 0,
            sacredSightStacks: 2,
            sacredSightRemaining: 2.0f);
        var aoeTarget = MakeEnemy(7u);
        targeting
            .Setup(t => t.FindBestAoETarget(
                WHMActions.GlareIV.Radius, WHMActions.GlareIV.Range, player))
            .Returns((aoeTarget.Object, 1));

        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.GlareIV);
    }

    // -----------------------------------------------------------------------
    // Glare IV: burst imminent AND stacks have plenty of time -> held
    // -----------------------------------------------------------------------
    [Fact]
    public void GlareIV_BurstImminent_SacredSightStillFresh_Held()
    {
        var burstSvc = MakeBurst(isInBurst: false, isImminent: true);
        var module = new DamageModule(burstSvc.Object);
        var (context, _, _) = BuildContext(
            bloodLilyCount: 0,
            sacredSightStacks: 2,
            sacredSightRemaining: 20f);  // well above 2.5f - no escape

        var scheduler = SchedulerFactory.CreateForTest();

        module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.GlareIV);
    }
}
