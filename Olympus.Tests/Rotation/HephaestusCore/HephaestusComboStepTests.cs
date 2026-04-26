using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.HephaestusCore;

/// <summary>
/// Pure-switch tests for Hephaestus.ComputeComboStep. Distinct from
/// HephaestusGaugeTests (which covers ComputeStepsFromAmmoCombo, the
/// Gnashing Fang / Reign chain). This covers the basic Keen Edge combo
/// and the Demon Slice AoE combo.
/// </summary>
public class HephaestusComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]       // No combo
    [InlineData(16137u, 0f, 0)]    // Keen Edge - timer expired
    [InlineData(16137u, 30f, 1)]   // Keen Edge → step 1 (Brutal Shell next)
    [InlineData(16139u, 30f, 2)]   // Brutal Shell → step 2 (Solid Barrel next)
    [InlineData(16141u, 30f, 1)]   // Demon Slice (AoE) → step 1 (Demon Slaughter next)
    [InlineData(99999u, 30f, 0)]   // Unknown action
    public void ComputeComboStep_MapsActionAndTimer(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Hephaestus.ComputeComboStep(comboAction, comboTimer));
    }
}
