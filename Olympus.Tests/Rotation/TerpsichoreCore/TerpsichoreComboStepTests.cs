using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.TerpsichoreCore;

/// <summary>
/// Pure-switch tests for Terpsichore.ComputeComboStep. Dancer has two combo
/// chains: ST (Cascade → Fountain) and AoE (Windmill → Bladeshower).
/// </summary>
public class TerpsichoreComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]        // No combo
    [InlineData(15989u, 0f, 0)]     // Cascade - timer expired
    [InlineData(15989u, 30f, 1)]    // Cascade → step 1 (Fountain next)
    [InlineData(15993u, 30f, 1)]    // Windmill (AoE) → step 1 (Bladeshower next)
    [InlineData(99999u, 30f, 0)]    // Unknown action
    public void ComputeComboStep_MapsActionAndTimer(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Terpsichore.ComputeComboStep(comboAction, comboTimer));
    }
}
