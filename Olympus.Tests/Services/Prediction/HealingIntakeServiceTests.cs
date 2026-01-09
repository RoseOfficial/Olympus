using System;
using Moq;
using Olympus.Services;
using Olympus.Services.Prediction;
using Xunit;

namespace Olympus.Tests.Services.Prediction;

/// <summary>
/// Unit tests for HealingIntakeService.
/// </summary>
public class HealingIntakeServiceTests
{
    #region RecordHealing Tests

    [Fact]
    public void RecordHealing_PositiveAmount_RecordsHealing()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);

        // Act
        service.RecordHealing(1, 1000);

        // Assert
        var intake = service.GetRecentHealingIntake(1, 5f);
        Assert.Equal(1000, intake);
    }

    [Fact]
    public void RecordHealing_ZeroAmount_DoesNotRecord()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);

        // Act
        service.RecordHealing(1, 0);

        // Assert
        var intake = service.GetRecentHealingIntake(1, 5f);
        Assert.Equal(0, intake);
    }

    [Fact]
    public void RecordHealing_NegativeAmount_DoesNotRecord()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);

        // Act
        service.RecordHealing(1, -500);

        // Assert
        var intake = service.GetRecentHealingIntake(1, 5f);
        Assert.Equal(0, intake);
    }

    [Fact]
    public void RecordHealing_MultipleHealingEvents_AccumulatesTotal()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);

        // Act
        service.RecordHealing(1, 1000);
        service.RecordHealing(1, 500);
        service.RecordHealing(1, 250);

        // Assert
        var intake = service.GetRecentHealingIntake(1, 5f);
        Assert.Equal(1750, intake);
    }

    [Fact]
    public void RecordHealing_DifferentEntities_TrackedSeparately()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);

        // Act
        service.RecordHealing(1, 1000);
        service.RecordHealing(2, 500);

        // Assert
        Assert.Equal(1000, service.GetRecentHealingIntake(1, 5f));
        Assert.Equal(500, service.GetRecentHealingIntake(2, 5f));
    }

    #endregion

    #region GetRecentHealingIntake Tests

    [Fact]
    public void GetRecentHealingIntake_NoRecords_ReturnsZero()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);

        // Act
        var intake = service.GetRecentHealingIntake(999, 5f);

        // Assert
        Assert.Equal(0, intake);
    }

    [Fact]
    public void GetRecentHealingIntake_WithinWindow_ReturnsTotal()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);
        service.RecordHealing(1, 1000);
        service.RecordHealing(1, 500);

        // Act
        var intake = service.GetRecentHealingIntake(1, 5f);

        // Assert
        Assert.Equal(1500, intake);
    }

    #endregion

    #region GetHealingRate Tests

    [Fact]
    public void GetHealingRate_NoHealing_ReturnsZero()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);

        // Act
        var rate = service.GetHealingRate(1, 5f);

        // Assert
        Assert.Equal(0f, rate);
    }

    [Fact]
    public void GetHealingRate_WithHealing_ReturnsHealingPerSecond()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);
        service.RecordHealing(1, 5000);

        // Act - 5000 healing over 5 second window = 1000 HPS
        var rate = service.GetHealingRate(1, 5f);

        // Assert
        Assert.Equal(1000f, rate);
    }

    [Fact]
    public void GetHealingRate_ZeroWindow_ReturnsZero()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);
        service.RecordHealing(1, 5000);

        // Act
        var rate = service.GetHealingRate(1, 0f);

        // Assert
        Assert.Equal(0f, rate);
    }

    [Fact]
    public void GetHealingRate_NegativeWindow_ReturnsZero()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);
        service.RecordHealing(1, 5000);

        // Act
        var rate = service.GetHealingRate(1, -5f);

        // Assert
        Assert.Equal(0f, rate);
    }

    #endregion

    #region GetPartyHealingIntake Tests

    [Fact]
    public void GetPartyHealingIntake_NoHealing_ReturnsZero()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);

        // Act
        var total = service.GetPartyHealingIntake(5f);

        // Assert
        Assert.Equal(0, total);
    }

    [Fact]
    public void GetPartyHealingIntake_MultipleEntities_ReturnsSumOfAll()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);
        service.RecordHealing(1, 1000);
        service.RecordHealing(2, 500);
        service.RecordHealing(3, 250);

        // Act
        var total = service.GetPartyHealingIntake(5f);

        // Assert
        Assert.Equal(1750, total);
    }

    #endregion

    #region GetPartyHealingRate Tests

    [Fact]
    public void GetPartyHealingRate_NoHealing_ReturnsZero()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);

        // Act
        var rate = service.GetPartyHealingRate(5f);

        // Assert
        Assert.Equal(0f, rate);
    }

    [Fact]
    public void GetPartyHealingRate_WithHealing_ReturnsPartyHps()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);
        service.RecordHealing(1, 2500);
        service.RecordHealing(2, 2500);

        // Act - 5000 total healing over 5 second window = 1000 party HPS
        var rate = service.GetPartyHealingRate(5f);

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
        var service = new HealingIntakeService(combatEventService.Object);
        service.RecordHealing(1, 1000);
        service.RecordHealing(2, 500);

        // Act
        service.Clear();

        // Assert
        Assert.Equal(0, service.GetRecentHealingIntake(1, 5f));
        Assert.Equal(0, service.GetRecentHealingIntake(2, 5f));
        Assert.Equal(0, service.GetPartyHealingIntake(5f));
    }

    [Fact]
    public void ClearEntity_RemovesOnlySpecificEntity()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);
        service.RecordHealing(1, 1000);
        service.RecordHealing(2, 500);

        // Act
        service.ClearEntity(1);

        // Assert
        Assert.Equal(0, service.GetRecentHealingIntake(1, 5f));
        Assert.Equal(500, service.GetRecentHealingIntake(2, 5f));
    }

    #endregion

    #region Event Integration Tests

    [Fact]
    public void OnAnyHealReceived_RecordsHealingFromEvent()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        Action<uint, uint, int>? healHandler = null;

        combatEventService.SetupAdd(x => x.OnAnyHealReceived += It.IsAny<Action<uint, uint, int>>())
            .Callback<Action<uint, uint, int>>(handler => healHandler = handler);

        var service = new HealingIntakeService(combatEventService.Object);

        // Act - Simulate heal event (healerId, targetId, amount)
        healHandler?.Invoke(100, 1, 1500);

        // Assert - Healing should be recorded for the target (entity 1)
        var intake = service.GetRecentHealingIntake(1, 5f);
        Assert.Equal(1500, intake);
    }

    [Fact]
    public void OnAnyHealReceived_TracksHealingByTarget_NotByHealer()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        Action<uint, uint, int>? healHandler = null;

        combatEventService.SetupAdd(x => x.OnAnyHealReceived += It.IsAny<Action<uint, uint, int>>())
            .Callback<Action<uint, uint, int>>(handler => healHandler = handler);

        var service = new HealingIntakeService(combatEventService.Object);

        // Act - Different healers healing the same target
        healHandler?.Invoke(100, 1, 1000); // Healer 100 heals target 1
        healHandler?.Invoke(200, 1, 500);  // Healer 200 heals target 1

        // Assert - All healing should be tracked for the target
        var targetIntake = service.GetRecentHealingIntake(1, 5f);
        Assert.Equal(1500, targetIntake);

        // Healer IDs should not have any records (we track by target)
        Assert.Equal(0, service.GetRecentHealingIntake(100, 5f));
        Assert.Equal(0, service.GetRecentHealingIntake(200, 5f));
    }

    [Fact]
    public void Dispose_UnsubscribesFromEvents()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);

        // Act
        service.Dispose();

        // Assert
        combatEventService.VerifyRemove(
            x => x.OnAnyHealReceived -= It.IsAny<Action<uint, uint, int>>(),
            Times.Once);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void RecordHealing_LargeAmount_HandlesCorrectly()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);

        // Act - Very large heal (Benediction can be 50k+)
        service.RecordHealing(1, 50000);

        // Assert
        Assert.Equal(50000, service.GetRecentHealingIntake(1, 5f));
    }

    [Fact]
    public void RecordHealing_ManyRecords_HandlesMaxEntriesLimit()
    {
        // Arrange
        var combatEventService = new Mock<ICombatEventService>();
        var service = new HealingIntakeService(combatEventService.Object);

        // Act - Record more than MaxEntriesPerEntity (100)
        for (var i = 0; i < 150; i++)
        {
            service.RecordHealing(1, 100);
        }

        // Assert - Should have pruned to 100 entries
        // Total would be 15000 if all kept, but we keep latest 100 = 10000
        var intake = service.GetRecentHealingIntake(1, 60f); // Large window to get all
        Assert.Equal(10000, intake);
    }

    #endregion
}
