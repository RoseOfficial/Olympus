using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.ZeusCore.Helpers;

/// <summary>
/// Helper class for checking Dragoon status effects.
/// </summary>
public sealed class ZeusStatusHelper : BaseStatusHelper
{
    #region Damage Buffs

    /// <summary>
    /// Checks if Power Surge (+15% damage) is active.
    /// </summary>
    public bool HasPowerSurge(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.PowerSurge);
    }

    /// <summary>
    /// Gets the remaining duration of Power Surge.
    /// </summary>
    public float GetPowerSurgeRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, DRGActions.StatusIds.PowerSurge);
    }

    /// <summary>
    /// Checks if Lance Charge (+10% damage) is active.
    /// </summary>
    public bool HasLanceCharge(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.LanceCharge);
    }

    /// <summary>
    /// Gets the remaining duration of Lance Charge.
    /// </summary>
    public float GetLanceChargeRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, DRGActions.StatusIds.LanceCharge);
    }

    /// <summary>
    /// Checks if Life Surge (guaranteed crit) is active.
    /// </summary>
    public bool HasLifeSurge(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.LifeSurge);
    }

    /// <summary>
    /// Checks if Battle Litany (party crit) is active.
    /// </summary>
    public bool HasBattleLitany(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.BattleLitany);
    }

    /// <summary>
    /// Gets the remaining duration of Battle Litany.
    /// </summary>
    public float GetBattleLitanyRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, DRGActions.StatusIds.BattleLitany);
    }

    /// <summary>
    /// Checks if Right Eye (Dragon Sight self buff) is active.
    /// </summary>
    public bool HasRightEye(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.RightEye);
    }

    #endregion

    #region Proc Detection

    /// <summary>
    /// Checks if Dive Ready (can use Mirage Dive) is active.
    /// </summary>
    public bool HasDiveReady(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.DiveReady);
    }

    /// <summary>
    /// Checks if Fang and Claw Bared proc is ready.
    /// </summary>
    public bool HasFangAndClawBared(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.FangAndClawBared);
    }

    /// <summary>
    /// Checks if Wheel in Motion proc is ready.
    /// </summary>
    public bool HasWheelInMotion(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.WheelInMotion);
    }

    /// <summary>
    /// Checks if Draconian Fire is active.
    /// </summary>
    public bool HasDraconianFire(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.DraconianFire);
    }

    /// <summary>
    /// Checks if Nastrond Ready is active.
    /// </summary>
    public bool HasNastrondReady(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.NastrondReady);
    }

    /// <summary>
    /// Checks if Stardiver Ready is active.
    /// </summary>
    public bool HasStardiverReady(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.StardiverReady);
    }

    /// <summary>
    /// Checks if Starcross Ready is active.
    /// </summary>
    public bool HasStarcrossReady(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.StarcrossReady);
    }

    #endregion

    #region DoT Tracking

    /// <summary>
    /// Checks if the DoT (Chaos Thrust or Chaotic Spring) is active on target.
    /// </summary>
    public bool HasDot(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if ((status.StatusId == DRGActions.StatusIds.ChaosThrust ||
                 status.StatusId == DRGActions.StatusIds.ChaoticSpring) &&
                status.SourceId == playerId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the remaining duration of the DoT on target.
    /// </summary>
    public float GetDotRemaining(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if ((status.StatusId == DRGActions.StatusIds.ChaosThrust ||
                 status.StatusId == DRGActions.StatusIds.ChaoticSpring) &&
                status.SourceId == playerId)
                return status.RemainingTime;
        }
        return 0f;
    }

    #endregion

    #region Role Buffs

    /// <summary>
    /// Checks if True North is active.
    /// </summary>
    public bool HasTrueNorth(IBattleChara player)
    {
        return HasStatus(player, DRGActions.StatusIds.TrueNorth);
    }

    #endregion

    // Core status methods (HasStatus, GetStatusRemaining, GetStatusStacks) inherited from BaseStatusHelper
}
