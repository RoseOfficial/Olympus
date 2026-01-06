using Olympus.Data;
using Olympus.Services.Healing.Strategies;
using Xunit;

namespace Olympus.Tests.Services.Healing.Strategies;

/// <summary>
/// Tests for HealScoreContext record used in scored heal selection.
/// </summary>
public class HealScoreContextTests
{
    [Fact]
    public void HealScoreContext_RequiredProperties_AreRequired()
    {
        // Verify the required properties exist via reflection
        var type = typeof(HealScoreContext);

        Assert.NotNull(type.GetProperty("Action"));
        Assert.NotNull(type.GetProperty("HealAmount"));
        Assert.NotNull(type.GetProperty("MissingHp"));
        Assert.NotNull(type.GetProperty("HasFreecure"));
        Assert.NotNull(type.GetProperty("IsWeaveWindow"));
        Assert.NotNull(type.GetProperty("LilyCount"));
        Assert.NotNull(type.GetProperty("BloodLilyCount"));
        Assert.NotNull(type.GetProperty("IsInMpConservationMode"));
    }

    [Fact]
    public void HealScoreContext_CanBeCreated_WithValidValues()
    {
        var context = new HealScoreContext
        {
            Action = WHMActions.CureII,
            HealAmount = 5000,
            MissingHp = 4000,
            HasFreecure = true,
            IsWeaveWindow = false,
            LilyCount = 2,
            BloodLilyCount = 1,
            IsInMpConservationMode = false
        };

        Assert.Equal(WHMActions.CureII, context.Action);
        Assert.Equal(5000, context.HealAmount);
        Assert.Equal(4000, context.MissingHp);
        Assert.True(context.HasFreecure);
        Assert.False(context.IsWeaveWindow);
        Assert.Equal(2, context.LilyCount);
        Assert.Equal(1, context.BloodLilyCount);
        Assert.False(context.IsInMpConservationMode);
    }

    [Fact]
    public void HealScoreContext_RecordEquality_WorksCorrectly()
    {
        var context1 = new HealScoreContext
        {
            Action = WHMActions.Cure,
            HealAmount = 3000,
            MissingHp = 2500,
            HasFreecure = false,
            IsWeaveWindow = false,
            LilyCount = 1,
            BloodLilyCount = 0,
            IsInMpConservationMode = false
        };

        var context2 = new HealScoreContext
        {
            Action = WHMActions.Cure,
            HealAmount = 3000,
            MissingHp = 2500,
            HasFreecure = false,
            IsWeaveWindow = false,
            LilyCount = 1,
            BloodLilyCount = 0,
            IsInMpConservationMode = false
        };

        Assert.Equal(context1, context2);
    }

    [Fact]
    public void HealScoreContext_RecordInequality_WorksCorrectly()
    {
        var context1 = new HealScoreContext
        {
            Action = WHMActions.Cure,
            HealAmount = 3000,
            MissingHp = 2500,
            HasFreecure = false,
            IsWeaveWindow = false,
            LilyCount = 1,
            BloodLilyCount = 0,
            IsInMpConservationMode = false
        };

        var context2 = new HealScoreContext
        {
            Action = WHMActions.CureII, // Different action
            HealAmount = 3000,
            MissingHp = 2500,
            HasFreecure = false,
            IsWeaveWindow = false,
            LilyCount = 1,
            BloodLilyCount = 0,
            IsInMpConservationMode = false
        };

        Assert.NotEqual(context1, context2);
    }

    [Fact]
    public void HealScoreContext_WithMutation_CreatesNewRecord()
    {
        var original = new HealScoreContext
        {
            Action = WHMActions.Cure,
            HealAmount = 3000,
            MissingHp = 2500,
            HasFreecure = false,
            IsWeaveWindow = false,
            LilyCount = 1,
            BloodLilyCount = 0,
            IsInMpConservationMode = false
        };

        var modified = original with { HasFreecure = true };

        Assert.False(original.HasFreecure);
        Assert.True(modified.HasFreecure);
        Assert.Equal(original.HealAmount, modified.HealAmount);
    }
}
