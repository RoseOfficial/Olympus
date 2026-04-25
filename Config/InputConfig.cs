namespace Olympus.Config;

/// <summary>
/// Modifier-key override settings. When enabled, holding the configured keys
/// overrides burst-pooling decisions while the rotation runs.
///
/// Default disabled because Shift / Ctrl are commonly held while typing in chat
/// or for game keybinds; an accidental override would dump cooldowns at the
/// wrong moment. Players who want the feature opt in explicitly.
/// </summary>
public sealed class InputConfig
{
    /// <summary>
    /// Master toggle. When false, modifier keys are ignored regardless of which
    /// keys are bound below.
    /// </summary>
    public bool EnableModifierOverrides { get; set; } = false;

    /// <summary>
    /// Modifier key that forces "burst now" behavior while held.
    /// Burst pooling and phase-transition holds are bypassed; the rotation
    /// dumps cooldowns and gauge spenders as soon as they come up.
    /// </summary>
    public ModifierKey BurstOverrideKey { get; set; } = ModifierKey.Shift;

    /// <summary>
    /// Modifier key that forces "conservative" behavior while held.
    /// Burst pooling is forced on; the rotation holds cooldowns and gauge
    /// regardless of whether a burst window is detected.
    /// </summary>
    public ModifierKey ConservativeOverrideKey { get; set; } = ModifierKey.Control;
}

/// <summary>
/// Modifier keys available for override binding.
/// </summary>
public enum ModifierKey
{
    None = 0,
    Shift = 1,
    Control = 2,
    Alt = 3,
}
