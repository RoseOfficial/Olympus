using System;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders shared settings for all ranged physical DPS jobs.
/// </summary>
public sealed class RangedDpsSharedSection
{
    private readonly Configuration config;
    private readonly Action save;

    public RangedDpsSharedSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0.8f, 1f), Loc.T(LocalizedStrings.RangedDps.Header, "Ranged Physical DPS Shared Settings"));
        ImGui.Spacing();

        DrawUtilitySection();
        DrawBurstSection();
        DrawAoESection();
    }

    private void DrawUtilitySection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.RangedDps.UtilitySection, "Utility Abilities"), "RangedDPS"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.RangedDps.UtilityDesc, "Ranged physical DPS have unique utility abilities."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.RangedDps.InterruptLabel, "Interrupt:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.RangedDps.HeadGraze, "Head Graze - All ranged physical DPS"));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.RangedDps.PartyMitLabel, "Party Mitigation:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.RangedDps.Troubadour, "Bard: Troubadour"));
            ImGui.BulletText(Loc.T(LocalizedStrings.RangedDps.Tactician, "Machinist: Tactician"));
            ImGui.BulletText(Loc.T(LocalizedStrings.RangedDps.ShieldSamba, "Dancer: Shield Samba"));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.RangedDps.UtilityNote, "Individual utility settings in each job's section."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.RangedDps.BurstSection, "Burst Window Coordination"), "RangedDPS"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.RangedDps.BurstDesc, "Ranged physical DPS coordinate burst windows with party buffs."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.RangedDps.PartyBuffs, "Party Buffs Provided:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.RangedDps.BattleVoice, "Bard: Battle Voice, Radiant Finale"));
            ImGui.BulletText(Loc.T(LocalizedStrings.RangedDps.TechnicalFinish, "Dancer: Technical Finish, Devilment"));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.RangedDps.BurstNote, "Individual alignment settings in each job's section."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawAoESection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.RangedDps.AoESection, "AoE Settings"), "RangedDPS", false))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.RangedDps.AoEDesc, "Default AoE target thresholds can be customized per job."));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.RangedDps.AoEDefault, "Default: 3 targets for AoE rotation"));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.RangedDps.AoENote, "Adjust in each job's section for specific needs."));

            ConfigUIHelpers.EndIndent();
        }
    }
}
