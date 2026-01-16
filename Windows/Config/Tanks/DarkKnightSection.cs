using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;

namespace Olympus.Windows.Config.Tanks;

/// <summary>
/// Renders the Dark Knight (Nyx) settings section.
/// </summary>
public sealed class DarkKnightSection
{
    private readonly Configuration config;
    private readonly Action save;

    public DarkKnightSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Dark Knight", "Nyx", ConfigUIHelpers.DarkKnightColor);

        DrawMitigationSection();
        DrawBloodGaugeSection();
        DrawDamageSection();
    }

    private void DrawMitigationSection()
    {
        if (ConfigUIHelpers.SectionHeader("Mitigation", "DRK"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled("Dark Knight-specific mitigation settings:");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("The Blackest Night (TBN):");
            ImGui.TextDisabled("Powerful single-target shield.");
            ImGui.TextDisabled("Grants Dark Arts when shield breaks.");
            ImGui.TextDisabled("Uses shared tank gauge setting.");
            ImGui.TextDisabled("Current minimum: " + config.Tank.SheltronMinGauge);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Available Abilities:");
            ImGui.BulletText("The Blackest Night");
            ImGui.BulletText("Oblation");
            ImGui.BulletText("Dark Mind (magic only)");
            ImGui.BulletText("Dark Missionary");
            ImGui.BulletText("Living Dead");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText("Detailed DRK settings coming in future update.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBloodGaugeSection()
    {
        if (ConfigUIHelpers.SectionHeader("Blood Gauge & MP", "DRK", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Blood Gauge:");
            ImGui.TextDisabled("Built from combo actions.");
            ImGui.TextDisabled("Spent on Bloodspiller/Quietus and Living Shadow.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("MP Management:");
            ImGui.TextDisabled("Edge of Shadow/Flood of Shadow costs 3000 MP.");
            ImGui.TextDisabled("TBN costs 3000 MP.");
            ImGui.TextDisabled("Balance offense and defense.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Delirium:");
            ImGui.TextDisabled("Grants 3 free Bloodspillers.");
            ImGui.TextDisabled("Scarlet Delirium follow-up combo.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText("Blood Gauge settings coming in future update.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader("Damage", "DRK"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Rotation Features:");
            ImGui.BulletText("Hard Slash combo");
            ImGui.BulletText("Delirium + Scarlet Delirium combo");
            ImGui.BulletText("Edge of Shadow weaves");
            ImGui.BulletText("Shadowbringer");
            ImGui.BulletText("Living Shadow");
            ImGui.BulletText("Salted Earth + Salt and Darkness");

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel("AoE Rotation:");
            ImGui.BulletText("Unleash combo");
            ImGui.BulletText("Quietus under Delirium");
            ImGui.BulletText("Flood of Shadow");
            ImGui.BulletText("Abyssal Drain");

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled("Uses shared tank AoE settings.");
            ImGui.TextDisabled("Current min targets: " + config.Tank.AoEMinTargets);

            ConfigUIHelpers.EndIndent();
        }
    }
}
