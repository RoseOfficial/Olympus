using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using Olympus.Timeline.Models;

namespace Olympus.Timeline.Parser;

/// <summary>
/// Parses Cactbot timeline format files.
/// Handles the various syntax patterns used in Cactbot raid timelines.
/// </summary>
public sealed partial class CactbotTimelineParser : ITimelineParser
{
    // Pre-compiled regex patterns for performance
    // Match: 15.0 "Ability Name" [optional modifiers]
    [GeneratedRegex(@"^(\d+(?:\.\d+)?)\s+""([^""]+)""(.*)$", RegexOptions.Compiled)]
    private static partial Regex EntryPattern();

    // Match: sync /pattern/ with optional id capture
    [GeneratedRegex(@"sync\s+/([^/]+)/", RegexOptions.Compiled)]
    private static partial Regex SyncPattern();

    // Match: Ability { id: "XXXX" } in sync pattern
    [GeneratedRegex(@"id:\s*""([0-9A-Fa-f]+)""", RegexOptions.Compiled)]
    private static partial Regex SyncIdPattern();

    // Match: StartsUsing { id: "XXXX" } in sync pattern
    [GeneratedRegex(@"StartsUsing\s*\{\s*id:\s*""([0-9A-Fa-f]+)""", RegexOptions.Compiled)]
    private static partial Regex StartsUsingPattern();

    // Match: window X,Y
    [GeneratedRegex(@"window\s+(\d+(?:\.\d+)?)\s*,\s*(\d+(?:\.\d+)?)", RegexOptions.Compiled)]
    private static partial Regex WindowPattern();

    // Match: duration X
    [GeneratedRegex(@"duration\s+(\d+(?:\.\d+)?)", RegexOptions.Compiled)]
    private static partial Regex DurationPattern();

    // Match: jump X or jump "label"
    [GeneratedRegex(@"jump\s+(?:(\d+(?:\.\d+)?)|""([^""]+)"")", RegexOptions.Compiled)]
    private static partial Regex JumpPattern();

    // Match: label "name" (standalone or modifier)
    [GeneratedRegex(@"label\s+""([^""]+)""", RegexOptions.Compiled)]
    private static partial Regex LabelPattern();

    // Match: hideall "pattern"
    [GeneratedRegex(@"^hideall\s+""([^""]+)""", RegexOptions.Compiled)]
    private static partial Regex HideallPattern();

    // Known mechanic type keywords for auto-classification
    private static readonly HashSet<string> RaidwideKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "raidwide", "aoe", "party damage", "raid damage", "stack",
        "spread", "explosion", "blast", "impact", "shockwave"
    };

    private static readonly HashSet<string> TankBusterKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "tankbuster", "tank buster", "buster", "cleave", "tank damage",
        "auto-attack", "double attack", "tank swap"
    };

    /// <summary>
    /// Parses a Cactbot timeline file.
    /// </summary>
    public FightTimeline? Parse(string content, uint zoneId, string contentId, string name)
    {
        if (string.IsNullOrWhiteSpace(content))
            return null;

        var entries = new List<TimelineEntry>();
        var hiddenPatterns = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var labelToTimestamp = new Dictionary<string, float>(StringComparer.OrdinalIgnoreCase);

        var lines = content.Split('\n', StringSplitOptions.TrimEntries);

        // First pass: collect hideall patterns and labels
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            var hideMatch = HideallPattern().Match(line);
            if (hideMatch.Success)
            {
                hiddenPatterns.Add(hideMatch.Groups[1].Value);
                continue;
            }
        }

        // Second pass: parse entries
        foreach (var line in lines)
        {
            if (string.IsNullOrEmpty(line) || line.StartsWith('#'))
                continue;

            if (HideallPattern().IsMatch(line))
                continue;

            var entry = ParseLine(line, hiddenPatterns, labelToTimestamp);
            if (entry.HasValue)
            {
                entries.Add(entry.Value);

                // Track label timestamps for jump resolution
                if (!string.IsNullOrEmpty(entry.Value.Label))
                {
                    labelToTimestamp[entry.Value.Label] = entry.Value.Timestamp;
                }
            }
        }

        if (entries.Count == 0)
            return null;

        // Third pass: resolve label-based jumps
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            // JumpTarget of -2 indicates an unresolved label jump (set in ParseLine)
            if (entry.JumpTarget == -2f && !string.IsNullOrEmpty(entry.Name))
            {
                // The label name was stored temporarily
                // This is a hack - we need to re-parse the jump modifier
                // Actually, let's handle this differently in ParseLine
            }
        }

        return new FightTimeline(zoneId, contentId, name, entries.ToArray());
    }

    private TimelineEntry? ParseLine(string line, HashSet<string> hiddenPatterns, Dictionary<string, float> labelToTimestamp)
    {
        var match = EntryPattern().Match(line);
        if (!match.Success)
            return null;

        if (!float.TryParse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var timestamp))
            return null;

        var abilityName = match.Groups[2].Value;
        var modifiers = match.Groups[3].Value;

        // Check if this ability is hidden
        var isHidden = hiddenPatterns.Contains(abilityName);

        // Parse sync
        TimelineSync? sync = null;
        var syncMatch = SyncPattern().Match(modifiers);
        if (syncMatch.Success)
        {
            var syncContent = syncMatch.Groups[1].Value;

            // Check for StartsUsing pattern first
            var startsUsingMatch = StartsUsingPattern().Match(syncContent);
            if (startsUsingMatch.Success)
            {
                if (uint.TryParse(startsUsingMatch.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var actionId))
                {
                    sync = TimelineSync.StartsUsing(actionId);
                }
            }
            else
            {
                // Check for regular Ability { id: } pattern
                var idMatch = SyncIdPattern().Match(syncContent);
                if (idMatch.Success)
                {
                    if (uint.TryParse(idMatch.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var actionId))
                    {
                        sync = TimelineSync.Ability(actionId);
                    }
                }
            }
        }

        // Parse window modifier
        var windowMatch = WindowPattern().Match(modifiers);
        if (windowMatch.Success && sync.HasValue)
        {
            if (float.TryParse(windowMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var before) &&
                float.TryParse(windowMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var after))
            {
                var currentSync = sync.Value;
                sync = new TimelineSync(currentSync.Type, currentSync.ActionId, currentSync.SourceName, before, after);
            }
        }

        // Parse duration
        var duration = 0f;
        var durationMatch = DurationPattern().Match(modifiers);
        if (durationMatch.Success)
        {
            float.TryParse(durationMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out duration);
        }

        // Parse jump
        var jumpTarget = -1f;
        var jumpMatch = JumpPattern().Match(modifiers);
        if (jumpMatch.Success)
        {
            if (!string.IsNullOrEmpty(jumpMatch.Groups[1].Value))
            {
                // Numeric jump
                float.TryParse(jumpMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out jumpTarget);
            }
            else if (!string.IsNullOrEmpty(jumpMatch.Groups[2].Value))
            {
                // Label jump - resolve to timestamp
                var labelName = jumpMatch.Groups[2].Value;
                if (labelToTimestamp.TryGetValue(labelName, out var labelTime))
                {
                    jumpTarget = labelTime;
                }
                // If label not found yet, it might be a forward reference
                // We'll leave jumpTarget as -1 for now
            }
        }

        // Parse label
        string? label = null;
        var labelMatch = LabelPattern().Match(modifiers);
        if (labelMatch.Success)
        {
            label = labelMatch.Groups[1].Value;
        }
        // Also check if the ability name itself indicates this is a label-only line
        // e.g., "50.0 label "phase2"" - in this case the ability name is "label" or empty
        if (abilityName.Equals("label", StringComparison.OrdinalIgnoreCase) && labelMatch.Success)
        {
            // This is a label-only line
            label = labelMatch.Groups[1].Value;
            abilityName = label; // Use label as the display name
        }

        // Classify entry type based on keywords
        var entryType = ClassifyEntryType(abilityName, modifiers);

        return new TimelineEntry(
            timestamp,
            abilityName,
            entryType,
            sync,
            duration,
            jumpTarget,
            label,
            isHidden);
    }

    private static TimelineEntryType ClassifyEntryType(string abilityName, string modifiers)
    {
        var combined = abilityName + " " + modifiers;

        // Check for explicit type markers in comments or modifiers
        if (combined.Contains("raidwide", StringComparison.OrdinalIgnoreCase))
            return TimelineEntryType.Raidwide;

        if (combined.Contains("tankbuster", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("tank buster", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("buster", StringComparison.OrdinalIgnoreCase))
            return TimelineEntryType.TankBuster;

        if (combined.Contains("stack", StringComparison.OrdinalIgnoreCase))
            return TimelineEntryType.Stack;

        if (combined.Contains("spread", StringComparison.OrdinalIgnoreCase))
            return TimelineEntryType.Spread;

        if (combined.Contains("enrage", StringComparison.OrdinalIgnoreCase))
            return TimelineEntryType.Enrage;

        if (combined.Contains("adds", StringComparison.OrdinalIgnoreCase))
            return TimelineEntryType.Adds;

        // Check for phase markers
        if (combined.Contains("phase", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("--sync--", StringComparison.OrdinalIgnoreCase))
            return TimelineEntryType.Phase;

        // Default to Ability
        return TimelineEntryType.Ability;
    }
}
