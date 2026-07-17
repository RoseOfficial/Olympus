using System.Linq;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.CalliopeCore.Modules;
using Olympus.Services.Targeting;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.CalliopeCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.CalliopeCore.Modules;

/// <summary>
/// Verifies that TryPushApplyDots in DamageModule routes both the DoT-need gate and
/// the dispatch target to the same highest-HP enemy, so DoT uptime is correct in
/// multi-enemy phases (boss + add) where the primary strategy target and the DoT
/// dispatch target differ.
/// </summary>
public class DamageModuleDoTTargetingTests
{
    private static Mock<IBattleNpc> MakeEnemy(ulong id, uint hp = 100_000u)
    {
        var mock = new Mock<IBattleNpc>();
        mock.Setup(x => x.MaxHp).Returns(hp);
        mock.Setup(x => x.CurrentHp).Returns(hp);
        mock.Setup(x => x.GameObjectId).Returns(id);
        mock.Setup(x => x.IsCasting).Returns(false);
        mock.Setup(x => x.IsCastInterruptible).Returns(false);
        // Null StatusList: HasStatusFromSource returns false for all statuses, meaning
        // the enemy does not have the DoT and it should be applied.
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return mock;
    }

    /// <summary>
    /// Single-target scenario: FindEnemy returns the same target for all strategies.
    /// Stormbite push must target that enemy directly (no TargetingOverride on the behavior).
    /// </summary>
    [Fact]
    public void TryPushApplyDots_Stormbite_TargetsHighestHpEnemy_SingleTarget()
    {
        var enemy = MakeEnemy(42ul);

        var targetingMock = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        // Both StatusList=null means neither Stormbite nor CausticBite is present.
        // Stormbite fires first (appears first in TryPushApplyDots) and returns early.
        var context = CalliopeTestContext.Create(
            targetingService: targetingMock,
            inCombat: true,
            level: 100);

        var scheduler = SchedulerFactory.CreateForTest();
        var module = new DamageModule();

        module.CollectCandidates(context, scheduler, false);

        var queue = scheduler.InspectGcdQueue();
        var stormbiteCandidate = queue
            .Single(c => c.Behavior.Action.ActionId == BRDActions.Stormbite.ActionId);

        // Push goes directly to dotTarget.GameObjectId -- no TargetingOverride needed.
        Assert.Equal(42ul, stormbiteCandidate.TargetId);
        Assert.Null(stormbiteCandidate.Behavior.TargetingOverride);
    }

    /// <summary>
    /// Single-target scenario: CausticBite push must also target the highest-HP enemy directly.
    /// Stormbite is marked not-ready (on cooldown) so the method falls through to CausticBite.
    /// </summary>
    [Fact]
    public void TryPushApplyDots_CausticBite_TargetsHighestHpEnemy_SingleTarget()
    {
        var enemy = MakeEnemy(42ul);

        var targetingMock = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => enemy.Object);

        // Make Stormbite not ready so TryPushApplyDots falls through to CausticBite.
        // Both Windbite and Stormbite IDs must be marked not-ready.
        var actionService = MockBuilders.CreateMockActionService(
            isActionReady: id =>
                id != BRDActions.Stormbite.ActionId &&
                id != BRDActions.Windbite.ActionId);

        var context = CalliopeTestContext.Create(
            targetingService: targetingMock,
            actionService: actionService,
            inCombat: true,
            level: 100);

        var scheduler = SchedulerFactory.CreateForTest();
        var module = new DamageModule();

        module.CollectCandidates(context, scheduler, false);

        var queue = scheduler.InspectGcdQueue();
        var causticCandidate = queue
            .Single(c => c.Behavior.Action.ActionId == BRDActions.CausticBite.ActionId);

        Assert.Equal(42ul, causticCandidate.TargetId);
        Assert.Null(causticCandidate.Behavior.TargetingOverride);
    }

    /// <summary>
    /// Multi-target scenario: the EnemyStrategy target is an add (low HP, GameObjectId=1)
    /// but FindEnemy(HighestHp) returns a boss (GameObjectId=2). Both targets have null
    /// StatusList (no DoT). Stormbite must be pushed to the boss (highest-HP target),
    /// not to the add, because the boss lives longer and the DoT check should match dispatch.
    /// </summary>
    [Fact]
    public void TryPushApplyDots_Stormbite_RoutesToHighestHpTarget_WhenDifferentFromStrategyTarget()
    {
        var addTarget  = MakeEnemy(1ul, hp: 5_000u);   // primary strategy target (lowest HP)
        var bossTarget = MakeEnemy(2ul, hp: 500_000u); // highest-HP target

        // Strategy target returns addTarget; HighestHp returns bossTarget.
        var targetingMock = MockBuilders.CreateMockTargetingService(
            findEnemy: (strategy, _, _) =>
                strategy == EnemyTargetingStrategy.HighestHp ? bossTarget.Object : addTarget.Object);

        // StatusList=null on both enemies means neither has the DoT; Stormbite fires and returns.
        var context = CalliopeTestContext.Create(
            targetingService: targetingMock,
            inCombat: true,
            level: 100);

        var scheduler = SchedulerFactory.CreateForTest();
        var module = new DamageModule();

        module.CollectCandidates(context, scheduler, false);

        var queue = scheduler.InspectGcdQueue();
        var stormbiteCandidate = queue
            .Single(c => c.Behavior.Action.ActionId == BRDActions.Stormbite.ActionId);

        // Must push to the boss (GameObjectId=2), not the add (GameObjectId=1).
        Assert.Equal(2ul, stormbiteCandidate.TargetId);
    }

    /// <summary>
    /// Fallback: when FindEnemy(HighestHp) returns null, TryPushApplyDots falls back
    /// to the primary strategy target.
    /// </summary>
    [Fact]
    public void TryPushApplyDots_Stormbite_FallsBackToPrimaryTarget_WhenHighestHpReturnsNull()
    {
        var primaryTarget = MakeEnemy(99ul);

        // HighestHp returns null; any other strategy returns primaryTarget.
        var targetingMock = MockBuilders.CreateMockTargetingService(
            findEnemy: (strategy, _, _) =>
                strategy == EnemyTargetingStrategy.HighestHp ? null : primaryTarget.Object);

        var context = CalliopeTestContext.Create(
            targetingService: targetingMock,
            inCombat: true,
            level: 100);

        var scheduler = SchedulerFactory.CreateForTest();
        var module = new DamageModule();

        module.CollectCandidates(context, scheduler, false);

        var queue = scheduler.InspectGcdQueue();
        var stormbiteCandidate = queue
            .Single(c => c.Behavior.Action.ActionId == BRDActions.Stormbite.ActionId);

        // Falls back to the primary target.
        Assert.Equal(99ul, stormbiteCandidate.TargetId);
    }

    /// <summary>
    /// When EnemyStrategy is CurrentTarget, TryPushApplyDots must use the strategy-resolved
    /// target directly and must NOT redirect to the highest-HP enemy, even when a higher-HP
    /// enemy exists. The player's explicit targeting choice overrides the aggregate redirect.
    /// </summary>
    [Fact]
    public void TryPushApplyDots_Stormbite_UsesStrategyTarget_WhenStrategyIsCurrentTarget()
    {
        var currentTarget = MakeEnemy(10ul, hp: 5_000u);  // player's explicit current target (low HP)
        var bossTarget    = MakeEnemy(20ul, hp: 500_000u); // highest-HP enemy (should NOT be used)

        // With CurrentTarget strategy: FindEnemy returns currentTarget for any strategy.
        // For HighestHp it would return bossTarget -- the redirect we want to suppress.
        var targetingMock = MockBuilders.CreateMockTargetingService(
            findEnemy: (strategy, _, _) =>
                strategy == EnemyTargetingStrategy.HighestHp
                    ? bossTarget.Object
                    : currentTarget.Object);

        var config = CalliopeTestContext.CreateDefaultBardConfiguration();
        config.Targeting.EnemyStrategy = EnemyTargetingStrategy.CurrentTarget;

        var context = CalliopeTestContext.Create(
            config: config,
            targetingService: targetingMock,
            inCombat: true,
            level: 100);

        var scheduler = SchedulerFactory.CreateForTest();
        var module = new DamageModule();

        module.CollectCandidates(context, scheduler, false);

        var queue = scheduler.InspectGcdQueue();
        var stormbiteCandidate = queue
            .Single(c => c.Behavior.Action.ActionId == BRDActions.Stormbite.ActionId);

        // Must push to the player's current target (id=10), NOT the boss (id=20).
        Assert.Equal(10ul, stormbiteCandidate.TargetId);
    }

    /// <summary>
    /// When EnemyStrategy is FocusTarget, TryPushApplyDots must use the strategy-resolved
    /// target directly and must NOT redirect to the highest-HP enemy.
    /// </summary>
    [Fact]
    public void TryPushApplyDots_Stormbite_UsesStrategyTarget_WhenStrategyIsFocusTarget()
    {
        var focusTarget = MakeEnemy(30ul, hp: 8_000u);   // player's explicit focus target (low HP)
        var bossTarget  = MakeEnemy(40ul, hp: 500_000u); // highest-HP enemy (should NOT be used)

        var targetingMock = MockBuilders.CreateMockTargetingService(
            findEnemy: (strategy, _, _) =>
                strategy == EnemyTargetingStrategy.HighestHp
                    ? bossTarget.Object
                    : focusTarget.Object);

        var config = CalliopeTestContext.CreateDefaultBardConfiguration();
        config.Targeting.EnemyStrategy = EnemyTargetingStrategy.FocusTarget;

        var context = CalliopeTestContext.Create(
            config: config,
            targetingService: targetingMock,
            inCombat: true,
            level: 100);

        var scheduler = SchedulerFactory.CreateForTest();
        var module = new DamageModule();

        module.CollectCandidates(context, scheduler, false);

        var queue = scheduler.InspectGcdQueue();
        var stormbiteCandidate = queue
            .Single(c => c.Behavior.Action.ActionId == BRDActions.Stormbite.ActionId);

        // Must push to the player's focus target (id=30), NOT the boss (id=40).
        Assert.Equal(30ul, stormbiteCandidate.TargetId);
    }
}
