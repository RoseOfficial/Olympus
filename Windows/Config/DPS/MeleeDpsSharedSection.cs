using System;
using Dalamud.Bindings.ImGui;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders shared settings for all melee DPS jobs.
/// </summary>
public sealed class MeleeDpsSharedSection
{
    private readonly Configuration config;
    private readonly Action save;

    public MeleeDpsSharedSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0.8f, 1f), Loc.T(LocalizedStrings.MeleeDps.Header, "Melee DPS Shared Settings"));
        ImGui.Spacing();

        DrawPositionalSection();
        DrawBurstSection();
        DrawAoESection();
    }

    private void DrawPositionalSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.MeleeDps.PositionalSection, "Positional Settings"), "MeleeDPS"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.MeleeDps.PositionalDesc, "Configure how melee DPS jobs handle positional requirements."));

            ConfigUIHelpers.Spacing();
            ImGui.BulletText(Loc.T(LocalizedStrings.MeleeDps.PositionalFlank, "Flank positionals: Samurai, Ninja (some), Viper"));
            ImGui.BulletText(Loc.T(LocalizedStrings.MeleeDps.PositionalRear, "Rear positionals: Monk, Dragoon, Ninja (some), Reaper"));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText(Loc.T(LocalizedStrings.MeleeDps.PositionalNote, "Per-job positional settings in each job's section."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.MeleeDps.BurstSection, "Burst Window Coordination"), "MeleeDPS"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.MeleeDps.BurstDesc, "Melee DPS jobs coordinate burst windows with party buffs."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.MeleeDps.PartyBuffs, "Party Buffs Provided:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.MeleeDps.BattleLitany, "Dragoon: Battle Litany"));
            ImGui.BulletText(Loc.T(LocalizedStrings.MeleeDps.Brotherhood, "Monk: Brotherhood"));
            ImGui.BulletText(Loc.T(LocalizedStrings.MeleeDps.ArcaneCircle, "Reaper: Arcane Circle"));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.MeleeDps.BurstNote, "Individual alignment settings in each job's section."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawAoESection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.MeleeDps.AoESection, "AoE Settings"), "MeleeDPS", false))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.MeleeDps.AoEDesc, "Default AoE target thresholds can be customized per job."));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.MeleeDps.AoEDefault, "Default: 3 targets for AoE rotation"));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.MeleeDps.AoENote, "Adjust in each job's section for specific needs."));

            ConfigUIHelpers.EndIndent();
        }
    }
}
