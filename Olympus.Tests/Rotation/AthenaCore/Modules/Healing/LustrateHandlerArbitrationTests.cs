using System;
using System.Collections.Generic;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Ipc;
using Olympus.Rotation.AthenaCore.Abilities;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Rotation.AthenaCore.Helpers;
using Olympus.Rotation.AthenaCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AthenaCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AthenaCore.Modules.Healing;

/// <summary>
/// Wiring tests for CoHealerArbitration inside LustrateHandler.
///
/// Verifies that:
///   - toggle off: handler pushes normally even when co-healer is richer
///   - toggle on + richer co-healer + HP above 25% floor: no push (deferred)
///   - toggle on + richer co-healer + HP at 25% floor: push (deadman gate fires)
///   - toggle on + no remote data: push (nothing to defer to)
/// </summary>
public class LustrateHandlerArbitrationTests
{
    private readonly LustrateHandler _handler = new();

    // -----------------------------------------------------------------------
    // Toggle off: arbitration skipped, handler pushes normally
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ArbitrationOff_PushesNormally()
    {
        // Target at 50% HP; co-healer has primaryResource=3 (richer than my 2 stacks).
        // Toggle off → arbitration bypassed → push expected.
        var context = BuildContext(
            toggleEnabled: false,
            remoteGauge: MakeGauge(primaryResource: 3),
            aetherflowStacks: 2,
            targetCurrentHp: 25000,
            targetMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Lustrate);
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
            aetherflowStacks: 2,
            targetCurrentHp: 25000,
            targetMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Lustrate);
    }

    // -----------------------------------------------------------------------
    // Toggle on + richer co-healer + HP at 25% floor → push (deadman gate)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ArbitrationOn_CoHealerRicher_HpAtFloor_Pushes()
    {
        // hpPercent = 12500/50000 = 0.25 = floor → ShouldDefer returns false
        var context = BuildContext(
            toggleEnabled: true,
            remoteGauge: MakeGauge(primaryResource: 3),
            aetherflowStacks: 2,
            targetCurrentHp: 12500,
            targetMaxHp: 50000,
            lustrateThreshold: 0.35f);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Lustrate);
    }

    // -----------------------------------------------------------------------
    // Toggle on + no remote data → push (nothing to defer to)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ArbitrationOn_NoRemoteData_Pushes()
    {
        var context = BuildContext(
            toggleEnabled: true,
            remoteGauge: null,
            aetherflowStacks: 2,
            targetCurrentHp: 25000,
            targetMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AthenaAbilities.Lustrate);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static RemoteHealerGaugeState MakeGauge(int primaryResource) =>
        new RemoteHealerGaugeState
        {
            InstanceId = Guid.NewGuid(),
            JobId = 28,
            PrimaryResource = primaryResource,
            LastUpdate = DateTime.UtcNow,
        };

    /// <summary>
    /// Builds an AthenaContext with an injected IPartyCoordinationService for
    /// arbitration testing. Uses the same direct-construct pattern as
    /// SacredSoilHandlerTests.CreateContextWithCoordination.
    /// </summary>
    private static AthenaContext BuildContext(
        bool toggleEnabled,
        RemoteHealerGaugeState? remoteGauge,
        int aetherflowStacks,
        uint targetCurrentHp,
        uint targetMaxHp,
        float lustrateThreshold = 0.60f)
    {
        var config = AthenaTestContext.CreateDefaultScholarConfiguration();
        config.Scholar.EnableLustrate = true;
        config.Scholar.LustrateThreshold = lustrateThreshold;
        config.Scholar.AetherflowReserve = 0;
        config.PartyCoordination.EnableHealerResourceArbitration = toggleEnabled;
        config.PartyCoordination.EnablePartyCoordination = true;
        config.PartyCoordination.EnableHealerGaugeSharing = true;

        var injured = MockBuilders.CreateMockBattleChara(
            entityId: 10u, currentHp: targetCurrentHp, maxHp: targetMaxHp, position: Vector3.Zero);
        var partyHelper = new TestableAthenaPartyHelper(
            new List<IBattleChara> { injured.Object }, config);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SCHActions.Lustrate.ActionId)).Returns(true);

        var coordMock = new Mock<IPartyCoordinationService>();
        coordMock.Setup(x => x.GetFreshestRemoteHealerGauge(It.IsAny<float>())).Returns(remoteGauge);

        var aetherflow = AthenaTestContext.CreateMockAetherflowService(aetherflowStacks);
        var fairyGauge = AthenaTestContext.CreateMockFairyGaugeService(50);
        var fairyState = AthenaTestContext.CreateMockFairyStateManager();
        var healingSpellSelector = new Mock<IHealingSpellSelector>();

        var player = MockBuilders.CreateMockPlayerCharacter(level: 100);

        return new AthenaContext(
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
            aetherflowService: aetherflow.Object,
            fairyGaugeService: fairyGauge.Object,
            fairyStateManager: fairyState.Object,
            statusHelper: new AthenaStatusHelper(),
            partyHelper: partyHelper,
            cooldownPlanner: MockBuilders.CreateMockCooldownPlanner().Object,
            healingSpellSelector: healingSpellSelector.Object,
            partyCoordinationService: coordMock.Object,
            debugState: new AthenaDebugState());
    }
}
