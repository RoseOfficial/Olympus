using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HephaestusCore.Abilities;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Rotation.HephaestusCore.Modules;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Timeline;
using Olympus.Timeline.Models;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;

namespace Olympus.Tests.Rotation.HephaestusCore.Modules;

/// <summary>
/// Verifies that GnashingFang is held when No Mercy is ready but held by burst or
/// phase-transition guards, matching TryPushNoMercy's hold semantics exactly.
/// </summary>
public class DamageModuleGnashingFangHoldTests
{
    // -----------------------------------------------------------------------
    // 1. nmCd == 0 + burst hold active (imminent, not in window) + carts < max
    //    -> GnashingFang must NOT be pushed (RED before fix)
    // -----------------------------------------------------------------------
    [Fact]
    public void GnashingFang_NotPushed_WhenNoMercyReadyButBurstHoldActive()
    {
        var burstService = new Mock<IBurstWindowService>();
        burstService.Setup(x => x.IsInBurstWindow).Returns(false);
        burstService.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);

        var module = new DamageModule(burstService.Object);

        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        // No Mercy cooldown is 0 (ready but held by burst guard)
        actionService.Setup(x => x.GetCooldownRemaining(GNBActions.NoMercy.ActionId)).Returns(0f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            targeting: targeting,
            actionService: actionService,
            cartridges: 1,
            hasMaxCartridges: false,
            hasNoMercy: false,
            timelineService: null);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == GnbAbilities.GnashingFang);
    }

    // -----------------------------------------------------------------------
    // 2. nmCd == 0 + phase transition imminent + carts < max
    //    -> GnashingFang must NOT be pushed (RED before fix)
    // -----------------------------------------------------------------------
    [Fact]
    public void GnashingFang_NotPushed_WhenNoMercyReadyButPhaseTransitionImminent()
    {
        var module = new DamageModule(burstWindowService: null);

        var timeline = new Mock<ITimelineService>();
        timeline.Setup(x => x.GetNextMechanic(TimelineEntryType.Phase))
            .Returns(new MechanicPrediction(5f, TimelineEntryType.Phase, "Phase 2", 0.9f));

        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        // No Mercy cooldown is 0 (ready but held by phase guard)
        actionService.Setup(x => x.GetCooldownRemaining(GNBActions.NoMercy.ActionId)).Returns(0f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            targeting: targeting,
            actionService: actionService,
            cartridges: 1,
            hasMaxCartridges: false,
            hasNoMercy: false,
            timelineService: timeline.Object);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.DoesNotContain(gcd, c => c.Behavior == GnbAbilities.GnashingFang);
    }

    // -----------------------------------------------------------------------
    // 3. Escape: HasMaxCartridges true + burst hold active -> GF IS pushed
    //    (overcap override takes priority over No Mercy alignment)
    // -----------------------------------------------------------------------
    [Fact]
    public void GnashingFang_Pushed_WhenMaxCartridges_EvenIfBurstHoldActive()
    {
        var burstService = new Mock<IBurstWindowService>();
        burstService.Setup(x => x.IsInBurstWindow).Returns(false);
        burstService.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(true);

        var module = new DamageModule(burstService.Object);

        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCooldownRemaining(GNBActions.NoMercy.ActionId)).Returns(0f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            targeting: targeting,
            actionService: actionService,
            cartridges: 3,
            hasMaxCartridges: true,
            hasNoMercy: false,
            timelineService: null);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.GnashingFang && c.Priority == 5);
    }

    // -----------------------------------------------------------------------
    // 4. Normal case preserved: nmCd == 0, no holds active -> GF IS pushed
    // -----------------------------------------------------------------------
    [Fact]
    public void GnashingFang_Pushed_WhenNoMercyReadyAndNoHoldsActive()
    {
        var burstService = new Mock<IBurstWindowService>();
        burstService.Setup(x => x.IsInBurstWindow).Returns(false);
        burstService.Setup(x => x.IsBurstImminent(It.IsAny<float>())).Returns(false);

        var module = new DamageModule(burstService.Object);

        var enemy = CreateMockEnemy(12345UL);
        var targeting = BuildTargetingWithMeleeEnemy(enemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionService.Setup(x => x.GetCooldownRemaining(GNBActions.NoMercy.ActionId)).Returns(0f);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = CreateContext(
            targeting: targeting,
            actionService: actionService,
            cartridges: 1,
            hasMaxCartridges: false,
            hasNoMercy: false,
            timelineService: null);

        module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == GnbAbilities.GnashingFang && c.Priority == 5);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static Mock<IBattleNpc> CreateMockEnemy(ulong objectId = 99999UL)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns(objectId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        return mock;
    }

    private static Mock<ITargetingService> BuildTargetingWithMeleeEnemy(
        Mock<IBattleNpc> enemy, int enemyCount = 1)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.IsDamageTargetingPaused()).Returns(false);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemyCount);

        var safetyMock = new Mock<IGapCloserSafetyService>();
        safetyMock.Setup(x => x.ShouldBlockGapCloser(It.IsAny<IBattleChara>(), It.IsAny<IPlayerCharacter>()))
            .Returns(false);
        safetyMock.Setup(x => x.LastBlockReason).Returns((string?)null);
        targeting.Setup(x => x.GapCloserSafety).Returns(safetyMock.Object);

        return targeting;
    }

    private static IHephaestusContext CreateContext(
        bool inCombat = true,
        byte level = 100,
        int cartridges = 0,
        bool hasMaxCartridges = false,
        bool hasNoMercy = false,
        ITimelineService? timelineService = null,
        Mock<IActionService>? actionService = null,
        Mock<ITargetingService>? targeting = null)
    {
        var config = HephaestusTestContext.CreateDefaultGunbreakerConfiguration();
        actionService ??= MockBuilders.CreateMockActionService();
        targeting ??= MockBuilders.CreateMockTargetingService();

        var player = MockBuilders.CreateMockPlayerCharacter(level: level);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var mock = new Mock<IHephaestusContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(false);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        mock.Setup(x => x.TimelineService).Returns(timelineService);

        // GNB gauge/combo state
        mock.Setup(x => x.Cartridges).Returns(cartridges);
        mock.Setup(x => x.HasMaxCartridges).Returns(hasMaxCartridges || cartridges >= 3);
        mock.Setup(x => x.CanUseGnashingFang).Returns(cartridges >= 1);
        mock.Setup(x => x.CanUseDoubleDown).Returns(cartridges >= 2);
        mock.Setup(x => x.HasNoMercy).Returns(hasNoMercy);
        mock.Setup(x => x.NoMercyRemaining).Returns(0f);
        mock.Setup(x => x.HasSonicBreakDot).Returns(false);
        mock.Setup(x => x.HasBowShockDot).Returns(false);
        mock.Setup(x => x.IsInGnashingFangCombo).Returns(false);
        mock.Setup(x => x.GnashingFangStep).Returns(0);
        mock.Setup(x => x.IsReadyToRip).Returns(false);
        mock.Setup(x => x.IsReadyToTear).Returns(false);
        mock.Setup(x => x.IsReadyToGouge).Returns(false);
        mock.Setup(x => x.IsReadyToBlast).Returns(false);
        mock.Setup(x => x.IsReadyToBrand).Returns(false);
        mock.Setup(x => x.IsReadyToReign).Returns(false);
        mock.Setup(x => x.HasAnyContinuationReady).Returns(false);
        mock.Setup(x => x.IsInReignCombo).Returns(false);
        mock.Setup(x => x.ReignComboStep).Returns(0);
        mock.Setup(x => x.ComboStep).Returns(0);
        mock.Setup(x => x.LastComboAction).Returns(0u);
        mock.Setup(x => x.ComboTimeRemaining).Returns(30f);
        mock.Setup(x => x.CurrentTarget).Returns((Dalamud.Game.ClientState.Objects.Types.IBattleChara?)null);

        mock.Setup(x => x.Debug).Returns(new HephaestusDebugState());

        return mock.Object;
    }
}
