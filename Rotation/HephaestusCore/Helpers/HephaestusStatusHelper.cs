using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;

namespace Olympus.Rotation.HephaestusCore.Helpers;

/// <summary>
/// Helper class for checking Gunbreaker status effects.
/// </summary>
public sealed class HephaestusStatusHelper
{
    #region Tank Stance

    /// <summary>
    /// Checks if the player has Royal Guard (tank stance) active.
    /// </summary>
    public bool HasRoyalGuard(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.RoyalGuard);
    }

    #endregion

    #region Continuation Ready States

    /// <summary>
    /// Checks if Ready to Rip is active (follow-up to Gnashing Fang).
    /// </summary>
    public bool HasReadyToRip(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.ReadyToRip);
    }

    /// <summary>
    /// Checks if Ready to Tear is active (follow-up to Savage Claw).
    /// </summary>
    public bool HasReadyToTear(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.ReadyToTear);
    }

    /// <summary>
    /// Checks if Ready to Gouge is active (follow-up to Wicked Talon).
    /// </summary>
    public bool HasReadyToGouge(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.ReadyToGouge);
    }

    /// <summary>
    /// Checks if Ready to Blast is active (follow-up to Burst Strike, Lv.86+).
    /// </summary>
    public bool HasReadyToBlast(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.ReadyToBlast);
    }

    /// <summary>
    /// Checks if Ready to Reign is active (from Bloodfest at Lv.100).
    /// </summary>
    public bool HasReadyToReign(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.ReadyToReign);
    }

    /// <summary>
    /// Checks if any Continuation action is ready.
    /// </summary>
    public bool HasAnyContinuationReady(IBattleChara player)
    {
        return HasReadyToRip(player) ||
               HasReadyToTear(player) ||
               HasReadyToGouge(player) ||
               HasReadyToBlast(player);
    }

    #endregion

    #region Damage Buffs

    /// <summary>
    /// Checks if the player has No Mercy active (+20% damage).
    /// </summary>
    public bool HasNoMercy(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.NoMercy);
    }

    /// <summary>
    /// Gets the remaining duration of No Mercy.
    /// </summary>
    public float GetNoMercyRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, GNBActions.StatusIds.NoMercy);
    }

    #endregion

    #region Defensive Buffs

    /// <summary>
    /// Checks if the player has Camouflage active.
    /// </summary>
    public bool HasCamouflage(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.Camouflage);
    }

    /// <summary>
    /// Checks if the player has Nebula or Great Nebula active.
    /// </summary>
    public bool HasNebula(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.Nebula) ||
               HasStatus(player, GNBActions.StatusIds.GreatNebula);
    }

    /// <summary>
    /// Checks if the player has Superbolide active.
    /// </summary>
    public bool HasSuperbolide(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.Superbolide);
    }

    /// <summary>
    /// Checks if the player has Heart of Stone or Heart of Corundum active.
    /// </summary>
    public bool HasHeartOfCorundum(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.HeartOfStone) ||
               HasStatus(player, GNBActions.StatusIds.HeartOfCorundum);
    }

    /// <summary>
    /// Checks if the player has Aurora HoT active.
    /// </summary>
    public bool HasAurora(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.Aurora);
    }

    /// <summary>
    /// Checks if the player has Heart of Light active.
    /// </summary>
    public bool HasHeartOfLight(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.HeartOfLight);
    }

    /// <summary>
    /// Checks if the player has Rampart active.
    /// </summary>
    public bool HasRampart(IBattleChara player)
    {
        return HasStatus(player, GNBActions.StatusIds.Rampart);
    }

    /// <summary>
    /// Checks if any defensive cooldown is active.
    /// </summary>
    public bool HasActiveMitigation(IBattleChara player)
    {
        return HasSuperbolide(player) ||
               HasNebula(player) ||
               HasHeartOfCorundum(player) ||
               HasCamouflage(player) ||
               HasRampart(player) ||
               HasStatus(player, GNBActions.StatusIds.ArmsLength);
    }

    /// <summary>
    /// Gets a string listing all active mitigations for debug display.
    /// </summary>
    public string GetActiveMitigations(IBattleChara player)
    {
        var active = new System.Collections.Generic.List<string>();

        if (HasSuperbolide(player)) active.Add("Superbolide!");
        if (HasNebula(player)) active.Add("Nebula");
        if (HasHeartOfCorundum(player)) active.Add("Heart");
        if (HasCamouflage(player)) active.Add("Camouflage");
        if (HasRampart(player)) active.Add("Rampart");
        if (HasStatus(player, GNBActions.StatusIds.ArmsLength)) active.Add("Arm's Length");
        if (HasAurora(player)) active.Add("Aurora");

        return active.Count > 0 ? string.Join(", ", active) : "None";
    }

    #endregion

    #region DoT Debuffs (on targets)

    /// <summary>
    /// Checks if the target has Sonic Break DoT active.
    /// </summary>
    public bool HasSonicBreakDebuff(IBattleChara target)
    {
        return HasStatus(target, GNBActions.StatusIds.SonicBreak);
    }

    /// <summary>
    /// Checks if the target has Bow Shock DoT active.
    /// </summary>
    public bool HasBowShockDebuff(IBattleChara target)
    {
        return HasStatus(target, GNBActions.StatusIds.BowShock);
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
