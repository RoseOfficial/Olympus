using System.Collections.Generic;

namespace Olympus.Config;

/// <summary>
/// Configuration for debug settings.
/// </summary>
public sealed class DebugConfig
{
    public bool DebugWindowVisible { get; set; } = false;
    public int ActionHistorySize { get; set; } = 100;

    /// <summary>
    /// Enable verbose logging of healing decisions to Dalamud log.
    /// When enabled, logs each heal/oGCD/defensive decision with target, HP%, spell, and reason.
    /// Useful for understanding why specific healing choices are made.
    /// Default false to avoid log spam.
    /// </summary>
    public bool EnableVerboseLogging { get; set; } = false;

    /// <summary>
    /// Visibility settings for debug window sections.
    /// Key is section name, value is whether it's visible.
    /// </summary>
    public Dictionary<string, bool> DebugSectionVisibility { get; set; } = new()
    {
        // Overview tab
        ["GcdPlanning"] = true,
        ["QuickStats"] = true,

        // Healing tab
        ["HpPrediction"] = true,
        ["AoEHealing"] = true,
        ["RecentHeals"] = true,
        ["ShadowHp"] = true,

        // Actions tab
        ["GcdDetails"] = true,
        ["SpellUsage"] = true,
        ["ActionHistory"] = true,

        // Performance tab
        ["Statistics"] = true,
        ["Downtime"] = true,
    };
}
