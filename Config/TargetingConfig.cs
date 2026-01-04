using Olympus.Services.Targeting;

namespace Olympus.Config;

/// <summary>
/// Configuration for targeting settings.
/// </summary>
public sealed class TargetingConfig
{
    /// <summary>
    /// Strategy for selecting enemy targets during combat.
    /// </summary>
    public EnemyTargetingStrategy EnemyStrategy { get; set; } = EnemyTargetingStrategy.LowestHp;

    /// <summary>
    /// When using TankAssist strategy, fall back to LowestHp if no tank target is found.
    /// </summary>
    public bool UseTankAssistFallback { get; set; } = true;

    /// <summary>
    /// How long to cache valid enemy list in milliseconds.
    /// Higher values improve performance but may delay target switching.
    /// </summary>
    public int TargetCacheTtlMs { get; set; } = 100;
}
