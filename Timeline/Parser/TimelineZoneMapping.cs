using System.Collections.Generic;

namespace Olympus.Timeline.Parser;

/// <summary>
/// Maps FFXIV zone IDs (territory IDs) to timeline content identifiers.
/// Add new mappings here as timelines are added.
/// </summary>
public static class TimelineZoneMapping
{
    /// <summary>
    /// Information about a supported timeline zone.
    /// </summary>
    public readonly struct ZoneInfo
    {
        /// <summary>
        /// The content identifier used for timeline file names (e.g., "r1s").
        /// </summary>
        public string ContentId { get; init; }

        /// <summary>
        /// Human-readable name of the fight.
        /// </summary>
        public string Name { get; init; }

        /// <summary>
        /// The embedded resource name for the bundled timeline file.
        /// </summary>
        public string ResourceName { get; init; }

        public ZoneInfo(string contentId, string name, string resourceName)
        {
            ContentId = contentId;
            Name = name;
            ResourceName = resourceName;
        }
    }

    /// <summary>
    /// Maps zone IDs to their timeline information.
    /// Zone IDs are FFXIV territory IDs.
    /// </summary>
    private static readonly Dictionary<uint, ZoneInfo> ZoneMappings = new()
    {
        // Arcadion Savage (Dawntrail)
        // AAC Light-heavyweight M1 (Savage) - Black Cat
        [1226] = new ZoneInfo("r1s", "AAC Light-heavyweight M1 (Savage)", "Olympus.Timeline.Data.r1s.txt"),

        // AAC Light-heavyweight M2 (Savage) - Honey B. Lovely
        [1228] = new ZoneInfo("r2s", "AAC Light-heavyweight M2 (Savage)", "Olympus.Timeline.Data.r2s.txt"),

        // AAC Light-heavyweight M3 (Savage) - Brute Bomber
        [1230] = new ZoneInfo("r3s", "AAC Light-heavyweight M3 (Savage)", "Olympus.Timeline.Data.r3s.txt"),

        // AAC Light-heavyweight M4 (Savage) - Wicked Thunder
        [1232] = new ZoneInfo("r4s", "AAC Light-heavyweight M4 (Savage)", "Olympus.Timeline.Data.r4s.txt"),
    };

    /// <summary>
    /// Gets the zone info for a given territory ID.
    /// </summary>
    /// <param name="zoneId">The FFXIV territory ID.</param>
    /// <returns>The zone info if found, null otherwise.</returns>
    public static ZoneInfo? GetZoneInfo(uint zoneId)
        => ZoneMappings.TryGetValue(zoneId, out var info) ? info : null;

    /// <summary>
    /// Checks if a timeline is available for the given zone.
    /// </summary>
    public static bool HasTimeline(uint zoneId)
        => ZoneMappings.ContainsKey(zoneId);

    /// <summary>
    /// Gets all supported zone IDs.
    /// </summary>
    public static IEnumerable<uint> GetSupportedZones()
        => ZoneMappings.Keys;
}
