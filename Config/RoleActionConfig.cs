namespace Olympus.Config;

/// <summary>
/// Configuration for role actions (Esuna, Surecast, Rescue).
/// </summary>
public sealed class RoleActionConfig
{
    // Esuna
    /// <summary>
    /// Enable automatic Esuna usage to cleanse debuffs.
    /// </summary>
    public bool EnableEsuna { get; set; } = true;

    /// <summary>
    /// Minimum debuff priority to auto-cleanse (0-3).
    /// 0 = Lethal only (Doom/Throttle)
    /// 1 = High+ (also Vulnerability Up)
    /// 2 = Medium+ (also Paralysis/Silence/Pacification)
    /// 3 = All dispellable debuffs
    /// </summary>
    public int EsunaPriorityThreshold { get; set; } = 2;

    // Surecast
    /// <summary>
    /// Enable Surecast role action.
    /// </summary>
    public bool EnableSurecast { get; set; } = false;

    /// <summary>
    /// Surecast usage mode:
    /// 0 = Manual only (never auto-use)
    /// 1 = Use on cooldown in combat
    /// </summary>
    public int SurecastMode { get; set; } = 0;

    // Rescue
    /// <summary>
    /// Enable Rescue role action. Disabled by default - use with extreme caution.
    /// Rescue can kill party members if used incorrectly.
    /// </summary>
    public bool EnableRescue { get; set; } = false;

    /// <summary>
    /// Rescue mode:
    /// 0 = Manual only (never auto-use)
    /// Note: Automatic rescue is not implemented due to extreme risk.
    /// </summary>
    public int RescueMode { get; set; } = 0;
}
