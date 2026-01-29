using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Pictomancer (Iris) settings section.
/// </summary>
public sealed class PictomancerSection
{
    private readonly Configuration config;
    private readonly Action save;

    public PictomancerSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Pictomancer", "Iris", ConfigUIHelpers.PictomancerColor);

        DrawDamageSection();
        DrawCanvasSection();
        DrawMuseSection();
        DrawBurstSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Pictomancer.DamageSection, "Damage"), "PCT"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableHolyInWhite = config.Pictomancer.EnableHolyInWhite;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Pictomancer.EnableHolyInWhite, "Enable Holy in White"), ref enableHolyInWhite,
                Loc.T(LocalizedStrings.Pictomancer.EnableHolyInWhiteDesc, "Use Holy in White"), save))
            {
                config.Pictomancer.EnableHolyInWhite = enableHolyInWhite;
            }

            var enableCometInBlack = config.Pictomancer.EnableCometInBlack;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Pictomancer.EnableCometInBlack, "Enable Comet in Black"), ref enableCometInBlack,
                Loc.T(LocalizedStrings.Pictomancer.EnableCometInBlackDesc, "Use Comet in Black"), save))
            {
                config.Pictomancer.EnableCometInBlack = enableCometInBlack;
            }

            var enableStarPrism = config.Pictomancer.EnableStarPrism;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Pictomancer.EnableStarPrism, "Enable Star Prism"), ref enableStarPrism,
                Loc.T(LocalizedStrings.Pictomancer.EnableStarPrismDesc, "Use Star Prism"), save))
            {
                config.Pictomancer.EnableStarPrism = enableStarPrism;
            }

            ConfigUIHelpers.Spacing();

            config.Pictomancer.AoEMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Pictomancer.AoEMinTargets, "AoE Min Targets"),
                config.Pictomancer.AoEMinTargets, 2, 8,
                Loc.T(LocalizedStrings.Pictomancer.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawCanvasSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Pictomancer.CanvasSection, "Canvas Motifs"), "PCT"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableCreatureMotif = config.Pictomancer.EnableCreatureMotif;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Pictomancer.EnableCreatureMotif, "Enable Creature Motif"), ref enableCreatureMotif,
                Loc.T(LocalizedStrings.Pictomancer.EnableCreatureMotifDesc, "Use Creature Motif abilities"), save))
            {
                config.Pictomancer.EnableCreatureMotif = enableCreatureMotif;
            }

            var enableWeaponMotif = config.Pictomancer.EnableWeaponMotif;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Pictomancer.EnableWeaponMotif, "Enable Weapon Motif"), ref enableWeaponMotif,
                Loc.T(LocalizedStrings.Pictomancer.EnableWeaponMotifDesc, "Use Hammer Motif abilities"), save))
            {
                config.Pictomancer.EnableWeaponMotif = enableWeaponMotif;
            }

            var enableLandscapeMotif = config.Pictomancer.EnableLandscapeMotif;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Pictomancer.EnableLandscapeMotif, "Enable Landscape Motif"), ref enableLandscapeMotif,
                Loc.T(LocalizedStrings.Pictomancer.EnableLandscapeMotifDesc, "Use Starry Sky Motif abilities"), save))
            {
                config.Pictomancer.EnableLandscapeMotif = enableLandscapeMotif;
            }

            ConfigUIHelpers.Spacing();

            var prepaintMotifs = config.Pictomancer.PrepaintMotifs;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Pictomancer.PrepaintMotifs, "Pre-paint Motifs"), ref prepaintMotifs,
                Loc.T(LocalizedStrings.Pictomancer.PrepaintMotifsDesc, "Paint motifs out of combat"), save))
            {
                config.Pictomancer.PrepaintMotifs = prepaintMotifs;
            }

            var prepaintOption = config.Pictomancer.PrepaintOption;
            if (ConfigUIHelpers.EnumCombo(Loc.T(LocalizedStrings.Pictomancer.PrepaintOption, "Pre-paint Option"), ref prepaintOption,
                Loc.T(LocalizedStrings.Pictomancer.PrepaintOptionDesc, "Which motifs to pre-paint"), save))
            {
                config.Pictomancer.PrepaintOption = prepaintOption;
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawMuseSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Pictomancer.MuseSection, "Muse Abilities"), "PCT"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableLivingMuse = config.Pictomancer.EnableLivingMuse;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Pictomancer.EnableLivingMuse, "Enable Living Muse"), ref enableLivingMuse,
                Loc.T(LocalizedStrings.Pictomancer.EnableLivingMuseDesc, "Use Living Muse"), save))
            {
                config.Pictomancer.EnableLivingMuse = enableLivingMuse;
            }

            var enableSteelMuse = config.Pictomancer.EnableSteelMuse;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Pictomancer.EnableSteelMuse, "Enable Steel Muse"), ref enableSteelMuse,
                Loc.T(LocalizedStrings.Pictomancer.EnableSteelMuseDesc, "Use Steel Muse"), save))
            {
                config.Pictomancer.EnableSteelMuse = enableSteelMuse;
            }

            var enableScenicMuse = config.Pictomancer.EnableScenicMuse;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Pictomancer.EnableScenicMuse, "Enable Scenic Muse"), ref enableScenicMuse,
                Loc.T(LocalizedStrings.Pictomancer.EnableScenicMuseDesc, "Use Scenic Muse"), save))
            {
                config.Pictomancer.EnableScenicMuse = enableScenicMuse;
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Pictomancer.BurstSection, "Burst Windows"), "PCT", false))
        {
            ConfigUIHelpers.BeginIndent();

            var enableStarryMuse = config.Pictomancer.EnableStarryMuse;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Pictomancer.EnableStarryMuse, "Enable Starry Muse"), ref enableStarryMuse,
                Loc.T(LocalizedStrings.Pictomancer.EnableStarryMuseDesc, "Use Starry Muse (party buff)"), save))
            {
                config.Pictomancer.EnableStarryMuse = enableStarryMuse;
            }

            var alignWithParty = config.Pictomancer.AlignStarryMuseWithParty;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Pictomancer.AlignWithParty, "Align with Party"), ref alignWithParty,
                Loc.T(LocalizedStrings.Pictomancer.AlignWithPartyDesc, "Coordinate Starry Muse with party burst"), save))
            {
                config.Pictomancer.AlignStarryMuseWithParty = alignWithParty;
            }

            config.Pictomancer.StarryMuseHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Pictomancer.StarryMuseHoldTime, "Starry Muse Hold Time"),
                config.Pictomancer.StarryMuseHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Pictomancer.StarryMuseHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
