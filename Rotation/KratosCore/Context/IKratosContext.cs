using Olympus.Rotation.Common;
using Olympus.Rotation.KratosCore.Helpers;

namespace Olympus.Rotation.KratosCore.Context;

/// <summary>
/// Monk-specific rotation context interface.
/// Extends IMeleeDpsRotationContext with Monk-specific state.
/// </summary>
public interface IKratosContext : IMeleeDpsRotationContext
{
    #region Form State

    /// <summary>
    /// Current form (Opo-opo, Raptor, Coeurl, or Formless).
    /// </summary>
    MonkForm CurrentForm { get; }

    /// <summary>
    /// Whether the player has Formless Fist (can use any form GCD).
    /// </summary>
    bool HasFormlessFist { get; }

    /// <summary>
    /// Whether the player has Perfect Balance active.
    /// </summary>
    bool HasPerfectBalance { get; }

    /// <summary>
    /// Remaining stacks of Perfect Balance.
    /// </summary>
    int PerfectBalanceStacks { get; }

    #endregion

    #region Chakra Gauge

    /// <summary>
    /// Current Chakra count (0-5).
    /// Used for Forbidden Chakra/Enlightenment.
    /// </summary>
    int Chakra { get; }

    /// <summary>
    /// First Beast Chakra type (0=None, 1=Opo, 2=Raptor, 3=Coeurl).
    /// </summary>
    byte BeastChakra1 { get; }

    /// <summary>
    /// Second Beast Chakra type.
    /// </summary>
    byte BeastChakra2 { get; }

    /// <summary>
    /// Third Beast Chakra type.
    /// </summary>
    byte BeastChakra3 { get; }

    /// <summary>
    /// Count of Beast Chakra accumulated (0-3).
    /// </summary>
    int BeastChakraCount { get; }

    /// <summary>
    /// Whether Lunar Nadi is active.
    /// </summary>
    bool HasLunarNadi { get; }

    /// <summary>
    /// Whether Solar Nadi is active.
    /// </summary>
    bool HasSolarNadi { get; }

    /// <summary>
    /// Whether both Nadi are active (ready for Phantom Rush).
    /// </summary>
    bool HasBothNadi { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Disciplined Fist (+15% damage) is active.
    /// </summary>
    bool HasDisciplinedFist { get; }

    /// <summary>
    /// Remaining duration of Disciplined Fist (seconds).
    /// </summary>
    float DisciplinedFistRemaining { get; }

    /// <summary>
    /// Whether Leaden Fist (next Bootshine upgrade) is active.
    /// </summary>
    bool HasLeadenFist { get; }

    /// <summary>
    /// Whether Riddle of Fire is active.
    /// </summary>
    bool HasRiddleOfFire { get; }

    /// <summary>
    /// Remaining duration of Riddle of Fire (seconds).
    /// </summary>
    float RiddleOfFireRemaining { get; }

    /// <summary>
    /// Whether Brotherhood buff is active.
    /// </summary>
    bool HasBrotherhood { get; }

    /// <summary>
    /// Whether Riddle of Wind is active.
    /// </summary>
    bool HasRiddleOfWind { get; }

    #endregion

    #region Proc State

    /// <summary>
    /// Whether Raptor's Fury proc is active (from Opo-opo form).
    /// </summary>
    bool HasRaptorsFury { get; }

    /// <summary>
    /// Whether Coeurl's Fury proc is active (from Raptor form).
    /// </summary>
    bool HasCoeurlsFury { get; }

    /// <summary>
    /// Whether Opo-opo's Fury proc is active (from Coeurl form).
    /// </summary>
    bool HasOpooposFury { get; }

    /// <summary>
    /// Whether Fire's Rumination proc is ready (after Riddle of Fire).
    /// </summary>
    bool HasFiresRumination { get; }

    /// <summary>
    /// Whether Wind's Rumination proc is ready (after Riddle of Wind).
    /// </summary>
    bool HasWindsRumination { get; }

    #endregion

    #region DoT State

    /// <summary>
    /// Whether Demolish DoT is active on the target.
    /// </summary>
    bool HasDemolishOnTarget { get; }

    /// <summary>
    /// Remaining duration of Demolish on target (seconds).
    /// </summary>
    float DemolishRemaining { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    KratosStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    KratosPartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    KratosDebugState Debug { get; }

    #endregion
}

/// <summary>
/// Monk form types.
/// </summary>
public enum MonkForm
{
    None = 0,
    OpoOpo = 1,
    Raptor = 2,
    Coeurl = 3,
    Formless = 4
}
