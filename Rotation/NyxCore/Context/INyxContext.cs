using Olympus.Rotation.Common;
using Olympus.Rotation.NyxCore.Helpers;

namespace Olympus.Rotation.NyxCore.Context;

/// <summary>
/// Dark Knight-specific rotation context interface.
/// Extends ITankRotationContext with Dark Knight-specific state.
/// </summary>
public interface INyxContext : ITankRotationContext
{
    #region Dark Knight Gauge

    /// <summary>
    /// Current Blood Gauge value (0-100).
    /// Used for Bloodspiller/Quietus and Living Shadow.
    /// </summary>
    int BloodGauge { get; }

    #endregion

    #region MP Resource

    /// <summary>
    /// Current MP value (0-10000).
    /// Used for Edge/Flood of Shadow and The Blackest Night.
    /// </summary>
    int CurrentMp { get; }

    /// <summary>
    /// Maximum MP value (10000).
    /// </summary>
    int MaxMp { get; }

    /// <summary>
    /// Whether the player has enough MP for The Blackest Night (>= 3000).
    /// </summary>
    bool HasEnoughMpForTbn { get; }

    /// <summary>
    /// Whether the player has enough MP for Edge/Flood of Shadow (>= 3000).
    /// </summary>
    bool HasEnoughMpForEdge { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Grit (tank stance) is currently active.
    /// </summary>
    bool HasGrit { get; }

    /// <summary>
    /// Whether Darkside (+10% damage) is currently active.
    /// Critical to maintain 100% uptime.
    /// </summary>
    bool HasDarkside { get; }

    /// <summary>
    /// Remaining duration of Darkside buff (seconds).
    /// Should refresh when below 10 seconds.
    /// </summary>
    float DarksideRemaining { get; }

    /// <summary>
    /// Whether Blood Weapon is currently active.
    /// Grants MP and Blood on weaponskill hit.
    /// </summary>
    bool HasBloodWeapon { get; }

    /// <summary>
    /// Remaining duration of Blood Weapon buff (seconds).
    /// </summary>
    float BloodWeaponRemaining { get; }

    /// <summary>
    /// Whether Delirium is currently active.
    /// Enables free Bloodspillers (Lv.68-95) or Scarlet Delirium combo (Lv.96+).
    /// </summary>
    bool HasDelirium { get; }

    /// <summary>
    /// Remaining stacks of Delirium (pre-Lv.96 only).
    /// Each Bloodspiller consumes one stack.
    /// </summary>
    int DeliriumStacks { get; }

    /// <summary>
    /// Whether Dark Arts is active (TBN broke - free Edge/Flood).
    /// High priority to consume for damage.
    /// </summary>
    bool HasDarkArts { get; }

    /// <summary>
    /// Whether Scornful Edge buff is active (from Torcleaver at Lv.96+).
    /// Enables Disesteem.
    /// </summary>
    bool HasScornfulEdge { get; }

    #endregion

    #region Defensive State

    /// <summary>
    /// Whether any defensive cooldown is currently active.
    /// </summary>
    bool HasActiveMitigation { get; }

    /// <summary>
    /// Whether Living Dead is active (cannot die).
    /// </summary>
    bool HasLivingDead { get; }

    /// <summary>
    /// Whether Walking Dead is active (MUST be healed to full or die).
    /// Critical state - skip mitigation and let healers work.
    /// </summary>
    bool HasWalkingDead { get; }

    /// <summary>
    /// Whether Shadow Wall/Shadowed Vigil is active.
    /// </summary>
    bool HasShadowWall { get; }

    /// <summary>
    /// Whether Dark Mind is active (magic damage reduction).
    /// </summary>
    bool HasDarkMind { get; }

    /// <summary>
    /// Whether The Blackest Night shield is currently active.
    /// </summary>
    bool HasTheBlackestNight { get; }

    /// <summary>
    /// Whether Oblation is active.
    /// </summary>
    bool HasOblation { get; }

    #endregion

    #region Living Shadow

    /// <summary>
    /// Whether Living Shadow (clone) is currently active.
    /// </summary>
    bool HasLivingShadow { get; }

    #endregion

    #region Ground DoT

    /// <summary>
    /// Whether Salted Earth ground DoT is currently active.
    /// </summary>
    bool HasSaltedEarth { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    NyxStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    NyxPartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    NyxDebugState Debug { get; }

    #endregion
}
