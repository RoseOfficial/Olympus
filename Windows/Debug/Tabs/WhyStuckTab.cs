using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
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
        ImGui.Text(Loc.T(LocalizedStrings.Debug.CurrentState, "Current State"));
        ImGui.Separator();

        // Show GCD readiness prominently
        var gcdReady = gcd.CanExecuteGcd;
        var gcdColor = gcdReady ? DebugColors.Success : DebugColors.Warning;
        var gcdText = gcdReady
            ? Loc.T(LocalizedStrings.Debug.GcdReady, "GCD READY")
            : Loc.TFormat(LocalizedStrings.Debug.GcdStateFormat, "GCD: {0} ({1}s)", gcd.State, gcd.GcdRemaining.ToString("F2"));
        ImGui.TextColored(gcdColor, gcdText);

        ImGui.SameLine();
        ImGui.TextColored(DebugColors.Dim, " | ");
        ImGui.SameLine();

        // Planning state with color
        var planColor = DebugColors.GetPlanningStateColor(rotation.PlanningState);
        ImGui.TextColored(planColor, Loc.TFormat(LocalizedStrings.Debug.PriorityFormat, "Priority: {0}", rotation.PlanningState));

        // Last action
        var actionColor = rotation.PlannedAction != "None" ? DebugColors.Success : DebugColors.Dim;
        ImGui.TextColored(actionColor, Loc.TFormat(LocalizedStrings.Debug.LastFormat, "Last: {0}", rotation.PlannedAction));
    }

    private static void DrawGcdPriorityChain(DebugRotationState rotation, DebugHealingState healing)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.GcdPriorityChainHeader, "GCD Priority Chain (checked top to bottom)"));
        ImGui.Separator();

        if (ImGui.BeginTable("GcdPriorityTable", 4, ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.Priority, "Priority"), ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.Category, "Category"), ImGuiTableColumnFlags.WidthFixed, 90);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.State, "State"), ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.Target, "Target"), ImGuiTableColumnFlags.WidthFixed, 120);
            ImGui.TableHeadersRow();

            // Priority 0.5: Esuna
            DrawPriorityRow("0.5", Loc.T(LocalizedStrings.Debug.Esuna, "Esuna"), rotation.EsunaState, rotation.EsunaTarget);

            // Priority 1: Raise
            DrawPriorityRow("1", Loc.T(LocalizedStrings.Debug.Raise, "Raise"), rotation.RaiseState, rotation.RaiseTarget);

            // Priority 2: AoE Heal
            var aoEDetails = healing.AoEInjuredCount > 0
                ? Loc.TFormat(LocalizedStrings.Debug.InjuredCountFormat, "{0} injured", healing.AoEInjuredCount)
                : "";
            DrawPriorityRow("2", Loc.T(LocalizedStrings.Debug.AoEHeal, "AoE Heal"), healing.AoEStatus, aoEDetails);

            // Priority 3: Single Heal
            var singleHealState = rotation.PlanningState == "Single Heal"
                ? Loc.T(LocalizedStrings.Debug.ActiveLabel, "Active")
                : Loc.T(LocalizedStrings.Debug.CheckingLabel, "Checking...");
            DrawPriorityRow("3", Loc.T(LocalizedStrings.Debug.SingleHeal, "Single Heal"), singleHealState, "");

            // Priority 4: Regen
            var regenState = rotation.PlanningState == "Regen"
                ? Loc.T(LocalizedStrings.Debug.ActiveLabel, "Active")
                : Loc.T(LocalizedStrings.Debug.CheckingLabel, "Checking...");
            DrawPriorityRow("4", Loc.T(LocalizedStrings.Debug.Regen, "Regen"), regenState, "");

            // Priority 5: DPS
            DrawPriorityRow("5", Loc.T(LocalizedStrings.Debug.DpsLabel, "DPS"), rotation.DpsState, rotation.TargetInfo);

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
        ImGui.Text(Loc.T(LocalizedStrings.Debug.OgcdState, "oGCD State"));
        ImGui.Separator();

        // Weave window status
        var weaveColor = gcd.CanExecuteOgcd ? DebugColors.Success : DebugColors.Dim;
        var weaveText = gcd.CanExecuteOgcd
            ? Loc.TFormat(LocalizedStrings.Debug.WeaveWindowOpen, "Weave Window Open ({0} slots)", gcd.WeaveSlots)
            : Loc.T(LocalizedStrings.Debug.NoWeaveWindow, "No Weave Window");
        ImGui.TextColored(weaveColor, weaveText);

        if (ImGui.BeginTable("OgcdTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.OgcdHeader, "oGCD"), ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.State, "State"), ImGuiTableColumnFlags.WidthStretch);

            DrawOgcdRow(Loc.T(LocalizedStrings.Debug.ThinAir, "Thin Air"), rotation.ThinAirState);
            DrawOgcdRow(Loc.T(LocalizedStrings.Debug.Asylum, "Asylum"), rotation.AsylumState + (rotation.AsylumTarget != "None" ? $" -> {rotation.AsylumTarget}" : ""));
            DrawOgcdRow(Loc.T(LocalizedStrings.Debug.Temperance, "Temperance"), rotation.TemperanceState);
            DrawOgcdRow(Loc.T(LocalizedStrings.Debug.Defensives, "Defensives"), rotation.DefensiveState);
            DrawOgcdRow(Loc.T(LocalizedStrings.Debug.Surecast, "Surecast"), rotation.SurecastState);

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
        ImGui.Text(Loc.T(LocalizedStrings.Debug.DpsDetails, "DPS Details"));
        ImGui.Separator();

        if (ImGui.BeginTable("DpsTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.TypeHeader, "Type"), ImGuiTableColumnFlags.WidthFixed, 100);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.State, "State"), ImGuiTableColumnFlags.WidthStretch);

            // Single target
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SingleTarget, "Single Target"));
            ImGui.TableNextColumn();
            var singleColor = GetStateColor(rotation.DpsState);
            ImGui.TextColored(singleColor, rotation.DpsState);

            // AoE
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.AoE, "AoE"));
            ImGui.TableNextColumn();
            var aoEText = Loc.TFormat(LocalizedStrings.Debug.AoEEnemiesFormat, "{0} ({1} enemies)", rotation.AoEDpsState, rotation.AoEDpsEnemyCount);
            var aoEColor = GetStateColor(rotation.AoEDpsState);
            ImGui.TextColored(aoEColor, aoEText);

            // Misery
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.AfflatusMisery, "Afflatus Misery"));
            ImGui.TableNextColumn();
            var miseryColor = GetStateColor(rotation.MiseryState);
            ImGui.TextColored(miseryColor, rotation.MiseryState);

            ImGui.EndTable();
        }
    }

    private static void DrawResources(DebugRotationState rotation)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.WhmResources, "WHM Resources"));
        ImGui.Separator();

        // Lily gauge (healing lilies)
        var lilyColor = rotation.LilyCount > 0 ? DebugColors.Success : DebugColors.Dim;
        ImGui.TextColored(lilyColor, Loc.TFormat(LocalizedStrings.Debug.LilyFormat, "Lily: {0}/3", rotation.LilyCount));
        if (rotation.LilyCount > 0)
        {
            ImGui.SameLine();
            ImGui.TextColored(DebugColors.Dim, Loc.T(LocalizedStrings.Debug.SolaceRaptureAvailable, " (Solace/Rapture available)"));
        }

        // Blood Lily gauge
        var bloodLilyColor = rotation.BloodLilyCount >= 3 ? DebugColors.Success : DebugColors.Dim;
        ImGui.TextColored(bloodLilyColor, Loc.TFormat(LocalizedStrings.Debug.BloodLilyFormat, "Blood Lily: {0}/3", rotation.BloodLilyCount));
        if (rotation.BloodLilyCount >= 3)
        {
            ImGui.SameLine();
            ImGui.TextColored(DebugColors.Success, Loc.T(LocalizedStrings.Debug.MiseryReady, " (Misery ready!)"));
        }

        // Lily Strategy
        ImGui.TextColored(DebugColors.Dim, Loc.TFormat(LocalizedStrings.Debug.LilyStrategyFormat, "Lily Strategy: {0}", rotation.LilyStrategy));

        // Sacred Sight stacks
        if (rotation.SacredSightStacks > 0)
        {
            ImGui.TextColored(DebugColors.Success, Loc.TFormat(LocalizedStrings.Debug.SacredSightFormat, "Sacred Sight: {0} stacks (instant Glare IV)", rotation.SacredSightStacks));
        }
        else
        {
            ImGui.TextColored(DebugColors.Dim, Loc.T(LocalizedStrings.Debug.SacredSightZero, "Sacred Sight: 0 stacks"));
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
