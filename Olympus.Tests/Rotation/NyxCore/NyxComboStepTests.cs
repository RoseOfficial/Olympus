using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.NyxCore;

/// <summary>
/// Pure-switch tests for Nyx.ComputeComboStep. Catches the regression class
/// where a future patch changes a step value, removes a case from the switch,
/// or breaks the comboTimer guard.
/// </summary>
public class NyxComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]       // No combo
    [InlineData(3617u, 0f, 0)]     // Hard Slash - timer expired
    [InlineData(3617u, 30f, 1)]    // Hard Slash → step 1
    [InlineData(3623u, 30f, 2)]    // Syphon Strike → step 2
    [InlineData(3621u, 30f, 1)]    // Unleash (AoE) → step 1
    [InlineData(99999u, 30f, 0)]   // Unknown action
    public void ComputeComboStep_MapsActionAndTimer(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Nyx.ComputeComboStep(comboAction, comboTimer));
    }
}
