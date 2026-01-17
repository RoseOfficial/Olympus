using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.KratosCore.Context;

namespace Olympus.Rotation.KratosCore.Helpers;

/// <summary>
/// Helper class for checking Monk status effects.
/// </summary>
public sealed class KratosStatusHelper
{
    #region Form Detection

    /// <summary>
    /// Gets the current Monk form from status effects.
    /// </summary>
    public MonkForm GetCurrentForm(IBattleChara player)
    {
        if (HasStatus(player, MNKActions.StatusIds.FormlessFist))
            return MonkForm.Formless;
        if (HasStatus(player, MNKActions.StatusIds.OpoOpoForm))
            return MonkForm.OpoOpo;
        if (HasStatus(player, MNKActions.StatusIds.RaptorForm))
            return MonkForm.Raptor;
        if (HasStatus(player, MNKActions.StatusIds.CoeurlForm))
            return MonkForm.Coeurl;
        return MonkForm.None;
    }

    /// <summary>
    /// Checks if the player has Formless Fist active.
    /// </summary>
    public bool HasFormlessFist(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.FormlessFist);
    }

    /// <summary>
    /// Checks if the player has Perfect Balance active.
    /// </summary>
    public bool HasPerfectBalance(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.PerfectBalance);
    }

    /// <summary>
    /// Gets the remaining stacks of Perfect Balance.
    /// </summary>
    public int GetPerfectBalanceStacks(IBattleChara player)
    {
        return GetStatusStacks(player, MNKActions.StatusIds.PerfectBalance);
    }

    #endregion

    #region Damage Buffs

    /// <summary>
    /// Checks if the player has Disciplined Fist active.
    /// </summary>
    public bool HasDisciplinedFist(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.DisciplinedFist);
    }

    /// <summary>
    /// Gets the remaining duration of Disciplined Fist.
    /// </summary>
    public float GetDisciplinedFistRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, MNKActions.StatusIds.DisciplinedFist);
    }

    /// <summary>
    /// Checks if the player has Leaden Fist active.
    /// </summary>
    public bool HasLeadenFist(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.LeadenFist);
    }

    /// <summary>
    /// Checks if Riddle of Fire is active.
    /// </summary>
    public bool HasRiddleOfFire(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.RiddleOfFire);
    }

    /// <summary>
    /// Gets the remaining duration of Riddle of Fire.
    /// </summary>
    public float GetRiddleOfFireRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, MNKActions.StatusIds.RiddleOfFire);
    }

    /// <summary>
    /// Checks if Brotherhood is active.
    /// </summary>
    public bool HasBrotherhood(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.Brotherhood);
    }

    /// <summary>
    /// Checks if Riddle of Wind is active.
    /// </summary>
    public bool HasRiddleOfWind(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.RiddleOfWind);
    }

    #endregion

    #region Proc Detection

    /// <summary>
    /// Checks if Raptor's Fury proc is active.
    /// </summary>
    public bool HasRaptorsFury(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.RaptorsFury);
    }

    /// <summary>
    /// Checks if Coeurl's Fury proc is active.
    /// </summary>
    public bool HasCoeurlsFury(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.CoeurlsFury);
    }

    /// <summary>
    /// Checks if Opo-opo's Fury proc is active.
    /// </summary>
    public bool HasOpooposFury(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.OpooposFury);
    }

    /// <summary>
    /// Checks if Fire's Rumination proc is ready.
    /// </summary>
    public bool HasFiresRumination(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.FiresRumination);
    }

    /// <summary>
    /// Checks if Wind's Rumination proc is ready.
    /// </summary>
    public bool HasWindsRumination(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.WindsRumination);
    }

    #endregion

    #region DoT Tracking

    /// <summary>
    /// Checks if Demolish is active on the target.
    /// </summary>
    public bool HasDemolish(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == MNKActions.StatusIds.Demolish && status.SourceId == playerId)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Gets the remaining duration of Demolish on target.
    /// </summary>
    public float GetDemolishRemaining(IBattleChara target, uint playerId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == MNKActions.StatusIds.Demolish && status.SourceId == playerId)
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
        return HasStatus(player, MNKActions.StatusIds.TrueNorth);
    }

    /// <summary>
    /// Checks if Bloodbath is active.
    /// </summary>
    public bool HasBloodbath(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.Bloodbath);
    }

    #endregion

    #region Defensive Buffs

    /// <summary>
    /// Checks if Riddle of Earth is active.
    /// </summary>
    public bool HasRiddleOfEarth(IBattleChara player)
    {
        return HasStatus(player, MNKActions.StatusIds.RiddleOfEarth);
    }

    #endregion

    #region Public Helpers

    /// <summary>
    /// Checks if the character has a specific status effect.
    /// </summary>
    public bool HasStatus(IBattleChara character, uint statusId)
    {
        foreach (var status in character.StatusList)
        {
            if (status.StatusId == statusId)
                return true;
        }
        return false;
    }

    #endregion

    #region Private Helpers

    private static float GetStatusRemaining(IBattleChara character, uint statusId)
    {
        foreach (var status in character.StatusList)
        {
            if (status.StatusId == statusId)
                return status.RemainingTime;
        }
        return 0f;
    }

    private static int GetStatusStacks(IBattleChara character, uint statusId)
    {
        foreach (var status in character.StatusList)
        {
            if (status.StatusId == statusId)
                return status.Param;
        }
        return 0;
    }

    #endregion
}
