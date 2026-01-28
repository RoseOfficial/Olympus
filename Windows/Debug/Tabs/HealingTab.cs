using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;
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
            ImGui.TextColored(DebugColors.Dim, Loc.T(LocalizedStrings.Debug.NotLoggedIn, "Not logged in"));
            return;
        }

        var snapshot = debugService.GetSpellStatus(playerLevel);

        ImGui.Text(Loc.T(LocalizedStrings.Debug.SpellStatus, "Spell Status"));
        ImGui.SameLine();
        ImGui.TextColored(DebugColors.Dim, Loc.TFormat(LocalizedStrings.Debug.LevelFormat, "(Lv{0})", snapshot.PlayerLevel));
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
        SpellCategory.GcdHealSingle => Loc.T(LocalizedStrings.Debug.GcdHealsSingle, "GCD Heals (Single)"),
        SpellCategory.GcdHealAoE => Loc.T(LocalizedStrings.Debug.GcdHealsAoE, "GCD Heals (AoE)"),
        SpellCategory.GcdHealHoT => Loc.T(LocalizedStrings.Debug.GcdHealsHoT, "GCD Heals (HoT)"),
        SpellCategory.OgcdHealSingle => Loc.T(LocalizedStrings.Debug.OgcdHealsSingle, "oGCD Heals (Single)"),
        SpellCategory.OgcdHealAoE => Loc.T(LocalizedStrings.Debug.OgcdHealsAoE, "oGCD Heals (AoE)"),
        SpellCategory.GcdDamageSingle => Loc.T(LocalizedStrings.Debug.GcdDamageSingle, "GCD Damage (Single)"),
        SpellCategory.GcdDamageAoE => Loc.T(LocalizedStrings.Debug.GcdDamageAoE, "GCD Damage (AoE)"),
        SpellCategory.GcdDoT => Loc.T(LocalizedStrings.Debug.GcdDoT, "GCD DoT"),
        SpellCategory.Utility => Loc.T(LocalizedStrings.Debug.Utility, "Utility"),
        _ => category.ToString()
    };

    private static void DrawSpellSelection(DebugService debugService)
    {
        var selection = debugService.GetLastSpellSelection();

        ImGui.Text(Loc.T(LocalizedStrings.Debug.SpellSelection, "Spell Selection"));
        ImGui.Separator();

        if (selection == null)
        {
            ImGui.TextColored(DebugColors.Dim, Loc.T(LocalizedStrings.Debug.NoSelectionYet, "No selection yet"));
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
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.TargetLabel, "Target: {0}", selection.TargetName));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.Missing, "Missing: {0:N0} HP", selection.MissingHp));

            ImGui.TableNextColumn();
            var weaveColor = selection.IsWeaveWindow ? DebugColors.Success : DebugColors.Dim;
            var weaveText = selection.IsWeaveWindow ? Loc.T(LocalizedStrings.Debug.Yes, "Yes") : Loc.T(LocalizedStrings.Debug.No, "No");
            ImGui.TextColored(weaveColor, Loc.TFormat(LocalizedStrings.Debug.WeaveLabel, "Weave: {0}", weaveText));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.Lilies, "Lilies: {0}/3", selection.LilyCount));

            ImGui.EndTable();
        }

        // Selected spell
        if (selection.SelectedSpell != null)
        {
            ImGui.TextColored(DebugColors.Success, Loc.TFormat(LocalizedStrings.Debug.Selected, "✓ Selected: {0}", selection.SelectedSpell));
            ImGui.TextColored(DebugColors.Dim, Loc.TFormat(LocalizedStrings.Debug.Reason, "  Reason: {0}", selection.SelectionReason ?? "-"));
        }
        else
        {
            ImGui.TextColored(DebugColors.Warning, Loc.T(LocalizedStrings.Debug.NoSpellSelected, "✗ No spell selected"));
            ImGui.TextColored(DebugColors.Dim, Loc.TFormat(LocalizedStrings.Debug.Reason, "  Reason: {0}", selection.SelectionReason ?? "-"));
        }

        // Candidates table (collapsible)
        if (selection.Candidates.Count > 0 && ImGui.CollapsingHeader(Loc.TFormat(LocalizedStrings.Debug.Candidates, "Candidates ({0})", selection.Candidates.Count) + "##SpellCandidates"))
        {
            if (ImGui.BeginTable("CandidatesTable", 5, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
            {
                ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.Spell, "Spell"), ImGuiTableColumnFlags.WidthFixed, 120);
                ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.Heal, "Heal"), ImGuiTableColumnFlags.WidthFixed, 60);
                ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.Eff, "Eff"), ImGuiTableColumnFlags.WidthFixed, 45);
                ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.Score, "Score"), ImGuiTableColumnFlags.WidthFixed, 50);
                ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.Status, "Status"), ImGuiTableColumnFlags.WidthStretch);
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
        ImGui.Text(Loc.T(LocalizedStrings.Debug.HpPrediction, "HP Prediction"));
        ImGui.Separator();

        // Player stats for heal calculation
        var statsInfo = debugService.GetPlayerStatsDebugInfo();
        ImGui.TextColored(DebugColors.Dim, Loc.TFormat(LocalizedStrings.Debug.Stats, "Stats: {0}", statsInfo));

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
            ImGui.TextColored(gcdColor, Loc.TFormat(LocalizedStrings.Debug.GcdFormat, "GCD: {0}", gcd.State));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.Rem, "Rem: {0:F2}s", gcd.GcdRemaining));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.Anim, "Anim: {0:F2}s", gcd.AnimationLockRemaining));

            ImGui.TableNextColumn();
            var castingText = gcd.IsCasting ? Loc.T(LocalizedStrings.Debug.Yes, "Yes") : Loc.T(LocalizedStrings.Debug.No, "No");
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.Casting, "Casting: {0}", castingText));

            // Row 2: Weave, slots, last action
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            var weaveColor = gcd.CanExecuteOgcd ? DebugColors.Heal : DebugColors.Dim;
            var weaveText = gcd.CanExecuteOgcd ? Loc.T(LocalizedStrings.Debug.Yes, "Yes") : Loc.T(LocalizedStrings.Debug.No, "No");
            ImGui.TextColored(weaveColor, Loc.TFormat(LocalizedStrings.Debug.WeaveLabel, "Weave: {0}", weaveText));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.SlotsFormat, "Slots: {0}", gcd.WeaveSlots));

            ImGui.TableNextColumn();
            ImGui.TextColored(DebugColors.Dim, Loc.TFormat(LocalizedStrings.Debug.LastFormat, "Last: {0}", gcd.LastActionName));

            ImGui.TableNextColumn();
            // Empty

            ImGui.EndTable();
        }

        // Last heal calculation (debug)
        if (!string.IsNullOrEmpty(healing.LastHealStats))
        {
            ImGui.TextColored(DebugColors.Warning, Loc.TFormat(LocalizedStrings.Debug.LastCalc, "Last Calc: {0:N0} HP", healing.LastHealAmount));
            ImGui.TextColored(DebugColors.Dim, $"  ({healing.LastHealStats})");
        }

        // Pending heals
        var pendingColor = healing.PendingHeals.Count > 0 ? DebugColors.Warning : DebugColors.Dim;
        ImGui.TextColored(pendingColor, Loc.TFormat(LocalizedStrings.Debug.PendingHeals, "Pending Heals: {0} ({1:N0} HP total)", healing.PendingHeals.Count, healing.TotalPendingHealAmount));

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

        ImGui.Text(Loc.T(LocalizedStrings.Debug.AoEHealing, "AoE Healing"));
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
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.Injured, "Injured: {0}", healing.AoEInjuredCount));

            ImGui.TableNextColumn();
            var spellName = healing.AoESelectedSpell > 0
                ? debugService.GetActionName(healing.AoESelectedSpell)
                : Loc.T(LocalizedStrings.Debug.NoneLabel, "None");
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.SpellLabel, "Spell: {0}", spellName));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.PlayerHp, "Player HP: {0:F1}%", healing.PlayerHpPercent));

            // Row 2: Party info
            ImGui.TableNextRow();

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.Party, "Party: {0}", healing.PartyListCount));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.Valid, "Valid: {0}", healing.PartyValidCount));

            ImGui.TableNextColumn();
            ImGui.Text(Loc.TFormat(LocalizedStrings.Debug.Npcs, "NPCs: {0}", healing.BattleNpcCount));

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

        ImGui.Text(Loc.T(LocalizedStrings.Debug.RecentHeals, "Recent Heals"));
        ImGui.SameLine();
        ImGui.TextColored(DebugColors.Heal, Loc.TFormat(LocalizedStrings.Debug.Total, "(Total: {0:N0} HP)", healing.TotalRecentHealAmount));
        ImGui.Separator();

        if (healing.RecentHeals.Count == 0)
        {
            ImGui.TextColored(DebugColors.Dim, Loc.T(LocalizedStrings.Debug.NoHealsYet, "No heals yet"));
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
        if (!ImGui.CollapsingHeader(Loc.T(LocalizedStrings.Debug.ShadowHpTracking, "Shadow HP Tracking")))
            return;

        var healing = snapshot.Healing;

        if (healing.ShadowHpEntries.Count == 0)
        {
            ImGui.TextColored(DebugColors.Dim, Loc.T(LocalizedStrings.Debug.NoEntitiesTrackedYet, "No entities tracked yet"));
            return;
        }

        if (ImGui.BeginTable("ShadowHpTable", 4, ImGuiTableFlags.BordersInnerV | ImGuiTableFlags.RowBg | ImGuiTableFlags.SizingStretchProp))
        {
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.Entity, "Entity"), ImGuiTableColumnFlags.WidthFixed, 150);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.GameHp, "Game HP"), ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.ShadowHpLabel, "Shadow HP"), ImGuiTableColumnFlags.WidthFixed, 80);
            ImGui.TableSetupColumn(Loc.T(LocalizedStrings.Debug.Delta, "Delta"), ImGuiTableColumnFlags.WidthFixed, 80);
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
