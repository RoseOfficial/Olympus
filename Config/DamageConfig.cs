namespace Olympus.Config;

/// <summary>
/// Configuration for damage spells (Stone/Glare and Holy progression).
/// </summary>
public sealed class DamageConfig
{
    // Single-target damage (Stone/Glare progression)
    public bool EnableStone { get; set; } = true;
    public bool EnableStoneII { get; set; } = true;
    public bool EnableStoneIII { get; set; } = true;
    public bool EnableStoneIV { get; set; } = true;
    public bool EnableGlare { get; set; } = true;
    public bool EnableGlareIII { get; set; } = true;
    public bool EnableGlareIV { get; set; } = true;

    // AoE damage (Holy progression)
    public bool EnableHoly { get; set; } = true;
    public bool EnableHolyIII { get; set; } = true;

    // Blood Lily damage
    public bool EnableAfflatusMisery { get; set; } = true;

    /// <summary>
    /// Minimum number of enemies in range to trigger AoE damage (Holy).
    /// Default 3 means use Holy when 3+ enemies are within 8y radius.
    /// </summary>
    public int AoEDamageMinTargets { get; set; } = 3;
}
