using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.HermesCore;

/// <summary>
/// Pure-switch tests for Hermes.ComputeComboStep. Covers the basic ST combo
/// (Spinning Edge → Gust Slash → Aeolian Edge/Armor Crush) and the AoE combo
/// (Death Blossom → Hakke Mujinsatsu).
/// </summary>
public class HermesComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]       // No combo
    [InlineData(2240u, 0f, 0)]     // Spinning Edge - timer expired
    [InlineData(2240u, 30f, 1)]    // Spinning Edge → step 1 (Gust Slash next)
    [InlineData(2242u, 30f, 2)]    // Gust Slash → step 2 (Aeolian Edge / Armor Crush next)
    [InlineData(2254u, 30f, 1)]    // Death Blossom (AoE) → step 1 (Hakke Mujinsatsu next)
    [InlineData(99999u, 30f, 0)]   // Unknown action
    public void ComputeComboStep_MapsActionAndTimer(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Hermes.ComputeComboStep(comboAction, comboTimer));
    }
}
