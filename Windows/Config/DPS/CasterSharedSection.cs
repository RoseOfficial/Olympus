using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders settings that apply to all caster DPS jobs (BLM/SMN/RDM/PCT).
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
        ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.9f, 0.8f, 1f),
            Loc.T(LocalizedStrings.CasterShared.Header, "Shared Caster Settings"));
        ImGui.TextDisabled(Loc.T(LocalizedStrings.CasterShared.Description,
            "These settings apply to all caster DPS jobs."));
        ConfigUIHelpers.Spacing();

        if (ConfigUIHelpers.SectionHeader(
            Loc.T(LocalizedStrings.CasterShared.MpRecovery, "MP Recovery"), "Caster"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.CasterShared.EnableLucidDreaming, "Enable Lucid Dreaming"),
                () => config.CasterShared.EnableLucidDreaming,
                v => config.CasterShared.EnableLucidDreaming = v,
                null, save,
                actionId: RoleActions.LucidDreaming.ActionId);

            if (config.CasterShared.EnableLucidDreaming)
            {
                config.CasterShared.LucidDreamingThreshold = ConfigUIHelpers.ThresholdSlider(
                    Loc.T(LocalizedStrings.CasterShared.LucidDreamingThreshold, "Lucid MP Threshold"),
                    config.CasterShared.LucidDreamingThreshold, 40f, 90f, null, save,
                    v => config.CasterShared.LucidDreamingThreshold = v);
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
