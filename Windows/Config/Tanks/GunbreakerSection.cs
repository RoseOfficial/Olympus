using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;
using Olympus.Data;
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

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Gunbreaker.EnableHeartOfCorundum, "Heart of Corundum"),
                () => config.Tank.EnableHeartOfCorundum,
                v => config.Tank.EnableHeartOfCorundum = v,
                null,
                save,
                actionId: GNBActions.HeartOfCorundum.ActionId);

            ConfigUIHelpers.BeginDisabledGroup(!config.Tank.EnableHeartOfCorundum);
            config.Tank.HeartOfCorundumThreshold = ConfigUIHelpers.ThresholdSlider(
                Loc.T(LocalizedStrings.Gunbreaker.HeartOfCorundumThreshold, "Heart of Corundum Threshold"),
                config.Tank.HeartOfCorundumThreshold, 50f, 100f,
                Loc.T(LocalizedStrings.Gunbreaker.HeartOfCorundumThresholdDesc, "Apply Heart of Corundum when HP falls below this %."),
                save, v => config.Tank.HeartOfCorundumThreshold = v);
            ConfigUIHelpers.EndDisabledGroup();

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Gunbreaker.EnableHeartOfLight, "Heart of Light"),
                () => config.Tank.EnableHeartOfLight,
                v => config.Tank.EnableHeartOfLight = v,
                null,
                save,
                actionId: GNBActions.HeartOfLight.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Gunbreaker.EnableNebula, "Nebula / Great Nebula"),
                () => config.Tank.EnableNebula,
                v => config.Tank.EnableNebula = v,
                null,
                save,
                actionId: GNBActions.Nebula.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Gunbreaker.EnableCamouflage, "Camouflage"),
                () => config.Tank.EnableCamouflage,
                v => config.Tank.EnableCamouflage = v,
                null,
                save,
                actionId: GNBActions.Camouflage.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Gunbreaker.EnableAurora, "Aurora"),
                () => config.Tank.EnableAurora,
                v => config.Tank.EnableAurora = v,
                null,
                save,
                actionId: GNBActions.Aurora.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Gunbreaker.EnableSuperbolide, "Superbolide"),
                () => config.Tank.EnableSuperbolide,
                v => config.Tank.EnableSuperbolide = v,
                null,
                save,
                actionId: GNBActions.Superbolide.ActionId);

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

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Gunbreaker.DamageSection, "Damage"), "GNB"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Gunbreaker.BuffAbilitiesLabel, "Buff Abilities:"));

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Gunbreaker.EnableNoMercy, "No Mercy"),
                () => config.Tank.EnableNoMercy,
                v => config.Tank.EnableNoMercy = v,
                null,
                save,
                actionId: GNBActions.NoMercy.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Gunbreaker.EnableBloodfest, "Bloodfest"),
                () => config.Tank.EnableBloodfest,
                v => config.Tank.EnableBloodfest = v,
                null,
                save,
                actionId: GNBActions.Bloodfest.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Gunbreaker.DamageOgcdLabel, "Damage oGCDs:"));

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Gunbreaker.EnableBowShock, "Bow Shock"),
                () => config.Tank.EnableBowShock,
                v => config.Tank.EnableBowShock = v,
                null,
                save,
                actionId: GNBActions.BowShock.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Gunbreaker.EnableContinuation, "Continuation (Jugular Rip, Abdomen Tear, Eye Gouge, Hypervelocity)"),
                () => config.Tank.EnableContinuation,
                v => config.Tank.EnableContinuation = v,
                null,
                save,
                actionId: GNBActions.Continuation.ActionId);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Gunbreaker.GapCloserLabel, "Gap Closer:"));

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Gunbreaker.EnableTrajectory, "Trajectory"),
                () => config.Tank.EnableTrajectory,
                v => config.Tank.EnableTrajectory = v,
                null,
                save,
                actionId: GNBActions.Trajectory.ActionId);

            ConfigUIHelpers.Spacing();

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
