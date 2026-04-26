using System;
using Olympus.Services.Pull;
using Xunit;

namespace Olympus.Tests.Services;

public class PullIntentServiceTests
{
    private static readonly DateTime T0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Initial_state_is_None()
    {
        var sut = new PullIntentService();
        Assert.Equal(PullIntent.None, sut.Current);
    }

    [Fact]
    public void Cast_on_hostile_target_triggers_Imminent_immediately()
    {
        var sut = new PullIntentService();
        sut.Update(isPlayerCasting: true, isCastTargetHostile: true,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: false, utcNow: T0);
        Assert.Equal(PullIntent.Imminent, sut.Current);
    }

    [Fact]
    public void Cast_on_non_hostile_target_does_not_trigger_Imminent()
    {
        var sut = new PullIntentService();
        sut.Update(isPlayerCasting: true, isCastTargetHostile: false,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: false, utcNow: T0);
        Assert.Equal(PullIntent.None, sut.Current);
    }

    [Fact]
    public void Hostile_queued_action_requires_100ms_confirmation()
    {
        var sut = new PullIntentService();
        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: 16458u, isQueuedActionHostile: true,
                   isInCombat: false, utcNow: T0);
        Assert.Equal(PullIntent.None, sut.Current);  // not yet confirmed

        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: 16458u, isQueuedActionHostile: true,
                   isInCombat: false, utcNow: T0.AddMilliseconds(100));
        Assert.Equal(PullIntent.Imminent, sut.Current);
    }

    [Fact]
    public void Queued_action_canceled_within_100ms_does_not_trigger_Imminent()
    {
        var sut = new PullIntentService();
        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: 16458u, isQueuedActionHostile: true,
                   isInCombat: false, utcNow: T0);
        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: false, utcNow: T0.AddMilliseconds(50));
        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: false, utcNow: T0.AddMilliseconds(150));
        Assert.Equal(PullIntent.None, sut.Current);
    }

    [Fact]
    public void Imminent_transitions_to_Active_when_combat_starts()
    {
        var sut = new PullIntentService();
        sut.Update(isPlayerCasting: true, isCastTargetHostile: true,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: false, utcNow: T0);
        Assert.Equal(PullIntent.Imminent, sut.Current);

        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: true, utcNow: T0.AddMilliseconds(200));
        Assert.Equal(PullIntent.Active, sut.Current);
    }

    [Fact]
    public void Active_transitions_to_None_after_2_seconds_in_combat()
    {
        var sut = new PullIntentService();
        sut.Update(isPlayerCasting: true, isCastTargetHostile: true,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: false, utcNow: T0);
        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: true, utcNow: T0.AddMilliseconds(200));
        Assert.Equal(PullIntent.Active, sut.Current);

        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: true, utcNow: T0.AddSeconds(2.5));
        Assert.Equal(PullIntent.None, sut.Current);
    }

    [Fact]
    public void Imminent_transitions_to_None_after_3_second_timeout_without_combat()
    {
        var sut = new PullIntentService();
        sut.Update(isPlayerCasting: true, isCastTargetHostile: true,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: false, utcNow: T0);
        Assert.Equal(PullIntent.Imminent, sut.Current);

        // Player stops casting, no combat
        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: false, utcNow: T0.AddSeconds(3.5));
        Assert.Equal(PullIntent.None, sut.Current);
    }

    [Fact]
    public void Combat_with_no_prior_intent_jumps_directly_to_Active()
    {
        var sut = new PullIntentService();
        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: true, utcNow: T0);
        Assert.Equal(PullIntent.Active, sut.Current);
    }

    [Fact]
    public void Active_does_not_re_enter_after_opener_window_while_combat_continues()
    {
        var sut = new PullIntentService();
        // Combat starts directly (instant opener, no prior intent).
        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: true, utcNow: T0);
        Assert.Equal(PullIntent.Active, sut.Current);

        // 2.5s in combat: opener window has expired.
        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: true, utcNow: T0.AddSeconds(2.5));
        Assert.Equal(PullIntent.None, sut.Current);

        // Still in combat 30s later -- Active must NOT re-enter.
        sut.Update(isPlayerCasting: false, isCastTargetHostile: false,
                   queuedActionId: null, isQueuedActionHostile: false,
                   isInCombat: true, utcNow: T0.AddSeconds(30));
        Assert.Equal(PullIntent.None, sut.Current);
    }

    [Fact]
    public void Active_re_enters_on_a_fresh_pull_after_combat_ends()
    {
        var sut = new PullIntentService();
        // First pull
        sut.Update(false, false, null, false, isInCombat: true, utcNow: T0);
        sut.Update(false, false, null, false, isInCombat: true, utcNow: T0.AddSeconds(2.5));
        Assert.Equal(PullIntent.None, sut.Current);

        // Combat ends (wipe / clear)
        sut.Update(false, false, null, false, isInCombat: false, utcNow: T0.AddSeconds(10));
        Assert.Equal(PullIntent.None, sut.Current);

        // Fresh pull
        sut.Update(false, false, null, false, isInCombat: true, utcNow: T0.AddSeconds(15));
        Assert.Equal(PullIntent.Active, sut.Current);
    }
}
