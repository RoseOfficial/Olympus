using Olympus.Rotation.ApolloCore.Helpers;
using Olympus.Rotation.AsclepiusCore.Helpers;
using Olympus.Rotation.Common;
using Olympus.Services.Healing;
using Olympus.Services.Party;
using Olympus.Services.Prediction;
using Olympus.Services.Sage;

namespace Olympus.Rotation.AsclepiusCore.Context;

/// <summary>
/// Interface for Asclepius (Sage) context.
/// Extends the healer rotation context with SGE-specific properties.
/// </summary>
public interface IAsclepiusContext : IHealerRotationContext
{
    #region SGE-Specific Services

    /// <summary>
    /// Service for tracking Addersgall resource (0-3 stacks, 20s passive regen).
    /// Timer pauses at max stacks.
    /// </summary>
    IAddersgallTrackingService AddersgallService { get; }

    /// <summary>
    /// Service for tracking Addersting resource (0-3 stacks, from E.Diagnosis shield breaks).
    /// Used for Toxikon.
    /// </summary>
    IAdderstingTrackingService AdderstingService { get; }

    /// <summary>
    /// Service for managing Kardia target and Soteria/Philosophia tracking.
    /// </summary>
    IKardiaManager KardiaManager { get; }

    /// <summary>
    /// Service for tracking Eukrasia buff state.
    /// </summary>
    IEukrasiaStateService EukrasiaService { get; }

    #endregion

    #region SGE-Specific Helpers

    /// <summary>
    /// Helper for SGE-specific status checks.
    /// </summary>
    AsclepiusStatusHelper StatusHelper { get; }

    /// <summary>
    /// Helper for party health analysis.
    /// </summary>
    IPartyHelper PartyHelper { get; }

    #endregion

    #region Debug State

    /// <summary>
    /// Debug state for UI display.
    /// </summary>
    AsclepiusDebugState Debug { get; }

    #endregion

    #region Healing Coordination

    /// <summary>
    /// Frame-scoped coordination state to prevent multiple handlers from targeting the same entity.
    /// </summary>
    HealingCoordinationState HealingCoordination { get; }

    #endregion

    #region Smart Healing Services

    /// <summary>
    /// Service for tracking co-healer presence and activity.
    /// Null if co-healer awareness is disabled or not available.
    /// </summary>
    ICoHealerDetectionService? CoHealerDetectionService { get; }

    /// <summary>
    /// Detector for boss mechanic patterns (raidwides, tank busters).
    /// Null if mechanic awareness is disabled or not available.
    /// </summary>
    IBossMechanicDetector? BossMechanicDetector { get; }

    /// <summary>
    /// Service for tracking shields and mitigation buffs on party members.
    /// </summary>
    IShieldTrackingService? ShieldTrackingService { get; }

    #endregion

    #region SGE Resource Properties (Cached)

    /// <summary>
    /// Current Addersgall stack count (0-3).
    /// </summary>
    int AddersgallStacks { get; }

    /// <summary>
    /// Time remaining until next Addersgall stack generates.
    /// Returns 0 if at max stacks (timer paused).
    /// </summary>
    float AddersgallTimer { get; }

    /// <summary>
    /// Current Addersting stack count (0-3).
    /// </summary>
    int AdderstingStacks { get; }

    /// <summary>
    /// Whether Eukrasia buff is currently active.
    /// </summary>
    bool HasEukrasia { get; }

    /// <summary>
    /// Whether Zoe buff is currently active (+50% next GCD heal).
    /// </summary>
    bool HasZoe { get; }

    /// <summary>
    /// Whether Kardia is currently placed on a target.
    /// </summary>
    bool HasKardiaPlaced { get; }

    /// <summary>
    /// Object ID of current Kardia target, or 0 if none.
    /// </summary>
    ulong KardiaTargetId { get; }

    /// <summary>
    /// Whether Kardia swap is off cooldown.
    /// </summary>
    bool CanSwapKardia { get; }

    /// <summary>
    /// Whether Soteria is currently active (boosted Kardia heals).
    /// </summary>
    bool HasSoteria { get; }

    /// <summary>
    /// Whether Philosophia is currently active (party-wide Kardia).
    /// </summary>
    bool HasPhilosophia { get; }

    #endregion

    #region Logging Helpers

    /// <summary>
    /// Logs a healing decision for debugging.
    /// </summary>
    void LogHealDecision(string targetName, float hpPercent, string spellName, int predictedHeal, string reason);

    /// <summary>
    /// Logs an oGCD healing decision.
    /// </summary>
    void LogOgcdDecision(string targetName, float hpPercent, string spellName, string reason);

    /// <summary>
    /// Logs a defensive cooldown decision.
    /// </summary>
    void LogDefensiveDecision(string targetName, float hpPercent, string spellName, float damageRate, string reason);

    /// <summary>
    /// Logs an Addersgall spending decision.
    /// </summary>
    void LogAddersgallDecision(string spellName, int stacksBefore, string reason);

    /// <summary>
    /// Logs a Kardia-related decision.
    /// </summary>
    void LogKardiaDecision(string targetName, string action, string reason);

    #endregion
}
