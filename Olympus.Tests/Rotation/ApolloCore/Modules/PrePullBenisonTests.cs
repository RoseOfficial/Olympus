using Dalamud.Game.ClientState.Objects.SubKinds;
using Moq;
using Olympus.Data;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.ApolloCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ApolloCore.Modules;

public class PrePullBenisonTests
{
    private static (Mock<IPartyHelper> ph, Mock<IActionService> svc) HappyPathSetup()
    {
        var tank = MockBuilders.CreateMockBattleChara(entityId: 2u, currentHp: 50000, maxHp: 50000);
        var ph = MockBuilders.CreateMockPartyHelper();
        ph.Setup(p => p.FindTankInParty(It.IsAny<IPlayerCharacter>()))
          .Returns(tank.Object);

        var svc = MockBuilders.CreateMockActionService();
        svc.Setup(a => a.IsActionReady(WHMActions.DivineBenison.ActionId)).Returns(true);
        return (ph, svc);
    }

    // 1. Countdown at or below 5s, tank present, action ready -> DivineBenison in oGCD queue.
    [Fact]
    public void DefensiveModule_PrePullBenison_PushesOgcdWhenCountdownAtOrBelow5()
    {
        var (ph, svc) = HappyPathSetup();
        var context = ApolloTestContext.Create(
            inCombat: false,
            countdownRemaining: 4f,
            partyHelper: ph,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new DefensiveModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.DivineBenison);
    }

    // 2. Countdown above 5s -> no push (same setup; only countdown differs).
    [Fact]
    public void DefensiveModule_PrePullBenison_NoPushWhenCountdownAbove5()
    {
        var (ph, svc) = HappyPathSetup();
        var context = ApolloTestContext.Create(
            inCombat: false,
            countdownRemaining: 6f,
            partyHelper: ph,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new DefensiveModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.DivineBenison);
    }

    // 3. EnablePrePullActions disabled -> no push (same positive setup; only toggle differs).
    [Fact]
    public void DefensiveModule_PrePullBenison_NoPushWhenPrePullDisabled()
    {
        var cfg = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        cfg.PrePull.EnablePrePullActions = false;
        var (ph, svc) = HappyPathSetup();
        var context = ApolloTestContext.Create(
            config: cfg,
            inCombat: false,
            countdownRemaining: 4f,
            partyHelper: ph,
            actionService: svc);
        var scheduler = SchedulerFactory.CreateForTest(svc);

        new DefensiveModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectOgcdQueue(),
            c => c.Behavior == ApolloAbilities.DivineBenison);
    }
}
