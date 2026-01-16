using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;

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
        if (ConfigUIHelpers.SectionHeader("Mitigation", "GNB"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled("Gunbreaker-specific mitigation settings:");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Heart of Corundum:");
            ImGui.TextDisabled("Powerful short cooldown mitigation.");
            ImGui.TextDisabled("Grants healing and damage reduction.");
            ImGui.TextDisabled("Uses shared tank gauge setting.");
            ImGui.TextDisabled("Current minimum: " + config.Tank.SheltronMinGauge);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Available Abilities:");
            ImGui.BulletText("Heart of Stone / Heart of Corundum");
            ImGui.BulletText("Aurora (regen)");
            ImGui.BulletText("Camouflage");
            ImGui.BulletText("Heart of Light");
            ImGui.BulletText("Superbolide");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText("Detailed GNB settings coming in future update.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawCartridgeSection()
    {
        if (ConfigUIHelpers.SectionHeader("Cartridges", "GNB", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Powder Gauge:");
            ImGui.TextDisabled("Holds up to 3 cartridges.");
            ImGui.TextDisabled("Built from Solid Barrel combo.");
            ImGui.TextDisabled("Bloodfest grants 3 cartridges.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Cartridge Usage:");
            ImGui.BulletText("Gnashing Fang combo (1 cartridge)");
            ImGui.BulletText("Burst Strike (1 cartridge)");
            ImGui.BulletText("Double Down (2 cartridges)");
            ImGui.BulletText("Fated Circle AoE (1 cartridge)");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText("Cartridge settings coming in future update.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader("Damage", "GNB"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Rotation Features:");
            ImGui.BulletText("Keen Edge combo");
            ImGui.BulletText("No Mercy window");
            ImGui.BulletText("Gnashing Fang + Continuation");
            ImGui.BulletText("Double Down");
            ImGui.BulletText("Burst Strike + Hypervelocity");
            ImGui.BulletText("Sonic Break / Bow Shock");
            ImGui.BulletText("Reign of Beasts combo");

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel("AoE Rotation:");
            ImGui.BulletText("Demon Slice combo");
            ImGui.BulletText("Fated Circle");
            ImGui.BulletText("Bow Shock");

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled("Uses shared tank AoE settings.");
            ImGui.TextDisabled("Current min targets: " + config.Tank.AoEMinTargets);

            ConfigUIHelpers.EndIndent();
        }
    }
}
