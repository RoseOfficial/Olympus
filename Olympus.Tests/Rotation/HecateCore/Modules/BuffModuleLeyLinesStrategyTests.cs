using Moq;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Models.Action;
using Olympus.Rotation.Common.Scheduling;
using Olympus.Rotation.HecateCore.Abilities;
using Olympus.Rotation.HecateCore.Context;
using Olympus.Rotation.HecateCore.Modules;
using Olympus.Services.Action;
using Olympus.Tests.Mocks;
using Olympus.Tests.Rotation.Common.Scheduling;
using Xunit;

namespace Olympus.Tests.Rotation.HecateCore.Modules;

/// <summary>
/// Tests for BlackMageConfig.LeylinesStrategy gating in BuffModule.TryPushLeyLines.
/// Discrimination standard: every negative shares the same setup as its paired positive;
/// the strategy value is the only variable that changes outcome.
/// </summary>
public class BuffModuleLeyLinesStrategyTests
{
    // Common "would push" preconditions: level 100, in Astral Fire, ready, stationary, no burst hold.
    private static (IHecateContext ctx, RotationScheduler sched) BuildReadyContext(
        LeylinesStrategy strategy,
        float combatDuration,
        Mock<IActionService>? actionSvc = null)
    {
        var config = HecateTestContext.CreateDefaultBlmConfiguration();
        config.BlackMage.EnableLeyLines = true;
        config.BlackMage.LeylinesStrategy = strategy;
        config.BlackMage.UseLeyLinesDuringBurst = false; // no burst-hold interference

        actionSvc ??= MockBuilders.CreateMockActionService();
        actionSvc.Setup(x => x.IsActionReady(It.IsAny<uint>())).Returns(true);
        actionSvc.Setup(x => x.ExecuteOgcd(It.IsAny<ActionDefinition>(), It.IsAny<ulong>())).Returns(true);

        var scheduler = SchedulerFactory.CreateForTest(actionService: actionSvc, config: config);
        var context = HecateTestContext.Create(
            config: config,
            actionService: actionSvc,
            inCombat: true,
            isMoving: false,
            combatDuration: combatDuration,
            inAstralFire: true,
            leyLinesReady: true,
            hasLeyLines: false,
            amplifierReady: false,
            manafontReady: false);

        return (context, scheduler);
    }

    // -----------------------------------------------------------------------
    // OnCooldown: pushes regardless of combat duration
    // -----------------------------------------------------------------------

    [Fact]
    public void OnCooldown_Pushes_AtCombatDuration10s()
    {
        var (ctx, sched) = BuildReadyContext(LeylinesStrategy.OnCooldown, combatDuration: 10f);
        new BuffModule().CollectCandidates(ctx, sched, isMoving: false);
        Assert.Contains(sched.InspectOgcdQueue(), c => c.Behavior == HecateAbilities.LeyLines);
    }

    [Fact]
    public void OnCooldown_Pushes_AtCombatDuration60s()
    {
        var (ctx, sched) = BuildReadyContext(LeylinesStrategy.OnCooldown, combatDuration: 60f);
        new BuffModule().CollectCandidates(ctx, sched, isMoving: false);
        Assert.Contains(sched.InspectOgcdQueue(), c => c.Behavior == HecateAbilities.LeyLines);
    }

    // -----------------------------------------------------------------------
    // OpenerOnly: pushes inside 25s, blocked at or above 25s
    // -----------------------------------------------------------------------

    [Fact]
    public void OpenerOnly_Pushes_WhenCombatDuration10s()
    {
        var (ctx, sched) = BuildReadyContext(LeylinesStrategy.OpenerOnly, combatDuration: 10f);
        new BuffModule().CollectCandidates(ctx, sched, isMoving: false);
        Assert.Contains(sched.InspectOgcdQueue(), c => c.Behavior == HecateAbilities.LeyLines);
    }

    /// <summary>Discrimination: same setup as above, only combatDuration differs.</summary>
    [Fact]
    public void OpenerOnly_DoesNotPush_WhenCombatDuration60s()
    {
        var (ctx, sched) = BuildReadyContext(LeylinesStrategy.OpenerOnly, combatDuration: 60f);
        new BuffModule().CollectCandidates(ctx, sched, isMoving: false);
        Assert.DoesNotContain(sched.InspectOgcdQueue(), c => c.Behavior == HecateAbilities.LeyLines);
    }

    [Fact]
    public void OpenerOnly_DoesNotPush_AtExactly25s()
    {
        var (ctx, sched) = BuildReadyContext(LeylinesStrategy.OpenerOnly, combatDuration: 25f);
        new BuffModule().CollectCandidates(ctx, sched, isMoving: false);
        Assert.DoesNotContain(sched.InspectOgcdQueue(), c => c.Behavior == HecateAbilities.LeyLines);
    }

    // -----------------------------------------------------------------------
    // Manual: never pushes regardless of any other state
    // -----------------------------------------------------------------------

    [Fact]
    public void Manual_NeverPushes_EvenWhenAllConditionsMet()
    {
        var (ctx, sched) = BuildReadyContext(LeylinesStrategy.Manual, combatDuration: 10f);
        new BuffModule().CollectCandidates(ctx, sched, isMoving: false);
        Assert.DoesNotContain(sched.InspectOgcdQueue(), c => c.Behavior == HecateAbilities.LeyLines);
    }

    /// <summary>Discrimination: strategy=OnCooldown with identical setup does push.</summary>
    [Fact]
    public void Manual_ToggleOff_Companion_OnCooldownDoesPush()
    {
        var (ctx, sched) = BuildReadyContext(LeylinesStrategy.OnCooldown, combatDuration: 10f);
        new BuffModule().CollectCandidates(ctx, sched, isMoving: false);
        Assert.Contains(sched.InspectOgcdQueue(), c => c.Behavior == HecateAbilities.LeyLines);
    }
}
