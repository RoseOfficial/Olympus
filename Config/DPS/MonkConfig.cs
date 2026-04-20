using System;

namespace Olympus.Config.DPS;

/// <summary>
/// Monk (Kratos) configuration options.
/// Controls form system, Chakra gauge, and burst windows.
/// </summary>
public sealed class MonkConfig
{
    #region Damage Toggles

    /// <summary>
    /// Whether to use AoE combo rotation.
    /// </summary>
    public bool EnableAoERotation { get; set; } = true;

    /// <summary>
    /// Whether to use The Forbidden Chakra/Enlightenment (Chakra spender).
    /// </summary>
    public bool EnableChakraSpenders { get; set; } = true;

    /// <summary>
    /// Whether to use Masterful Blitz (Beast Chakra combo).
    /// </summary>
    public bool EnableMasterfulBlitz { get; set; } = true;

    /// <summary>
    /// Whether to use Six-Sided Star.
    /// </summary>
    public bool EnableSixSidedStar { get; set; } = true;

    /// <summary>
    /// Whether to use Thunderclap for gap closing.
    /// </summary>
    public bool EnableThunderclap { get; set; } = true;

    #endregion

    #region Buff Toggles

    /// <summary>
    /// Whether to use Riddle of Fire.
    /// </summary>
    public bool EnableRiddleOfFire { get; set; } = true;

    /// <summary>
    /// Whether to use Brotherhood (party buff).
    /// </summary>
    public bool EnableBrotherhood { get; set; } = true;

    /// <summary>
    /// Whether to use Perfect Balance.
    /// </summary>
    public bool EnablePerfectBalance { get; set; } = true;

    /// <summary>
    /// Whether to use Riddle of Wind.
    /// </summary>
    public bool EnableRiddleOfWind { get; set; } = true;

    /// <summary>
    /// Whether to use Fire's Reply (follow-up after Riddle of Fire).
    /// </summary>
    public bool EnableFiresReply { get; set; } = true;

    /// <summary>
    /// Whether to use Wind's Reply (follow-up after Riddle of Wind).
    /// </summary>
    public bool EnableWindsReply { get; set; } = true;

    #endregion

    #region Chakra Settings

    /// <summary>
    /// Minimum Chakra to use The Forbidden Chakra/Enlightenment.
    /// </summary>
    private int _chakraMinGauge = 5;
    public int ChakraMinGauge
    {
        get => _chakraMinGauge;
        set => _chakraMinGauge = Math.Clamp(value, 1, 5);
    }

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Maximum seconds to hold Brotherhood waiting for party buffs.
    /// </summary>
    private float _brotherhoodHoldTime = 3.0f;
    public float BrotherhoodHoldTime
    {
        get => _brotherhoodHoldTime;
        set => _brotherhoodHoldTime = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Pool gauge resources (Forbidden Chakra, Masterful Blitz) for raid buff burst windows.
    /// When enabled, holds gauge spenders within 8s of an imminent burst.
    /// </summary>
    public bool EnableBurstPooling { get; set; } = true;

    #endregion

    #region AoE Settings

    /// <summary>
    /// Minimum enemies for AoE rotation. Defaults to 2 because Arm of the Destroyer / Shadow of
    /// the Destroyer is 100 potency per target vs Bootshine at 200 single-target — break-even is
    /// at 2 enemies. The AoE chain also builds Chakra per hit, so entering at 2 is a net gain on
    /// resource generation even without the pure potency uplift.
    /// </summary>
    private int _aoEMinTargets = 2;
    public int AoEMinTargets
    {
        get => _aoEMinTargets;
        set => _aoEMinTargets = Math.Clamp(value, 2, 8);
    }

    #endregion

    #region Positional Settings

    /// <summary>
    /// Whether to enforce positional requirements.
    /// MNK has the most positionals, so this can significantly impact DPS.
    /// </summary>
    public bool EnforcePositionals { get; set; } = false;

    /// <summary>
    /// Allow weaponskills even without True North when out of position.
    /// </summary>
    public bool AllowPositionalLoss { get; set; } = true;

    /// <summary>
    /// Strictness level for positional enforcement.
    /// </summary>
    public PositionalStrictness PositionalStrictness { get; set; } = PositionalStrictness.Relaxed;

    #endregion
}

/// <summary>
/// Positional strictness level for Monk.
/// </summary>
public enum PositionalStrictness
{
    /// <summary>
    /// Always use actions regardless of position.
    /// </summary>
    Relaxed,

    /// <summary>
    /// Prefer correct positions but allow losses.
    /// </summary>
    Moderate,

    /// <summary>
    /// Only use positional actions when in correct position.
    /// </summary>
    Strict
}
