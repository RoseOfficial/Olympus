using Olympus.Data;
using Olympus.Rotation.ThanatosCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ThanatosCore.Helpers;

/// <summary>
/// Tests for ThanatosStatusHelper utility methods.
/// ThanatosStatusHelper is sealed — status behavior is controlled via StatusList on mock characters.
/// Null-guard tests exercise the BaseStatusHelper null-safe path.
/// </summary>
public class StatusHelperTests
{
    private readonly ThanatosStatusHelper _helper = new();

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_SoulReaverEnhancedBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            RPRActions.StatusIds.SoulReaver,
            RPRActions.StatusIds.EnhancedGibbet,
            RPRActions.StatusIds.EnhancedGallows,
            RPRActions.StatusIds.EnhancedVoidReaping,
            RPRActions.StatusIds.EnhancedCrossReaping,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_EnshroudAndPartyBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            RPRActions.StatusIds.Enshrouded,
            RPRActions.StatusIds.ArcaneCircle,
            RPRActions.StatusIds.ImmortalSacrifice,
            RPRActions.StatusIds.BloodsownCircle,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_ProcStatuses_AreDistinct()
    {
        var ids = new uint[]
        {
            RPRActions.StatusIds.Soulsow,
            RPRActions.StatusIds.PerfectioParata,
            RPRActions.StatusIds.Oblatio,
            RPRActions.StatusIds.IdealHost,
            RPRActions.StatusIds.EnhancedHarpe,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, RPRActions.StatusIds.DeathsDesign);
        Assert.NotEqual(0u, RPRActions.StatusIds.SoulReaver);
        Assert.NotEqual(0u, RPRActions.StatusIds.EnhancedGibbet);
        Assert.NotEqual(0u, RPRActions.StatusIds.EnhancedGallows);
        Assert.NotEqual(0u, RPRActions.StatusIds.EnhancedVoidReaping);
        Assert.NotEqual(0u, RPRActions.StatusIds.EnhancedCrossReaping);
        Assert.NotEqual(0u, RPRActions.StatusIds.Enshrouded);
        Assert.NotEqual(0u, RPRActions.StatusIds.ArcaneCircle);
        Assert.NotEqual(0u, RPRActions.StatusIds.ImmortalSacrifice);
        Assert.NotEqual(0u, RPRActions.StatusIds.BloodsownCircle);
        Assert.NotEqual(0u, RPRActions.StatusIds.Soulsow);
        Assert.NotEqual(0u, RPRActions.StatusIds.PerfectioParata);
        Assert.NotEqual(0u, RPRActions.StatusIds.Oblatio);
        Assert.NotEqual(0u, RPRActions.StatusIds.IdealHost);
        Assert.NotEqual(0u, RPRActions.StatusIds.EnhancedHarpe);
        Assert.NotEqual(0u, RPRActions.StatusIds.TrueNorth);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_DeathsDesign_MatchesGameData()
    {
        Assert.Equal(2586u, RPRActions.StatusIds.DeathsDesign);
    }

    [Fact]
    public void StatusId_SoulReaver_MatchesGameData()
    {
        Assert.Equal(2587u, RPRActions.StatusIds.SoulReaver);
    }

    [Fact]
    public void StatusId_EnhancedGibbet_MatchesGameData()
    {
        Assert.Equal(2588u, RPRActions.StatusIds.EnhancedGibbet);
    }

    [Fact]
    public void StatusId_EnhancedGallows_MatchesGameData()
    {
        Assert.Equal(2589u, RPRActions.StatusIds.EnhancedGallows);
    }

    [Fact]
    public void StatusId_Enshrouded_MatchesGameData()
    {
        Assert.Equal(2593u, RPRActions.StatusIds.Enshrouded);
    }

    [Fact]
    public void StatusId_ArcaneCircle_MatchesGameData()
    {
        Assert.Equal(2599u, RPRActions.StatusIds.ArcaneCircle);
    }

    [Fact]
    public void StatusId_TrueNorth_MatchesGameData()
    {
        Assert.Equal(1250u, RPRActions.StatusIds.TrueNorth);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    [Fact]
    public void HasSoulReaver_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasSoulReaver(mock.Object));
    }

    [Fact]
    public void HasEnhancedGibbet_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasEnhancedGibbet(mock.Object));
    }

    [Fact]
    public void HasEnhancedGallows_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasEnhancedGallows(mock.Object));
    }

    [Fact]
    public void HasEnhancedVoidReaping_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasEnhancedVoidReaping(mock.Object));
    }

    [Fact]
    public void HasEnhancedCrossReaping_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasEnhancedCrossReaping(mock.Object));
    }

    [Fact]
    public void HasEnshrouded_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasEnshrouded(mock.Object));
    }

    [Fact]
    public void HasArcaneCircle_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasArcaneCircle(mock.Object));
    }

    [Fact]
    public void HasBloodsownCircle_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasBloodsownCircle(mock.Object));
    }

    [Fact]
    public void HasSoulsow_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasSoulsow(mock.Object));
    }

    [Fact]
    public void HasPerfectioParata_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasPerfectioParata(mock.Object));
    }

    [Fact]
    public void HasOblatio_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasOblatio(mock.Object));
    }

    [Fact]
    public void HasIdealHost_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasIdealHost(mock.Object));
    }

    [Fact]
    public void HasEnhancedHarpe_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasEnhancedHarpe(mock.Object));
    }

    [Fact]
    public void HasTrueNorth_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasTrueNorth(mock.Object));
    }

    [Fact]
    public void HasBloodbath_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasBloodbath(mock.Object));
    }

    [Fact]
    public void GetSoulReaverStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0, _helper.GetSoulReaverStacks(mock.Object));
    }

    [Fact]
    public void GetEnshroudedRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetEnshroudedRemaining(mock.Object));
    }

    [Fact]
    public void GetArcaneCircleRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetArcaneCircleRemaining(mock.Object));
    }

    [Fact]
    public void GetImmortalSacrificeStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0, _helper.GetImmortalSacrificeStacks(mock.Object));
    }

    #endregion

    #region RPRActions Lookup Helpers

    [Fact]
    public void GetSoulReaverAction_WithEnhancedGibbet_ReturnsGibbet()
    {
        var action = RPRActions.GetSoulReaverAction(hasEnhancedGibbet: true, hasEnhancedGallows: false, isAoe: false);
        Assert.Equal(RPRActions.Gibbet.ActionId, action.ActionId);
    }

    [Fact]
    public void GetSoulReaverAction_WithEnhancedGallows_ReturnsGallows()
    {
        var action = RPRActions.GetSoulReaverAction(hasEnhancedGibbet: false, hasEnhancedGallows: true, isAoe: false);
        Assert.Equal(RPRActions.Gallows.ActionId, action.ActionId);
    }

    [Fact]
    public void GetSoulReaverAction_AoE_ReturnsGuillotine()
    {
        var action = RPRActions.GetSoulReaverAction(hasEnhancedGibbet: false, hasEnhancedGallows: false, isAoe: true);
        Assert.Equal(RPRActions.Guillotine.ActionId, action.ActionId);
    }

    [Fact]
    public void GetSoulReaverAction_NoEnhanced_ReturnsGibbet()
    {
        var action = RPRActions.GetSoulReaverAction(hasEnhancedGibbet: false, hasEnhancedGallows: false, isAoe: false);
        Assert.Equal(RPRActions.Gibbet.ActionId, action.ActionId);
    }

    [Fact]
    public void GetEnshroudGcd_WithEnhancedVoidReaping_ReturnsVoidReaping()
    {
        var action = RPRActions.GetEnshroudGcd(hasEnhancedVoidReaping: true, hasEnhancedCrossReaping: false, isAoe: false);
        Assert.Equal(RPRActions.VoidReaping.ActionId, action.ActionId);
    }

    [Fact]
    public void GetEnshroudGcd_WithEnhancedCrossReaping_ReturnsCrossReaping()
    {
        var action = RPRActions.GetEnshroudGcd(hasEnhancedVoidReaping: false, hasEnhancedCrossReaping: true, isAoe: false);
        Assert.Equal(RPRActions.CrossReaping.ActionId, action.ActionId);
    }

    [Fact]
    public void GetEnshroudGcd_AoE_ReturnsGrimReaping()
    {
        var action = RPRActions.GetEnshroudGcd(hasEnhancedVoidReaping: false, hasEnhancedCrossReaping: false, isAoe: true);
        Assert.Equal(RPRActions.GrimReaping.ActionId, action.ActionId);
    }

    [Fact]
    public void GetSoulSpender_GluttonyReady_ReturnsGluttony()
    {
        var action = RPRActions.GetSoulSpender(hasEnhancedGibbet: false, hasEnhancedGallows: false, gluttonyReady: true, isAoe: false);
        Assert.Equal(RPRActions.Gluttony.ActionId, action.ActionId);
    }

    [Fact]
    public void GetSoulSpender_AoE_ReturnsGrimSwathe()
    {
        var action = RPRActions.GetSoulSpender(hasEnhancedGibbet: false, hasEnhancedGallows: false, gluttonyReady: false, isAoe: true);
        Assert.Equal(RPRActions.GrimSwathe.ActionId, action.ActionId);
    }

    #endregion
}
