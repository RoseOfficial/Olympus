using Olympus.Data;
using Olympus.Rotation.ZeusCore.Helpers;
using Olympus.Tests.Mocks;

namespace Olympus.Tests.Rotation.ZeusCore.Helpers;

/// <summary>
/// Tests for ZeusStatusHelper utility methods.
/// ZeusStatusHelper is sealed — status behavior is controlled via StatusList on mock characters.
/// Null-guard tests exercise the BaseStatusHelper null-safe path.
/// </summary>
public class StatusHelperTests
{
    private readonly ZeusStatusHelper _helper = new();

    #region Status ID Constants — Distinctness

    [Fact]
    public void StatusIds_DamageBuffs_AreDistinct()
    {
        var ids = new uint[]
        {
            DRGActions.StatusIds.PowerSurge,
            DRGActions.StatusIds.LanceCharge,
            DRGActions.StatusIds.LifeSurge,
            DRGActions.StatusIds.BattleLitany,
            DRGActions.StatusIds.RightEye,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_ProcStatuses_AreDistinct()
    {
        var ids = new uint[]
        {
            DRGActions.StatusIds.FangAndClawBared,
            DRGActions.StatusIds.WheelInMotion,
            DRGActions.StatusIds.DraconianFire,
            DRGActions.StatusIds.DiveReady,
            DRGActions.StatusIds.NastrondReady,
            DRGActions.StatusIds.StardiverReady,
            DRGActions.StatusIds.StarcrossReady,
        };

        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [Fact]
    public void StatusIds_AllCoreStatuses_AreNonZero()
    {
        Assert.NotEqual(0u, DRGActions.StatusIds.PowerSurge);
        Assert.NotEqual(0u, DRGActions.StatusIds.LanceCharge);
        Assert.NotEqual(0u, DRGActions.StatusIds.LifeSurge);
        Assert.NotEqual(0u, DRGActions.StatusIds.BattleLitany);
        Assert.NotEqual(0u, DRGActions.StatusIds.RightEye);
        Assert.NotEqual(0u, DRGActions.StatusIds.FangAndClawBared);
        Assert.NotEqual(0u, DRGActions.StatusIds.WheelInMotion);
        Assert.NotEqual(0u, DRGActions.StatusIds.DraconianFire);
        Assert.NotEqual(0u, DRGActions.StatusIds.DiveReady);
        Assert.NotEqual(0u, DRGActions.StatusIds.NastrondReady);
        Assert.NotEqual(0u, DRGActions.StatusIds.StardiverReady);
        Assert.NotEqual(0u, DRGActions.StatusIds.StarcrossReady);
        Assert.NotEqual(0u, DRGActions.StatusIds.ChaosThrust);
        Assert.NotEqual(0u, DRGActions.StatusIds.ChaoticSpring);
        Assert.NotEqual(0u, DRGActions.StatusIds.TrueNorth);
    }

    #endregion

    #region Known Status ID Values — Match Game Data

    [Fact]
    public void StatusId_PowerSurge_MatchesGameData()
    {
        Assert.Equal(2720u, DRGActions.StatusIds.PowerSurge);
    }

    [Fact]
    public void StatusId_LanceCharge_MatchesGameData()
    {
        Assert.Equal(1864u, DRGActions.StatusIds.LanceCharge);
    }

    [Fact]
    public void StatusId_LifeSurge_MatchesGameData()
    {
        Assert.Equal(116u, DRGActions.StatusIds.LifeSurge);
    }

    [Fact]
    public void StatusId_BattleLitany_MatchesGameData()
    {
        Assert.Equal(786u, DRGActions.StatusIds.BattleLitany);
    }

    [Fact]
    public void StatusId_ChaosThrust_MatchesGameData()
    {
        Assert.Equal(118u, DRGActions.StatusIds.ChaosThrust);
    }

    [Fact]
    public void StatusId_ChaoticSpring_MatchesGameData()
    {
        Assert.Equal(2719u, DRGActions.StatusIds.ChaoticSpring);
    }

    [Fact]
    public void StatusId_TrueNorth_MatchesGameData()
    {
        Assert.Equal(1250u, DRGActions.StatusIds.TrueNorth);
    }

    #endregion

    #region Has* Methods — Null StatusList Guard Tests

    [Fact]
    public void HasPowerSurge_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasPowerSurge(mock.Object));
    }

    [Fact]
    public void HasLanceCharge_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasLanceCharge(mock.Object));
    }

    [Fact]
    public void HasLifeSurge_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasLifeSurge(mock.Object));
    }

    [Fact]
    public void HasBattleLitany_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasBattleLitany(mock.Object));
    }

    [Fact]
    public void HasRightEye_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasRightEye(mock.Object));
    }

    [Fact]
    public void HasDiveReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasDiveReady(mock.Object));
    }

    [Fact]
    public void HasFangAndClawBared_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasFangAndClawBared(mock.Object));
    }

    [Fact]
    public void HasWheelInMotion_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasWheelInMotion(mock.Object));
    }

    [Fact]
    public void HasDraconianFire_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasDraconianFire(mock.Object));
    }

    [Fact]
    public void HasNastrondReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasNastrondReady(mock.Object));
    }

    [Fact]
    public void HasStardiverReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasStardiverReady(mock.Object));
    }

    [Fact]
    public void HasStarcrossReady_NullStatusList_ReturnsFalse()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.False(_helper.HasStarcrossReady(mock.Object));
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
    public void GetPowerSurgeRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetPowerSurgeRemaining(mock.Object));
    }

    [Fact]
    public void GetLanceChargeRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetLanceChargeRemaining(mock.Object));
    }

    [Fact]
    public void GetBattleLitanyRemaining_NullStatusList_ReturnsZero()
    {
        var mock = MockBuilders.CreateMockPlayerCharacter();
        mock.Setup(x => x.StatusList).Returns((Dalamud.Game.ClientState.Statuses.StatusList?)null!);
        Assert.Equal(0f, _helper.GetBattleLitanyRemaining(mock.Object));
    }

    #endregion

    #region DRGActions Lookup Helpers

    [Theory]
    [InlineData(100, 25771u)] // HeavensThrust at level 100
    [InlineData(86, 25771u)]  // HeavensThrust at level 86
    [InlineData(85, 84u)]     // FullThrust before level 86
    [InlineData(26, 84u)]     // FullThrust at level 26
    public void GetVorpalFinisher_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = DRGActions.GetVorpalFinisher(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 25772u)] // ChaoticSpring at level 100
    [InlineData(86, 25772u)]  // ChaoticSpring at level 86
    [InlineData(85, 88u)]     // ChaosThrust before level 86
    [InlineData(50, 88u)]     // ChaosThrust at level 50
    public void GetDisembowelFinisher_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = DRGActions.GetDisembowelFinisher(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 36952u)] // Drakesbane at level 100
    [InlineData(64, 36952u)]  // Drakesbane at level 64 (new MinLevel)
    [InlineData(63, 3556u)]   // WheelingThrust below Drakesbane
    [InlineData(58, 3556u)]   // WheelingThrust at level 58
    [InlineData(56, 3554u)]   // FangAndClaw at level 56
    public void GetPositionalFinisher_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = DRGActions.GetPositionalFinisher(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 16478u)] // HighJump at level 100
    [InlineData(74, 16478u)]  // HighJump at level 74
    [InlineData(73, 92u)]     // Jump before level 74
    public void GetJumpAction_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = DRGActions.GetJumpAction(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    [Theory]
    [InlineData(100, 16477u)] // CoerthanTorment at level 100
    [InlineData(72, 16477u)]  // CoerthanTorment at level 72
    [InlineData(71, 7397u)]   // SonicThrust before level 72
    [InlineData(62, 7397u)]   // SonicThrust at level 62
    [InlineData(40, 86u)]     // DoomSpike at level 40
    public void GetAoeFinisher_ReturnsCorrectActionForLevel(byte level, uint expectedActionId)
    {
        var action = DRGActions.GetAoeFinisher(level);
        Assert.Equal(expectedActionId, action.ActionId);
    }

    #endregion
}
