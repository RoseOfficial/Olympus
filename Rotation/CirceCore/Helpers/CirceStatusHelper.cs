using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.CirceCore.Helpers;

/// <summary>
/// Helper for checking Red Mage-specific buffs and debuffs.
/// </summary>
public sealed class CirceStatusHelper : BaseStatusHelper
{
    #region Core Buffs

    /// <summary>
    /// Checks if Dualcast buff is active (next spell is instant).
    /// </summary>
    public bool HasDualcast(IBattleChara player)
        => HasStatus(player, RDMActions.StatusIds.Dualcast);

    /// <summary>
    /// Gets remaining duration of Dualcast buff.
    /// </summary>
    public float GetDualcastRemaining(IBattleChara player)
        => GetStatusRemaining(player, RDMActions.StatusIds.Dualcast);

    #endregion

    #region Proc Buffs

    /// <summary>
    /// Checks if Verfire Ready buff is active.
    /// </summary>
    public bool HasVerfireReady(IBattleChara player)
        => HasStatus(player, RDMActions.StatusIds.VerfireReady);

    /// <summary>
    /// Gets remaining duration of Verfire Ready buff.
    /// </summary>
    public float GetVerfireRemaining(IBattleChara player)
        => GetStatusRemaining(player, RDMActions.StatusIds.VerfireReady);

    /// <summary>
    /// Checks if Verstone Ready buff is active.
    /// </summary>
    public bool HasVerstoneReady(IBattleChara player)
        => HasStatus(player, RDMActions.StatusIds.VerstoneReady);

    /// <summary>
    /// Gets remaining duration of Verstone Ready buff.
    /// </summary>
    public float GetVerstoneRemaining(IBattleChara player)
        => GetStatusRemaining(player, RDMActions.StatusIds.VerstoneReady);

    #endregion

    #region Damage Buffs

    /// <summary>
    /// Checks if Embolden buff is active on self.
    /// </summary>
    public bool HasEmbolden(IBattleChara player)
        => HasStatus(player, RDMActions.StatusIds.Embolden);

    /// <summary>
    /// Gets remaining duration of Embolden buff.
    /// </summary>
    public float GetEmboldenRemaining(IBattleChara player)
        => GetStatusRemaining(player, RDMActions.StatusIds.Embolden);

    /// <summary>
    /// Checks if Manafication buff is active.
    /// </summary>
    public bool HasManafication(IBattleChara player)
        => HasStatus(player, RDMActions.StatusIds.Manafication);

    /// <summary>
    /// Gets remaining duration of Manafication buff.
    /// </summary>
    public float GetManaficationRemaining(IBattleChara player)
        => GetStatusRemaining(player, RDMActions.StatusIds.Manafication);

    /// <summary>
    /// Checks if Acceleration buff is active.
    /// </summary>
    public bool HasAcceleration(IBattleChara player)
        => HasStatus(player, RDMActions.StatusIds.Acceleration);

    /// <summary>
    /// Gets remaining duration of Acceleration buff.
    /// </summary>
    public float GetAccelerationRemaining(IBattleChara player)
        => GetStatusRemaining(player, RDMActions.StatusIds.Acceleration);

    #endregion

    #region Special Ability Buffs

    /// <summary>
    /// Checks if Thorned Flourish buff is active (Vice of Thorns ready).
    /// </summary>
    public bool HasThornedFlourish(IBattleChara player)
        => HasStatus(player, RDMActions.StatusIds.ThornedFlourish);

    /// <summary>
    /// Checks if Grand Impact Ready buff is active.
    /// </summary>
    public bool HasGrandImpactReady(IBattleChara player)
        => HasStatus(player, RDMActions.StatusIds.GrandImpactReady);

    /// <summary>
    /// Checks if Prefulgence Ready buff is active.
    /// </summary>
    public bool HasPrefulgenceReady(IBattleChara player)
        => HasStatus(player, RDMActions.StatusIds.PrefulgenceReady);

    #endregion

    #region Defensive Buffs

    /// <summary>
    /// Checks if Magick Barrier buff is active.
    /// </summary>
    public bool HasMagickBarrier(IBattleChara player)
        => HasStatus(player, RDMActions.StatusIds.MagickBarrier);

    /// <summary>
    /// Checks if Surecast buff is active.
    /// </summary>
    public bool HasSurecast(IBattleChara player)
        => HasStatus(player, RDMActions.StatusIds.Surecast);

    #endregion

    #region Instant Cast Check

    /// <summary>
    /// Checks if the player has any instant cast buff (Dualcast, Swiftcast, or Acceleration).
    /// </summary>
    public bool HasAnyInstantCast(IBattleChara player)
        => HasDualcast(player) || HasSwiftcast(player) || HasAcceleration(player);

    #endregion

}
