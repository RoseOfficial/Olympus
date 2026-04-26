using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.CalliopeCore;

/// <summary>
/// Pure-function tests for Calliope.ComputeComboStep. Bard has no strict
/// combo system - all GCDs are standalone. The function always returns 0.
/// A regression flipping this to non-zero would feed bogus combo state to
/// the rotation.
/// </summary>
public class CalliopeComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]
    [InlineData(0u, 0f, 0)]
    [InlineData(97u, 30f, 0)]      // Heavy Shot - still 0
    [InlineData(99999u, 30f, 0)]   // Unknown action - still 0
    public void ComputeComboStep_AlwaysReturnsZero(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Calliope.ComputeComboStep(comboAction, comboTimer));
    }
}
