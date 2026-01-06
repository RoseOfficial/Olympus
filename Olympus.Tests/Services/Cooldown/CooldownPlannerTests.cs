using Moq;
using Olympus.Services.Cooldown;
using Olympus.Services.Prediction;
using Xunit;

namespace Olympus.Tests.Services.Cooldown;

/// <summary>
/// Unit tests for CooldownPlanner.
/// </summary>
public class CooldownPlannerTests
{
    private static CooldownPlanner CreatePlanner(
        Mock<IDamageIntakeService>? damageIntakeService = null,
        Mock<IDamageTrendService>? damageTrendService = null,
        Configuration? configuration = null)
    {
        // Create mocks with default setups only if not provided
        if (damageIntakeService == null)
        {
            damageIntakeService = new Mock<IDamageIntakeService>();
            damageIntakeService.Setup(d => d.GetPartyDamageRate(It.IsAny<float>())).Returns(0f);
        }

        if (damageTrendService == null)
        {
            damageTrendService = new Mock<IDamageTrendService>();
            damageTrendService.Setup(d => d.GetPartyDamageTrend(It.IsAny<float>())).Returns(DamageTrend.Stable);
            damageTrendService.Setup(d => d.IsDamageSpikeImminent(It.IsAny<float>())).Returns(false);
        }

        configuration ??= new Configuration();

        return new CooldownPlanner(damageIntakeService.Object, damageTrendService.Object, configuration);
    }

    #region ShouldUseMajorDefensive Tests

    [Fact]
    public void ShouldUseMajorDefensive_MultipleCritical_ReturnsTrue()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.50f, lowestHpPercent: 0.20f, injuredCount: 3, criticalCount: 2);

        // Act & Assert
        Assert.True(planner.ShouldUseMajorDefensive());
    }

    [Fact]
    public void ShouldUseMajorDefensive_VeryLowAverageHp_ReturnsTrue()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.35f, lowestHpPercent: 0.25f, injuredCount: 4, criticalCount: 1);

        // Act & Assert
        Assert.True(planner.ShouldUseMajorDefensive());
    }

    [Fact]
    public void ShouldUseMajorDefensive_SpikeImminent_ReturnsTrue()
    {
        // Arrange
        var damageTrendService = new Mock<IDamageTrendService>();
        damageTrendService.Setup(d => d.GetPartyDamageTrend(It.IsAny<float>())).Returns(DamageTrend.Stable);
        damageTrendService.Setup(d => d.IsDamageSpikeImminent(It.IsAny<float>())).Returns(true);

        var planner = CreatePlanner(damageTrendService: damageTrendService);
        planner.Update(avgPartyHpPercent: 0.80f, lowestHpPercent: 0.70f, injuredCount: 2, criticalCount: 0);

        // Act & Assert
        Assert.True(planner.ShouldUseMajorDefensive());
    }

    [Fact]
    public void ShouldUseMajorDefensive_HealthyParty_ReturnsFalse()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.95f, lowestHpPercent: 0.90f, injuredCount: 0, criticalCount: 0);

        // Act & Assert
        Assert.False(planner.ShouldUseMajorDefensive());
    }

    #endregion

    #region ShouldUseMinorDefensive Tests

    [Fact]
    public void ShouldUseMinorDefensive_IncreasingDamage_ReturnsTrue()
    {
        // Arrange
        var damageTrendService = new Mock<IDamageTrendService>();
        damageTrendService.Setup(d => d.GetPartyDamageTrend(It.IsAny<float>())).Returns(DamageTrend.Increasing);
        damageTrendService.Setup(d => d.IsDamageSpikeImminent(It.IsAny<float>())).Returns(false);

        var planner = CreatePlanner(damageTrendService: damageTrendService);
        planner.Update(avgPartyHpPercent: 0.80f, lowestHpPercent: 0.70f, injuredCount: 2, criticalCount: 0);

        // Act & Assert
        Assert.True(planner.ShouldUseMinorDefensive());
    }

    [Fact]
    public void ShouldUseMinorDefensive_HighDamageRate_ReturnsTrue()
    {
        // Arrange
        var config = new Configuration();
        config.Defensive.ProactiveBenisonDamageRate = 300f;

        var damageIntakeService = new Mock<IDamageIntakeService>();
        damageIntakeService.Setup(d => d.GetPartyDamageRate(It.IsAny<float>())).Returns(500f);

        var damageTrendService = new Mock<IDamageTrendService>();
        damageTrendService.Setup(d => d.GetPartyDamageTrend(It.IsAny<float>())).Returns(DamageTrend.Stable);
        damageTrendService.Setup(d => d.IsDamageSpikeImminent(It.IsAny<float>())).Returns(false);

        var planner = CreatePlanner(damageIntakeService: damageIntakeService, damageTrendService: damageTrendService, configuration: config);
        planner.Update(avgPartyHpPercent: 0.90f, lowestHpPercent: 0.85f, injuredCount: 1, criticalCount: 0);

        // Act & Assert
        Assert.True(planner.ShouldUseMinorDefensive());
    }

    [Fact]
    public void ShouldUseMinorDefensive_StableAndLowDamage_ReturnsFalse()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.95f, lowestHpPercent: 0.90f, injuredCount: 0, criticalCount: 0);

        // Act & Assert
        Assert.False(planner.ShouldUseMinorDefensive());
    }

    #endregion

    #region IsInEmergencyMode Tests

    [Fact]
    public void IsInEmergencyMode_MultipleCritical_ReturnsTrue()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.50f, lowestHpPercent: 0.25f, injuredCount: 3, criticalCount: 2);

        // Act & Assert
        Assert.True(planner.IsInEmergencyMode());
    }

    [Fact]
    public void IsInEmergencyMode_LowestBelowCritical_ReturnsTrue()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.70f, lowestHpPercent: 0.25f, injuredCount: 2, criticalCount: 1);

        // Act & Assert
        Assert.True(planner.IsInEmergencyMode());
    }

    [Fact]
    public void IsInEmergencyMode_LowAverageWithSpiking_ReturnsTrue()
    {
        // Arrange
        var damageTrendService = new Mock<IDamageTrendService>();
        damageTrendService.Setup(d => d.GetPartyDamageTrend(It.IsAny<float>())).Returns(DamageTrend.Spiking);
        damageTrendService.Setup(d => d.IsDamageSpikeImminent(It.IsAny<float>())).Returns(false);

        var planner = CreatePlanner(damageTrendService: damageTrendService);
        planner.Update(avgPartyHpPercent: 0.35f, lowestHpPercent: 0.35f, injuredCount: 4, criticalCount: 0);

        // Act & Assert
        Assert.True(planner.IsInEmergencyMode());
    }

    [Fact]
    public void IsInEmergencyMode_HealthyParty_ReturnsFalse()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.95f, lowestHpPercent: 0.90f, injuredCount: 0, criticalCount: 0);

        // Act & Assert
        Assert.False(planner.IsInEmergencyMode());
    }

    #endregion

    #region GetCooldownPriority Tests

    [Fact]
    public void GetCooldownPriority_Temperance_Emergency_ReturnsEmergency()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.30f, lowestHpPercent: 0.20f, injuredCount: 4, criticalCount: 2);

        // Act
        var priority = planner.GetCooldownPriority("temperance");

        // Assert
        Assert.Equal(CooldownPriority.Emergency, priority);
    }

    [Fact]
    public void GetCooldownPriority_DivineBenison_HighDamage_ReturnsHigh()
    {
        // Arrange
        var config = new Configuration();
        config.Defensive.ProactiveBenisonDamageRate = 300f;

        var damageIntakeService = new Mock<IDamageIntakeService>();
        damageIntakeService.Setup(d => d.GetPartyDamageRate(It.IsAny<float>())).Returns(500f);

        var damageTrendService = new Mock<IDamageTrendService>();
        damageTrendService.Setup(d => d.GetPartyDamageTrend(It.IsAny<float>())).Returns(DamageTrend.Stable);
        damageTrendService.Setup(d => d.IsDamageSpikeImminent(It.IsAny<float>())).Returns(false);

        var planner = CreatePlanner(damageIntakeService: damageIntakeService, damageTrendService: damageTrendService, configuration: config);
        planner.Update(avgPartyHpPercent: 0.80f, lowestHpPercent: 0.70f, injuredCount: 2, criticalCount: 0);

        // Act
        var priority = planner.GetCooldownPriority("divinebenison");

        // Assert
        Assert.Equal(CooldownPriority.High, priority);
    }

    [Fact]
    public void GetCooldownPriority_UnknownCooldown_ReturnsMedium()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.80f, lowestHpPercent: 0.70f, injuredCount: 2, criticalCount: 0);

        // Act
        var priority = planner.GetCooldownPriority("unknowncooldown");

        // Assert
        Assert.Equal(CooldownPriority.Medium, priority);
    }

    #endregion

    #region IsDamageSpikeExpected Tests

    [Fact]
    public void IsDamageSpikeExpected_SpikeImminent_ReturnsTrue()
    {
        // Arrange
        var damageTrendService = new Mock<IDamageTrendService>();
        damageTrendService.Setup(d => d.GetPartyDamageTrend(It.IsAny<float>())).Returns(DamageTrend.Stable);
        damageTrendService.Setup(d => d.IsDamageSpikeImminent(It.IsAny<float>())).Returns(true);

        var planner = CreatePlanner(damageTrendService: damageTrendService);
        planner.Update(avgPartyHpPercent: 0.90f, lowestHpPercent: 0.85f, injuredCount: 1, criticalCount: 0);

        // Act & Assert
        Assert.True(planner.IsDamageSpikeExpected());
    }

    [Fact]
    public void IsDamageSpikeExpected_SpikingTrend_ReturnsTrue()
    {
        // Arrange
        var damageTrendService = new Mock<IDamageTrendService>();
        damageTrendService.Setup(d => d.GetPartyDamageTrend(It.IsAny<float>())).Returns(DamageTrend.Spiking);
        damageTrendService.Setup(d => d.IsDamageSpikeImminent(It.IsAny<float>())).Returns(false);

        var planner = CreatePlanner(damageTrendService: damageTrendService);
        planner.Update(avgPartyHpPercent: 0.80f, lowestHpPercent: 0.70f, injuredCount: 2, criticalCount: 0);

        // Act & Assert
        Assert.True(planner.IsDamageSpikeExpected());
    }

    [Fact]
    public void IsDamageSpikeExpected_StableAndNoSpike_ReturnsFalse()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.95f, lowestHpPercent: 0.90f, injuredCount: 0, criticalCount: 0);

        // Act & Assert
        Assert.False(planner.IsDamageSpikeExpected());
    }

    #endregion

    #region GetHealingUrgency Tests

    [Fact]
    public void GetHealingUrgency_HealthyParty_ReturnsLowUrgency()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.95f, lowestHpPercent: 0.90f, injuredCount: 0, criticalCount: 0);

        // Act
        var urgency = planner.GetHealingUrgency();

        // Assert
        Assert.True(urgency < 0.2f);
    }

    [Fact]
    public void GetHealingUrgency_LowHpParty_ReturnsHighUrgency()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.30f, lowestHpPercent: 0.20f, injuredCount: 4, criticalCount: 2);

        // Act
        var urgency = planner.GetHealingUrgency();

        // Assert
        Assert.True(urgency > 0.5f);
    }

    [Fact]
    public void GetHealingUrgency_SpikingDamage_IncreasesUrgency()
    {
        // Arrange
        var damageTrendService = new Mock<IDamageTrendService>();
        damageTrendService.Setup(d => d.GetPartyDamageTrend(It.IsAny<float>())).Returns(DamageTrend.Spiking);
        damageTrendService.Setup(d => d.IsDamageSpikeImminent(It.IsAny<float>())).Returns(false);

        var planner = CreatePlanner(damageTrendService: damageTrendService);
        planner.Update(avgPartyHpPercent: 0.60f, lowestHpPercent: 0.50f, injuredCount: 3, criticalCount: 0);

        // Act
        var urgency = planner.GetHealingUrgency();

        // Assert - Spiking trend applies 1.5x multiplier, so urgency should be higher
        Assert.True(urgency > 0.5f);
    }

    [Fact]
    public void GetHealingUrgency_NeverExceedsOne()
    {
        // Arrange
        var damageIntakeService = new Mock<IDamageIntakeService>();
        damageIntakeService.Setup(d => d.GetPartyDamageRate(It.IsAny<float>())).Returns(10000f); // Very high

        var damageTrendService = new Mock<IDamageTrendService>();
        damageTrendService.Setup(d => d.GetPartyDamageTrend(It.IsAny<float>())).Returns(DamageTrend.Spiking);
        damageTrendService.Setup(d => d.IsDamageSpikeImminent(It.IsAny<float>())).Returns(true);

        var planner = CreatePlanner(damageIntakeService: damageIntakeService, damageTrendService: damageTrendService);
        planner.Update(avgPartyHpPercent: 0.10f, lowestHpPercent: 0.05f, injuredCount: 8, criticalCount: 4);

        // Act
        var urgency = planner.GetHealingUrgency();

        // Assert
        Assert.True(urgency <= 1.0f);
    }

    #endregion

    #region ShouldConserveResources Tests

    [Fact]
    public void ShouldConserveResources_SpikeExpectedButHealthy_ReturnsTrue()
    {
        // Arrange
        var damageTrendService = new Mock<IDamageTrendService>();
        damageTrendService.Setup(d => d.GetPartyDamageTrend(It.IsAny<float>())).Returns(DamageTrend.Stable);
        damageTrendService.Setup(d => d.IsDamageSpikeImminent(It.IsAny<float>())).Returns(true);

        var planner = CreatePlanner(damageTrendService: damageTrendService);
        planner.Update(avgPartyHpPercent: 0.85f, lowestHpPercent: 0.80f, injuredCount: 1, criticalCount: 0);

        // Act & Assert
        Assert.True(planner.ShouldConserveResources());
    }

    [Fact]
    public void ShouldConserveResources_NoSpikeExpected_ReturnsFalse()
    {
        // Arrange
        var planner = CreatePlanner();
        planner.Update(avgPartyHpPercent: 0.90f, lowestHpPercent: 0.85f, injuredCount: 1, criticalCount: 0);

        // Act & Assert
        Assert.False(planner.ShouldConserveResources());
    }

    [Fact]
    public void ShouldConserveResources_SpikeExpectedButLowHp_ReturnsFalse()
    {
        // Arrange - Spike expected but party HP already low, so don't conserve
        var damageTrendService = new Mock<IDamageTrendService>();
        damageTrendService.Setup(d => d.GetPartyDamageTrend(It.IsAny<float>())).Returns(DamageTrend.Stable);
        damageTrendService.Setup(d => d.IsDamageSpikeImminent(It.IsAny<float>())).Returns(true);

        var planner = CreatePlanner(damageTrendService: damageTrendService);
        planner.Update(avgPartyHpPercent: 0.50f, lowestHpPercent: 0.40f, injuredCount: 3, criticalCount: 1);

        // Act & Assert
        Assert.False(planner.ShouldConserveResources());
    }

    #endregion
}
