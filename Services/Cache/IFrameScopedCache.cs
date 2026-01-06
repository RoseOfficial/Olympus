using System;
using System.Collections.Generic;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.ClientState.Objects.Types;

namespace Olympus.Services.Cache;

/// <summary>
/// Interface for frame-scoped caching service.
/// Caches frequently accessed data that doesn't change within a single frame.
/// Call InvalidateAll() at the start of each frame to reset the cache.
/// </summary>
public interface IFrameScopedCache
{
    /// <summary>
    /// Gets the cached current time (DateTime.UtcNow).
    /// Cached once per frame to avoid repeated system calls.
    /// </summary>
    DateTime CurrentTime { get; }

    /// <summary>
    /// Gets the current frame number for cache versioning.
    /// </summary>
    ulong FrameNumber { get; }

    /// <summary>
    /// Invalidates all cached data. Call at the start of each frame.
    /// </summary>
    void InvalidateAll();

    /// <summary>
    /// Gets or computes a cached value by key.
    /// The value is cached for the current frame only.
    /// </summary>
    /// <typeparam name="T">The type of value to cache.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="compute">Function to compute the value if not cached.</param>
    /// <returns>The cached or computed value.</returns>
    T GetOrCompute<T>(string key, Func<T> compute);

    /// <summary>
    /// Tries to get a cached value without computing it.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The cached value if found.</param>
    /// <returns>True if the value was found in cache.</returns>
    bool TryGetCached<T>(string key, out T? value);

    /// <summary>
    /// Sets a value in the cache directly.
    /// </summary>
    /// <typeparam name="T">The type of value.</typeparam>
    /// <param name="key">The cache key.</param>
    /// <param name="value">The value to cache.</param>
    void SetCached<T>(string key, T value);

    /// <summary>
    /// Gets cached party members for the current frame.
    /// Uses "PartyMembers" as the cache key internally.
    /// </summary>
    /// <param name="player">The local player.</param>
    /// <param name="computeMembers">Function to compute party members if not cached.</param>
    /// <returns>List of party members (cached for this frame).</returns>
    IReadOnlyList<IBattleChara> GetPartyMembers(
        IPlayerCharacter player,
        Func<IPlayerCharacter, IEnumerable<IBattleChara>> computeMembers);

    /// <summary>
    /// Gets cached player stats for the current frame.
    /// Uses "PlayerStats_{level}" as the cache key internally.
    /// </summary>
    /// <param name="level">The player's level.</param>
    /// <param name="computeStats">Function to compute stats if not cached.</param>
    /// <returns>Tuple of (Mind, Determination, WeaponDamage).</returns>
    (int Mind, int Determination, int WeaponDamage) GetPlayerStats(
        int level,
        Func<int, (int, int, int)> computeStats);
}
