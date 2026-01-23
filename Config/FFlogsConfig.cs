using System;

namespace Olympus.Config;

/// <summary>
/// Configuration for FFLogs API integration.
/// Users must create their own FFLogs API client at https://www.fflogs.com/api/clients/
/// </summary>
public sealed class FFlogsConfig
{
    /// <summary>
    /// OAuth client ID from FFLogs API client registration.
    /// </summary>
    public string ClientId { get; set; } = "";

    /// <summary>
    /// OAuth client secret from FFLogs API client registration.
    /// </summary>
    public string ClientSecret { get; set; } = "";

    /// <summary>
    /// Character name to look up on FFLogs.
    /// </summary>
    public string CharacterName { get; set; } = "";

    /// <summary>
    /// Server slug (e.g., "gilgamesh", "cactuar").
    /// </summary>
    public string ServerSlug { get; set; } = "";

    /// <summary>
    /// Server region: NA, EU, JP, OCE.
    /// </summary>
    public string Region { get; set; } = "NA";

    /// <summary>
    /// Cached character ID from FFLogs after first successful lookup.
    /// Avoids repeated character searches.
    /// </summary>
    public int? CachedCharacterId { get; set; }

    /// <summary>
    /// Whether to automatically compare fight results against FFLogs data.
    /// </summary>
    public bool EnableAutoComparison { get; set; } = true;

    /// <summary>
    /// Whether to show FFLogs comparison in the Fight Summary tab.
    /// </summary>
    public bool ShowInFightSummary { get; set; } = true;

    /// <summary>
    /// How long to cache FFLogs data before refreshing (minutes).
    /// Valid range: 15-240 minutes.
    /// </summary>
    private int _cacheExpiryMinutes = 60;
    public int CacheExpiryMinutes
    {
        get => _cacheExpiryMinutes;
        set => _cacheExpiryMinutes = Math.Clamp(value, 15, 240);
    }

    /// <summary>
    /// Whether FFLogs integration is configured with valid credentials.
    /// </summary>
    public bool IsConfigured => !string.IsNullOrWhiteSpace(ClientId) && !string.IsNullOrWhiteSpace(ClientSecret);

    /// <summary>
    /// Whether character binding is configured.
    /// </summary>
    public bool HasCharacterBinding => !string.IsNullOrWhiteSpace(CharacterName) && !string.IsNullOrWhiteSpace(ServerSlug);
}
