using Olympus.Services.Healing.Strategies;
using Xunit;

namespace Olympus.Tests.Services.Healing.Strategies;

/// <summary>
/// Tests for ScoredHealSelectionStrategy.
/// Verifies the strategy name and basic structure.
/// Full integration testing requires Dalamud runtime.
/// </summary>
public class ScoredHealSelectionStrategyTests
{
    [Fact]
    public void ScoredStrategy_StrategyName_ReturnsScored()
    {
        var strategy = new ScoredHealSelectionStrategy();

        Assert.Equal("Scored", strategy.StrategyName);
    }

    [Fact]
    public void ScoredStrategy_ImplementsInterface()
    {
        var strategy = new ScoredHealSelectionStrategy();

        Assert.IsAssignableFrom<IHealSelectionStrategy>(strategy);
    }
}
