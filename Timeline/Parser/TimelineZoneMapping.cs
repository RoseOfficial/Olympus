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
        // Pandaemonium Savage (Endwalker)
        // Asphodelos: The First Circle (Savage) - Erichthonios
        [1003] = new ZoneInfo("p1s", "Asphodelos: The First Circle (Savage)", "Olympus.Timeline.Data.p1s.txt"),

        // Asphodelos: The Second Circle (Savage) - Hippokampos
        [1005] = new ZoneInfo("p2s", "Asphodelos: The Second Circle (Savage)", "Olympus.Timeline.Data.p2s.txt"),

        // Asphodelos: The Third Circle (Savage) - Phoinix
        [1007] = new ZoneInfo("p3s", "Asphodelos: The Third Circle (Savage)", "Olympus.Timeline.Data.p3s.txt"),

        // Asphodelos: The Fourth Circle (Savage) - Hesperos
        [1009] = new ZoneInfo("p4s", "Asphodelos: The Fourth Circle (Savage)", "Olympus.Timeline.Data.p4s.txt"),

        // Abyssos: The Fifth Circle (Savage) - Proto-Carbuncle
        [1083] = new ZoneInfo("p5s", "Abyssos: The Fifth Circle (Savage)", "Olympus.Timeline.Data.p5s.txt"),

        // Abyssos: The Sixth Circle (Savage) - Hegemone
        [1085] = new ZoneInfo("p6s", "Abyssos: The Sixth Circle (Savage)", "Olympus.Timeline.Data.p6s.txt"),

        // Abyssos: The Seventh Circle (Savage) - Agdistis
        [1087] = new ZoneInfo("p7s", "Abyssos: The Seventh Circle (Savage)", "Olympus.Timeline.Data.p7s.txt"),

        // Abyssos: The Eighth Circle (Savage) - Hephaistos
        [1089] = new ZoneInfo("p8s", "Abyssos: The Eighth Circle (Savage)", "Olympus.Timeline.Data.p8s.txt"),

        // Arcadion Savage (Dawntrail)
        // AAC Light-heavyweight M1 (Savage) - Black Cat
        [1226] = new ZoneInfo("r1s", "AAC Light-heavyweight M1 (Savage)", "Olympus.Timeline.Data.r1s.txt"),

        // AAC Light-heavyweight M2 (Savage) - Honey B. Lovely
        [1228] = new ZoneInfo("r2s", "AAC Light-heavyweight M2 (Savage)", "Olympus.Timeline.Data.r2s.txt"),

        // AAC Light-heavyweight M3 (Savage) - Brute Bomber
        [1230] = new ZoneInfo("r3s", "AAC Light-heavyweight M3 (Savage)", "Olympus.Timeline.Data.r3s.txt"),

        // AAC Light-heavyweight M4 (Savage) - Wicked Thunder
        [1232] = new ZoneInfo("r4s", "AAC Light-heavyweight M4 (Savage)", "Olympus.Timeline.Data.r4s.txt"),

        // Ultimate Raids
        // The Unending Coil of Bahamut (Ultimate) - UCoB
        [280] = new ZoneInfo("ucob", "The Unending Coil of Bahamut (Ultimate)", "Olympus.Timeline.Data.ucob.txt"),

        // The Weapon's Refrain (Ultimate) - UWU
        [539] = new ZoneInfo("uwu", "The Weapon's Refrain (Ultimate)", "Olympus.Timeline.Data.uwu.txt"),

        // The Epic of Alexander (Ultimate) - TEA
        [694] = new ZoneInfo("tea", "The Epic of Alexander (Ultimate)", "Olympus.Timeline.Data.tea.txt"),

        // Dragonsong's Reprise (Ultimate) - DSU
        [968] = new ZoneInfo("dsu", "Dragonsong's Reprise (Ultimate)", "Olympus.Timeline.Data.dsu.txt"),

        // The Omega Protocol (Ultimate) - TOP
        [1122] = new ZoneInfo("top", "The Omega Protocol (Ultimate)", "Olympus.Timeline.Data.top.txt"),

        // Futures Rewritten (Ultimate) - FRU
        [1238] = new ZoneInfo("fru", "Futures Rewritten (Ultimate)", "Olympus.Timeline.Data.fru.txt"),
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
