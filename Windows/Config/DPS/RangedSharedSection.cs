using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders settings that apply to all ranged physical DPS jobs (BRD/MCH/DNC).
/// </summary>
public sealed class RangedSharedSection
{
    private readonly Configuration config;
    private readonly Action save;

    public RangedSharedSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.9f, 0.8f, 1f),
            Loc.T(LocalizedStrings.RangedShared.Header, "Shared Ranged Settings"));
        ImGui.TextDisabled(Loc.T(LocalizedStrings.RangedShared.Description,
            "These settings apply to all ranged physical DPS jobs."));
        ConfigUIHelpers.Spacing();

        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.RangedShared.Interrupts, "Interrupts"), "Ranged"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.RangedShared.EnableHeadGraze, "Enable Head Graze"),
                () => config.RangedShared.EnableHeadGraze,
                v => config.RangedShared.EnableHeadGraze = v,
                null, save,
                actionId: RoleActions.HeadGraze.ActionId);

            ConfigUIHelpers.EndIndent();
        }
    }
}
