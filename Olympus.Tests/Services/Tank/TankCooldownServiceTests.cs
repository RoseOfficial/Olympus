using Olympus.Config;
using Olympus.Services.Tank;
using Xunit;

namespace Olympus.Tests.Services.Tank;

/// <summary>
/// Unit tests for TankCooldownService.
/// TankConfig is a plain POCO — no mocks required. The three decision methods
/// accept their own parameters directly, so Update() is not required before
/// calling them.
/// </summary>
public class TankCooldownServiceTests
{
    // -------------------------------------------------------------------------
    // Helper
    // -------------------------------------------------------------------------

    private static TankCooldownService CreateService(
        bool enableMitigation = true,
        float mitigationThreshold = 0.70f,
        bool useRampartOnCooldown = false,
        int sheltronMinGauge = 50)
    {
        var config = new TankConfig
        {
            EnableMitigation     = enableMitigation,
            MitigationThreshold  = mitigationThreshold,
            UseRampartOnCooldown = useRampartOnCooldown,
            SheltronMinGauge     = sheltronMinGauge,
        };
        return new TankCooldownService(config);
    }

    // =========================================================================
    // ShouldUseMitigation
    // =========================================================================

    [Fact]
    public void ShouldUseMitigation_EnableMitigationFalse_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(enableMitigation: false);

        // Act & Assert — disabled config short-circuits everything
        Assert.False(service.ShouldUseMitigation(0.50f, 600f, hasActiveMitigation: false));
    }

    [Fact]
    public void ShouldUseMitigation_NoActiveMit_BelowThreshold_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act — hpPercent=0.60 < threshold=0.70, no active mitigation
        Assert.True(service.ShouldUseMitigation(0.60f, 0f, hasActiveMitigation: false));
    }

    [Fact]
    public void ShouldUseMitigation_NoActiveMit_AboveThreshold_LowDamage_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act — hpPercent=0.80 > threshold=0.70, damageRate=100 <= 500 → false
        Assert.False(service.ShouldUseMitigation(0.80f, 100f, hasActiveMitigation: false));
    }

    [Fact]
    public void ShouldUseMitigation_NoActiveMit_AboveThreshold_HighDamageAndBelowCap_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act — hpPercent=0.80 < 0.85 AND damageRate=600 > 500 → true
        Assert.True(service.ShouldUseMitigation(0.80f, 600f, hasActiveMitigation: false));
    }

    [Fact]
    public void ShouldUseMitigation_ActiveMit_AboveThirtyPercent_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act — hasActiveMitigation=true AND hpPercent=0.80 > 0.30 → no-stack guard fires → false
        Assert.False(service.ShouldUseMitigation(0.80f, 0f, hasActiveMitigation: true));
    }

    [Fact]
    public void ShouldUseMitigation_ActiveMit_AtOrBelowThirtyPercent_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act — hpPercent=0.30 is NOT > 0.30, so no-stack guard does not fire;
        //       then hpPercent < threshold (0.70) → true
        Assert.True(service.ShouldUseMitigation(0.30f, 0f, hasActiveMitigation: true));
    }

    [Fact]
    public void ShouldUseMitigation_ActiveMit_ExactlyAboveThirtyPercent_ReturnsFalse()
    {
        // Arrange
        var service = CreateService();

        // Act — hpPercent=0.31 > 0.30 → no-stack guard fires → false
        Assert.False(service.ShouldUseMitigation(0.31f, 0f, hasActiveMitigation: true));
    }

    // =========================================================================
    // ShouldUseMajorCooldown
    // =========================================================================

    [Fact]
    public void ShouldUseMajorCooldown_EnableMitigationFalse_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(enableMitigation: false);

        // Act & Assert
        Assert.False(service.ShouldUseMajorCooldown(0.30f, 1500f));
    }

    [Fact]
    public void ShouldUseMajorCooldown_HighDamageRate_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act — damageRate=1001 > 1000 → true
        Assert.True(service.ShouldUseMajorCooldown(0.90f, 1001f));
    }

    [Fact]
    public void ShouldUseMajorCooldown_CriticallyLowHp_ReturnsTrue()
    {
        // Arrange
        var service = CreateService();

        // Act — hpPercent=0.35 < 0.40 → true
        Assert.True(service.ShouldUseMajorCooldown(0.35f, 0f));
    }

    [Fact]
    public void ShouldUseMajorCooldown_UseRampartOnCooldown_ModerateDamage_ReturnsTrue()
    {
        // Arrange
        var service = CreateService(useRampartOnCooldown: true);

        // Act — UseRampartOnCooldown=true AND damageRate=300 > 200 → true
        Assert.True(service.ShouldUseMajorCooldown(0.90f, 300f));
    }

    [Fact]
    public void ShouldUseMajorCooldown_DefaultConfig_ModerateDamageHighHp_ReturnsFalse()
    {
        // Arrange — default config: UseRampartOnCooldown=false
        var service = CreateService();

        // Act — damageRate=300 not > 1000, hpPercent=0.80 not < 0.40, UseRampartOnCooldown=false → false
        Assert.False(service.ShouldUseMajorCooldown(0.80f, 300f));
    }

    // =========================================================================
    // ShouldUseShortCooldown
    // =========================================================================

    [Fact]
    public void ShouldUseShortCooldown_EnableMitigationFalse_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(enableMitigation: false);

        // Act & Assert
        Assert.False(service.ShouldUseShortCooldown(0.50f, gaugeValue: 80, minGauge: 50));
    }

    [Fact]
    public void ShouldUseShortCooldown_GaugeBelowMin_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(sheltronMinGauge: 50);

        // Act — gaugeValue=40 < minGauge=50 → gauge guard fires → false
        Assert.False(service.ShouldUseShortCooldown(0.50f, gaugeValue: 40, minGauge: 50));
    }

    [Fact]
    public void ShouldUseShortCooldown_GaugeAtMin_HpBelowThreshold_ReturnsTrue()
    {
        // Arrange — sheltronMinGauge=50, minGauge param=50, hpPercent=0.60 < threshold=0.70
        var service = CreateService(sheltronMinGauge: 50);

        // Act — gaugeValue=50 >= minGauge=50; hpPercent=0.60 < 0.70; gaugeValue >= SheltronMinGauge → true
        Assert.True(service.ShouldUseShortCooldown(0.60f, gaugeValue: 50, minGauge: 50));
    }

    [Fact]
    public void ShouldUseShortCooldown_MaxGauge_HpBelowNinetyPercent_ReturnsTrue()
    {
        // Arrange
        var service = CreateService(sheltronMinGauge: 50);

        // Act — gaugeValue=100 >= minGauge=50; gaugeValue >= 100 AND hpPercent=0.80 < 0.90 → true
        Assert.True(service.ShouldUseShortCooldown(0.80f, gaugeValue: 100, minGauge: 50));
    }

    [Fact]
    public void ShouldUseShortCooldown_MaxGauge_HpAboveNinetyPercent_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(sheltronMinGauge: 50);

        // Act — gaugeValue=100 >= minGauge=50; hpPercent=0.95 >= 0.90 → second branch false;
        //       hpPercent=0.95 > threshold=0.70 → first branch false → false
        Assert.False(service.ShouldUseShortCooldown(0.95f, gaugeValue: 100, minGauge: 50));
    }

    [Fact]
    public void ShouldUseShortCooldown_HpAboveThreshold_GaugeBelowMax_ReturnsFalse()
    {
        // Arrange
        var service = CreateService(sheltronMinGauge: 50);

        // Act — hpPercent=0.75 > threshold=0.70 → first branch false;
        //       gaugeValue=60 < 100 → second branch false → false
        Assert.False(service.ShouldUseShortCooldown(0.75f, gaugeValue: 60, minGauge: 50));
    }
}
