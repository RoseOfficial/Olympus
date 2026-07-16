using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules.Healing;
using Olympus.Rotation.Common;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Stats;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Tests for BloodLilyBuildingHandler.CollectCandidates.
///
/// The handler's job is building the THIRD blood lily when at exactly 2 stacks.
/// At 3 stacks building is complete: pushing a lily heal there would outrank the
/// pending Afflatus Misery (priority 60 vs 300) while returning no gauge, so the
/// handler must stand down.
/// </summary>
public class BloodLilyBuildingHandlerTests
{
    private readonly BloodLilyBuildingHandler _handler = new();

    // -----------------------------------------------------------------------
    // 1. Blood lily 2/3, injured target below threshold → Solace pushed at 60
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_TwoBloodLilies_InjuredTarget_PushesSolace()
    {
        var target = CreateTarget(entityId: 8u);
        var partyHelper = CreatePartyHelper(target.Object, targetHpPercent: 0.5f);

        var context = BuildContext(
            lilyCount: 2,
            bloodLilyCount: 2,
            playerLevel: 80,
            partyHelper: partyHelper);

        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectGcdQueue();
        Assert.Contains(queue, c =>
            c.Behavior == ApolloAbilities.AfflatusSolace &&
            c.Priority == (int)HealingPriority.BloodLilyBuilding &&
            c.TargetId == target.Object.GameObjectId);
    }

    // -----------------------------------------------------------------------
    // 2. Blood lily 3/3 → building complete, handler stands down (Misery's slot)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ThreeBloodLilies_NoPush()
    {
        var target = CreateTarget(entityId: 8u);
        var partyHelper = CreatePartyHelper(target.Object, targetHpPercent: 0.5f);

        var context = BuildContext(
            lilyCount: 2,
            bloodLilyCount: 3,
            playerLevel: 80,
            partyHelper: partyHelper);

        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
    }

    // -----------------------------------------------------------------------
    // 3. Blood lily 1/3 → below the building threshold, nothing pushed
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_OneBloodLily_NoPush()
    {
        var target = CreateTarget(entityId: 8u);
        var partyHelper = CreatePartyHelper(target.Object, targetHpPercent: 0.5f);

        var context = BuildContext(
            lilyCount: 2,
            bloodLilyCount: 1,
            playerLevel: 80,
            partyHelper: partyHelper);

        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<IBattleChara> CreateTarget(uint entityId)
    {
        var mock = new Mock<IBattleChara>();
        mock.Setup(x => x.EntityId).Returns(entityId);
        mock.Setup(x => x.GameObjectId).Returns((ulong)entityId);
        mock.Setup(x => x.CurrentHp).Returns(25000u);
        mock.Setup(x => x.MaxHp).Returns(50000u);
        mock.Setup(x => x.Position).Returns(Vector3.Zero);
        mock.Setup(x => x.StatusList).Returns(
            (Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return mock;
    }

    private static Mock<IPartyHelper> CreatePartyHelper(IBattleChara lowestHpMember, float targetHpPercent)
    {
        var mock = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowestHpMember);
        mock.Setup(p => p.GetHpPercent(It.IsAny<IBattleChara>())).Returns(targetHpPercent);
        mock.Setup(p => p.CountInjuredInAoERange(
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
                It.IsAny<float>(),
                It.IsAny<float>()))
            .Returns(0); // < 2 → Rapture branch skipped, Solace path exercised
        return mock;
    }

    private static IApolloContext BuildContext(
        int lilyCount,
        int bloodLilyCount,
        byte playerLevel,
        Mock<IPartyHelper> partyHelper)
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        var player = MockBuilders.CreateMockPlayerCharacter(level: playerLevel);

        var ctx = new Mock<IApolloContext>();
        ctx.Setup(x => x.Configuration).Returns(config);
        ctx.Setup(x => x.Player).Returns(player.Object);
        ctx.Setup(x => x.PartyHelper).Returns(partyHelper.Object);
        ctx.Setup(x => x.PlayerStatsService).Returns(MockBuilders.CreateMockPlayerStatsService().Object);
        ctx.Setup(x => x.HpPredictionService).Returns(MockBuilders.CreateMockHpPredictionService().Object);
        ctx.Setup(x => x.HealingCoordination).Returns(new HealingCoordinationState());
        ctx.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        ctx.Setup(x => x.Debug).Returns(new DebugState());
        ctx.Setup(x => x.LilyCount).Returns(lilyCount);
        ctx.Setup(x => x.BloodLilyCount).Returns(bloodLilyCount);

        return ctx.Object;
    }
}
