using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.ApolloCore.Modules.Healing;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Tests for AoEHealingHandler.CollectCandidates.
///
/// The primary gate is the injured-count threshold: push only when
/// injuredCount &gt;= config.Healing.AoEHealMinTargets (default 3).
///
/// Two production-code details that require specific test setup:
///
///   1. ThinAir gate: when EnableThinAir=true and ThinAir is ready (which is the
///      default because IsActionReady defaults to true and the player mock has
///      StatusList=null so HasThinAir=false → ShouldWaitForThinAir returns true).
///      Set config.Buffs.EnableThinAir = false in all push tests.
///
///   2. SelectBestAoEHeal: the handler returns early when the selector returns
///      null. Override healingSpellSelector to return WHMActions.Medica in push
///      tests.
///
///   3. AoEHealingHandler creates an INLINE AbilityBehavior (not from ApolloAbilities),
///      so assertions must use c.Behavior.Action.ActionId rather than object identity.
/// </summary>
public class AoEHealingHandlerTests
{
    private readonly AoEHealingHandler _handler = new();

    // -----------------------------------------------------------------------
    // 1. Enough injured (>= minTargets) + spell selector returns Medica → GCD pushed
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_EnoughInjuredAndSpellSelected_PushesGcdAtPriority34()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.AoEHealMinTargets = 3;
        config.Buffs.EnableThinAir = false; // prevent ThinAir wait from blocking push

        // Three injured party members with a non-zero average missing HP.
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper
            .Setup(p => p.CountPartyMembersNeedingAoEHeal(
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
                It.IsAny<int>()))
            .Returns((3, false, new List<(uint, string)> { (1u, "T"), (2u, "D1"), (3u, "D2") }, 5000));

        // Return Medica as the chosen AoE spell.
        var healingSpellSelector = ApolloTestContext.CreateDefaultHealingSpellSelector();
        healingSpellSelector
            .Setup(x => x.SelectBestAoEHeal(
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
                It.IsAny<int>(),
                It.IsAny<int>(),
                It.IsAny<bool>(),
                It.IsAny<bool>(),
                It.IsAny<int>(),
                It.IsAny<IBattleChara?>()))
            .Returns(((ActionDefinition?)WHMActions.Medica, 5000, (IBattleChara?)null));

        var context = ApolloTestContext.Create(
            config: config,
            partyHelper: partyHelper,
            healingSpellSelector: healingSpellSelector);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        var queue = scheduler.InspectGcdQueue();
        Assert.Contains(queue, c =>
            c.Behavior.Action.ActionId == WHMActions.Medica.ActionId &&
            c.Priority == (int)HealingPriority.AoEHeal);
    }

    // -----------------------------------------------------------------------
    // 2. Below injured threshold (injuredCount < minTargets, cureIIITargetCount=0) → no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_BelowInjuredThreshold_NoPush()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.AoEHealMinTargets = 3;

        // Only 2 injured when threshold is 3 → falls through both hasEnough checks.
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper
            .Setup(p => p.CountPartyMembersNeedingAoEHeal(
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
                It.IsAny<int>()))
            .Returns((2, false, new List<(uint, string)> { (1u, "T"), (2u, "D1") }, 3000));

        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
    }

    // -----------------------------------------------------------------------
    // 3. Master healing switch disabled → handler exits at first check, no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_HealingMasterDisabled_NoPush()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.EnableHealing = false;

        // Even if partyHelper would return targets, the early-exit fires first.
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper
            .Setup(p => p.CountPartyMembersNeedingAoEHeal(
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
                It.IsAny<int>()))
            .Returns((5, false, new List<(uint, string)> { (1u, "T") }, 10000));

        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
    }

    // -----------------------------------------------------------------------
    // 4. Player level below Medica minimum (10) → handler exits at level check, no push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_LevelBelowMedicaMinimum_NoPush()
    {
        var config = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        config.Healing.AoEHealMinTargets = 1; // low threshold so it's definitely not the issue

        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper
            .Setup(p => p.CountPartyMembersNeedingAoEHeal(
                It.IsAny<Dalamud.Game.ClientState.Objects.SubKinds.IPlayerCharacter>(),
                It.IsAny<int>()))
            .Returns((3, false, new List<(uint, string)> { (1u, "T") }, 5000));

        // Medica.MinLevel = 10; pass level 9 to trigger the level gate.
        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper, level: 9);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Empty(scheduler.InspectGcdQueue());
    }
}
