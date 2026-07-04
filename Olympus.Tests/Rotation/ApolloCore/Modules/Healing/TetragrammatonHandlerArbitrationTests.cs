using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Ipc;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules.Healing;
using Olympus.Rotation.Common;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Wiring tests for CoHealerArbitration inside TetragrammatonHandler.
///
/// Verifies that:
///   - toggle off: handler pushes normally even when co-healer is richer
///   - toggle on + richer co-healer + HP above 25% floor: no push (deferred)
///   - toggle on + richer co-healer + HP at 25% floor: push (deadman gate fires)
///   - toggle on + no remote data: push (nothing to defer to)
/// </summary>
public class TetragrammatonHandlerArbitrationTests
{
    private readonly TetragrammatonHandler _handler = new();

    // -----------------------------------------------------------------------
    // Toggle off: arbitration skipped, handler pushes normally
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ArbitrationOff_PushesNormally()
    {
        // Target at 60% HP; co-healer has 3 charges (richer).
        // Toggle off → arbitration bypassed → push expected.
        var context = BuildContext(
            toggleEnabled: false,
            remoteGauge: MakeGauge(primaryResource: 3),
            currentCharges: 1u,
            maxCharges: 2,
            targetCurrentHp: 30000,
            targetMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Tetragrammaton);
    }

    // -----------------------------------------------------------------------
    // Toggle on + richer co-healer + HP above floor → deferred (no push)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ArbitrationOn_CoHealerRicher_HpAboveFloor_NoPush()
    {
        var context = BuildContext(
            toggleEnabled: true,
            remoteGauge: MakeGauge(primaryResource: 3),
            currentCharges: 1u,
            maxCharges: 2,
            targetCurrentHp: 30000,
            targetMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Tetragrammaton);
    }

    // -----------------------------------------------------------------------
    // Toggle on + richer co-healer + HP at 25% floor → push (deadman gate)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ArbitrationOn_CoHealerRicher_HpAtFloor_Pushes()
    {
        // predictedHp = 12500, maxHp = 50000 → hpPercentForDefer = 0.25 = floor
        // ShouldDefer returns false at floor → handler should push
        var context = BuildContext(
            toggleEnabled: true,
            remoteGauge: MakeGauge(primaryResource: 3),
            currentCharges: 1u,
            maxCharges: 2,
            targetCurrentHp: 12500,
            targetMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Tetragrammaton);
    }

    // -----------------------------------------------------------------------
    // Toggle on + no remote data → push (no co-healer to defer to)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ArbitrationOn_NoRemoteData_Pushes()
    {
        var context = BuildContext(
            toggleEnabled: true,
            remoteGauge: null,
            currentCharges: 1u,
            maxCharges: 2,
            targetCurrentHp: 30000,
            targetMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Tetragrammaton);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static RemoteHealerGaugeState MakeGauge(int primaryResource) =>
        new RemoteHealerGaugeState
        {
            InstanceId = Guid.NewGuid(),
            JobId = 33,
            PrimaryResource = primaryResource,
            LastUpdate = DateTime.UtcNow,
        };

    /// <summary>
    /// Builds a minimal IApolloContext mock wired for TetragrammatonHandler.
    /// Sets config.Healing.UseDamageIntakeTriage = false so FindLowestHpPartyMember is used.
    /// </summary>
    private static IApolloContext BuildContext(
        bool toggleEnabled,
        RemoteHealerGaugeState? remoteGauge,
        uint currentCharges,
        ushort maxCharges,
        uint targetCurrentHp,
        uint targetMaxHp)
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.UseDamageIntakeTriage = false;
        config.PartyCoordination.EnableHealerResourceArbitration = toggleEnabled;
        config.PartyCoordination.EnablePartyCoordination = true;
        config.PartyCoordination.EnableHealerGaugeSharing = true;

        var player = MockBuilders.CreateMockPlayerCharacter(level: 90);

        var actionService = MockBuilders.CreateMockActionService(canExecuteOgcd: true, canExecuteGcd: false);
        actionService.Setup(x => x.GetCurrentCharges(WHMActions.Tetragrammaton.ActionId)).Returns(currentCharges);
        actionService.Setup(x => x.GetMaxCharges(WHMActions.Tetragrammaton.ActionId, It.IsAny<uint>())).Returns(maxCharges);

        var target = new Mock<IBattleChara>();
        target.Setup(x => x.EntityId).Returns(10u);
        target.Setup(x => x.GameObjectId).Returns(10ul);
        target.Setup(x => x.CurrentHp).Returns(targetCurrentHp);
        target.Setup(x => x.MaxHp).Returns(targetMaxHp);
        target.Setup(x => x.Position).Returns(Vector3.Zero);
        target.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.FindLowestHpPartyMember(
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
            It.IsAny<int>()))
            .Returns(target.Object);

        var coordMock = new Mock<IPartyCoordinationService>();
        coordMock.Setup(x => x.GetFreshestRemoteHealerGauge(It.IsAny<float>())).Returns(remoteGauge);

        var hpPredictionService = MockBuilders.CreateMockHpPredictionService();
        var playerStatsService = MockBuilders.CreateMockPlayerStatsService();
        var damageTrendService = MockBuilders.CreateMockDamageTrendService();
        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();

        var ctx = new Mock<IApolloContext>();
        ctx.Setup(x => x.Configuration).Returns(config);
        ctx.Setup(x => x.Player).Returns(player.Object);
        ctx.Setup(x => x.ActionService).Returns(actionService.Object);
        ctx.Setup(x => x.PartyHelper).Returns(partyHelper.Object);
        ctx.Setup(x => x.HealingCoordination).Returns(new HealingCoordinationState());
        ctx.Setup(x => x.PartyCoordinationService).Returns(coordMock.Object);
        ctx.Setup(x => x.HpPredictionService).Returns(hpPredictionService.Object);
        ctx.Setup(x => x.PlayerStatsService).Returns(playerStatsService.Object);
        ctx.Setup(x => x.DamageTrendService).Returns(damageTrendService.Object);
        ctx.Setup(x => x.DamageIntakeService).Returns(damageIntakeService.Object);
        ctx.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        ctx.Setup(x => x.Debug).Returns(new DebugState());

        return ctx.Object;
    }
}
