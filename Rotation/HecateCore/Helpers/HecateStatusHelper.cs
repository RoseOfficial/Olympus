using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;

namespace Olympus.Rotation.HecateCore.Helpers;

/// <summary>
/// Helper for checking Black Mage-specific buffs and debuffs.
/// </summary>
public sealed class HecateStatusHelper
{
    #region Proc Buffs

    /// <summary>
    /// Checks if Firestarter proc is active (instant Fire III).
    /// </summary>
    public bool HasFirestarter(IBattleChara player)
        => HasStatus(player, BLMActions.StatusIds.Firestarter);

    /// <summary>
    /// Gets remaining duration of Firestarter proc.
    /// </summary>
    public float GetFirestarterRemaining(IBattleChara player)
        => GetStatusRemaining(player, BLMActions.StatusIds.Firestarter);

    /// <summary>
    /// Checks if Thunderhead proc is active (instant Thunder).
    /// </summary>
    public bool HasThunderhead(IBattleChara player)
        => HasStatus(player, BLMActions.StatusIds.Thunderhead);

    /// <summary>
    /// Gets remaining duration of Thunderhead proc.
    /// </summary>
    public float GetThunderheadRemaining(IBattleChara player)
        => GetStatusRemaining(player, BLMActions.StatusIds.Thunderhead);

    #endregion

    #region Self Buffs

    /// <summary>
    /// Checks if Triplecast buff is active.
    /// </summary>
    public bool HasTriplecast(IBattleChara player)
        => HasStatus(player, BLMActions.StatusIds.Triplecast);

    /// <summary>
    /// Gets remaining stacks of Triplecast (0-3).
    /// </summary>
    public int GetTriplecastStacks(IBattleChara player)
        => GetStatusStacks(player, BLMActions.StatusIds.Triplecast);

    /// <summary>
    /// Checks if Swiftcast buff is active.
    /// </summary>
    public bool HasSwiftcast(IBattleChara player)
        => HasStatus(player, BLMActions.StatusIds.Swiftcast);

    /// <summary>
    /// Checks if Ley Lines buff is active.
    /// </summary>
    public bool HasLeyLines(IBattleChara player)
        => HasStatus(player, BLMActions.StatusIds.LeyLines);

    /// <summary>
    /// Gets remaining duration of Ley Lines.
    /// </summary>
    public float GetLeyLinesRemaining(IBattleChara player)
        => GetStatusRemaining(player, BLMActions.StatusIds.LeyLines);

    /// <summary>
    /// Checks if Manaward buff is active.
    /// </summary>
    public bool HasManaward(IBattleChara player)
        => HasStatus(player, BLMActions.StatusIds.Manaward);

    /// <summary>
    /// Checks if Surecast buff is active.
    /// </summary>
    public bool HasSurecast(IBattleChara player)
        => HasStatus(player, BLMActions.StatusIds.Surecast);

    #endregion

    #region Target Debuffs (Thunder DoTs)

    /// <summary>
    /// Checks if any Thunder DoT is on the target from this player.
    /// </summary>
    public bool HasThunderDoT(IBattleChara target, uint sourceId)
    {
        // Check all Thunder variants
        return HasDebuff(target, BLMActions.StatusIds.Thunder, sourceId) ||
               HasDebuff(target, BLMActions.StatusIds.Thunder3, sourceId) ||
               HasDebuff(target, BLMActions.StatusIds.HighThunder, sourceId) ||
               HasDebuff(target, BLMActions.StatusIds.Thunder2, sourceId) ||
               HasDebuff(target, BLMActions.StatusIds.Thunder4, sourceId) ||
               HasDebuff(target, BLMActions.StatusIds.HighThunder2, sourceId);
    }

    /// <summary>
    /// Gets the remaining duration of any Thunder DoT on target.
    /// </summary>
    public float GetThunderDoTRemaining(IBattleChara target, uint sourceId)
    {
        // Return the highest remaining duration of any Thunder variant
        var remaining = 0f;

        remaining = System.Math.Max(remaining, GetDebuffRemaining(target, BLMActions.StatusIds.Thunder, sourceId));
        remaining = System.Math.Max(remaining, GetDebuffRemaining(target, BLMActions.StatusIds.Thunder3, sourceId));
        remaining = System.Math.Max(remaining, GetDebuffRemaining(target, BLMActions.StatusIds.HighThunder, sourceId));
        remaining = System.Math.Max(remaining, GetDebuffRemaining(target, BLMActions.StatusIds.Thunder2, sourceId));
        remaining = System.Math.Max(remaining, GetDebuffRemaining(target, BLMActions.StatusIds.Thunder4, sourceId));
        remaining = System.Math.Max(remaining, GetDebuffRemaining(target, BLMActions.StatusIds.HighThunder2, sourceId));

        return remaining;
    }

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

    private static bool HasDebuff(IBattleChara target, uint statusId, uint sourceId)
    {
        if (target.StatusList == null)
            return false;

        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId && status.SourceId == sourceId)
                return true;
        }

        return false;
    }

    private static float GetDebuffRemaining(IBattleChara target, uint statusId, uint sourceId)
    {
        if (target.StatusList == null)
            return 0f;

        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId && status.SourceId == sourceId)
                return status.RemainingTime;
        }

        return 0f;
    }

    #endregion
}
