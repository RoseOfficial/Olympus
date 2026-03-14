using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.HephaestusCore.Context;
using Olympus.Windows.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Gunbreaker tab: Hephaestus-specific debug info including Cartridges, Gnashing Fang combo, and Continuation.
/// </summary>
public static class HephaestusTab
{
    public static void Draw(HephaestusDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.GunbreakerNotActive, "Gunbreaker rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToGunbreaker, "Switch to Gunbreaker to see debug info."));
            return;
        }

        DrawStatusSection(state);
        ImGui.Spacing();

        DrawResourcesSection(state);
        ImGui.Spacing();

        DrawComboSection(state);
        ImGui.Spacing();

        DrawContinuationSection(state);
        ImGui.Spacing();

        DrawBuffSection(state);
        ImGui.Spacing();

        DrawDefensiveSection(state);
        ImGui.Spacing();

        DrawModuleStates(state);
    }

    private static void DrawStatusSection(HephaestusDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.TankStatus, "Status"), "HephStatusTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.IsMainTankLabel, "Role:"));
            ImGui.TableNextColumn();
            if (state.IsMainTank)
                ImGui.TextColored(new Vector4(0.4f, 0.8f, 1f, 1f), Loc.T(LocalizedStrings.Debug.MainTankValue, "Main Tank"));
            else
                ImGui.TextColored(new Vector4(0.8f, 0.8f, 0.4f, 1f), Loc.T(LocalizedStrings.Debug.OffTankValue, "Off Tank"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TankStance, "Stance:"));
            ImGui.TableNextColumn();
            if (state.HasRoyalGuard)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.RoyalGuard, "Royal Guard"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.RoyalGuard, "Royal Guard"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PlannedActionLabel, "Planned Action:"));
            ImGui.TableNextColumn();
            var actionColor = !string.IsNullOrEmpty(state.PlannedAction) && state.PlannedAction != "None"
                ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(actionColor, string.IsNullOrEmpty(state.PlannedAction) ? "—" : state.PlannedAction);
        });
    }

    private static void DrawResourcesSection(HephaestusDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Resources, "Resources"), "HephResourcesTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.CartridgesLabel, "Cartridges:"));
            ImGui.TableNextColumn();
            var cartColor = state.Cartridges switch
            {
                0 => new Vector4(1f, 0.5f, 0.5f, 1f),
                1 => new Vector4(1f, 1f, 0.5f, 1f),
                2 => new Vector4(0.7f, 1f, 0.7f, 1f),
                _ => new Vector4(0.5f, 1f, 0.5f, 1f)
            };
            ImGui.TextColored(cartColor, $"{state.Cartridges}/3");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.NearbyEnemies, "Nearby Enemies:"));
            ImGui.TableNextColumn();
            ImGui.Text($"{state.NearbyEnemies}");
        });
    }

    private static void DrawComboSection(HephaestusDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Combo, "Combo"), "HephComboTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ComboState, "Combo Step:"));
            ImGui.TableNextColumn();
            ImGui.Text($"{state.ComboStep}");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ComboTimer, "Combo Timer:"));
            ImGui.TableNextColumn();
            var timerColor = state.ComboTimeRemaining < 5f
                ? new Vector4(1f, 0.5f, 0.5f, 1f)
                : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(timerColor, $"{state.ComboTimeRemaining:F1}s");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.GnashingFangStepLabel, "GNF Step:"));
            ImGui.TableNextColumn();
            if (state.IsInGnashingFangCombo)
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), $"{state.GnashingFangStep}");
            else
                ImGui.TextDisabled("—");
        });
    }

    private static void DrawContinuationSection(HephaestusDebugState state)
    {
        DebugTabHelpers.DrawSection("Continuation", "HephContTable", () =>
        {
            void DrawReadyRow(string label, bool ready)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(label);
                ImGui.TableNextColumn();
                if (ready)
                    ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Ready, "Ready"));
                else
                    ImGui.TextDisabled("—");
            }

            DrawReadyRow(Loc.T(LocalizedStrings.Debug.ReadyToRip, "Ready to Rip:"), state.IsReadyToRip);
            DrawReadyRow(Loc.T(LocalizedStrings.Debug.ReadyToTear, "Ready to Tear:"), state.IsReadyToTear);
            DrawReadyRow(Loc.T(LocalizedStrings.Debug.ReadyToGouge, "Ready to Gouge:"), state.IsReadyToGouge);
            DrawReadyRow(Loc.T(LocalizedStrings.Debug.ReadyToBlast, "Ready to Blast:"), state.IsReadyToBlast);
            DrawReadyRow(Loc.T(LocalizedStrings.Debug.ReadyToReign, "Ready to Reign:"), state.IsReadyToReign);
        });
    }

    private static void DrawBuffSection(HephaestusDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs"), "HephBuffTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.NoMercy, "No Mercy:"));
            ImGui.TableNextColumn();
            if (state.HasNoMercy)
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.NoMercyRemaining:F1}s");
            else
                ImGui.TextDisabled("—");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SonicBreak, "Sonic Break DoT:"));
            ImGui.TableNextColumn();
            if (state.HasSonicBreakDot)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.NotApplied, "Not applied"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.BowShock, "Bow Shock DoT:"));
            ImGui.TableNextColumn();
            if (state.HasBowShockDot)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.NotApplied, "Not applied"));
        });
    }

    private static void DrawDefensiveSection(HephaestusDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.MitigationHeader, "Defensive"), "HephDefTable", () =>
        {
            void DrawBoolRow(string label, bool active, string activeText = "Active")
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(label);
                ImGui.TableNextColumn();
                if (active)
                    ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), activeText);
                else
                    ImGui.TextDisabled("—");
            }

            DrawBoolRow(Loc.T(LocalizedStrings.Debug.Superbolide, "Superbolide:"), state.HasSuperbolide, "INVULN");
            DrawBoolRow(Loc.T(LocalizedStrings.Debug.Nebula, "Nebula:"), state.HasNebula);
            DrawBoolRow(Loc.T(LocalizedStrings.Debug.HeartOfCorundum, "Heart of Corundum:"), state.HasHeartOfCorundum);
            DrawBoolRow(Loc.T(LocalizedStrings.Debug.Camouflage, "Camouflage:"), state.HasCamouflage);
            DrawBoolRow(Loc.T(LocalizedStrings.Debug.Aurora, "Aurora:"), state.HasAurora);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ActiveMitigationsLabel, "Active:"));
            ImGui.TableNextColumn();
            if (state.HasActiveMitigation && !string.IsNullOrEmpty(state.ActiveMitigations))
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), state.ActiveMitigations);
            else
                ImGui.TextDisabled("—");
        });
    }

    private static void DrawModuleStates(HephaestusDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.ModuleStatesHeader, "Module States"), "HephModuleTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DamageStateLabel, "Damage:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(string.IsNullOrEmpty(state.DamageState) ? "—" : state.DamageState);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.MitigationStateLabel, "Mitigation:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(string.IsNullOrEmpty(state.MitigationState) ? "—" : state.MitigationState);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.BuffStateLabel, "Buff:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(string.IsNullOrEmpty(state.BuffState) ? "—" : state.BuffState);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.EnmityStateLabel, "Enmity:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(string.IsNullOrEmpty(state.EnmityState) ? "—" : state.EnmityState);
        });
    }
}
