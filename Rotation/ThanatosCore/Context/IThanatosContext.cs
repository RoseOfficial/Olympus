using Olympus.Rotation.Common;
using Olympus.Rotation.Common.Helpers;
using Olympus.Rotation.ThanatosCore.Helpers;
using Olympus.Services.Party;

namespace Olympus.Rotation.ThanatosCore.Context;

/// <summary>
/// Reaper-specific rotation context interface.
/// Extends IMeleeDpsRotationContext with Reaper-specific state.
/// </summary>
public interface IThanatosContext : IMeleeDpsRotationContext
{
    #region Gauge State

    /// <summary>
    /// Current Soul gauge (0-100).
    /// Used for Blood Stalk, Gluttony, etc.
    /// </summary>
    int Soul { get; }

    /// <summary>
    /// Current Shroud gauge (0-100).
    /// Used for Enshroud.
    /// </summary>
    int Shroud { get; }

    /// <summary>
    /// Lemure Shroud stacks during Enshroud (0-5).
    /// Consumed by Void/Cross Reaping.
    /// </summary>
    int LemureShroud { get; }

    /// <summary>
    /// Void Shroud stacks during Enshroud (0-5).
    /// Built by Void/Cross Reaping, consumed by Lemure's Slice.
    /// </summary>
    int VoidShroud { get; }

    /// <summary>
    /// Whether currently in Enshroud state.
    /// </summary>
    bool IsEnshrouded { get; }

    /// <summary>
    /// Remaining time on Enshroud in seconds.
    /// </summary>
    float EnshroudTimer { get; }

    #endregion

    #region Soul Reaver State

    /// <summary>
    /// Whether Soul Reaver is active (enables Gibbet/Gallows/Guillotine).
    /// </summary>
    bool HasSoulReaver { get; }

    /// <summary>
    /// Number of Soul Reaver stacks (1-2 from Gluttony, 1 from Blood Stalk).
    /// </summary>
    int SoulReaverStacks { get; }

    /// <summary>
    /// Whether Enhanced Gibbet is active (use Gibbet for bonus).
    /// </summary>
    bool HasEnhancedGibbet { get; }

    /// <summary>
    /// Whether Enhanced Gallows is active (use Gallows for bonus).
    /// </summary>
    bool HasEnhancedGallows { get; }

    /// <summary>
    /// Whether Enhanced Void Reaping is active (use Void Reaping for bonus).
    /// </summary>
    bool HasEnhancedVoidReaping { get; }

    /// <summary>
    /// Whether Enhanced Cross Reaping is active (use Cross Reaping for bonus).
    /// </summary>
    bool HasEnhancedCrossReaping { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Arcane Circle party buff is active.
    /// </summary>
    bool HasArcaneCircle { get; }

    /// <summary>
    /// Remaining duration of Arcane Circle in seconds.
    /// </summary>
    float ArcaneCircleRemaining { get; }

    /// <summary>
    /// Whether Bloodsown Circle personal damage buff is active.
    /// </summary>
    bool HasBloodsownCircle { get; }

    /// <summary>
    /// Number of Immortal Sacrifice stacks (for Plentiful Harvest).
    /// </summary>
    int ImmortalSacrificeStacks { get; }

    /// <summary>
    /// Whether Soulsow buff is active (enables Harvest Moon).
    /// </summary>
    bool HasSoulsow { get; }

    #endregion

    #region Proc State

    /// <summary>
    /// Whether Perfectio Parata proc is ready (enables Perfectio).
    /// </summary>
    bool HasPerfectioParata { get; }

    /// <summary>
    /// Whether Oblatio proc is ready (enables Sacrificium).
    /// </summary>
    bool HasOblatio { get; }

    /// <summary>
    /// Whether Ideal Host buff is active (Dawntrail).
    /// </summary>
    bool HasIdealHost { get; }

    /// <summary>
    /// Whether Enhanced Harpe is active.
    /// </summary>
    bool HasEnhancedHarpe { get; }

    #endregion

    #region Target State

    /// <summary>
    /// Whether Death's Design debuff is active on current target.
    /// </summary>
    bool HasDeathsDesign { get; }

    /// <summary>
    /// Remaining duration of Death's Design on target in seconds.
    /// </summary>
    float DeathsDesignRemaining { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    ThanatosStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    MeleeDpsPartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    ThanatosDebugState Debug { get; }

    #endregion

    #region Party Coordination

    /// <summary>
    /// Party coordination service for raid buff synchronization.
    /// </summary>
    IPartyCoordinationService? PartyCoordinationService { get; }

    #endregion

    #region Training

    /// <summary>
    /// Service for Training Mode - captures and explains rotation decisions.
    /// Null if training mode is not available.
    /// </summary>
    Olympus.Services.Training.ITrainingService? TrainingService { get; }

    #endregion
}
