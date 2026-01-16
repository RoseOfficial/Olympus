using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config;

namespace Olympus.Windows.Config.Tanks;

/// <summary>
/// Renders the Paladin (Themis) settings section.
/// </summary>
public sealed class PaladinSection
{
    private readonly Configuration config;
    private readonly Action save;

    public PaladinSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Paladin", "Themis", ConfigUIHelpers.PaladinColor);

        DrawMitigationSection();
        DrawHealingSection();
        DrawDamageSection();
    }

    private void DrawMitigationSection()
    {
        if (ConfigUIHelpers.SectionHeader("Mitigation", "PLD"))
        {
            ConfigUIHelpers.BeginIndent();

            ImGui.TextDisabled("Paladin-specific mitigation settings:");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Oath Gauge Abilities:");

            ImGui.TextDisabled("Sheltron uses shared tank gauge setting.");
            ImGui.TextDisabled("Current minimum: " + config.Tank.SheltronMinGauge);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel("Available Abilities:");
            ImGui.BulletText("Sheltron / Holy Sheltron");
            ImGui.BulletText("Intervention");
            ImGui.BulletText("Cover");
            ImGui.BulletText("Divine Veil");
            ImGui.BulletText("Passage of Arms");
            ImGui.BulletText("Hallowed Ground");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText("Detailed PLD settings coming in future update.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawHealingSection()
    {
        if (ConfigUIHelpers.SectionHeader("Self-Healing", "PLD", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Clemency:");
            ImGui.TextDisabled("GCD heal that can be used on self or party members.");
            ImGui.TextDisabled("Uses MP that could be spent on damage spells.");

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.WarningText("Clemency settings coming in future update.");

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader("Damage", "PLD"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.SectionLabel("Rotation Features:");
            ImGui.BulletText("Fast Blade combo");
            ImGui.BulletText("Requiescat window");
            ImGui.BulletText("Fight or Flight window");
            ImGui.BulletText("Confiteor combo");
            ImGui.BulletText("Blade of Honor");

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel("AoE Rotation:");
            ImGui.BulletText("Total Eclipse combo");
            ImGui.BulletText("Circle of Scorn");

            ConfigUIHelpers.Spacing();
            ImGui.TextDisabled("Uses shared tank AoE settings.");
            ImGui.TextDisabled("Current min targets: " + config.Tank.AoEMinTargets);

            ConfigUIHelpers.EndIndent();
        }
    }
}
