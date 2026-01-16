using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;

namespace Olympus.Windows.Config.Tanks;

/// <summary>
/// Renders the Warrior (Ares) settings section.
/// </summary>
public sealed class WarriorSection
{
    private readonly Configuration config;
    private readonly Action save;

    public WarriorSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Warrior", "Ares", ConfigUIHelpers.WarriorColor);

        DrawMitigationSection();
        DrawBeastGaugeSection();
        DrawDamageSection();
    }

    private void DrawMitigationSection()
    {
        if (ConfigUIHelpers.SectionHeader("Mitigation", "WAR"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled("Warrior-specific mitigation settings:");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Beast Gauge Abilities:");
            ImGui.TextDisabled("Uses shared tank gauge setting for Raw Intuition.");
            ImGui.TextDisabled("Current minimum: " + config.Tank.SheltronMinGauge);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Available Abilities:");
            ImGui.BulletText("Raw Intuition / Bloodwhetting");
            ImGui.BulletText("Nascent Flash (party member)");
            ImGui.BulletText("Thrill of Battle");
            ImGui.BulletText("Equilibrium (self-heal)");
            ImGui.BulletText("Shake It Off");
            ImGui.BulletText("Holmgang");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText("Detailed WAR settings coming in future update.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBeastGaugeSection()
    {
        if (ConfigUIHelpers.SectionHeader("Beast Gauge", "WAR", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Gauge Usage:");
            ImGui.TextDisabled("Beast Gauge builds from combo actions.");
            ImGui.TextDisabled("Spent on Fell Cleave/Decimate and Raw Intuition.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Inner Release:");
            ImGui.TextDisabled("Burst window with free Fell Cleaves.");
            ImGui.TextDisabled("Primal Rend follow-up.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText("Beast Gauge settings coming in future update.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader("Damage", "WAR"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Rotation Features:");
            ImGui.BulletText("Heavy Swing combo");
            ImGui.BulletText("Inner Release window");
            ImGui.BulletText("Fell Cleave spam");
            ImGui.BulletText("Primal Rend + Primal Ruination");
            ImGui.BulletText("Onslaught charges");

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel("AoE Rotation:");
            ImGui.BulletText("Overpower combo");
            ImGui.BulletText("Decimate under Inner Release");
            ImGui.BulletText("Orogeny");

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled("Uses shared tank AoE settings.");
            ImGui.TextDisabled("Current min targets: " + config.Tank.AoEMinTargets);

            ConfigUIHelpers.EndIndent();
        }
    }
}
