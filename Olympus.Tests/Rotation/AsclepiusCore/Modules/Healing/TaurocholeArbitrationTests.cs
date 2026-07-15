using System;
using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Ipc;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Abilities;
using Olympus.Rotation.AsclepiusCore.Context;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AsclepiusCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.AsclepiusCore.Modules.Healing;

/// <summary>
/// Wiring tests for CoHealerArbitration inside SingleTargetOgcdHandler.TryPushTaurochole.
///
/// Verifies that:
///   - toggle off: handler pushes normally even when co-healer is richer
///   - toggle on + richer co-healer + HP above 25% floor: no push (deferred)
///   - toggle on + richer co-healer + HP at 25% floor: push (deadman gate fires)
///   - toggle on + no remote data: push (nothing to defer to)
/// </summary>
public class TaurocholeArbitrationTests
{
    private readonly SingleTargetOgcdHandler _handler = new();

    // -----------------------------------------------------------------------
    // Toggle off: arbitration skipped, handler pushes normally
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ArbitrationOff_PushesTaurochole()
    {
        // Tank at 60% HP; co-healer has primaryResource=3 (richer than my 2 stacks).
        // Toggle off → arbitration bypassed → push expected.
        var context = BuildContext(
            toggleEnabled: false,
            remoteGauge: MakeGauge(primaryResource: 3),
            addersgallStacks: 2,
            tankCurrentHp: 30000,
            tankMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AsclepiusAbilities.Taurochole);
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
            addersgallStacks: 2,
            tankCurrentHp: 30000,
            tankMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AsclepiusAbilities.Taurochole);
    }

    // -----------------------------------------------------------------------
    // Toggle on + richer co-healer + HP at 25% floor → push (deadman gate)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_ArbitrationOn_CoHealerRicher_HpAtFloor_Pushes()
    {
        // Tank at 25% HP = floor; ShouldDefer returns false → handler pushes
        var context = BuildContext(
            toggleEnabled: true,
            remoteGauge: MakeGauge(primaryResource: 3),
            addersgallStacks: 2,
            tankCurrentHp: 12500,
            tankMaxHp: 50000,
            taurocholeThreshold: 0.35f);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AsclepiusAbilities.Taurochole);
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
            addersgallStacks: 2,
            tankCurrentHp: 30000,
            tankMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == AsclepiusAbilities.Taurochole);
    }

    // -----------------------------------------------------------------------
    // Priority ordering: Taurochole must dispatch before Druochole
    // -----------------------------------------------------------------------

    /// <summary>
    /// When both Druochole and Taurochole are eligible in the same frame,
    /// Taurochole must have a lower priority number so it dispatches first.
    /// Taurochole provides 700 potency + 10% mitigation; Druochole provides 600 potency only.
    /// The scheduler dispatches the candidate with the lowest priority number first.
    /// </summary>
    [Fact]
    public void CollectCandidates_BothEligible_TaurocholeHasLowerPriorityNumberThanDruochole()
    {
        var context = BuildContextBothEnabled(
            addersgallStacks: 3,
            tankCurrentHp: 30000,
            tankMaxHp: 50000,
            lowestHpCurrentHp: 30000,
            lowestHpMaxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        var taurochole = Assert.Single(ogcd, c => c.Behavior == AsclepiusAbilities.Taurochole);
        var druochole = Assert.Single(ogcd, c => c.Behavior == AsclepiusAbilities.Druochole);
        Assert.True(taurochole.Priority < druochole.Priority,
            $"Taurochole (priority {taurochole.Priority}) should dispatch before Druochole (priority {druochole.Priority})");
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static RemoteHealerGaugeState MakeGauge(int primaryResource) =>
        new RemoteHealerGaugeState
        {
            InstanceId = Guid.NewGuid(),
            JobId = 40,
            PrimaryResource = primaryResource,
            LastUpdate = DateTime.UtcNow,
        };

    /// <summary>
    /// Builds an AsclepiusContext directly with an injected IPartyCoordinationService.
    /// Uses a mocked IPartyHelper wired to return the given tank from FindTankInParty.
    /// </summary>
    private static IAsclepiusContext BuildContext(
        bool toggleEnabled,
        RemoteHealerGaugeState? remoteGauge,
        int addersgallStacks,
        uint tankCurrentHp,
        uint tankMaxHp,
        float taurocholeThreshold = 0.75f)
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnableTaurochole = true;
        config.Sage.TaurocholeThreshold = taurocholeThreshold;
        config.Sage.EnableDruochole = false;
        config.PartyCoordination.EnableHealerResourceArbitration = toggleEnabled;
        config.PartyCoordination.EnablePartyCoordination = true;
        config.PartyCoordination.EnableHealerGaugeSharing = true;

        var tank = MockBuilders.CreateMockBattleChara(
            entityId: 20u, currentHp: tankCurrentHp, maxHp: tankMaxHp, position: Vector3.Zero);

        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.FindTankInParty(
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(tank.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SGEActions.Taurochole.ActionId)).Returns(true);

        var coordMock = new Mock<IPartyCoordinationService>();
        coordMock.Setup(x => x.GetFreshestRemoteHealerGauge(It.IsAny<float>())).Returns(remoteGauge);

        var addersgallService = AsclepiusTestContext.CreateMockAddersgallService(addersgallStacks);
        var adderstingService = AsclepiusTestContext.CreateMockAdderstingService(0);
        var kardiaManager = AsclepiusTestContext.CreateMockKardiaManager();
        var eukrasiaService = AsclepiusTestContext.CreateMockEukrasiaService();
        var healingSpellSelector = new Mock<IHealingSpellSelector>();

        var player = MockBuilders.CreateMockPlayerCharacter(level: 90);

        return new AsclepiusContext(
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
            configuration: config,
            debuffDetectionService: MockBuilders.CreateMockDebuffDetectionService().Object,
            hpPredictionService: MockBuilders.CreateMockHpPredictionService().Object,
            mpForecastService: MockBuilders.CreateMockMpForecastService().Object,
            objectTable: MockBuilders.CreateMockObjectTable().Object,
            partyList: MockBuilders.CreateMockPartyList().Object,
            playerStatsService: MockBuilders.CreateMockPlayerStatsService().Object,
            targetingService: MockBuilders.CreateMockTargetingService().Object,
            healingSpellSelector: healingSpellSelector.Object,
            cooldownPlanner: MockBuilders.CreateMockCooldownPlanner().Object,
            addersgallService: addersgallService.Object,
            adderstingService: adderstingService.Object,
            kardiaManager: kardiaManager.Object,
            eukrasiaService: eukrasiaService.Object,
            statusHelper: new AsclepiusStatusHelper(),
            partyHelper: partyHelper.Object,
            partyCoordinationService: coordMock.Object,
            debugState: new AsclepiusDebugState());
    }

    /// <summary>
    /// Builds a context with both Druochole and Taurochole enabled and both targets eligible.
    /// No arbitration coordination — toggle off so only priority determines dispatch order.
    /// </summary>
    private static IAsclepiusContext BuildContextBothEnabled(
        int addersgallStacks,
        uint tankCurrentHp,
        uint tankMaxHp,
        uint lowestHpCurrentHp,
        uint lowestHpMaxHp)
    {
        var config = AsclepiusTestContext.CreateDefaultSageConfiguration();
        config.Sage.EnableTaurochole = true;
        config.Sage.EnableDruochole = true;
        config.Sage.TaurocholeThreshold = 0.75f;
        config.Sage.DruocholeThreshold = 0.75f;
        config.Sage.AddersgallReserve = 0;
        config.PartyCoordination.EnableHealerResourceArbitration = false;

        var tank = MockBuilders.CreateMockBattleChara(
            entityId: 20u, currentHp: tankCurrentHp, maxHp: tankMaxHp, position: Vector3.Zero);

        var lowestHpTarget = MockBuilders.CreateMockBattleChara(
            entityId: 21u, currentHp: lowestHpCurrentHp, maxHp: lowestHpMaxHp, position: Vector3.Zero);

        var partyHelper = new Mock<IPartyHelper>();
        partyHelper.Setup(p => p.FindTankInParty(
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>()))
            .Returns(tank.Object);
        partyHelper.Setup(p => p.FindLowestHpPartyMember(
            It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
            It.IsAny<int>()))
            .Returns(lowestHpTarget.Object);

        var actionService = MockBuilders.CreateMockActionService(canExecuteGcd: true, canExecuteOgcd: false);
        actionService.Setup(x => x.IsActionReady(SGEActions.Taurochole.ActionId)).Returns(true);

        var addersgallService = AsclepiusTestContext.CreateMockAddersgallService(addersgallStacks);
        var adderstingService = AsclepiusTestContext.CreateMockAdderstingService(0);
        var kardiaManager = AsclepiusTestContext.CreateMockKardiaManager();
        var eukrasiaService = AsclepiusTestContext.CreateMockEukrasiaService();
        var healingSpellSelector = new Mock<IHealingSpellSelector>();

        var player = MockBuilders.CreateMockPlayerCharacter(level: 90);

        return new AsclepiusContext(
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
            configuration: config,
            debuffDetectionService: MockBuilders.CreateMockDebuffDetectionService().Object,
            hpPredictionService: MockBuilders.CreateMockHpPredictionService().Object,
            mpForecastService: MockBuilders.CreateMockMpForecastService().Object,
            objectTable: MockBuilders.CreateMockObjectTable().Object,
            partyList: MockBuilders.CreateMockPartyList().Object,
            playerStatsService: MockBuilders.CreateMockPlayerStatsService().Object,
            targetingService: MockBuilders.CreateMockTargetingService().Object,
            healingSpellSelector: healingSpellSelector.Object,
            cooldownPlanner: MockBuilders.CreateMockCooldownPlanner().Object,
            addersgallService: addersgallService.Object,
            adderstingService: adderstingService.Object,
            kardiaManager: kardiaManager.Object,
            eukrasiaService: eukrasiaService.Object,
            statusHelper: new AsclepiusStatusHelper(),
            partyHelper: partyHelper.Object,
            partyCoordinationService: null,
            debugState: new AsclepiusDebugState());
    }
}
