using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Services.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Why Stuck tab: shows all decision states to diagnose why rotation isn't casting.
/// </summary>
public static class WhyStuckTab
{
    public static void Draw(DebugSnapshot snapshot, Configuration config)
    {
        var rotation = snapshot.Rotation;
        var healing = snapshot.Healing;
        var gcd = snapshot.GcdState;

        // Current State Summary
        DrawCurrentState(rotation, gcd);
        ImGui.Spacing();

        // GCD Priority Chain
        if (IsSectionVisible(config, "GcdPriority"))
        {
            DrawGcdPriorityChain(rotation, healing);
            ImGui.Spacing();
        }

        // oGCD State
        if (IsSectionVisible(config, "OgcdState"))
        {
            DrawOgcdState(rotation, gcd);
            ImGui.Spacing();
        }

        // DPS Details
        if (IsSectionVisible(config, "DpsDetails"))
        {
            DrawDpsDetails(rotation);
            ImGui.Spacing();
        }

        // Resources
        if (IsSectionVisible(config, "Resources"))
        {
            DrawResources(rotation);
        }
    }

    private static void DrawCurrentState(DebugRotationState rotation, DebugGcdState gcd)
    {
        ImGui.Text("Current State");
        ImGui.Separator();

        // Show GCD readiness prominently
        var gcdReady = gcd.CanExecuteGcd;
        var gcdColor = gcdReady ? DebugColors.Success : DebugColors.Warning;
        var gcdText = gcdReady ? "GCD READY" : $"GCD: {gcd.State} ({gcd.GcdRemaining:F2}s)";
        ImGui.TextColored(gcdColor, gcdText);

        ImGui.SameLine();
        ImGui.TextColored(DebugColors.Dim, " | ");
        ImGui.SameLine();

        // Planning state with color
        var planColor = DebugColors.GetPlanningStateColor(rotation.PlanningState);
        ImGui.TextColored(planColor, $"Priority: {rotation.PlanningState}");

        // Last action
        var actionColor = rotation.PlannedAction != "None" ? DebugColors.Success : DebugColors.Dim;
        ImGui.TextColored(actionColor, $"Last: {rotation.PlannedAction}");
    }

    private static void DrawGcdPriorityChain(DebugRotationState rotation, DebugHealingState healing)
    {
        ImGui.Text("GCD Priority Chain (checked top to bottom)");
        ImGui.Separator();

        if (ImGui.BeginTable("GcdPriorityTable", 4, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Priority", ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn("Category", ImGuiTableColumnFlags.WidthFixed, 90);
            ImGui.TableSetupColumn("State", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Target", ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableHeadersRow();

            // Priority 0.5: Esuna
            DrawPriorityRow("0.5", "Esuna", rotation.EsunaState, rotation.EsunaTarget);

            // Priority 1: Raise
            DrawPriorityRow("1", "Raise", rotation.RaiseState, rotation.RaiseTarget);

            // Priority 2: AoE Heal
            var aoEDetails = healing.AoEInjuredCount > 0 ? $"{healing.AoEInjuredCount} injured" : "";
            DrawPriorityRow("2", "AoE Heal", healing.AoEStatus, aoEDetails);

            // Priority 3: Single Heal
            var singleHealState = rotation.PlanningState == "Single Heal" ? "Active" : "Checking...";
            DrawPriorityRow("3", "Single Heal", singleHealState, "");

            // Priority 4: Regen
            var regenState = rotation.PlanningState == "Regen" ? "Active" : "Checking...";
            DrawPriorityRow("4", "Regen", regenState, "");

            // Priority 5: DPS
            DrawPriorityRow("5", "DPS", rotation.DpsState, rotation.TargetInfo);

            ImGui.EndTable();
        }
    }

    private static void DrawPriorityRow(string priority, string category, string state, string target)
    {
        ImGui.TableNextRow();

        ImGui.TableNextColumn();
        ImGui.TextColored(DebugColors.Dim, priority);

        ImGui.TableNextColumn();
        ImGui.Text(category);

        ImGui.TableNextColumn();
        var stateColor = GetStateColor(state);
        ImGui.TextColored(stateColor, state);

        ImGui.TableNextColumn();
        if (!string.IsNullOrEmpty(target) && target != "None")
        {
            ImGui.TextColored(DebugColors.Dim, target);
        }
    }

    private static void DrawOgcdState(DebugRotationState rotation, DebugGcdState gcd)
    {
        ImGui.Text("oGCD State");
        ImGui.Separator();

        // Weave window status
        var weaveColor = gcd.CanExecuteOgcd ? DebugColors.Success : DebugColors.Dim;
        var weaveText = gcd.CanExecuteOgcd ? $"Weave Window Open ({gcd.WeaveSlots} slots)" : "No Weave Window";
        ImGui.TextColored(weaveColor, weaveText);

        if (ImGui.BeginTable("OgcdTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("oGCD", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("State", ImGuiTableColumnFlags.WidthStretch);

            DrawOgcdRow("Thin Air", rotation.ThinAirState);
            DrawOgcdRow("Asylum", rotation.AsylumState + (rotation.AsylumTarget != "None" ? $" -> {rotation.AsylumTarget}" : ""));
            DrawOgcdRow("Temperance", rotation.TemperanceState);
            DrawOgcdRow("Defensives", rotation.DefensiveState);
            DrawOgcdRow("Surecast", rotation.SurecastState);

            ImGui.EndTable();
        }
    }

    private static void DrawOgcdRow(string name, string state)
    {
        ImGui.TableNextRow();
        ImGui.TableNextColumn();
        ImGui.Text(name);
        ImGui.TableNextColumn();
        var color = GetStateColor(state);
        ImGui.TextColored(color, state);
    }

    private static void DrawDpsDetails(DebugRotationState rotation)
    {
        ImGui.Text("DPS Details");
        ImGui.Separator();

        if (ImGui.BeginTable("DpsTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Type", ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn("State", ImGuiTableColumnFlags.WidthStretch);

            // Single target
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Single Target");
            ImGui.TableNextColumn();
            var singleColor = GetStateColor(rotation.DpsState);
            ImGui.TextColored(singleColor, rotation.DpsState);

            // AoE
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("AoE");
            ImGui.TableNextColumn();
            var aoEText = $"{rotation.AoEDpsState} ({rotation.AoEDpsEnemyCount} enemies)";
            var aoEColor = GetStateColor(rotation.AoEDpsState);
            ImGui.TextColored(aoEColor, aoEText);

            // Misery
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Afflatus Misery");
            ImGui.TableNextColumn();
            var miseryColor = GetStateColor(rotation.MiseryState);
            ImGui.TextColored(miseryColor, rotation.MiseryState);

            ImGui.EndTable();
        }
    }

    private static void DrawResources(DebugRotationState rotation)
    {
        ImGui.Text("WHM Resources");
        ImGui.Separator();

        // Lily gauge (healing lilies)
        var lilyColor = rotation.LilyCount > 0 ? DebugColors.Success : DebugColors.Dim;
        ImGui.TextColored(lilyColor, $"Lily: {rotation.LilyCount}/3");
        if (rotation.LilyCount > 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(DebugColors.Dim, " (Solace/Rapture available)");
        }

        // Blood Lily gauge
        var bloodLilyColor = rotation.BloodLilyCount >= 3 ? DebugColors.Success : DebugColors.Dim;
        ImGui.TextColored(bloodLilyColor, $"Blood Lily: {rotation.BloodLilyCount}/3");
        if (rotation.BloodLilyCount >= 3)
        {
            ImGui.SameLine();
            ImGui.TextColored(DebugColors.Success, " (Misery ready!)");
        }

        // Lily Strategy
        ImGui.TextColored(DebugColors.Dim, $"Lily Strategy: {rotation.LilyStrategy}");

        // Sacred Sight stacks
        if (rotation.SacredSightStacks > 0)
        {
            ImGui.TextColored(DebugColors.Success, $"Sacred Sight: {rotation.SacredSightStacks} stacks (instant Glare IV)");
        }
        else
        {
            ImGui.TextColored(DebugColors.Dim, "Sacred Sight: 0 stacks");
        }
    }

    private static Vector4 GetStateColor(string state)
    {
        if (string.IsNullOrEmpty(state))
            return DebugColors.Dim;

        // Green: Active/Ready/Executing states
        if (state.StartsWith("Executing") ||
            state.StartsWith("Active") ||
            state == "Ready" ||
            state.StartsWith("Swiftcast") ||
            state.StartsWith("Hardcast") ||
            state.StartsWith("Cleansing") ||
            state.Contains("Glare") ||
            state.Contains("Damage:") ||
            state.Contains("DoT"))
            return DebugColors.Success;

        // Yellow: Waiting/Blocked but recoverable
        if (state.StartsWith("Waiting") ||
            state.StartsWith("CD:") ||
            state.StartsWith("On cooldown") ||
            state.StartsWith("MP ") ||
            state.StartsWith("Level ") ||
            state.Contains("< min") ||
            state.Contains("/3 Blood"))
            return DebugColors.Warning;

        // Red: Disabled/No target/Error states
        if (state == "Disabled" ||
            state == "No target" ||
            state == "No enemy found" ||
            state.StartsWith("Healing disabled") ||
            state.StartsWith("Manual mode") ||
            state == "Moving")
            return DebugColors.Failure;

        // Default gray
        return DebugColors.Dim;
    }

    private static bool IsSectionVisible(Configuration config, string section)
    {
        if (config.Debug.DebugSectionVisibility.TryGetValue(section, out var visible))
            return visible;
        return true; // Default to visible
    }
}
