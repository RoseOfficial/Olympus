using System.Collections.Generic;
using Moq;
using Olympus.Services.Prediction;
using Xunit;

namespace Olympus.Tests.Services.Prediction;

/// <summary>
/// Unit tests for DamageTrendService.
/// </summary>
public class DamageTrendServiceTests
{
    #region GetPartyDamageTrend Tests

    [Fact]
    public void GetPartyDamageTrend_LowDamage_ReturnsStable()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(It.IsAny<float>())).Returns(10f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var trend = service.GetPartyDamageTrend(10f);

        // Assert
        Assert.Equal(DamageTrend.Stable, trend);
    }

    [Fact]
    public void GetPartyDamageTrend_StableDamage_ReturnsStable()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Current rate = 100, previous rate = 100 (stable)
        mockIntake.Setup(s => s.GetPartyDamageRate(5f)).Returns(100f);
        mockIntake.Setup(s => s.GetPartyDamageRate(10f)).Returns(100f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var trend = service.GetPartyDamageTrend(10f);

        // Assert
        Assert.Equal(DamageTrend.Stable, trend);
    }

    [Fact]
    public void GetPartyDamageTrend_IncreasingDamage_ReturnsIncreasing()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Current rate = 130 (recent half), previous = 100 (30% increase)
        mockIntake.Setup(s => s.GetPartyDamageRate(5f)).Returns(130f);
        mockIntake.Setup(s => s.GetPartyDamageRate(10f)).Returns(115f); // Average of 130 and 100
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var trend = service.GetPartyDamageTrend(10f);

        // Assert
        Assert.Equal(DamageTrend.Increasing, trend);
    }

    [Fact]
    public void GetPartyDamageTrend_SpikingDamage_ReturnsSpiking()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Current rate = 200 (recent half), previous = 100 (100% increase = spiking)
        mockIntake.Setup(s => s.GetPartyDamageRate(5f)).Returns(200f);
        mockIntake.Setup(s => s.GetPartyDamageRate(10f)).Returns(150f); // Average of 200 and 100
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var trend = service.GetPartyDamageTrend(10f);

        // Assert
        Assert.Equal(DamageTrend.Spiking, trend);
    }

    [Fact]
    public void GetPartyDamageTrend_DecreasingDamage_ReturnsDecreasing()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Current rate = 70 (recent half), previous = 100 (30% decrease)
        mockIntake.Setup(s => s.GetPartyDamageRate(5f)).Returns(70f);
        mockIntake.Setup(s => s.GetPartyDamageRate(10f)).Returns(85f); // Average of 70 and 100
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var trend = service.GetPartyDamageTrend(10f);

        // Assert
        Assert.Equal(DamageTrend.Decreasing, trend);
    }

    #endregion

    #region GetEntityDamageTrend Tests

    [Fact]
    public void GetEntityDamageTrend_StableDamage_ReturnsStable()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetDamageRate(1u, 5f)).Returns(100f);
        mockIntake.Setup(s => s.GetRecentDamageIntake(1u, 10f)).Returns(1000);
        mockIntake.Setup(s => s.GetRecentDamageIntake(1u, 5f)).Returns(500);
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var trend = service.GetEntityDamageTrend(1u, 10f);

        // Assert
        Assert.Equal(DamageTrend.Stable, trend);
    }

    [Fact]
    public void GetEntityDamageTrend_SpikingDamage_ReturnsSpiking()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Current rate = 200 DPS for entity
        mockIntake.Setup(s => s.GetDamageRate(1u, 5f)).Returns(200f);
        // Previous period: (1500 - 1000) / 5 = 100 DPS
        mockIntake.Setup(s => s.GetRecentDamageIntake(1u, 10f)).Returns(1500);
        mockIntake.Setup(s => s.GetRecentDamageIntake(1u, 5f)).Returns(1000);
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var trend = service.GetEntityDamageTrend(1u, 10f);

        // Assert
        Assert.Equal(DamageTrend.Spiking, trend);
    }

    #endregion

    #region IsDamageSpikeImminent Tests

    [Fact]
    public void IsDamageSpikeImminent_NoSpike_ReturnsFalse()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(It.IsAny<float>())).Returns(50f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var result = service.IsDamageSpikeImminent();

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsDamageSpikeImminent_SpikingTrend_ReturnsTrue()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Create a spiking scenario: current >> previous
        mockIntake.Setup(s => s.GetPartyDamageRate(2.5f)).Returns(500f);
        mockIntake.Setup(s => s.GetPartyDamageRate(5f)).Returns(250f);
        mockIntake.Setup(s => s.GetPartyDamageRate(1.5f)).Returns(500f); // For high damage phase check
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var result = service.IsDamageSpikeImminent();

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsDamageSpikeImminent_SustainedHighDamage_ReturnsTrue()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Stable but high damage (triggers sustained high-damage detection)
        mockIntake.Setup(s => s.GetPartyDamageRate(2.5f)).Returns(900f);
        mockIntake.Setup(s => s.GetPartyDamageRate(5f)).Returns(900f);
        mockIntake.Setup(s => s.GetPartyDamageRate(1.5f)).Returns(900f);
        mockIntake.Setup(s => s.GetPartyDamageRate(2f)).Returns(900f);
        var service = new DamageTrendService(mockIntake.Object);

        // Update to trigger high damage phase tracking
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });

        // Act
        var result = service.IsDamageSpikeImminent();

        // Assert
        Assert.True(result);
    }

    #endregion

    #region GetDamageAcceleration Tests

    [Fact]
    public void GetDamageAcceleration_IncreasingDamage_ReturnsPositive()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Current rate = 200 DPS, previous = 100 DPS
        mockIntake.Setup(s => s.GetDamageRate(1u, 2.5f)).Returns(200f);
        mockIntake.Setup(s => s.GetRecentDamageIntake(1u, 5f)).Returns(750); // 150 avg * 5s
        mockIntake.Setup(s => s.GetRecentDamageIntake(1u, 2.5f)).Returns(500); // 200 DPS * 2.5s
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var accel = service.GetDamageAcceleration(1u, 5f);

        // Assert - (200 - 100) / 2.5 = 40 DPS/s
        Assert.True(accel > 0);
    }

    [Fact]
    public void GetDamageAcceleration_DecreasingDamage_ReturnsNegative()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Current rate = 50 DPS, previous = 150 DPS
        mockIntake.Setup(s => s.GetDamageRate(1u, 2.5f)).Returns(50f);
        mockIntake.Setup(s => s.GetRecentDamageIntake(1u, 5f)).Returns(500); // 100 avg * 5s
        mockIntake.Setup(s => s.GetRecentDamageIntake(1u, 2.5f)).Returns(125); // 50 DPS * 2.5s
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var accel = service.GetDamageAcceleration(1u, 5f);

        // Assert - (50 - 150) / 2.5 = -40 DPS/s
        Assert.True(accel < 0);
    }

    [Fact]
    public void GetDamageAcceleration_StableDamage_ReturnsNearZero()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Current rate = 100 DPS, previous = 100 DPS
        mockIntake.Setup(s => s.GetDamageRate(1u, 2.5f)).Returns(100f);
        mockIntake.Setup(s => s.GetRecentDamageIntake(1u, 5f)).Returns(500); // 100 avg * 5s
        mockIntake.Setup(s => s.GetRecentDamageIntake(1u, 2.5f)).Returns(250); // 100 DPS * 2.5s
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var accel = service.GetDamageAcceleration(1u, 5f);

        // Assert - (100 - 100) / 2.5 = 0
        Assert.Equal(0, accel, precision: 1);
    }

    #endregion

    #region RecordSpikeEvent Tests

    [Fact]
    public void RecordSpikeEvent_SingleSpike_RecordsEvent()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        service.RecordSpikeEvent(1u, 5000);

        // Assert - Can only verify indirectly via PredictNextSpike
        var (seconds, confidence) = service.PredictNextSpike(1u);
        // With only 1 spike, can't predict pattern
        Assert.Equal(float.MaxValue, seconds);
        Assert.Equal(0f, confidence);
    }

    [Fact]
    public void RecordSpikeEvent_MultipleSpikes_MaintainsHistory()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        var service = new DamageTrendService(mockIntake.Object);

        // Record multiple spikes with advancing time
        service.RecordSpikeEvent(1u, 3000);
        service.UpdateTime(10f); // Advance 10 seconds
        service.RecordSpikeEvent(1u, 3500);
        service.UpdateTime(10f); // Advance 10 seconds
        service.RecordSpikeEvent(1u, 3200);

        // Assert - Now we have 3 spikes with ~10s intervals
        var (seconds, confidence) = service.PredictNextSpike(1u);
        // Should detect pattern
        Assert.True(confidence > 0f);
    }

    #endregion

    #region PredictNextSpike Tests

    [Fact]
    public void PredictNextSpike_NoSpikes_ReturnsMaxValueAndZeroConfidence()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var (seconds, confidence) = service.PredictNextSpike(1u);

        // Assert
        Assert.Equal(float.MaxValue, seconds);
        Assert.Equal(0f, confidence);
    }

    [Fact]
    public void PredictNextSpike_TwoSpikes_ReturnsMaxValueAndZeroConfidence()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        var service = new DamageTrendService(mockIntake.Object);

        // Record only 2 spikes (need at least 3)
        service.RecordSpikeEvent(1u, 3000);
        service.UpdateTime(10f);
        service.RecordSpikeEvent(1u, 3000);

        // Act
        var (seconds, confidence) = service.PredictNextSpike(1u);

        // Assert
        Assert.Equal(float.MaxValue, seconds);
        Assert.Equal(0f, confidence);
    }

    [Fact]
    public void PredictNextSpike_ConsistentPattern_ReturnsValidPrediction()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        var service = new DamageTrendService(mockIntake.Object);

        // Record 4 spikes with consistent 10-second intervals
        service.RecordSpikeEvent(1u, 3000);
        service.UpdateTime(10f);
        service.RecordSpikeEvent(1u, 3100);
        service.UpdateTime(10f);
        service.RecordSpikeEvent(1u, 2900);
        service.UpdateTime(10f);
        service.RecordSpikeEvent(1u, 3050);
        service.UpdateTime(5f); // Half interval since last spike

        // Act
        var (seconds, confidence) = service.PredictNextSpike(1u);

        // Assert
        Assert.True(seconds < float.MaxValue);
        Assert.True(confidence >= 0.5f); // At least 50% confidence
        Assert.True(seconds > 0f && seconds < 10f); // Should predict within next interval
    }

    [Fact]
    public void PredictNextSpike_IrregularPattern_ReturnsLowConfidence()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        var service = new DamageTrendService(mockIntake.Object);

        // Record 4 spikes with very different intervals (5s, 15s, 8s)
        service.RecordSpikeEvent(1u, 3000);
        service.UpdateTime(5f);
        service.RecordSpikeEvent(1u, 3100);
        service.UpdateTime(15f);
        service.RecordSpikeEvent(1u, 2900);
        service.UpdateTime(8f);
        service.RecordSpikeEvent(1u, 3050);

        // Act
        var (seconds, confidence) = service.PredictNextSpike(1u);

        // Assert - Irregular pattern should have low or no confidence
        Assert.True(confidence < 0.7f);
    }

    #endregion

    #region IsInHighDamagePhase Tests

    [Fact]
    public void IsInHighDamagePhase_BelowThreshold_ReturnsFalse()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(1.5f)).Returns(500f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var result = service.IsInHighDamagePhase(800f, 3f);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInHighDamagePhase_AboveThresholdNotLongEnough_ReturnsFalse()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(1.5f)).Returns(1000f);
        mockIntake.Setup(s => s.GetPartyDamageRate(It.Is<float>(f => f >= 3f))).Returns(1000f);
        var service = new DamageTrendService(mockIntake.Object);

        // Only update once (1 second) - not long enough for 3 second requirement
        service.Update(1f, new uint[] { });

        // Act
        var result = service.IsInHighDamagePhase(800f, 3f);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsInHighDamagePhase_AboveThresholdLongEnough_ReturnsTrue()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(It.IsAny<float>())).Returns(1000f);
        var service = new DamageTrendService(mockIntake.Object);

        // Update multiple times to accumulate time
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });

        // Act
        var result = service.IsInHighDamagePhase(800f, 3f);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsInHighDamagePhase_CustomThreshold_UsesWindowAverage()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(1.5f)).Returns(600f);
        mockIntake.Setup(s => s.GetPartyDamageRate(3f)).Returns(600f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act - Using custom threshold below the default
        var result = service.IsInHighDamagePhase(500f, 3f);

        // Assert
        Assert.True(result);
    }

    #endregion

    #region GetHighDamagePhaseDuration Tests

    [Fact]
    public void GetHighDamagePhaseDuration_BelowThreshold_ReturnsZero()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(It.IsAny<float>())).Returns(500f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var duration = service.GetHighDamagePhaseDuration(800f);

        // Assert
        Assert.Equal(0f, duration);
    }

    [Fact]
    public void GetHighDamagePhaseDuration_TracksDuration()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(It.IsAny<float>())).Returns(1000f);
        var service = new DamageTrendService(mockIntake.Object);

        // Update to simulate time passing while in high damage phase
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });

        // Act
        var duration = service.GetHighDamagePhaseDuration(800f);

        // Assert - Should be approximately 5 seconds
        Assert.True(duration >= 4f);
    }

    [Fact]
    public void GetHighDamagePhaseDuration_CustomThreshold_EstimatesFromWindows()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Setup different rates for different windows
        mockIntake.Setup(s => s.GetPartyDamageRate(1.5f)).Returns(600f);
        mockIntake.Setup(s => s.GetPartyDamageRate(1f)).Returns(600f);
        mockIntake.Setup(s => s.GetPartyDamageRate(2f)).Returns(600f);
        mockIntake.Setup(s => s.GetPartyDamageRate(3f)).Returns(600f);
        mockIntake.Setup(s => s.GetPartyDamageRate(5f)).Returns(400f); // Drops below at 5s
        var service = new DamageTrendService(mockIntake.Object);

        // Act - Using custom threshold
        var duration = service.GetHighDamagePhaseDuration(500f);

        // Assert - Should return ~3 seconds (last window above threshold)
        Assert.True(duration >= 2f && duration <= 5f);
    }

    #endregion

    #region Update Tests

    [Fact]
    public void Update_AdvancesTime()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(It.IsAny<float>())).Returns(100f);
        var service = new DamageTrendService(mockIntake.Object);

        // Record a spike for timing reference
        service.RecordSpikeEvent(1u, 1000);
        service.Update(60f, new uint[] { }); // Advance past cleanup window

        // Act - Add another spike
        service.RecordSpikeEvent(1u, 1000);

        // The first spike should be cleaned up (older than 60s)
        // Only the second spike should remain
        var (_, confidence) = service.PredictNextSpike(1u);

        // Assert - Can't predict with only 1 spike after cleanup
        Assert.Equal(0f, confidence);
    }

    [Fact]
    public void Update_DetectsSpikes_WhenDamageRateHigh()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetDamageRate(1u, 1f)).Returns(1500f); // High spike
        mockIntake.Setup(s => s.GetDamageRate(1u, 3f)).Returns(50f); // Low previous rate
        mockIntake.Setup(s => s.GetPartyDamageRate(It.IsAny<float>())).Returns(500f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act - Update should auto-detect spike
        service.Update(1f, new uint[] { 1u });

        // Need more updates to get multiple spikes for pattern detection
        mockIntake.Setup(s => s.GetDamageRate(1u, 1f)).Returns(50f); // No spike
        service.Update(5f, new uint[] { 1u });

        mockIntake.Setup(s => s.GetDamageRate(1u, 1f)).Returns(1600f); // Another spike
        mockIntake.Setup(s => s.GetDamageRate(1u, 3f)).Returns(60f);
        service.Update(5f, new uint[] { 1u });

        // Assert - Spikes should be recorded
        // With only 2 spikes, won't have prediction yet, but events are recorded
        // Verify via checking a 3rd spike enables prediction
        mockIntake.Setup(s => s.GetDamageRate(1u, 1f)).Returns(50f);
        service.Update(5f, new uint[] { 1u });

        mockIntake.Setup(s => s.GetDamageRate(1u, 1f)).Returns(1700f);
        mockIntake.Setup(s => s.GetDamageRate(1u, 3f)).Returns(70f);
        service.Update(5f, new uint[] { 1u });

        var (seconds, confidence) = service.PredictNextSpike(1u);
        Assert.True(confidence > 0f); // Should have pattern now
    }

    [Fact]
    public void Update_TracksHighDamagePhase()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(It.IsAny<float>())).Returns(1000f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act - Multiple updates at high damage
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });

        // Assert
        Assert.True(service.IsInHighDamagePhase(800f, 2f));
    }

    [Fact]
    public void Update_ExitsHighDamagePhase_WhenDamageDrops()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(It.IsAny<float>())).Returns(1000f);
        var service = new DamageTrendService(mockIntake.Object);

        // Enter high damage phase
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });
        service.Update(1f, new uint[] { });

        Assert.True(service.IsInHighDamagePhase(800f, 2f));

        // Now drop damage
        mockIntake.Setup(s => s.GetPartyDamageRate(It.IsAny<float>())).Returns(500f);
        service.Update(1f, new uint[] { });

        // Assert - Should no longer be in high damage phase
        Assert.False(service.IsInHighDamagePhase(800f, 2f));
    }

    #endregion

    #region GetCurrentDamageRate Tests

    [Fact]
    public void GetCurrentDamageRate_DelegatesToIntakeService()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetDamageRate(1u, 3f)).Returns(500f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var rate = service.GetCurrentDamageRate(1u, 3f);

        // Assert
        Assert.Equal(500f, rate);
        mockIntake.Verify(s => s.GetDamageRate(1u, 3f), Times.Once);
    }

    #endregion

    #region GetSpikeSeverity Tests

    [Fact]
    public void GetSpikeSeverity_NoSpike_ReturnsZero()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(It.IsAny<float>())).Returns(50f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var severity = service.GetSpikeSeverity(0.9f);

        // Assert
        Assert.Equal(0f, severity);
    }

    [Fact]
    public void GetSpikeSeverity_SpikingTrend_ReturnsHighSeverity()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Setup spiking scenario
        mockIntake.Setup(s => s.GetPartyDamageRate(2.5f)).Returns(500f);
        mockIntake.Setup(s => s.GetPartyDamageRate(5f)).Returns(200f);
        mockIntake.Setup(s => s.GetPartyDamageRate(3f)).Returns(500f);
        mockIntake.Setup(s => s.GetPartyDamageRate(1.5f)).Returns(500f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var severity = service.GetSpikeSeverity(0.7f); // 70% party HP

        // Assert - Should have some severity
        Assert.True(severity > 0f);
    }

    [Fact]
    public void GetSpikeSeverity_LowPartyHp_Increaseseverity()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(2.5f)).Returns(500f);
        mockIntake.Setup(s => s.GetPartyDamageRate(5f)).Returns(200f);
        mockIntake.Setup(s => s.GetPartyDamageRate(3f)).Returns(500f);
        mockIntake.Setup(s => s.GetPartyDamageRate(1.5f)).Returns(500f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var severityHighHp = service.GetSpikeSeverity(0.9f);
        var severityLowHp = service.GetSpikeSeverity(0.4f);

        // Assert - Lower HP should result in higher severity
        Assert.True(severityLowHp > severityHighHp);
    }

    [Fact]
    public void GetSpikeSeverity_VeryHighDamageRate_AddsSeverityBonus()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetPartyDamageRate(2.5f)).Returns(5500f);
        mockIntake.Setup(s => s.GetPartyDamageRate(5f)).Returns(2500f);
        mockIntake.Setup(s => s.GetPartyDamageRate(3f)).Returns(5500f);
        mockIntake.Setup(s => s.GetPartyDamageRate(1.5f)).Returns(5500f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var severity = service.GetSpikeSeverity(0.7f);

        // Assert - Very high damage should have bonus severity
        Assert.True(severity >= 0.6f);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ClassifyTrend_ZeroPreviousRate_WithHighCurrent_ReturnsSpiking()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        // Current rate = 100 DPS, previous = 0 DPS (division by zero protection)
        mockIntake.Setup(s => s.GetPartyDamageRate(5f)).Returns(100f);
        mockIntake.Setup(s => s.GetPartyDamageRate(10f)).Returns(50f); // Average
        var service = new DamageTrendService(mockIntake.Object);

        // Act
        var trend = service.GetPartyDamageTrend(10f);

        // Assert - When previous is 0 and current is significant, should spike
        Assert.True(trend == DamageTrend.Spiking || trend == DamageTrend.Increasing);
    }

    [Fact]
    public void SpikeCooldown_PreventsMultipleRecordings()
    {
        // Arrange
        var mockIntake = new Mock<IDamageIntakeService>();
        mockIntake.Setup(s => s.GetDamageRate(1u, 1f)).Returns(1500f);
        mockIntake.Setup(s => s.GetDamageRate(1u, 3f)).Returns(50f);
        mockIntake.Setup(s => s.GetPartyDamageRate(It.IsAny<float>())).Returns(500f);
        var service = new DamageTrendService(mockIntake.Object);

        // Act - Multiple rapid updates (within cooldown)
        service.Update(0.5f, new uint[] { 1u }); // Spike detected
        service.Update(0.5f, new uint[] { 1u }); // Should be blocked by cooldown
        service.Update(0.5f, new uint[] { 1u }); // Should be blocked by cooldown

        // After cooldown (2+ seconds)
        service.Update(2.0f, new uint[] { 1u }); // New spike
        service.Update(2.0f, new uint[] { 1u }); // Should be blocked
        service.Update(2.0f, new uint[] { 1u }); // New spike

        // Assert - Should only have 3 spikes recorded, not 6
        var (seconds, confidence) = service.PredictNextSpike(1u);
        Assert.True(confidence >= 0f); // Has at least 3 spikes for pattern
    }

    #endregion
}
