namespace Olympus.Config;

/// <summary>
/// DPS priority mode - controls how aggressively to pursue damage vs healing.
/// </summary>
public enum DpsPriorityMode
{
    /// <summary>
    /// Healing first - Only DPS when party is healthy above threshold.
    /// Safest option, recommended for progression/learning content.
    /// </summary>
    HealFirst,

    /// <summary>
    /// Balanced - More aggressive DPS while still maintaining healing.
    /// Good for farm content where damage is predictable.
    /// </summary>
    Balanced,

    /// <summary>
    /// DPS first - Maximum damage output, minimal proactive healing.
    /// For easy content or when another healer covers healing.
    /// </summary>
    DpsFirst
}

/// <summary>
/// Configuration for damage spells (Stone/Glare and Holy progression).
/// </summary>
public sealed class DamageConfig
{
    /// <summary>
    /// DPS priority mode - affects when to prioritize damage vs healing.
    /// </summary>
    public DpsPriorityMode DpsPriority { get; set; } = DpsPriorityMode.HealFirst;

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
