namespace Olympus.Services.Pull;

/// <summary>
/// Reads the party countdown timer from native game memory.
/// Null return means no countdown is currently active.
/// Implementations are unsafe and untestable in unit tests;
/// inject a mock <see cref="ICountdownProbe"/> for all service tests.
/// </summary>
public interface ICountdownProbe
{
    /// <summary>
    /// Returns the seconds remaining on the party countdown, or null when
    /// no countdown is active. Fails open: returns null on any exception
    /// or unavailable native pointer so the feature is inert on probe failure.
    /// </summary>
    float? GetCountdownRemaining();
}
