using System;
using Moq;
using Olympus.Services;
using Olympus.Services.Prediction;
using Xunit;

namespace Olympus.Tests.Services.Prediction;

/// <summary>
/// Unit tests for DamageIntakeService.
/// </summary>
public class DamageIntakeServiceTests
{
    #region RecordDamage Tests

    [Fact]
    public void RecordDamage_PositiveAmount_RecordsDamage()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);

        // Act
        service.RecordDamage(1, 1000);

        // Assert
        var intake = service.GetRecentDamageIntake(1, 5f);
        Assert.Equal(1000, intake);
    }

    [Fact]
    public void RecordDamage_ZeroAmount_DoesNotRecord()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);

        // Act
        service.RecordDamage(1, 0);

        // Assert
        var intake = service.GetRecentDamageIntake(1, 5f);
        Assert.Equal(0, intake);
    }

    [Fact]
    public void RecordDamage_NegativeAmount_DoesNotRecord()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);

        // Act
        service.RecordDamage(1, -500);

        // Assert
        var intake = service.GetRecentDamageIntake(1, 5f);
        Assert.Equal(0, intake);
    }

    [Fact]
    public void RecordDamage_MultipleDamageEvents_AccumulatesTotal()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);

        // Act
        service.RecordDamage(1, 1000);
        service.RecordDamage(1, 500);
        service.RecordDamage(1, 250);

        // Assert
        var intake = service.GetRecentDamageIntake(1, 5f);
        Assert.Equal(1750, intake);
    }

    [Fact]
    public void RecordDamage_DifferentEntities_TrackedSeparately()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);

        // Act
        service.RecordDamage(1, 1000);
        service.RecordDamage(2, 500);

        // Assert
        Assert.Equal(1000, service.GetRecentDamageIntake(1, 5f));
        Assert.Equal(500, service.GetRecentDamageIntake(2, 5f));
    }

    #endregion

    #region GetRecentDamageIntake Tests

    [Fact]
    public void GetRecentDamageIntake_NoRecords_ReturnsZero()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);

        // Act
        var intake = service.GetRecentDamageIntake(999, 5f);

        // Assert
        Assert.Equal(0, intake);
    }

    [Fact]
    public void GetRecentDamageIntake_WithinWindow_ReturnsTotal()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);
        service.RecordDamage(1, 1000);
        service.RecordDamage(1, 500);

        // Act
        var intake = service.GetRecentDamageIntake(1, 5f);

        // Assert
        Assert.Equal(1500, intake);
    }

    #endregion

    #region GetDamageRate Tests

    [Fact]
    public void GetDamageRate_NoDamage_ReturnsZero()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);

        // Act
        var rate = service.GetDamageRate(1, 5f);

        // Assert
        Assert.Equal(0f, rate);
    }

    [Fact]
    public void GetDamageRate_WithDamage_ReturnsDamagePerSecond()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);
        service.RecordDamage(1, 5000);

        // Act - 5000 damage over 5 second window = 1000 DPS
        var rate = service.GetDamageRate(1, 5f);

        // Assert
        Assert.Equal(1000f, rate);
    }

    [Fact]
    public void GetDamageRate_ZeroWindow_ReturnsZero()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);
        service.RecordDamage(1, 5000);

        // Act
        var rate = service.GetDamageRate(1, 0f);

        // Assert
        Assert.Equal(0f, rate);
    }

    [Fact]
    public void GetDamageRate_NegativeWindow_ReturnsZero()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);
        service.RecordDamage(1, 5000);

        // Act
        var rate = service.GetDamageRate(1, -5f);

        // Assert
        Assert.Equal(0f, rate);
    }

    #endregion

    #region GetPartyDamageIntake Tests

    [Fact]
    public void GetPartyDamageIntake_NoDamage_ReturnsZero()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);

        // Act
        var total = service.GetPartyDamageIntake(5f);

        // Assert
        Assert.Equal(0, total);
    }

    [Fact]
    public void GetPartyDamageIntake_MultipleEntities_ReturnsSumOfAll()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);
        service.RecordDamage(1, 1000);
        service.RecordDamage(2, 500);
        service.RecordDamage(3, 250);

        // Act
        var total = service.GetPartyDamageIntake(5f);

        // Assert
        Assert.Equal(1750, total);
    }

    #endregion

    #region GetPartyDamageRate Tests

    [Fact]
    public void GetPartyDamageRate_NoDamage_ReturnsZero()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);

        // Act
        var rate = service.GetPartyDamageRate(5f);

        // Assert
        Assert.Equal(0f, rate);
    }

    [Fact]
    public void GetPartyDamageRate_WithDamage_ReturnsPartyDps()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);
        service.RecordDamage(1, 2500);
        service.RecordDamage(2, 2500);

        // Act - 5000 total damage over 5 second window = 1000 party DPS
        var rate = service.GetPartyDamageRate(5f);

        // Assert
        Assert.Equal(1000f, rate);
    }

    #endregion

    #region Clear Tests

    [Fact]
    public void Clear_RemovesAllRecords()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);
        service.RecordDamage(1, 1000);
        service.RecordDamage(2, 500);

        // Act
        service.Clear();

        // Assert
        Assert.Equal(0, service.GetRecentDamageIntake(1, 5f));
        Assert.Equal(0, service.GetRecentDamageIntake(2, 5f));
        Assert.Equal(0, service.GetPartyDamageIntake(5f));
    }

    [Fact]
    public void ClearEntity_RemovesOnlySpecificEntity()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);
        service.RecordDamage(1, 1000);
        service.RecordDamage(2, 500);

        // Act
        service.ClearEntity(1);

        // Assert
        Assert.Equal(0, service.GetRecentDamageIntake(1, 5f));
        Assert.Equal(500, service.GetRecentDamageIntake(2, 5f));
    }

    #endregion

    #region Event Integration Tests

    [Fact]
    public void OnDamageReceived_RecordsDamageFromEvent()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        Action<uint, int>? damageHandler = null;

        combatEventService.SetupAdd(x => x.OnDamageReceived += It.IsAny<Action<uint, int>>())
            .Callback<Action<uint, int>>(handler => damageHandler = handler);

        var service = new DamageIntakeService(combatEventService.Object);

        // Act - Simulate damage event
        damageHandler?.Invoke(1, 1500);

        // Assert
        var intake = service.GetRecentDamageIntake(1, 5f);
        Assert.Equal(1500, intake);
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);

        // Act
        service.Dispose();

        // Assert
        combatEventService.VerifyRemove(
            x => x.OnDamageReceived -= It.IsAny<Action<uint, int>>(),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void RecordDamage_LargeAmount_HandlesCorrectly()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);

        // Act - Very large damage (raid-wide might be 30-50k)
        service.RecordDamage(1, 50000);

        // Assert
        Assert.Equal(50000, service.GetRecentDamageIntake(1, 5f));
    }

    [Fact]
    public void RecordDamage_ManyRecords_HandlesMaxEntriesLimit()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new DamageIntakeService(combatEventService.Object);

        // Act - Record more than MaxEntriesPerEntity (100)
        for (var i = 0; i < 150; i++)
        {
            service.RecordDamage(1, 100);
        }

        // Assert - Should have pruned to 100 entries
        // Total would be 15000 if all kept, but we keep latest 100 = 10000
        var intake = service.GetRecentDamageIntake(1, 60f); // Large window to get all
        Assert.Equal(10000, intake);
    }

    #endregion
}
