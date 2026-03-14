using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.NyxCore.Context;
using Olympus.Windows.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Dark Knight tab: Nyx-specific debug info including Blood Gauge, Darkside, and defensive cooldowns.
/// </summary>
public static class NyxTab
{
    public static void Draw(NyxDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.DarkKnightNotActive, "Dark Knight rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToDarkKnight, "Switch to Dark Knight to see debug info."));
            return;
        }

        DrawStatusSection(state);
        ImGui.Spacing();

        DrawResourcesSection(state);
        ImGui.Spacing();

        DrawDarksideSection(state);
        ImGui.Spacing();

        DrawComboSection(state);
        ImGui.Spacing();

        DrawBuffSection(state);
        ImGui.Spacing();

        DrawDefensiveSection(state);
        ImGui.Spacing();

        DrawModuleStates(state);
    }

    private static void DrawStatusSection(NyxDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.TankStatus, "Status"), "NyxStatusTable", () =>
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
            if (state.HasGrit)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Grit, "Grit"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.Grit, "Grit"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PlannedActionLabel, "Planned Action:"));
            ImGui.TableNextColumn();
            var actionColor = !string.IsNullOrEmpty(state.PlannedAction) && state.PlannedAction != "None"
                ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(actionColor, string.IsNullOrEmpty(state.PlannedAction) ? "—" : state.PlannedAction);
        });
    }

    private static void DrawResourcesSection(NyxDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Resources, "Resources"), "NyxResourcesTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.BloodGaugeLabel, "Blood Gauge:"));
            ImGui.TableNextColumn();
            ImGui.ProgressBar(state.BloodGauge / 100f, new Vector2(-1, 0), $"{state.BloodGauge}/100");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.MpLabel, "MP:"));
            ImGui.TableNextColumn();
            var mpPercent = state.MaxMp > 0 ? (float)state.CurrentMp / state.MaxMp : 0f;
            ImGui.ProgressBar(mpPercent, new Vector2(-1, 0), $"{state.CurrentMp:N0}/{state.MaxMp:N0}");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.NearbyEnemies, "Nearby Enemies:"));
            ImGui.TableNextColumn();
            ImGui.Text($"{state.NearbyEnemies}");
        });
    }

    private static void DrawDarksideSection(NyxDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Darkside, "Darkside"), "NyxDarksideTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Darkside, "Darkside:"));
            ImGui.TableNextColumn();
            if (state.HasDarkside)
                ImGui.TextColored(new Vector4(0.6f, 0.3f, 1f, 1f), Loc.T(LocalizedStrings.Debug.DarksideTimer, $"{state.DarksideRemaining:F1}s"));
            else
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Inactive");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DarkArts, "Dark Arts (TBN):"));
            ImGui.TableNextColumn();
            if (state.HasDarkArts)
                ImGui.TextColored(new Vector4(0.6f, 0.3f, 1f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SaltedEarth, "Salted Earth:"));
            ImGui.TableNextColumn();
            if (state.HasSaltedEarth)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "—"));
        });
    }

    private static void DrawComboSection(NyxDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Combo, "Combo"), "NyxComboTable", () =>
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
        });
    }

    private static void DrawBuffSection(NyxDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs"), "NyxBuffTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.BloodWeapon, "Blood Weapon:"));
            ImGui.TableNextColumn();
            if (state.HasBloodWeapon)
                ImGui.TextColored(new Vector4(1f, 0.3f, 0.3f, 1f), $"{state.BloodWeaponRemaining:F1}s");
            else
                ImGui.TextDisabled("—");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Delirium, "Delirium:"));
            ImGui.TableNextColumn();
            if (state.HasDelirium)
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.8f, 1f), Loc.T(LocalizedStrings.Debug.DeliriumStacksLabel, $"{state.DeliriumStacks} stacks"));
            else
                ImGui.TextDisabled("—");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ScornfulEdge, "Scornful Edge:"));
            ImGui.TableNextColumn();
            if (state.HasScornfulEdge)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Ready, "Ready"));
            else
                ImGui.TextDisabled("—");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.LivingShadow, "Living Shadow:"));
            ImGui.TableNextColumn();
            if (state.HasLivingShadow)
                ImGui.TextColored(new Vector4(0.6f, 0.3f, 1f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            else
                ImGui.TextDisabled("—");
        });
    }

    private static void DrawDefensiveSection(NyxDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.MitigationHeader, "Defensive"), "NyxDefTable", () =>
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

            DrawBoolRow(Loc.T(LocalizedStrings.Debug.TheBlackestNight, "TBN:"), state.HasTheBlackestNight);
            DrawBoolRow(Loc.T(LocalizedStrings.Debug.ShadowWall, "Shadow Wall:"), state.HasShadowWall);
            DrawBoolRow(Loc.T(LocalizedStrings.Debug.DarkMindBuff, "Dark Mind:"), state.HasDarkMind);
            DrawBoolRow(Loc.T(LocalizedStrings.Debug.DrkOblation, "Oblation:"), state.HasOblation);
            DrawBoolRow(Loc.T(LocalizedStrings.Debug.LivingDead, "Living Dead:"), state.HasLivingDead);
            DrawBoolRow(Loc.T(LocalizedStrings.Debug.WalkingDead, "Walking Dead:"), state.HasWalkingDead, "WALKING DEAD");

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

    private static void DrawModuleStates(NyxDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.ModuleStatesHeader, "Module States"), "NyxModuleTable", () =>
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
