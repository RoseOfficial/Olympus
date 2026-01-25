using Olympus.Rotation.Common;
using Olympus.Rotation.PersephoneCore.Helpers;
using Olympus.Services.Party;
using Olympus.Services.Training;

namespace Olympus.Rotation.PersephoneCore.Context;

/// <summary>
/// Summoner-specific rotation context interface.
/// Extends ICasterDpsRotationContext with Summoner-specific state.
/// Named after Persephone, the Greek queen of the underworld who commands souls.
/// </summary>
public interface IPersephoneContext : ICasterDpsRotationContext
{
    #region Demi-Summon State

    /// <summary>
    /// Whether Demi-Bahamut is currently active.
    /// </summary>
    bool IsBahamutActive { get; }

    /// <summary>
    /// Whether Demi-Phoenix is currently active.
    /// </summary>
    bool IsPhoenixActive { get; }

    /// <summary>
    /// Whether Solar Bahamut is currently active.
    /// </summary>
    bool IsSolarBahamutActive { get; }

    /// <summary>
    /// Whether any demi-summon is currently active.
    /// </summary>
    bool IsDemiSummonActive { get; }

    /// <summary>
    /// Remaining duration of the current demi-summon in seconds.
    /// </summary>
    float DemiSummonTimer { get; }

    /// <summary>
    /// Estimated GCDs remaining in the current demi-summon phase.
    /// </summary>
    int DemiSummonGcdsRemaining { get; }

    #endregion

    #region Primal Attunement State

    /// <summary>
    /// Current attunement type (0=None, 1=Ifrit/Ruby, 2=Titan/Topaz, 3=Garuda/Emerald).
    /// </summary>
    int CurrentAttunement { get; }

    /// <summary>
    /// Remaining attunement stacks for current primal.
    /// Ruby = 2 stacks, Topaz/Emerald = 4 stacks.
    /// </summary>
    int AttunementStacks { get; }

    /// <summary>
    /// Remaining attunement timer in seconds.
    /// </summary>
    float AttunementTimer { get; }

    /// <summary>
    /// Whether currently attuned to Ifrit (Ruby Arcanum).
    /// </summary>
    bool IsIfritAttuned { get; }

    /// <summary>
    /// Whether currently attuned to Titan (Topaz Arcanum).
    /// </summary>
    bool IsTitanAttuned { get; }

    /// <summary>
    /// Whether currently attuned to Garuda (Emerald Arcanum).
    /// </summary>
    bool IsGarudaAttuned { get; }

    #endregion

    #region Primal Availability

    /// <summary>
    /// Whether Ifrit can be summoned.
    /// </summary>
    bool CanSummonIfrit { get; }

    /// <summary>
    /// Whether Titan can be summoned.
    /// </summary>
    bool CanSummonTitan { get; }

    /// <summary>
    /// Whether Garuda can be summoned.
    /// </summary>
    bool CanSummonGaruda { get; }

    /// <summary>
    /// Count of primals available to summon (0-3).
    /// </summary>
    int PrimalsAvailable { get; }

    #endregion

    #region Aetherflow State

    /// <summary>
    /// Current Aetherflow stacks (0-2).
    /// </summary>
    int AetherflowStacks { get; }

    /// <summary>
    /// Whether Aetherflow stacks are available.
    /// </summary>
    bool HasAetherflow { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Further Ruin buff is active (enables Ruin IV).
    /// </summary>
    bool HasFurtherRuin { get; }

    /// <summary>
    /// Remaining duration of Further Ruin buff.
    /// </summary>
    float FurtherRuinRemaining { get; }

    /// <summary>
    /// Whether Searing Light party buff is active.
    /// </summary>
    bool HasSearingLight { get; }

    /// <summary>
    /// Remaining duration of Searing Light buff.
    /// </summary>
    float SearingLightRemaining { get; }

    /// <summary>
    /// Whether Ifrit's Favor buff is active (enables Crimson Cyclone).
    /// </summary>
    bool HasIfritsFavor { get; }

    /// <summary>
    /// Whether Titan's Favor buff is active (enables Mountain Buster).
    /// </summary>
    bool HasTitansFavor { get; }

    /// <summary>
    /// Whether Garuda's Favor buff is active (enables Slipstream).
    /// </summary>
    bool HasGarudasFavor { get; }

    /// <summary>
    /// Whether Radiant Aegis shield is active.
    /// </summary>
    bool HasRadiantAegis { get; }

    #endregion

    #region Cooldown State

    /// <summary>
    /// Whether Searing Light is ready to use.
    /// </summary>
    bool SearingLightReady { get; }

    /// <summary>
    /// Whether Energy Drain is ready to use.
    /// </summary>
    bool EnergyDrainReady { get; }

    /// <summary>
    /// Whether Enkindle is ready (for current demi-summon).
    /// </summary>
    bool EnkindleReady { get; }

    /// <summary>
    /// Whether Astral Flow ability is ready (Deathflare/Rekindle/Sunflare).
    /// </summary>
    bool AstralFlowReady { get; }

    /// <summary>
    /// Number of Radiant Aegis charges available.
    /// </summary>
    int RadiantAegisCharges { get; }

    /// <summary>
    /// Whether Swiftcast is ready to use.
    /// </summary>
    bool SwiftcastReady { get; }

    /// <summary>
    /// Whether Lucid Dreaming is ready to use.
    /// </summary>
    bool LucidDreamingReady { get; }

    #endregion

    #region Summoner State Tracking

    /// <summary>
    /// Whether we have used Enkindle during the current demi-summon phase.
    /// </summary>
    bool HasUsedEnkindleThisPhase { get; }

    /// <summary>
    /// Whether we have used Astral Flow during the current demi-summon phase.
    /// </summary>
    bool HasUsedAstralFlowThisPhase { get; }

    /// <summary>
    /// Whether a pet/summon is currently active.
    /// </summary>
    bool HasPetSummoned { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    PersephoneStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    PersephonePartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    PersephoneDebugState Debug { get; }

    #endregion

    #region Party Coordination

    /// <summary>
    /// Service for coordinating raid buffs with other Olympus instances.
    /// Null if party coordination is disabled or unavailable.
    /// </summary>
    IPartyCoordinationService? PartyCoordinationService { get; }

    #endregion

    #region Training

    /// <summary>
    /// Service for recording training decisions and explanations.
    /// Null if Training Mode is disabled.
    /// </summary>
    ITrainingService? TrainingService { get; }

    #endregion
}
