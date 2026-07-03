using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AresCore.Abilities;
using Olympus.Rotation.AresCore.Context;
using Olympus.Rotation.AresCore.Helpers;
using Olympus.Rotation.AresCore.Modules;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Services;
using Olympus.Services.Action;
using Olympus.Services.Party;
using Olympus.Services.Tank;
using Olympus.Services.Targeting;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.AresCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Timeline;
using Xunit;

namespace Olympus.Tests.Rotation.AresCore.Modules;

/// <summary>
/// Scheduler-push tests for <see cref="MitigationModule.CollectCandidates"/>.
/// Each test exercises a specific gate (toggle, HP threshold, coordination overlap) and
/// verifies the correct <see cref="AbilityBehavior"/> is pushed (or not) at the right priority.
/// Tests inspect <see cref="RotationScheduler.InspectOgcdQueue"/> only; the mitigation module
/// pushes no GCD candidates.
/// </summary>
public class MitigationModuleCollectCandidatesTests
{
    private readonly MitigationModule _module = new();

    // -----------------------------------------------------------------------
    // Early-exit guards
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Disabled_PushesNothing()
    {
        var context = CreateContext(enableMitigation: false);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Disabled", context.Debug.MitigationState);
    }

    [Fact]
    public void CollectCandidates_NotInCombat_PushesNothing()
    {
        var context = CreateContext(inCombat: false);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.MitigationState);
    }

    // -----------------------------------------------------------------------
    // Reprisal
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Reprisal_PushedAtPriority4_WhenConditionsMet()
    {
        // 3 injured party members → passes the "injuredCount < 3 && avgHp > 0.85" skip
        var target = CreateMockTarget(entityId: 500u);
        var context = CreateContext(
            currentTarget: target,
            partyHealthMetrics: (0.70f, 0.50f, 3));
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == AresAbilities.Reprisal && c.Priority == 4);
    }

    [Fact]
    public void CollectCandidates_Reprisal_NotPushed_WhenNoTarget()
    {
        // CurrentTarget == null → early return inside TryPushReprisal
        var context = CreateContext(
            currentTarget: null,
            partyHealthMetrics: (0.70f, 0.50f, 3));
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == AresAbilities.Reprisal);
    }

    [Fact]
    public void CollectCandidates_Reprisal_NotPushed_WhenRemoteOverlapDetected()
    {
        // EnableCooldownCoordination=true (default) + WasActionUsedByOther=true → skip
        var target = CreateMockTarget(entityId: 501u);

        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.WasActionUsedByOther(RoleActions.Reprisal.ActionId, 10f)).Returns(true);

        var context = CreateContext(
            currentTarget: target,
            partyHealthMetrics: (0.70f, 0.50f, 3),
            partyCoord: partyCoord);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == AresAbilities.Reprisal);
        Assert.Equal("Reprisal skipped (remote Reprisal up)", context.Debug.MitigationState);
    }

    // -----------------------------------------------------------------------
    // Thrill of Battle
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_ThrillOfBattle_PushedAtPriority4_WhenHpAt60Pct()
    {
        // 60% HP < 70% threshold → pushed
        var context = CreateContext(currentHp: 30000, maxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == AresAbilities.ThrillOfBattle && c.Priority == 4);
    }

    [Fact]
    public void CollectCandidates_ThrillOfBattle_NotPushed_WhenHpAbove70Pct()
    {
        // 80% HP > 70% threshold → skipped
        var context = CreateContext(currentHp: 40000, maxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == AresAbilities.ThrillOfBattle);
    }

    [Fact]
    public void CollectCandidates_ThrillOfBattle_NotPushed_WhenDisabled()
    {
        var context = CreateContext(
            enableThrillOfBattle: false,
            currentHp: 30000,
            maxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == AresAbilities.ThrillOfBattle);
    }

    // -----------------------------------------------------------------------
    // Equilibrium
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Equilibrium_PushedAtPriority4_WhenHpAt40Pct()
    {
        // 40% HP < 50% threshold → pushed
        var context = CreateContext(currentHp: 20000, maxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == AresAbilities.Equilibrium && c.Priority == 4);
    }

    [Fact]
    public void CollectCandidates_Equilibrium_NotPushed_WhenHpAbove50Pct()
    {
        // 70% HP > 50% threshold → skipped
        var context = CreateContext(currentHp: 35000, maxHp: 50000);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == AresAbilities.Equilibrium);
    }

    // -----------------------------------------------------------------------
    // Rampart via RoleActionPushers
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Rampart_PushedAtPriority3_WhenShouldUseMitigationTrue()
    {
        // ShouldUseMitigation=true, no holmgang, no active mit, no coord overlap (partyCoord=null)
        var tankCooldown = new Mock<ITankCooldownService>();
        tankCooldown.Setup(x => x.ShouldUseMitigation(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>())).Returns(true);
        tankCooldown.Setup(x => x.ShouldUseMajorCooldown(It.IsAny<float>(), It.IsAny<float>())).Returns(false);

        var context = CreateContext(tankCooldownService: tankCooldown);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == AresAbilities.Rampart && c.Priority == 3);
    }

    [Fact]
    public void CollectCandidates_Rampart_NotPushed_WhenDefensiveCoordinationOverlap()
    {
        // EnableDefensiveCoordination=true + WasActionUsedByOther(Rampart, 20s)=true → skip
        var tankCooldown = new Mock<ITankCooldownService>();
        tankCooldown.Setup(x => x.ShouldUseMitigation(It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>())).Returns(true);
        tankCooldown.Setup(x => x.ShouldUseMajorCooldown(It.IsAny<float>(), It.IsAny<float>())).Returns(false);

        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.WasActionUsedByOther(RoleActions.Rampart.ActionId, 20f)).Returns(true);

        var context = CreateContext(
            enableDefensiveCoordination: true,
            tankCooldownService: tankCooldown,
            partyCoord: partyCoord);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == AresAbilities.Rampart);
    }

    // -----------------------------------------------------------------------
    // Interrupt: toggle-off must push nothing and make no IPC reservation
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Interject_AndLowBlow_BothDisabled_NothingPushed_NoIpcReservationMade()
    {
        // Target is casting an interruptible spell with enough cast time (1.0s > max delay 0.7s).
        // Both toggles off → neither Interject nor LowBlow is dispatched and ReserveInterruptTarget
        // must never be called even though EnableInterruptCoordination is true.
        var target = CreateMockCastingTarget(entityId: 999u, castTime: 1.0f, totalCastTime: 3.0f);

        var partyCoord = new Mock<IPartyCoordinationService>();
        partyCoord.Setup(x => x.IsInterruptTargetReservedByOther(It.IsAny<uint>())).Returns(false);

        var context = CreateContext(
            enableInterject: false,
            enableLowBlow: false,
            currentTarget: target,
            partyCoord: partyCoord);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == AresAbilities.Interject);
        Assert.DoesNotContain(ogcd, c => c.Behavior == AresAbilities.LowBlow);
        partyCoord.Verify(
            x => x.ReserveInterruptTarget(It.IsAny<uint>(), It.IsAny<uint>(), It.IsAny<int>()),
            Times.Never(),
            "ReserveInterruptTarget must not be called when both interrupt toggles are off");
    }

    [Fact]
    public void CollectCandidates_LowBlow_PushedAtPriority1_WhenInterjectUnavailable()
    {
        // EnableInterject=false (so interject block is skipped), EnableLowBlow=true,
        // LowBlow ready → falls through to LowBlow branch.
        var target = CreateMockCastingTarget(entityId: 998u, castTime: 1.0f, totalCastTime: 3.0f);

        // Disable interrupt coordination so no IPC reservation is required
        var config = AresTestContext.CreateDefaultWarriorConfiguration();
        config.PartyCoordination.EnableInterruptCoordination = false;

        var context = CreateContext(
            enableInterject: false,
            enableLowBlow: true,
            currentTarget: target,
            config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == AresAbilities.LowBlow && c.Priority == 1);
        Assert.DoesNotContain(ogcd, c => c.Behavior == AresAbilities.Interject);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a minimal casting, interruptible target mock.
    /// <paramref name="castTime"/> must be &gt; 0.7s to guarantee the humanized delay check passes.
    /// </summary>
    private static IBattleChara CreateMockCastingTarget(uint entityId, float castTime, float totalCastTime)
    {
        var mock = new Mock<IBattleChara>();
        mock.Setup(x => x.EntityId).Returns(entityId);
        mock.Setup(x => x.GameObjectId).Returns((ulong)entityId);
        mock.Setup(x => x.IsCasting).Returns(true);
        mock.Setup(x => x.IsCastInterruptible).Returns(true);
        mock.Setup(x => x.CurrentCastTime).Returns(castTime);
        mock.Setup(x => x.TotalCastTime).Returns(totalCastTime);
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return mock.Object;
    }

    private static IBattleChara CreateMockTarget(uint entityId)
    {
        var mock = new Mock<IBattleChara>();
        mock.Setup(x => x.EntityId).Returns(entityId);
        mock.Setup(x => x.GameObjectId).Returns((ulong)entityId);
        mock.Setup(x => x.IsCasting).Returns(false);
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return mock.Object;
    }

    /// <summary>
    /// Builds a complete <see cref="IAresContext"/> mock tuned for MitigationModule tests.
    /// The module pushes only oGCD candidates. At 100% HP (default) the HP-gated defensive
    /// abilities are not triggered; tests that exercise them pass low HP values explicitly.
    /// </summary>
    private static IAresContext CreateContext(
        bool inCombat = true,
        byte level = 100,
        uint currentHp = 50000,
        uint maxHp = 50000,
        bool enableMitigation = true,
        bool enableThrillOfBattle = true,
        bool enableEquilibrium = true,
        bool enableReprisal = true,
        bool enableInterject = true,
        bool enableLowBlow = true,
        bool enableDefensiveCoordination = false,
        bool hasHolmgang = false,
        bool hasBloodwhetting = false,
        bool hasActiveMitigation = false,
        (float avg, float lowest, int injured) partyHealthMetrics = default,
        IBattleChara? currentTarget = null,
        Mock<IActionService>? actionService = null,
        Mock<IPartyCoordinationService>? partyCoord = null,
        Mock<ITankCooldownService>? tankCooldownService = null,
        Configuration? config = null)
    {
        config ??= AresTestContext.CreateDefaultWarriorConfiguration();
        config.Tank.EnableMitigation = enableMitigation;
        config.Tank.EnableThrillOfBattle = enableThrillOfBattle;
        config.Tank.EnableEquilibrium = enableEquilibrium;
        config.Tank.EnableReprisal = enableReprisal;
        config.Tank.EnableInterject = enableInterject;
        config.Tank.EnableLowBlow = enableLowBlow;
        config.Tank.EnableDefensiveCoordination = enableDefensiveCoordination;

        actionService ??= MockBuilders.CreateMockActionService();

        // Targeting used by Holmgang (FindEnemy) and Reprisal (CountEnemiesInRange)
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemy(
            It.IsAny<EnemyTargetingStrategy>(), It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns((IBattleNpc?)null);
        targeting.Setup(x => x.CountEnemiesInRange(It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);

        if (tankCooldownService == null)
        {
            tankCooldownService = new Mock<ITankCooldownService>();
            tankCooldownService.Setup(x => x.ShouldUseMitigation(
                It.IsAny<float>(), It.IsAny<float>(), It.IsAny<bool>())).Returns(false);
            tankCooldownService.Setup(x => x.ShouldUseMajorCooldown(
                It.IsAny<float>(), It.IsAny<float>())).Returns(false);
        }

        var player = MockBuilders.CreateMockPlayerCharacter(
            level: level, currentHp: currentHp, maxHp: maxHp);
        player.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var damageIntakeService = MockBuilders.CreateMockDamageIntakeService();
        var statusHelper = new AresStatusHelper();
        var partyHelper = new AresPartyHelper(
            MockBuilders.CreateMockObjectTable().Object,
            MockBuilders.CreateMockPartyList().Object);
        var debugState = new AresDebugState();

        // Default to healthy party so HP-based skips (Reprisal, ShakeItOff) activate unless overridden
        var healthMetrics = (partyHealthMetrics.avg == 0f && partyHealthMetrics.lowest == 0f)
            ? (1.0f, 1.0f, 0)
            : partyHealthMetrics;

        var mock = new Mock<IAresContext>();
        mock.Setup(x => x.Player).Returns(player.Object);
        mock.Setup(x => x.InCombat).Returns(inCombat);
        mock.Setup(x => x.IsMoving).Returns(false);
        mock.Setup(x => x.CanExecuteGcd).Returns(true);
        mock.Setup(x => x.CanExecuteOgcd).Returns(false);
        mock.Setup(x => x.Configuration).Returns(config);
        mock.Setup(x => x.ActionService).Returns(actionService.Object);
        mock.Setup(x => x.TargetingService).Returns(targeting.Object);
        mock.Setup(x => x.TankCooldownService).Returns(tankCooldownService.Object);
        mock.Setup(x => x.DamageIntakeService).Returns(damageIntakeService.Object);
        mock.Setup(x => x.TimelineService).Returns((ITimelineService?)null);
        mock.Setup(x => x.CurrentTarget).Returns(currentTarget);
        mock.Setup(x => x.PartyCoordinationService).Returns(partyCoord?.Object);
        mock.Setup(x => x.PartyHealthMetrics).Returns(healthMetrics);
        mock.Setup(x => x.HasHolmgang).Returns(hasHolmgang);
        mock.Setup(x => x.HasBloodwhetting).Returns(hasBloodwhetting);
        mock.Setup(x => x.HasActiveMitigation).Returns(hasActiveMitigation);
        mock.Setup(x => x.HasVengeance).Returns(false);
        mock.Setup(x => x.StatusHelper).Returns(statusHelper);
        mock.Setup(x => x.PartyHelper).Returns(partyHelper);
        mock.Setup(x => x.Debug).Returns(debugState);
        mock.Setup(x => x.TrainingService).Returns((ITrainingService?)null);

        return mock.Object;
    }
}
