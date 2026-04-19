using Dalamud.Game.ClientState.Objects.Types;
using Olympus.Data;
using Olympus.Rotation.Common.Helpers;

namespace Olympus.Rotation.AresCore.Helpers;

/// <summary>
/// Helper class for checking Warrior status effects.
/// </summary>
public sealed class AresStatusHelper : BaseStatusHelper
{
    #region Tank Stance

    /// <summary>
    /// Checks if the player has Defiance (tank stance) active.
    /// </summary>
    public bool HasDefiance(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.Defiance);
    }

    #endregion

    #region Damage Buffs

    /// <summary>
    /// Checks if the player has Surging Tempest active.
    /// </summary>
    public bool HasSurgingTempest(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.SurgingTempest);
    }

    /// <summary>
    /// Gets the remaining duration of Surging Tempest.
    /// </summary>
    public float GetSurgingTempestRemaining(IBattleChara player)
    {
        return GetStatusRemaining(player, WARActions.StatusIds.SurgingTempest);
    }

    /// <summary>
    /// Checks if the player has Inner Release active.
    /// </summary>
    public bool HasInnerRelease(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.InnerRelease);
    }

    /// <summary>
    /// Gets the number of Inner Release stacks remaining.
    /// </summary>
    public int GetInnerReleaseStacks(IBattleChara player)
    {
        return GetStatusStacks(player, WARActions.StatusIds.InnerRelease);
    }

    /// <summary>
    /// Checks if the player has Berserk active (pre-70).
    /// </summary>
    public bool HasBerserk(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.Berserk);
    }

    /// <summary>
    /// Checks if the player has Nascent Chaos active (enables Inner Chaos/Chaotic Cyclone).
    /// </summary>
    public bool HasNascentChaos(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.NascentChaos);
    }

    /// <summary>
    /// Checks if Primal Rend Ready buff is active.
    /// </summary>
    public bool HasPrimalRendReady(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.PrimalRendReady);
    }

    /// <summary>
    /// Checks if Primal Ruination Ready buff is active.
    /// </summary>
    public bool HasPrimalRuinationReady(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.PrimalRuinationReady);
    }

    /// <summary>
    /// Checks if Wrathful buff is active (enables Primal Wrath at Lv.96+).
    /// Granted after stacking 3 Burgeoning Fury during Inner Release (from Fell Cleave).
    /// </summary>
    public bool HasWrathful(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.Wrathful);
    }

    #endregion

    #region Defensive Buffs

    /// <summary>
    /// Checks if the player has Holmgang active.
    /// </summary>
    public bool HasHolmgang(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.Holmgang);
    }

    /// <summary>
    /// Checks if the player has Vengeance/Damnation active.
    /// </summary>
    public bool HasVengeance(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.Vengeance) ||
               HasStatus(player, WARActions.StatusIds.Damnation);
    }

    /// <summary>
    /// Checks if the player has Raw Intuition/Bloodwhetting active.
    /// </summary>
    public bool HasBloodwhetting(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.RawIntuition) ||
               HasStatus(player, WARActions.StatusIds.Bloodwhetting);
    }

    /// <summary>
    /// Checks if the player has Thrill of Battle active.
    /// </summary>
    public bool HasThrillOfBattle(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.ThrillOfBattle);
    }

    /// <summary>
    /// Checks if the player has Rampart active.
    /// </summary>
    public bool HasRampart(IBattleChara player)
    {
        return HasStatus(player, WARActions.StatusIds.Rampart);
    }

    /// <summary>
    /// Checks if any defensive cooldown is active.
    /// </summary>
    public bool HasActiveMitigation(IBattleChara player)
    {
        return HasHolmgang(player) ||
               HasVengeance(player) ||
               HasBloodwhetting(player) ||
               HasThrillOfBattle(player) ||
               HasRampart(player) ||
               HasStatus(player, WARActions.StatusIds.ArmsLength);
    }

    /// <summary>
    /// Gets a string listing all active mitigations for debug display.
    /// </summary>
    public string GetActiveMitigations(IBattleChara player)
    {
        var active = new System.Collections.Generic.List<string>();

        if (HasHolmgang(player)) active.Add("Holmgang");
        if (HasVengeance(player)) active.Add("Vengeance");
        if (HasBloodwhetting(player)) active.Add("Bloodwhetting");
        if (HasThrillOfBattle(player)) active.Add("Thrill");
        if (HasRampart(player)) active.Add("Rampart");
        if (HasStatus(player, WARActions.StatusIds.ArmsLength)) active.Add("Arm's Length");

        return FormatActiveList(active);
    }

    #endregion

    // Core status methods (HasStatus, GetStatusRemaining, GetStatusStacks) inherited from BaseStatusHelper
}
