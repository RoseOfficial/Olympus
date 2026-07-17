namespace Olympus.Config;

/// <summary>
/// Configuration for pre-pull automation: ability casts and oGCD preparations
/// timed to the party countdown signal.
/// </summary>
public sealed class PrePullConfig
{
    /// <summary>
    /// Master toggle for all pre-pull ability automation.
    /// A running party countdown is the player's explicit pull signal; when this
    /// is true, Olympus prepares opener abilities timed to the countdown:
    /// pre-cast GCDs (Fire III, Ruin III, Verthunder III, Rainbow Drip, healer
    /// damage spells), oGCD preps (Regen, Benison, Recitation, Reassemble,
    /// Meikyo Shisui), and sequence starters (Suiton, Soulsow, Form Shift).
    /// Individual behaviors still respect their per-job toggles when this is on.
    /// Default on -- pre-pull actions are universally correct opener behavior.
    /// </summary>
    public bool EnablePrePullActions { get; set; } = true;
}
