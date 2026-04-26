using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.ThemisCore;

/// <summary>
/// Pure-switch tests for Themis.ComputeComboStep. Catches the regression class
/// where a future patch changes a step value, removes a case from the switch,
/// or breaks the comboTimer guard. Note: PLD's combo step values are "next
/// expected step" indices (FastBlade returns 2 because RiotBlade is step 2).
/// </summary>
public class ThemisComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]       // No combo
    [InlineData(9u, 0f, 0)]        // Fast Blade - timer expired
    [InlineData(9u, 30f, 2)]       // Fast Blade → step 2 (RiotBlade next)
    [InlineData(15u, 30f, 3)]      // Riot Blade → step 3 (RoyalAuthority next)
    [InlineData(7381u, 30f, 2)]    // Total Eclipse (AoE) → step 2 (Prominence next)
    [InlineData(99999u, 30f, 0)]   // Unknown action
    public void ComputeComboStep_MapsActionAndTimer(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Themis.ComputeComboStep(comboAction, comboTimer));
    }
}
