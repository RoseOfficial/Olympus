using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;

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
        ImGui.TextColored(new System.Numerics.Vector4(0.8f, 0.8f, 0.9f, 1f), Loc.T(LocalizedStrings.Tank.SharedHeader, "Shared Tank Settings"));
        ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.SharedDescription, "These settings apply to all tank jobs."));
        ConfigUIHelpers.Spacing();

        DrawMitigationSection();
        DrawStanceSection();
        DrawDamageSection();
    }

    private void DrawMitigationSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Tank.Mitigation, "Mitigation"), "Tank"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableMit = config.Tank.EnableMitigation;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Tank.EnableMitigation, "Enable Mitigation"), ref enableMit))
            {
                config.Tank.EnableMitigation = enableMit;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.EnableMitigationDesc, "Automatically use defensive cooldowns."));

            ConfigUIHelpers.BeginDisabledGroup(!config.Tank.EnableMitigation);

            config.Tank.MitigationThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Tank.MitigationThreshold, "Mitigation Threshold"),
                config.Tank.MitigationThreshold, 40f, 90f, Loc.T(LocalizedStrings.Tank.MitigationThresholdDesc, "Use mitigation when HP drops below this %."), save);

            var useRampart = config.Tank.UseRampartOnCooldown;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Tank.UseRampartOnCooldown, "Use Rampart on Cooldown"), ref useRampart))
            {
                config.Tank.UseRampartOnCooldown = useRampart;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.UseRampartOnCooldownDesc, "If disabled, saves major cooldowns for tank busters."));

            config.Tank.SheltronMinGauge = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Tank.SheltronMinGauge, "Min Gauge for Short CDs"),
                config.Tank.SheltronMinGauge, 0, 100,
                Loc.T(LocalizedStrings.Tank.SheltronMinGaugeDesc, "Minimum gauge for Sheltron/TBN/Heart of Stone/etc."), save);

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawStanceSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Tank.TankStanceSection, "Tank Stance & Aggro"), "Tank"))
        {
            ConfigUIHelpers.BeginIndent();

            var autoStance = config.Tank.AutoTankStance;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Tank.AutoTankStance, "Auto Tank Stance"), ref autoStance))
            {
                config.Tank.AutoTankStance = autoStance;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.AutoTankStanceDesc, "Enable tank stance when entering combat."));

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel(Loc.T("config.job.tank.mt_ot_role", "Role:"));

            var roleNames = new[]
            {
                Loc.T("config.job.tank.role_auto", "Auto (detect from enmity)"),
                Loc.T("config.job.tank.role_mt", "Main Tank"),
                Loc.T("config.job.tank.role_ot", "Off Tank"),
            };
            var currentRole = this.config.Tank.IsMainTankOverride switch
            {
                true => 1,
                false => 2,
                null => 0
            };
            ImGui.SetNextItemWidth(200);
            if (ImGui.Combo(Loc.T("config.job.tank.mt_ot_combo_label", "##mt_ot_role"), ref currentRole, roleNames, roleNames.Length))
            {
                this.config.Tank.IsMainTankOverride = currentRole switch
                {
                    1 => true,
                    2 => false,
                    _ => null
                };
                this.save();
            }
            ImGui.TextDisabled(Loc.T("config.job.tank.mt_ot_role_desc", "Auto detects based on who the enemy is targeting. Override if detection is unreliable."));

            var autoProvoke = config.Tank.AutoProvoke;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Tank.AutoProvoke, "Auto Provoke"), ref autoProvoke))
            {
                config.Tank.AutoProvoke = autoProvoke;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.AutoProvokeDesc, "Automatically Provoke when losing aggro."));

            if (config.Tank.AutoProvoke)
            {
                config.Tank.ProvokeDelay = ConfigUIHelpers.FloatSlider(Loc.T(LocalizedStrings.Tank.ProvokeDelay, "Provoke Delay"),
                    config.Tank.ProvokeDelay, 0f, 5f, "%.1f sec",
                    Loc.T(LocalizedStrings.Tank.ProvokeDelayDesc, "Delay before Provoking (prevents accidental provokes during swaps)."), save);
            }

            var autoShirk = config.Tank.AutoShirk;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Tank.AutoShirk, "Auto Shirk"), ref autoShirk))
            {
                config.Tank.AutoShirk = autoShirk;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.AutoShirkDesc, "Shirk to co-tank after tank swap."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Tank.DamageSection, "Damage"), "Tank"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableDamage = config.Tank.EnableDamage;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Tank.EnableDamage, "Enable Damage"), ref enableDamage))
            {
                config.Tank.EnableDamage = enableDamage;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.EnableDamageDesc, "Execute damage rotation."));

            ConfigUIHelpers.BeginDisabledGroup(!config.Tank.EnableDamage);

            var enableAoE = config.Tank.EnableAoEDamage;
            if (ImGui.Checkbox(Loc.T(LocalizedStrings.Tank.EnableAoEDamage, "Enable AoE Damage"), ref enableAoE))
            {
                config.Tank.EnableAoEDamage = enableAoE;
                save();
            }
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.EnableAoEDamageDesc, "Use AoE abilities (Total Eclipse, etc.)."));

            if (config.Tank.EnableAoEDamage)
            {
                config.Tank.AoEMinTargets = ConfigUIHelpers.IntSlider(Loc.T(LocalizedStrings.Tank.AoEMinTargets, "AoE Min Targets"),
                    config.Tank.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Tank.AoEMinTargetsDesc, "Minimum enemies for AoE rotation."), save);
            }

            ConfigUIHelpers.EndDisabledGroup();
            ConfigUIHelpers.EndIndent();
        }
    }
}
