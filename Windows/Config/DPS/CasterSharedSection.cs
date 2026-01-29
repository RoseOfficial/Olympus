using System;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders shared settings for all caster DPS jobs.
/// </summary>
public sealed class CasterSharedSection
{
    private readonly Configuration config;
    private readonly Action save;

    public CasterSharedSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0.8f, 1f), Loc.T(LocalizedStrings.Caster.Header, "Caster DPS Shared Settings"));
        ImGui.Spacing();

        DrawMpSection();
        DrawUtilitySection();
        DrawBurstSection();
    }

    private void DrawMpSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Caster.MpSection, "MP Management"), "Caster"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.Caster.MpDesc, "All casters share Lucid Dreaming for MP recovery."));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Caster.LucidNote, "Individual Lucid Dreaming thresholds in each job's section."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Caster.LucidDefault, "Default: 70% MP"));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawUtilitySection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Caster.UtilitySection, "Utility Abilities"), "Caster"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.Caster.UtilityDesc, "Casters provide various utility to the party."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Caster.RaiseLabel, "Raise Abilities:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Caster.SummonerRaise, "Summoner: Resurrection"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Caster.RedMageRaise, "Red Mage: Verraise (Dualcast)"));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Caster.PartyMitLabel, "Party Mitigation:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Caster.Addle, "Addle - All casters (role action)"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Caster.MagickBarrier, "Red Mage: Magick Barrier"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Caster.TemperaGrassa, "Pictomancer: Tempera Grassa"));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Caster.UtilityNote, "Individual utility settings in each job's section."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Caster.BurstSection, "Burst Window Coordination"), "Caster", false))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.Caster.BurstDesc, "Casters coordinate burst windows with party buffs."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Caster.PartyBuffs, "Party Buffs Provided:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Caster.SearingLight, "Summoner: Searing Light"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Caster.Embolden, "Red Mage: Embolden"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Caster.StarryMuse, "Pictomancer: Starry Muse"));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Caster.BurstNote, "Individual alignment settings in each job's section."));

            ConfigUIHelpers.EndIndent();
        }
    }
}
