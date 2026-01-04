using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Services.Debug;
using Olympus.Services.Healing;

namespace Olympus.Windows.Debug.Tabs;

/// <summary>
/// Healing tab: HP prediction, AoE healing, recent heals, shadow HP.
/// </summary>
public static class HealingTab
{
    public static void Draw(DebugSnapshot snapshot, Configuration config, DebugService debugService)
    {
        // Spell Status Section (comprehensive spell list with ready/cooldown status)
        if (IsSectionVisible(config, "SpellStatus"))
        {
            DrawSpellStatus(debugService);
            ImGui.Spacing();
        }

        // Spell Selection Section (shows last healing decision)
        if (IsSectionVisible(config, "SpellSelection"))
        {
            DrawSpellSelection(debugService);
            ImGui.Spacing();
        }

        // HP Prediction Section
        if (IsSectionVisible(config, "HpPrediction"))
        {
            DrawHpPrediction(snapshot, debugService);
            ImGui.Spacing();
        }

        // AoE Healing Section
        if (IsSectionVisible(config, "AoEHealing"))
        {
            DrawAoEHealing(snapshot, debugService);
            ImGui.Spacing();
        }

        // Recent Heals Section
        if (IsSectionVisible(config, "RecentHeals"))
        {
            DrawRecentHeals(snapshot);
            ImGui.Spacing();
        }

        // Shadow HP Section (collapsible)
        if (IsSectionVisible(config, "ShadowHp"))
        {
            DrawShadowHpTracking(snapshot);
        }
    }

    private static void DrawSpellStatus(DebugService debugService)
    {
        var playerLevel = debugService.GetPlayerLevel();
        if (playerLevel == 0)
        {
            ImGui.TextColored(DebugColors.Dim, "Not logged in");
            return;
        }

        var snapshot = debugService.GetSpellStatus(playerLevel);

        ImGui.Text("Spell Status");
        ImGui.SameLine();
        ImGui.TextColored(DebugColors.Dim, $"(Lv{snapshot.PlayerLevel})");
        ImGui.Separator();

        // Group spells by category
        var groups = snapshot.Spells
            .GroupBy(s => s.Category)
            .OrderBy(g => (int)g.Key);

        foreach (var group in groups)
        {
            var categoryName = GetCategoryDisplayName(group.Key);
            var readyCount = group.Count(s => s.IsReady);
            var totalCount = group.Count();

            if (ImGui.CollapsingHeader($"{categoryName} ({readyCount}/{totalCount})##{group.Key}"))
            {
                DrawSpellGroup(group.ToList());
            }
        }
    }

    private static void DrawSpellGroup(List<SpellStatusEntry> spells)
    {
        if (ImGui.BeginTable("SpellStatusTable", 4,
            ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Spell", ImGuiTableColumnFlags.WidthFixed, 140);
            ImGui.TableSetupColumn("Lv", ImGuiTableColumnFlags.WidthFixed, 30);
            ImGui.TableSetupColumn("Ready", ImGuiTableColumnFlags.WidthFixed, 50);
            ImGui.TableSetupColumn("Cooldown", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableHeadersRow();

            foreach (var spell in spells)
            {
                ImGui.TableNextRow();

                // Spell name
                ImGui.TableNextColumn();
                var nameColor = spell.IsLevelSynced
                    ? (spell.IsReady ? DebugColors.Success : DebugColors.Dim)
                    : DebugColors.Failure;
                ImGui.TextColored(nameColor, spell.Name);

                // Level
                ImGui.TableNextColumn();
                var lvColor = spell.IsLevelSynced ? DebugColors.Dim : DebugColors.Failure;
                ImGui.TextColored(lvColor, spell.MinLevel.ToString());

                // Ready status
                ImGui.TableNextColumn();
                if (spell.IsReady)
                {
                    ImGui.TextColored(DebugColors.Success, "OK");
                }
                else if (!spell.IsLevelSynced)
                {
                    ImGui.TextColored(DebugColors.Failure, "Sync");
                }
                else
                {
                    ImGui.TextColored(DebugColors.Warning, "CD");
                }

                // Cooldown
                ImGui.TableNextColumn();
                if (!spell.IsGCD && spell.CooldownRemaining > 0)
                {
                    ImGui.TextColored(DebugColors.Warning, $"{spell.CooldownRemaining:F1}s");
                }
                else if (!spell.IsReady && spell.NotReadyReason != null)
                {
                    ImGui.TextColored(DebugColors.Dim, spell.NotReadyReason);
                }
                else
                {
                    ImGui.TextColored(DebugColors.Dim, "-");
                }
            }

            ImGui.EndTable();
        }
    }

    private static string GetCategoryDisplayName(SpellCategory category) => category switch
    {
        SpellCategory.GcdHealSingle => "GCD Heals (Single)",
        SpellCategory.GcdHealAoE => "GCD Heals (AoE)",
        SpellCategory.GcdHealHoT => "GCD Heals (HoT)",
        SpellCategory.OgcdHealSingle => "oGCD Heals (Single)",
        SpellCategory.OgcdHealAoE => "oGCD Heals (AoE)",
        SpellCategory.GcdDamageSingle => "GCD Damage (Single)",
        SpellCategory.GcdDamageAoE => "GCD Damage (AoE)",
        SpellCategory.GcdDoT => "GCD DoT",
        SpellCategory.Utility => "Utility",
        _ => category.ToString()
    };

    private static void DrawSpellSelection(DebugService debugService)
    {
        var selection = debugService.GetLastSpellSelection();

        ImGui.Text("Spell Selection");
        ImGui.Separator();

        if (selection == null)
        {
            ImGui.TextColored(DebugColors.Dim, "No selection yet");
            return;
        }

        // Show age of selection
        var ageColor = selection.SecondsAgo < 1f ? DebugColors.Success : DebugColors.Dim;
        ImGui.TextColored(ageColor, $"[{selection.SecondsAgo:F1}s ago] {selection.SelectionType} Target");

        // Context info
        if (ImGui.BeginTable("SelectionContext", 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text($"Target: {selection.TargetName}");

            ImGui.TableNextColumn();
            ImGui.Text($"Missing: {selection.MissingHp:N0} HP");

            ImGui.TableNextColumn();
            var weaveColor = selection.IsWeaveWindow ? DebugColors.Success : DebugColors.Dim;
            ImGui.TextColored(weaveColor, $"Weave: {(selection.IsWeaveWindow ? "Yes" : "No")}");

            ImGui.TableNextColumn();
            ImGui.Text($"Lilies: {selection.LilyCount}/3");

            ImGui.EndTable();
        }

        // Selected spell
        if (selection.SelectedSpell != null)
        {
            ImGui.TextColored(DebugColors.Success, $"✓ Selected: {selection.SelectedSpell}");
            ImGui.TextColored(DebugColors.Dim, $"  Reason: {selection.SelectionReason}");
        }
        else
        {
            ImGui.TextColored(DebugColors.Warning, $"✗ No spell selected");
            ImGui.TextColored(DebugColors.Dim, $"  Reason: {selection.SelectionReason}");
        }

        // Candidates table (collapsible)
        if (selection.Candidates.Count > 0 && ImGui.CollapsingHeader($"Candidates ({selection.Candidates.Count})##SpellCandidates"))
        {
            if (ImGui.BeginTable("CandidatesTable", 5, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn("Spell", ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn("Heal", ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn("Eff", ImGuiTableColumnFlags.WidthFixed, 45);
                ImGui.TableSetupColumn("Score", ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn("Status", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableHeadersRow();

                foreach (var candidate in selection.Candidates)
                {
                    ImGui.TableNextRow();

                    // Spell name
                    ImGui.TableNextColumn();
                    var nameColor = candidate.WasSelected ? DebugColors.Success :
                                    candidate.RejectionReason != null ? DebugColors.Failure : DebugColors.Dim;
                    ImGui.TextColored(nameColor, candidate.SpellName);

                    // Heal amount
                    ImGui.TableNextColumn();
                    if (candidate.HealAmount > 0)
                        ImGui.Text($"{candidate.HealAmount:N0}");
                    else
                        ImGui.TextColored(DebugColors.Dim, "-");

                    // Efficiency
                    ImGui.TableNextColumn();
                    if (candidate.Efficiency > 0)
                    {
                        var effColor = candidate.Efficiency >= 0.7f ? DebugColors.Success :
                                       candidate.Efficiency >= 0.3f ? DebugColors.Warning : DebugColors.Failure;
                        ImGui.TextColored(effColor, $"{candidate.Efficiency:P0}");
                    }
                    else
                        ImGui.TextColored(DebugColors.Dim, "-");

                    // Score
                    ImGui.TableNextColumn();
                    if (candidate.Score > 0)
                        ImGui.Text($"{candidate.Score:F2}");
                    else
                        ImGui.TextColored(DebugColors.Dim, "-");

                    // Status (bonuses or rejection reason)
                    ImGui.TableNextColumn();
                    if (candidate.WasSelected)
                    {
                        ImGui.TextColored(DebugColors.Success, $"✓ {candidate.Bonuses}");
                    }
                    else if (candidate.RejectionReason != null)
                    {
                        ImGui.TextColored(DebugColors.Failure, candidate.RejectionReason);
                    }
                    else
                    {
                        ImGui.TextColored(DebugColors.Dim, candidate.Bonuses);
                    }
                }

                ImGui.EndTable();
            }
        }
    }

    private static void DrawHpPrediction(DebugSnapshot snapshot, DebugService debugService)
    {
        ImGui.Text("HP Prediction");
        ImGui.Separator();

        // Player stats for heal calculation
        var statsInfo = debugService.GetPlayerStatsDebugInfo();
        ImGui.TextColored(DebugColors.Dim, $"Stats: {statsInfo}");

        var gcd = snapshot.GcdState;
        var healing = snapshot.Healing;

        if (ImGui.BeginTable("HpPredictionTable", 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Col1", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Col2", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Col3", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Col4", ImGuiTableColumnFlags.WidthStretch);

            // Row 1: GCD state, remaining, anim lock, casting
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var gcdColor = DebugColors.GetGcdStateColor(gcd.State);
            ImGui.TextColored(gcdColor, $"GCD: {gcd.State}");

            ImGui.TableNextColumn();
            ImGui.Text($"Rem: {gcd.GcdRemaining:F2}s");

            ImGui.TableNextColumn();
            ImGui.Text($"Anim: {gcd.AnimationLockRemaining:F2}s");

            ImGui.TableNextColumn();
            ImGui.Text($"Casting: {(gcd.IsCasting ? "Yes" : "No")}");

            // Row 2: Weave, slots, last action
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var weaveColor = gcd.CanExecuteOgcd ? DebugColors.Heal : DebugColors.Dim;
            ImGui.TextColored(weaveColor, $"Weave: {(gcd.CanExecuteOgcd ? "Yes" : "No")}");

            ImGui.TableNextColumn();
            ImGui.Text($"Slots: {gcd.WeaveSlots}");

            ImGui.TableNextColumn();
            ImGui.TextColored(DebugColors.Dim, $"Last: {gcd.LastActionName}");

            ImGui.TableNextColumn();
            // Empty

            ImGui.EndTable();
        }

        // Last heal calculation (debug)
        if (!string.IsNullOrEmpty(healing.LastHealStats))
        {
            ImGui.TextColored(DebugColors.Warning, $"Last Calc: {healing.LastHealAmount:N0} HP");
            ImGui.TextColored(DebugColors.Dim, $"  ({healing.LastHealStats})");
        }

        // Pending heals
        var pendingColor = healing.PendingHeals.Count > 0 ? DebugColors.Warning : DebugColors.Dim;
        ImGui.TextColored(pendingColor, $"Pending Heals: {healing.PendingHeals.Count} ({healing.TotalPendingHealAmount:N0} HP total)");

        if (healing.PendingHeals.Count > 0)
        {
            ImGui.Indent();
            foreach (var heal in healing.PendingHeals)
            {
                ImGui.TextColored(DebugColors.Warning, $"-> {heal.TargetName}: +{heal.Amount} HP");
            }
            ImGui.Unindent();
        }
    }

    private static void DrawAoEHealing(DebugSnapshot snapshot, DebugService debugService)
    {
        var healing = snapshot.Healing;

        ImGui.Text("AoE Healing");
        ImGui.SameLine();
        ImGui.TextColored(DebugColors.GetAoEStatusColor(healing.AoEStatus), $"[{healing.AoEStatus}]");
        ImGui.Separator();

        if (ImGui.BeginTable("AoEHealingTable", 3, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Col1", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Col2", ImGuiTableColumnFlags.WidthStretch);
            ImGui.TableSetupColumn("Col3", ImGuiTableColumnFlags.WidthStretch);

            // Row 1: Injured count, selected spell
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text($"Injured: {healing.AoEInjuredCount}");

            ImGui.TableNextColumn();
            var spellName = healing.AoESelectedSpell > 0
                ? debugService.GetActionName(healing.AoESelectedSpell)
                : "None";
            ImGui.Text($"Spell: {spellName}");

            ImGui.TableNextColumn();
            ImGui.Text($"Player HP: {healing.PlayerHpPercent:F1}%");

            // Row 2: Party info
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text($"Party: {healing.PartyListCount}");

            ImGui.TableNextColumn();
            ImGui.Text($"Valid: {healing.PartyValidCount}");

            ImGui.TableNextColumn();
            ImGui.Text($"NPCs: {healing.BattleNpcCount}");

            ImGui.EndTable();
        }

        // NPC fallback info
        if (healing.PartyListCount == 0 && !string.IsNullOrEmpty(healing.NpcInfo))
        {
            ImGui.TextColored(DebugColors.Dim, healing.NpcInfo);
        }
    }

    private static void DrawRecentHeals(DebugSnapshot snapshot)
    {
        var healing = snapshot.Healing;

        ImGui.Text("Recent Heals");
        ImGui.SameLine();
        ImGui.TextColored(DebugColors.Heal, $"(Total: {healing.TotalRecentHealAmount:N0} HP)");
        ImGui.Separator();

        if (healing.RecentHeals.Count == 0)
        {
            ImGui.TextColored(DebugColors.Dim, "No heals yet");
            return;
        }

        // Show last 5 heals
        var displayCount = Math.Min(5, healing.RecentHeals.Count);
        for (var i = 0; i < displayCount; i++)
        {
            var heal = healing.RecentHeals[i];

            ImGui.TextColored(DebugColors.Dim, $"{heal.SecondsAgo:F1}s ago");
            ImGui.SameLine();
            ImGui.TextColored(DebugColors.Heal, $"+{heal.Amount}");
            ImGui.SameLine();
            ImGui.Text($"{heal.ActionName} -> {heal.TargetName}");
        }
    }

    private static void DrawShadowHpTracking(DebugSnapshot snapshot)
    {
        if (!ImGui.CollapsingHeader("Shadow HP Tracking"))
            return;

        var healing = snapshot.Healing;

        if (healing.ShadowHpEntries.Count == 0)
        {
            ImGui.TextColored(DebugColors.Dim, "No entities tracked yet");
            return;
        }

        if (ImGui.BeginTable("ShadowHpTable", 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn("Entity", ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableSetupColumn("Game HP", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Shadow HP", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn("Delta", ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableHeadersRow();

            foreach (var entry in healing.ShadowHpEntries)
            {
                ImGui.TableNextRow();

                ImGui.TableNextColumn();
                ImGui.Text(entry.EntityName);

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.GameHp}");

                ImGui.TableNextColumn();
                ImGui.Text($"{entry.ShadowHp}");

                ImGui.TableNextColumn();
                if (entry.Delta != 0)
                {
                    ImGui.TextColored(DebugColors.Warning, $"{entry.Delta:+#;-#;0}");
                }
                else
                {
                    ImGui.TextColored(DebugColors.Dim, "-");
                }
            }

            ImGui.EndTable();
        }
    }

    private static bool IsSectionVisible(Configuration config, string section)
    {
        if (config.Debug.DebugSectionVisibility.TryGetValue(section, out var visible))
            return visible;
        return true;
    }
}
