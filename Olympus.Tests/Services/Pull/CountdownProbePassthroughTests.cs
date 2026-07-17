using System;
using Moq;
using Olympus.Services.Pull;
using Xunit;

namespace Olympus.Tests.Services.Pull;

/// <summary>
/// Verifies that PullIntentService.CountdownRemaining reflects whatever
/// ICountdownProbe.GetCountdownRemaining() returns, including null (no probe,
/// or probe cancels) and positive float (active countdown).
///
/// Discrimination standard: every negative test shares the positive's Update()
/// call setup — the only discriminating variable is whether the probe is present
/// and what value it returns.
/// </summary>
public class CountdownProbePassthroughTests
{
    private static readonly DateTime T0 = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static void CallUpdate(PullIntentService sut, DateTime utcNow)
        => sut.Update(
            isPlayerCasting: false, isCastTargetHostile: false,
            queuedActionId: null, isQueuedActionHostile: false,
            isInCombat: false, utcNow: utcNow);

    // -----------------------------------------------------------------------
    // Positive: probe present and returning a value
    // -----------------------------------------------------------------------

    [Fact]
    public void CountdownRemaining_propagates_probe_value()
    {
        var probe = new Mock<ICountdownProbe>();
        probe.Setup(p => p.GetCountdownRemaining()).Returns(5.0f);

        var sut = new PullIntentService(probe.Object);
        CallUpdate(sut, T0);

        Assert.Equal(5.0f, sut.CountdownRemaining);
    }

    // -----------------------------------------------------------------------
    // Negative: no probe injected — property stays null (feature inert path)
    // -----------------------------------------------------------------------

    [Fact]
    public void CountdownRemaining_is_null_when_no_probe_injected()
    {
        // Same Update() args as the positive test; ONLY difference is no probe.
        var sut = new PullIntentService();   // countdownProbe defaults to null
        CallUpdate(sut, T0);

        Assert.Null(sut.CountdownRemaining);
    }

    // -----------------------------------------------------------------------
    // Cancellation: probe returns a value then null — property must follow
    // -----------------------------------------------------------------------

    [Fact]
    public void CountdownRemaining_becomes_null_when_probe_cancels()
    {
        var probe = new Mock<ICountdownProbe>();

        // Frame 1: countdown active
        probe.Setup(p => p.GetCountdownRemaining()).Returns(10.0f);
        var sut = new PullIntentService(probe.Object);
        CallUpdate(sut, T0);
        Assert.Equal(10.0f, sut.CountdownRemaining);  // confirm non-null first

        // Frame 2: countdown cancelled (player typed /countdown 0 or it expired)
        probe.Setup(p => p.GetCountdownRemaining()).Returns((float?)null);
        CallUpdate(sut, T0.AddSeconds(0.016));
        Assert.Null(sut.CountdownRemaining);
    }

    // -----------------------------------------------------------------------
    // Isolation: CountdownRemaining is driven ONLY by the probe, not by
    // isInCombat or other Update params (existing state machine is unaffected).
    // -----------------------------------------------------------------------

    [Fact]
    public void CountdownRemaining_is_independent_of_PullIntent_state()
    {
        var probe = new Mock<ICountdownProbe>();
        probe.Setup(p => p.GetCountdownRemaining()).Returns(3.0f);

        var sut = new PullIntentService(probe.Object);

        // Trigger Imminent via cast-on-hostile
        sut.Update(
            isPlayerCasting: true, isCastTargetHostile: true,
            queuedActionId: null, isQueuedActionHostile: false,
            isInCombat: false, utcNow: T0);

        Assert.Equal(PullIntent.Imminent, sut.Current);     // state machine ran
        Assert.Equal(3.0f, sut.CountdownRemaining);         // probe value also propagated
    }

    // -----------------------------------------------------------------------
    // Existing PullIntentService state-machine tests must still compile and
    // pass — verified by running the sibling filter below.
    // -----------------------------------------------------------------------
}
