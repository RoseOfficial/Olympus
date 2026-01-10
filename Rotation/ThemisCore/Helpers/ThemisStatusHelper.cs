using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;

namespace Olympus.Rotation.ThemisCore.Helpers;

/// <summary>
/// Helper class for checking Paladin status effects.
/// </summary>
public sealed class ThemisStatusHelper
{
    /// <summary>
    /// Checks if the player has Iron Will (tank stance) active.
    /// </summary>
    public bool HasIronWill(IBattleChara player)
    {
        return HasStatus(player, PLDActions.StatusIds.IronWill);
    }

    /// <summary>
    /// Checks if the player has Fight or Flight active.
    /// </summary>
    public bool HasFightOrFlight(IBattleChara player)
    {
        return HasStatus(player, PLDActions.StatusIds.FightOrFlight);
    }

    /// <summary>
    /// Gets the remaining duration of Fight or Flight.
    /// </summary>
    public float GetFightOrFlightRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, PLDActions.StatusIds.FightOrFlight);
    }

    /// <summary>
    /// Checks if the player has Requiescat active.
    /// </summary>
    public bool HasRequiescat(IBattleChara player)
    {
        return HasStatus(player, PLDActions.StatusIds.Requiescat);
    }

    /// <summary>
    /// Gets the number of Requiescat stacks remaining.
    /// </summary>
    public int GetRequiescatStacks(IBattleChara player)
    {
        return GetStatusStacks(player, PLDActions.StatusIds.Requiescat);
    }

    /// <summary>
    /// Checks if the player has Sword Oath active (Atonement ready).
    /// </summary>
    public bool HasSwordOath(IBattleChara player)
    {
        return HasStatus(player, PLDActions.StatusIds.SwordOath);
    }

    /// <summary>
    /// Gets the number of Sword Oath stacks remaining.
    /// </summary>
    public int GetSwordOathStacks(IBattleChara player)
    {
        return GetStatusStacks(player, PLDActions.StatusIds.SwordOath);
    }

    /// <summary>
    /// Checks if the player has Sheltron or Holy Sheltron active.
    /// </summary>
    public bool HasSheltron(IBattleChara player)
    {
        return HasStatus(player, PLDActions.StatusIds.Sheltron) ||
               HasStatus(player, PLDActions.StatusIds.HolySheltron);
    }

    /// <summary>
    /// Checks if the player has Rampart active.
    /// </summary>
    public bool HasRampart(IBattleChara player)
    {
        return HasStatus(player, PLDActions.StatusIds.Rampart);
    }

    /// <summary>
    /// Checks if the player has Sentinel/Guardian active.
    /// </summary>
    public bool HasSentinel(IBattleChara player)
    {
        return HasStatus(player, PLDActions.StatusIds.Sentinel) ||
               HasStatus(player, PLDActions.StatusIds.Guardian);
    }

    /// <summary>
    /// Checks if the player has Hallowed Ground active.
    /// </summary>
    public bool HasHallowedGround(IBattleChara player)
    {
        return HasStatus(player, PLDActions.StatusIds.HallowedGround);
    }

    /// <summary>
    /// Checks if any defensive cooldown is active.
    /// </summary>
    public bool HasActiveMitigation(IBattleChara player)
    {
        return HasSheltron(player) ||
               HasRampart(player) ||
               HasSentinel(player) ||
               HasHallowedGround(player) ||
               HasStatus(player, PLDActions.StatusIds.Bulwark) ||
               HasStatus(player, PLDActions.StatusIds.ArmsLength);
    }

    /// <summary>
    /// Gets a string listing all active mitigations for debug display.
    /// </summary>
    public string GetActiveMitigations(IBattleChara player)
    {
        var active = new System.Collections.Generic.List<string>();

        if (HasHallowedGround(player)) active.Add("Hallowed");
        if (HasSentinel(player)) active.Add("Sentinel");
        if (HasRampart(player)) active.Add("Rampart");
        if (HasSheltron(player)) active.Add("Sheltron");
        if (HasStatus(player, PLDActions.StatusIds.Bulwark)) active.Add("Bulwark");
        if (HasStatus(player, PLDActions.StatusIds.ArmsLength)) active.Add("Arm's Length");

        return active.Count > 0 ? string.Join(", ", active) : "None";
    }

    /// <summary>
    /// Gets the remaining duration of Goring Blade DoT on a target.
    /// </summary>
    public float GetGoringBladeRemaining(IBattleChara? target, uint playerId)
    {
        if (target == null) return 0f;
        return GetStatusRemainingOnTarget(target, PLDActions.StatusIds.GoringBladeDot, playerId);
    }

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

    private static float GetStatusRemainingOnTarget(IBattleChara target, uint statusId, uint sourceId)
    {
        foreach (var status in target.StatusList)
        {
            if (status.StatusId == statusId && status.SourceId == sourceId)
                return status.RemainingTime;
        }
        return 0f;
    }

    #endregion
}
