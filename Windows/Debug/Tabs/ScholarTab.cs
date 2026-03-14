using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.AthenaCore.Context;
using Olympus.Services.Debug;
using Olympus.Windows.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Scholar tab: Athena-specific debug info including Aetherflow, Fairy, Shields.
/// </summary>
public static class ScholarTab
{
    public static void Draw(AthenaDebugState? athenaState, Configuration config)
    {
        if (athenaState == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.ScholarNotActive, "Scholar rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToScholar, "Switch to Scholar to see debug info."));
            return;
        }

        // Resources Section
        DrawResourcesSection(athenaState);
        ImGui.Spacing();

        // Fairy Section
        DrawFairySection(athenaState);
        ImGui.Spacing();

        // Healing Section
        DrawHealingSection(athenaState);
        ImGui.Spacing();

        // DPS Section
        DrawDpsSection(athenaState);
    }

    private static void DrawResourcesSection(AthenaDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Resources, "Resources"), "SchResourcesTable", () =>
        {
            // Aetherflow Stacks
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Aetherflow, "Aetherflow:"));
            ImGui.TableNextColumn();
            var stackColor = state.AetherflowStacks switch
            {
                0 => new Vector4(1f, 0.5f, 0.5f, 1f),  // Red - empty
                1 => new Vector4(1f, 1f, 0.5f, 1f),    // Yellow - low
                2 => new Vector4(0.7f, 1f, 0.7f, 1f),  // Light green
                3 => new Vector4(0.5f, 1f, 0.5f, 1f),  // Green - full
                _ => new Vector4(1f, 1f, 1f, 1f)
            };
            ImGui.TextColored(stackColor, $"{state.AetherflowStacks}/3");
            ImGui.SameLine();
            ImGui.TextDisabled($"({state.AetherflowState})");

            // Fairy Gauge
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.FairyGauge, "Fairy Gauge:"));
            ImGui.TableNextColumn();
            var gaugePercent = state.FairyGauge / 100f;
            ImGui.ProgressBar(gaugePercent, new Vector2(-1, 0), $"{state.FairyGauge}/100");

            // Player HP
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PlayerHpLabel, "Player HP:"));
            ImGui.TableNextColumn();
            var hpColor = state.PlayerHpPercent < 0.5f ? new Vector4(1f, 0.5f, 0.5f, 1f)
                : state.PlayerHpPercent < 0.8f ? new Vector4(1f, 1f, 0.5f, 1f)
                : new Vector4(0.5f, 1f, 0.5f, 1f);
            ImGui.TextColored(hpColor, $"{state.PlayerHpPercent:P0}");

            // Party Info
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PartyLabel, "Party:"));
            ImGui.TableNextColumn();
            ImGui.Text($"{state.PartyValidCount}/{state.PartyListCount} {Loc.T(LocalizedStrings.Debug.ValidLabel, "valid")}");
        }, 140f);
    }

    private static void DrawFairySection(AthenaDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Fairy, "Fairy"), "SchFairyTable", () =>
        {
            // Fairy State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.FairyState, "Fairy State:"));
            ImGui.TableNextColumn();
            var fairyColor = state.FairyState switch
            {
                "Eos" => new Vector4(0.5f, 1f, 0.5f, 1f),        // Green - active
                "Seraph" => new Vector4(1f, 0.8f, 0.5f, 1f),     // Orange - transformed
                "Seraphism" => new Vector4(1f, 0.6f, 0.8f, 1f),  // Pink - lv100 mode
                "Dissipated" => new Vector4(0.7f, 0.7f, 0.7f, 1f), // Gray - dismissed
                _ => new Vector4(1f, 0.5f, 0.5f, 1f)             // Red - none
            };
            ImGui.TextColored(fairyColor, state.FairyState);

            // Fey Union State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.FeyUnion, "Fey Union:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.FeyUnionState);

            // Seraph State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Seraph, "Seraph:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.SeraphState);

            // Dissipation State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Dissipation, "Dissipation:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.DissipationState);
        }, 140f);
    }

    private static void DrawHealingSection(AthenaDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Healing, "Healing"), "SchHealingTable", () =>
        {
            // Single Target Healing
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SingleHealLabel, "Single Heal:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.SingleHealState);

            // AoE Healing
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.AoEHealLabel, "AoE Heal:"));
            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.InjuredFormat, "{0} ({1} injured)", state.AoEHealState, state.AoEInjuredCount));

            // Lustrate
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Lustrate, "Lustrate:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.LustrateState);

            // Indomitability
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Indomitability, "Indomitability:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.IndomitabilityState);

            // Excogitation
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Excogitation, "Excogitation:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.ExcogitationState);

            // Sacred Soil
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SacredSoil, "Sacred Soil:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.SacredSoilState);

            // Shields
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Shields, "Shields:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.ShieldState);

            // Emergency Tactics
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.EmergencyTactics, "Emergency Tactics:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.EmergencyTacticsState);

            // Deployment
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Deployment, "Deployment:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.DeploymentState);

            // Recitation
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Recitation, "Recitation:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.RecitationState);

            // Last Heal
            if (state.LastHealAmount > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(Loc.T(LocalizedStrings.Debug.LastHeal, "Last Heal:"));
                ImGui.TableNextColumn();
                ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.HpFormat, "{0:N0} HP", state.LastHealAmount));
                if (!string.IsNullOrEmpty(state.LastHealStats))
                {
                    ImGui.TextDisabled(state.LastHealStats);
                }
            }
        }, 140f);
    }

    private static void DrawDpsSection(AthenaDebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.DpsSection, "DPS"), "SchDpsTable", () =>
        {
            // Planned Action
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.PlannedActionLabel, "Planned Action:"));
            ImGui.TableNextColumn();
            var actionColor = state.PlannedAction != "None" ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(actionColor, state.PlannedAction);

            // DPS State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DpsStateLabel, "DPS State:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.DpsState);

            // DoT State
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DoT, "DoT:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.DotState);

            // AoE DPS
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.AoEDpsLabel, "AoE DPS:"));
            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.EnemiesFormat, "{0} ({1} enemies)", state.AoEDpsState, state.AoEDpsEnemyCount));

            // Chain Stratagem
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ChainStratagem, "Chain Stratagem:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.ChainStratagemState);

            // Energy Drain
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.EnergyDrain, "Energy Drain:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.EnergyDrainState);

            // Lucid Dreaming
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.LucidDreaming, "Lucid Dreaming:"));
            ImGui.TableNextColumn();
            ImGui.Text(state.LucidState);
        }, 140f);

        // Raise/Esuna at bottom
        ImGui.Spacing();
        if (state.RaiseState != "Idle" || state.RaiseTarget != "None")
        {
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.5f, 1f), $"{Loc.T(LocalizedStrings.Debug.RaiseLabel, "Raise:")} {state.RaiseState}");
            if (!string.IsNullOrEmpty(state.RaiseTarget) && state.RaiseTarget != "None")
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"→ {state.RaiseTarget}");
            }
        }

        if (state.EsunaState != "Idle" || state.EsunaTarget != "None")
        {
            ImGui.TextColored(new Vector4(0.5f, 1f, 1f, 1f), $"{Loc.T(LocalizedStrings.Debug.EsunaLabel, "Esuna:")} {state.EsunaState}");
            if (!string.IsNullOrEmpty(state.EsunaTarget) && state.EsunaTarget != "None")
            {
                ImGui.SameLine();
                ImGui.TextDisabled($"→ {state.EsunaTarget}");
            }
        }
    }
}
