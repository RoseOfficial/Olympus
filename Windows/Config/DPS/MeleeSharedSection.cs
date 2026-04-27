using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders settings that apply to all melee DPS jobs (DRG/MNK/NIN/SAM/RPR/VPR).
/// </summary>
public sealed class MeleeSharedSection
{
    private readonly Configuration config;
    private readonly Action save;

    public MeleeSharedSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.9f, 0.8f, 1f),
            Loc.T(LocalizedStrings.MeleeShared.Header, "Shared Melee Settings"));
        ImGui.TextDisabled(Loc.T(LocalizedStrings.MeleeShared.Description,
            "These settings apply to all melee DPS jobs."));
        ConfigUIHelpers.Spacing();

        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.MeleeShared.SelfHeal, "Self-Heal"), "Melee"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.MeleeShared.EnableSecondWind, "Enable Second Wind"),
                () => config.MeleeShared.EnableSecondWind,
                v => config.MeleeShared.EnableSecondWind = v,
                null, save,
                actionId: RoleActions.SecondWind.ActionId);

            if (config.MeleeShared.EnableSecondWind)
            {
                config.MeleeShared.SecondWindHpThreshold = ConfigUIHelpers.ThresholdSlider(
                    Loc.T(LocalizedStrings.MeleeShared.SecondWindHpThreshold, "Second Wind HP Threshold"),
                    config.MeleeShared.SecondWindHpThreshold, 10f, 90f,
                    null, save, v => config.MeleeShared.SecondWindHpThreshold = v);
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
