using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Timeline;
using Olympus.Timeline.Models;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Timeline tab: fight timeline state, predictions, and simulation controls.
/// </summary>
public static class TimelineTab
{
    public static void Draw(ITimelineService? timelineService, Configuration config)
    {
        if (timelineService == null)
        {
            ImGui.TextColored(new Vector4(1, 0.5f, 0, 1), "Timeline service not available");
            return;
        }

        // Simulation Controls
        DrawSimulationControls(timelineService);
        ImGui.Spacing();

        // Timeline State
        DrawTimelineState(timelineService);
        ImGui.Spacing();

        // Predictions
        DrawPredictions(timelineService);
        ImGui.Spacing();

        // Upcoming Mechanics
        DrawUpcomingMechanics(timelineService);
    }

    private static void DrawSimulationControls(ITimelineService timelineService)
    {
        ImGui.Text("Simulation Controls");
        ImGui.Separator();

        if (timelineService.IsSimulating)
        {
            ImGui.TextColored(new Vector4(0, 1, 0, 1), "● SIMULATION ACTIVE");
            ImGui.SameLine();

            if (ImGui.Button("Stop Simulation"))
            {
                timelineService.StopSimulation();
            }

            ImGui.Spacing();

            // Time controls
            ImGui.Text("Time Controls:");
            ImGui.SameLine();

            if (ImGui.Button("+5s"))
                timelineService.AdvanceSimulationTime(5f);
            ImGui.SameLine();

            if (ImGui.Button("+10s"))
                timelineService.AdvanceSimulationTime(10f);
            ImGui.SameLine();

            if (ImGui.Button("+30s"))
                timelineService.AdvanceSimulationTime(30f);
            ImGui.SameLine();

            if (ImGui.Button("Reset"))
            {
                timelineService.StopSimulation();
                timelineService.StartSimulation();
            }
        }
        else
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1), "○ Simulation Inactive");
            ImGui.SameLine();

            if (ImGui.Button("Start Simulation"))
            {
                timelineService.StartSimulation();
            }

            ImGui.Spacing();
            ImGui.TextWrapped("Start a simulation to test the timeline system without entering actual content. " +
                "The simulation runs a fake 2-minute fight with raidwides, tankbusters, and phase transitions.");
        }
    }

    private static void DrawTimelineState(ITimelineService timelineService)
    {
        ImGui.Text("Timeline State");
        ImGui.Separator();

        if (ImGui.BeginTable("TimelineStateTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Active status
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Status:");
            ImGui.TableNextColumn();
            var statusColor = timelineService.IsActive
                ? new Vector4(0, 1, 0, 1)
                : new Vector4(0.5f, 0.5f, 0.5f, 1);
            var statusText = timelineService.IsActive
                ? (timelineService.IsSimulating ? "Active (Simulating)" : "Active")
                : "Inactive";
            ImGui.TextColored(statusColor, statusText);

            // Fight name
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Fight:");
            ImGui.TableNextColumn();
            ImGui.Text(string.IsNullOrEmpty(timelineService.FightName) ? "None" : timelineService.FightName);

            // Current time
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Time:");
            ImGui.TableNextColumn();
            ImGui.Text($"{timelineService.CurrentTime:F1}s");

            // Current phase
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Phase:");
            ImGui.TableNextColumn();
            ImGui.Text(string.IsNullOrEmpty(timelineService.CurrentPhase) ? "None" : timelineService.CurrentPhase);

            // Confidence
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Confidence:");
            ImGui.TableNextColumn();
            var confidence = timelineService.Confidence;
            var confColor = confidence >= 0.8f ? new Vector4(0, 1, 0, 1)
                : confidence >= 0.5f ? new Vector4(1, 1, 0, 1)
                : new Vector4(1, 0.5f, 0, 1);
            ImGui.TextColored(confColor, $"{confidence:P0}");

            ImGui.EndTable();
        }
    }

    private static void DrawPredictions(ITimelineService timelineService)
    {
        ImGui.Text("Current Predictions");
        ImGui.Separator();

        if (!timelineService.IsActive)
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1), "No active timeline");
            return;
        }

        if (ImGui.BeginTable("PredictionsTable", 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("In", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableHeadersRow();

            // Next Raidwide
            var raidwide = timelineService.NextRaidwide;
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextColored(new Vector4(1, 0.3f, 0.3f, 1), "Raidwide");
            ImGui.TableNextColumn();
            ImGui.Text(raidwide?.Name ?? "-");
            ImGui.TableNextColumn();
            if (raidwide.HasValue)
            {
                var timeColor = GetTimeColor(raidwide.Value.SecondsUntil);
                ImGui.TextColored(timeColor, $"{raidwide.Value.SecondsUntil:F1}s");
            }
            else
            {
                ImGui.Text("-");
            }
            ImGui.TableNextColumn();
            DrawImminenceStatus(raidwide);

            // Next Tank Buster
            var tankbuster = timelineService.NextTankBuster;
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.TextColored(new Vector4(1, 0.6f, 0, 1), "TankBuster");
            ImGui.TableNextColumn();
            ImGui.Text(tankbuster?.Name ?? "-");
            ImGui.TableNextColumn();
            if (tankbuster.HasValue)
            {
                var timeColor = GetTimeColor(tankbuster.Value.SecondsUntil);
                ImGui.TextColored(timeColor, $"{tankbuster.Value.SecondsUntil:F1}s");
            }
            else
            {
                ImGui.Text("-");
            }
            ImGui.TableNextColumn();
            DrawImminenceStatus(tankbuster);

            ImGui.EndTable();
        }
    }

    private static void DrawUpcomingMechanics(ITimelineService timelineService)
    {
        ImGui.Text("Upcoming Mechanics (30s)");
        ImGui.Separator();

        if (!timelineService.IsActive)
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1), "No active timeline");
            return;
        }

        var upcoming = timelineService.GetUpcomingMechanics(30f);

        if (upcoming.Length == 0)
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1), "No mechanics in the next 30 seconds");
            return;
        }

        if (ImGui.BeginTable("UpcomingTable", 3, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp | ImGuiTableFlags.RowBg))
        {
            ImGui.TableSetupColumn("Time", ImGuiTableColumnFlags.WidthFixed, 60);
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("Name", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var mechanic in upcoming)
            {
                ImGui.TableNextRow();

                // Time
                ImGui.TableNextColumn();
                var timeColor = GetTimeColor(mechanic.SecondsUntil);
                ImGui.TextColored(timeColor, $"{mechanic.SecondsUntil:F1}s");

                // Type
                ImGui.TableNextColumn();
                var typeColor = GetTypeColor(mechanic.Type);
                ImGui.TextColored(typeColor, mechanic.Type.ToString());

                // Name
                ImGui.TableNextColumn();
                ImGui.Text(mechanic.Name);
            }

            ImGui.EndTable();
        }
    }

    private static void DrawImminenceStatus(MechanicPrediction? prediction)
    {
        if (!prediction.HasValue)
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1), "-");
            return;
        }

        var seconds = prediction.Value.SecondsUntil;

        if (seconds <= 3f)
        {
            ImGui.TextColored(new Vector4(1, 0, 0, 1), "IMMINENT!");
        }
        else if (seconds <= 8f)
        {
            ImGui.TextColored(new Vector4(1, 1, 0, 1), "Pre-shield");
        }
        else if (seconds <= 15f)
        {
            ImGui.TextColored(new Vector4(0, 1, 0, 1), "Prepare");
        }
        else
        {
            ImGui.TextColored(new Vector4(0.5f, 0.5f, 0.5f, 1), "Upcoming");
        }
    }

    private static Vector4 GetTimeColor(float seconds)
    {
        if (seconds <= 3f)
            return new Vector4(1, 0, 0, 1);       // Red - imminent
        if (seconds <= 8f)
            return new Vector4(1, 1, 0, 1);       // Yellow - soon
        if (seconds <= 15f)
            return new Vector4(0, 1, 0, 1);       // Green - prepare
        return new Vector4(0.7f, 0.7f, 0.7f, 1);  // Gray - far
    }

    private static Vector4 GetTypeColor(TimelineEntryType type)
    {
        return type switch
        {
            TimelineEntryType.Raidwide => new Vector4(1, 0.3f, 0.3f, 1),    // Red
            TimelineEntryType.TankBuster => new Vector4(1, 0.6f, 0, 1),     // Orange
            TimelineEntryType.Stack => new Vector4(0.3f, 0.3f, 1, 1),       // Blue
            TimelineEntryType.Spread => new Vector4(0.6f, 0, 1, 1),         // Purple
            TimelineEntryType.Adds => new Vector4(0, 0.8f, 0.8f, 1),        // Cyan
            TimelineEntryType.Enrage => new Vector4(1, 0, 0, 1),            // Bright red
            TimelineEntryType.Phase => new Vector4(0, 1, 0.5f, 1),          // Teal
            _ => new Vector4(0.7f, 0.7f, 0.7f, 1),                          // Gray
        };
    }
}
