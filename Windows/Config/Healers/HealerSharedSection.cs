using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.Healers;

/// <summary>
/// Renders settings that apply to all healer jobs (WHM/SCH/AST/SGE).
/// </summary>
public sealed class HealerSharedSection
{
    private readonly Configuration config;
    private readonly Action save;

    public HealerSharedSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.9f, 0.8f, 1f),
            Loc.T(LocalizedStrings.HealerShared.Header, "Shared Healer Settings"));
        ImGui.TextDisabled(Loc.T(LocalizedStrings.HealerShared.Description,
            "These settings apply to all healer jobs."));
        ConfigUIHelpers.Spacing();

        DrawMpManagement();
    }

    private void DrawMpManagement()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.HealerShared.MpManagement, "MP Management"), "Healer"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.HealerShared.EnableLucidDreaming, "Enable Lucid Dreaming"),
                () => config.HealerShared.EnableLucidDreaming,
                v => config.HealerShared.EnableLucidDreaming = v,
                null, save,
                actionId: RoleActions.LucidDreaming.ActionId);

            if (config.HealerShared.EnableLucidDreaming)
            {
                config.HealerShared.LucidDreamingThreshold = ConfigUIHelpers.ThresholdSlider(
                    Loc.T(LocalizedStrings.HealerShared.LucidMpThreshold, "Lucid MP Threshold"),
                    config.HealerShared.LucidDreamingThreshold, 40f, 90f,
                    Loc.T(LocalizedStrings.HealerShared.LucidMpThresholdDesc,
                        "Fire Lucid Dreaming when MP drops below this percentage. White Mage uses its own predictive MP forecast and ignores this slider."),
                    save,
                    v => config.HealerShared.LucidDreamingThreshold = v);
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
