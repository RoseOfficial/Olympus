using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Modules.Healing;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Tests for RegenHandler.CollectCandidates.
///
/// The only gate that controls whether a GCD is pushed (other than the config
/// and level flags) is whether FindRegenTarget returns a non-null target. All
/// other runtime conditions (tank vs. non-tank threshold, IsMoving) are either
/// transparent to the mock or do not apply (Regen.CastTime = 0, so moving never
/// blocks it).
/// </summary>
public class RegenHandlerTests
{
    private readonly RegenHandler _handler = new();

    // -----------------------------------------------------------------------
    // 1. FindRegenTarget returns a target → GCD pushed at priority 35 (Regen)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_RegenTargetFound_PushesGcdAtPriority35()
    {
        var target = MockBuilders.CreateMockBattleChara(entityId: 7u);
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper
            .Setup(p => p.FindRegenTarget(
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<float>()))
            .Returns(target.Object);

        var context = ApolloTestContext.Create(partyHelper: partyHelper);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectGcdQueue();
        Assert.Contains(queue, c =>
            c.Behavior == ApolloAbilities.Regen &&
            c.Priority == (int)HealingPriority.Regen &&
            c.TargetId == target.Object.GameObjectId);
    }

    // -----------------------------------------------------------------------
    // 2. FindRegenTarget returns null → no push (everyone is healthy / has regen)
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_NoRegenTarget_NoPush()
    {
        // Default partyHelper.FindRegenTarget returns null
        var context = ApolloTestContext.Create();
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.Regen);
    }

    // -----------------------------------------------------------------------
    // 3. Master healing switch disabled → handler exits immediately, no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_HealingMasterDisabled_NoPush()
    {
        var target = MockBuilders.CreateMockBattleChara(entityId: 7u);
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper
            .Setup(p => p.FindRegenTarget(
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<float>()))
            .Returns(target.Object);

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = false;

        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.Regen);
    }

    // -----------------------------------------------------------------------
    // 4. Regen per-ability toggle disabled → no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_RegenToggleDisabled_NoPush()
    {
        var target = MockBuilders.CreateMockBattleChara(entityId: 7u);
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper
            .Setup(p => p.FindRegenTarget(
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<float>()))
            .Returns(target.Object);

        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.EnableRegen = false;

        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.Regen);
    }

    // -----------------------------------------------------------------------
    // 5. Player level below Regen minimum (35) → no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_LevelTooLow_NoPush()
    {
        var target = MockBuilders.CreateMockBattleChara(entityId: 7u);
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper
            .Setup(p => p.FindRegenTarget(
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
                It.IsAny<float>(),
                It.IsAny<float>(),
                It.IsAny<float>()))
            .Returns(target.Object);

        // Regen.MinLevel = 35; pass level 34 to exercise the gate
        var context = ApolloTestContext.Create(partyHelper: partyHelper, level: 34);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.Regen);
    }
}
