using System;
using Dalamud.Bindings.ImGui;

namespace Olympus.Windows.Debug;

/// <summary>
/// Shared helpers for job debug tabs to eliminate repeated table boilerplate.
/// </summary>
public static class DebugTabHelpers
{
    /// <summary>
    /// Renders a titled separator and a standard 2-column label/value ImGui table.
    /// The label column is fixed-width; the value column stretches.
    /// Call ImGui.TableNextRow / TableNextColumn inside drawRows.
    /// </summary>
    public static void DrawSection(string title, string tableName, Action drawRows, float labelWidth = 160f)
    {
        ImGui.Text(title);
        ImGui.Separator();
        if (!ImGui.BeginTable(tableName, 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
            return;
        ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, labelWidth);
        ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);
        drawRows();
        ImGui.EndTable();
    }
}
