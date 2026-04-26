using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.KratosCore;

/// <summary>
/// Pure-function tests for Kratos.ComputeComboStep. Monk doesn't use a
/// traditional combo system - forms drive action availability via status
/// effects. The function always returns 0. A regression flipping this to
/// non-zero would feed bogus combo state to the rotation.
/// </summary>
public class KratosComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]
    [InlineData(0u, 0f, 0)]
    [InlineData(53u, 30f, 0)]      // Bootshine - still 0 (forms, not combo)
    [InlineData(56u, 30f, 0)]      // True Strike - still 0
    [InlineData(99999u, 30f, 0)]   // Unknown action - still 0
    public void ComputeComboStep_AlwaysReturnsZero(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Kratos.ComputeComboStep(comboAction, comboTimer));
    }
}
