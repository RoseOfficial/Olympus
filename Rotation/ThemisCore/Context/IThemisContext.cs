using Olympus.Rotation.Common;
using Olympus.Rotation.ThemisCore.Helpers;

namespace Olympus.Rotation.ThemisCore.Context;

/// <summary>
/// Paladin-specific rotation context interface.
/// Extends ITankRotationContext with Paladin-specific state.
/// </summary>
public interface IThemisContext : ITankRotationContext
{
    #region Paladin Gauge

    /// <summary>
    /// Current Oath Gauge value (0-100).
    /// Used for Sheltron/Holy Sheltron.
    /// </summary>
    int OathGauge { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Fight or Flight is currently active.
    /// </summary>
    bool HasFightOrFlight { get; }

    /// <summary>
    /// Remaining duration of Fight or Flight buff (seconds).
    /// </summary>
    float FightOrFlightRemaining { get; }

    /// <summary>
    /// Whether Requiescat is currently active.
    /// </summary>
    bool HasRequiescat { get; }

    /// <summary>
    /// Remaining stacks of Requiescat.
    /// Each Holy Spirit/Circle consumes one stack.
    /// </summary>
    int RequiescatStacks { get; }

    /// <summary>
    /// Whether Sword Oath is active (enables Atonement chain).
    /// </summary>
    bool HasSwordOath { get; }

    /// <summary>
    /// Remaining stacks of Sword Oath.
    /// </summary>
    int SwordOathStacks { get; }

    /// <summary>
    /// Current position in the Atonement chain (0-3).
    /// 0 = not in chain, 1 = Atonement ready, 2 = Supplication ready, 3 = Sepulchre ready
    /// </summary>
    int AtonementStep { get; }

    /// <summary>
    /// Current position in the Confiteor chain (0-4).
    /// 0 = not in chain, 1 = Confiteor ready, 2 = Faith, 3 = Truth, 4 = Valor
    /// </summary>
    int ConfiteorStep { get; }

    /// <summary>
    /// Whether Blade of Honor is ready (after Goring Blade during FoF).
    /// </summary>
    bool HasBladeOfHonor { get; }

    #endregion

    #region Defensive State

    /// <summary>
    /// Whether any defensive cooldown is currently active.
    /// </summary>
    bool HasActiveMitigation { get; }

    /// <summary>
    /// Whether Hallowed Ground (invulnerability) is active.
    /// </summary>
    bool HasHallowedGround { get; }

    #endregion

    #region DoT State

    /// <summary>
    /// Remaining duration of Goring Blade DoT on current target (seconds).
    /// Returns 0 if no DoT or no target.
    /// </summary>
    float GoringBladeRemaining { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    ThemisStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    ThemisPartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    ThemisDebugState Debug { get; }

    #endregion
}
