using System.Numerics;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Windowing;
using Olympus.Localization;
using Olympus.Windows.Config;

namespace Olympus.Windows;

/// <summary>
/// Scrollable window showing the 20 most recent plugin changelog entries.
/// </summary>
public sealed class ChangelogWindow : Window
{
    public ChangelogWindow() : base("Olympus Changelog", ImGuiWindowFlags.None)
    {
        Size = new Vector2(500, 600);
        SizeCondition = ImGuiCond.FirstUseEver;
    }

    public override void Draw()
    {
        var entries = ChangelogParser.Entries;
        if (entries.Count == 0)
        {
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Changelog.NoChangelog, "No changelog available."));
            return;
        }

        ImGui.BeginChild("##changelog", new Vector2(0, 0), false);

        var accentBlue = ConfigUIHelpers.AccentBlue;
        var first = true;

        foreach (var entry in entries)
        {
            if (!first)
                ImGui.Separator();
            first = false;

            ImGui.TextColored(accentBlue, entry.Version);

            foreach (var line in entry.Lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.StartsWith("###"))
                {
                    ImGui.TextDisabled(line.TrimStart('#').Trim());
                }
                else
                {
                    var text = line.StartsWith("- ") ? line[2..] : line;
                    ImGui.Text($"• {text}");
                }
            }
        }

        ImGui.EndChild();
    }
}
