using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Monk.EnableMasterfulBlitz, "Enable Masterful Blitz"),
                () => config.Monk.EnableMasterfulBlitz,
                v => config.Monk.EnableMasterfulBlitz = v,
                Loc.T(LocalizedStrings.Monk.EnableMasterfulBlitzDesc, "Use Beast Chakra combos"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Monk.EnableSixSidedStar, "Enable Six-Sided Star"),
                () => config.Monk.EnableSixSidedStar,
                v => config.Monk.EnableSixSidedStar = v,
                null, save,
                actionId: MNKActions.SixSidedStar.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Monk.EnableAoERotation, "Enable AoE Rotation"),
                () => config.Monk.EnableAoERotation,
                v => config.Monk.EnableAoERotation = v,
                Loc.T(LocalizedStrings.Monk.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.Monk.EnableAoERotation)
            {
                config.Monk.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.Monk.AoEMinTargets, "AoE Min Targets"),
                    config.Monk.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Monk.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawChakraSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Monk.ChakraSection, "Chakra"), "MNK"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Monk.EnableChakraSpenders, "Enable Chakra Spenders"),
                () => config.Monk.EnableChakraSpenders,
                v => config.Monk.EnableChakraSpenders = v,
                Loc.T(LocalizedStrings.Monk.EnableChakraSpendersDesc, "Use The Forbidden Chakra/Enlightenment"), save);

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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Monk.EnableBurstPooling, "Enable Burst Pooling"),
                () => config.Monk.EnableBurstPooling,
                v => config.Monk.EnableBurstPooling = v,
                Loc.T(LocalizedStrings.Monk.EnableBurstPoolingDesc, "Hold Brotherhood for burst windows."), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Monk.EnableRiddleOfFire, "Enable Riddle of Fire"),
                () => config.Monk.EnableRiddleOfFire,
                v => config.Monk.EnableRiddleOfFire = v,
                null, save,
                actionId: MNKActions.RiddleOfFire.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Monk.EnableBrotherhood, "Enable Brotherhood"),
                () => config.Monk.EnableBrotherhood,
                v => config.Monk.EnableBrotherhood = v,
                null, save,
                actionId: MNKActions.Brotherhood.ActionId);

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

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Monk.AllowPositionalLoss, "Allow Positional Loss"),
                () => config.Monk.AllowPositionalLoss,
                v => config.Monk.AllowPositionalLoss = v,
                Loc.T(LocalizedStrings.Monk.AllowPositionalLossDesc, "Continue rotation even if positionals will miss"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
