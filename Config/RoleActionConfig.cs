using System;

namespace Olympus.Config;

/// <summary>
/// Configuration for role actions (Esuna, Surecast, Rescue).
/// All numeric values are bounds-checked to prevent invalid configurations.
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
    /// Valid range: 0 to 3.
    /// </summary>
    private int _esunaPriorityThreshold = 2;
    public int EsunaPriorityThreshold
    {
        get => _esunaPriorityThreshold;
        set => _esunaPriorityThreshold = Math.Clamp(value, 0, 3);
    }

    // Surecast
    /// <summary>
    /// Enable Surecast role action.
    /// </summary>
    public bool EnableSurecast { get; set; } = false;

    /// <summary>
    /// Surecast usage mode:
    /// 0 = Manual only (never auto-use)
    /// 1 = Use on cooldown in combat
    /// Valid range: 0 to 1.
    /// </summary>
    private int _surecastMode = 0;
    public int SurecastMode
    {
        get => _surecastMode;
        set => _surecastMode = Math.Clamp(value, 0, 1);
    }

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
    /// Valid range: 0 only (automatic rescue is dangerous).
    /// </summary>
    private int _rescueMode = 0;
    public int RescueMode
    {
        get => _rescueMode;
        set => _rescueMode = Math.Clamp(value, 0, 0);
    }
}
