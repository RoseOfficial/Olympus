using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
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

            var enableTwinbladeCombo = config.Viper.EnableTwinbladeCombo;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Viper.EnableTwinbladeCombo, "Enable Twinblade Combo"), ref enableTwinbladeCombo,
                Loc.T(LocalizedStrings.Viper.EnableTwinbladeComboDesc, "Use Twinblade combo actions"), save))
            {
                config.Viper.EnableTwinbladeCombo = enableTwinbladeCombo;
            }

            var enableUncoiledFury = config.Viper.EnableUncoiledFury;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Viper.EnableUncoiledFury, "Enable Uncoiled Fury"), ref enableUncoiledFury,
                Loc.T(LocalizedStrings.Viper.EnableUncoiledFuryDesc, "Use Uncoiled Fury"), save))
            {
                config.Viper.EnableUncoiledFury = enableUncoiledFury;
            }

            ConfigUIHelpers.Spacing();

            config.Viper.AoEMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Viper.AoEMinTargets, "AoE Min Targets"),
                config.Viper.AoEMinTargets, 2, 8,
                Loc.T(LocalizedStrings.Viper.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawReawakenSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Viper.ReawakenSection, "Reawaken"), "VPR"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableReawaken = config.Viper.EnableReawaken;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Viper.EnableReawaken, "Enable Reawaken"), ref enableReawaken,
                Loc.T(LocalizedStrings.Viper.EnableReawakenDesc, "Use Reawaken burst sequence"), save))
            {
                config.Viper.EnableReawaken = enableReawaken;
            }

            var enableOuroboros = config.Viper.EnableOuroboros;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Viper.EnableOuroboros, "Enable Ouroboros"), ref enableOuroboros,
                Loc.T(LocalizedStrings.Viper.EnableOuroborosDesc, "Use Ouroboros finisher"), save))
            {
                config.Viper.EnableOuroboros = enableOuroboros;
            }

            config.Viper.AnguineMinStacks = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Viper.AnguineMinStacks, "Anguine Min Stacks"),
                config.Viper.AnguineMinStacks, 1, 5,
                Loc.T(LocalizedStrings.Viper.AnguineMinStacksDesc, "Minimum Anguine Tribute for Reawaken"), save);

            var saveAnguineForBurst = config.Viper.SaveAnguineForBurst;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Viper.SaveAnguineForBurst, "Save Anguine for Burst"), ref saveAnguineForBurst,
                Loc.T(LocalizedStrings.Viper.SaveAnguineForBurstDesc, "Hold Anguine Tribute for burst windows"), save))
            {
                config.Viper.SaveAnguineForBurst = saveAnguineForBurst;
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Viper.BurstSection, "Burst Windows"), "VPR"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableSerpentsIre = config.Viper.EnableSerpentsIre;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Viper.EnableSerpentsIre, "Enable Serpent's Ire"), ref enableSerpentsIre,
                Loc.T(LocalizedStrings.Viper.EnableSerpentsIreDesc, "Use Serpent's Ire"), save))
            {
                config.Viper.EnableSerpentsIre = enableSerpentsIre;
            }

            var alignWithParty = config.Viper.AlignSerpentsIreWithParty;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Viper.AlignWithParty, "Align with Party"), ref alignWithParty,
                Loc.T(LocalizedStrings.Viper.AlignWithPartyDesc, "Coordinate Serpent's Ire with party burst"), save))
            {
                config.Viper.AlignSerpentsIreWithParty = alignWithParty;
            }

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

            var enforcePositionals = config.Viper.EnforcePositionals;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Viper.EnforcePositionals, "Enforce Positionals"), ref enforcePositionals,
                Loc.T(LocalizedStrings.Viper.EnforcePositionalsDesc, "Only use positional actions when in correct position"), save))
            {
                config.Viper.EnforcePositionals = enforcePositionals;
            }

            var optimizeVenomPositionals = config.Viper.OptimizeVenomPositionals;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Viper.OptimizeVenomPositionals, "Optimize Venom Positionals"), ref optimizeVenomPositionals,
                Loc.T(LocalizedStrings.Viper.OptimizeVenomPositionalsDesc, "Prioritize venom based on position"), save))
            {
                config.Viper.OptimizeVenomPositionals = optimizeVenomPositionals;
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
