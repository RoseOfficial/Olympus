using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

namespace Olympus.Services.Cache;

/// <summary>
/// Frame-scoped caching service for frequently accessed data.
/// All cached values are invalidated at the start of each frame.
/// This reduces redundant game memory reads and DateTime.UtcNow calls.
/// </summary>
public sealed class FrameScopedCache : IFrameScopedCache
{
    private readonly Dictionary<string, object> _cache = new(32);
    private DateTime _currentTime;
    private ulong _frameNumber;

    // Pre-allocated list for party members to reduce GC pressure
    private readonly List<IBattleChara> _partyMembersCache = new(8);

    /// <inheritdoc />
    public DateTime CurrentTime => _currentTime;

    /// <inheritdoc />
    public ulong FrameNumber => _frameNumber;

    /// <summary>
    /// Creates a new frame-scoped cache.
    /// </summary>
    public FrameScopedCache()
    {
        _currentTime = DateTime.UtcNow;
        _frameNumber = 0;
    }

    /// <inheritdoc />
    public void InvalidateAll()
    {
        _cache.Clear();
        _partyMembersCache.Clear();
        _currentTime = DateTime.UtcNow;
        _frameNumber++;
    }

    /// <inheritdoc />
    public T GetOrCompute<T>(string key, Func<T> compute)
    {
        if (_cache.TryGetValue(key, out var cached) && cached is T typedValue)
        {
            return typedValue;
        }

        var computed = compute();
        if (computed is not null)
        {
            _cache[key] = computed;
        }
        return computed;
    }

    /// <inheritdoc />
    public bool TryGetCached<T>(string key, out T? value)
    {
        if (_cache.TryGetValue(key, out var cached) && cached is T typedValue)
        {
            value = typedValue;
            return true;
        }

        value = default;
        return false;
    }

    /// <inheritdoc />
    public void SetCached<T>(string key, T value)
    {
        if (value is not null)
        {
            _cache[key] = value;
        }
    }

    /// <inheritdoc />
    public IReadOnlyList<IBattleChara> GetPartyMembers(
        IPlayerCharacter player,
        Func<IPlayerCharacter, IEnumerable<IBattleChara>> computeMembers)
    {
        const string cacheKey = "PartyMembers";

        // Check if already cached this frame
        if (_cache.ContainsKey(cacheKey))
        {
            return _partyMembersCache;
        }

        // Compute and cache
        _partyMembersCache.Clear();
        foreach (var member in computeMembers(player))
        {
            _partyMembersCache.Add(member);
        }

        _cache[cacheKey] = true; // Mark as computed
        return _partyMembersCache;
    }

    /// <inheritdoc />
    public (int Mind, int Determination, int WeaponDamage) GetPlayerStats(
        int level,
        Func<int, (int, int, int)> computeStats)
    {
        var cacheKey = $"PlayerStats_{level}";

        if (_cache.TryGetValue(cacheKey, out var cached) && cached is ValueTuple<int, int, int> stats)
        {
            return stats;
        }

        var computed = computeStats(level);
        _cache[cacheKey] = computed;
        return computed;
    }
}

/// <summary>
/// Well-known cache keys used throughout the plugin.
/// </summary>
public static class CacheKeys
{
    /// <summary>Cache key for party members list.</summary>
    public const string PartyMembers = "PartyMembers";

    /// <summary>Cache key prefix for player stats by level.</summary>
    public const string PlayerStatsPrefix = "PlayerStats_";

    /// <summary>Cache key prefix for entity damage rate.</summary>
    public const string DamageRatePrefix = "DamageRate_";

    /// <summary>Cache key for party damage rate.</summary>
    public const string PartyDamageRate = "PartyDamageRate";

    /// <summary>Cache key prefix for status checks.</summary>
    public const string StatusPrefix = "Status_";

    /// <summary>Cache key for party health metrics.</summary>
    public const string PartyHealthMetrics = "PartyHealthMetrics";
}
