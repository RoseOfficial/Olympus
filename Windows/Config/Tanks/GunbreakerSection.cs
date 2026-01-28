using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Localization;

namespace Olympus.Windows.Config.Tanks;

/// <summary>
/// Renders the Gunbreaker (Hephaestus) settings section.
/// </summary>
public sealed class GunbreakerSection
{
    private readonly Configuration config;
    private readonly Action save;

    public GunbreakerSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Gunbreaker", "Hephaestus", ConfigUIHelpers.GunbreakerColor);

        DrawMitigationSection();
        DrawCartridgeSection();
        DrawDamageSection();
    }

    private void DrawMitigationSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Gunbreaker.MitigationSection, "Mitigation"), "GNB"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled(Loc.T(LocalizedStrings.Gunbreaker.MitigationDesc, "Gunbreaker-specific mitigation settings:"));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Gunbreaker.HeartOfCorundumLabel, "Heart of Corundum:"));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Gunbreaker.HeartOfCorundumDesc1, "Powerful short cooldown mitigation."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Gunbreaker.HeartOfCorundumDesc2, "Grants healing and damage reduction."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.UsesSharedGaugeSetting, "Uses shared tank gauge setting."));
            ImGui.TextDisabled(Loc.TFormat(LocalizedStrings.Tank.CurrentMinGauge, "Current minimum: {0}", config.Tank.SheltronMinGauge));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Gunbreaker.AvailableAbilities, "Available Abilities:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.HeartOfStoneCorundum, "Heart of Stone / Heart of Corundum"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.Aurora, "Aurora (regen)"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.Camouflage, "Camouflage"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.HeartOfLight, "Heart of Light"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.Superbolide, "Superbolide"));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText(Loc.T(LocalizedStrings.Gunbreaker.DetailedSettingsWarning, "Detailed GNB settings coming in future update."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawCartridgeSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Gunbreaker.CartridgeSection, "Cartridges"), "GNB", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Gunbreaker.PowderGaugeLabel, "Powder Gauge:"));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Gunbreaker.PowderGaugeDesc1, "Holds up to 3 cartridges."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Gunbreaker.PowderGaugeDesc2, "Built from Solid Barrel combo."));
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Gunbreaker.PowderGaugeDesc3, "Bloodfest grants 3 cartridges."));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Gunbreaker.CartridgeUsage, "Cartridge Usage:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.GnashingFangCombo, "Gnashing Fang combo (1 cartridge)"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.BurstStrike, "Burst Strike (1 cartridge)"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.DoubleDown, "Double Down (2 cartridges)"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.FatedCircleAoE, "Fated Circle AoE (1 cartridge)"));

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText(Loc.T(LocalizedStrings.Gunbreaker.CartridgeWarning, "Cartridge settings coming in future update."));

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Gunbreaker.DamageSection, "Damage"), "GNB"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Gunbreaker.RotationFeatures, "Rotation Features:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.KeenEdgeCombo, "Keen Edge combo"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.NoMercyWindow, "No Mercy window"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.GnashingFangContinuation, "Gnashing Fang + Continuation"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.DoubleDownDamage, "Double Down"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.BurstStrikeHypervelocity, "Burst Strike + Hypervelocity"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.SonicBreakBowShock, "Sonic Break / Bow Shock"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.ReignOfBeastsCombo, "Reign of Beasts combo"));

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Gunbreaker.AoERotation, "AoE Rotation:"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.DemonSliceCombo, "Demon Slice combo"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.FatedCircle, "Fated Circle"));
            ImGui.BulletText(Loc.T(LocalizedStrings.Gunbreaker.BowShock, "Bow Shock"));

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled(Loc.T(LocalizedStrings.Tank.UsesSharedAoESettings, "Uses shared tank AoE settings."));
            ImGui.TextDisabled(Loc.TFormat(LocalizedStrings.Tank.CurrentMinTargets, "Current min targets: {0}", config.Tank.AoEMinTargets));

            ConfigUIHelpers.EndIndent();
        }
    }
}
