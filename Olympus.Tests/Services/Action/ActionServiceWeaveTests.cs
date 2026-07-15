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
        var slots = ActionService.ComputeWeaveSlots(gcdRemaining, animLock, isCasting, used);
        Assert.Equal(expected, slots);
    }
}
