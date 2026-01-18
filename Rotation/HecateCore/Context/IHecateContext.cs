using Olympus.Rotation.Common;
using Olympus.Rotation.HecateCore.Helpers;

namespace Olympus.Rotation.HecateCore.Context;

/// <summary>
/// Black Mage-specific rotation context interface.
/// Extends ICasterDpsRotationContext with Black Mage-specific state.
/// </summary>
public interface IHecateContext : ICasterDpsRotationContext
{
    #region Element State

    /// <summary>
    /// Whether currently in Astral Fire (fire phase).
    /// </summary>
    bool InAstralFire { get; }

    /// <summary>
    /// Whether currently in Umbral Ice (ice phase).
    /// </summary>
    bool InUmbralIce { get; }

    /// <summary>
    /// Current element stacks (positive = Astral Fire 1-3, negative = Umbral Ice 1-3).
    /// </summary>
    int ElementStacks { get; }

    /// <summary>
    /// Remaining duration of current element state in seconds.
    /// When this reaches 0, Enochian drops and element is lost.
    /// </summary>
    float ElementTimer { get; }

    /// <summary>
    /// Whether Enochian is active (element timer > 0).
    /// </summary>
    bool IsEnochianActive { get; }

    /// <summary>
    /// Number of Astral Fire stacks (0-3, positive only).
    /// </summary>
    int AstralFireStacks { get; }

    /// <summary>
    /// Number of Umbral Ice stacks (0-3, positive only).
    /// </summary>
    int UmbralIceStacks { get; }

    #endregion

    #region Resource State

    /// <summary>
    /// Number of Umbral Hearts (0-3).
    /// Used to reduce Fire IV MP cost in Astral Fire.
    /// </summary>
    int UmbralHearts { get; }

    /// <summary>
    /// Number of Polyglot stacks (0-3).
    /// Generated every 30s while Enochian is active.
    /// Spent on Xenoglossy (ST) or Foul (AoE).
    /// </summary>
    int PolyglotStacks { get; }

    /// <summary>
    /// Number of Astral Soul stacks (0-6).
    /// Built by Fire IV, spent on Flare Star at 6 stacks.
    /// </summary>
    int AstralSoulStacks { get; }

    /// <summary>
    /// Whether Paradox marker is active.
    /// Grants access to Paradox spell.
    /// </summary>
    bool HasParadox { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Firestarter proc is active (instant Fire III).
    /// </summary>
    bool HasFirestarter { get; }

    /// <summary>
    /// Remaining duration of Firestarter proc.
    /// </summary>
    float FirestarterRemaining { get; }

    /// <summary>
    /// Whether Thunderhead proc is active (instant Thunder).
    /// </summary>
    bool HasThunderhead { get; }

    /// <summary>
    /// Remaining duration of Thunderhead proc.
    /// </summary>
    float ThunderheadRemaining { get; }

    /// <summary>
    /// Whether Ley Lines buff is active (casting speed increase).
    /// </summary>
    bool HasLeyLines { get; }

    /// <summary>
    /// Remaining duration of Ley Lines.
    /// </summary>
    float LeyLinesRemaining { get; }

    /// <summary>
    /// Number of Triplecast stacks remaining (0-3).
    /// </summary>
    int TriplecastStacksRemaining { get; }

    #endregion

    #region Target State

    /// <summary>
    /// Whether Thunder DoT is active on current target.
    /// </summary>
    bool HasThunderDoT { get; }

    /// <summary>
    /// Remaining duration of Thunder DoT on target in seconds.
    /// </summary>
    float ThunderDoTRemaining { get; }

    #endregion

    #region Movement State

    /// <summary>
    /// Whether the rotation needs an instant cast for movement.
    /// True when moving and no instant cast buff available.
    /// </summary>
    bool NeedsInstant { get; }

    #endregion

    #region Cooldown Tracking

    /// <summary>
    /// Charges of Triplecast available (0-2).
    /// </summary>
    int TriplecastCharges { get; }

    /// <summary>
    /// Whether Swiftcast is available.
    /// </summary>
    bool SwiftcastReady { get; }

    /// <summary>
    /// Whether Manafont is available.
    /// </summary>
    bool ManafontReady { get; }

    /// <summary>
    /// Whether Amplifier is available.
    /// </summary>
    bool AmplifierReady { get; }

    /// <summary>
    /// Whether Ley Lines is available.
    /// </summary>
    bool LeyLinesReady { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    HecateStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    HecatePartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    HecateDebugState Debug { get; }

    #endregion
}
