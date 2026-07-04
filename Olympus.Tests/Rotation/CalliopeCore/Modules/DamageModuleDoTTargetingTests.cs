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
/// Verifies that the DoT-apply path in DamageModule carries the HighestHp targeting
/// override so the scheduler can land initial DoTs on the longest-lived enemy.
/// </summary>
public class DamageModuleDoTTargetingTests
{
    [Fact]
    public void TryPushApplyDots_Stormbite_CarriesHighestHpOverride()
    {
        // Arrange: a single valid enemy target
        var mockTarget = new Mock<IBattleNpc>();
        mockTarget.Setup(x => x.MaxHp).Returns(100_000u);
        mockTarget.Setup(x => x.CurrentHp).Returns(100_000u);
        mockTarget.Setup(x => x.GameObjectId).Returns(42ul);
        mockTarget.Setup(x => x.IsCasting).Returns(false);
        mockTarget.Setup(x => x.IsCastInterruptible).Returns(false);

        // FindEnemy returns the mock target; everything else returns null/default.
        var targetingMock = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => mockTarget.Object);

        // HasStormbite = false  → apply path fires for Stormbite
        // HasCausticBite = true → apply path returns after Stormbite (no CausticBite push)
        var context = CalliopeTestContext.Create(
            targetingService: targetingMock,
            hasStormbite: false,
            hasCausticBite: true,
            inCombat: true,
            level: 100);

        var scheduler = SchedulerFactory.CreateForTest();
        var module = new DamageModule();

        // Act
        module.CollectCandidates(context, scheduler, false);

        // Assert: Stormbite candidate must be in the GCD queue with HighestHp override.
        var queue = scheduler.InspectGcdQueue();
        var stormbiteCandidate = queue
            .Single(c => c.Behavior.Action.ActionId == BRDActions.Stormbite.ActionId);
        Assert.Equal(EnemyTargetingStrategy.HighestHp, stormbiteCandidate.Behavior.TargetingOverride);
    }

    [Fact]
    public void TryPushApplyDots_CausticBite_CarriesHighestHpOverride()
    {
        // Arrange: Stormbite already applied, CausticBite missing → only CausticBite fires.
        var mockTarget = new Mock<IBattleNpc>();
        mockTarget.Setup(x => x.MaxHp).Returns(100_000u);
        mockTarget.Setup(x => x.CurrentHp).Returns(100_000u);
        mockTarget.Setup(x => x.GameObjectId).Returns(42ul);
        mockTarget.Setup(x => x.IsCasting).Returns(false);
        mockTarget.Setup(x => x.IsCastInterruptible).Returns(false);

        var targetingMock = MockBuilders.CreateMockTargetingService(
            findEnemy: (_, _, _) => mockTarget.Object);

        // HasStormbite = true   → Stormbite apply is skipped
        // HasCausticBite = false → CausticBite apply fires
        var context = CalliopeTestContext.Create(
            targetingService: targetingMock,
            hasStormbite: true,
            hasCausticBite: false,
            inCombat: true,
            level: 100);

        var scheduler = SchedulerFactory.CreateForTest();
        var module = new DamageModule();

        // Act
        module.CollectCandidates(context, scheduler, false);

        // Assert: CausticBite candidate must carry the HighestHp override.
        var queue = scheduler.InspectGcdQueue();
        var causticCandidate = queue
            .Single(c => c.Behavior.Action.ActionId == BRDActions.CausticBite.ActionId);
        Assert.Equal(EnemyTargetingStrategy.HighestHp, causticCandidate.Behavior.TargetingOverride);
    }
}
