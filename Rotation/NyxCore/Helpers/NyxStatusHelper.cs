using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;

namespace Olympus.Rotation.NyxCore.Helpers;

/// <summary>
/// Helper class for checking Dark Knight status effects.
/// </summary>
public sealed class NyxStatusHelper
{
    #region Tank Stance

    /// <summary>
    /// Checks if the player has Grit (tank stance) active.
    /// </summary>
    public bool HasGrit(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.Grit);
    }

    #endregion

    #region Darkside and Dark Arts

    /// <summary>
    /// Checks if the player has Dark Arts active (TBN broke - free Edge/Flood).
    /// </summary>
    public bool HasDarkArts(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.DarkArts);
    }

    #endregion

    #region Damage Buffs

    /// <summary>
    /// Checks if the player has Blood Weapon active.
    /// </summary>
    public bool HasBloodWeapon(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.BloodWeapon);
    }

    /// <summary>
    /// Gets the remaining duration of Blood Weapon.
    /// </summary>
    public float GetBloodWeaponRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, DRKActions.StatusIds.BloodWeapon);
    }

    /// <summary>
    /// Checks if the player has Delirium active.
    /// </summary>
    public bool HasDelirium(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.Delirium);
    }

    /// <summary>
    /// Gets the number of Delirium stacks remaining (pre-Lv.96).
    /// </summary>
    public int GetDeliriumStacks(IBattleChara player)
    {
        return GetStatusStacks(player, DRKActions.StatusIds.Delirium);
    }

    /// <summary>
    /// Checks if Scornful Edge buff is active (from Torcleaver at Lv.96+).
    /// Enables Disesteem.
    /// </summary>
    public bool HasScornfulEdge(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.ScornfulEdge);
    }

    #endregion

    #region Living Shadow

    /// <summary>
    /// Checks if Living Shadow is currently active.
    /// Note: Living Shadow doesn't apply a visible status to the player,
    /// so this checks for the presence of the summoned entity.
    /// For now, we'll track via action cooldown in the rotation.
    /// </summary>
    public bool HasLivingShadow(IBattleChara player)
    {
        // Living Shadow doesn't give the player a status buff
        // We track this via the action's cooldown state instead
        // Return false here - actual tracking done via action cooldown
        return false;
    }

    #endregion

    #region Ground DoT

    /// <summary>
    /// Checks if Salted Earth is active.
    /// Note: Salted Earth is a ground effect, not a player buff.
    /// For now, tracking is done via action cooldown in the rotation.
    /// </summary>
    public bool HasSaltedEarth(IBattleChara player)
    {
        // Salted Earth doesn't give the player a status buff
        // We track this via the action's cooldown state instead
        return false;
    }

    #endregion

    #region Defensive Buffs

    /// <summary>
    /// Checks if the player has Living Dead active.
    /// </summary>
    public bool HasLivingDead(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.LivingDead);
    }

    /// <summary>
    /// Checks if the player has Walking Dead active (MUST be healed to full or die).
    /// </summary>
    public bool HasWalkingDead(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.WalkingDead);
    }

    /// <summary>
    /// Checks if the player has Shadow Wall/Shadowed Vigil active.
    /// </summary>
    public bool HasShadowWall(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.ShadowWall) ||
               HasStatus(player, DRKActions.StatusIds.ShadowedVigil);
    }

    /// <summary>
    /// Checks if the player has Dark Mind active.
    /// </summary>
    public bool HasDarkMind(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.DarkMind);
    }

    /// <summary>
    /// Checks if the player has Dark Missionary active.
    /// </summary>
    public bool HasDarkMissionary(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.DarkMissionary);
    }

    /// <summary>
    /// Checks if the player has The Blackest Night shield active.
    /// </summary>
    public bool HasTheBlackestNight(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.TheBlackestNight);
    }

    /// <summary>
    /// Checks if the player has Oblation active.
    /// </summary>
    public bool HasOblation(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.Oblation);
    }

    /// <summary>
    /// Checks if the player has Rampart active.
    /// </summary>
    public bool HasRampart(IBattleChara player)
    {
        return HasStatus(player, DRKActions.StatusIds.Rampart);
    }

    /// <summary>
    /// Checks if any defensive cooldown is active.
    /// </summary>
    public bool HasActiveMitigation(IBattleChara player)
    {
        return HasLivingDead(player) ||
               HasWalkingDead(player) ||
               HasShadowWall(player) ||
               HasDarkMind(player) ||
               HasTheBlackestNight(player) ||
               HasOblation(player) ||
               HasRampart(player) ||
               HasStatus(player, DRKActions.StatusIds.ArmsLength);
    }

    /// <summary>
    /// Gets a string listing all active mitigations for debug display.
    /// </summary>
    public string GetActiveMitigations(IBattleChara player)
    {
        var active = new System.Collections.Generic.List<string>();

        if (HasLivingDead(player)) active.Add("Living Dead");
        if (HasWalkingDead(player)) active.Add("Walking Dead!");
        if (HasShadowWall(player)) active.Add("Shadow Wall");
        if (HasDarkMind(player)) active.Add("Dark Mind");
        if (HasTheBlackestNight(player)) active.Add("TBN");
        if (HasOblation(player)) active.Add("Oblation");
        if (HasRampart(player)) active.Add("Rampart");
        if (HasStatus(player, DRKActions.StatusIds.ArmsLength)) active.Add("Arm's Length");

        return active.Count > 0 ? string.Join(", ", active) : "None";
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
