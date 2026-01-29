using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Monk (Kratos) settings section.
/// </summary>
public sealed class MonkSection
{
    private readonly Configuration config;
    private readonly Action save;

    public MonkSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Monk", "Kratos", ConfigUIHelpers.MonkColor);

        DrawDamageSection();
        DrawChakraSection();
        DrawBuffSection();
        DrawPositionalSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Monk.DamageSection, "Damage"), "MNK"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableMasterfulBlitz = config.Monk.EnableMasterfulBlitz;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Monk.EnableMasterfulBlitz, "Enable Masterful Blitz"), ref enableMasterfulBlitz,
                Loc.T(LocalizedStrings.Monk.EnableMasterfulBlitzDesc, "Use Beast Chakra combos"), save))
            {
                config.Monk.EnableMasterfulBlitz = enableMasterfulBlitz;
            }

            var enableSixSidedStar = config.Monk.EnableSixSidedStar;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Monk.EnableSixSidedStar, "Enable Six-Sided Star"), ref enableSixSidedStar,
                Loc.T(LocalizedStrings.Monk.EnableSixSidedStarDesc, "Use Six-Sided Star for downtime"), save))
            {
                config.Monk.EnableSixSidedStar = enableSixSidedStar;
            }

            ConfigUIHelpers.Spacing();

            config.Monk.AoEMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Monk.AoEMinTargets, "AoE Min Targets"),
                config.Monk.AoEMinTargets, 2, 8,
                Loc.T(LocalizedStrings.Monk.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawChakraSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Monk.ChakraSection, "Chakra"), "MNK"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableChakraSpenders = config.Monk.EnableChakraSpenders;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Monk.EnableChakraSpenders, "Enable Chakra Spenders"), ref enableChakraSpenders,
                Loc.T(LocalizedStrings.Monk.EnableChakraSpendersDesc, "Use The Forbidden Chakra/Enlightenment"), save))
            {
                config.Monk.EnableChakraSpenders = enableChakraSpenders;
            }

            config.Monk.ChakraMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Monk.ChakraMinGauge, "Chakra Min Stacks"),
                config.Monk.ChakraMinGauge, 1, 5,
                Loc.T(LocalizedStrings.Monk.ChakraMinGaugeDesc, "Minimum Chakra stacks to use spenders"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBuffSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Monk.BuffSection, "Buffs"), "MNK"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableRiddleOfFire = config.Monk.EnableRiddleOfFire;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Monk.EnableRiddleOfFire, "Enable Riddle of Fire"), ref enableRiddleOfFire,
                Loc.T(LocalizedStrings.Monk.EnableRiddleOfFireDesc, "Use Riddle of Fire for damage buff"), save))
            {
                config.Monk.EnableRiddleOfFire = enableRiddleOfFire;
            }

            var enableBrotherhood = config.Monk.EnableBrotherhood;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Monk.EnableBrotherhood, "Enable Brotherhood"), ref enableBrotherhood,
                Loc.T(LocalizedStrings.Monk.EnableBrotherhoodDesc, "Use Brotherhood (party buff)"), save))
            {
                config.Monk.EnableBrotherhood = enableBrotherhood;
            }

            var alignWithParty = config.Monk.AlignBrotherhoodWithParty;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Monk.AlignWithParty, "Align Brotherhood with Party"), ref alignWithParty,
                Loc.T(LocalizedStrings.Monk.AlignWithPartyDesc, "Coordinate with party burst windows"), save))
            {
                config.Monk.AlignBrotherhoodWithParty = alignWithParty;
            }

            config.Monk.BrotherhoodHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Monk.BrotherhoodHoldTime, "Brotherhood Hold Time"),
                config.Monk.BrotherhoodHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Monk.BrotherhoodHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawPositionalSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Monk.PositionalSection, "Positionals"), "MNK", false))
        {
            ConfigUIHelpers.BeginIndent();

            var strictness = config.Monk.PositionalStrictness;
            if (ConfigUIHelpers.EnumCombo(Loc.T(LocalizedStrings.Monk.PositionalStrictness, "Positional Strictness"), ref strictness,
                Loc.T(LocalizedStrings.Monk.PositionalStrictnessDesc, "How strictly to enforce positionals"), save))
            {
                config.Monk.PositionalStrictness = strictness;
            }

            var allowPositionalLoss = config.Monk.AllowPositionalLoss;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Monk.AllowPositionalLoss, "Allow Positional Loss"), ref allowPositionalLoss,
                Loc.T(LocalizedStrings.Monk.AllowPositionalLossDesc, "Continue rotation even if positionals will miss"), save))
            {
                config.Monk.AllowPositionalLoss = allowPositionalLoss;
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
