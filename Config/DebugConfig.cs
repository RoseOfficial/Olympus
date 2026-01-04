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
