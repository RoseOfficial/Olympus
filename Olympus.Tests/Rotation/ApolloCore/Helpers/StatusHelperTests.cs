using Olympus.Rotation.ApolloCore.Helpers;

namespace Olympus.Tests.Rotation.ApolloCore.Helpers;

/// <summary>
/// Tests for StatusHelper utility methods.
/// Note: Methods requiring IBattleChara or job gauge access cannot be unit tested
/// without game state mocking.
/// </summary>
public class StatusHelperTests
{
    #region GetDotStatusId Tests

    [Theory]
    [InlineData(1, 143u)]   // Level 1 = Aero
    [InlineData(45, 143u)]  // Level 45 = Aero (just below Aero II)
    [InlineData(46, 144u)]  // Level 46 = Aero II
    [InlineData(71, 144u)]  // Level 71 = Aero II (just below Dia)
    [InlineData(72, 1871u)] // Level 72 = Dia
    [InlineData(90, 1871u)] // Level 90 = Dia
    [InlineData(100, 1871u)] // Level 100 = Dia
    public void GetDotStatusId_ReturnsCorrectStatusForLevel(byte level, uint expectedStatusId)
    {
        var result = StatusHelper.GetDotStatusId(level);
        Assert.Equal(expectedStatusId, result);
    }

    [Fact]
    public void GetDotStatusId_LevelProgression_UpgradesCorrectly()
    {
        // Verify the progression: Aero → Aero II → Dia
        var aeroLevel = StatusHelper.GetDotStatusId(1);
        var aeroIILevel = StatusHelper.GetDotStatusId(46);
        var diaLevel = StatusHelper.GetDotStatusId(72);

        Assert.NotEqual(aeroLevel, aeroIILevel);
        Assert.NotEqual(aeroIILevel, diaLevel);
        Assert.NotEqual(aeroLevel, diaLevel);
    }

    #endregion

    #region Status ID Constants Tests

    [Fact]
    public void StatusIds_DoTStatuses_AreDistinct()
    {
        // Verify DoT status IDs are unique
        var dotIds = new[]
        {
            StatusHelper.StatusIds.Aero,
            StatusHelper.StatusIds.AeroII,
            StatusHelper.StatusIds.Dia
        };

        Assert.Equal(dotIds.Length, dotIds.Distinct().Count());
    }

    [Fact]
    public void StatusIds_BuffStatuses_AreDistinct()
    {
        // Verify buff status IDs are unique
        var buffIds = new[]
        {
            StatusHelper.StatusIds.Swiftcast,
            StatusHelper.StatusIds.ThinAir,
            StatusHelper.StatusIds.Freecure,
            StatusHelper.StatusIds.SacredSight,
            StatusHelper.StatusIds.Surecast
        };

        Assert.Equal(buffIds.Length, buffIds.Distinct().Count());
    }

    [Fact]
    public void StatusIds_DefensiveStatuses_AreDistinct()
    {
        // Verify defensive status IDs are unique
        var defensiveIds = new[]
        {
            StatusHelper.StatusIds.DivineBenison,
            StatusHelper.StatusIds.Aquaveil,
            StatusHelper.StatusIds.Temperance,
            StatusHelper.StatusIds.DivineGrace,
            StatusHelper.StatusIds.PlenaryIndulgence
        };

        Assert.Equal(defensiveIds.Length, defensiveIds.Distinct().Count());
    }

    [Fact]
    public void StatusIds_MedicaRegenStatuses_AreDistinct()
    {
        // Verify Medica regen status IDs are unique
        var medicaIds = new[]
        {
            StatusHelper.StatusIds.MedicaII,
            StatusHelper.StatusIds.MedicaIII
        };

        Assert.Equal(medicaIds.Length, medicaIds.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllStatusIds_AreNonZero()
    {
        // All status IDs should be non-zero (0 typically means no status)
        Assert.NotEqual(0u, StatusHelper.StatusIds.Aero);
        Assert.NotEqual(0u, StatusHelper.StatusIds.AeroII);
        Assert.NotEqual(0u, StatusHelper.StatusIds.Dia);
        Assert.NotEqual(0u, StatusHelper.StatusIds.Regen);
        Assert.NotEqual(0u, StatusHelper.StatusIds.MedicaII);
        Assert.NotEqual(0u, StatusHelper.StatusIds.MedicaIII);
        Assert.NotEqual(0u, StatusHelper.StatusIds.Raise);
        Assert.NotEqual(0u, StatusHelper.StatusIds.Swiftcast);
        Assert.NotEqual(0u, StatusHelper.StatusIds.ThinAir);
        Assert.NotEqual(0u, StatusHelper.StatusIds.Freecure);
        Assert.NotEqual(0u, StatusHelper.StatusIds.SacredSight);
        Assert.NotEqual(0u, StatusHelper.StatusIds.Surecast);
        Assert.NotEqual(0u, StatusHelper.StatusIds.DivineBenison);
        Assert.NotEqual(0u, StatusHelper.StatusIds.Aquaveil);
        Assert.NotEqual(0u, StatusHelper.StatusIds.Temperance);
        Assert.NotEqual(0u, StatusHelper.StatusIds.DivineGrace);
        Assert.NotEqual(0u, StatusHelper.StatusIds.PlenaryIndulgence);
    }

    #endregion

    #region Known Status ID Values Tests

    [Fact]
    public void StatusIds_Aero_MatchesGameData()
    {
        // Aero status ID from game data
        Assert.Equal(143u, StatusHelper.StatusIds.Aero);
    }

    [Fact]
    public void StatusIds_AeroII_MatchesGameData()
    {
        Assert.Equal(144u, StatusHelper.StatusIds.AeroII);
    }

    [Fact]
    public void StatusIds_Dia_MatchesGameData()
    {
        Assert.Equal(1871u, StatusHelper.StatusIds.Dia);
    }

    [Fact]
    public void StatusIds_Regen_MatchesGameData()
    {
        Assert.Equal(158u, StatusHelper.StatusIds.Regen);
    }

    [Fact]
    public void StatusIds_Swiftcast_MatchesGameData()
    {
        Assert.Equal(167u, StatusHelper.StatusIds.Swiftcast);
    }

    [Fact]
    public void StatusIds_ThinAir_MatchesGameData()
    {
        Assert.Equal(1217u, StatusHelper.StatusIds.ThinAir);
    }

    #endregion
}
