using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.ThanatosCore;

/// <summary>
/// Pure-switch tests for Thanatos.ComputeComboStep. Reaper's basic combos:
/// ST (Slice → Waxing Slice → Infernal Slice) and AoE (Spinning Scythe →
/// Nightmare Scythe).
/// </summary>
public class ThanatosComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]        // No combo
    [InlineData(24373u, 0f, 0)]     // Slice - timer expired
    [InlineData(24373u, 30f, 1)]    // Slice → step 1
    [InlineData(24374u, 30f, 2)]    // Waxing Slice → step 2 (Infernal Slice next)
    [InlineData(24376u, 30f, 1)]    // Spinning Scythe (AoE) → step 1 (Nightmare Scythe next)
    [InlineData(99999u, 30f, 0)]    // Unknown action
    public void ComputeComboStep_MapsActionAndTimer(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Thanatos.ComputeComboStep(comboAction, comboTimer));
    }
}
