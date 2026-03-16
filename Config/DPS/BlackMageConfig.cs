using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Black Mage (Hecate) configuration options.
/// Controls Fire/Ice phases, movement handling, and burst windows.
/// </summary>
public sealed class BlackMageConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use single-target rotation.
    /// </summary>
    public bool EnableSingleTargetRotation { get; set; } = true;

    /// <summary>
    /// Whether to use AoE rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use Fire IV.
    /// </summary>
    public bool EnableFireIV { get; set; } = true;

    /// <summary>
    /// Whether to use Blizzard IV.
    /// </summary>
    public bool EnableBlizzardIV { get; set; } = true;

    /// <summary>
    /// Whether to use Despair.
    /// </summary>
    public bool EnableDespair { get; set; } = true;

    /// <summary>
    /// Whether to use Xenoglossy (Polyglot spender).
    /// </summary>
    public bool EnableXenoglossy { get; set; } = true;

    /// <summary>
    /// Whether to use Foul (AoE Polyglot spender).
    /// </summary>
    public bool EnableFoul { get; set; } = true;

    /// <summary>
    /// Whether to use Paradox.
    /// </summary>
    public bool EnableParadox { get; set; } = true;

    /// <summary>
    /// Whether to use Flare Star.
    /// </summary>
    public bool EnableFlareStar { get; set; } = true;

    /// <summary>
    /// Whether to use High Fire II/High Blizzard II for AoE.
    /// </summary>
    public bool EnableHighAoE { get; set; } = true;

    /// <summary>
    /// Whether to use Flare for AoE.
    /// </summary>
    public bool EnableFlare { get; set; } = true;

    /// <summary>
    /// Whether to use Freeze for AoE.
    /// </summary>
    public bool EnableFreeze { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Ley Lines.
    /// </summary>
    public bool EnableLeyLines { get; set; } = true;

    /// <summary>
    /// Whether to use Triplecast.
    /// </summary>
    public bool EnableTriplecast { get; set; } = true;

    /// <summary>
    /// Whether to use Sharpcast.
    /// </summary>
    public bool EnableSharpcast { get; set; } = true;

    /// <summary>
    /// Whether to use Swiftcast.
    /// </summary>
    public bool EnableSwiftcast { get; set; } = true;

    /// <summary>
    /// Whether to use Amplifier.
    /// </summary>
    public bool EnableAmplifier { get; set; } = true;

    /// <summary>
    /// Whether to use Manafont.
    /// </summary>
    public bool EnableManafont { get; set; } = true;

    #endregion

    #region Phase Settings

    /// <summary>
    /// Number of Fire IV casts before Despair.
    /// </summary>
    private int _fireIVsBeforeDespair = 4;
    public int FireIVsBeforeDespair
    {
        get => _fireIVsBeforeDespair;
        set => _fireIVsBeforeDespair = Math.Clamp(value, 2, 6);
    }

    /// <summary>
    /// Minimum MP to cast Fire IV in Astral Fire.
    /// </summary>
    private int _fireIVMinMp = 800;
    public int FireIVMinMp
    {
        get => _fireIVMinMp;
        set => _fireIVMinMp = Math.Clamp(value, 400, 2000);
    }

    /// <summary>
    /// Use Fire III to enter Astral Fire from Umbral Ice.
    /// </summary>
    public bool UseFireIIITransition { get; set; } = true;

    /// <summary>
    /// Use Blizzard III to enter Umbral Ice from Astral Fire.
    /// </summary>
    public bool UseBlizzardIIITransition { get; set; } = true;

    #endregion

    #region Polyglot Settings

    /// <summary>
    /// Minimum Polyglot stacks to use Xenoglossy/Foul.
    /// </summary>
    private int _polyglotMinStacks = 1;
    public int PolyglotMinStacks
    {
        get => _polyglotMinStacks;
        set => _polyglotMinStacks = Math.Clamp(value, 1, 2);
    }

    /// <summary>
    /// Save Polyglot for movement.
    /// </summary>
    public bool SavePolyglotForMovement { get; set; } = true;

    /// <summary>
    /// Polyglot stacks to reserve for movement.
    /// </summary>
    private int _polyglotMovementReserve = 1;
    public int PolyglotMovementReserve
    {
        get => _polyglotMovementReserve;
        set => _polyglotMovementReserve = Math.Clamp(value, 0, 2);
    }

    #endregion

    #region Movement Settings

    /// <summary>
    /// Preferred instant cast priority for movement.
    /// </summary>
    public MovementPriority MovementPriority { get; set; } = MovementPriority.Triplecast;

    /// <summary>
    /// Whether to use Scathe as a last resort while moving.
    /// </summary>
    public bool UseScatheForMovement { get; set; } = false;

    /// <summary>
    /// Whether to use Aetherial Manipulation for mobility.
    /// </summary>
    public bool EnableAetherialManipulation { get; set; } = true;

    /// <summary>
    /// Whether to use Between the Lines to return to Ley Lines.
    /// </summary>
    public bool EnableBetweenTheLines { get; set; } = true;

    #endregion

    #region Ley Lines Settings

    /// <summary>
    /// Use Ley Lines during burst windows.
    /// </summary>
    public bool UseLeyLinesDuringBurst { get; set; } = true;

    /// <summary>
    /// Hold Ley Lines for upcoming burst.
    /// </summary>
    private float _leyLinesHoldTime = 5.0f;
    public float LeyLinesHoldTime
    {
        get => _leyLinesHoldTime;
        set => _leyLinesHoldTime = Math.Clamp(value, 0f, 15f);
    }

    #endregion

    #region Thunder Settings

    /// <summary>
    /// Whether to maintain Thunder DoT.
    /// </summary>
    public bool MaintainThunder { get; set; } = true;

    /// <summary>
    /// Seconds remaining on Thunder before refreshing.
    /// </summary>
    private float _thunderRefreshThreshold = 6.0f;
    public float ThunderRefreshThreshold
    {
        get => _thunderRefreshThreshold;
        set => _thunderRefreshThreshold = Math.Clamp(value, 0f, 15f);
    }

    /// <summary>
    /// Minimum target HP percentage to apply Thunder.
    /// </summary>
    private float _thunderMinTargetHp = 0.10f;
    public float ThunderMinTargetHp
    {
        get => _thunderMinTargetHp;
        set => _thunderMinTargetHp = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Whether to use Thunderhead procs immediately.
    /// </summary>
    public bool UseThunderheadImmediately { get; set; } = true;

    #endregion

    #region AoE Settings

    /// <summary>
    /// Minimum enemies for AoE rotation.
    /// </summary>
    private int _aoEMinTargets = 3;
    public int AoEMinTargets
    {
        get => _aoEMinTargets;
        set => _aoEMinTargets = Math.Clamp(value, 2, 8);
    }

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Pool Polyglot stacks for raid buff burst windows.
    /// When enabled, holds Xenoglossy/Foul within 8s of an imminent burst.
    /// </summary>
    public bool EnableBurstPooling { get; set; } = true;

    #endregion

    #region MP Management

    /// <summary>
    /// Whether to use Lucid Dreaming.
    /// </summary>
    public bool EnableLucidDreaming { get; set; } = true;

    /// <summary>
    /// MP percentage threshold for Lucid Dreaming.
    /// </summary>
    private float _lucidDreamingThreshold = 0.70f;
    public float LucidDreamingThreshold
    {
        get => _lucidDreamingThreshold;
        set => _lucidDreamingThreshold = Math.Clamp(value, 0f, 1f);
    }

    #endregion
}

/// <summary>
/// Movement instant cast priority.
/// </summary>
public enum MovementPriority
{
    /// <summary>
    /// Prioritize Triplecast for movement.
    /// </summary>
    Triplecast,

    /// <summary>
    /// Prioritize Swiftcast for movement.
    /// </summary>
    Swiftcast,

    /// <summary>
    /// Prioritize Xenoglossy/Polyglot for movement.
    /// </summary>
    Polyglot,

    /// <summary>
    /// Use whichever is available first.
    /// </summary>
    FirstAvailable
}
