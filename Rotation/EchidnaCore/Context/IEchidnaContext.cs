using Olympus.Data;
using Olympus.Rotation.Common;
using Olympus.Rotation.EchidnaCore.Helpers;

namespace Olympus.Rotation.EchidnaCore.Context;

/// <summary>
/// Viper-specific rotation context interface.
/// Extends IMeleeDpsRotationContext with Viper-specific state.
/// </summary>
public interface IEchidnaContext : IMeleeDpsRotationContext
{
    #region Gauge State

    /// <summary>
    /// Current Serpent Offering gauge (0-100).
    /// Used for Reawaken (requires 50).
    /// </summary>
    int SerpentOffering { get; }

    /// <summary>
    /// Anguine Tribute stacks during Reawaken (0-5).
    /// Consumed by Generations, ends with Ouroboros.
    /// </summary>
    int AnguineTribute { get; }

    /// <summary>
    /// Rattling Coil stacks (0-3).
    /// Built by Vicewinder/Serpent's Ire, consumed by Uncoiled Fury.
    /// </summary>
    int RattlingCoils { get; }

    /// <summary>
    /// Whether currently in Reawakened state.
    /// </summary>
    bool IsReawakened { get; }

    /// <summary>
    /// Current DreadCombo state from the job gauge.
    /// Tracks twinblade combo progression.
    /// </summary>
    VPRActions.DreadCombo DreadCombo { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Hunter's Instinct buff is active (+10% damage).
    /// </summary>
    bool HasHuntersInstinct { get; }

    /// <summary>
    /// Remaining duration of Hunter's Instinct in seconds.
    /// </summary>
    float HuntersInstinctRemaining { get; }

    /// <summary>
    /// Whether Swiftscaled buff is active (-15% GCD).
    /// </summary>
    bool HasSwiftscaled { get; }

    /// <summary>
    /// Remaining duration of Swiftscaled in seconds.
    /// </summary>
    float SwiftscaledRemaining { get; }

    /// <summary>
    /// Whether Honed Steel buff is active (enhances Steel Fangs).
    /// </summary>
    bool HasHonedSteel { get; }

    /// <summary>
    /// Whether Honed Reavers buff is active (enhances Reaving Fangs).
    /// </summary>
    bool HasHonedReavers { get; }

    /// <summary>
    /// Whether Ready to Reawaken proc is active (from Serpent's Ire).
    /// </summary>
    bool HasReadyToReawaken { get; }

    #endregion

    #region Venom Buffs (Positional Tracking)

    /// <summary>
    /// Whether Flankstung Venom is active (use rear for bonus).
    /// </summary>
    bool HasFlankstungVenom { get; }

    /// <summary>
    /// Whether Hindstung Venom is active (use flank for bonus).
    /// </summary>
    bool HasHindstungVenom { get; }

    /// <summary>
    /// Whether Flanksbane Venom is active (use rear for bonus).
    /// </summary>
    bool HasFlanksbaneVenom { get; }

    /// <summary>
    /// Whether Hindsbane Venom is active (use flank for bonus).
    /// </summary>
    bool HasHindsbaneVenom { get; }

    /// <summary>
    /// Whether Grimskin's Venom is active (AoE buff).
    /// </summary>
    bool HasGrimskinsVenom { get; }

    /// <summary>
    /// Whether Grimhunter's Venom is active (AoE buff).
    /// </summary>
    bool HasGrimhuntersVenom { get; }

    #endregion

    #region oGCD Proc State

    /// <summary>
    /// Whether Poised for Twinfang is active.
    /// </summary>
    bool HasPoisedForTwinfang { get; }

    /// <summary>
    /// Whether Poised for Twinblood is active.
    /// </summary>
    bool HasPoisedForTwinblood { get; }

    #endregion

    #region Target State

    /// <summary>
    /// Whether Noxious Gnash debuff is active on current target.
    /// </summary>
    bool HasNoxiousGnash { get; }

    /// <summary>
    /// Remaining duration of Noxious Gnash on target in seconds.
    /// </summary>
    float NoxiousGnashRemaining { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    EchidnaStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    EchidnaPartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    EchidnaDebugState Debug { get; }

    #endregion
}
