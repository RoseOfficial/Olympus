namespace Olympus.Config;

/// <summary>
/// Blood Lily generation strategy for optimizing Afflatus Misery usage.
/// Controls when to prefer lily heals (Solace/Rapture) over MP-based alternatives.
/// </summary>
public enum LilyGenerationStrategy
{
    /// <summary>
    /// Aggressive: Always prefer lily heals when lilies are available.
    /// Maximizes lily throughput but may waste Blood Lily stacks if Misery can't be used.
    /// </summary>
    Aggressive,

    /// <summary>
    /// Balanced (Default): Prefer lily heals when Blood Lilies are below 3.
    /// At 3 Blood Lilies, falls back to MP-based heals to avoid overcapping.
    /// </summary>
    Balanced,

    /// <summary>
    /// Conservative: Only prefer lily heals when target HP is below threshold.
    /// Preserves lilies for emergencies while still generating Blood Lilies over time.
    /// </summary>
    Conservative,

    /// <summary>
    /// Disabled: No Blood Lily optimization. Uses normal tier priority.
    /// Lily heals are still Tier 1 priority but not specially preferred over alternatives.
    /// </summary>
    Disabled
}
