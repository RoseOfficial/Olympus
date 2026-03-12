using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.AsclepiusCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Sage tab: Asclepius-specific debug info including Addersgall, Kardia, Eukrasia, shields, and DoTs.
/// </summary>
public static class AsclepiusTab
{
    public static void Draw(AsclepiusDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.SageNotActive, "Sage rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToSage, "Switch to Sage to see debug info."));
            return;
        }

        DrawOverviewSection(state);
        ImGui.Spacing();

        DrawResourcesSection(state);
        ImGui.Spacing();

        DrawKardiaSection(state);
        ImGui.Spacing();

        DrawEukrasiaSection(state);
        ImGui.Spacing();

        DrawHealingSection(state);
        ImGui.Spacing();

        DrawShieldsSection(state);
        ImGui.Spacing();

        DrawDpsSection(state);
    }

    private static void DrawOverviewSection(AsclepiusDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.TankStatus, "Status"));
        ImGui.Separator();

        if (ImGui.BeginTable("AsclepiusOverviewTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PlannedActionLabel, "Planned Action:"));
            ImGui.TableNextColumn();
            var actionColor = state.PlannedAction != "None" && !string.IsNullOrEmpty(state.PlannedAction)
                ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(actionColor, state.PlannedAction);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PlayerHpLabel, "Player HP:"));
            ImGui.TableNextColumn();
            var hpColor = state.PlayerHpPercent < 0.5f ? new Vector4(1f, 0.5f, 0.5f, 1f)
                : state.PlayerHpPercent < 0.8f ? new Vector4(1f, 1f, 0.5f, 1f)
                : new Vector4(0.5f, 1f, 0.5f, 1f);
            ImGui.TextColored(hpColor, $"{state.PlayerHpPercent:P0}");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PartyLabel, "Party:"));
            ImGui.TableNextColumn();
            ImGui.Text($"{state.PartyValidCount}/{state.PartyListCount} valid");

            ImGui.EndTable();
        }

        if (state.RaiseState != "Idle" || state.RaiseTarget != "None")
        {
            ImGui.Spacing();
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.5f, 1f), $"Raise: {state.RaiseState}");
            if (!string.IsNullOrEmpty(state.RaiseTarget) && state.RaiseTarget != "None")
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"→ {state.RaiseTarget}");
            }
        }

        if (state.EsunaState != "Idle" || state.EsunaTarget != "None")
        {
            ImGui.TextColored(new Vector4(0.5f, 1f, 1f, 1f), $"Esuna: {state.EsunaState}");
            if (!string.IsNullOrEmpty(state.EsunaTarget) && state.EsunaTarget != "None")
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"→ {state.EsunaTarget}");
            }
        }
    }

    private static void DrawResourcesSection(AsclepiusDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Resources, "Resources"));
        ImGui.Separator();

        if (ImGui.BeginTable("AsclepiusResourcesTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SgeAddersgall, "Addersgall:"));
            ImGui.TableNextColumn();
            var sgColor = state.AddersgallStacks switch
            {
                0 => new Vector4(1f, 0.5f, 0.5f, 1f),
                1 or 2 => new Vector4(1f, 1f, 0.5f, 1f),
                _ => new Vector4(0.5f, 1f, 0.5f, 1f)
            };
            ImGui.TextColored(sgColor, $"{state.AddersgallStacks}/3");
            ImGui.SameLine();
            ImGui.TextDisabled($"({state.AddersgallTimer:F1}s)");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SgeAddersting, "Adersting:"));
            ImGui.TableNextColumn();
            var stingColor = state.AdderstingStacks switch
            {
                0 => new Vector4(0.7f, 0.7f, 0.7f, 1f),
                1 or 2 => new Vector4(0.7f, 1f, 0.7f, 1f),
                _ => new Vector4(0.5f, 1f, 0.5f, 1f)
            };
            ImGui.TextColored(stingColor, $"{state.AdderstingStacks}/3");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SgeStrategy, "Strategy:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(state.AddersgallStrategy);

            ImGui.EndTable();
        }
    }

    private static void DrawKardiaSection(AsclepiusDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.SgeKardia, "Kardia"));
        ImGui.Separator();

        if (ImGui.BeginTable("AsclepiusKardiaTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SgeKardiaState, "Kardia:"));
            ImGui.TableNextColumn();
            if (state.KardiaState != "Idle" && state.KardiaState != "None")
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.8f, 1f), state.KardiaState);
            else
                ImGui.TextDisabled(state.KardiaState);

            if (!string.IsNullOrEmpty(state.KardiaTarget) && state.KardiaTarget != "None")
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"→ {state.KardiaTarget}");
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SgeSoteria, "Soteria:"));
            ImGui.TableNextColumn();
            if (state.SoteriaStacks > 0)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), $"{state.SoteriaStacks} stacks");
            else
                ImGui.TextDisabled(state.SoteriaState);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SgePhilosophia, "Philosophia:"));
            ImGui.TableNextColumn();
            if (state.PhilosophiaState != "Idle")
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), state.PhilosophiaState);
            else
                ImGui.TextDisabled(state.PhilosophiaState);

            ImGui.EndTable();
        }
    }

    private static void DrawEukrasiaSection(AsclepiusDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.SgeEukrasia, "Eukrasia"));
        ImGui.Separator();

        if (ImGui.BeginTable("AsclepiusEukrasiaTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SgeEukrasiaActive, "Eukrasia:"));
            ImGui.TableNextColumn();
            if (state.EukrasiaActive)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SgeZoe, "Zoe:"));
            ImGui.TableNextColumn();
            if (state.ZoeActive)
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SgeEukrasiaState, "Eukrasia State:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(state.EukrasiaState);

            ImGui.EndTable();
        }
    }

    private static void DrawHealingSection(AsclepiusDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Healing, "Healing"));
        ImGui.Separator();

        if (ImGui.BeginTable("AsclepiusHealingTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            void DrawStateRow(string label, string stateVal)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(label);
                ImGui.TableNextColumn();
                if (stateVal == "Idle")
                    ImGui.TextDisabled(stateVal);
                else
                    ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), stateVal);
            }

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.AoEHealLabel, "AoE Heal:"));
            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.InjuredFormat, "{0} ({1} injured)", state.AoEStatus, state.AoEInjuredCount));

            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgeDruochole, "Druochole:"), state.DruocholeState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgeTaurochole, "Taurochole:"), state.TaurocholeState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgeIxochole, "Ixochole:"), state.IxocholeState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgeKerachole, "Kerachole:"), state.KeracholeState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgePneuma, "Pneuma:"), state.PneumaState);

            if (state.LastHealAmount > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(Loc.T(LocalizedStrings.Debug.LastHeal, "Last Heal:"));
                ImGui.TableNextColumn();
                ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.HpFormat, "{0:N0} HP", state.LastHealAmount));
                if (!string.IsNullOrEmpty(state.LastHealStats))
                    ImGui.TextDisabled(state.LastHealStats);
            }

            ImGui.EndTable();
        }
    }

    private static void DrawShieldsSection(AsclepiusDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.SgeShields, "Shields"));
        ImGui.Separator();

        if (ImGui.BeginTable("AsclepiusShieldsTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            void DrawStateRow(string label, string stateVal, string? target = null)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(label);
                ImGui.TableNextColumn();
                if (stateVal == "Idle")
                    ImGui.TextDisabled(stateVal);
                else
                    ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), stateVal);
                if (target != null && target != "None" && !string.IsNullOrEmpty(target))
                {
                    ImGui.SameLine();
                    ImGui.TextDisabled($"→ {target}");
                }
            }

            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgeHaima, "Haima:"), state.HaimaState, state.HaimaTarget);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgePanhaima, "Panhaima:"), state.PanhaimaState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgeEukrasianDiagnosis, "Eukrasian Diagnosis:"), state.EukrasianDiagnosisState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgeEukrasianPrognosis, "Eukrasian Prognosis:"), state.EukrasianPrognosisState);

            // Buffs that complement shields
            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgePhysisII, "Physis II:"), state.PhysisIIState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgeHolos, "Holos:"), state.HolosState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgeKrasis, "Krasis:"), state.KrasisState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgePepsis, "Pepsis:"), state.PepsisState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.SgeRhizomata, "Rhizomata:"), state.RhizomataState);

            ImGui.EndTable();
        }
    }

    private static void DrawDpsSection(AsclepiusDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.DpsSection, "DPS"));
        ImGui.Separator();

        if (ImGui.BeginTable("AsclepiusDpsTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 160);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DoT, "DoT:"));
            ImGui.TableNextColumn();
            if (state.DoTState != "Idle" && state.DoTRemaining > 0f)
            {
                var dotColor = state.DoTRemaining < 3f
                    ? new Vector4(1f, 0.5f, 0.5f, 1f)
                    : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(dotColor, $"{state.DoTState} ({state.DoTRemaining:F1}s)");
            }
            else
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.NotApplied, "Not applied"));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SgePhlegma, "Phlegma:"));
            ImGui.TableNextColumn();
            var phlegmaColor = state.PhlegmaCharges > 0
                ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(phlegmaColor, $"{state.PhlegmaCharges} charges");
            ImGui.SameLine();
            ImGui.TextDisabled($"({state.PhlegmaState})");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SgeToxikon, "Toxikon:"));
            ImGui.TableNextColumn();
            if (state.ToxikonState != "Idle")
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), state.ToxikonState);
            else
                ImGui.TextDisabled(state.ToxikonState);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SgePsyche, "Psyche:"));
            ImGui.TableNextColumn();
            if (state.PsycheState != "Idle")
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), state.PsycheState);
            else
                ImGui.TextDisabled(state.PsycheState);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DpsStateLabel, "DPS State:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(state.DpsState);

            ImGui.EndTable();
        }
    }
}
