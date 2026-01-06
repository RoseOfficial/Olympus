using System;
using System.Collections.Generic;
using System.Threading;
using Moq;
using Olympus.Services;
using Olympus.Services.Prediction;
using Olympus.Tests.Mocks;
using Xunit;

namespace Olympus.Tests.Services.Prediction;

/// <summary>
/// Tests for HpPredictionService covering HP prediction, pending heals,
/// timeout logic, and edge cases.
/// </summary>
public sealed class HpPredictionServiceTests : IDisposable
{
    private readonly Mock<ICombatEventService> _mockCombatEvent;
    private readonly Configuration _configuration;
    private readonly HpPredictionService _service;

    public HpPredictionServiceTests()
    {
        _mockCombatEvent = MockBuilders.CreateMockCombatEventService();
        _configuration = MockBuilders.CreateDefaultConfiguration();
        // Disable crit variance by default to maintain existing test behavior
        _configuration.Healing.EnableCritVarianceReduction = false;
        _service = new HpPredictionService(_mockCombatEvent.Object, _configuration);
    }

    public void Dispose()
    {
        _service.Dispose();
    }

    #region Basic HP Prediction

    [Fact]
    public void GetPredictedHp_NoPendingHeals_ReturnsShadowHp()
    {
        // Arrange
        const uint entityId = 1;
        const uint currentHp = 5000;
        const uint maxHp = 10000;

        // Act
        var result = _service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert - should return currentHp (pass-through via mock)
        Assert.Equal(currentHp, result);
    }

    [Fact]
    public void GetPredictedHp_WithPendingHeal_AddsAmount()
    {
        // Arrange
        const uint entityId = 1;
        const uint currentHp = 5000;
        const uint maxHp = 10000;
        const int healAmount = 2000;

        _service.RegisterPendingHeal(entityId, healAmount);

        // Act
        var result = _service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert
        Assert.Equal(currentHp + (uint)healAmount, result);
    }

    [Fact]
    public void GetPredictedHp_ClampsAtMaxHp_NeverExceeds()
    {
        // Arrange
        const uint entityId = 1;
        const uint currentHp = 9000;
        const uint maxHp = 10000;
        const int healAmount = 5000; // Would exceed max

        _service.RegisterPendingHeal(entityId, healAmount);

        // Act
        var result = _service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert - should clamp to maxHp
        Assert.Equal(maxHp, result);
    }

    [Fact]
    public void GetPredictedHp_ClampsAtZero_NeverNegative()
    {
        // Arrange - simulate negative damage pending (shouldn't happen but test edge case)
        const uint entityId = 1;
        const uint currentHp = 1000;
        const uint maxHp = 10000;

        // Register a negative heal (simulating damage, though not typical usage)
        _service.RegisterPendingHeal(entityId, -5000);

        // Act
        var result = _service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert - should clamp to 0
        Assert.Equal(0u, result);
    }

    [Theory]
    [InlineData(10000, 10000, 1.0f)]   // Full HP
    [InlineData(5000, 10000, 0.5f)]    // Half HP
    [InlineData(2500, 10000, 0.25f)]   // Quarter HP
    [InlineData(0, 10000, 0.0f)]       // Dead
    public void GetPredictedHpPercent_CalculatesCorrectly(uint currentHp, uint maxHp, float expected)
    {
        // Arrange
        const uint entityId = 1;

        // Act
        var result = _service.GetPredictedHpPercent(entityId, currentHp, maxHp);

        // Assert
        Assert.Equal(expected, result, precision: 3);
    }

    [Fact]
    public void GetPredictedHpPercent_ZeroMaxHp_ReturnsZero()
    {
        // Arrange
        const uint entityId = 1;
        const uint currentHp = 5000;
        const uint maxHp = 0;

        // Act
        var result = _service.GetPredictedHpPercent(entityId, currentHp, maxHp);

        // Assert
        Assert.Equal(0f, result);
    }

    #endregion

    #region Pending Heal Registration

    [Fact]
    public void RegisterPendingHeal_StoresCorrectly()
    {
        // Arrange
        const uint entityId = 1;
        const int healAmount = 3000;

        // Act
        _service.RegisterPendingHeal(entityId, healAmount);

        // Assert
        Assert.True(_service.HasPendingHeals);
        Assert.Equal(healAmount, _service.GetPendingHealAmount(entityId));
    }

    [Fact]
    public void RegisterPendingHeal_MultipleDifferentTargets_AccumulatesSeparately()
    {
        // Arrange
        const uint entityId1 = 1;
        const uint entityId2 = 2;

        // Act - register two different targets
        _service.RegisterPendingHeal(entityId1, 1000);
        _service.RegisterPendingHeal(entityId2, 2000);

        // Assert - both heals should be tracked independently
        Assert.Equal(1000, _service.GetPendingHealAmount(entityId1));
        Assert.Equal(2000, _service.GetPendingHealAmount(entityId2));
    }

    [Fact]
    public void RegisterPendingHeal_SameTarget_AccumulatesHeals()
    {
        // Arrange
        const uint entityId = 1;

        // Act - register two heals for the same target (simulates GCD + oGCD weaving)
        _service.RegisterPendingHeal(entityId, 1000);
        _service.RegisterPendingHeal(entityId, 2000);

        // Assert - heals should accumulate
        Assert.Equal(3000, _service.GetPendingHealAmount(entityId));
    }

    [Fact]
    public void RegisterPendingAoEHeal_StoresMultipleTargets()
    {
        // Arrange
        var targetIds = new uint[] { 1, 2, 3, 4 };
        const int healAmount = 1500;

        // Act
        _service.RegisterPendingAoEHeal(targetIds, healAmount);

        // Assert
        Assert.True(_service.HasPendingHeals);
        foreach (var targetId in targetIds)
        {
            Assert.Equal(healAmount, _service.GetPendingHealAmount(targetId));
        }
    }

    [Fact]
    public void RegisterPendingAoEHeal_AccumulatesWithPrevious()
    {
        // Arrange - register a single-target heal first
        _service.RegisterPendingHeal(1, 5000);

        // Act - register AoE heal that includes the same target
        _service.RegisterPendingAoEHeal(new uint[] { 1, 2 }, 1000);

        // Assert - heals should accumulate for target 1
        Assert.Equal(6000, _service.GetPendingHealAmount(1));
        Assert.Equal(1000, _service.GetPendingHealAmount(2));
    }

    [Fact]
    public void ClearPendingHeals_RemovesAll()
    {
        // Arrange
        _service.RegisterPendingAoEHeal(new uint[] { 1, 2, 3 }, 1000);
        Assert.True(_service.HasPendingHeals);

        // Act
        _service.ClearPendingHeals();

        // Assert
        Assert.False(_service.HasPendingHeals);
        Assert.Equal(0, _service.GetPendingHealAmount(1));
        Assert.Equal(0, _service.GetPendingHealAmount(2));
        Assert.Equal(0, _service.GetPendingHealAmount(3));
    }

    [Fact]
    public void ClearPendingHeals_ByTargetId_OnlyClearsSpecificTarget()
    {
        // Arrange - register heals for multiple targets
        _service.RegisterPendingHeal(1, 1000);
        _service.RegisterPendingHeal(2, 2000);
        _service.RegisterPendingHeal(3, 3000);
        Assert.True(_service.HasPendingHeals);

        // Act - clear only target 2
        _service.ClearPendingHeals(2);

        // Assert - only target 2 should be cleared
        Assert.True(_service.HasPendingHeals);
        Assert.Equal(1000, _service.GetPendingHealAmount(1));
        Assert.Equal(0, _service.GetPendingHealAmount(2));
        Assert.Equal(3000, _service.GetPendingHealAmount(3));
    }

    [Fact]
    public void GetPendingHealAmount_MissingTarget_ReturnsZero()
    {
        // Arrange - no pending heals registered

        // Act
        var result = _service.GetPendingHealAmount(999);

        // Assert
        Assert.Equal(0, result);
    }

    #endregion

    #region State Queries

    [Fact]
    public void HasPendingHeals_WithHeals_ReturnsTrue()
    {
        // Arrange
        _service.RegisterPendingHeal(1, 1000);

        // Act & Assert
        Assert.True(_service.HasPendingHeals);
    }

    [Fact]
    public void HasPendingHeals_Empty_ReturnsFalse()
    {
        // Act & Assert
        Assert.False(_service.HasPendingHeals);
    }

    [Fact]
    public void GetAllPendingHeals_ReturnsSnapshot()
    {
        // Arrange
        _service.RegisterPendingAoEHeal(new uint[] { 1, 2, 3 }, 1000);

        // Act
        var result = _service.GetAllPendingHeals();

        // Assert
        Assert.Equal(3, result.Count);
        Assert.Equal(1000, result[1]);
        Assert.Equal(1000, result[2]);
        Assert.Equal(1000, result[3]);
    }

    [Fact]
    public void GetAllPendingHeals_WithAccumulatedHeals_ReturnsSums()
    {
        // Arrange - register multiple heals for same targets
        _service.RegisterPendingHeal(1, 1000);
        _service.RegisterPendingHeal(1, 2000);
        _service.RegisterPendingHeal(2, 500);

        // Act
        var result = _service.GetAllPendingHeals();

        // Assert - should return sum for target 1
        Assert.Equal(2, result.Count);
        Assert.Equal(3000, result[1]);
        Assert.Equal(500, result[2]);
    }

    #endregion

    #region Timeout Logic

    [Fact]
    public void GetPredictedHp_WithinTimeout_RetainsPending()
    {
        // Arrange
        const uint entityId = 1;
        const uint currentHp = 5000;
        const uint maxHp = 10000;
        const int healAmount = 2000;

        _service.RegisterPendingHeal(entityId, healAmount);

        // Act - query immediately (within timeout)
        var result = _service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert
        Assert.Equal(currentHp + (uint)healAmount, result);
        Assert.True(_service.HasPendingHeals);
    }

    [Fact]
    public void GetPredictedHp_AfterTimeout_IgnoresPending()
    {
        // Arrange
        const uint entityId = 1;
        const uint currentHp = 5000;
        const uint maxHp = 10000;
        const int healAmount = 2000;

        _service.RegisterPendingHeal(entityId, healAmount);

        // Wait for timeout (3 seconds + buffer)
        Thread.Sleep(3100);

        // Act - query after timeout
        var result = _service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert - pending heal should be ignored (not added to predicted HP)
        Assert.Equal(currentHp, result);
    }

    [Fact]
    public void GetPredictedHp_WithMultiplePendingHeals_SumsAll()
    {
        // Arrange - register multiple heals for same target (simulates GCD + oGCD weaving)
        const uint entityId = 1;
        const uint currentHp = 3000;
        const uint maxHp = 10000;

        _service.RegisterPendingHeal(entityId, 2000); // GCD heal
        _service.RegisterPendingHeal(entityId, 1500); // oGCD heal

        // Act
        var result = _service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert - both heals should be summed
        Assert.Equal(6500u, result); // 3000 + 2000 + 1500
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void GetPredictedHp_EntityIdZero_HandlesGracefully()
    {
        // Arrange
        const uint entityId = 0;
        const uint currentHp = 5000;
        const uint maxHp = 10000;

        _service.RegisterPendingHeal(entityId, 1000);

        // Act
        var result = _service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert - should work normally
        Assert.Equal(6000u, result);
    }

    [Fact]
    public void GetPredictedHp_LargeHealAmount_ClampsCorrectly()
    {
        // Arrange
        const uint entityId = 1;
        const uint currentHp = 5000;
        const uint maxHp = 10000;
        const int healAmount = 100000; // Large but won't overflow

        _service.RegisterPendingHeal(entityId, healAmount);

        // Act
        var result = _service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert - should clamp to maxHp
        Assert.Equal(maxHp, result);
    }

    [Fact]
    public void RegisterPendingAoEHeal_EmptyList_NoError()
    {
        // Arrange
        var emptyList = Array.Empty<uint>();

        // Act
        _service.RegisterPendingAoEHeal(emptyList, 1000);

        // Assert - should not throw, no pending heals
        Assert.False(_service.HasPendingHeals);
    }

    [Fact]
    public void GetPredictedHp_UsesShadowHp_NotCurrentHp()
    {
        // Arrange - mock returns different shadow HP
        const uint entityId = 1;
        const uint currentHp = 5000;
        const uint shadowHp = 4000; // Lower than current (took damage)
        const uint maxHp = 10000;

        var mockWithShadow = MockBuilders.CreateMockCombatEventService(
            (id, fallback) => id == entityId ? shadowHp : fallback);
        var configNoVariance = MockBuilders.CreateDefaultConfiguration();
        configNoVariance.Healing.EnableCritVarianceReduction = false;
        using var serviceWithShadow = new HpPredictionService(mockWithShadow.Object, configNoVariance);

        // Act
        var result = serviceWithShadow.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert - should use shadow HP (4000) not current (5000)
        Assert.Equal(shadowHp, result);
    }

    [Fact]
    public void GetPredictedHp_ShadowHpPlusPending_ClampsCorrectly()
    {
        // Arrange
        const uint entityId = 1;
        const uint currentHp = 9000;
        const uint shadowHp = 8500;
        const uint maxHp = 10000;
        const int healAmount = 3000;

        var mockWithShadow = MockBuilders.CreateMockCombatEventService(
            (id, fallback) => id == entityId ? shadowHp : fallback);
        var configNoVariance = MockBuilders.CreateDefaultConfiguration();
        configNoVariance.Healing.EnableCritVarianceReduction = false;
        using var serviceWithShadow = new HpPredictionService(mockWithShadow.Object, configNoVariance);

        serviceWithShadow.RegisterPendingHeal(entityId, healAmount);

        // Act - 8500 + 3000 = 11500, should clamp to 10000
        var result = serviceWithShadow.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert
        Assert.Equal(maxHp, result);
    }

    #endregion

    #region Event Subscription

    [Fact]
    public void OnLocalPlayerHealLanded_ClearsPendingHealsForTarget()
    {
        // Arrange - register heals for multiple targets
        _service.RegisterPendingHeal(1, 1000);
        _service.RegisterPendingHeal(2, 2000);
        Assert.True(_service.HasPendingHeals);

        // Act - raise the event for target 1 only
        _mockCombatEvent.Raise(x => x.OnLocalPlayerHealLanded += null, 1u);

        // Assert - only target 1's heals should be cleared
        Assert.True(_service.HasPendingHeals); // Still has heals for target 2
        Assert.Equal(0, _service.GetPendingHealAmount(1));
        Assert.Equal(2000, _service.GetPendingHealAmount(2));
    }

    [Fact]
    public void OnLocalPlayerHealLanded_ClearsAllHealsForTarget()
    {
        // Arrange - register multiple heals for the same target
        _service.RegisterPendingHeal(1, 1000);
        _service.RegisterPendingHeal(1, 2000);
        _service.RegisterPendingHeal(1, 500);
        Assert.Equal(3500, _service.GetPendingHealAmount(1));

        // Act - raise the event for target 1
        _mockCombatEvent.Raise(x => x.OnLocalPlayerHealLanded += null, 1u);

        // Assert - all heals for target 1 should be cleared
        Assert.Equal(0, _service.GetPendingHealAmount(1));
    }

    #endregion

    #region Crit Variance Reduction

    [Fact]
    public void GetPredictedHp_WithCritVarianceEnabled_ReducesPendingHeals()
    {
        // Arrange
        var mockCombatEvent = MockBuilders.CreateMockCombatEventService();
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.EnableCritVarianceReduction = true;
        config.Healing.CritVarianceReduction = 0.10f; // 10% reduction

        using var service = new HpPredictionService(mockCombatEvent.Object, config);

        const uint entityId = 1;
        const uint currentHp = 5000;
        const uint maxHp = 10000;
        const int healAmount = 2000;

        service.RegisterPendingHeal(entityId, healAmount);

        // Act
        var result = service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert - heal should be reduced by 10%: 5000 + (2000 * 0.9) = 6800
        Assert.Equal(6800u, result);
    }

    [Fact]
    public void GetPredictedHp_WithCritVarianceDisabled_NoReduction()
    {
        // Arrange
        var mockCombatEvent = MockBuilders.CreateMockCombatEventService();
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.EnableCritVarianceReduction = false;
        config.Healing.CritVarianceReduction = 0.10f; // 10% reduction (should be ignored)

        using var service = new HpPredictionService(mockCombatEvent.Object, config);

        const uint entityId = 1;
        const uint currentHp = 5000;
        const uint maxHp = 10000;
        const int healAmount = 2000;

        service.RegisterPendingHeal(entityId, healAmount);

        // Act
        var result = service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert - no reduction: 5000 + 2000 = 7000
        Assert.Equal(7000u, result);
    }

    [Fact]
    public void GetPredictedHp_WithCritVariance_NoPendingHeals_NoChange()
    {
        // Arrange
        var mockCombatEvent = MockBuilders.CreateMockCombatEventService();
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.EnableCritVarianceReduction = true;
        config.Healing.CritVarianceReduction = 0.10f;

        using var service = new HpPredictionService(mockCombatEvent.Object, config);

        const uint entityId = 1;
        const uint currentHp = 5000;
        const uint maxHp = 10000;

        // No pending heals registered

        // Act
        var result = service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert - should just return current HP
        Assert.Equal(currentHp, result);
    }

    [Fact]
    public void GetPredictedHp_WithCritVariance_MultiplePendingHeals_ReducesSum()
    {
        // Arrange
        var mockCombatEvent = MockBuilders.CreateMockCombatEventService();
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.EnableCritVarianceReduction = true;
        config.Healing.CritVarianceReduction = 0.08f; // 8% reduction (default)

        using var service = new HpPredictionService(mockCombatEvent.Object, config);

        const uint entityId = 1;
        const uint currentHp = 3000;
        const uint maxHp = 10000;

        service.RegisterPendingHeal(entityId, 2000); // GCD heal
        service.RegisterPendingHeal(entityId, 1000); // oGCD heal
        // Total pending: 3000, after 8% reduction: 2760

        // Act
        var result = service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert - 3000 + (3000 * 0.92) = 3000 + 2760 = 5760
        Assert.Equal(5760u, result);
    }

    [Theory]
    [InlineData(0.0f, 7000u)]  // No reduction
    [InlineData(0.08f, 6840u)] // 8% reduction (default)
    [InlineData(0.15f, 6700u)] // 15% reduction
    [InlineData(0.25f, 6500u)] // 25% reduction (max)
    public void GetPredictedHp_WithCritVariance_DifferentRates_ReducesCorrectly(float varianceRate, uint expected)
    {
        // Arrange
        var mockCombatEvent = MockBuilders.CreateMockCombatEventService();
        var config = MockBuilders.CreateDefaultConfiguration();
        config.Healing.EnableCritVarianceReduction = true;
        config.Healing.CritVarianceReduction = varianceRate;

        using var service = new HpPredictionService(mockCombatEvent.Object, config);

        const uint entityId = 1;
        const uint currentHp = 5000;
        const uint maxHp = 10000;
        const int healAmount = 2000;

        service.RegisterPendingHeal(entityId, healAmount);

        // Act
        var result = service.GetPredictedHp(entityId, currentHp, maxHp);

        // Assert
        Assert.Equal(expected, result);
    }

    #endregion
}
