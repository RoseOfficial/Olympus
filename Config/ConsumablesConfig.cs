namespace Olympus.Config;

/// <summary>
/// Configuration for combat consumable automation. Currently scoped to tinctures.
/// </summary>
public sealed class ConsumablesConfig
{
    /// <summary>
    /// Master toggle. When true, Olympus will attempt to use combat tinctures
    /// in high-end content. Defaults OFF -- pots cost real gil and players
    /// must opt in deliberately.
    /// </summary>
    public bool EnableAutoTincture { get; set; } = false;

    /// <summary>
    /// When true, fires a one-shot per-fight chat warning if the master toggle
    /// is on but no matching tincture is in inventory.
    /// </summary>
    public bool WarnOnEmptyInventory { get; set; } = true;
}
