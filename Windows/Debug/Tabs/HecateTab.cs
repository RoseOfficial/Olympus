using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.HecateCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Black Mage tab: Hecate-specific debug info including element state, MP, and Enochian tracking.
/// </summary>
public static class HecateTab
{
    public static void Draw(HecateDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.BlackMageNotActive, "Black Mage rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToBlackMage, "Switch to Black Mage to see debug info."));
            return;
        }

        // Element Section
        DrawElementSection(state);
        ImGui.Spacing();

        // Resource Section
        DrawResourceSection(state);
        ImGui.Spacing();

        // Buffs Section
        DrawBuffSection(state);
        ImGui.Spacing();

        // Cooldowns Section
        DrawCooldownSection(state);
        ImGui.Spacing();

        // Target Section
        DrawTargetSection(state);
    }

    private static void DrawElementSection(HecateDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.ElementState, "Element State"));
        ImGui.Separator();

        if (ImGui.BeginTable("BlmElementTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Phase
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PhaseLabel, "Phase:"));
            ImGui.TableNextColumn();
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 1f, 1f), state.Phase);

            // Element
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Element, "Element:"));
            ImGui.TableNextColumn();
            if (state.InAstralFire)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{Loc.T(LocalizedStrings.Debug.AstralFire, "Astral Fire")} x{state.ElementStacks}");
            }
            else if (state.InUmbralIce)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), $"{Loc.T(LocalizedStrings.Debug.UmbralIce, "Umbral Ice")} x{state.ElementStacks}");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.NoneLabel, "None"));
            }

            // Element Timer
            if (state.ElementTimer > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(Loc.T(LocalizedStrings.Debug.ElementTimer, "Element Timer:"));
                ImGui.TableNextColumn();
                var timerColor = state.ElementTimer < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(timerColor, $"{state.ElementTimer:F1}s");
            }

            // Enochian
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Enochian, "Enochian:"));
            ImGui.TableNextColumn();
            if (state.IsEnochianActive)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            ImGui.EndTable();
        }
    }

    private static void DrawResourceSection(HecateDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Resources, "Resources"));
        ImGui.Separator();

        if (ImGui.BeginTable("BlmResourceTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // MP
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Mp, "MP:"));
            ImGui.TableNextColumn();
            var mpPercent = state.MaxMp > 0 ? (float)state.CurrentMp / state.MaxMp : 0;
            ImGui.ProgressBar(mpPercent, new Vector2(-1, 0), $"{state.CurrentMp:N0}/{state.MaxMp:N0}");

            // Umbral Hearts
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.UmbralHearts, "Umbral Hearts:"));
            ImGui.TableNextColumn();
            var heartColor = state.UmbralHearts >= 3 ? new Vector4(0.5f, 0.8f, 1f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(heartColor, $"{state.UmbralHearts}/3");

            // Polyglot
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Polyglot, "Polyglot:"));
            ImGui.TableNextColumn();
            var polyColor = state.PolyglotStacks >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.PolyglotStacks >= 2 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(polyColor, $"{state.PolyglotStacks}/3");

            // Astral Soul
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.AstralSoul, "Astral Soul:"));
            ImGui.TableNextColumn();
            var astralColor = state.AstralSoulStacks >= 6 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(astralColor, $"{state.AstralSoulStacks}/6");

            // Paradox
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Paradox, "Paradox:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasParadox);

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(HecateDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs"));
        ImGui.Separator();

        if (ImGui.BeginTable("BlmBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Firestarter
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Firestarter, "Firestarter:"));
            ImGui.TableNextColumn();
            if (state.HasFirestarter)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.FirestarterRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.No, "No"));
            }

            // Thunderhead
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Thunderhead, "Thunderhead:"));
            ImGui.TableNextColumn();
            if (state.HasThunderhead)
            {
                ImGui.TextColored(new Vector4(0.8f, 0.5f, 1f, 1f), $"{state.ThunderheadRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.No, "No"));
            }

            // Ley Lines
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.LeyLines, "Ley Lines:"));
            ImGui.TableNextColumn();
            if (state.HasLeyLines)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.8f, 1f), $"{state.LeyLinesRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            // Triplecast
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Triplecast, "Triplecast:"));
            ImGui.TableNextColumn();
            if (state.TriplecastStacks > 0)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.TFormat(LocalizedStrings.Debug.StacksFormat, "{0} stacks", state.TriplecastStacks));
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.No, "No"));
            }

            // Swiftcast
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Swiftcast, "Swiftcast:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasSwiftcast);

            ImGui.EndTable();
        }
    }

    private static void DrawCooldownSection(HecateDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Cooldowns, "Cooldowns"));
        ImGui.Separator();

        if (ImGui.BeginTable("BlmCooldownTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Triplecast Charges
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TriplecastCharges, "Triplecast Charges:"));
            ImGui.TableNextColumn();
            var tripleColor = state.TriplecastCharges >= 2 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.TriplecastCharges >= 1 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(tripleColor, $"{state.TriplecastCharges}/2");

            // Manafont
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Manafont, "Manafont:"));
            ImGui.TableNextColumn();
            DrawReadyStatus(state.ManafontReady);

            // Amplifier
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Amplifier, "Amplifier:"));
            ImGui.TableNextColumn();
            DrawReadyStatus(state.AmplifierReady);

            // Ley Lines
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.LeyLinesCd, "Ley Lines CD:"));
            ImGui.TableNextColumn();
            DrawReadyStatus(state.LeyLinesReady);

            ImGui.EndTable();
        }
    }

    private static void DrawTargetSection(HecateDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.TargetSection, "Target"));
        ImGui.Separator();

        if (ImGui.BeginTable("BlmTargetTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Thunder DoT
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ThunderDoT, "Thunder DoT:"));
            ImGui.TableNextColumn();
            if (state.HasThunderDoT)
            {
                var color = state.ThunderDoTRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.ThunderDoTRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.NotApplied, "Not applied"));
            }

            // Current Target
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TargetLabel, "Target:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.CurrentTarget);

            // Nearby Enemies
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.NearbyEnemies, "Nearby Enemies:"));
            ImGui.TableNextColumn();
            var aoeColor = state.NearbyEnemies >= 3 ? new Vector4(1f, 0.6f, 0.2f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(aoeColor, $"{state.NearbyEnemies}");

            ImGui.EndTable();
        }
    }

    private static void DrawProcStatus(bool hasProc)
    {
        if (hasProc)
        {
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Ready, "Ready"));
        }
        else
        {
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.No, "No"));
        }
    }

    private static void DrawReadyStatus(bool isReady)
    {
        if (isReady)
        {
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Ready, "Ready"));
        }
        else
        {
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.OnCd, "On CD"));
        }
    }
}
