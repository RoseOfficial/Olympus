using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Viper (Echidna) settings section.
/// </summary>
public sealed class ViperSection
{
    private readonly Configuration config;
    private readonly Action save;

    public ViperSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Viper", "Echidna", ConfigUIHelpers.ViperColor);

        DrawDamageSection();
        DrawReawakenSection();
        DrawBurstSection();
        DrawPositionalSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Viper.DamageSection, "Damage"), "VPR"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Viper.EnableTwinbladeCombo, "Enable Twinblade Combo"),
                () => config.Viper.EnableTwinbladeCombo,
                v => config.Viper.EnableTwinbladeCombo = v,
                Loc.T(LocalizedStrings.Viper.EnableTwinbladeComboDesc, "Use Twinblade combo actions"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Viper.EnableUncoiledFury, "Enable Uncoiled Fury"),
                () => config.Viper.EnableUncoiledFury,
                v => config.Viper.EnableUncoiledFury = v,
                null, save,
                actionId: VPRActions.UncoiledFury.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Viper.EnableAoERotation, "Enable AoE Rotation"),
                () => config.Viper.EnableAoERotation,
                v => config.Viper.EnableAoERotation = v,
                Loc.T(LocalizedStrings.Viper.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.Viper.EnableAoERotation)
            {
                config.Viper.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.Viper.AoEMinTargets, "AoE Min Targets"),
                    config.Viper.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Viper.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawReawakenSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Viper.ReawakenSection, "Reawaken"), "VPR"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Viper.EnableReawaken, "Enable Reawaken"),
                () => config.Viper.EnableReawaken,
                v => config.Viper.EnableReawaken = v,
                null, save,
                actionId: VPRActions.Reawaken.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Viper.EnableOuroboros, "Enable Ouroboros"),
                () => config.Viper.EnableOuroboros,
                v => config.Viper.EnableOuroboros = v,
                null, save,
                actionId: VPRActions.Ouroboros.ActionId);

            config.Viper.AnguineMinStacks = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Viper.AnguineMinStacks, "Anguine Min Stacks"),
                config.Viper.AnguineMinStacks, 1, 5,
                Loc.T(LocalizedStrings.Viper.AnguineMinStacksDesc, "Minimum Anguine Tribute for Reawaken"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Viper.SaveAnguineForBurst, "Save Anguine for Burst"),
                () => config.Viper.SaveAnguineForBurst,
                v => config.Viper.SaveAnguineForBurst = v,
                Loc.T(LocalizedStrings.Viper.SaveAnguineForBurstDesc, "Hold Anguine Tribute for burst windows"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Viper.BurstSection, "Burst Windows"), "VPR"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Viper.EnableSerpentsIre, "Enable Serpent's Ire"),
                () => config.Viper.EnableSerpentsIre,
                v => config.Viper.EnableSerpentsIre = v,
                null, save,
                actionId: VPRActions.SerpentsIre.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Viper.AlignWithParty, "Align with Party"),
                () => config.Viper.AlignSerpentsIreWithParty,
                v => config.Viper.AlignSerpentsIreWithParty = v,
                Loc.T(LocalizedStrings.Viper.AlignWithPartyDesc, "Coordinate Serpent's Ire with party burst"), save);

            config.Viper.SerpentsIreHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Viper.SerpentsIreHoldTime, "Serpent's Ire Hold Time"),
                config.Viper.SerpentsIreHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Viper.SerpentsIreHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawPositionalSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Viper.PositionalSection, "Positionals"), "VPR", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Viper.EnforcePositionals, "Enforce Positionals"),
                () => config.Viper.EnforcePositionals,
                v => config.Viper.EnforcePositionals = v,
                Loc.T(LocalizedStrings.Viper.EnforcePositionalsDesc, "Only use positional actions when in correct position"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Viper.OptimizeVenomPositionals, "Optimize Venom Positionals"),
                () => config.Viper.OptimizeVenomPositionals,
                v => config.Viper.OptimizeVenomPositionals = v,
                Loc.T(LocalizedStrings.Viper.OptimizeVenomPositionalsDesc, "Prioritize venom based on position"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
