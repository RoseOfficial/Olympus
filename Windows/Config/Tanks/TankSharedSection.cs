using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
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

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Tank.EnableMitigation, "Enable Mitigation"), () => config.Tank.EnableMitigation, v => config.Tank.EnableMitigation = v,
                Loc.T(LocalizedStrings.Tank.EnableMitigationDesc, "Automatically use defensive cooldowns."), save);

            ConfigUIHelpers.BeginDisabledGroup(!config.Tank.EnableMitigation);

            config.Tank.MitigationThreshold = ConfigUIHelpers.ThresholdSlider(Loc.T(LocalizedStrings.Tank.MitigationThreshold, "Mitigation Threshold"),
                config.Tank.MitigationThreshold, 40f, 90f, Loc.T(LocalizedStrings.Tank.MitigationThresholdDesc, "Use mitigation when HP drops below this %."), save);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Tank.UseRampartOnCooldown, "Use Rampart on Cooldown"), () => config.Tank.UseRampartOnCooldown, v => config.Tank.UseRampartOnCooldown = v,
                null, save,
                actionId: PLDActions.Rampart.ActionId);

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

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Tank.AutoTankStance, "Auto Tank Stance"), () => config.Tank.AutoTankStance, v => config.Tank.AutoTankStance = v,
                Loc.T(LocalizedStrings.Tank.AutoTankStanceDesc, "Enable tank stance when entering combat."), save);

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

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Tank.AutoProvoke, "Auto Provoke"), () => config.Tank.AutoProvoke, v => config.Tank.AutoProvoke = v,
                null, save,
                actionId: PLDActions.Provoke.ActionId);

            if (config.Tank.AutoProvoke)
            {
                config.Tank.ProvokeDelay = ConfigUIHelpers.FloatSlider(Loc.T(LocalizedStrings.Tank.ProvokeDelay, "Provoke Delay"),
                    config.Tank.ProvokeDelay, 0f, 5f, "%.1f sec",
                    Loc.T(LocalizedStrings.Tank.ProvokeDelayDesc, "Delay before Provoking (prevents accidental provokes during swaps)."), save);
            }

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Tank.AutoShirk, "Auto Shirk"), () => config.Tank.AutoShirk, v => config.Tank.AutoShirk = v,
                null, save,
                actionId: PLDActions.Shirk.ActionId);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Tank.DamageSection, "Damage"), "Tank"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Tank.EnableDamage, "Enable Damage"), () => config.Tank.EnableDamage, v => config.Tank.EnableDamage = v,
                Loc.T(LocalizedStrings.Tank.EnableDamageDesc, "Execute damage rotation."), save);

            ConfigUIHelpers.BeginDisabledGroup(!config.Tank.EnableDamage);

            ConfigUIHelpers.Toggle(Loc.T(LocalizedStrings.Tank.EnableAoEDamage, "Enable AoE Damage"), () => config.Tank.EnableAoEDamage, v => config.Tank.EnableAoEDamage = v,
                Loc.T(LocalizedStrings.Tank.EnableAoEDamageDesc, "Use AoE abilities (Total Eclipse, etc.)."), save);

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
