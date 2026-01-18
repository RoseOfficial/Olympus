using Olympus.Rotation.Common;
using Olympus.Rotation.CirceCore.Helpers;

namespace Olympus.Rotation.CirceCore.Context;

/// <summary>
/// Red Mage-specific rotation context interface.
/// Extends ICasterDpsRotationContext with Red Mage-specific state.
/// Named after Circe, the Greek goddess of sorcery who transformed her enemies.
/// </summary>
public interface ICirceContext : ICasterDpsRotationContext
{
    #region Mana State

    /// <summary>
    /// Current Black Mana value (0-100).
    /// </summary>
    int BlackMana { get; }

    /// <summary>
    /// Current White Mana value (0-100).
    /// </summary>
    int WhiteMana { get; }

    /// <summary>
    /// Mana imbalance (Black - White). Positive = more Black, Negative = more White.
    /// </summary>
    int ManaImbalance { get; }

    /// <summary>
    /// Absolute value of mana imbalance.
    /// </summary>
    int AbsoluteManaImbalance { get; }

    /// <summary>
    /// Whether mana is within safe balance (within 30 difference).
    /// </summary>
    bool IsManaBalanced { get; }

    /// <summary>
    /// Current Mana Stack count (0-3) for melee finisher tracking.
    /// </summary>
    int ManaStacks { get; }

    /// <summary>
    /// Whether both mana values are >= 50 (can start melee combo).
    /// </summary>
    bool CanStartMeleeCombo { get; }

    /// <summary>
    /// The lower of the two mana values.
    /// </summary>
    int LowerMana { get; }

    #endregion

    #region Dualcast State

    /// <summary>
    /// Whether Dualcast buff is active (next spell is instant).
    /// </summary>
    bool HasDualcast { get; }

    /// <summary>
    /// Remaining duration of Dualcast buff in seconds.
    /// </summary>
    float DualcastRemaining { get; }

    /// <summary>
    /// Whether we should perform a hardcast (no Dualcast/Swiftcast/Acceleration).
    /// </summary>
    bool ShouldHardcast { get; }

    #endregion

    #region Proc State

    /// <summary>
    /// Whether Verfire Ready buff is active.
    /// </summary>
    bool HasVerfire { get; }

    /// <summary>
    /// Remaining duration of Verfire Ready buff in seconds.
    /// </summary>
    float VerfireRemaining { get; }

    /// <summary>
    /// Whether Verstone Ready buff is active.
    /// </summary>
    bool HasVerstone { get; }

    /// <summary>
    /// Remaining duration of Verstone Ready buff in seconds.
    /// </summary>
    float VerstoneRemaining { get; }

    /// <summary>
    /// Whether either proc is active.
    /// </summary>
    bool HasAnyProc { get; }

    /// <summary>
    /// Whether both procs are active.
    /// </summary>
    bool HasBothProcs { get; }

    #endregion

    #region Melee Combo State

    /// <summary>
    /// Whether currently in melee combo (after Riposte).
    /// </summary>
    bool IsInMeleeCombo { get; }

    /// <summary>
    /// Current step in melee combo (0=None, 1=Zwerchhau, 2=Redoublement).
    /// </summary>
    int MeleeComboStep { get; }

    /// <summary>
    /// Whether finisher is ready (after Redoublement).
    /// </summary>
    bool IsFinisherReady { get; }

    /// <summary>
    /// Whether Scorch is ready (after Verflare/Verholy).
    /// </summary>
    bool IsScorchReady { get; }

    /// <summary>
    /// Whether Resolution is ready (after Scorch).
    /// </summary>
    bool IsResolutionReady { get; }

    /// <summary>
    /// Whether Grand Impact is ready.
    /// </summary>
    bool IsGrandImpactReady { get; }

    #endregion

    #region Buff State

    /// <summary>
    /// Whether Embolden party buff is active.
    /// </summary>
    bool HasEmbolden { get; }

    /// <summary>
    /// Remaining duration of Embolden buff in seconds.
    /// </summary>
    float EmboldenRemaining { get; }

    /// <summary>
    /// Whether Manafication buff is active.
    /// </summary>
    bool HasManafication { get; }

    /// <summary>
    /// Remaining duration of Manafication buff in seconds.
    /// </summary>
    float ManaficationRemaining { get; }

    /// <summary>
    /// Whether Acceleration buff is active.
    /// </summary>
    bool HasAcceleration { get; }

    /// <summary>
    /// Remaining duration of Acceleration buff in seconds.
    /// </summary>
    float AccelerationRemaining { get; }

    /// <summary>
    /// Whether Thorned Flourish buff is active (Vice of Thorns ready).
    /// </summary>
    bool HasThornedFlourish { get; }

    /// <summary>
    /// Whether Prefulgence Ready buff is active.
    /// </summary>
    bool HasPrefulgenceReady { get; }

    /// <summary>
    /// Whether Magick Barrier is active on self.
    /// </summary>
    bool HasMagickBarrier { get; }

    #endregion

    #region Cooldown State

    /// <summary>
    /// Whether Fleche is ready to use.
    /// </summary>
    bool FlecheReady { get; }

    /// <summary>
    /// Whether Contre Sixte is ready to use.
    /// </summary>
    bool ContreSixteReady { get; }

    /// <summary>
    /// Whether Embolden is ready to use.
    /// </summary>
    bool EmboldenReady { get; }

    /// <summary>
    /// Whether Manafication is ready to use.
    /// </summary>
    bool ManaficationReady { get; }

    /// <summary>
    /// Number of Corps-a-corps charges available.
    /// </summary>
    int CorpsACorpsCharges { get; }

    /// <summary>
    /// Number of Engagement charges available.
    /// </summary>
    int EngagementCharges { get; }

    /// <summary>
    /// Number of Acceleration charges available.
    /// </summary>
    int AccelerationCharges { get; }

    /// <summary>
    /// Whether Swiftcast is ready to use.
    /// </summary>
    bool SwiftcastReady { get; }

    /// <summary>
    /// Whether Lucid Dreaming is ready to use.
    /// </summary>
    bool LucidDreamingReady { get; }

    #endregion

    #region Helpers

    /// <summary>
    /// Status helper for checking buffs/debuffs.
    /// </summary>
    CirceStatusHelper StatusHelper { get; }

    /// <summary>
    /// Party helper for party member queries.
    /// </summary>
    CircePartyHelper PartyHelper { get; }

    #endregion

    #region Debug

    /// <summary>
    /// Debug state for this rotation.
    /// </summary>
    CirceDebugState Debug { get; }

    #endregion
}
