using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Rotation.AstraeaCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Astrologian tab: Astraea-specific debug info including Cards, Earthly Star, and healing states.
/// </summary>
public static class AstrologianTab
{
    public static void Draw(AstraeaDebugState? astraeaState, Configuration config)
    {
        if (astraeaState == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), "Astrologian rotation not active.");
            ImGui.TextDisabled("Switch to Astrologian to see debug info.");
            return;
        }

        // Cards Section
        DrawCardsSection(astraeaState);
        ImGui.Spacing();

        // Earthly Star Section
        DrawEarthlyStarSection(astraeaState);
        ImGui.Spacing();

        // Healing Section
        DrawHealingSection(astraeaState);
        ImGui.Spacing();

        // DPS Section
        DrawDpsSection(astraeaState);
    }

    private static void DrawCardsSection(AstraeaDebugState state)
    {
        ImGui.Text("Cards");
        ImGui.Separator();

        if (ImGui.BeginTable("AstCardsTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Card State (shows cards in hand)
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Cards in Hand:");
            ImGui.TableNextColumn();
            var cardColor = state.CardState.Contains("cards") ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(cardColor, state.CardState);

            // Draw State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Draw State:");
            ImGui.TableNextColumn();
            ImGui.Text(state.DrawState);

            // Play State (what's happening with card plays)
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Play State:");
            ImGui.TableNextColumn();
            var playColor = state.PlayState.Contains("FAILED") ? new Vector4(1f, 0.5f, 0.5f, 1f)
                : state.PlayState.Contains("→") ? new Vector4(0.5f, 1f, 0.5f, 1f)
                : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(playColor, state.PlayState);

            // Current Card Type
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Current Card:");
            ImGui.TableNextColumn();
            ImGui.Text(state.CurrentCardType);

            // Minor Arcana
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Minor Arcana:");
            ImGui.TableNextColumn();
            ImGui.Text(state.MinorArcanaType);

            // Divination State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Divination:");
            ImGui.TableNextColumn();
            ImGui.Text(state.DivinationState);

            // Oracle State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Oracle:");
            ImGui.TableNextColumn();
            ImGui.Text(state.OracleState);

            ImGui.EndTable();
        }
    }

    private static void DrawEarthlyStarSection(AstraeaDebugState state)
    {
        ImGui.Text("Earthly Star");
        ImGui.Separator();

        if (ImGui.BeginTable("AstStarTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Star State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Star State:");
            ImGui.TableNextColumn();
            var starColor = state.IsStarMature ? new Vector4(0.5f, 1f, 0.5f, 1f)
                : state.EarthlyStarState != "Not Placed" ? new Vector4(1f, 1f, 0.5f, 1f)
                : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(starColor, state.EarthlyStarState);

            // Time Remaining
            if (state.StarTimeRemaining > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Time Left:");
                ImGui.TableNextColumn();
                ImGui.Text($"{state.StarTimeRemaining:F1}s");
            }

            // Targets in Range
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Targets in Range:");
            ImGui.TableNextColumn();
            ImGui.Text($"{state.StarTargetsInRange}");

            ImGui.EndTable();
        }
    }

    private static void DrawHealingSection(AstraeaDebugState state)
    {
        ImGui.Text("Healing");
        ImGui.Separator();

        if (ImGui.BeginTable("AstHealingTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Single Target Healing
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Single Heal:");
            ImGui.TableNextColumn();
            ImGui.Text(state.SingleHealState);

            // AoE Healing
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("AoE Heal:");
            ImGui.TableNextColumn();
            ImGui.Text($"{state.AoEHealState} ({state.AoEInjuredCount} injured)");

            // Essential Dignity
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Essential Dignity:");
            ImGui.TableNextColumn();
            ImGui.Text(state.EssentialDignityState);

            // Celestial Intersection
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Celestial Inter.:");
            ImGui.TableNextColumn();
            ImGui.Text(state.CelestialIntersectionState);

            // Celestial Opposition
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Celestial Opp.:");
            ImGui.TableNextColumn();
            ImGui.Text(state.CelestialOppositionState);

            // Exaltation
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Exaltation:");
            ImGui.TableNextColumn();
            ImGui.Text(state.ExaltationState);

            // Horoscope
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Horoscope:");
            ImGui.TableNextColumn();
            ImGui.Text(state.HoroscopeState);

            // Macrocosmos
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Macrocosmos:");
            ImGui.TableNextColumn();
            ImGui.Text(state.MacrocosmosState);

            // Neutral Sect
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Neutral Sect:");
            ImGui.TableNextColumn();
            ImGui.Text(state.NeutralSectState);

            // Synastry
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Synastry:");
            ImGui.TableNextColumn();
            ImGui.Text(state.SynastryState);
            if (!string.IsNullOrEmpty(state.SynastryTarget) && state.SynastryTarget != "None")
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"→ {state.SynastryTarget}");
            }

            // Last Heal
            if (state.LastHealAmount > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text("Last Heal:");
                ImGui.TableNextColumn();
                ImGui.Text($"{state.LastHealAmount:N0} HP");
                if (!string.IsNullOrEmpty(state.LastHealStats))
                {
                    ImGui.TextDisabled(state.LastHealStats);
                }
            }

            ImGui.EndTable();
        }
    }

    private static void DrawDpsSection(AstraeaDebugState state)
    {
        ImGui.Text("DPS");
        ImGui.Separator();

        if (ImGui.BeginTable("AstDpsTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Planned Action
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Planned Action:");
            ImGui.TableNextColumn();
            var actionColor = state.PlannedAction != "None" ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(actionColor, state.PlannedAction);

            // DPS State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("DPS State:");
            ImGui.TableNextColumn();
            ImGui.Text(state.DpsState);

            // Lightspeed
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text("Lightspeed:");
            ImGui.TableNextColumn();
            ImGui.Text(state.LightspeedState);

            ImGui.EndTable();
        }
    }
}
