using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.ZeusCore;

/// <summary>
/// Pure-switch tests for Zeus.ComputeComboStep. Dragoon's combo system has
/// a 3-step single-target chain (True Thrust → Vorpal/Disembowel → Full/Chaos)
/// with action-replacement at Lv.86 (Full Thrust→Heavens' Thrust, Chaos
/// Thrust→Chaotic Spring), plus a 3-step AoE chain (Doom Spike → Sonic
/// Thrust → Coerthan Torment).
/// </summary>
public class ZeusComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]        // No combo
    [InlineData(75u, 0f, 0)]        // True Thrust - timer expired
    [InlineData(75u, 30f, 1)]       // True Thrust → step 1
    [InlineData(78u, 30f, 2)]       // Vorpal Thrust → step 2
    [InlineData(87u, 30f, 2)]       // Disembowel → step 2
    [InlineData(84u, 30f, 3)]       // Full Thrust → step 3
    [InlineData(25771u, 30f, 3)]    // Heavens' Thrust (Lv.86 replacement) → step 3
    [InlineData(88u, 30f, 3)]       // Chaos Thrust → step 3
    [InlineData(25772u, 30f, 3)]    // Chaotic Spring (Lv.86 replacement) → step 3
    [InlineData(86u, 30f, 1)]       // Doom Spike (AoE) → step 1
    [InlineData(7397u, 30f, 2)]     // Sonic Thrust → step 2
    [InlineData(16477u, 30f, 3)]    // Coerthan Torment → step 3
    [InlineData(99999u, 30f, 0)]    // Unknown action
    public void ComputeComboStep_MapsActionAndTimer(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Zeus.ComputeComboStep(comboAction, comboTimer));
    }
}
