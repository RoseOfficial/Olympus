using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Modules.Healing;
using Olympus.Services.Action;
using Olympus.Services.Calculation;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Tests for TetragrammatonHandler.CollectCandidates.
///
/// Critical paths under test:
///   - Overheal prevention: push only when healAmount &lt;= missingHp * overhealMultiplier.
///   - At-max-charges path: overhealMultiplier widens to 2.5 so near-full targets get healed
///     when a charge would otherwise be capped.
///   - Config toggle, level gate, cooldown gate.
///
/// Uses ApolloTestContext.Create() with config.Healing.UseDamageIntakeTriage = false
/// so FindLowestHpPartyMember (which MockBuilders exposes) is used instead of
/// FindMostEndangeredPartyMember (not set up in the default factory mock).
///
/// HealingCalculator.ResetCalibration() is called in the constructor so static
/// calibration data from other test classes does not influence healAmount.
/// </summary>
public class TetragrammatonHandlerTests
{
    private readonly TetragrammatonHandler _handler = new();

    public TetragrammatonHandlerTests()
    {
        HealingCalculator.ResetCalibration();
    }

    // -----------------------------------------------------------------------
    // 1. Target with large missing HP → healAmount < 1.5 * missingHp → push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_TargetWithLargeMissingHp_PushesOgcdAtPriority25()
    {
        // currentHp=1000, maxHp=50000 → missingHp=49000, threshold=73500
        // Tetragrammaton estHeal ≈18224, well below 73500 → should push
        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 1000, maxHp: 50000);
        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.UseDamageIntakeTriage = false;

        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectOgcdQueue();
        Assert.Contains(queue, c =>
            c.Behavior == ApolloAbilities.Tetragrammaton &&
            c.Priority == (int)HealingPriority.Tetragrammaton &&
            c.TargetId == target.Object.GameObjectId);
    }

    // -----------------------------------------------------------------------
    // 2. Target almost full HP → healAmount > 1.5 * missingHp → overheal blocked
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_TargetNearFullHp_OverhealPreventsP()
    {
        // currentHp=49999, maxHp=50000 → missingHp=1, threshold=1.5
        // estHeal ≈18224 > 1.5 → blocked by overheal guard
        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 49999, maxHp: 50000);
        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.UseDamageIntakeTriage = false;

        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Tetragrammaton);
    }

    // -----------------------------------------------------------------------
    // 3. At max charges with 80% HP target: normal 1.5x blocks; 2.5x cap allows push
    //
    //    missingHp = 10000 (80% HP). Normal threshold = 15000 (1.5 * 10000 = 15000).
    //    estHeal ≈18224 > 15000 → blocked normally.
    //    Max charges threshold = 25000 (2.5 * 10000). 18224 < 25000 → pushed.
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_AtMaxCharges_WiderOverhealAllowsPush()
    {
        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 40000, maxHp: 50000);
        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.UseDamageIntakeTriage = false;

        // Force at-max-charges: GetCurrentCharges = 2, GetMaxCharges = 2
        var actionService = MockBuilders.CreateMockActionService();
        actionService.Setup(x => x.GetCurrentCharges(WHMActions.Tetragrammaton.ActionId))
            .Returns(2u);

        var context = ApolloTestContext.Create(
            config: config, partyHelper: partyHelper, actionService: actionService);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Tetragrammaton &&
                 c.Priority == (int)HealingPriority.Tetragrammaton);
    }

    // -----------------------------------------------------------------------
    // 4. Same 80% HP target but normal charges: normal 1.5x BLOCKS (no push).
    //    This is the counterpart to test 3, proving the threshold actually changed.
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_NormalCharges_NormalOverhealBlocks()
    {
        // Same target as test 3; without max-charges the 1.5x multiplier blocks.
        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 40000, maxHp: 50000);
        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.UseDamageIntakeTriage = false;
        // Default mock: GetCurrentCharges=0, GetMaxCharges=2 → isAtMaxCharges = false

        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Tetragrammaton);
    }

    // -----------------------------------------------------------------------
    // 5. No party member in range (FindLowestHpPartyMember returns null) → no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_NoTarget_NoPush()
    {
        // Default partyHelper returns null for FindLowestHpPartyMember
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.UseDamageIntakeTriage = false;

        var context = ApolloTestContext.Create(config: config);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Tetragrammaton);
    }

    // -----------------------------------------------------------------------
    // 6. Tetragrammaton per-ability toggle disabled → ActionValidator rejects, no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_TetragrammatonToggleDisabled_NoPush()
    {
        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 1000, maxHp: 50000);
        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.UseDamageIntakeTriage = false;
        config.Healing.EnableTetragrammaton = false;

        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Tetragrammaton);
    }

    // -----------------------------------------------------------------------
    // 7. Player level below Tetragrammaton minimum (60) → ActionValidator rejects, no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_LevelTooLow_NoPush()
    {
        var target = MockBuilders.CreateMockBattleChara(entityId: 5u, currentHp: 1000, maxHp: 50000);
        var partyHelper = MockBuilders.CreateMockPartyHelper(lowestHpMember: target.Object);

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.UseDamageIntakeTriage = false;

        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper, level: 59);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.Tetragrammaton);
    }
}
