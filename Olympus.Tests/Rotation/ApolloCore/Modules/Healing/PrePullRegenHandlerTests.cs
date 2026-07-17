using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Rotation.ApolloCore.Abilities;
using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.ApolloCore.Modules;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.ApolloCore;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.ApolloCore.Modules.Healing;

public class PrePullRegenHandlerTests
{
    private static Mock<IPartyHelper> TankHelper()
    {
        var tank = MockBuilders.CreateMockBattleChara(entityId: 2u, currentHp: 50000, maxHp: 50000);
        var ph = MockBuilders.CreateMockPartyHelper();
        ph.Setup(p => p.FindTankInParty(It.IsAny<IPlayerCharacter>()))
          .Returns(tank.Object);
        return ph;
    }

    // 1. Countdown at or below 4s, tank present -> Regen pushed to GCD queue.
    [Fact]
    public void HealingModule_PrePullRegen_PushesGcdWhenCountdownAtOrBelow4()
    {
        var context = ApolloTestContext.Create(
            inCombat: false,
            countdownRemaining: 3f,
            partyHelper: TankHelper());
        var scheduler = SchedulerFactory.CreateForTest();

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.Contains(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.Regen);
    }

    // 2. Countdown above 4s -> no push (discriminates on countdown gate only).
    [Fact]
    public void HealingModule_PrePullRegen_NoPushWhenCountdownAbove4()
    {
        var context = ApolloTestContext.Create(
            inCombat: false,
            countdownRemaining: 5f,
            partyHelper: TankHelper());
        var scheduler = SchedulerFactory.CreateForTest();

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.Regen);
    }

    // 3. EnablePrePullActions disabled -> no push (same positive setup, only toggle differs).
    [Fact]
    public void HealingModule_PrePullRegen_NoPushWhenPrePullDisabled()
    {
        var cfg = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        cfg.PrePull.EnablePrePullActions = false;
        var context = ApolloTestContext.Create(
            config: cfg,
            inCombat: false,
            countdownRemaining: 3f,
            partyHelper: TankHelper());
        var scheduler = SchedulerFactory.CreateForTest();

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.Regen);
    }

    // 4. Healing.EnableRegen disabled -> no push (same positive setup, only per-job toggle differs).
    [Fact]
    public void HealingModule_PrePullRegen_NoPushWhenRegenToggleOff()
    {
        var cfg = ApolloTestContext.CreateDefaultWhiteMageConfiguration();
        cfg.Healing.EnableRegen = false;
        var context = ApolloTestContext.Create(
            config: cfg,
            inCombat: false,
            countdownRemaining: 3f,
            partyHelper: TankHelper());
        var scheduler = SchedulerFactory.CreateForTest();

        new HealingModule().CollectCandidates(context, scheduler, isMoving: false);

        Assert.DoesNotContain(scheduler.InspectGcdQueue(),
            c => c.Behavior == ApolloAbilities.Regen);
    }
}
