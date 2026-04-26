using Olympus.Rotation;
using Xunit;

namespace Olympus.Tests.Rotation.HephaestusCore;

/// <summary>
/// Unit tests for Hephaestus.ComputeStepsFromAmmoCombo. Exercises the pure
/// translation from GNBGauge.AmmoComboStep byte to (gnashingFang, reign)
/// step pair. Catches the regression class where a future patch flips the
/// byte semantics or someone removes a case from the switch.
/// </summary>
public class HephaestusGaugeTests
{
    [Theory]
    [InlineData((byte)0, 0, 0)]   // No combo
    [InlineData((byte)1, 1, 0)]   // Savage Claw next (Gnashing Fang step 1)
    [InlineData((byte)2, 2, 0)]   // Wicked Talon next (Gnashing Fang step 2)
    [InlineData((byte)3, 0, 1)]   // Noble Blood next (Reign step 1)
    [InlineData((byte)4, 0, 2)]   // Lion Heart next (Reign step 2)
    [InlineData((byte)5, 0, 0)]   // Unknown step (5)
    [InlineData((byte)255, 0, 0)] // Unknown step (255 / out of range)
    public void ComputeStepsFromAmmoCombo_MapsByteToStepPair(byte ammoComboStep, int expectedGnashingFang, int expectedReign)
    {
        var (gnashingFang, reign) = Hephaestus.ComputeStepsFromAmmoCombo(ammoComboStep);

        Assert.Equal(expectedGnashingFang, gnashingFang);
        Assert.Equal(expectedReign, reign);
    }
}
