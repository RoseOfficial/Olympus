namespace Olympus.Services.Healing;

/// <summary>
/// Service for checking whether spells are enabled in configuration.
/// </summary>
public interface ISpellEnablementService
{
    /// <summary>
    /// Checks if a spell is enabled in the configuration.
    /// </summary>
    /// <param name="actionId">The action ID to check.</param>
    /// <returns>True if the spell is enabled, false otherwise.</returns>
    bool IsSpellEnabled(uint actionId);
}
