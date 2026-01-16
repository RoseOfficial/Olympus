using Olympus.Rotation.Common;
using Olympus.Rotation.HephaestusCore.Helpers;

namespace Olympus.Rotation.HephaestusCore.Context;

/// <summary>
/// Gunbreaker-specific rotation context interface.
/// Extends ITankRotationContext with Gunbreaker-specific state.
/// </summary>
public interface IHephaestusContext : ITankRotationContext
{
    #region Cartridge Gauge

    /// <summary>
    /// Current Cartridge count (0-3, up to 6 with Bloodfest Ready).
    /// </summary>
    int Cartridges { get; }

    /// <summary>
    /// Whether the player has maximum cartridges (3).
    /// </summary>
    bool HasMaxCartridges { get; }

    /// <summary>
    /// Whether the player has at least 1 cartridge for Gnashing Fang or Burst Strike.
    /// </summary>
    bool CanUseGnashingFang { get; }

    /// <summary>
    /// Whether the player has at least 2 cartridges for Double Down.
    /// </summary>
    bool CanUseDoubleDown { get; }

    #endregion

    #region Continuation Ready States

    /// <summary>
    /// Whether Ready to Rip is active (follow-up to Gnashing Fang).
    /// Must use before next GCD or proc expires.
    /// </summary>
    bool IsReadyToRip { get; }

    /// <summary>
    /// Whether Ready to Tear is active (follow-up to Savage Claw).
    /// Must use before next GCD or proc expires.
    /// </summary>
    bool IsReadyToTear { get; }

    /// <summary>
    /// Whether Ready to Gouge is active (follow-up to Wicked Talon).
    /// Must use before next GCD or proc expires.
    /// </summary>
    bool IsReadyToGouge { get; }

    /// <summary>
    /// Whether Ready to Blast is active (follow-up to Burst Strike, Lv.86+).
    /// Must use before next GCD or proc expires.
    /// </summary>
    bool IsReadyToBlast { get; }

    /// <summary>
    /// Whether Ready to Reign is active (from Bloodfest at Lv.100).
    /// Enables Reign of Beasts combo.
    /// </summary>
    bool IsReadyToReign { get; }

    /// <summary>
    /// Whether any Continuation action is ready.
    /// </summary>
    bool HasAnyContinuationReady { get; }

    #endregion

    #region Gnashing Fang Combo State

    /// <summary>
    /// Current step in Gnashing Fang combo (0=none, 1=after GF, 2=after SC, 3=complete).
    /// </summary>
    int GnashingFangStep { get; }

    /// <summary>
    /// Whether currently in the middle of a Gnashing Fang combo.
    /// </summary>
    bool IsInGnashingFangCombo { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Royal Guard (tank stance) is currently active.
    /// </summary>
    bool HasRoyalGuard { get; }

    /// <summary>
    /// Whether No Mercy (+20% damage) is currently active.
    /// </summary>
    bool HasNoMercy { get; }

    /// <summary>
    /// Remaining duration of No Mercy buff (seconds).
    /// </summary>
    float NoMercyRemaining { get; }

    #endregion

    #region Defensive State

    /// <summary>
    /// Whether any defensive cooldown is currently active.
    /// </summary>
    bool HasActiveMitigation { get; }

    /// <summary>
    /// Whether Superbolide is active (invulnerability).
    /// </summary>
    bool HasSuperbolide { get; }

    /// <summary>
    /// Whether Nebula/Great Nebula is active.
    /// </summary>
    bool HasNebula { get; }

    /// <summary>
    /// Whether Heart of Stone/Corundum is active.
    /// </summary>
    bool HasHeartOfCorundum { get; }

    /// <summary>
    /// Whether Camouflage is active.
    /// </summary>
    bool HasCamouflage { get; }

    /// <summary>
    /// Whether Aurora HoT is active.
    /// </summary>
    bool HasAurora { get; }

    #endregion

    #region DoT State

    /// <summary>
    /// Whether Sonic Break DoT is currently active on the target.
    /// </summary>
    bool HasSonicBreakDot { get; }

    /// <summary>
    /// Whether Bow Shock DoT is currently active on the target.
    /// </summary>
    bool HasBowShockDot { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    HephaestusStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    HephaestusPartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    HephaestusDebugState Debug { get; }

    #endregion
}
