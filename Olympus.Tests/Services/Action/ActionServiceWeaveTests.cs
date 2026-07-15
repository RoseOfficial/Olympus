using Olympus.Services.Action;
using Xunit;

namespace Olympus.Tests.Services.Action;

/// <summary>
/// Tests for the pure weave-slot computation. Thresholds with AnimationLockBase = 0.6f
/// and ClipPreventionBuffer = 0.1f: one weave fits when GcdRemaining > 0.7s, two when > 1.3s.
/// The queue-window tail (last 0.5s) is reserved for the next GCD's early submit.
/// </summary>
public class ActionServiceWeaveTests
{
    [Theory]
    // Post-hardcast single weave: 1.5s cast on 2.5s GCD leaves ~0.9s. Must yield 1 slot.
    [InlineData(0.9f, 0f, false, 0, 1)]
    // Post-instant double weave: ~1.8s remaining after animation lock clears. Must yield 2.
    [InlineData(1.8f, 0f, false, 0, 2)]
    // Second weave mid-cycle: first oGCD done (used=1), ~1.15s left. Must yield 1.
    [InlineData(1.15f, 0f, false, 1, 1)]
    // Queue window reserved: at 0.5s the tail belongs to the next GCD.
    [InlineData(0.5f, 0f, false, 0, 0)]
    // GCD idle (not rolling): no weave slots; the GCD pass owns this moment.
    [InlineData(0f, 0f, false, 0, 0)]
    // Double-weave cap: two already used this cycle.
    [InlineData(2.4f, 0f, false, 2, 0)]
    // Animation lock still running.
    [InlineData(1.8f, 0.5f, false, 0, 0)]
    // Casting.
    [InlineData(1.8f, 0f, true, 0, 0)]
    // Boundary: just above the one-slot threshold (0.71 - 0.1) / 0.6 = 1.016 -> 1.
    [InlineData(0.71f, 0f, false, 0, 1)]
    // Boundary: just below (0.69 - 0.1) / 0.6 = 0.983 -> 0.
    [InlineData(0.69f, 0f, false, 0, 0)]
    // Boundary: just above two-slot threshold (1.31 - 0.1) / 0.6 = 2.016 -> 2.
    [InlineData(1.31f, 0f, false, 0, 2)]
    public void ComputeWeaveSlots_ReturnsExpectedSlots(
        float gcdRemaining, float animLock, bool isCasting, int used, int expected)
    {
        var slots = ActionService.ComputeWeaveSlots(gcdRemaining, animLock, isCasting, used, 0.6f);
        Assert.Equal(expected, slots);
    }

    [Theory]
    // 80ms ping: post-instant window 1.8s, cost 0.68 -> (1.7 / 0.68) = 2 slots still.
    [InlineData(1.8f, 0f, false, 0, 0.68f, 2)]
    // 200ms ping: cost 0.8 -> (1.7 / 0.8) = 2.125 -> 2; at 1.5s remaining (1.4 / 0.8) = 1.
    [InlineData(1.5f, 0f, false, 0, 0.8f, 1)]
    // High ping tightens the single-weave floor: 0.85s remaining, cost 0.8 -> (0.75/0.8) -> 0.
    [InlineData(0.85f, 0f, false, 0, 0.8f, 0)]
    public void ComputeWeaveSlots_WithPerWeaveCost_ReturnsExpectedSlots(
        float gcdRemaining, float animLock, bool isCasting, int used, float cost, int expected)
    {
        var slots = ActionService.ComputeWeaveSlots(gcdRemaining, animLock, isCasting, used, cost);
        Assert.Equal(expected, slots);
    }

    [Fact]
    public void SmoothDelaySample_ClampsAndSmooths()
    {
        // Starts at 0; one 100ms sample moves the estimate 20% of the way.
        var s1 = ActionService.SmoothDelaySample(0f, 0.100f);
        Assert.Equal(0.020f, s1, 3);
        // Samples above 300ms clamp to 300ms.
        var s2 = ActionService.SmoothDelaySample(0f, 5f);
        Assert.Equal(0.060f, s2, 3);
        // Negative samples clamp to 0 (clock skew safety).
        var s3 = ActionService.SmoothDelaySample(0.1f, -1f);
        Assert.Equal(0.080f, s3, 3);
    }
}
