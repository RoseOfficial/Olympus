using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
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
        DrawUtilitySection();
        DrawRoleActionsSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Pictomancer.DamageSection, "Damage"), "PCT"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableSubtractiveCombo, "Enable Subtractive Combo"),
                () => config.Pictomancer.EnableSubtractiveCombo,
                v => config.Pictomancer.EnableSubtractiveCombo = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableSubtractiveComboDesc, "Use Fire in Red / Aero in Green / Water in Blue combo spells"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableRainbowDrip, "Enable Rainbow Drip"),
                () => config.Pictomancer.EnableRainbowDrip,
                v => config.Pictomancer.EnableRainbowDrip = v,
                null, save,
                actionId: PCTActions.RainbowDrip.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableHolyInWhite, "Enable Holy in White"),
                () => config.Pictomancer.EnableHolyInWhite,
                v => config.Pictomancer.EnableHolyInWhite = v,
                null, save, actionId: PCTActions.HolyInWhite.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableCometInBlack, "Enable Comet in Black"),
                () => config.Pictomancer.EnableCometInBlack,
                v => config.Pictomancer.EnableCometInBlack = v,
                null, save, actionId: PCTActions.CometInBlack.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableStarPrism, "Enable Star Prism"),
                () => config.Pictomancer.EnableStarPrism,
                v => config.Pictomancer.EnableStarPrism = v,
                null, save, actionId: PCTActions.StarPrism.ActionId);

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
                    Loc.T(LocalizedStrings.Pictomancer.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save, v => config.Pictomancer.AoEMinTargets = v);
            }

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Pictomancer.PaletteSection, "Palette:"));

            config.Pictomancer.HolyMinPalette = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Pictomancer.HolyMinPalette, "Holy in White Min Palette"),
                config.Pictomancer.HolyMinPalette, 25, 100,
                Loc.T(LocalizedStrings.Pictomancer.HolyMinPaletteDesc, "Minimum Palette gauge to spend on Holy in White"), save, v => config.Pictomancer.HolyMinPalette = v);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.SavePaletteForComet, "Save Palette for Comet"),
                () => config.Pictomancer.SavePaletteForComet,
                v => config.Pictomancer.SavePaletteForComet = v,
                Loc.T(LocalizedStrings.Pictomancer.SavePaletteForCometDesc, "Reserve Palette for Comet in Black when Subtractive is active"), save);

            ConfigUIHelpers.Spacing();

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

            if (config.Pictomancer.EnableCreatureMotif)
            {
                ConfigUIHelpers.BeginIndent();

                ConfigUIHelpers.Toggle(
                    Loc.T(LocalizedStrings.Pictomancer.EnablePomMotif, "Enable Pom Motif"),
                    () => config.Pictomancer.EnablePomMotif,
                    v => config.Pictomancer.EnablePomMotif = v,
                    null, save, actionId: PCTActions.PomMotif.ActionId);

                ConfigUIHelpers.Toggle(
                    Loc.T(LocalizedStrings.Pictomancer.EnableWingMotif, "Enable Wing Motif"),
                    () => config.Pictomancer.EnableWingMotif,
                    v => config.Pictomancer.EnableWingMotif = v,
                    null, save, actionId: PCTActions.WingMotif.ActionId);

                ConfigUIHelpers.Toggle(
                    Loc.T(LocalizedStrings.Pictomancer.EnableClawMotif, "Enable Claw Motif"),
                    () => config.Pictomancer.EnableClawMotif,
                    v => config.Pictomancer.EnableClawMotif = v,
                    null, save, actionId: PCTActions.ClawMotif.ActionId);

                ConfigUIHelpers.Toggle(
                    Loc.T(LocalizedStrings.Pictomancer.EnableMawMotif, "Enable Maw Motif"),
                    () => config.Pictomancer.EnableMawMotif,
                    v => config.Pictomancer.EnableMawMotif = v,
                    null, save, actionId: PCTActions.MawMotif.ActionId);

                ConfigUIHelpers.EndIndent();
            }

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableWeaponMotif, "Enable Weapon Motif"),
                () => config.Pictomancer.EnableWeaponMotif,
                v => config.Pictomancer.EnableWeaponMotif = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableWeaponMotifDesc, "Use Hammer Motif abilities"), save);

            if (config.Pictomancer.EnableWeaponMotif)
            {
                ConfigUIHelpers.BeginIndent();

                ConfigUIHelpers.Toggle(
                    Loc.T(LocalizedStrings.Pictomancer.EnableHammerMotif, "Enable Hammer Motif"),
                    () => config.Pictomancer.EnableHammerMotif,
                    v => config.Pictomancer.EnableHammerMotif = v,
                    null, save, actionId: PCTActions.HammerMotif.ActionId);

                ConfigUIHelpers.EndIndent();
            }

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableLandscapeMotif, "Enable Landscape Motif"),
                () => config.Pictomancer.EnableLandscapeMotif,
                v => config.Pictomancer.EnableLandscapeMotif = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableLandscapeMotifDesc, "Use Starry Sky Motif abilities"), save);

            if (config.Pictomancer.EnableLandscapeMotif)
            {
                ConfigUIHelpers.BeginIndent();

                ConfigUIHelpers.Toggle(
                    Loc.T(LocalizedStrings.Pictomancer.EnableStarrySkyMotif, "Enable Starry Sky Motif"),
                    () => config.Pictomancer.EnableStarrySkyMotif,
                    v => config.Pictomancer.EnableStarrySkyMotif = v,
                    null, save, actionId: PCTActions.StarrySkyMotif.ActionId);

                ConfigUIHelpers.EndIndent();
            }

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

            var creatureOrder = config.Pictomancer.CreatureMotifOrder;
            if (ConfigUIHelpers.EnumCombo(
                Loc.T(LocalizedStrings.Pictomancer.CreatureMotifOrder, "Creature Motif Order"),
                ref creatureOrder,
                Loc.T(LocalizedStrings.Pictomancer.CreatureMotifOrderDesc, "Order to cycle through creature motifs"), save))
            {
                config.Pictomancer.CreatureMotifOrder = creatureOrder;
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
                null, save, actionId: PCTActions.LivingMuse.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableSteelMuse, "Enable Steel Muse"),
                () => config.Pictomancer.EnableSteelMuse,
                v => config.Pictomancer.EnableSteelMuse = v,
                null, save, actionId: PCTActions.SteelMuse.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnablePortraits, "Enable Portraits"),
                () => config.Pictomancer.EnablePortraits,
                v => config.Pictomancer.EnablePortraits = v,
                Loc.T(LocalizedStrings.Pictomancer.EnablePortraitsDesc, "Use Mog of the Ages / Retribution of the Madeen"), save);

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
                null, save, actionId: PCTActions.StarryMuse.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableSubtractivePalette, "Enable Subtractive Palette"),
                () => config.Pictomancer.EnableSubtractivePalette,
                v => config.Pictomancer.EnableSubtractivePalette = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableSubtractivePaletteDesc, "Use Subtractive Palette to spend Palette gauge during burst"),
                save, actionId: PCTActions.SubtractivePalette.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableBurstPooling, "Enable Burst Pooling"),
                () => config.Pictomancer.EnableBurstPooling,
                v => config.Pictomancer.EnableBurstPooling = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableBurstPoolingDesc, "Hold Hammer Time and paint resources for party burst windows"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.UseHammerDuringBurst, "Use Hammer During Burst"),
                () => config.Pictomancer.UseHammerDuringBurst,
                v => config.Pictomancer.UseHammerDuringBurst = v,
                Loc.T(LocalizedStrings.Pictomancer.UseHammerDuringBurstDesc, "Prioritize Hammer Stamp combo inside Starry Muse windows"), save);

            config.Pictomancer.StarryMuseHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Pictomancer.StarryMuseHoldTime, "Starry Muse Hold Time"),
                config.Pictomancer.StarryMuseHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Pictomancer.StarryMuseHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save, v => config.Pictomancer.StarryMuseHoldTime = v);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawUtilitySection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Pictomancer.UtilitySection, "Utility"), "PCT", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableTemperaCoat, "Enable Tempera Coat"),
                () => config.Pictomancer.EnableTemperaCoat,
                v => config.Pictomancer.EnableTemperaCoat = v,
                null, save,
                actionId: PCTActions.TemperaCoat.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableTemperaGrassa, "Enable Tempera Grassa"),
                () => config.Pictomancer.EnableTemperaGrassa,
                v => config.Pictomancer.EnableTemperaGrassa = v,
                null, save,
                actionId: PCTActions.TemperaGrassa.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableSmudge, "Enable Smudge"),
                () => config.Pictomancer.EnableSmudge,
                v => config.Pictomancer.EnableSmudge = v,
                Loc.T(LocalizedStrings.Pictomancer.EnableSmudgeDesc, "Use Smudge for movement"),
                save, actionId: PCTActions.Smudge.ActionId);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawRoleActionsSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Pictomancer.RoleActionsSection, "Role Actions"), "PCT", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Pictomancer.EnableAddle, "Enable Addle"),
                () => config.Pictomancer.EnableAddle,
                v => config.Pictomancer.EnableAddle = v,
                null, save,
                actionId: RoleActions.Addle.ActionId);

            ConfigUIHelpers.EndIndent();
        }
    }
}
