using Olympus.Data;
using Olympus.Services.Action;
using Xunit;

namespace Olympus.Tests.Services.Action;

/// <summary>
/// Tests for ActionService-related types that don't require Dalamud runtime.
/// Note: Tests requiring ActionService instantiation are skipped because
/// ActionTracker depends on Dalamud's IDataManager at runtime.
/// </summary>
public sealed class ActionServiceTests
{
    #region GcdState Enum Values

    [Fact]
    public void GcdState_HasExpectedValues()
    {
        // Assert - verify enum values exist
        Assert.Equal(0, (int)GcdState.Ready);
        Assert.Equal(1, (int)GcdState.Rolling);
        Assert.Equal(2, (int)GcdState.WeaveWindow);
        Assert.Equal(3, (int)GcdState.Casting);
        Assert.Equal(4, (int)GcdState.AnimationLock);
    }

    [Fact]
    public void GcdState_AllValuesAreDefined()
    {
        // Assert - all 5 states should be defined
        var values = Enum.GetValues<GcdState>();
        Assert.Equal(5, values.Length);
    }

    #endregion

    #region Action Definition Properties

    [Fact]
    public void ActionDefinition_Cure_IsGCD()
    {
        // Assert
        Assert.True(WHMActions.Cure.IsGCD);
        Assert.False(WHMActions.Cure.IsOGCD);
    }

    [Fact]
    public void ActionDefinition_Benediction_IsOGCD()
    {
        // Assert
        Assert.True(WHMActions.Benediction.IsOGCD);
        Assert.False(WHMActions.Benediction.IsGCD);
    }

    [Fact]
    public void ActionDefinition_CureII_HasCorrectProperties()
    {
        // Assert
        var cureII = WHMActions.CureII;
        Assert.Equal(135u, cureII.ActionId);
        Assert.Equal("Cure II", cureII.Name);
        Assert.Equal(30, cureII.MinLevel);
        Assert.Equal(800, cureII.HealPotency);
        Assert.True(cureII.IsGCD);
    }

    [Fact]
    public void ActionDefinition_Tetragrammaton_HasCorrectProperties()
    {
        // Assert
        var tetra = WHMActions.Tetragrammaton;
        Assert.Equal(3570u, tetra.ActionId);
        Assert.Equal("Tetragrammaton", tetra.Name);
        Assert.Equal(60, tetra.MinLevel);
        Assert.Equal(700, tetra.HealPotency);
        Assert.True(tetra.IsOGCD);
    }

    #endregion
}
