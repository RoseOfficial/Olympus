using System.Threading.Tasks;

namespace Olympus.Services.FFLogs;

/// <summary>
/// Interface for FFLogs API operations.
/// </summary>
public interface IFFlogsService
{
    /// <summary>
    /// Whether the service is configured with valid credentials.
    /// </summary>
    bool IsConfigured { get; }

    /// <summary>
    /// Whether the service has successfully authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Current rate limit information.
    /// </summary>
    FFlogsRateLimitInfo? RateLimitInfo { get; }

    /// <summary>
    /// Last error message if any operation failed.
    /// </summary>
    string? LastError { get; }

    /// <summary>
    /// Looks up a character on FFLogs by name and server.
    /// </summary>
    /// <param name="name">Character name.</param>
    /// <param name="server">Server slug (e.g., "gilgamesh").</param>
    /// <param name="region">Region code (NA, EU, JP, OCE).</param>
    /// <returns>Character data or null if not found.</returns>
    Task<FFlogsResult<FFlogsCharacter>> GetCharacterAsync(string name, string server, string region);

    /// <summary>
    /// Gets zone rankings for a character (e.g., Arcadion Savage tier).
    /// </summary>
    /// <param name="characterId">FFLogs character ID.</param>
    /// <param name="zoneId">Zone ID to get rankings for.</param>
    /// <returns>Zone ranking data including all encounters.</returns>
    Task<FFlogsResult<FFlogsZoneRanking>> GetZoneRankingsAsync(int characterId, int zoneId);

    /// <summary>
    /// Gets historical parses for a specific encounter.
    /// </summary>
    /// <param name="characterId">FFLogs character ID.</param>
    /// <param name="encounterId">Encounter ID.</param>
    /// <returns>List of parse records.</returns>
    Task<FFlogsResult<FFlogsParseRecord[]>> GetEncounterParsesAsync(int characterId, int encounterId);

    /// <summary>
    /// Estimates percentile for a given DPS value on an encounter.
    /// Uses cached ranking data if available.
    /// </summary>
    /// <param name="encounterId">Encounter ID.</param>
    /// <param name="jobId">FFXIV job ID.</param>
    /// <param name="dps">DPS value to estimate percentile for.</param>
    /// <returns>Estimated percentile (0-100).</returns>
    float EstimatePercentile(int encounterId, uint jobId, float dps);

    /// <summary>
    /// Generates a comparison between local fight metrics and FFLogs data.
    /// </summary>
    /// <param name="encounterId">Encounter ID.</param>
    /// <param name="localDps">Local DPS from the fight.</param>
    /// <param name="gcdUptime">Local GCD uptime percentage.</param>
    /// <param name="cooldownEfficiency">Local cooldown efficiency percentage.</param>
    /// <returns>Comparison data with improvement tips.</returns>
    FFlogsParseComparison? GenerateComparison(int encounterId, float localDps, float gcdUptime, float cooldownEfficiency);

    /// <summary>
    /// Refreshes cached data for the bound character.
    /// </summary>
    Task RefreshCacheAsync();

    /// <summary>
    /// Clears all cached data.
    /// </summary>
    void ClearCache();

    /// <summary>
    /// Tests the API connection with current credentials.
    /// </summary>
    /// <returns>True if connection is successful.</returns>
    Task<bool> TestConnectionAsync();
}
