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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableHolyInWhite, "Enable Holy in White"),
                () => config.Pictomancer.EnableHolyInWhite,
                v => config.Pictomancer.EnableHolyInWhite = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableHolyInWhiteDesc, "Use Holy in White"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableCometInBlack, "Enable Comet in Black"),
                () => config.Pictomancer.EnableCometInBlack,
                v => config.Pictomancer.EnableCometInBlack = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableCometInBlackDesc, "Use Comet in Black"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableStarPrism, "Enable Star Prism"),
                () => config.Pictomancer.EnableStarPrism,
                v => config.Pictomancer.EnableStarPrism = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableStarPrismDesc, "Use Star Prism"), save);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableAoERotation, "Enable AoE Rotation"),
                () => config.Pictomancer.EnableAoERotation,
                v => config.Pictomancer.EnableAoERotation = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.Pictomancer.EnableAoERotation)
            {
                config.Pictomancer.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.Pictomancer.AoEMinTargets, "AoE Min Targets"),
                    config.Pictomancer.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Pictomancer.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawCanvasSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Pictomancer.CanvasSection, "Canvas Motifs"), "PCT"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableCreatureMotif, "Enable Creature Motif"),
                () => config.Pictomancer.EnableCreatureMotif,
                v => config.Pictomancer.EnableCreatureMotif = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableCreatureMotifDesc, "Use Creature Motif abilities"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableWeaponMotif, "Enable Weapon Motif"),
                () => config.Pictomancer.EnableWeaponMotif,
                v => config.Pictomancer.EnableWeaponMotif = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableWeaponMotifDesc, "Use Hammer Motif abilities"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableLandscapeMotif, "Enable Landscape Motif"),
                () => config.Pictomancer.EnableLandscapeMotif,
                v => config.Pictomancer.EnableLandscapeMotif = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableLandscapeMotifDesc, "Use Starry Sky Motif abilities"), save);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.PrepaintMotifs, "Pre-paint Motifs"),
                () => config.Pictomancer.PrepaintMotifs,
                v => config.Pictomancer.PrepaintMotifs = v,
                Loc.T(LocalizedStrings.Pictomancer.PrepaintMotifsDesc, "Paint motifs out of combat"), save);

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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableLivingMuse, "Enable Living Muse"),
                () => config.Pictomancer.EnableLivingMuse,
                v => config.Pictomancer.EnableLivingMuse = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableLivingMuseDesc, "Use Living Muse"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableSteelMuse, "Enable Steel Muse"),
                () => config.Pictomancer.EnableSteelMuse,
                v => config.Pictomancer.EnableSteelMuse = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableSteelMuseDesc, "Use Steel Muse"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableScenicMuse, "Enable Scenic Muse"),
                () => config.Pictomancer.EnableScenicMuse,
                v => config.Pictomancer.EnableScenicMuse = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableScenicMuseDesc, "Use Scenic Muse"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Pictomancer.BurstSection, "Burst Windows"), "PCT", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableStarryMuse, "Enable Starry Muse"),
                () => config.Pictomancer.EnableStarryMuse,
                v => config.Pictomancer.EnableStarryMuse = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableStarryMuseDesc, "Use Starry Muse (party buff)"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.AlignWithParty, "Align with Party"),
                () => config.Pictomancer.AlignStarryMuseWithParty,
                v => config.Pictomancer.AlignStarryMuseWithParty = v,
                Loc.T(LocalizedStrings.Pictomancer.AlignWithPartyDesc, "Coordinate Starry Muse with party burst"), save);

            config.Pictomancer.StarryMuseHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Pictomancer.StarryMuseHoldTime, "Starry Muse Hold Time"),
                config.Pictomancer.StarryMuseHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Pictomancer.StarryMuseHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
