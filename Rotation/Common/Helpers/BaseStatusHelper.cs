using Dalamud.Game.ClientState.Objects.Types;

namespace Olympus.Rotation.Common.Helpers;

/// <summary>
/// Base class providing common status effect checking utilities.
/// All job-specific StatusHelpers should inherit from this.
/// </summary>
public abstract class BaseStatusHelper
{
    #region Core Status Methods

    /// <summary>
    /// Checks if a character has a specific status effect.
    /// </summary>
    public static bool HasStatus(IBattleChara chara, uint statusId)
    {
        if (chara.StatusList == null)
            return false;

        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == statusId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a character has a specific status effect and returns remaining time.
    /// </summary>
    public static bool HasStatus(IBattleChara chara, uint statusId, out float remainingTime)
    {
        remainingTime = 0f;

        if (chara.StatusList == null)
            return false;

        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == statusId)
            {
                remainingTime = status.RemainingTime;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Gets the remaining duration of a specific status effect.
    /// </summary>
    public static float GetStatusRemaining(IBattleChara chara, uint statusId)
    {
        if (chara.StatusList == null)
            return 0f;

        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == statusId)
                return status.RemainingTime;
        }
        return 0f;
    }

    /// <summary>
    /// Gets the stack count for a specific status effect.
    /// </summary>
    public static int GetStatusStacks(IBattleChara chara, uint statusId)
    {
        if (chara.StatusList == null)
            return 0;

        foreach (var status in chara.StatusList)
        {
            if (status.StatusId == statusId)
                return status.Param;
        }
        return 0;
    }

    /// <summary>
    /// Checks if a character has a status effect from a specific source (for DoTs/debuffs).
    /// </summary>
    public static bool HasStatusFromSource(IBattleChara target, uint statusId, uint sourceId)
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

    /// <summary>
    /// Gets remaining duration of a status effect from a specific source.
    /// </summary>
    public static float GetStatusRemainingFromSource(IBattleChara target, uint statusId, uint sourceId)
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

    #region Shared Role Action Status IDs

    /// <summary>
    /// Common status IDs shared across multiple roles.
    /// </summary>
    public static class SharedStatusIds
    {
        public const uint Swiftcast = 167;
        public const uint LucidDreaming = 1204;
        public const uint Surecast = 160;
        public const uint TrueNorth = 1250;
        public const uint Bloodbath = 84;
        public const uint ArmsLength = 1209;
    }

    #endregion

    #region Debug Formatting Helpers

    /// <summary>
    /// Formats a list of active buff/mitigation names for debug display.
    /// Returns a comma-separated string, or "None" if the list is empty.
    /// </summary>
    protected static string FormatActiveList(System.Collections.Generic.List<string> items) =>
        items.Count > 0 ? string.Join(", ", items) : "None";

    #endregion
}
