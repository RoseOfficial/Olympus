using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Black Mage (Hecate) settings section.
/// </summary>
public sealed class BlackMageSection
{
    private readonly Configuration config;
    private readonly Action save;

    public BlackMageSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Black Mage", "Hecate", ConfigUIHelpers.BlackMageColor);

        DrawDamageSection();
        DrawPhaseSection();
        DrawMovementSection();
        DrawThunderSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.BlackMage.DamageSection, "Damage"), "BLM"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableXenoglossy = config.BlackMage.EnableXenoglossy;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.BlackMage.EnableXenoglossy, "Enable Xenoglossy"), ref enableXenoglossy,
                Loc.T(LocalizedStrings.BlackMage.EnableXenoglossyDesc, "Use Xenoglossy (Polyglot spender)"), save))
            {
                config.BlackMage.EnableXenoglossy = enableXenoglossy;
            }

            var enableDespair = config.BlackMage.EnableDespair;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.BlackMage.EnableDespair, "Enable Despair"), ref enableDespair,
                Loc.T(LocalizedStrings.BlackMage.EnableDespairDesc, "Use Despair"), save))
            {
                config.BlackMage.EnableDespair = enableDespair;
            }

            var enableFlareStar = config.BlackMage.EnableFlareStar;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.BlackMage.EnableFlareStar, "Enable Flare Star"), ref enableFlareStar,
                Loc.T(LocalizedStrings.BlackMage.EnableFlareStarDesc, "Use Flare Star"), save))
            {
                config.BlackMage.EnableFlareStar = enableFlareStar;
            }

            ConfigUIHelpers.Spacing();

            config.BlackMage.AoEMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.BlackMage.AoEMinTargets, "AoE Min Targets"),
                config.BlackMage.AoEMinTargets, 2, 8,
                Loc.T(LocalizedStrings.BlackMage.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawPhaseSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.BlackMage.PhaseSection, "Fire/Ice Phases"), "BLM"))
        {
            ConfigUIHelpers.BeginIndent();

            config.BlackMage.FireIVsBeforeDespair = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.BlackMage.FireIVsBeforeDespair, "Fire IVs Before Despair"),
                config.BlackMage.FireIVsBeforeDespair, 2, 6,
                Loc.T(LocalizedStrings.BlackMage.FireIVsBeforeDespairDesc, "Number of Fire IV casts before Despair"), save);

            config.BlackMage.FireIVMinMp = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.BlackMage.FireIVMinMp, "Fire IV Min MP"),
                config.BlackMage.FireIVMinMp, 400, 2000,
                Loc.T(LocalizedStrings.BlackMage.FireIVMinMpDesc, "Minimum MP to cast Fire IV"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawMovementSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.BlackMage.MovementSection, "Movement"), "BLM"))
        {
            ConfigUIHelpers.BeginIndent();

            var movementPriority = config.BlackMage.MovementPriority;
            if (ConfigUIHelpers.EnumCombo(Loc.T(LocalizedStrings.BlackMage.MovementPriority, "Movement Priority"), ref movementPriority,
                Loc.T(LocalizedStrings.BlackMage.MovementPriorityDesc, "Preferred instant cast for movement"), save))
            {
                config.BlackMage.MovementPriority = movementPriority;
            }

            var savePolyglotForMovement = config.BlackMage.SavePolyglotForMovement;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.BlackMage.SavePolyglotForMovement, "Save Polyglot for Movement"), ref savePolyglotForMovement,
                Loc.T(LocalizedStrings.BlackMage.SavePolyglotForMovementDesc, "Reserve Polyglot stacks for movement"), save))
            {
                config.BlackMage.SavePolyglotForMovement = savePolyglotForMovement;
            }

            config.BlackMage.PolyglotMovementReserve = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.BlackMage.PolyglotMovementReserve, "Polyglot Reserve"),
                config.BlackMage.PolyglotMovementReserve, 0, 2,
                Loc.T(LocalizedStrings.BlackMage.PolyglotMovementReserveDesc, "Polyglot stacks to reserve for movement"), save);

            var enableLeyLines = config.BlackMage.EnableLeyLines;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.BlackMage.EnableLeyLines, "Enable Ley Lines"), ref enableLeyLines,
                Loc.T(LocalizedStrings.BlackMage.EnableLeyLinesDesc, "Use Ley Lines"), save))
            {
                config.BlackMage.EnableLeyLines = enableLeyLines;
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawThunderSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.BlackMage.ThunderSection, "Thunder DoT"), "BLM", false))
        {
            ConfigUIHelpers.BeginIndent();

            var maintainThunder = config.BlackMage.MaintainThunder;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.BlackMage.MaintainThunder, "Maintain Thunder"), ref maintainThunder,
                Loc.T(LocalizedStrings.BlackMage.MaintainThunderDesc, "Keep Thunder DoT active"), save))
            {
                config.BlackMage.MaintainThunder = maintainThunder;
            }

            config.BlackMage.ThunderRefreshThreshold = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.BlackMage.ThunderRefreshThreshold, "Thunder Refresh"),
                config.BlackMage.ThunderRefreshThreshold, 0f, 15f, "%.0f s",
                Loc.T(LocalizedStrings.BlackMage.ThunderRefreshThresholdDesc, "Seconds remaining before refreshing"), save);

            var useThunderheadImmediately = config.BlackMage.UseThunderheadImmediately;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.BlackMage.UseThunderheadImmediately, "Use Thunderhead Immediately"), ref useThunderheadImmediately,
                Loc.T(LocalizedStrings.BlackMage.UseThunderheadImmediatelyDesc, "Use Thunderhead procs immediately"), save))
            {
                config.BlackMage.UseThunderheadImmediately = useThunderheadImmediately;
            }

            ConfigUIHelpers.EndIndent();
        }
    }
}
