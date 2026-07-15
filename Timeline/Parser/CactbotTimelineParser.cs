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

    // Match: StartsUsing/Ability { id: "XXXX" } or { id: ["XXXX", ...] } without sync /.../ wrapper
    [GeneratedRegex(@"\b(StartsUsing|Ability)\s*\{\s*id:\s*(\[[^\]]*\]|""[0-9A-Fa-f]+"")", RegexOptions.Compiled)]
    private static partial Regex NetworkSyncPattern();

    // Match: quoted hex tokens like "B34E" within an id array or single id value
    [GeneratedRegex(@"""([0-9A-Fa-f]+)""", RegexOptions.Compiled)]
    private static partial Regex HexIdListPattern();

    // Known mechanic type keywords for auto-classification.
    // ClassifyEntryType checks these sets in priority order: TankBuster → Stack → Spread →
    // Raidwide → Enrage → Adds → Phase → Ability.
    // Note: "stack" and "spread" appear here and also map to their own entry types, so they
    // are checked before the general RaidwideKeywords scan.
    private static readonly HashSet<string> TankBusterKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "tankbuster", "tank buster", "buster", "cleave", "tank damage",
        "auto-attack", "double attack", "tank swap"
    };

    private static readonly HashSet<string> StackKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "stack", "share"
    };

    private static readonly HashSet<string> SpreadKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "spread", "out"
    };

    private static readonly HashSet<string> RaidwideKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "raidwide", "aoe", "party damage", "raid damage",
        "explosion", "blast", "impact", "shockwave"
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

        // Tracks forward-reference label jumps: (entry index in 'entries', unresolved label name)
        var pendingLabelJumps = new List<(int EntryIndex, string LabelName)>();

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

            var entry = ParseLine(line, hiddenPatterns, labelToTimestamp, pendingLabelJumps, entries.Count);
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

        // Third pass: resolve forward label-based jumps now that labelToTimestamp is complete
        foreach (var (entryIndex, labelName) in pendingLabelJumps)
        {
            if (labelToTimestamp.TryGetValue(labelName, out var resolvedTime))
            {
                var existing = entries[entryIndex];
                entries[entryIndex] = new TimelineEntry(
                    existing.Timestamp,
                    existing.Name,
                    existing.EntryType,
                    existing.Sync,
                    existing.Duration,
                    resolvedTime,
                    existing.Label,
                    existing.IsHidden);
            }
            // If still unresolved after full parse, the entry keeps JumpTarget=-1 (no jump),
            // which is safe and avoids corrupting the timeline clock with a -1 jump.
        }

        return new FightTimeline(zoneId, contentId, name, entries.ToArray());
    }

    private TimelineEntry? ParseLine(
        string line,
        HashSet<string> hiddenPatterns,
        Dictionary<string, float> labelToTimestamp,
        List<(int EntryIndex, string LabelName)> pendingLabelJumps,
        int currentEntryIndex)
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

        // Parse sync. Two Cactbot generations exist side by side in Timeline/Data:
        //  - old ACT format:     sync /^.{14} Erichthonios 6DA1/           (regex wrapper)
        //  - new network format: StartsUsing { id: "B384", ... }            (no wrapper)
        //                        Ability { id: ["B34E", "B34F"], ... }      (array IDs)
        TimelineSync? sync = null;
        var syncMatch = SyncPattern().Match(modifiers);
        if (syncMatch.Success)
        {
            var syncContent = syncMatch.Groups[1].Value;

            var startsUsingMatch = StartsUsingPattern().Match(syncContent);
            if (startsUsingMatch.Success)
            {
                if (uint.TryParse(startsUsingMatch.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var actionId))
                    sync = TimelineSync.StartsUsing(actionId);
            }
            else
            {
                var idMatch = SyncIdPattern().Match(syncContent);
                if (idMatch.Success)
                {
                    if (uint.TryParse(idMatch.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var actionId))
                        sync = TimelineSync.Ability(actionId);
                }
                else if (ExtractTrailingHexId(syncContent) is { } trailingId)
                {
                    // Old ACT network-log regex: the ability ID is the trailing hex token.
                    sync = TimelineSync.Ability(trailingId);
                }
            }
        }
        else
        {
            // New format: no sync /.../ wrapper at all.
            // Cactbot comments out sync lines deliberately (unreliable anchors like auto-attacks).
            // Only match network syncs in the uncommented portion. The FULL modifiers string still
            // feeds window/duration/jump/label parsing and ClassifyEntryType: the "# Raidwide" /
            // "# Tankbuster" comment annotations are load-bearing for classification.
            var hashIndex = modifiers.IndexOf('#');
            var uncommented = hashIndex >= 0 ? modifiers[..hashIndex] : modifiers;
            var netMatch = NetworkSyncPattern().Match(uncommented);
            if (netMatch.Success)
            {
                var ids = new List<uint>(4);
                foreach (Match idToken in HexIdListPattern().Matches(netMatch.Groups[2].Value))
                {
                    if (uint.TryParse(idToken.Groups[1].Value, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id) && id > 0)
                        ids.Add(id);
                }
                if (ids.Count > 0)
                {
                    var extra = ids.Count > 1 ? ids.GetRange(1, ids.Count - 1).ToArray() : null;
                    sync = netMatch.Groups[1].Value == "StartsUsing"
                        ? TimelineSync.StartsUsing(ids[0]) with { AdditionalActionIds = extra }
                        : TimelineSync.Ability(ids[0]) with { AdditionalActionIds = extra };
                }
            }
        }

        // Parse window modifier. Use `with` so AdditionalActionIds is preserved.
        var windowMatch = WindowPattern().Match(modifiers);
        if (windowMatch.Success && sync.HasValue)
        {
            if (float.TryParse(windowMatch.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var before) &&
                float.TryParse(windowMatch.Groups[2].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out var after))
            {
                sync = sync.Value with { WindowBefore = before, WindowAfter = after };
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
                    // Label already seen (backward reference) — resolve immediately
                    jumpTarget = labelTime;
                }
                else
                {
                    // Forward reference: label not yet seen during this pass.
                    // Register for resolution in the post-parse pass; leave jumpTarget as -1
                    // (HasJump returns false for -1, so this entry is safe until resolved).
                    //
                    // Index correctness: currentEntryIndex == entries.Count at call time, which
                    // is the index this entry will occupy after Add(entry.Value) returns in the
                    // caller. The registration is therefore correct for all entry types including
                    // label-only lines. Known limitation: Cactbot label lines (abilityName=="label")
                    // that simultaneously carry a jump modifier would also register here using the
                    // same index. This combination does not occur in practice in Cactbot timeline
                    // files (labels declare positions; jumps are on ordinary ability entries), so
                    // no structural fix is required.
                    pendingLabelJumps.Add((currentEntryIndex, labelName));
                }
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

    /// <summary>
    /// Extracts the trailing bare-hex ability ID from an old ACT-format sync regex,
    /// e.g. "^.{14} Erichthonios 6DA1" -&gt; 0x6DA1. Returns null when the last token is
    /// not a plausible hex action ID (zone-seal chat lines end in words, not hex).
    /// </summary>
    internal static uint? ExtractTrailingHexId(string syncContent)
    {
        var tokens = syncContent.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (tokens.Length == 0) return null;
        var last = tokens[^1].Trim(':', '/', ')');
        if (last.Length is < 2 or > 8) return null;
        return uint.TryParse(last, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var id) && id > 0
            ? id : null;
    }

    private static TimelineEntryType ClassifyEntryType(string abilityName, string modifiers)
    {
        var combined = abilityName + " " + modifiers;

        // Check in priority order. TankBuster is checked first because tank-buster names
        // sometimes also contain raidwide-sounding words (e.g. "stack cleave").
        foreach (var k in TankBusterKeywords)
            if (combined.Contains(k, StringComparison.OrdinalIgnoreCase))
                return TimelineEntryType.TankBuster;

        // Stack and Spread checked before general raidwide to preserve their specific types.
        // "stack" appeared in the old RaidwideKeywords set but has its own dedicated type.
        foreach (var k in StackKeywords)
            if (combined.Contains(k, StringComparison.OrdinalIgnoreCase))
                return TimelineEntryType.Stack;

        foreach (var k in SpreadKeywords)
            if (combined.Contains(k, StringComparison.OrdinalIgnoreCase))
                return TimelineEntryType.Spread;

        foreach (var k in RaidwideKeywords)
            if (combined.Contains(k, StringComparison.OrdinalIgnoreCase))
                return TimelineEntryType.Raidwide;

        if (combined.Contains("enrage", StringComparison.OrdinalIgnoreCase))
            return TimelineEntryType.Enrage;

        if (combined.Contains("adds", StringComparison.OrdinalIgnoreCase))
            return TimelineEntryType.Adds;

        // Phase markers
        if (combined.Contains("phase", StringComparison.OrdinalIgnoreCase) ||
            combined.Contains("--sync--", StringComparison.OrdinalIgnoreCase))
            return TimelineEntryType.Phase;

        return TimelineEntryType.Ability;
    }
}
