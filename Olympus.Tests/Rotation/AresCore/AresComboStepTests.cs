using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.AresCore;

/// <summary>
/// Pure-switch tests for Ares.ComputeComboStep. Catches the regression class
/// where a future patch changes a step value, removes a case from the switch,
/// or breaks the comboTimer guard.
/// </summary>
public class AresComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]       // No combo
    [InlineData(31u, 0f, 0)]       // Heavy Swing - timer expired
    [InlineData(31u, 30f, 1)]      // Heavy Swing → step 1
    [InlineData(37u, 30f, 2)]      // Maim → step 2
    [InlineData(41u, 30f, 1)]      // Overpower (AoE) → step 1
    [InlineData(99999u, 30f, 0)]   // Unknown action
    public void ComputeComboStep_MapsActionAndTimer(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Ares.ComputeComboStep(comboAction, comboTimer));
    }
}
