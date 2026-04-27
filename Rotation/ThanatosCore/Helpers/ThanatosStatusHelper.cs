using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.ThanatosCore.Helpers;

/// <summary>
/// Helper class for checking Reaper status effects.
/// </summary>
public sealed class ThanatosStatusHelper : BaseStatusHelper
{
    #region Soul Reaver Detection

    /// <summary>
    /// Checks if Soul Reaver is active.
    /// </summary>
    public bool HasSoulReaver(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.SoulReaver);
    }

    /// <summary>
    /// Gets the number of Soul Reaver stacks.
    /// </summary>
    public int GetSoulReaverStacks(IBattleChara player)
    {
        return GetStatusStacks(player, RPRActions.StatusIds.SoulReaver);
    }

    #endregion

    #region Enhanced Buff Detection

    /// <summary>
    /// Checks if Enhanced Gibbet is active.
    /// </summary>
    public bool HasEnhancedGibbet(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.EnhancedGibbet);
    }

    /// <summary>
    /// Checks if Enhanced Gallows is active.
    /// </summary>
    public bool HasEnhancedGallows(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.EnhancedGallows);
    }

    /// <summary>
    /// Checks if Enhanced Void Reaping is active.
    /// </summary>
    public bool HasEnhancedVoidReaping(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.EnhancedVoidReaping);
    }

    /// <summary>
    /// Checks if Enhanced Cross Reaping is active.
    /// </summary>
    public bool HasEnhancedCrossReaping(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.EnhancedCrossReaping);
    }

    #endregion

    #region Enshroud Detection

    /// <summary>
    /// Checks if Enshrouded status is active.
    /// </summary>
    public bool HasEnshrouded(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.Enshrouded);
    }

    /// <summary>
    /// Gets remaining duration of Enshrouded status.
    /// </summary>
    public float GetEnshroudedRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, RPRActions.StatusIds.Enshrouded);
    }

    #endregion

    #region Party Buff Detection

    /// <summary>
    /// Checks if Arcane Circle is active.
    /// </summary>
    public bool HasArcaneCircle(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.ArcaneCircle);
    }

    /// <summary>
    /// Gets remaining duration of Arcane Circle.
    /// </summary>
    public float GetArcaneCircleRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, RPRActions.StatusIds.ArcaneCircle);
    }

    /// <summary>
    /// Checks if Bloodsown Circle personal damage buff is active.
    /// </summary>
    public bool HasBloodsownCircle(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.BloodsownCircle);
    }

    /// <summary>
    /// Gets the number of Immortal Sacrifice stacks.
    /// </summary>
    public int GetImmortalSacrificeStacks(IBattleChara player)
    {
        return GetStatusStacks(player, RPRActions.StatusIds.ImmortalSacrifice);
    }

    /// <summary>
    /// Checks if Immortal Sacrifice has any stacks.
    /// </summary>
    public bool HasImmortalSacrifice(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.ImmortalSacrifice);
    }

    #endregion

    #region Proc Detection

    /// <summary>
    /// Checks if Soulsow buff is active (enables Harvest Moon).
    /// </summary>
    public bool HasSoulsow(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.Soulsow);
    }

    /// <summary>
    /// Checks if Perfectio Parata proc is ready.
    /// </summary>
    public bool HasPerfectioParata(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.PerfectioParata);
    }

    /// <summary>
    /// Checks if Oblatio proc is ready (enables Sacrificium).
    /// </summary>
    public bool HasOblatio(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.Oblatio);
    }

    /// <summary>
    /// Checks if Ideal Host buff is active.
    /// </summary>
    public bool HasIdealHost(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.IdealHost);
    }

    /// <summary>
    /// Checks if Enhanced Harpe is active.
    /// </summary>
    public bool HasEnhancedHarpe(IBattleChara player)
    {
        return HasStatus(player, RPRActions.StatusIds.EnhancedHarpe);
    }

    #endregion

    #region Target Debuff Detection

    /// <summary>
    /// Checks if Death's Design is active on the target.
    /// </summary>
    public bool HasDeathsDesign(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == RPRActions.StatusIds.DeathsDesign && status.SourceId == playerId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the remaining duration of Death's Design on target.
    /// </summary>
    public float GetDeathsDesignRemaining(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == RPRActions.StatusIds.DeathsDesign && status.SourceId == playerId)
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
        return HasStatus(player, RPRActions.StatusIds.TrueNorth);
    }

    #endregion

    // Core status methods (HasStatus, GetStatusRemaining, GetStatusStacks) inherited from BaseStatusHelper
}
