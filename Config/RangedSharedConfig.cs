namespace Olympus.Config;

/// <summary>
/// Shared role-action configuration for ranged physical DPS jobs (BRD/MCH/DNC).
/// </summary>
public sealed class RangedSharedConfig
{
    /// <summary>
    /// Whether to use Head Graze for enemy cast interrupts.
    /// </summary>
    public bool EnableHeadGraze { get; set; } = true;

    /// <summary>
    /// Whether to apply Peloton automatically while moving out of combat.
    /// </summary>
    public bool EnablePeloton { get; set; } = true;
}
