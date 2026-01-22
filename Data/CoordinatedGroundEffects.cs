using System.Collections.Generic;

namespace Olympus.Data;

/// <summary>
/// Registry of ground-targeted healing effects that should be coordinated between Olympus instances.
/// These abilities create healing zones that overlap inefficiently when stacked.
/// </summary>
public static class CoordinatedGroundEffects
{
    /// <summary>
    /// Ground effect configuration data.
    /// </summary>
    public readonly struct GroundEffectInfo
    {
        /// <summary>Action ID.</summary>
        public uint ActionId { get; init; }

        /// <summary>Effect radius in yalms.</summary>
        public float Radius { get; init; }

        /// <summary>Effect duration in seconds.</summary>
        public float Duration { get; init; }

        /// <summary>Recast time in milliseconds.</summary>
        public int RecastTimeMs { get; init; }
    }

    /// <summary>
    /// White Mage - Asylum: Ground-targeted healing zone with regen and healing boost.
    /// </summary>
    public static readonly GroundEffectInfo Asylum = new()
    {
        ActionId = ActionIds.Asylum,
        Radius = 8f,
        Duration = 24f,
        RecastTimeMs = 90_000,
    };

    /// <summary>
    /// Scholar - Sacred Soil: Ground-targeted healing zone with mitigation and regen.
    /// </summary>
    public static readonly GroundEffectInfo SacredSoil = new()
    {
        ActionId = ActionIds.SacredSoil,
        Radius = 8f,
        Duration = 15f,
        RecastTimeMs = 30_000,
    };

    /// <summary>
    /// Astrologian - Earthly Star: Ground-targeted healing zone (explodes after maturation).
    /// </summary>
    public static readonly GroundEffectInfo EarthlyStar = new()
    {
        ActionId = ActionIds.EarthlyStar,
        Radius = 8f,
        Duration = 20f, // Max duration before auto-explode
        RecastTimeMs = 60_000,
    };

    /// <summary>
    /// Sage - Kerachole: Ground-targeted healing zone with mitigation and regen.
    /// </summary>
    public static readonly GroundEffectInfo Kerachole = new()
    {
        ActionId = ActionIds.Kerachole,
        Radius = 8f,
        Duration = 15f,
        RecastTimeMs = 30_000,
    };

    /// <summary>
    /// All coordinated ground effects.
    /// </summary>
    public static readonly Dictionary<uint, GroundEffectInfo> Effects = new()
    {
        { Asylum.ActionId, Asylum },
        { SacredSoil.ActionId, SacredSoil },
        { EarthlyStar.ActionId, EarthlyStar },
        { Kerachole.ActionId, Kerachole },
    };

    /// <summary>
    /// Checks if an action is a coordinated ground effect.
    /// </summary>
    public static bool IsCoordinatedGroundEffect(uint actionId)
        => Effects.ContainsKey(actionId);

    /// <summary>
    /// Gets the effect info for a ground effect action.
    /// </summary>
    public static GroundEffectInfo? GetEffectInfo(uint actionId)
        => Effects.TryGetValue(actionId, out var info) ? info : null;

    /// <summary>
    /// Gets the radius for a ground effect.
    /// </summary>
    public static float GetRadius(uint actionId)
        => Effects.TryGetValue(actionId, out var info) ? info.Radius : 8f;

    /// <summary>
    /// Gets the duration for a ground effect.
    /// </summary>
    public static float GetDuration(uint actionId)
        => Effects.TryGetValue(actionId, out var info) ? info.Duration : 15f;
}
