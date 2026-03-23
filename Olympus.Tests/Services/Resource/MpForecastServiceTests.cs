using Olympus.Services.Resource;
using Xunit;

namespace Olympus.Tests.Services.Resource;

/// <summary>
/// Unit tests for MpForecastService.
/// No mocks needed — the service takes no constructor args and all state is
/// driven by Update() / RecordMpExpenditure().
///
/// Note on GetMpConsumptionRate(): uses DateTime.UtcNow internally. When an
/// expenditure is recorded and queried in the same test with no real elapsed time,
/// windowDuration is ~0 and the method returns 0. Tests avoid relying on a
/// non-zero consumption rate.
/// </summary>
public class MpForecastServiceTests
{
    // -------------------------------------------------------------------------
    // Update — state setters
    // -------------------------------------------------------------------------

    [Fact]
    public void Update_SetsCurrentMpAndMaxMp()
    {
        // Arrange
        var service = new MpForecastService();

        // Act
        service.Update(7500, 10000, hasLucidDreaming: false);

        // Assert
        Assert.Equal(7500, service.CurrentMp);
        Assert.Equal(10000, service.MaxMp);
    }

    [Fact]
    public void Update_ZeroMaxMp_DefaultsToTenThousand()
    {
        // Arrange
        var service = new MpForecastService();

        // Act — passing 0 for maxMp should fall back to 10000
        service.Update(5000, 0, hasLucidDreaming: false);

        // Assert
        Assert.Equal(10000, service.MaxMp);
    }

    [Fact]
    public void Update_SetsLucidDreamingFlag()
    {
        // Arrange
        var service = new MpForecastService();

        // Act
        service.Update(10000, 10000, hasLucidDreaming: true);

        // Assert
        Assert.True(service.IsLucidDreamingActive);
    }

    // -------------------------------------------------------------------------
    // MpPercent
    // -------------------------------------------------------------------------

    [Fact]
    public void MpPercent_ReflectsCurrentOverMax()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(5000, 10000, hasLucidDreaming: false);

        // Act
        var percent = service.MpPercent;

        // Assert
        Assert.Equal(0.5f, percent, precision: 5);
    }

    // -------------------------------------------------------------------------
    // GetMpRegenRate
    // -------------------------------------------------------------------------

    [Fact]
    public void GetMpRegenRate_NoLucid_ReturnsBaseRegen()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(10000, 10000, hasLucidDreaming: false);

        // Act — base = (10000 * 0.02) / 3 = 66.6667 MP/s
        var rate = service.GetMpRegenRate();

        // Assert
        Assert.Equal(66.6667f, rate, precision: 3);
    }

    [Fact]
    public void GetMpRegenRate_WithLucid_ReturnsBaseRegenPlusBonus()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(10000, 10000, hasLucidDreaming: true);

        // Act — (10000 * 0.02 + 10000 * 0.055) / 3 = 250 MP/s
        var rate = service.GetMpRegenRate();

        // Assert
        Assert.Equal(250.0f, rate, precision: 3);
    }

    [Fact]
    public void GetMpRegenRate_BaseRegen_Formula_Verified()
    {
        // Arrange — smaller max MP to verify formula scales correctly
        var service = new MpForecastService();
        service.Update(6000, 6000, hasLucidDreaming: false);

        // Act — (6000 * 0.02) / 3 = 40 MP/s
        var rate = service.GetMpRegenRate();

        // Assert
        Assert.Equal(40.0f, rate, precision: 3);
    }

    // -------------------------------------------------------------------------
    // GetMpConsumptionRate
    // -------------------------------------------------------------------------

    [Fact]
    public void GetMpConsumptionRate_NoExpenditures_ReturnsZero()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(10000, 10000, hasLucidDreaming: false);

        // Act
        var rate = service.GetMpConsumptionRate();

        // Assert
        Assert.Equal(0f, rate);
    }

    // -------------------------------------------------------------------------
    // RecordMpExpenditure — guard-rail behavior
    // -------------------------------------------------------------------------

    [Fact]
    public void RecordMpExpenditure_ZeroAmount_NotRecorded()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(10000, 10000, hasLucidDreaming: false);

        // Act — zero should be rejected
        service.RecordMpExpenditure(0);

        // Assert — consumption rate stays at 0
        Assert.Equal(0f, service.GetMpConsumptionRate());
    }

    [Fact]
    public void RecordMpExpenditure_NegativeAmount_NotRecorded()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(10000, 10000, hasLucidDreaming: false);

        // Act — negative should be rejected
        service.RecordMpExpenditure(-500);

        // Assert — consumption rate stays at 0
        Assert.Equal(0f, service.GetMpConsumptionRate());
    }

    // -------------------------------------------------------------------------
    // PredictMpAtTime
    // -------------------------------------------------------------------------

    [Fact]
    public void PredictMpAtTime_ZeroSeconds_ReturnsCurrentMp()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(7000, 10000, hasLucidDreaming: false);

        // Act
        var predicted = service.PredictMpAtTime(0f);

        // Assert
        Assert.Equal(7000, predicted);
    }

    [Fact]
    public void PredictMpAtTime_NegativeSeconds_ReturnsCurrentMp()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(7000, 10000, hasLucidDreaming: false);

        // Act
        var predicted = service.PredictMpAtTime(-5f);

        // Assert
        Assert.Equal(7000, predicted);
    }

    [Fact]
    public void PredictMpAtTime_PositiveSeconds_NoConsumption_ReturnsRegenAdded()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(5000, 10000, hasLucidDreaming: false);

        // Act — 3s from now at 10000 max MP, no Lucid, no consumption:
        // fractional ticks = 3.0 / 3.0 = 1.0 ticks
        // regen = 1.0 * (10000 * 0.02) = 200 MP
        // result: 5000 + 200 = 5200
        // Note: the service uses fractional ticks, not discrete; 1.5s would yield 100 MP regen
        var predicted = service.PredictMpAtTime(3.0f);

        // Assert
        Assert.Equal(5200, predicted);
    }

    [Fact]
    public void PredictMpAtTime_ClampsAtMaxMp()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(9900, 10000, hasLucidDreaming: false);

        // Act — large window: regen would push past 10000
        var predicted = service.PredictMpAtTime(300.0f);

        // Assert
        Assert.Equal(10000, predicted);
    }

    [Fact]
    public void PredictMpAtTime_ClampsAtZero()
    {
        // Arrange — start at 0 MP
        var service = new MpForecastService();
        service.Update(0, 10000, hasLucidDreaming: false);

        // Act — 0 seconds, no regen, no consumption; result stays at 0
        var predicted = service.PredictMpAtTime(0f);

        // Assert
        Assert.Equal(0, predicted);
    }

    // -------------------------------------------------------------------------
    // SecondsUntilOom
    // -------------------------------------------------------------------------

    [Fact]
    public void SecondsUntilOom_NoConsumption_ReturnsMaxValue()
    {
        // Arrange — net rate is positive (regen only), so we never run out
        var service = new MpForecastService();
        service.Update(10000, 10000, hasLucidDreaming: false);

        // Act
        var result = service.SecondsUntilOom();

        // Assert
        Assert.Equal(float.MaxValue, result);
    }

    [Fact]
    public void SecondsUntilOom_AlreadyAtReserve_NoConsumption_ReturnsMaxValue()
    {
        // Arrange — currentMp == reserveMp. However, netRate >= 0 (no consumption),
        // so the method short-circuits and returns float.MaxValue before checking spendable MP.
        var service = new MpForecastService();
        service.Update(2400, 10000, hasLucidDreaming: false);

        // Act
        var result = service.SecondsUntilOom(reserveMp: 2400);

        // Assert — netRate positive → float.MaxValue regardless of current MP level
        Assert.Equal(float.MaxValue, result);
    }

    [Fact]
    public void SecondsUntilOom_BelowReserve_NoConsumption_ReturnsMaxValue()
    {
        // Arrange — currentMp below reserve. netRate >= 0 (no consumption) →
        // method short-circuits and returns float.MaxValue before the spendable-MP check.
        var service = new MpForecastService();
        service.Update(1000, 10000, hasLucidDreaming: false);

        // Act
        var result = service.SecondsUntilOom(reserveMp: 2400);

        // Assert — netRate positive → float.MaxValue
        Assert.Equal(float.MaxValue, result);
    }

    [Fact]
    public void SecondsUntilOom_AtReserve_WithExpenditure_ReturnsZero()
    {
        // Arrange — MP is at reserve and an expenditure has been recorded.
        // The consumption rate floor (1s minimum window) ensures the expenditure
        // produces a non-zero rate even in unit tests. With netRate < 0 and
        // spendableMp = currentMp - reserve = 0, the method returns 0f (already at reserve).
        var service = new MpForecastService();
        service.Update(currentMp: 2400, maxMp: 10000, hasLucidDreaming: false);
        service.RecordMpExpenditure(800);

        // Act
        var result = service.SecondsUntilOom(reserveMp: 2400);

        // Assert — already at reserve with net consumption → 0
        Assert.Equal(0f, result);
    }

    // -------------------------------------------------------------------------
    // GetTimeUntilMpBelowThreshold
    // -------------------------------------------------------------------------

    [Fact]
    public void GetTimeUntilMpBelowThreshold_AlreadyBelowThreshold_NoConsumption_ReturnsMaxValue()
    {
        // Arrange — currentMp (3000) below threshold (5000). However, netRate >= 0 (no consumption),
        // so the method short-circuits and returns float.MaxValue before checking mpToLose.
        var service = new MpForecastService();
        service.Update(3000, 10000, hasLucidDreaming: false);

        // Act
        var result = service.GetTimeUntilMpBelowThreshold(5000);

        // Assert — netRate positive → float.MaxValue
        Assert.Equal(float.MaxValue, result);
    }

    [Fact]
    public void GetTimeUntilMpBelowThreshold_BelowThreshold_WithExpenditure_ReturnsZero()
    {
        // Arrange — MP (3000) is already below the threshold (4000) and an expenditure
        // has been recorded. The consumption rate floor (1s minimum window) ensures a
        // non-zero rate. With netRate < 0 and mpToLose = 3000 - 4000 = -1000 (already
        // below threshold), the method returns 0f.
        var service = new MpForecastService();
        service.Update(currentMp: 3000, maxMp: 10000, hasLucidDreaming: false);
        service.RecordMpExpenditure(800);

        // Act
        var result = service.GetTimeUntilMpBelowThreshold(thresholdMp: 4000);

        // Assert — already below threshold with net consumption → 0
        Assert.Equal(0f, result);
    }

    [Fact]
    public void GetTimeUntilMpBelowThreshold_NoConsumption_ReturnsMaxValue()
    {
        // Arrange — net rate >= 0 (no consumption), so we won't drop below threshold
        var service = new MpForecastService();
        service.Update(10000, 10000, hasLucidDreaming: false);

        // Act
        var result = service.GetTimeUntilMpBelowThreshold(5000);

        // Assert
        Assert.Equal(float.MaxValue, result);
    }

    // -------------------------------------------------------------------------
    // CanAffordSpellIn
    // -------------------------------------------------------------------------

    [Fact]
    public void CanAffordSpellIn_InstantCast_CurrentMpSufficient_ReturnsTrue()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(5000, 10000, hasLucidDreaming: false);

        // Act
        var result = service.CanAffordSpellIn(1500, castTime: 0f);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void CanAffordSpellIn_InstantCast_InsufficientMp_ReturnsFalse()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(1000, 10000, hasLucidDreaming: false);

        // Act
        var result = service.CanAffordSpellIn(1500, castTime: 0f);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void CanAffordSpellIn_WithCastTime_EnoughRegenToAfford_ReturnsTrue()
    {
        // Arrange — start at 9800 MP. Cast time = 3s → 1 tick regen = 200 MP → predicted = 10000.
        // Spell costs 200 MP → 10000 >= 200 → true.
        var service = new MpForecastService();
        service.Update(9800, 10000, hasLucidDreaming: false);

        // Act
        var result = service.CanAffordSpellIn(200, castTime: 3.0f);

        // Assert
        Assert.True(result);
    }

    // -------------------------------------------------------------------------
    // Conservation mode
    // -------------------------------------------------------------------------

    [Fact]
    public void ConservationMode_InitiallyFalse()
    {
        // Arrange & Act
        var service = new MpForecastService();

        // Assert — default state before any Update
        Assert.False(service.IsInConservationMode);
    }

    [Fact]
    public void ConservationMode_HighMpNoConsumption_RemainsOff()
    {
        // Arrange — high MP, no consumption → net rate >= 0 → conservation never enters
        var service = new MpForecastService();
        service.Update(9000, 10000, hasLucidDreaming: false);

        // Act (Update has already run UpdateConservationMode internally)
        var inConservation = service.IsInConservationMode;

        // Assert
        Assert.False(inConservation);
    }

    // -------------------------------------------------------------------------
    // GetNetMpRate
    // -------------------------------------------------------------------------

    [Fact]
    public void GetNetMpRate_NoConsumption_EqualsRegenRate()
    {
        // Arrange
        var service = new MpForecastService();
        service.Update(10000, 10000, hasLucidDreaming: false);

        // Act
        var netRate  = service.GetNetMpRate();
        var regenRate = service.GetMpRegenRate();

        // Assert — with no expenditures, net == regen
        Assert.Equal(regenRate, netRate, precision: 5);
    }
}
