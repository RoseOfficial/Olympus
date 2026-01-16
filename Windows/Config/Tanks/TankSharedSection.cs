using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;

namespace Olympus.Windows.Config.Tanks;

/// <summary>
/// Renders the shared tank settings section.
/// </summary>
public sealed class TankSharedSection
{
    private readonly Configuration config;
    private readonly Action save;

    public TankSharedSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0.9f, 1f), "Shared Tank Settings");
        ImGui.TextDisabled("These settings apply to all tank jobs.");
        ConfigUIHelpers.Spacing();

        DrawMitigationSection();
        DrawStanceSection();
        DrawDamageSection();
    }

    private void DrawMitigationSection()
    {
        if (ConfigUIHelpers.SectionHeader("Mitigation", "Tank"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableMit = config.Tank.EnableMitigation;
            if (ImGui.Checkbox("Enable Mitigation", ref enableMit))
            {
                config.Tank.EnableMitigation = enableMit;
                save();
            }
            ImGui.TextDisabled("Automatically use defensive cooldowns.");

            ConfigUIHelpers.BeginDisabledGroup(!config.Tank.EnableMitigation);

            config.Tank.MitigationThreshold = ConfigUIHelpers.ThresholdSlider("Mitigation Threshold",
                config.Tank.MitigationThreshold, 40f, 90f, "Use mitigation when HP drops below this %.", save);

            var useRampart = config.Tank.UseRampartOnCooldown;
            if (ImGui.Checkbox("Use Rampart on Cooldown", ref useRampart))
            {
                config.Tank.UseRampartOnCooldown = useRampart;
                save();
            }
            ImGui.TextDisabled("If disabled, saves major cooldowns for tank busters.");

            config.Tank.SheltronMinGauge = ConfigUIHelpers.IntSlider("Min Gauge for Short CDs",
                config.Tank.SheltronMinGauge, 0, 100,
                "Minimum gauge for Sheltron/TBN/Heart of Stone/etc.", save);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawStanceSection()
    {
        if (ConfigUIHelpers.SectionHeader("Tank Stance & Aggro", "Tank"))
        {
            ConfigUIHelpers.BeginIndent();

            var autoStance = config.Tank.AutoTankStance;
            if (ImGui.Checkbox("Auto Tank Stance", ref autoStance))
            {
                config.Tank.AutoTankStance = autoStance;
                save();
            }
            ImGui.TextDisabled("Enable tank stance when entering combat.");

            var autoProvoke = config.Tank.AutoProvoke;
            if (ImGui.Checkbox("Auto Provoke", ref autoProvoke))
            {
                config.Tank.AutoProvoke = autoProvoke;
                save();
            }
            ImGui.TextDisabled("Automatically Provoke when losing aggro.");

            if (config.Tank.AutoProvoke)
            {
                config.Tank.ProvokeDelay = ConfigUIHelpers.FloatSlider("Provoke Delay",
                    config.Tank.ProvokeDelay, 0f, 5f, "%.1f sec",
                    "Delay before Provoking (prevents accidental provokes during swaps).", save);
            }

            var autoShirk = config.Tank.AutoShirk;
            if (ImGui.Checkbox("Auto Shirk", ref autoShirk))
            {
                config.Tank.AutoShirk = autoShirk;
                save();
            }
            ImGui.TextDisabled("Shirk to co-tank after tank swap.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader("Damage", "Tank"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableDamage = config.Tank.EnableDamage;
            if (ImGui.Checkbox("Enable Damage", ref enableDamage))
            {
                config.Tank.EnableDamage = enableDamage;
                save();
            }
            ImGui.TextDisabled("Execute damage rotation.");

            ConfigUIHelpers.BeginDisabledGroup(!config.Tank.EnableDamage);

            var enableAoE = config.Tank.EnableAoEDamage;
            if (ImGui.Checkbox("Enable AoE Damage", ref enableAoE))
            {
                config.Tank.EnableAoEDamage = enableAoE;
                save();
            }
            ImGui.TextDisabled("Use AoE abilities (Total Eclipse, etc.).");

            if (config.Tank.EnableAoEDamage)
            {
                config.Tank.AoEMinTargets = ConfigUIHelpers.IntSlider("AoE Min Targets",
                    config.Tank.AoEMinTargets, 2, 8,
                    "Minimum enemies for AoE rotation.", save);
            }

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }
}
