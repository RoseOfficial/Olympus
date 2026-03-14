using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.NikeCore.Context;
using Olympus.Windows.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Samurai tab: Nike-specific debug info including Sen, Kenki, and Iaijutsu tracking.
/// </summary>
public static class NikeTab
{
    public static void Draw(NikeDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.SamuraiNotActive, "Samurai rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToSamurai, "Switch to Samurai to see debug info."));
            return;
        }

        // Gauge Section
        DrawGaugeSection(state);
        ImGui.Spacing();

        // Buffs Section
        DrawBuffSection(state);
        ImGui.Spacing();

        // Procs Section
        DrawProcSection(state);
        ImGui.Spacing();

        // DoT Section
        DrawDotSection(state);
        ImGui.Spacing();

        // Positional Section
        DrawPositionalSection(state);
    }

    private static void DrawGaugeSection(NikeDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Gauge, "Gauge"), "SamGaugeTable", () =>
        {
            // Sen
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Sen, "Sen:"));
            ImGui.TableNextColumn();
            var senColor = state.SenCount switch
            {
                3 => new Vector4(0.5f, 1f, 0.5f, 1f),
                2 => new Vector4(1f, 1f, 0.5f, 1f),
                1 => new Vector4(1f, 0.8f, 0.5f, 1f),
                _ => new Vector4(0.7f, 0.7f, 0.7f, 1f)
            };
            ImGui.TextColored(senColor, NikeDebugState.FormatSen(state.Sen));
            ImGui.SameLine();
            ImGui.TextDisabled($"({state.SenCount}/3)");

            // Kenki
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Kenki, "Kenki:"));
            ImGui.TableNextColumn();
            var kenkiPercent = state.Kenki / 100f;
            ImGui.ProgressBar(kenkiPercent, new Vector2(-1, 0), $"{state.Kenki}/100");

            // Meditation
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Meditation, "Meditation:"));
            ImGui.TableNextColumn();
            var medColor = state.Meditation >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(medColor, $"{state.Meditation}/3");

            // Combo
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Combo, "Combo:"));
            ImGui.TableNextColumn();
            var comboColor = state.ComboStep > 0 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(comboColor, state.ComboStep > 0 ? $"Step {state.ComboStep} ({state.ComboTimeRemaining:F1}s)" : Loc.T(LocalizedStrings.Debug.NoneLabel, "None"));
        }, 140f);
    }

    private static void DrawBuffSection(NikeDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs"), "SamBuffTable", () =>
        {
            // Fugetsu (damage up)
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Fugetsu, "Fugetsu:"));
            ImGui.TableNextColumn();
            if (state.HasFugetsu)
            {
                var color = state.FugetsuRemaining < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.FugetsuRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            // Fuka (speed up)
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Fuka, "Fuka:"));
            ImGui.TableNextColumn();
            if (state.HasFuka)
            {
                var color = state.FukaRemaining < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.FukaRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            // Meikyo Shisui
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.MeikyoShisui, "Meikyo Shisui:"));
            ImGui.TableNextColumn();
            if (state.HasMeikyoShisui)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), Loc.TFormat(LocalizedStrings.Debug.StacksFormat, "{0} stacks", state.MeikyoStacks));
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }
        }, 140f);
    }

    private static void DrawProcSection(NikeDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Procs, "Procs"), "SamProcTable", () =>
        {
            // Last Iaijutsu
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.LastIaijutsu, "Last Iaijutsu:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.LastIaijutsu.ToString());

            // Tsubame-gaeshi Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TsubameReady, "Tsubame Ready:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasTsubameGaeshiReady);

            // Ogi Namikiri Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.OgiNamikiriReady, "Ogi Namikiri Ready:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasOgiNamikiriReady);

            // Kaeshi Namikiri Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.KaeshiReady, "Kaeshi Ready:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasKaeshiNamikiriReady);

            // Zanshin Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ZanshinReady, "Zanshin Ready:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasZanshinReady);
        }, 140f);
    }

    private static void DrawDotSection(NikeDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.DoT, "DoT"), "SamDotTable", () =>
        {
            // Higanbana
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Higanbana, "Higanbana:"));
            ImGui.TableNextColumn();
            if (state.HasHiganbanaOnTarget)
            {
                var color = state.HiganbanaRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, Loc.TFormat(LocalizedStrings.Debug.RemainingFormat, "{0:F1}s remaining", state.HiganbanaRemaining));
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.NotApplied, "Not applied"));
            }
        }, 140f);
    }

    private static void DrawPositionalSection(NikeDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Positional, "Positional"), "SamPositionalTable", () =>
        {
            // Current Position
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Position, "Position:"));
            ImGui.TableNextColumn();
            if (state.TargetHasPositionalImmunity)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), Loc.T(LocalizedStrings.Debug.ImmuneOmni, "Immune (omni)"));
            }
            else if (state.IsAtRear)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Rear, "Rear"));
            }
            else if (state.IsAtFlank)
            {
                ImGui.TextColored(new Vector4(1f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Flank, "Flank"));
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.Front, "Front"));
            }

            // True North
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TrueNorth, "True North:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasTrueNorth);

            // Target
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TargetLabel, "Target:"));
            ImGui.TableNextColumn();
            ImGui.Text(string.IsNullOrEmpty(state.CurrentTarget) ? Loc.T(LocalizedStrings.Debug.NoneLabel, "None") : state.CurrentTarget);

            // Nearby Enemies
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.NearbyEnemies, "Nearby Enemies:"));
            ImGui.TableNextColumn();
            var aoeColor = state.NearbyEnemies >= 3 ? new Vector4(1f, 0.6f, 0.2f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(aoeColor, $"{state.NearbyEnemies}");
        }, 140f);
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
}
