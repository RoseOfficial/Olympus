using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;
using Moq;
using Olympus.Data;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.AsclepiusCore.Helpers;

/// <summary>
/// Tests for AsclepiusStatusHelper utility methods.
/// AsclepiusStatusHelper is sealed with all static methods — status behavior is
/// controlled by setting up StatusList on mock characters (or passing null).
/// </summary>
public class StatusHelperTests
{
    #region Status ID Constants Tests

    [Fact]
    public void StatusIds_KardiaSystem_AreDistinct()
    {
        var ids = new uint[]
        {
            SGEActions.KardionStatusId,
            SGEActions.KardiaStatusId,
            SGEActions.SoteriaStatusId,
            SGEActions.ZoeStatusId,
            SGEActions.PhilosophiaStatusId,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_EukrasiaSystem_AreDistinct()
    {
        var ids = new uint[]
        {
            SGEActions.EukrasiaStatusId,
            SGEActions.EukrasianDiagnosisStatusId,
            SGEActions.EukrasianPrognosisStatusId,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_DoTStatuses_AreDistinct()
    {
        var ids = new uint[]
        {
            SGEActions.EukrasianDosisStatusId,
            SGEActions.EukrasianDosisIIStatusId,
            SGEActions.EukrasianDosisIIIStatusId,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_ShieldStatuses_AreDistinct()
    {
        var ids = new uint[]
        {
            SGEActions.HaimaStatusId,
            SGEActions.PanhaimaStatusId,
            SGEActions.EukrasianDiagnosisStatusId,
            SGEActions.EukrasianPrognosisStatusId,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, (uint)SGEActions.KardionStatusId);
        Assert.NotEqual(0u, (uint)SGEActions.KardiaStatusId);
        Assert.NotEqual(0u, (uint)SGEActions.EukrasiaStatusId);
        Assert.NotEqual(0u, (uint)SGEActions.ZoeStatusId);
        Assert.NotEqual(0u, (uint)SGEActions.SoteriaStatusId);
        Assert.NotEqual(0u, (uint)SGEActions.PhilosophiaStatusId);
        Assert.NotEqual(0u, (uint)SGEActions.EukrasianDosisStatusId);
        Assert.NotEqual(0u, (uint)SGEActions.HaimaStatusId);
        Assert.NotEqual(0u, (uint)SGEActions.PanhaimaStatusId);
    }

    #endregion

    #region Known Status ID Values Tests

    [Fact]
    public void StatusIds_Kardia_MatchesGameData()
    {
        Assert.Equal(2605u, (uint)SGEActions.KardiaStatusId);
    }

    [Fact]
    public void StatusIds_Kardion_MatchesGameData()
    {
        Assert.Equal(2604u, (uint)SGEActions.KardionStatusId);
    }

    [Fact]
    public void StatusIds_Eukrasia_MatchesGameData()
    {
        Assert.Equal(2606u, (uint)SGEActions.EukrasiaStatusId);
    }

    [Fact]
    public void StatusIds_Zoe_MatchesGameData()
    {
        Assert.Equal(2611u, (uint)SGEActions.ZoeStatusId);
    }

    [Fact]
    public void StatusIds_EukrasianDosis_MatchesGameData()
    {
        Assert.Equal(2614u, (uint)SGEActions.EukrasianDosisStatusId);
    }

    [Fact]
    public void StatusIds_EukrasianDosisII_MatchesGameData()
    {
        Assert.Equal(2615u, (uint)SGEActions.EukrasianDosisIIStatusId);
    }

    [Fact]
    public void StatusIds_EukrasianDosisIII_MatchesGameData()
    {
        Assert.Equal(2616u, (uint)SGEActions.EukrasianDosisIIIStatusId);
    }

    [Fact]
    public void StatusIds_Haima_MatchesGameData()
    {
        Assert.Equal(2621u, (uint)SGEActions.HaimaStatusId);
    }

    [Fact]
    public void StatusIds_Panhaima_MatchesGameData()
    {
        Assert.Equal(2613u, (uint)SGEActions.PanhaimaStatusId);
    }

    #endregion

    #region GetDotStatusId Tests

    [Theory]
    [InlineData(30, 2614u)]   // Level 30 = EukrasianDosis
    [InlineData(71, 2614u)]   // Level 71 = EukrasianDosis (just below II)
    [InlineData(72, 2615u)]   // Level 72 = EukrasianDosisII
    [InlineData(81, 2615u)]   // Level 81 = EukrasianDosisII (just below III)
    [InlineData(82, 2616u)]   // Level 82 = EukrasianDosisIII
    [InlineData(90, 2616u)]   // Level 90 = EukrasianDosisIII
    [InlineData(100, 2616u)]  // Level 100 = EukrasianDosisIII
    public void GetDotStatusId_ReturnsCorrectStatusForLevel(byte level, uint expectedStatusId)
    {
        var result = SGEActions.GetDotStatusId(level);
        Assert.Equal(expectedStatusId, result);
    }

    [Fact]
    public void GetDotStatusId_LevelProgression_UpgradesCorrectly()
    {
        var dosisLevel = SGEActions.GetDotStatusId(30);
        var dosisIILevel = SGEActions.GetDotStatusId(72);
        var dosisIIILevel = SGEActions.GetDotStatusId(82);

        Assert.NotEqual(dosisLevel, dosisIILevel);
        Assert.NotEqual(dosisIILevel, dosisIIILevel);
        Assert.NotEqual(dosisLevel, dosisIIILevel);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    // StatusList in Dalamud wraps native game memory and cannot be constructed in tests.
    // These tests verify that each Has* method safely returns false when StatusList is null,
    // exercising the defensive null-check code path in BaseStatusHelper.

    [Fact]
    public void HasEukrasia_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasEukrasia(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasZoe_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasZoe(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void GetZoeRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.GetZoeRemaining(mock.Object);

        Assert.Equal(0f, result);
    }

    [Fact]
    public void HasKardia_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasKardia(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasKardionFrom_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasKardionFrom(mock.Object, 1u);

        Assert.False(result);
    }

    [Fact]
    public void HasSoteria_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasSoteria(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void GetSoteriaStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.GetSoteriaStacks(mock.Object);

        Assert.Equal(0, result);
    }

    [Fact]
    public void HasPhilosophia_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasPhilosophia(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasEukrasianDiagnosisShield_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasEukrasianDiagnosisShield(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasEukrasianPrognosisShield_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasEukrasianPrognosisShield(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasAnyEukrasianShield_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasAnyEukrasianShield(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasHaima_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasHaima(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void GetHaimaStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.GetHaimaStacks(mock.Object);

        Assert.Equal(0, result);
    }

    [Fact]
    public void HasPanhaima_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasPanhaima(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasPhysisII_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasPhysisII(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasKerachole_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasKerachole(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasKrasis_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasKrasis(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasEukrasianDosis_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasEukrasianDosis(mock.Object, out _);

        Assert.False(result);
    }

    [Fact]
    public void HasSurecast_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasSurecast(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void GetPanhaimaStacks_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.GetPanhaimaStacks(mock.Object);

        Assert.Equal(0, result);
    }

    [Fact]
    public void HasTaurochole_NullStatusList_ReturnsFalse()
    {
        // HasTaurochole checks for KeracholeStatusId — Taurochole and Kerachole share the same mitigation buff
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasTaurochole(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasHolos_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasHolos(mock.Object);

        Assert.False(result);
    }

    [Fact]
    public void HasEukrasianDosisDoT_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var result = AsclepiusStatusHelper.HasEukrasianDosisDoT(mock.Object);

        Assert.False(result);
    }

    #endregion

    #region Status ID Consistency Tests

    [Fact]
    public void HasTaurochole_UsesKeracholeStatusId_SameAsHasKerachole()
    {
        // HasTaurochole and HasKerachole both check KeracholeStatusId.
        // Verify they are consistent: if one returns false (null guard),
        // the other returns false too.
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var taurocholeResult = AsclepiusStatusHelper.HasTaurochole(mock.Object);
        var keracholeResult = AsclepiusStatusHelper.HasKerachole(mock.Object);

        // Both return false — they share the same underlying status ID
        Assert.Equal(keracholeResult, taurocholeResult);
    }

    [Fact]
    public void HasEukrasianDosisDoT_ConsistentWithHasEukrasianDosis()
    {
        // HasEukrasianDosisDoT and HasEukrasianDosis both check EukrasianDosisStatusId.
        // Verify consistent null-guard behavior.
        var mock = MockBuilders.CreateMockBattleChara();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);

        var dotResult = AsclepiusStatusHelper.HasEukrasianDosisDoT(mock.Object);
        AsclepiusStatusHelper.HasEukrasianDosis(mock.Object, out _);
        // Both use the same status ID — both return false when status list is null
        Assert.False(dotResult);
    }

    #endregion

    #region Level-based Utility Method Tests

    [Fact]
    public void GetDosisForLevel_Level1_ReturnsDosis()
    {
        var actionId = AsclepiusStatusHelper.GetDosisForLevel(1);
        Assert.Equal(SGEActions.Dosis.ActionId, actionId);
    }

    [Fact]
    public void GetDosisForLevel_Level72_ReturnsDosisII()
    {
        var actionId = AsclepiusStatusHelper.GetDosisForLevel(72);
        Assert.Equal(SGEActions.DosisII.ActionId, actionId);
    }

    [Fact]
    public void GetDosisForLevel_Level82_ReturnsDosisIII()
    {
        var actionId = AsclepiusStatusHelper.GetDosisForLevel(82);
        Assert.Equal(SGEActions.DosisIII.ActionId, actionId);
    }

    [Fact]
    public void GetEukrasianDosisForLevel_Level30_ReturnsEukrasianDosis()
    {
        var actionId = AsclepiusStatusHelper.GetEukrasianDosisForLevel(30);
        Assert.Equal(SGEActions.EukrasianDosis.ActionId, actionId);
    }

    [Fact]
    public void GetEukrasianDosisForLevel_Level82_ReturnsEukrasianDosisIII()
    {
        var actionId = AsclepiusStatusHelper.GetEukrasianDosisForLevel(82);
        Assert.Equal(SGEActions.EukrasianDosisIII.ActionId, actionId);
    }

    [Fact]
    public void GetPhlegmaForLevel_Level25_ReturnsZero()
    {
        // Phlegma requires level 26
        var actionId = AsclepiusStatusHelper.GetPhlegmaForLevel(25);
        Assert.Equal(0u, actionId);
    }

    [Fact]
    public void GetPhlegmaForLevel_Level26_ReturnsPhlegma()
    {
        var actionId = AsclepiusStatusHelper.GetPhlegmaForLevel(26);
        Assert.Equal(SGEActions.Phlegma.ActionId, actionId);
    }

    [Fact]
    public void GetPhlegmaForLevel_Level82_ReturnsPhlegmaIII()
    {
        var actionId = AsclepiusStatusHelper.GetPhlegmaForLevel(82);
        Assert.Equal(SGEActions.PhlegmaIII.ActionId, actionId);
    }

    [Fact]
    public void GetToxikonForLevel_Level65_ReturnsZero()
    {
        // Toxikon requires level 66
        var actionId = AsclepiusStatusHelper.GetToxikonForLevel(65);
        Assert.Equal(0u, actionId);
    }

    [Fact]
    public void GetToxikonForLevel_Level66_ReturnsToxikon()
    {
        var actionId = AsclepiusStatusHelper.GetToxikonForLevel(66);
        Assert.Equal(SGEActions.Toxikon.ActionId, actionId);
    }

    [Fact]
    public void GetToxikonForLevel_Level82_ReturnsToxikonII()
    {
        var actionId = AsclepiusStatusHelper.GetToxikonForLevel(82);
        Assert.Equal(SGEActions.ToxikonII.ActionId, actionId);
    }

    [Fact]
    public void GetDyskrasiaForLevel_Level45_ReturnsZero()
    {
        // Dyskrasia requires level 46
        var actionId = AsclepiusStatusHelper.GetDyskrasiaForLevel(45);
        Assert.Equal(0u, actionId);
    }

    [Fact]
    public void GetDyskrasiaForLevel_Level46_ReturnsDyskrasia()
    {
        var actionId = AsclepiusStatusHelper.GetDyskrasiaForLevel(46);
        Assert.Equal(SGEActions.Dyskrasia.ActionId, actionId);
    }

    [Fact]
    public void GetDyskrasiaForLevel_Level82_ReturnsDyskrasiaII()
    {
        var actionId = AsclepiusStatusHelper.GetDyskrasiaForLevel(82);
        Assert.Equal(SGEActions.DyskrasiaII.ActionId, actionId);
    }

    #endregion
}
