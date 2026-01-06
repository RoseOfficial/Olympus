namespace Olympus.Config;

/// <summary>
/// Base interface for job-specific configuration.
/// Each job module will have its own configuration class implementing this interface.
/// This enables future expansion to support multiple jobs with their own settings.
/// </summary>
public interface IJobConfiguration
{
    /// <summary>
    /// Display name for this job configuration (e.g., "White Mage", "Scholar").
    /// </summary>
    string JobName { get; }

    /// <summary>
    /// Resets all configuration values to their defaults.
    /// </summary>
    void ResetToDefaults();
}
