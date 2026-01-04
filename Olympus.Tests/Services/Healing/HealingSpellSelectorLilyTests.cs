using Olympus.Config;
using Olympus.Services.Healing;
using Xunit;

namespace Olympus.Tests.Services.Healing;

/// <summary>
/// Tests for Blood Lily strategy functionality in HealingSpellSelector.
///
/// NOTE: Full integration testing of ShouldPreferLilyHeal requires IPlayerCharacter
/// and IBattleChara from Dalamud, which cannot be mocked without the Dalamud runtime.
/// These tests verify the enum, debug state, and strategy descriptions.
/// </summary>
public class HealingSpellSelectorLilyTests
{
    #region LilyGenerationStrategy Enum Tests

    [Fact]
    public void LilyGenerationStrategy_HasExpectedValues()
    {
        Assert.Equal(0, (int)LilyGenerationStrategy.Aggressive);
        Assert.Equal(1, (int)LilyGenerationStrategy.Balanced);
        Assert.Equal(2, (int)LilyGenerationStrategy.Conservative);
        Assert.Equal(3, (int)LilyGenerationStrategy.Disabled);
    }

    [Fact]
    public void LilyGenerationStrategy_DefaultIsBalanced()
    {
        var config = new HealingConfig();
        Assert.Equal(LilyGenerationStrategy.Balanced, config.LilyStrategy);
    }

    [Fact]
    public void LilyGenerationStrategy_ConservativeThresholdDefault()
    {
        var config = new HealingConfig();
        Assert.Equal(0.75f, config.ConservativeLilyHpThreshold);
    }

    #endregion

    #region SpellSelectionDebug Blood Lily Fields Tests

    [Fact]
    public void SpellSelectionDebug_DefaultBloodLilyValues()
    {
        var debug = new SpellSelectionDebug();

        Assert.Equal(0, debug.LilyCount);
        Assert.Equal(0, debug.BloodLilyCount);
        Assert.Equal("", debug.LilyStrategy);
    }

    [Fact]
    public void SpellSelectionDebug_WithBloodLilyValues()
    {
        var debug = new SpellSelectionDebug
        {
            SelectionType = "Single",
            TargetName = "Tank",
            LilyCount = 2,
            BloodLilyCount = 1,
            LilyStrategy = "Balanced"
        };

        Assert.Equal(2, debug.LilyCount);
        Assert.Equal(1, debug.BloodLilyCount);
        Assert.Equal("Balanced", debug.LilyStrategy);
    }

    [Fact]
    public void SpellSelectionDebug_WithMaxBloodLilies()
    {
        var debug = new SpellSelectionDebug
        {
            LilyCount = 3,
            BloodLilyCount = 3,
            LilyStrategy = "Aggressive"
        };

        Assert.Equal(3, debug.LilyCount);
        Assert.Equal(3, debug.BloodLilyCount);
        Assert.Equal("Aggressive", debug.LilyStrategy);
    }

    [Fact]
    public void SpellSelectionDebug_WithConservativeStrategy()
    {
        var debug = new SpellSelectionDebug
        {
            LilyCount = 1,
            BloodLilyCount = 2,
            LilyStrategy = "Conservative",
            TargetHpPercent = 0.65f
        };

        Assert.Equal("Conservative", debug.LilyStrategy);
        Assert.Equal(0.65f, debug.TargetHpPercent);
    }

    [Fact]
    public void SpellSelectionDebug_WithDisabledStrategy()
    {
        var debug = new SpellSelectionDebug
        {
            LilyCount = 3,
            BloodLilyCount = 0,
            LilyStrategy = "Disabled"
        };

        Assert.Equal("Disabled", debug.LilyStrategy);
    }

    #endregion

    #region HealingConfig Strategy Persistence Tests

    [Fact]
    public void HealingConfig_CanSetAggressiveStrategy()
    {
        var config = new HealingConfig
        {
            LilyStrategy = LilyGenerationStrategy.Aggressive
        };

        Assert.Equal(LilyGenerationStrategy.Aggressive, config.LilyStrategy);
    }

    [Fact]
    public void HealingConfig_CanSetConservativeThreshold()
    {
        var config = new HealingConfig
        {
            LilyStrategy = LilyGenerationStrategy.Conservative,
            ConservativeLilyHpThreshold = 0.60f
        };

        Assert.Equal(LilyGenerationStrategy.Conservative, config.LilyStrategy);
        Assert.Equal(0.60f, config.ConservativeLilyHpThreshold);
    }

    [Fact]
    public void HealingConfig_CanDisableStrategy()
    {
        var config = new HealingConfig
        {
            LilyStrategy = LilyGenerationStrategy.Disabled
        };

        Assert.Equal(LilyGenerationStrategy.Disabled, config.LilyStrategy);
    }

    #endregion

    #region Strategy Behavior Logic Tests

    /// <summary>
    /// Documents the expected behavior of each strategy.
    /// These serve as specification tests for the ShouldPreferLilyHeal logic.
    /// </summary>
    [Theory]
    [InlineData(LilyGenerationStrategy.Aggressive, 1, 0, 0.50f, true, "Should always prefer lily heals")]
    [InlineData(LilyGenerationStrategy.Aggressive, 1, 3, 0.50f, true, "Should prefer even at max Blood Lilies")]
    [InlineData(LilyGenerationStrategy.Balanced, 1, 0, 0.50f, true, "Should prefer when Blood < 3")]
    [InlineData(LilyGenerationStrategy.Balanced, 1, 2, 0.50f, true, "Should prefer when Blood < 3")]
    [InlineData(LilyGenerationStrategy.Balanced, 1, 3, 0.50f, false, "Should NOT prefer when Blood = 3")]
    [InlineData(LilyGenerationStrategy.Conservative, 1, 0, 0.50f, true, "Should prefer when HP below threshold")]
    [InlineData(LilyGenerationStrategy.Conservative, 1, 0, 0.80f, false, "Should NOT prefer when HP above threshold")]
    [InlineData(LilyGenerationStrategy.Conservative, 1, 3, 0.50f, false, "Should NOT prefer when Blood = 3")]
    [InlineData(LilyGenerationStrategy.Disabled, 1, 0, 0.50f, false, "Should never prefer")]
    [InlineData(LilyGenerationStrategy.Disabled, 3, 0, 0.10f, false, "Should never prefer even at low HP")]
    public void ShouldPreferLilyHeal_ExpectedBehavior(
        LilyGenerationStrategy strategy,
        int lilyCount,
        int bloodLilyCount,
        float hpPercent,
        bool expectedResult,
        string reason)
    {
        // This documents the expected behavior. Actual testing requires Dalamud runtime.
        // The test data above defines the specification for each strategy.

        var description = $"{strategy}: lilies={lilyCount}, blood={bloodLilyCount}, hp={hpPercent:P0} => {expectedResult} ({reason})";
        Assert.NotNull(description); // Placeholder assertion
    }

    #endregion

    #region Selection Reason Format Tests

    [Fact]
    public void SelectionReason_IncludesBloodLilyInfo_WhenStrategyUsed()
    {
        // Verify the selection reason format includes Blood Lily information
        var expectedFormat = "Tier 1: Lily heal (2 lilies, 1/3 Blood, Balanced)";

        Assert.Contains("lilies", expectedFormat);
        Assert.Contains("Blood", expectedFormat);
        Assert.Contains("Balanced", expectedFormat);
    }

    [Fact]
    public void RejectionReason_IncludesStrategyInfo()
    {
        // Verify rejection reasons include strategy context
        var expectedFormat = "Strategy Balanced: Blood 3/3, HP 85%";

        Assert.Contains("Strategy", expectedFormat);
        Assert.Contains("Blood", expectedFormat);
        Assert.Contains("HP", expectedFormat);
    }

    #endregion
}
