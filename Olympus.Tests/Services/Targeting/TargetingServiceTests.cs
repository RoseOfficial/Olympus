using Olympus.Config;
using Olympus.Services.Targeting;
using Xunit;

namespace Olympus.Tests.Services.Targeting;

/// <summary>
/// Tests for TargetingService-related types that don't require Dalamud runtime.
/// Note: Tests requiring TargetingService instantiation are limited because
/// they depend on Dalamud's IObjectTable, IPartyList, and ITargetManager at runtime.
/// </summary>
public sealed class TargetingServiceTests
{
    #region EnemyTargetingStrategy Enum Values

    [Fact]
    public void EnemyTargetingStrategy_HasExpectedValues()
    {
        // Assert - verify enum values exist
        Assert.Equal(0, (int)EnemyTargetingStrategy.LowestHp);
        Assert.Equal(1, (int)EnemyTargetingStrategy.HighestHp);
        Assert.Equal(2, (int)EnemyTargetingStrategy.Nearest);
        Assert.Equal(3, (int)EnemyTargetingStrategy.TankAssist);
        Assert.Equal(4, (int)EnemyTargetingStrategy.CurrentTarget);
        Assert.Equal(5, (int)EnemyTargetingStrategy.FocusTarget);
    }

    [Fact]
    public void EnemyTargetingStrategy_AllValuesAreDefined()
    {
        // Assert - all 6 strategies should be defined
        var values = Enum.GetValues<EnemyTargetingStrategy>();
        Assert.Equal(6, values.Length);
    }

    [Theory]
    [InlineData(EnemyTargetingStrategy.LowestHp)]
    [InlineData(EnemyTargetingStrategy.HighestHp)]
    [InlineData(EnemyTargetingStrategy.Nearest)]
    [InlineData(EnemyTargetingStrategy.TankAssist)]
    [InlineData(EnemyTargetingStrategy.CurrentTarget)]
    [InlineData(EnemyTargetingStrategy.FocusTarget)]
    public void EnemyTargetingStrategy_AllValues_AreCastable(EnemyTargetingStrategy strategy)
    {
        // Assert - each strategy should be a valid enum value
        Assert.True(Enum.IsDefined(strategy));
    }

    #endregion

    #region TargetingConfig Default Values

    [Fact]
    public void TargetingConfig_DefaultStrategy_IsLowestHp()
    {
        // Arrange & Act
        var config = new TargetingConfig();

        // Assert
        Assert.Equal(EnemyTargetingStrategy.LowestHp, config.EnemyStrategy);
    }

    [Fact]
    public void TargetingConfig_DefaultTankAssistFallback_IsTrue()
    {
        // Arrange & Act
        var config = new TargetingConfig();

        // Assert
        Assert.True(config.UseTankAssistFallback);
    }

    [Fact]
    public void TargetingConfig_DefaultCacheTtl_Is100Ms()
    {
        // Arrange & Act
        var config = new TargetingConfig();

        // Assert
        Assert.Equal(100, config.TargetCacheTtlMs);
    }

    #endregion

    #region Strategy Description Coverage

    [Theory]
    [InlineData(EnemyTargetingStrategy.LowestHp, "lowest")]
    [InlineData(EnemyTargetingStrategy.HighestHp, "highest")]
    [InlineData(EnemyTargetingStrategy.Nearest, "nearest")]
    [InlineData(EnemyTargetingStrategy.TankAssist, "tank")]
    [InlineData(EnemyTargetingStrategy.CurrentTarget, "current")]
    [InlineData(EnemyTargetingStrategy.FocusTarget, "focus")]
    public void EnemyTargetingStrategy_Names_AreDescriptive(EnemyTargetingStrategy strategy, string expectedSubstring)
    {
        // Assert - enum name should contain descriptive text
        var name = strategy.ToString().ToLowerInvariant();
        Assert.Contains(expectedSubstring, name);
    }

    #endregion

    #region Fallback Strategy Tests

    [Fact]
    public void TargetingConfig_TankAssistWithFallback_DefaultConfiguration()
    {
        // Arrange
        var config = new TargetingConfig
        {
            EnemyStrategy = EnemyTargetingStrategy.TankAssist,
            UseTankAssistFallback = true
        };

        // Assert - configuration should allow fallback to LowestHp
        Assert.Equal(EnemyTargetingStrategy.TankAssist, config.EnemyStrategy);
        Assert.True(config.UseTankAssistFallback);
    }

    [Fact]
    public void TargetingConfig_TankAssistWithoutFallback_Configuration()
    {
        // Arrange
        var config = new TargetingConfig
        {
            EnemyStrategy = EnemyTargetingStrategy.TankAssist,
            UseTankAssistFallback = false
        };

        // Assert - configuration should not allow fallback
        Assert.Equal(EnemyTargetingStrategy.TankAssist, config.EnemyStrategy);
        Assert.False(config.UseTankAssistFallback);
    }

    #endregion

    #region Cache Configuration Tests

    [Theory]
    [InlineData(50)]
    [InlineData(100)]
    [InlineData(200)]
    [InlineData(500)]
    public void TargetingConfig_CacheTtl_AcceptsVariousValues(int ttlMs)
    {
        // Arrange & Act
        var config = new TargetingConfig { TargetCacheTtlMs = ttlMs };

        // Assert
        Assert.Equal(ttlMs, config.TargetCacheTtlMs);
    }

    [Fact]
    public void TargetingConfig_CacheTtl_ZeroDisablesCache()
    {
        // Arrange & Act
        var config = new TargetingConfig { TargetCacheTtlMs = 0 };

        // Assert - 0 TTL effectively disables caching
        Assert.Equal(0, config.TargetCacheTtlMs);
    }

    #endregion

    #region SelectMainTankCandidateIndex -- MT-by-proxy heuristic

    [Fact]
    public void SelectMainTankCandidateIndex_EmptyArray_ReturnsMinusOne()
    {
        var result = TargetingService.SelectMainTankCandidateIndex([]);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void SelectMainTankCandidateIndex_AllNull_ReturnsMinusOne()
    {
        var result = TargetingService.SelectMainTankCandidateIndex([null, null]);
        Assert.Equal(-1, result);
    }

    [Fact]
    public void SelectMainTankCandidateIndex_SingleEntry_ReturnsZero()
    {
        var result = TargetingService.SelectMainTankCandidateIndex([500_000u]);
        Assert.Equal(0, result);
    }

    [Theory]
    [InlineData(1_000_000u, 500_000u, 0)]   // tank0 holds the boss (higher MaxHp) -> index 0
    [InlineData(500_000u, 1_000_000u, 1)]   // tank1 holds the boss -> index 1
    [InlineData(1_000_000u, 1_000_000u, 0)] // equal MaxHp -> first wins (stable)
    public void SelectMainTankCandidateIndex_TwoTanks_PrefersBossTarget(
        uint tank0MaxHp, uint tank1MaxHp, int expectedIndex)
    {
        var result = TargetingService.SelectMainTankCandidateIndex([tank0MaxHp, tank1MaxHp]);
        Assert.Equal(expectedIndex, result);
    }

    [Fact]
    public void SelectMainTankCandidateIndex_FirstTankNoTarget_ReturnsSecondIndex()
    {
        // MT has not yet locked on; OT is holding something -- follow the OT for now
        var result = TargetingService.SelectMainTankCandidateIndex([null, 800_000u]);
        Assert.Equal(1, result);
    }

    [Fact]
    public void SelectMainTankCandidateIndex_SecondTankNoTarget_ReturnsFirstIndex()
    {
        // MT holds boss, OT has no target
        var result = TargetingService.SelectMainTankCandidateIndex([1_200_000u, null]);
        Assert.Equal(0, result);
    }

    [Fact]
    public void SelectMainTankCandidateIndex_ThreeTanks_ReturnsIndexOfHighest()
    {
        // Three tank players (unusual but possible in alliance); boss is the highest MaxHp
        var result = TargetingService.SelectMainTankCandidateIndex([300_000u, 1_500_000u, 400_000u]);
        Assert.Equal(1, result);
    }

    #endregion
}
