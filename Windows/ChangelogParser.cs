using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Olympus.Windows;

public record ChangelogEntry(string Version, string[] Lines);

/// <summary>
/// Parses the embedded CHANGELOG.md and provides the 20 most recent entries.
/// Parsed lazily on first access and cached for the lifetime of the process.
/// </summary>
public static class ChangelogParser
{
    private static IReadOnlyList<ChangelogEntry>? entries;

    /// <summary>
    /// The 20 most recent changelog entries, parsed lazily on first access.
    /// </summary>
    public static IReadOnlyList<ChangelogEntry> Entries => entries ??= Parse();

    private static IReadOnlyList<ChangelogEntry> Parse()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly()
                .GetManifestResourceStream("Olympus.CHANGELOG.md");
            if (stream == null)
                return Array.Empty<ChangelogEntry>();

            using var reader = new StreamReader(stream);
            var content = reader.ReadToEnd();

            content = content
                .Replace("<!-- LATEST-START -->", "")
                .Replace("<!-- LATEST-END -->", "");

            // Split on "## v" — sections[0] is the file header (pre-version content), skip it.
            // Use StringSplitOptions.None so the header segment is always preserved at index 0
            // even if it becomes whitespace-only after stripping the comment markers.
            var sections = content.Split("## v", StringSplitOptions.None);
            var result = new List<ChangelogEntry>();

            for (var i = 1; i < sections.Length && result.Count < 20; i++)
            {
                var section = sections[i];
                var newlineIdx = section.IndexOfAny(['\n', '\r']);
                if (newlineIdx < 0) continue;

                var version = "v" + section[..newlineIdx].Trim();
                var body = section[newlineIdx..];
                var lines = body.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
                result.Add(new ChangelogEntry(version, lines));
            }

            return result;
        }
        catch
        {
            return Array.Empty<ChangelogEntry>();
        }
    }
}
