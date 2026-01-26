using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.CalliopeCore.Helpers;

/// <summary>
/// Helper for checking Bard-specific buffs and debuffs.
/// </summary>
public sealed class CalliopeStatusHelper : BaseStatusHelper
{
    #region Self Buffs

    /// <summary>
    /// Checks if Straight Shot Ready (Hawk's Eye) buff is active.
    /// </summary>
    public bool HasHawksEye(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.StraightShotReady);

    /// <summary>
    /// Checks if Raging Strikes buff is active.
    /// </summary>
    public bool HasRagingStrikes(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.RagingStrikes);

    /// <summary>
    /// Gets remaining duration of Raging Strikes buff.
    /// </summary>
    public float GetRagingStrikesRemaining(IBattleChara player)
        => GetStatusRemaining(player, BRDActions.StatusIds.RagingStrikes);

    /// <summary>
    /// Checks if Battle Voice buff is active.
    /// </summary>
    public bool HasBattleVoice(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.BattleVoice);

    /// <summary>
    /// Checks if Barrage buff is active.
    /// </summary>
    public bool HasBarrage(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.Barrage);

    /// <summary>
    /// Checks if Radiant Finale buff is active.
    /// </summary>
    public bool HasRadiantFinale(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.RadiantFinale);

    /// <summary>
    /// Checks if Blast Arrow Ready buff is active.
    /// </summary>
    public bool HasBlastArrowReady(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.BlastArrowReady);

    /// <summary>
    /// Checks if Resonant Arrow Ready buff is active.
    /// </summary>
    public bool HasResonantArrowReady(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.ResonantArrowReady);

    /// <summary>
    /// Checks if Radiant Encore Ready buff is active.
    /// </summary>
    public bool HasRadiantEncoreReady(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.RadiantEncoreReady);

    #endregion

    #region Song Buffs

    /// <summary>
    /// Checks if Wanderer's Minuet is active.
    /// </summary>
    public bool HasWanderersMinuet(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.WanderersMinuet);

    /// <summary>
    /// Checks if Mage's Ballad is active.
    /// </summary>
    public bool HasMagesBallad(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.MagesBallad);

    /// <summary>
    /// Checks if Army's Paeon is active.
    /// </summary>
    public bool HasArmysPaeon(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.ArmysPaeon);

    #endregion

    #region Target DoTs

    /// <summary>
    /// Checks if Caustic Bite DoT is on the target.
    /// </summary>
    public bool HasCausticBite(IBattleChara target, uint sourceId)
        => HasStatusFromSource(target, BRDActions.StatusIds.CausticBite, sourceId) ||
           HasStatusFromSource(target, BRDActions.StatusIds.VenomousBite, sourceId);

    /// <summary>
    /// Gets remaining duration of Caustic Bite on target.
    /// </summary>
    public float GetCausticBiteRemaining(IBattleChara target, uint sourceId)
    {
        var caustic = GetStatusRemainingFromSource(target, BRDActions.StatusIds.CausticBite, sourceId);
        if (caustic > 0) return caustic;
        return GetStatusRemainingFromSource(target, BRDActions.StatusIds.VenomousBite, sourceId);
    }

    /// <summary>
    /// Checks if Stormbite DoT is on the target.
    /// </summary>
    public bool HasStormbite(IBattleChara target, uint sourceId)
        => HasStatusFromSource(target, BRDActions.StatusIds.Stormbite, sourceId) ||
           HasStatusFromSource(target, BRDActions.StatusIds.Windbite, sourceId);

    /// <summary>
    /// Gets remaining duration of Stormbite on target.
    /// </summary>
    public float GetStormbiteRemaining(IBattleChara target, uint sourceId)
    {
        var storm = GetStatusRemainingFromSource(target, BRDActions.StatusIds.Stormbite, sourceId);
        if (storm > 0) return storm;
        return GetStatusRemainingFromSource(target, BRDActions.StatusIds.Windbite, sourceId);
    }

    #endregion

    #region Role Buffs

    /// <summary>
    /// Checks if Troubadour buff is active.
    /// </summary>
    public bool HasTroubadour(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.Troubadour);

    /// <summary>
    /// Checks if Nature's Minne buff is active.
    /// </summary>
    public bool HasNaturesMinne(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.NaturesMinne);

    /// <summary>
    /// Checks if Arm's Length buff is active.
    /// </summary>
    public bool HasArmsLength(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.ArmsLength);

    /// <summary>
    /// Checks if Peloton buff is active.
    /// </summary>
    public bool HasPeloton(IBattleChara player)
        => HasStatus(player, BRDActions.StatusIds.Peloton);

    #endregion

}
