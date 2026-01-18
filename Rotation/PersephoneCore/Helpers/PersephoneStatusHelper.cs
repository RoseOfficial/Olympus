using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;

namespace Olympus.Rotation.PersephoneCore.Helpers;

/// <summary>
/// Helper for checking Summoner-specific buffs and debuffs.
/// </summary>
public sealed class PersephoneStatusHelper
{
    #region Self Buffs

    /// <summary>
    /// Checks if Swiftcast buff is active.
    /// </summary>
    public bool HasSwiftcast(IBattleChara player)
        => HasStatus(player, SMNActions.StatusIds.Swiftcast);

    /// <summary>
    /// Checks if Lucid Dreaming buff is active.
    /// </summary>
    public bool HasLucidDreaming(IBattleChara player)
        => HasStatus(player, SMNActions.StatusIds.LucidDreaming);

    /// <summary>
    /// Checks if Further Ruin buff is active (enables Ruin IV).
    /// </summary>
    public bool HasFurtherRuin(IBattleChara player)
        => HasStatus(player, SMNActions.StatusIds.FurtherRuin);

    /// <summary>
    /// Gets remaining duration of Further Ruin buff.
    /// </summary>
    public float GetFurtherRuinRemaining(IBattleChara player)
        => GetStatusRemaining(player, SMNActions.StatusIds.FurtherRuin);

    /// <summary>
    /// Checks if Searing Light party buff is active on self.
    /// </summary>
    public bool HasSearingLight(IBattleChara player)
        => HasStatus(player, SMNActions.StatusIds.SearingLight);

    /// <summary>
    /// Gets remaining duration of Searing Light buff.
    /// </summary>
    public float GetSearingLightRemaining(IBattleChara player)
        => GetStatusRemaining(player, SMNActions.StatusIds.SearingLight);

    /// <summary>
    /// Checks if Radiant Aegis shield is active.
    /// </summary>
    public bool HasRadiantAegis(IBattleChara player)
        => HasStatus(player, SMNActions.StatusIds.RadiantAegis);

    /// <summary>
    /// Checks if Surecast buff is active.
    /// </summary>
    public bool HasSurecast(IBattleChara player)
        => HasStatus(player, SMNActions.StatusIds.Surecast);

    #endregion

    #region Primal Favor Buffs

    /// <summary>
    /// Checks if Ifrit's Favor buff is active (enables Crimson Cyclone).
    /// </summary>
    public bool HasIfritsFavor(IBattleChara player)
        => HasStatus(player, SMNActions.StatusIds.IfritsFavor);

    /// <summary>
    /// Checks if Titan's Favor buff is active (enables Mountain Buster).
    /// </summary>
    public bool HasTitansFavor(IBattleChara player)
        => HasStatus(player, SMNActions.StatusIds.TitansFavor);

    /// <summary>
    /// Checks if Garuda's Favor buff is active (enables Slipstream).
    /// </summary>
    public bool HasGarudasFavor(IBattleChara player)
        => HasStatus(player, SMNActions.StatusIds.GarudasFavor);

    #endregion

    #region Demi-Summon Detection

    // Demi-summon state is primarily tracked via gauge, but we can detect
    // phoenix via the Everlasting Flight buff on party members.

    /// <summary>
    /// Checks if Everlasting Flight (Phoenix regen) is active on player.
    /// Can be used to detect Phoenix phase.
    /// </summary>
    public bool HasEverlastingFlight(IBattleChara player)
        => HasStatus(player, SMNActions.StatusIds.EverlastingFlight);

    #endregion

    #region Helper Methods

    private static bool HasStatus(IBattleChara target, uint statusId)
    {
        if (target.StatusList == null)
            return false;

        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId)
                return true;
        }

        return false;
    }

    private static float GetStatusRemaining(IBattleChara target, uint statusId)
    {
        if (target.StatusList == null)
            return 0f;

        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId)
                return status.RemainingTime;
        }

        return 0f;
    }

    private static int GetStatusStacks(IBattleChara target, uint statusId)
    {
        if (target.StatusList == null)
            return 0;

        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId)
                return status.Param;
        }

        return 0;
    }

    #endregion
}
