using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.NikeCore;

/// <summary>
/// Pure-switch tests for Nike.ComputeComboStep. Samurai's basic ST combos
/// (Hakaze/Gyofu → Jinpu/Shifu → Gekko/Kasha, or Hakaze/Gyofu → Yukikaze)
/// and the AoE combo (Fuga/Fuko → Mangetsu/Oka). Note: Hakaze (7477) was
/// renamed to Gyofu (36963) at Lv.92 but both are valid combo starters.
/// </summary>
public class NikeComboStepTests
{
    [Theory]
    [InlineData(0u, 30f, 0)]        // No combo
    [InlineData(7477u, 0f, 0)]      // Hakaze - timer expired
    [InlineData(7477u, 30f, 1)]     // Hakaze → step 1
    [InlineData(36963u, 30f, 1)]    // Gyofu (Lv.92 replacement) → step 1
    [InlineData(7478u, 30f, 2)]     // Jinpu → step 2 (Gekko next)
    [InlineData(7479u, 30f, 2)]     // Shifu → step 2 (Kasha next)
    [InlineData(7483u, 30f, 1)]     // Fuga (AoE) → step 1
    [InlineData(25780u, 30f, 1)]    // Fuko (AoE replacement) → step 1
    [InlineData(99999u, 30f, 0)]    // Unknown action
    public void ComputeComboStep_MapsActionAndTimer(uint comboAction, float comboTimer, int expected)
    {
        Assert.Equal(expected, Nike.ComputeComboStep(comboAction, comboTimer));
    }
}
