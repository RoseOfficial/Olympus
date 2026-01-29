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
    /// Whether to use single-target combo rotation.
    /// </summary>
    public bool EnableSingleTargetRotation { get; set; } = true;

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

    /// <summary>
    /// Whether to use Steel Peak for extra Chakra generation.
    /// </summary>
    public bool EnableSteelPeak { get; set; } = true;

    /// <summary>
    /// Whether to use Howling Fist for AoE damage.
    /// </summary>
    public bool EnableHowlingFist { get; set; } = true;

    #endregion

    #region Form Settings

    /// <summary>
    /// Whether to use Form Shift out of combat to maintain Opo-opo form.
    /// </summary>
    public bool UseFormShiftPrecombat { get; set; } = true;

    /// <summary>
    /// Preferred Blitz sequence.
    /// </summary>
    public BlitzSequence PreferredBlitz { get; set; } = BlitzSequence.PhantomRush;

    #endregion

    #region Burst Window Settings

    /// <summary>
    /// Align Brotherhood with party burst windows.
    /// </summary>
    public bool AlignBrotherhoodWithParty { get; set; } = true;

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
    /// Use Riddle of Fire with Perfect Balance.
    /// </summary>
    public bool UseRiddleOfFireWithPerfectBalance { get; set; } = true;

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
/// Preferred Masterful Blitz sequence.
/// </summary>
public enum BlitzSequence
{
    /// <summary>
    /// Build toward Phantom Rush.
    /// </summary>
    PhantomRush,

    /// <summary>
    /// Build toward Rising Phoenix.
    /// </summary>
    RisingPhoenix,

    /// <summary>
    /// Build toward Elixir Field.
    /// </summary>
    ElixirField
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
