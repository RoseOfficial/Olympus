using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.Common;
using Olympus.Windows.Debug;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// White Mage tab: Apollo-specific debug info including Lily resources, healing state, and buffs.
/// </summary>
public static class ApolloTab
{
    public static void Draw(DebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.WhiteMageNotActive, "White Mage rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToWhiteMage, "Switch to White Mage to see debug info."));
            return;
        }

        DrawOverviewSection(state);
        ImGui.Spacing();

        DrawResourcesSection(state);
        ImGui.Spacing();

        DrawHealingSection(state);
        ImGui.Spacing();

        DrawBuffSection(state);
        ImGui.Spacing();

        DrawDpsSection(state);
    }

    private static void DrawOverviewSection(DebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.TankStatus, "Status"), "ApolloOverviewTable", () =>
        {
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

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.WhmPlanningState, "Planning State:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(state.PlanningState);
        });

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

    private static void DrawResourcesSection(DebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Resources, "Resources"), "ApolloResourcesTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.WhmLilies, "Lilies:"));
            ImGui.TableNextColumn();
            var lilyColor = state.LilyCount switch
            {
                0 => new Vector4(1f, 0.5f, 0.5f, 1f),
                1 or 2 => new Vector4(1f, 1f, 0.5f, 1f),
                _ => new Vector4(0.5f, 1f, 0.5f, 1f)
            };
            ImGui.TextColored(lilyColor, $"{state.LilyCount}/3");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.WhmBloodLily, "Blood Lily:"));
            ImGui.TableNextColumn();
            var bloodColor = state.BloodLilyCount switch
            {
                0 => new Vector4(0.7f, 0.7f, 0.7f, 1f),
                1 or 2 => new Vector4(1f, 0.7f, 0.7f, 1f),
                _ => new Vector4(1f, 0.3f, 0.3f, 1f)
            };
            ImGui.TextColored(bloodColor, $"{state.BloodLilyCount}/3");

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.WhmLilyStrategy, "Lily Strategy:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(state.LilyStrategy);

            if (state.SacredSightStacks > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(Loc.T(LocalizedStrings.Debug.WhmSacredSight, "Sacred Sight:"));
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(1f, 0.8f, 0.3f, 1f), $"{state.SacredSightStacks}");
            }
        });
    }

    private static void DrawHealingSection(DebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Healing, "Healing"), "ApolloHealingTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.AoEHealLabel, "AoE Heal:"));
            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.InjuredFormat, "{0} ({1} injured)", state.AoEStatus, state.AoEInjuredCount));

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
        });
    }

    private static void DrawBuffSection(DebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs / Cooldowns"), "ApolloBuffTable", () =>
        {
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

            DrawStateRow(Loc.T(LocalizedStrings.Debug.WhmTemperance, "Temperance:"), state.TemperanceState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.WhmAssizes, "Assizes:"), state.AssizeState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.WhmAsylum, "Asylum:"), state.AsylumState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.WhmPoM, "Presence of Mind:"), state.PoMState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.WhmThinAir, "Thin Air:"), state.ThinAirState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.LucidDreaming, "Lucid Dreaming:"), state.LucidState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.WhmSurecast, "Surecast:"), state.SurecastState);
            DrawStateRow(Loc.T(LocalizedStrings.Debug.WhmDefensive, "Defensive:"), state.DefensiveState);
        });
    }

    private static void DrawDpsSection(DebugState state)
    {
        DebugTabHelpers.DrawSection(Loc.T(LocalizedStrings.Debug.DpsSection, "DPS"), "ApolloDpsTable", () =>
        {
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.DpsStateLabel, "DPS State:"));
            ImGui.TableNextColumn();
            ImGui.TextDisabled(state.DpsState);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.AoEDpsLabel, "AoE DPS:"));
            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.EnemiesFormat, "{0} ({1} enemies)", state.AoEDpsState, state.AoEDpsEnemyCount));

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.WhmMisery, "Misery:"));
            ImGui.TableNextColumn();
            if (state.MiseryState != "Idle")
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), state.MiseryState);
            else
                ImGui.TextDisabled(state.MiseryState);
        });
    }
}
