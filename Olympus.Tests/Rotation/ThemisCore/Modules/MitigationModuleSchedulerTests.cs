using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.ThemisCore.Abilities;
using Olympus.Rotation.ThemisCore.Modules;
using Olympus.Services.Action;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Olympus.Tests.Rotation.ThemisCore;
using Xunit;

namespace Olympus.Tests.Rotation.ThemisCore.Modules;

/// <summary>
/// Scheduler-push tests for Paladin MitigationModule.
/// Verifies that candidates are pushed to the scheduler queue with correct
/// behaviors and priorities, and that enable toggles and state gates short-circuit
/// as expected. All tests call CollectCandidates and inspect queues without dispatching.
///
/// Factory constraints: TankCooldownService is hardcoded inside ThemisTestContext.Create()
/// to return false for all ShouldUseXxx queries, so Sentinel/Sheltron/Rampart reactive
/// paths cannot be reached here. The timeline-aware proactive path is similarly
/// untestable because TimelineService is null. Cover is untestable because
/// FindCoverTarget requires real ObjectTable entries. These paths are covered in
/// integration tests instead.
/// </summary>
public class MitigationModuleSchedulerTests
{
    private readonly MitigationModule _module = new();

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a mock enemy with a specified GameObjectId and EntityId.
    /// StatusList is null to make all status-based checks return false.
    /// IsCasting defaults to false via Moq.
    /// </summary>
    private static Mock<IBattleNpc> CreateMockEnemy(uint entityId = 12345u)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.GameObjectId).Returns((ulong)entityId);
        mock.Setup(x => x.EntityId).Returns(entityId);
        mock.Setup(x => x.CurrentHp).Returns(10000u);
        mock.Setup(x => x.MaxHp).Returns(10000u);
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return mock;
    }

    /// <summary>
    /// Creates a mock enemy that is actively casting an interruptible spell.
    /// CurrentCastTime=0.8f ensures it always exceeds the max humanized delay (0.7f).
    /// </summary>
    private static Mock<IBattleNpc> CreateMockCastingEnemy(uint entityId = 8888u)
    {
        var mock = CreateMockEnemy(entityId);
        mock.Setup(x => x.IsCasting).Returns(true);
        mock.Setup(x => x.IsCastInterruptible).Returns(true);
        mock.Setup(x => x.CurrentCastTime).Returns(0.8f);
        mock.Setup(x => x.TotalCastTime).Returns(3.0f);
        return mock;
    }

    /// <summary>
    /// Wires the targeting service so FindEnemyForAction returns the provided enemy,
    /// making context.CurrentTarget non-null for Reprisal and interrupt logic.
    /// </summary>
    private static Mock<ITargetingService> BuildTargetingWithEnemy(Mock<IBattleNpc> enemy)
    {
        var targeting = MockBuilders.CreateMockTargetingService();
        targeting.Setup(x => x.FindEnemyForAction(
                It.IsAny<EnemyTargetingStrategy>(), It.IsAny<uint>(), It.IsAny<IPlayerCharacter>()))
            .Returns(enemy.Object);
        targeting.Setup(x => x.CountEnemiesInRange(
                It.IsAny<float>(), It.IsAny<IPlayerCharacter>()))
            .Returns(1);
        return targeting;
    }

    // -----------------------------------------------------------------------
    // Early-exit guards
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_MitigationDisabled_PushesNothing()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableMitigation = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(config: config, inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Disabled", context.Debug.MitigationState);
    }

    [Fact]
    public void CollectCandidates_NotInCombat_PushesNothing()
    {
        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(inCombat: false);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
        Assert.Empty(scheduler.InspectOgcdQueue());
        Assert.Equal("Not in combat", context.Debug.MitigationState);
    }

    // -----------------------------------------------------------------------
    // Hallowed Ground (emergency invulnerability)
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_HallowedGround_PushedAtPriority1_WhenCriticalHP()
    {
        // 10% HP is below the 15% threshold that gates Hallowed Ground.
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableHallowedGround = true;
        // EnableInvulnerabilityCoordination=false (default in factory) so the IPC
        // stagger check is bypassed even though partyCoord is null.

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            currentHp: 5000,
            maxHp: 50000,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == ThemisAbilities.HallowedGround && c.Priority == 1);
    }

    [Fact]
    public void CollectCandidates_HallowedGround_NotPushed_WhenDisabled()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableHallowedGround = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(
            config: config,
            level: 100,
            currentHp: 5000,
            maxHp: 50000,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == ThemisAbilities.HallowedGround);
    }

    [Fact]
    public void CollectCandidates_HallowedGround_NotPushed_WhenHPAboveThreshold()
    {
        // 80% HP is above the 15% gate — Hallowed Ground must not fire.
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableHallowedGround = true;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(
            config: config,
            level: 100,
            currentHp: 40000,
            maxHp: 50000,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == ThemisAbilities.HallowedGround);
    }

    [Fact]
    public void CollectCandidates_HallowedGround_NotPushed_BelowMinLevel()
    {
        // HallowedGround.MinLevel = 50. At Lv.49 the action is not yet available.
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableHallowedGround = true;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(
            config: config,
            level: 49,
            currentHp: 5000,
            maxHp: 50000,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == ThemisAbilities.HallowedGround);
    }

    // -----------------------------------------------------------------------
    // Divine Veil (party barrier)
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_DivineVeil_PushedAtPriority5_WhenPartyInjured()
    {
        // Player at 20% HP → PartyHealthMetrics.avgHp=0.2, injuredCount=1.
        // Guard: injuredCount < 3 && avgHp > 0.85 → 1 < 3 && 0.2 > 0.85 → false → does not skip.
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableDivineVeil = true;

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            currentHp: 10000,
            maxHp: 50000,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ThemisAbilities.DivineVeil && c.Priority == 5);
    }

    [Fact]
    public void CollectCandidates_DivineVeil_NotPushed_WhenDisabled()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableDivineVeil = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(
            config: config,
            level: 100,
            currentHp: 10000,
            maxHp: 50000,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == ThemisAbilities.DivineVeil);
    }

    // -----------------------------------------------------------------------
    // Reprisal (party damage-reduction debuff)
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Reprisal_PushedAtPriority4_WhenPartyInjuredAndTargetPresent()
    {
        // Player at 20% HP drives avgHp below the 85% skip threshold.
        // Enemy from FindEnemyForAction makes context.CurrentTarget non-null.
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableReprisal = true;
        // partyCoord is null in the factory → null?.WasActionUsedByOther() == true → false.
        // EnableCooldownCoordination state is irrelevant since the null-safe check short-circuits.

        var enemy = CreateMockEnemy(entityId: 9999u);
        var targeting = BuildTargetingWithEnemy(enemy);
        var actionService = MockBuilders.CreateMockActionService();

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 100,
            currentHp: 10000,
            maxHp: 50000,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ThemisAbilities.Reprisal && c.Priority == 4);
    }

    [Fact]
    public void CollectCandidates_Reprisal_NotPushed_WhenDisabled()
    {
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableReprisal = false;

        var enemy = CreateMockEnemy();
        var targeting = BuildTargetingWithEnemy(enemy);

        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(
            config: config,
            targetingService: targeting,
            level: 100,
            currentHp: 10000,
            maxHp: 50000,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(), c => c.Behavior == ThemisAbilities.Reprisal);
    }

    // -----------------------------------------------------------------------
    // Interrupt (Interject / Low Blow)
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Interject_PushedAtPriority1_WhenTargetCastingInterruptible()
    {
        // CurrentCastTime=0.8f always exceeds max humanized delay (0.7f), so the
        // delay gate never blocks. EnableInterruptCoordination is irrelevant because
        // partyCoord is null → null?.IsInterruptTargetReservedByOther() == true → false.
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableInterject = true;

        var castingEnemy = CreateMockCastingEnemy(entityId: 7777u);
        var targeting = BuildTargetingWithEnemy(castingEnemy);
        var actionService = MockBuilders.CreateMockActionService();

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 100,
            // Full HP ensures HallowedGround / Bulwark / party-mit guards are all
            // bypassed so the oGCD queue contains only the interrupt candidate.
            currentHp: 50000,
            maxHp: 50000,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == ThemisAbilities.Interject && c.Priority == 1);
    }

    [Fact]
    public void CollectCandidates_LowBlow_PushedAtPriority1_WhenInterjectOnCooldown()
    {
        // Interject is on cooldown → IsActionReady returns false for it specifically.
        // Low Blow (backup stun) should then be pushed instead.
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableInterject = true;
        config.Tank.EnableLowBlow = true;

        var castingEnemy = CreateMockCastingEnemy(entityId: 6666u);
        var targeting = BuildTargetingWithEnemy(castingEnemy);

        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.IsActionReady(RoleActions.Interject.ActionId)).Returns(false);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            config: config,
            actionService: actionService,
            targetingService: targeting,
            level: 100,
            currentHp: 50000,
            maxHp: 50000,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.Contains(ogcd, c => c.Behavior == ThemisAbilities.LowBlow && c.Priority == 1);
        Assert.DoesNotContain(ogcd, c => c.Behavior == ThemisAbilities.Interject);
    }

    [Fact]
    public void CollectCandidates_NoInterrupt_WhenTargetNotCasting()
    {
        // IsCasting defaults to false on the mock — interrupt logic must not push anything.
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableInterject = true;
        config.Tank.EnableLowBlow = true;

        var enemy = CreateMockEnemy(entityId: 5555u);
        // IsCasting = false (Moq default for bool)
        var targeting = BuildTargetingWithEnemy(enemy);

        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(
            config: config,
            targetingService: targeting,
            level: 100,
            // Full HP so party-mit guards skip Reprisal / DivineVeil as well.
            currentHp: 50000,
            maxHp: 50000,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var ogcd = scheduler.InspectOgcdQueue();
        Assert.DoesNotContain(ogcd, c => c.Behavior == ThemisAbilities.Interject);
        Assert.DoesNotContain(ogcd, c => c.Behavior == ThemisAbilities.LowBlow);
    }

    // -----------------------------------------------------------------------
    // Clemency (emergency GCD heal for party / self)
    // -----------------------------------------------------------------------

    [Fact]
    public void CollectCandidates_Clemency_PushedAtPriority1_WhenLowestMemberBelowThreshold()
    {
        // Player at 20% HP (only party member in mock object table) is below the 30%
        // ClemencyThreshold → Clemency fires targeting the player themselves.
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableClemency = true;
        config.Tank.ClemencyThreshold = 0.30f;

        var actionService = MockBuilders.CreateMockActionService();
        var scheduler = SchedulerFactory.CreateForTest(actionService: actionService);
        var context = ThemisTestContext.Create(
            config: config,
            actionService: actionService,
            level: 100,
            currentHp: 10000,
            maxHp: 50000,
            currentMp: 10000, // >= 2000 MP gate
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        var gcd = scheduler.InspectGcdQueue();
        Assert.Contains(gcd, c => c.Behavior == ThemisAbilities.Clemency && c.Priority == 1);
    }

    [Fact]
    public void CollectCandidates_Clemency_NotPushed_WhenDisabled()
    {
        // EnableClemency defaults to false — confirming the opt-in gate holds.
        var config = ThemisTestContext.CreateDefaultPaladinConfiguration();
        config.Tank.EnableClemency = false;

        var scheduler = SchedulerFactory.CreateForTest();
        var context = ThemisTestContext.Create(
            config: config,
            level: 100,
            currentHp: 10000,
            maxHp: 50000,
            inCombat: true);

        _module.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(), c => c.Behavior == ThemisAbilities.Clemency);
    }
}
