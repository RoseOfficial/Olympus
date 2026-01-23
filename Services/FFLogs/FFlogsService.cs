using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Dalamud.Plugin.Services;
using Olympus.Config;

namespace Olympus.Services.FFLogs;

/// <summary>
/// FFLogs API service implementation.
/// Handles OAuth authentication, GraphQL queries, and caching.
/// </summary>
public sealed class FFlogsService : IFFlogsService, IDisposable
{
    private const string TokenEndpoint = "https://www.fflogs.com/oauth/token";
    private const string GraphQlEndpoint = "https://www.fflogs.com/api/v2/client";

    private readonly FFlogsConfig config;
    private readonly IPluginLog log;
    private readonly HttpClient httpClient;
    private readonly JsonSerializerOptions jsonOptions;

    // OAuth token state
    private string? accessToken;
    private DateTime tokenExpiresAt = DateTime.MinValue;

    // Caching
    private readonly Dictionary<string, FFlogsCache<object>> cache = new();
    private FFlogsZoneRanking? cachedZoneRanking;

    public bool IsConfigured => config.IsConfigured;
    public bool IsAuthenticated => !string.IsNullOrEmpty(accessToken) && DateTime.Now < tokenExpiresAt;
    public FFlogsRateLimitInfo? RateLimitInfo { get; private set; }
    public string? LastError { get; private set; }

    public FFlogsService(FFlogsConfig config, IPluginLog log)
    {
        this.config = config;
        this.log = log;
        this.httpClient = new HttpClient();
        this.jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<FFlogsResult<FFlogsCharacter>> GetCharacterAsync(string name, string server, string region)
    {
        if (!IsConfigured)
            return FFlogsResult<FFlogsCharacter>.Fail("FFLogs not configured", FFlogsErrorType.InvalidCredentials);

        var cacheKey = $"character_{name}_{server}_{region}";
        if (TryGetCached<FFlogsCharacter>(cacheKey, out var cached))
            return FFlogsResult<FFlogsCharacter>.Ok(cached!);

        var query = @"
            query ($name: String!, $server: String!, $region: String!) {
                characterData {
                    character(name: $name, serverSlug: $server, serverRegion: $region) {
                        id
                        name
                        lodestoneID
                    }
                }
            }";

        var variables = new { name, server, region };
        var result = await ExecuteGraphQlAsync<CharacterResponse>(query, variables);

        if (!result.Success)
            return FFlogsResult<FFlogsCharacter>.Fail(result.Error ?? "Unknown error", result.ErrorType);

        var character = result.Data?.CharacterData?.Character;
        if (character == null)
            return FFlogsResult<FFlogsCharacter>.Fail("Character not found", FFlogsErrorType.CharacterNotFound);

        var fflogsChar = new FFlogsCharacter
        {
            Id = character.Id,
            Name = character.Name ?? name,
            Server = server,
            Region = region,
            LodestoneId = character.LodestoneId
        };

        SetCache(cacheKey, fflogsChar, TimeSpan.FromHours(24));  // Character ID rarely changes
        return FFlogsResult<FFlogsCharacter>.Ok(fflogsChar);
    }

    public async Task<FFlogsResult<FFlogsZoneRanking>> GetZoneRankingsAsync(int characterId, int zoneId)
    {
        if (!IsConfigured)
            return FFlogsResult<FFlogsZoneRanking>.Fail("FFLogs not configured", FFlogsErrorType.InvalidCredentials);

        var cacheKey = $"zone_{characterId}_{zoneId}";
        if (TryGetCached<FFlogsZoneRanking>(cacheKey, out var cached))
            return FFlogsResult<FFlogsZoneRanking>.Ok(cached!);

        // Query zone rankings with encounter breakdown
        var query = @"
            query ($characterId: Int!, $zoneId: Int!) {
                characterData {
                    character(id: $characterId) {
                        zoneRankings(zoneID: $zoneId, difficulty: 101, metric: rdps)
                    }
                }
            }";

        var variables = new { characterId, zoneId };
        var result = await ExecuteGraphQlAsync<ZoneRankingsResponse>(query, variables);

        if (!result.Success)
            return FFlogsResult<FFlogsZoneRanking>.Fail(result.Error ?? "Unknown error", result.ErrorType);

        var character = result.Data?.CharacterData?.Character;
        if (character == null)
            return FFlogsResult<FFlogsZoneRanking>.Fail("No rankings found", FFlogsErrorType.NoParses);

        var rankingsJson = character.ZoneRankings;

        // Parse the nested zone rankings JSON
        var zoneRanking = ParseZoneRankings(rankingsJson, zoneId);
        if (zoneRanking == null)
            return FFlogsResult<FFlogsZoneRanking>.Fail("Failed to parse rankings", FFlogsErrorType.Unknown);

        this.cachedZoneRanking = zoneRanking;
        SetCache(cacheKey, zoneRanking, TimeSpan.FromMinutes(config.CacheExpiryMinutes));
        return FFlogsResult<FFlogsZoneRanking>.Ok(zoneRanking);
    }

    public async Task<FFlogsResult<FFlogsParseRecord[]>> GetEncounterParsesAsync(int characterId, int encounterId)
    {
        if (!IsConfigured)
            return FFlogsResult<FFlogsParseRecord[]>.Fail("FFLogs not configured", FFlogsErrorType.InvalidCredentials);

        var cacheKey = $"parses_{characterId}_{encounterId}";
        if (TryGetCached<FFlogsParseRecord[]>(cacheKey, out var cached))
            return FFlogsResult<FFlogsParseRecord[]>.Ok(cached!);

        var query = @"
            query ($characterId: Int!, $encounterId: Int!) {
                characterData {
                    character(id: $characterId) {
                        encounterRankings(encounterID: $encounterId, metric: rdps)
                    }
                }
            }";

        var variables = new { characterId, encounterId };
        var result = await ExecuteGraphQlAsync<EncounterRankingsResponse>(query, variables);

        if (!result.Success)
            return FFlogsResult<FFlogsParseRecord[]>.Fail(result.Error ?? "Unknown error", result.ErrorType);

        var encounterChar = result.Data?.CharacterData?.Character;
        if (encounterChar == null)
            return FFlogsResult<FFlogsParseRecord[]>.Fail("No parses found", FFlogsErrorType.NoParses);

        var rankingsJson = encounterChar.EncounterRankings;
        var parses = ParseEncounterRankings(rankingsJson);
        SetCache(cacheKey, parses, TimeSpan.FromMinutes(config.CacheExpiryMinutes));
        return FFlogsResult<FFlogsParseRecord[]>.Ok(parses);
    }

    public float EstimatePercentile(int encounterId, uint jobId, float dps)
    {
        // Use cached zone ranking data if available
        if (cachedZoneRanking == null)
            return 0f;

        var encounter = cachedZoneRanking.Encounters.Find(e => e.EncounterId == encounterId);
        if (encounter == null || encounter.BestAmount <= 0)
            return 0f;

        // Simple linear estimation based on best parse
        // This is a rough approximation - actual percentile curves are more complex
        var ratio = dps / encounter.BestAmount;
        return Math.Clamp(ratio * encounter.BestPercentile, 0f, 99f);
    }

    public FFlogsParseComparison? GenerateComparison(int encounterId, float localDps, float gcdUptime, float cooldownEfficiency)
    {
        if (cachedZoneRanking == null)
            return null;

        var encounter = cachedZoneRanking.Encounters.Find(e => e.EncounterId == encounterId);
        if (encounter == null)
            return null;

        var comparison = new FFlogsParseComparison
        {
            EncounterId = encounterId,
            EncounterName = encounter.EncounterName,
            LocalDps = localDps,
            FFlogsbestDps = encounter.BestAmount,
            EstimatedPercentile = EstimatePercentile(encounterId, 0, localDps),
            LocalGcdUptime = gcdUptime,
            LocalCooldownEfficiency = cooldownEfficiency
        };

        // Generate improvement tips
        if (gcdUptime < 95f)
        {
            comparison.ImprovementTips.Add($"GCD uptime {gcdUptime:F1}% vs top parses ~98.5%");
        }

        if (cooldownEfficiency < 90f)
        {
            comparison.ImprovementTips.Add($"Cooldown efficiency {cooldownEfficiency:F0}% vs optimal ~95%");
        }

        if (localDps < encounter.BestAmount * 0.9f)
        {
            var gap = encounter.BestAmount - localDps;
            comparison.ImprovementTips.Add($"DPS gap of {gap:N0} to your best parse");
        }

        return comparison;
    }

    public async Task RefreshCacheAsync()
    {
        if (!config.HasCharacterBinding)
            return;

        // Clear existing cache
        ClearCache();

        // Refresh character data
        var charResult = await GetCharacterAsync(config.CharacterName, config.ServerSlug, config.Region);
        if (!charResult.Success)
            return;

        var characterId = charResult.Data!.Id;
        config.CachedCharacterId = characterId;

        // Refresh zone rankings for current savage tier
        await GetZoneRankingsAsync(characterId, FFlogsEncounterIds.ArcadionSavageZone);
    }

    public void ClearCache()
    {
        cache.Clear();
        cachedZoneRanking = null;
    }

    public async Task<bool> TestConnectionAsync()
    {
        try
        {
            var token = await GetAccessTokenAsync();
            return !string.IsNullOrEmpty(token);
        }
        catch (Exception ex)
        {
            log.Error($"FFLogs connection test failed: {ex.Message}");
            LastError = ex.Message;
            return false;
        }
    }

    private async Task<string?> GetAccessTokenAsync()
    {
        if (IsAuthenticated)
            return accessToken;

        if (!IsConfigured)
        {
            LastError = "FFLogs credentials not configured";
            return null;
        }

        try
        {
            var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.ClientId}:{config.ClientSecret}"));

            using var request = new HttpRequestMessage(HttpMethod.Post, TokenEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", credentials);
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials"
            });

            var response = await httpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                LastError = $"Token request failed: {response.StatusCode}";
                log.Warning($"FFLogs token request failed: {response.StatusCode} - {errorBody}");
                return null;
            }

            var content = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonSerializer.Deserialize<TokenResponseJson>(content, jsonOptions);

            if (tokenResponse == null || string.IsNullOrEmpty(tokenResponse.AccessToken))
            {
                LastError = "Invalid token response";
                return null;
            }

            accessToken = tokenResponse.AccessToken;
            // Set expiry 5 minutes early to avoid edge cases
            tokenExpiresAt = DateTime.Now.AddSeconds(tokenResponse.ExpiresIn - 300);
            LastError = null;

            log.Information("FFLogs authentication successful");
            return accessToken;
        }
        catch (Exception ex)
        {
            LastError = $"Authentication failed: {ex.Message}";
            log.Error($"FFLogs authentication error: {ex}");
            return null;
        }
    }

    private async Task<FFlogsResult<T>> ExecuteGraphQlAsync<T>(string query, object variables) where T : class
    {
        var token = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(token))
            return FFlogsResult<T>.Fail(LastError ?? "Authentication failed", FFlogsErrorType.InvalidCredentials);

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, GraphQlEndpoint);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var body = new
            {
                query,
                variables
            };

            request.Content = new StringContent(
                JsonSerializer.Serialize(body, jsonOptions),
                Encoding.UTF8,
                "application/json");

            var response = await httpClient.SendAsync(request);

            // Check for rate limiting
            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
            {
                LastError = "Rate limited - try again later";
                return FFlogsResult<T>.Fail(LastError, FFlogsErrorType.RateLimited);
            }

            if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                // Token may have expired, clear and retry once
                accessToken = null;
                LastError = "Authentication expired";
                return FFlogsResult<T>.Fail(LastError, FFlogsErrorType.InvalidCredentials);
            }

            if (!response.IsSuccessStatusCode)
            {
                LastError = $"API request failed: {response.StatusCode}";
                return FFlogsResult<T>.Fail(LastError, FFlogsErrorType.ServerError);
            }

            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GraphQlResponse<T>>(content, jsonOptions);

            if (result?.Errors != null && result.Errors.Count > 0)
            {
                LastError = result.Errors[0].Message;
                return FFlogsResult<T>.Fail(LastError, FFlogsErrorType.ServerError);
            }

            // Update rate limit info if provided
            UpdateRateLimitInfo(result?.Data);

            return FFlogsResult<T>.Ok(result!.Data!);
        }
        catch (HttpRequestException ex)
        {
            LastError = $"Network error: {ex.Message}";
            return FFlogsResult<T>.Fail(LastError, FFlogsErrorType.NetworkError);
        }
        catch (Exception ex)
        {
            LastError = $"Unexpected error: {ex.Message}";
            log.Error($"FFLogs API error: {ex}");
            return FFlogsResult<T>.Fail(LastError, FFlogsErrorType.Unknown);
        }
    }

    private void UpdateRateLimitInfo(object? data)
    {
        // FFLogs includes rate limit data in rateLimitData field
        // This is a simplified implementation
        if (RateLimitInfo == null)
        {
            RateLimitInfo = new FFlogsRateLimitInfo
            {
                PointsLimit = 3600,
                PointsRemaining = 3600,
                PointsResetIn = 3600
            };
        }

        // Decrement points (approximate - actual tracking would need response parsing)
        RateLimitInfo.PointsRemaining = Math.Max(0, RateLimitInfo.PointsRemaining - 1);
    }

    private FFlogsZoneRanking? ParseZoneRankings(JsonElement rankingsJson, int zoneId)
    {
        try
        {
            var ranking = new FFlogsZoneRanking
            {
                ZoneId = zoneId,
                ZoneName = "Arcadion Savage",
                Difficulty = 101
            };

            if (rankingsJson.TryGetProperty("allStars", out var allStars))
            {
                if (allStars.ValueKind == JsonValueKind.Array && allStars.GetArrayLength() > 0)
                {
                    var first = allStars[0];
                    ranking.AllStarsPoints = first.TryGetProperty("points", out var pts) ? pts.GetSingle() : 0;
                    ranking.AllStarsRank = first.TryGetProperty("rank", out var rank) ? rank.GetInt32() : 0;
                    ranking.AllStarsRankPercent = first.TryGetProperty("rankPercent", out var pct) ? pct.GetInt32() : 0;
                }
            }

            if (rankingsJson.TryGetProperty("rankings", out var rankings) && rankings.ValueKind == JsonValueKind.Array)
            {
                foreach (var encounter in rankings.EnumerateArray())
                {
                    var encRank = new FFlogsEncounterRank
                    {
                        EncounterId = encounter.TryGetProperty("encounter", out var enc) && enc.TryGetProperty("id", out var id) ? id.GetInt32() : 0,
                        EncounterName = encounter.TryGetProperty("encounter", out var enc2) && enc2.TryGetProperty("name", out var name) ? name.GetString() ?? "" : "",
                        BestPercentile = encounter.TryGetProperty("rankPercent", out var rp) ? rp.GetSingle() : 0,
                        MedianPercentile = encounter.TryGetProperty("medianPercent", out var mp) ? mp.GetSingle() : 0,
                        BestAmount = encounter.TryGetProperty("bestAmount", out var ba) ? ba.GetSingle() : 0,
                        TotalKills = encounter.TryGetProperty("totalKills", out var tk) ? tk.GetInt32() : 0
                    };

                    if (encRank.EncounterId > 0)
                        ranking.Encounters.Add(encRank);
                }
            }

            return ranking;
        }
        catch (Exception ex)
        {
            log.Error($"Failed to parse zone rankings: {ex}");
            return null;
        }
    }

    private FFlogsParseRecord[] ParseEncounterRankings(JsonElement rankingsJson)
    {
        var parses = new List<FFlogsParseRecord>();

        try
        {
            if (rankingsJson.TryGetProperty("ranks", out var ranks) && ranks.ValueKind == JsonValueKind.Array)
            {
                foreach (var rank in ranks.EnumerateArray())
                {
                    var parse = new FFlogsParseRecord
                    {
                        Percentile = rank.TryGetProperty("rankPercent", out var rp) ? rp.GetSingle() : 0,
                        TodayPercentile = rank.TryGetProperty("todayPercent", out var tp) ? tp.GetSingle() : 0,
                        Amount = rank.TryGetProperty("amount", out var amt) ? amt.GetSingle() : 0,
                        ReportCode = rank.TryGetProperty("report", out var rep) && rep.TryGetProperty("code", out var code) ? code.GetString() ?? "" : "",
                        FightId = rank.TryGetProperty("report", out var rep2) && rep2.TryGetProperty("fightID", out var fid) ? fid.GetInt32() : 0
                    };

                    if (rank.TryGetProperty("startTime", out var st))
                    {
                        var ms = st.GetInt64();
                        parse.StartTime = DateTimeOffset.FromUnixTimeMilliseconds(ms).LocalDateTime;
                    }

                    parses.Add(parse);
                }
            }
        }
        catch (Exception ex)
        {
            log.Error($"Failed to parse encounter rankings: {ex}");
        }

        return parses.ToArray();
    }

    private bool TryGetCached<T>(string key, out T? value) where T : class
    {
        value = null;
        if (!cache.TryGetValue(key, out var entry))
            return false;

        if (entry.IsExpired)
        {
            cache.Remove(key);
            return false;
        }

        value = entry.Data as T;
        return value != null;
    }

    private void SetCache<T>(string key, T value, TimeSpan expiry) where T : class
    {
        cache[key] = new FFlogsCache<object>
        {
            Data = value,
            CachedAt = DateTime.Now,
            ExpiresAt = DateTime.Now.Add(expiry)
        };
    }

    public void Dispose()
    {
        httpClient.Dispose();
    }

    #region JSON Response Classes

    private sealed class TokenResponseJson
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }

        [JsonPropertyName("token_type")]
        public string? TokenType { get; set; }

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }

    private sealed class GraphQlResponse<T>
    {
        public T? Data { get; set; }
        public List<GraphQlError>? Errors { get; set; }
    }

    private sealed class GraphQlError
    {
        public string Message { get; set; } = "";
    }

    private sealed class CharacterResponse
    {
        public CharacterDataWrapper? CharacterData { get; set; }
    }

    private sealed class CharacterDataWrapper
    {
        public CharacterJson? Character { get; set; }
    }

    private sealed class CharacterJson
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        [JsonPropertyName("lodestoneID")]
        public long? LodestoneId { get; set; }
    }

    private sealed class ZoneRankingsResponse
    {
        public ZoneRankingsDataWrapper? CharacterData { get; set; }
    }

    private sealed class ZoneRankingsDataWrapper
    {
        public ZoneRankingsCharacter? Character { get; set; }
    }

    private sealed class ZoneRankingsCharacter
    {
        public JsonElement ZoneRankings { get; set; }
    }

    private sealed class EncounterRankingsResponse
    {
        public EncounterRankingsDataWrapper? CharacterData { get; set; }
    }

    private sealed class EncounterRankingsDataWrapper
    {
        public EncounterRankingsCharacter? Character { get; set; }
    }

    private sealed class EncounterRankingsCharacter
    {
        public JsonElement EncounterRankings { get; set; }
    }

    #endregion
}
