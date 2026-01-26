using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.PrometheusCore.Helpers;

/// <summary>
/// Helper for checking Machinist-specific buffs and debuffs.
/// </summary>
public sealed class PrometheusStatusHelper : BaseStatusHelper
{
    #region Self Buffs

    /// <summary>
    /// Checks if Reassembled buff is active (guaranteed crit/DH on next weaponskill).
    /// </summary>
    public bool HasReassemble(IBattleChara player)
        => HasStatus(player, MCHActions.StatusIds.Reassembled);

    /// <summary>
    /// Gets remaining duration of Reassembled buff.
    /// </summary>
    public float GetReassembleRemaining(IBattleChara player)
        => GetStatusRemaining(player, MCHActions.StatusIds.Reassembled);

    /// <summary>
    /// Checks if Hypercharged buff is active (from Barrel Stabilizer).
    /// </summary>
    public bool HasHypercharged(IBattleChara player)
        => HasStatus(player, MCHActions.StatusIds.Hypercharged);

    /// <summary>
    /// Gets remaining duration of Hypercharged buff.
    /// </summary>
    public float GetHyperchargedRemaining(IBattleChara player)
        => GetStatusRemaining(player, MCHActions.StatusIds.Hypercharged);

    /// <summary>
    /// Checks if Full Metal Machinist buff is active (can use Full Metal Field).
    /// </summary>
    public bool HasFullMetalMachinist(IBattleChara player)
        => HasStatus(player, MCHActions.StatusIds.FullMetalMachinist);

    /// <summary>
    /// Checks if Excavator Ready buff is active.
    /// </summary>
    public bool HasExcavatorReady(IBattleChara player)
        => HasStatus(player, MCHActions.StatusIds.ExcavatorReady);

    #endregion

    #region Target Debuffs

    /// <summary>
    /// Checks if Wildfire debuff is on the target.
    /// </summary>
    public bool HasWildfire(IBattleChara target, uint sourceId)
        => HasStatusFromSource(target, MCHActions.StatusIds.Wildfire, sourceId);

    /// <summary>
    /// Gets remaining duration of Wildfire on target.
    /// </summary>
    public float GetWildfireRemaining(IBattleChara target, uint sourceId)
        => GetStatusRemainingFromSource(target, MCHActions.StatusIds.Wildfire, sourceId);

    /// <summary>
    /// Checks if Bioblaster DoT is on the target.
    /// </summary>
    public bool HasBioblaster(IBattleChara target, uint sourceId)
        => HasStatusFromSource(target, MCHActions.StatusIds.Bioblaster, sourceId);

    /// <summary>
    /// Gets remaining duration of Bioblaster on target.
    /// </summary>
    public float GetBioblasterRemaining(IBattleChara target, uint sourceId)
        => GetStatusRemainingFromSource(target, MCHActions.StatusIds.Bioblaster, sourceId);

    #endregion

    #region Role Buffs

    /// <summary>
    /// Checks if Tactician buff is active.
    /// </summary>
    public bool HasTactician(IBattleChara player)
        => HasStatus(player, MCHActions.StatusIds.Tactician);

    /// <summary>
    /// Checks if Arm's Length buff is active.
    /// </summary>
    public bool HasArmsLength(IBattleChara player)
        => HasStatus(player, MCHActions.StatusIds.ArmsLength);

    /// <summary>
    /// Checks if Peloton buff is active.
    /// </summary>
    public bool HasPeloton(IBattleChara player)
        => HasStatus(player, MCHActions.StatusIds.Peloton);

    #endregion

}
