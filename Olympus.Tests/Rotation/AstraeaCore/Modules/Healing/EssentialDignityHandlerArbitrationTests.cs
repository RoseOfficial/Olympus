using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Ipc;
using Olympus.Rotation.AstraeaCore.Abilities;
using Olympus.Rotation.AstraeaCore.Context;
using Olympus.Rotation.AstraeaCore.Helpers;
using Olympus.Rotation.AstraeaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AstraeaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AstraeaCore.Modules.Healing;

/// <summary>
/// Wiring tests for CoHealerArbitration inside EssentialDignityHandler.
///
/// Verifies that:
///   - toggle off: handler pushes normally even when co-healer is richer
///   - toggle on + richer co-healer + HP above 25% floor: no push (deferred)
///   - toggle on + richer co-healer + HP at 25% floor: push (deadman gate fires)
///   - toggle on + no remote data: push (nothing to defer to)
/// </summary>
public class EssentialDignityHandlerArbitrationTests
{
    private readonly EssentialDignityHandler _handler = new();

    // -----------------------------------------------------------------------
    // Toggle off: arbitration skipped, handler pushes normally
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ArbitrationOff_PushesNormally()
    {
        // Target at 30% HP (below 40% threshold); co-healer has primaryResource=3 (richer).
        // Toggle off → arbitration bypassed → push expected.
        var context = BuildContext(
            toggleEnabled: false,
            remoteGauge: MakeGauge(primaryResource: 3),
            currentCharges: 1u,
            maxCharges: 2,
            targetCurrentHp: 15000,
            targetMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AstraeaAbilities.EssentialDignity);
    }

    // -----------------------------------------------------------------------
    // Toggle on + richer co-healer + HP above floor → deferred (no push)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ArbitrationOn_CoHealerRicher_HpAboveFloor_NoPush()
    {
        // Target at 30% HP (0.30 > 0.25 floor) → ShouldDefer returns true → no push
        var context = BuildContext(
            toggleEnabled: true,
            remoteGauge: MakeGauge(primaryResource: 3),
            currentCharges: 1u,
            maxCharges: 2,
            targetCurrentHp: 15000,
            targetMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AstraeaAbilities.EssentialDignity);
    }

    // -----------------------------------------------------------------------
    // Toggle on + richer co-healer + HP at 25% floor → push (deadman gate)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ArbitrationOn_CoHealerRicher_HpAtFloor_Pushes()
    {
        // Target at 25% HP = floor; ShouldDefer returns false → handler pushes
        var context = BuildContext(
            toggleEnabled: true,
            remoteGauge: MakeGauge(primaryResource: 3),
            currentCharges: 1u,
            maxCharges: 2,
            targetCurrentHp: 12500,
            targetMaxHp: 50000,
            essentialDignityThreshold: 0.30f);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AstraeaAbilities.EssentialDignity);
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
            targetCurrentHp: 15000,
            targetMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AstraeaAbilities.EssentialDignity);
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
    /// Builds an AstraeaContext with an injected IPartyCoordinationService.
    /// Uses the same direct-construct pattern as SacredSoilHandlerTests.
    /// </summary>
    private static AstraeaContext BuildContext(
        bool toggleEnabled,
        RemoteHealerGaugeState? remoteGauge,
        uint currentCharges,
        ushort maxCharges,
        uint targetCurrentHp,
        uint targetMaxHp,
        float essentialDignityThreshold = 0.40f)
    {
        var config = AstraeaTestContext.CreateDefaultAstrologianConfiguration();
        config.Astrologian.EnableEssentialDignity = true;
        config.Astrologian.EssentialDignityThreshold = essentialDignityThreshold;
        config.PartyCoordination.EnableHealerResourceArbitration = toggleEnabled;
        config.PartyCoordination.EnablePartyCoordination = true;
        config.PartyCoordination.EnableHealerGaugeSharing = true;

        var injured = MockBuilders.CreateMockBattleChara(
            entityId: 10u, currentHp: targetCurrentHp, maxHp: targetMaxHp, position: Vector3.Zero);
        var partyHelper = new TestableAstraeaPartyHelper(
            new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(ASTActions.EssentialDignity.ActionId)).Returns(true);
        actionService.Setup(x => x.GetCurrentCharges(ASTActions.EssentialDignity.ActionId)).Returns(currentCharges);
        actionService.Setup(x => x.GetMaxCharges(ASTActions.EssentialDignity.ActionId, It.IsAny<uint>())).Returns(maxCharges);

        var coordMock = new Mock<IPartyCoordinationService>();
        coordMock.Setup(x => x.GetFreshestRemoteHealerGauge(It.IsAny<float>())).Returns(remoteGauge);

        var cardService = AstraeaTestContext.CreateMockCardService();
        var earthlyStarService = AstraeaTestContext.CreateMockEarthlyStarService();
        var healingSpellSelector = new Mock<IHealingSpellSelector>();

        var player = MockBuilders.CreateMockPlayerCharacter(level: 90);

        return new AstraeaContext(
            player: player.Object,
            inCombat: true,
            isMoving: false,
            canExecuteGcd: true,
            canExecuteOgcd: false,
            actionService: actionService.Object,
            actionTracker: MockBuilders.CreateMockActionTracker(config),
            combatEventService: MockBuilders.CreateMockCombatEventService().Object,
            damageIntakeService: MockBuilders.CreateMockDamageIntakeService().Object,
            damageTrendService: MockBuilders.CreateMockDamageTrendService().Object,
            frameCache: MockBuilders.CreateMockFrameScopedCache().Object,
            configuration: config,
            debuffDetectionService: MockBuilders.CreateMockDebuffDetectionService().Object,
            hpPredictionService: MockBuilders.CreateMockHpPredictionService().Object,
            mpForecastService: MockBuilders.CreateMockMpForecastService().Object,
            objectTable: MockBuilders.CreateMockObjectTable().Object,
            partyList: MockBuilders.CreateMockPartyList().Object,
            playerStatsService: MockBuilders.CreateMockPlayerStatsService().Object,
            targetingService: MockBuilders.CreateMockTargetingService().Object,
            cardService: cardService.Object,
            earthlyStarService: earthlyStarService.Object,
            statusHelper: new AstraeaStatusHelper(),
            partyHelper: partyHelper,
            cooldownPlanner: MockBuilders.CreateMockCooldownPlanner().Object,
            healingSpellSelector: healingSpellSelector.Object,
            partyCoordinationService: coordMock.Object,
            debugState: new AstraeaDebugState());
    }
}
