using Olympus.Services.Healing.Strategies;
using Xunit;

namespace Olympus.Tests.Services.Healing.Strategies;

/// <summary>
/// Tests for TieredHealSelectionStrategy.
/// Verifies the strategy name and basic structure.
/// Full integration testing requires Dalamud runtime.
/// </summary>
public class TieredHealSelectionStrategyTests
{
    [Fact]
    public void TieredStrategy_StrategyName_ReturnsTierBased()
    {
        var strategy = new TieredHealSelectionStrategy();

        Assert.Equal("Tier-Based", strategy.StrategyName);
    }

    [Fact]
    public void TieredStrategy_ImplementsInterface()
    {
        var strategy = new TieredHealSelectionStrategy();

        Assert.IsAssignableFrom<IHealSelectionStrategy>(strategy);
    }
}
