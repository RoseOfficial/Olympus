using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
using Olympus.Rotation.PersephoneCore.Context;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Summoner tab: Persephone-specific debug info including demi-summons, primal attunement, and Aetherflow.
/// </summary>
public static class PersephoneTab
{
    public static void Draw(PersephoneDebugState? state, Configuration config)
    {
        if (state == null)
        {
            ImGui.TextColored(new Vector4(1f, 0.5f, 0.5f, 1f), Loc.T(LocalizedStrings.Debug.SummonerNotActive, "Summoner rotation not active."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.SwitchToSummoner, "Switch to Summoner to see debug info."));
            return;
        }

        // Demi-Summon Section
        DrawDemiSummonSection(state);
        ImGui.Spacing();

        // Primal Section
        DrawPrimalSection(state);
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

    private static void DrawDemiSummonSection(PersephoneDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.DemiSummon, "Demi-Summon"));
        ImGui.Separator();

        if (ImGui.BeginTable("SmnDemiTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Phase
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SummonerPhase, "Phase:"));
            ImGui.TableNextColumn();
            ImGui.TextColored(new Vector4(0.8f, 0.8f, 1f, 1f), state.Phase);

            // Active Demi
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.ActiveDemi, "Active Demi:"));
            ImGui.TableNextColumn();
            if (state.IsBahamutActive)
            {
                ImGui.TextColored(new Vector4(0.5f, 0.8f, 1f, 1f), $"Bahamut ({state.DemiSummonTimer:F1}s)");
            }
            else if (state.IsPhoenixActive)
            {
                ImGui.TextColored(new Vector4(1f, 0.6f, 0.2f, 1f), $"Phoenix ({state.DemiSummonTimer:F1}s)");
            }
            else if (state.IsSolarBahamutActive)
            {
                ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), $"Solar Bahamut ({state.DemiSummonTimer:F1}s)");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.NoneLabel, "None"));
            }

            // GCDs Remaining
            if (state.DemiSummonGcdsRemaining > 0)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(Loc.T(LocalizedStrings.Debug.GcdsRemaining, "GCDs Remaining:"));
                ImGui.TableNextColumn();
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), $"{state.DemiSummonGcdsRemaining}");
            }

            // Tracking
            if (state.IsBahamutActive || state.IsPhoenixActive || state.IsSolarBahamutActive)
            {
                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(Loc.T(LocalizedStrings.Debug.EnkindleUsed, "Enkindle Used:"));
                ImGui.TableNextColumn();
                DrawProcStatus(state.HasUsedEnkindleThisPhase);

                ImGui.TableNextRow();
                ImGui.TableNextColumn();
                ImGui.Text(Loc.T(LocalizedStrings.Debug.AstralFlowUsed, "Astral Flow Used:"));
                ImGui.TableNextColumn();
                DrawProcStatus(state.HasUsedAstralFlowThisPhase);
            }

            ImGui.EndTable();
        }
    }

    private static void DrawPrimalSection(PersephoneDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.PrimalAttunement, "Primal Attunement"));
        ImGui.Separator();

        if (ImGui.BeginTable("SmnPrimalTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Current Attunement
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Attunement, "Attunement:"));
            ImGui.TableNextColumn();
            var attunementColor = state.CurrentAttunement switch
            {
                1 => new Vector4(1f, 0.5f, 0.2f, 1f),  // Ifrit - fire
                2 => new Vector4(0.8f, 0.6f, 0.2f, 1f), // Titan - earth
                3 => new Vector4(0.5f, 1f, 0.5f, 1f),  // Garuda - wind
                _ => new Vector4(0.7f, 0.7f, 0.7f, 1f)
            };
            if (state.CurrentAttunement > 0)
            {
                ImGui.TextColored(attunementColor, $"{state.AttunementName} ({state.AttunementStacks} stacks, {state.AttunementTimer:F1}s)");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.NoneLabel, "None"));
            }

            // Available Primals
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Available, "Available:"));
            ImGui.TableNextColumn();
            var primals = new System.Collections.Generic.List<string>();
            if (state.CanSummonIfrit) primals.Add("Ifrit");
            if (state.CanSummonTitan) primals.Add("Titan");
            if (state.CanSummonGaruda) primals.Add("Garuda");
            if (primals.Count > 0)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), string.Join(", ", primals));
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.NoneLabel, "None"));
            }

            ImGui.EndTable();
        }
    }

    private static void DrawBuffSection(PersephoneDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Buffs, "Buffs"));
        ImGui.Separator();

        if (ImGui.BeginTable("SmnBuffTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // Searing Light
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SearingLight, "Searing Light:"));
            ImGui.TableNextColumn();
            if (state.HasSearingLight)
            {
                ImGui.TextColored(new Vector4(1f, 0.8f, 0.2f, 1f), $"{state.SearingLightRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.JobInactiveLabel, "Inactive"));
            }

            // Further Ruin
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.FurtherRuin, "Further Ruin:"));
            ImGui.TableNextColumn();
            if (state.HasFurtherRuin)
            {
                ImGui.TextColored(new Vector4(0.5f, 1f, 0.5f, 1f), $"{state.FurtherRuinRemaining:F1}s");
            }
            else
            {
                ImGui.TextDisabled(Loc.T(LocalizedStrings.Debug.No, "No"));
            }

            // Primal Favors
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.IfritsFavor, "Ifrit's Favor:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasIfritsFavor);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.TitansFavor, "Titan's Favor:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasTitansFavor);

            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.GarudasFavor, "Garuda's Favor:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasGarudasFavor);

            // Swiftcast
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Swiftcast, "Swiftcast:"));
            ImGui.TableNextColumn();
            DrawProcStatus(state.HasSwiftcast);

            ImGui.EndTable();
        }
    }

    private static void DrawCooldownSection(PersephoneDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.Cooldowns, "Cooldowns"));
        ImGui.Separator();

        if (ImGui.BeginTable("SmnCooldownTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

            // MP
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SummonerMp, "MP:"));
            ImGui.TableNextColumn();
            var mpPercent = state.MaxMp > 0 ? (float)state.CurrentMp / state.MaxMp : 0;
            ImGui.ProgressBar(mpPercent, new Vector2(-1, 0), $"{state.CurrentMp:N0}/{state.MaxMp:N0}");

            // Aetherflow
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SummonerAetherflow, "Aetherflow:"));
            ImGui.TableNextColumn();
            var aetherColor = state.AetherflowStacks >= 2 ? new Vector4(0.5f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(aetherColor, $"{state.AetherflowStacks}/2");

            // Searing Light
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SearingLightCd, "Searing Light CD:"));
            ImGui.TableNextColumn();
            DrawReadyStatus(state.SearingLightReady);

            // Energy Drain
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.SummonerEnergyDrain, "Energy Drain:"));
            ImGui.TableNextColumn();
            DrawReadyStatus(state.EnergyDrainReady);

            // Enkindle
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.Enkindle, "Enkindle:"));
            ImGui.TableNextColumn();
            DrawReadyStatus(state.EnkindleReady);

            // Astral Flow
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.AstralFlow, "Astral Flow:"));
            ImGui.TableNextColumn();
            DrawReadyStatus(state.AstralFlowReady);

            // Radiant Aegis
            ImGui.TableNextRow();
            ImGui.TableNextColumn();
            ImGui.Text(Loc.T(LocalizedStrings.Debug.RadiantAegis, "Radiant Aegis:"));
            ImGui.TableNextColumn();
            var aegisColor = state.RadiantAegisCharges >= 2 ? new Vector4(0.5f, 1f, 0.5f, 1f) : state.RadiantAegisCharges >= 1 ? new Vector4(1f, 1f, 0.5f, 1f) : new Vector4(0.7f, 0.7f, 0.7f, 1f);
            ImGui.TextColored(aegisColor, $"{state.RadiantAegisCharges}/2");

            ImGui.EndTable();
        }
    }

    private static void DrawTargetSection(PersephoneDebugState state)
    {
        ImGui.Text(Loc.T(LocalizedStrings.Debug.TargetLabel, "Target"));
        ImGui.Separator();

        if (ImGui.BeginTable("SmnTargetTable", 2, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Label", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Value", ImGuiTableColumnFlags.WidthStretch);

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
