using Dalamud.Game.ClientState.Objects.SubKinds;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules.Healing;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ApolloCore.Modules.Healing;

/// <summary>
/// Tests for AssizeHealingHandler.CollectCandidates.
///
/// Critical paths under test:
///   - EnableAssize=false, EnableAssizeHealing=true  -> no push (master toggle blocks)
///   - EnableAssize=true,  EnableAssizeHealing=true  -> push at AssizeHealing priority
///   - EnableAssize=true,  EnableAssizeHealing=false -> no push (path-specific toggle blocks)
///
/// Uses ApolloTestContext.Create() with a partyHelper stub that reports enough injured
/// members and low enough avg HP to satisfy the healing trigger. Assize MinLevel=56;
/// all tests run at level 90 (ApolloTestContext default).
/// </summary>
public class AssizeHealingHandlerTests
{
    private readonly AssizeHealingHandler _handler = new();

    // -----------------------------------------------------------------------
    // Shared helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Returns a partyHelper mock that reports 4 injured members at 70% avg HP
    /// so all conditions (injuredCount >= 3, avgHp < 0.85) pass.
    /// </summary>
    private static Mock<IPartyHelper> CreateInjuredPartyHelper()
    {
        var partyHelper = MockBuilders.CreateMockPartyHelper();
        partyHelper
            .Setup(p => p.CalculatePartyHealthMetrics(It.IsAny<IPlayerCharacter>()))
            .Returns((0.70f, 0.60f, 4)); // avgHp=70%, lowestHp=60%, injuredCount=4
        return partyHelper;
    }

    private static Configuration CreateConfig(
        bool enableAssize = true,
        bool enableAssizeHealing = true)
    {
        var cfg = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        cfg.EnableHealing = true;
        cfg.Healing.EnableAssize = enableAssize;
        cfg.Healing.EnableAssizeHealing = enableAssizeHealing;
        cfg.Healing.AssizeHealingMinTargets = 3;
        cfg.Healing.AssizeHealingHpThreshold = 0.85f;
        return cfg;
    }

    // -----------------------------------------------------------------------
    // 1. EnableAssize=false, EnableAssizeHealing=true -> master toggle blocks push
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_EnableAssizeFalse_EnableAssizeHealingTrue_NoPush()
    {
        var config = CreateConfig(enableAssize: false, enableAssizeHealing: true);
        var partyHelper = CreateInjuredPartyHelper();

        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.AssizeHeal);
    }

    // -----------------------------------------------------------------------
    // 2. EnableAssize=true, EnableAssizeHealing=true -> all gates pass, push queued
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_BothTogglesTrue_InjuredParty_PushesAtAssizeHealingPriority()
    {
        var config = CreateConfig(enableAssize: true, enableAssizeHealing: true);
        var partyHelper = CreateInjuredPartyHelper();

        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.AssizeHeal &&
                 c.Priority == (int)HealingPriority.AssizeHealing);
    }

    // -----------------------------------------------------------------------
    // 3. EnableAssize=true, EnableAssizeHealing=false -> path-specific toggle blocks
    // -----------------------------------------------------------------------
    [Fact]
    public void CollectCandidates_EnableAssizeTrue_EnableAssizeHealingFalse_NoPush()
    {
        var config = CreateConfig(enableAssize: true, enableAssizeHealing: false);
        var partyHelper = CreateInjuredPartyHelper();

        var context = ApolloTestContext.Create(config: config, partyHelper: partyHelper);
        var scheduler = SchedulerFactory.CreateForTest();

        _handler.CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.AssizeHeal);
    }
}
