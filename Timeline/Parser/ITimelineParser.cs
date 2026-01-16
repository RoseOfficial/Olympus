using Olympus.Timeline.Models;

namespace Olympus.Timeline.Parser;

/// <summary>
/// Interface for parsing fight timeline files from various formats.
/// </summary>
public interface ITimelineParser
{
    /// <summary>
    /// Parses a timeline file and returns the fight timeline.
    /// </summary>
    /// <param name="content">The raw file content to parse.</param>
    /// <param name="zoneId">The zone ID this timeline is for.</param>
    /// <param name="contentId">The content identifier (e.g., "r1s").</param>
    /// <param name="name">The display name of the fight.</param>
    /// <returns>The parsed fight timeline, or null if parsing failed.</returns>
    FightTimeline? Parse(string content, uint zoneId, string contentId, string name);
}
