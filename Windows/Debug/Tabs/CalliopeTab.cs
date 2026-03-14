using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.CalliopeCore.Context;
using Olympus.Windows.Debug;

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
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.BardNotActive, "Bard rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToBard, "Switch to Bard to see debug info."));
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
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Song, "Song"), "BrdSongTable", () =>
        {
            // Current Song
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.CurrentSong, "Current Song:"));
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
                ImGui.Text(Loc.T(LocalizedStrings.Debug.SongTimer, "Song Timer:"));
                ImGui.TableNextColumn();
                var timerColor = state.SongTimer < 5f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(timerColor, $"{state.SongTimer:F1}s");
            }

            // Repertoire
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Repertoire, "Repertoire:"));
            ImGui.TableNextColumn();
            var repColor = state.Repertoire >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(repColor, $"{state.Repertoire}/3");

            // Coda
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Coda, "Coda:"));
            ImGui.TableNextColumn();
            var codaColor = state.CodaCount >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.CodaCount >= 2 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(codaColor, $"{state.CodaCount}/3");
        }, 140f);
    }

    private static void DrawGaugeSection(CalliopeDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Gauge, "Gauge"), "BrdGaugeTable", () =>
        {
            // Soul Voice
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SoulVoice, "Soul Voice:"));
            ImGui.TableNextColumn();
            var soulVoicePercent = state.SoulVoice / 100f;
            ImGui.ProgressBar(soulVoicePercent, new Vector2(-1, 0), $"{state.SoulVoice}/100");

            // Bloodletter Charges
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Bloodletter, "Bloodletter:"));
            ImGui.TableNextColumn();
            var blColor = state.BloodletterCharges >= 3 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.BloodletterCharges >= 2 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(blColor, $"{state.BloodletterCharges}/3");
        }, 140f);
    }

    private static void DrawBuffSection(CalliopeDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs"), "BrdBuffTable", () =>
        {
            // Hawk's Eye (Straight Shot Ready)
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.HawksEye, "Hawk's Eye:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasHawksEye);

            // Raging Strikes
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.RagingStrikes, "Raging Strikes:"));
            ImGui.TableNextColumn();
            if (state.HasRagingStrikes)
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.2f, 1f), $"{state.RagingStrikesRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            // Battle Voice
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.BattleVoice, "Battle Voice:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasBattleVoice);

            // Barrage
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Barrage, "Barrage:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasBarrage);

            // Radiant Finale
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.RadiantFinale, "Radiant Finale:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasRadiantFinale);

            // Blast Arrow Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.BlastArrow, "Blast Arrow:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasBlastArrowReady);

            // Resonant Arrow Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ResonantArrow, "Resonant Arrow:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasResonantArrowReady);

            // Radiant Encore Ready
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.RadiantEncore, "Radiant Encore:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasRadiantEncoreReady);
        }, 140f);
    }

    private static void DrawDotSection(CalliopeDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.DoTs, "DoTs"), "BrdDotTable", () =>
        {
            // Caustic Bite
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.CausticBite, "Caustic Bite:"));
            ImGui.TableNextColumn();
            if (state.HasCausticBite)
            {
                var color = state.CausticBiteRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.CausticBiteRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.NotApplied, "Not applied"));
            }

            // Stormbite
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Stormbite, "Stormbite:"));
            ImGui.TableNextColumn();
            if (state.HasStormbite)
            {
                var color = state.StormbiteRemaining < 6f ? new Vector4(1f, 0.5f, 0.5f, 1f) : new Vector4(0.5f, 1f, 0.5f, 1f);
                ImGui.TextColored(color, $"{state.StormbiteRemaining:F1}s");
            }
            else
            {
                ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.NotApplied, "Not applied"));
            }
        }, 140f);
    }

    private static void DrawTargetSection(CalliopeDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.TargetLabel, "Target"), "BrdTargetTable", () =>
        {
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
        }, 140f);
    }

    private static void DrawProcStatus(bool hasProc)
    {
        if (hasProc)
        {
            ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.JobActiveLabel, "Active"));
        }
        else
        {
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.No, "No"));
        }
    }
}
