using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.Common;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;
using DamageModule = Olympus.Rotation.ApolloCore.Modules.DamageModule;

namespace Olympus.Tests.Rotation.ApolloCore.Modules;

/// <summary>
/// Tests for the WHM DamageModule scheduler pushes: Afflatus Misery cluster
/// targeting, the DoT/Misery priority ordering, and the moving-in-a-pack
/// instant-DoT movement filler.
///
/// GCD queue priorities under test: DoT 298 (single-target) &lt; Misery 300 &lt;
/// Glare IV 305 &lt; DoT 315 (moving in a pack) &lt; Holy 320 &lt; filler 330.
/// </summary>
public class DamageModuleTests
{
    private readonly DamageModule _module = new();

    // -----------------------------------------------------------------------
    // 1. Blood lily 3/3 → Misery pushed at 300, targeted via FindBestAoETarget
    //    (densest cluster), not the generic enemy strategy
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_BloodLilyFull_PushesMiseryAtDensestCluster()
    {
        var (context, targeting, player) = BuildContext(bloodLilyCount: 3);
        var cluster = CreateEnemy(42u);
        targeting
            .Setup(t => t.FindBestAoETarget(
                WHMActions.AfflatusMisery.Radius, WHMActions.AfflatusMisery.Range, player))
            .Returns((cluster.Object, 4));

        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectGcdQueue();
        Assert.Contains(queue, c =>
            c.Behavior == ApolloAbilities.AfflatusMisery &&
            c.Priority == 300 &&
            c.TargetId == cluster.Object.GameObjectId);
        targeting.Verify(t => t.FindBestAoETarget(
            WHMActions.AfflatusMisery.Radius, WHMActions.AfflatusMisery.Range, player), Times.Once);
    }

    // -----------------------------------------------------------------------
    // 2. Single target, DoT due → DoT pushed at 298, ahead of Misery's 300
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_SingleTarget_DotDue_PushesDotAheadOfMisery()
    {
        var (context, targeting, player) = BuildContext(bloodLilyCount: 3);
        var enemy = CreateEnemy(7u);
        targeting.Setup(t => t.CountEnemiesInRange(It.IsAny<float>(), player)).Returns(1);
        targeting
            .Setup(t => t.FindEnemyNeedingDot(It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(), player))
            .Returns(enemy.Object);
        targeting
            .Setup(t => t.FindBestAoETarget(It.IsAny<float>(), It.IsAny<float>(), player))
            .Returns((enemy.Object, 1));

        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectGcdQueue();
        Assert.Contains(queue, c =>
            c.Behavior.Action.ActionId == WHMActions.Dia.ActionId &&
            c.Priority == 298 &&
            c.TargetId == enemy.Object.GameObjectId);
        Assert.Contains(queue, c =>
            c.Behavior == ApolloAbilities.AfflatusMisery && c.Priority == 300);
    }

    // -----------------------------------------------------------------------
    // 3. Moving through a 3+ pack → instant DoT pushed at 315 as movement
    //    filler (behind Misery/Glare IV, ahead of the uncastable Holy at 320)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_MovingInPack_PushesDotAsMovementFiller()
    {
        var (context, targeting, player) = BuildContext();
        var enemy = CreateEnemy(7u);
        targeting.Setup(t => t.CountEnemiesInRange(It.IsAny<float>(), player)).Returns(5);
        targeting
            .Setup(t => t.FindEnemyNeedingDot(It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(), player))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: true);

        var queue = scheduler.InspectGcdQueue();
        Assert.Contains(queue, c =>
            c.Behavior.Action.ActionId == WHMActions.Dia.ActionId &&
            c.Priority == 315 &&
            c.TargetId == enemy.Object.GameObjectId);
    }

    // -----------------------------------------------------------------------
    // 4. Stationary in a 3+ pack → DoT skipped entirely, Holy owns the GCD
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_StationaryInPack_SkipsDot_PushesHoly()
    {
        var (context, targeting, player) = BuildContext();
        var enemy = CreateEnemy(7u);
        targeting.Setup(t => t.CountEnemiesInRange(It.IsAny<float>(), player)).Returns(5);
        targeting
            .Setup(t => t.FindEnemyNeedingDot(It.IsAny<uint>(), It.IsAny<float>(), It.IsAny<float>(), player))
            .Returns(enemy.Object);

        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(queue, c => c.Behavior.Action.ActionId == WHMActions.Dia.ActionId);
        Assert.Contains(queue, c =>
            c.Behavior.Action.ActionId == WHMActions.HolyIII.ActionId &&
            c.Priority == 320);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<IBattleNpc> CreateEnemy(uint entityId)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.EntityId).Returns(entityId);
        mock.Setup(x => x.GameObjectId).Returns((ulong)entityId);
        mock.Setup(x => x.StatusList).Returns(
            (Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return mock;
    }

    private static (IApolloContext context, Mock<ITargetingService> targeting, IPlayerCharacter player)
        BuildContext(int bloodLilyCount = 0, int sacredSightStacks = 0, byte playerLevel = 100)
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableDamage = true;
        config.EnableDoT = true;
        config.Targeting.SuppressDamageOnForcedMovement = false;

        var player = MockBuilders.CreateMockPlayerCharacter(level: playerLevel);

        var targeting = new Mock<ITargetingService>();
        targeting.Setup(t => t.IsDamageTargetingPaused()).Returns(false);

        var ctx = new Mock<IApolloContext>();
        ctx.Setup(x => x.InCombat).Returns(true);
        ctx.Setup(x => x.Configuration).Returns(config);
        ctx.Setup(x => x.Player).Returns(player.Object);
        ctx.Setup(x => x.TargetingService).Returns(targeting.Object);
        ctx.Setup(x => x.Debug).Returns(new DebugState());
        ctx.Setup(x => x.LilyCount).Returns(0);
        ctx.Setup(x => x.BloodLilyCount).Returns(bloodLilyCount);
        ctx.Setup(x => x.SacredSightStacks).Returns(sacredSightStacks);
        ctx.Setup(x => x.HasSwiftcast).Returns(false);
        ctx.Setup(x => x.TrainingService).Returns((ITrainingService?)null);

        return (ctx.Object, targeting, player.Object);
    }
}
