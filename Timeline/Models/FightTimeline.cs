using System;
using System.Collections.Generic;

namespace Olympus.Timeline.Models;

/// <summary>
/// Represents a complete parsed fight timeline.
/// Contains all entries pre-sorted by timestamp for efficient lookup.
/// </summary>
public sealed class FightTimeline
{
    /// <summary>
    /// The territory ID (zone ID) where this fight takes place.
    /// </summary>
    public uint ZoneId { get; }

    /// <summary>
    /// The content identifier (e.g., "r1s" for AAC M1S).
    /// Used for logging and display.
    /// </summary>
    public string ContentId { get; }

    /// <summary>
    /// Human-readable name of the fight (e.g., "AAC Light-heavyweight M1 (Savage)").
    /// </summary>
    public string Name { get; }

    /// <summary>
    /// All timeline entries, pre-sorted by timestamp.
    /// </summary>
    public TimelineEntry[] Entries { get; }

    /// <summary>
    /// Maps label names to their entry index for O(1) jump resolution.
    /// </summary>
    public IReadOnlyDictionary<string, int> LabelIndex { get; }

    /// <summary>
    /// Maps action IDs to entry indices that sync on that action.
    /// Used for efficient sync matching.
    /// </summary>
    public IReadOnlyDictionary<uint, List<int>> SyncIndex { get; }

    /// <summary>
    /// Creates a new fight timeline.
    /// </summary>
    /// <param name="zoneId">Territory ID for the zone.</param>
    /// <param name="contentId">Content identifier string.</param>
    /// <param name="name">Display name of the fight.</param>
    /// <param name="entries">Timeline entries (will be sorted by timestamp).</param>
    public FightTimeline(uint zoneId, string contentId, string name, TimelineEntry[] entries)
    {
        ZoneId = zoneId;
        ContentId = contentId;
        Name = name;

        // Sort entries by timestamp during construction
        Array.Sort(entries, (a, b) => a.Timestamp.CompareTo(b.Timestamp));
        Entries = entries;

        // Build label index
        var labelIndex = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (var i = 0; i < entries.Length; i++)
        {
            if (!string.IsNullOrEmpty(entries[i].Label))
            {
                labelIndex[entries[i].Label!] = i;
            }
        }
        LabelIndex = labelIndex;

        // Build sync index (action ID -> list of entry indices)
        var syncIndex = new Dictionary<uint, List<int>>();
        for (var i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (entry.Sync is { } sync && sync.ActionId > 0)
            {
                if (!syncIndex.TryGetValue(sync.ActionId, out var list))
                {
                    list = new List<int>(4);
                    syncIndex[sync.ActionId] = list;
                }
                list.Add(i);
            }
        }
        SyncIndex = syncIndex;
    }

    /// <summary>
    /// Finds the entry index for a given label.
    /// Returns -1 if the label is not found.
    /// </summary>
    public int FindLabelIndex(string label)
        => LabelIndex.TryGetValue(label, out var index) ? index : -1;

    /// <summary>
    /// Performs binary search to find the first entry at or after the given timestamp.
    /// Returns the index, or Entries.Length if all entries are before the timestamp.
    /// </summary>
    public int FindFirstEntryAtOrAfter(float timestamp)
    {
        var left = 0;
        var right = Entries.Length;

        while (left < right)
        {
            var mid = left + (right - left) / 2;
            if (Entries[mid].Timestamp < timestamp)
                left = mid + 1;
            else
                right = mid;
        }

        return left;
    }

    /// <summary>
    /// Gets entries in a time window [startTime, endTime).
    /// Yields entries without allocating a new collection.
    /// </summary>
    public IEnumerable<TimelineEntry> GetEntriesInWindow(float startTime, float endTime)
    {
        var startIndex = FindFirstEntryAtOrAfter(startTime);
        for (var i = startIndex; i < Entries.Length; i++)
        {
            if (Entries[i].Timestamp >= endTime)
                yield break;
            yield return Entries[i];
        }
    }
}
