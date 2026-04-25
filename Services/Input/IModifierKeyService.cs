namespace Olympus.Services.Input;

/// <summary>
/// Tracks modifier-key state for player-agency overrides during rotation execution.
/// Holding the configured burst key bypasses burst pooling so cooldowns fire ASAP;
/// holding the conservative key forces pooling on so cooldowns hold even when no
/// imminent burst is detected.
/// </summary>
public interface IModifierKeyService
{
    /// <summary>
    /// Updates modifier-key state from the OS key state. Call once per frame.
    /// Reads <c>Configuration.Input</c> to determine which keys are bound and
    /// whether the feature is enabled.
    /// </summary>
    void Update();

    /// <summary>
    /// True when the player is currently holding the burst-override key
    /// and the feature is enabled.
    /// </summary>
    bool IsBurstOverride { get; }

    /// <summary>
    /// True when the player is currently holding the conservative-override key
    /// and the feature is enabled.
    /// </summary>
    bool IsConservativeOverride { get; }
}
