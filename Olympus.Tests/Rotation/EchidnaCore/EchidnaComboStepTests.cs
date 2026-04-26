using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.EchidnaCore;

/// <summary>
/// Pure-switch tests for Echidna.ComputeComboStep. Viper has dual-blade
/// starters (Steel Fangs / Reaving Fangs) and matching second hits, plus
/// AoE variants (Steel Maw / Reaving Maw). Catches the regression class
/// where a future patch changes a step value, removes a case from the
/// switch, or breaks the comboTimer guard.
/// </summary>
public class EchidnaComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]        // No combo
    [InlineData(34606u, 0f, 0)]     // Steel Fangs - timer expired
    [InlineData(34606u, 30f, 1)]    // Steel Fangs → step 1
    [InlineData(34607u, 30f, 1)]    // Reaving Fangs → step 1
    [InlineData(34608u, 30f, 2)]    // Hunter's Sting → step 2
    [InlineData(34609u, 30f, 2)]    // Swiftskin's Sting → step 2
    [InlineData(34614u, 30f, 1)]    // Steel Maw (AoE) → step 1
    [InlineData(34615u, 30f, 1)]    // Reaving Maw (AoE) → step 1
    [InlineData(34616u, 30f, 2)]    // Hunter's Bite (AoE) → step 2
    [InlineData(34617u, 30f, 2)]    // Swiftskin's Bite (AoE) → step 2
    [InlineData(99999u, 30f, 0)]    // Unknown action
    public void ComputeComboStep_MapsActionAndTimer(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Echidna.ComputeComboStep(comboAction, comboTimer));
    }
}
