using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Rotation.CalliopeCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Bard tab: Calliope-specific debug info including songs, Soul Voice, and DoT tracking.
/// </summary>
public static class CalliopeTab
{
    public static void Draw(CalliopeDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Bard rotation not active.");
            ImGui.TextDisabled("Switch to Bard to see debug info.");
            return;
        }

        // Song Section
        DrawSongSection(state);
        ImGui.Spacing();

        // Gauge Section
        DrawGaugeSection(state);
        ImGui.Spacing();

        // Buffs Section
        DrawBuffSection(state);
        ImGui.Spacing();

        // DoTs Section
        DrawDotSection(state);
        ImGui.Spacing();

        // Target Section
        DrawTargetSection(state);
    }

    private static void DrawSongSection(CalliopeDebugState state)
    {
        ImGui.Text("Song");
        ImGui.Separator();

        if (ImGui.BeginTable("BrdSongTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Current Song
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Current Song:");
            ImGui.TableNextColumn();
            var songColor = state.CurrentSong switch
            {
                "Wanderer's Minuet" => new Vector4(0.5f, 0.8f, 1f, 1f),
                "Mage's Ballad" => new Vector4(0.8f, 0.5f, 1f, 1f),
                "Army's Paeon" => new Vector4(1f, 0.8f, 0.5f, 1f),
                _ => new Vector4(0.7f, 0.7f, 0.7f, 1f)
            };
            ImGui.TextColored(songColor, state.CurrentSong);

            // Song Timer
            if (state.SongTimer > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Song Timer:");
                ImGui.TableNextColumn();
                var timerColor = state.SongTimer < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(timerColor, $"{state.SongTimer:F1}s");
            }

            // Repertoire
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Repertoire:");
            ImGui.TableNextColumn();
            var repColor = state.Repertoire >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(repColor, $"{state.Repertoire}/3");

            // Coda
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Coda:");
            ImGui.TableNextColumn();
            var codaColor = state.CodaCount >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.CodaCount >= 2 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(codaColor, $"{state.CodaCount}/3");

            ImGui.EndTable();
        }
    }

    private static void DrawGaugeSection(CalliopeDebugState state)
    {
        ImGui.Text("Gauge");
        ImGui.Separator();

        if (ImGui.BeginTable("BrdGaugeTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Soul Voice
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Soul Voice:");
            ImGui.TableNextColumn();
            var soulVoicePercent = state.SoulVoice / 100f;
            ImGui.ProgressBar(soulVoicePercent, new Vector2(-1, 0), $"{state.SoulVoice}/100");

            // Bloodletter Charges
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Bloodletter:");
            ImGui.TableNextColumn();
            var blColor = state.BloodletterCharges >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.BloodletterCharges >= 2 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(blColor, $"{state.BloodletterCharges}/3");

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(CalliopeDebugState state)
    {
        ImGui.Text("Buffs");
        ImGui.Separator();

        if (ImGui.BeginTable("BrdBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Hawk's Eye (Straight Shot Ready)
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Hawk's Eye:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasHawksEye);

            // Raging Strikes
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Raging Strikes:");
            ImGui.TableNextColumn();
            if (state.HasRagingStrikes)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.RagingStrikesRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled("Inactive");
            }

            // Battle Voice
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Battle Voice:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasBattleVoice);

            // Barrage
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Barrage:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasBarrage);

            // Radiant Finale
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Radiant Finale:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasRadiantFinale);

            // Blast Arrow Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Blast Arrow:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasBlastArrowReady);

            // Resonant Arrow Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Resonant Arrow:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasResonantArrowReady);

            // Radiant Encore Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Radiant Encore:");
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasRadiantEncoreReady);

            ImGui.EndTable();
        }
    }

    private static void DrawDotSection(CalliopeDebugState state)
    {
        ImGui.Text("DoTs");
        ImGui.Separator();

        if (ImGui.BeginTable("BrdDotTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Caustic Bite
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Caustic Bite:");
            ImGui.TableNextColumn();
            if (state.HasCausticBite)
            {
                var color = state.CausticBiteRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.CausticBiteRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Not applied");
            }

            // Stormbite
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Stormbite:");
            ImGui.TableNextColumn();
            if (state.HasStormbite)
            {
                var color = state.StormbiteRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.StormbiteRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Not applied");
            }

            ImGui.EndTable();
        }
    }

    private static void DrawTargetSection(CalliopeDebugState state)
    {
        ImGui.Text("Target");
        ImGui.Separator();

        if (ImGui.BeginTable("BrdTargetTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Current Target
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Target:");
            ImGui.TableNextColumn();
            ImGui.Text(state.CurrentTarget);

            // Nearby Enemies
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Nearby Enemies:");
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
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), "Active");
        }
        else
        {
            ImGui.TextDisabled("No");
        }
    }
}
