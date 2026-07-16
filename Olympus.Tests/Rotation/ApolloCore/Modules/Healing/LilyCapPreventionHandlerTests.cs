using System.Numerics;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Context;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules.Healing;
using Olympus.Rotation.Common;
using Olympus.Services.Action;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Stats;
using Olympus.Services.Training;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Tests for LilyCapPreventionHandler.CollectCandidates.
///
/// DESIGN NOTE: Real ApolloContext.LilyCount reads SafeGameAccess.GetWhmLilyCount(),
/// which returns 0 in unit tests (no game pointer). Because the first handler gate is
/// "if (context.LilyCount &lt; 3) return", using the real context makes every test exit
/// without pushing. Direct IApolloContext mocking is required so LilyCount can be
/// returned as 3 (capped) from the mock property.
///
/// Two push paths are covered:
///   - AfflatusSolace (level >= 52, injuredInRange &lt; 2): pushed when
///     FindLowestHpPartyMember returns a target in range.
///   - AfflatusRapture (level >= 76, injuredInRange >= 2): pushed via the
///     AoE branch that fires before attempting Solace.
/// </summary>
public class LilyCapPreventionHandlerTests
{
    private readonly LilyCapPreventionHandler _handler = new();

    // -----------------------------------------------------------------------
    // 1. Lily count at cap (3), level >= 52, <2 injured → Solace GCD pushed at priority 80
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_LiliesCapped_LevelForSolace_PushesSolaceAtPriority80()
    {
        var target = CreateTarget(entityId: 8u);
        var partyHelper = CreatePartyHelper(
            lowestHpMember: target.Object,
            injuredInRange: 0); // fewer than 2 → Rapture branch skipped

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        // Default config already enables AfflatusSolace and LilyCapPrevention.

        var playerStatsService = MockBuilders.CreateMockPlayerStatsService();
        var context = BuildContext(
            lilyCount: 3,
            playerLevel: 60,
            config: config,
            partyHelper: partyHelper,
            playerStatsService: playerStatsService);

        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectGcdQueue();
        Assert.Contains(queue, c =>
            c.Behavior == ApolloAbilities.AfflatusSolace &&
            c.Priority == (int)HealingPriority.LilyCapPrevention &&
            c.TargetId == target.Object.GameObjectId);
    }

    // -----------------------------------------------------------------------
    // 2. Lily count at cap (3), level >= 76, injuredInRange >= 2 → Rapture GCD pushed
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_LiliesCapped_Level76Plus_MultipleInjured_PushesRaptureAtPriority80()
    {
        var partyHelper = CreatePartyHelper(
            lowestHpMember: null,   // Rapture path does not need FindLowestHpPartyMember
            injuredInRange: 3);     // >= 2 → triggers Rapture branch

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        var player = MockBuilders.CreateMockPlayerCharacter(level: 80);
        var playerStatsService = MockBuilders.CreateMockPlayerStatsService();

        var context = BuildContext(
            lilyCount: 3,
            playerLevel: 80,
            config: config,
            partyHelper: partyHelper,
            playerStatsService: playerStatsService);

        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectGcdQueue();
        Assert.Contains(queue, c =>
            c.Behavior == ApolloAbilities.AfflatusRapture &&
            c.Priority == (int)HealingPriority.LilyCapPrevention);
    }

    // -----------------------------------------------------------------------
    // 3. Lily count below cap (0) → first gate fires, nothing pushed
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_LilyCountBelowCap_NoPush()
    {
        var target = CreateTarget(entityId: 8u);
        var partyHelper = CreatePartyHelper(lowestHpMember: target.Object, injuredInRange: 0);

        var context = BuildContext(
            lilyCount: 0,
            playerLevel: 60,
            config: ApolloTestContext.CreateDefaultWhiteMageConfiguration(),
            partyHelper: partyHelper,
            playerStatsService: MockBuilders.CreateMockPlayerStatsService());

        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
    }

    // -----------------------------------------------------------------------
    // 4. EnableLilyCapPrevention = false → no push even when fully capped
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_LilyCapPreventionDisabled_NoPush()
    {
        var target = CreateTarget(entityId: 8u);
        var partyHelper = CreatePartyHelper(lowestHpMember: target.Object, injuredInRange: 0);

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.EnableLilyCapPrevention = false;

        var context = BuildContext(
            lilyCount: 3,
            playerLevel: 60,
            config: config,
            partyHelper: partyHelper,
            playerStatsService: MockBuilders.CreateMockPlayerStatsService());

        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
    }

    // -----------------------------------------------------------------------
    // 5. Level below AfflatusSolace minimum (52) → level gate fires, no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_LevelBelowSolaceMinimum_NoPush()
    {
        var target = CreateTarget(entityId: 8u);
        var partyHelper = CreatePartyHelper(lowestHpMember: target.Object, injuredInRange: 0);

        var context = BuildContext(
            lilyCount: 3,
            playerLevel: 51, // AfflatusSolaceMinLevel = 52
            config: ApolloTestContext.CreateDefaultWhiteMageConfiguration(),
            partyHelper: partyHelper,
            playerStatsService: MockBuilders.CreateMockPlayerStatsService());

        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
    }

    // -----------------------------------------------------------------------
    // 6. Capped, level 60, fully healed party (FindLowestHpPartyMember = null)
    //    → Solace falls back to self so lily regeneration is not wasted
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_LiliesCapped_PartyFullyHealed_PushesSolaceOnSelf()
    {
        // injuredInRange = 0 → Rapture branch skipped; FindLowestHp = null → self fallback
        var partyHelper = CreatePartyHelper(lowestHpMember: null, injuredInRange: 0);

        var context = BuildContext(
            lilyCount: 3,
            playerLevel: 60,
            config: ApolloTestContext.CreateDefaultWhiteMageConfiguration(),
            partyHelper: partyHelper,
            playerStatsService: MockBuilders.CreateMockPlayerStatsService());

        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectGcdQueue();
        Assert.Contains(queue, c =>
            c.Behavior == ApolloAbilities.AfflatusSolace &&
            c.Priority == (int)HealingPriority.LilyCapPrevention &&
            c.TargetId == 1ul); // CreateMockPlayerCharacter GameObjectId = 1 (self)
    }

    // -----------------------------------------------------------------------
    // 7. Blood lily 3/3 with Misery dispatchable (damage on, level 74+)
    //    → handler stands down so the lily heal cannot outrank Misery
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_BloodLiliesFull_MiseryDispatchable_NoPush()
    {
        var target = CreateTarget(entityId: 8u);
        var partyHelper = CreatePartyHelper(lowestHpMember: target.Object, injuredInRange: 0);

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableDamage = true;
        config.Damage.EnableAfflatusMisery = true;

        var context = BuildContext(
            lilyCount: 3,
            playerLevel: 80,
            config: config,
            partyHelper: partyHelper,
            playerStatsService: MockBuilders.CreateMockPlayerStatsService(),
            bloodLilyCount: 3);

        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
    }

    // -----------------------------------------------------------------------
    // 8. Blood lily 3/3 but damage disabled (heal-only config) → Misery can
    //    never fire, so overcap prevention keeps spending lilies
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_BloodLiliesFull_DamageDisabled_StillPushesSolace()
    {
        var target = CreateTarget(entityId: 8u);
        var partyHelper = CreatePartyHelper(lowestHpMember: target.Object, injuredInRange: 0);

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableDamage = false;

        var context = BuildContext(
            lilyCount: 3,
            playerLevel: 80,
            config: config,
            partyHelper: partyHelper,
            playerStatsService: MockBuilders.CreateMockPlayerStatsService(),
            bloodLilyCount: 3);

        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectGcdQueue();
        Assert.Contains(queue, c =>
            c.Behavior == ApolloAbilities.AfflatusSolace &&
            c.Priority == (int)HealingPriority.LilyCapPrevention &&
            c.TargetId == target.Object.GameObjectId);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Creates a target mock at the origin so DistanceHelper.IsInRange always passes.
    /// StatusList is null so HasNoHealStatus returns false (heals are allowed).
    /// </summary>
    private static Mock<IBattleChara> CreateTarget(uint entityId = 8u)
    {
        var mock = new Mock<IBattleChara>();
        mock.Setup(x => x.EntityId).Returns(entityId);
        mock.Setup(x => x.GameObjectId).Returns((ulong)entityId);
        mock.Setup(x => x.CurrentHp).Returns(40000u);
        mock.Setup(x => x.MaxHp).Returns(50000u);
        mock.Setup(x => x.Position).Returns(Vector3.Zero);
        mock.Setup(x => x.StatusList).Returns(
            (Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        return mock;
    }

    /// <summary>
    /// Creates a party-helper mock that controls both branch decisions:
    ///   - FindLowestHpPartyMember → used by AfflatusSolace path
    ///   - CountInjuredInAoERange → used to decide Rapture vs. Solace (>= 2 → Rapture)
    /// </summary>
    private static Mock<IPartyHelper> CreatePartyHelper(
        IBattleChara? lowestHpMember,
        int injuredInRange)
    {
        var mock = MockBuilders.CreateMockPartyHelper(lowestHpMember: lowestHpMember);
        mock.Setup(p => p.CountInjuredInAoERange(
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
                It.IsAny<float>(),
                It.IsAny<float>()))
            .Returns(injuredInRange);
        return mock;
    }

    /// <summary>
    /// Builds a mock IApolloContext wired for LilyCapPreventionHandler.
    /// LilyCount and BloodLilyCount are explicitly controllable — BloodLilyCount
    /// feeds the WHMActions.IsMiseryDispatchable stand-down gate.
    /// </summary>
    private static IApolloContext BuildContext(
        int lilyCount,
        byte playerLevel,
        Configuration config,
        Mock<IPartyHelper> partyHelper,
        Mock<IPlayerStatsService> playerStatsService,
        int bloodLilyCount = 0)
    {
        var player = MockBuilders.CreateMockPlayerCharacter(level: playerLevel);
        var actionService = MockBuilders.CreateMockActionService();
        var hpPredictionService = MockBuilders.CreateMockHpPredictionService();

        var ctx = new Mock<IApolloContext>();
        ctx.Setup(x => x.Configuration).Returns(config);
        ctx.Setup(x => x.Player).Returns(player.Object);
        ctx.Setup(x => x.ActionService).Returns(actionService.Object);
        ctx.Setup(x => x.PartyHelper).Returns(partyHelper.Object);
        ctx.Setup(x => x.PlayerStatsService).Returns(playerStatsService.Object);
        ctx.Setup(x => x.HpPredictionService).Returns(hpPredictionService.Object);
        ctx.Setup(x => x.HealingCoordination).Returns(new HealingCoordinationState());
        ctx.Setup(x => x.PartyCoordinationService).Returns((IPartyCoordinationService?)null);
        ctx.Setup(x => x.TrainingService).Returns((ITrainingService?)null);
        ctx.Setup(x => x.Debug).Returns(new DebugState());
        ctx.Setup(x => x.LilyCount).Returns(lilyCount);
        ctx.Setup(x => x.BloodLilyCount).Returns(bloodLilyCount); // gate-checked via IsMiseryDispatchable

        return ctx.Object;
    }
}
