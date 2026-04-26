using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
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
        DrawRoleActionsSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.BlackMage.DamageSection, "Damage"), "BLM"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.BlackMage.EnableXenoglossy, "Enable Xenoglossy"),
                () => config.BlackMage.EnableXenoglossy,
                v => config.BlackMage.EnableXenoglossy = v,
                null, save, actionId: BLMActions.Xenoglossy.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.BlackMage.EnableDespair, "Enable Despair"),
                () => config.BlackMage.EnableDespair,
                v => config.BlackMage.EnableDespair = v,
                null, save, actionId: BLMActions.Despair.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.BlackMage.EnableFlareStar, "Enable Flare Star"),
                () => config.BlackMage.EnableFlareStar,
                v => config.BlackMage.EnableFlareStar = v,
                null, save, actionId: BLMActions.FlareStar.ActionId);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.BlackMage.EnableAoERotation, "Enable AoE Rotation"),
                () => config.BlackMage.EnableAoERotation,
                v => config.BlackMage.EnableAoERotation = v,
                Loc.T(LocalizedStrings.BlackMage.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.BlackMage.EnableAoERotation)
            {
                config.BlackMage.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.BlackMage.AoEMinTargets, "AoE Min Targets"),
                    config.BlackMage.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.BlackMage.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save, v => config.BlackMage.AoEMinTargets = v);
            }

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
                Loc.T(LocalizedStrings.BlackMage.FireIVsBeforeDespairDesc, "Number of Fire IV casts before Despair"), save, v => config.BlackMage.FireIVsBeforeDespair = v);

            config.BlackMage.FireIVMinMp = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.BlackMage.FireIVMinMp, "Fire IV Min MP"),
                config.BlackMage.FireIVMinMp, 400, 2000,
                Loc.T(LocalizedStrings.BlackMage.FireIVMinMpDesc, "Minimum MP to cast Fire IV"), save, v => config.BlackMage.FireIVMinMp = v);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawMovementSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.BlackMage.MovementSection, "Movement"), "BLM"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.BlackMage.SavePolyglotForMovement, "Save Polyglot for Movement"),
                () => config.BlackMage.SavePolyglotForMovement,
                v => config.BlackMage.SavePolyglotForMovement = v,
                Loc.T(LocalizedStrings.BlackMage.SavePolyglotForMovementDesc, "Reserve Polyglot stacks for movement"), save);

            config.BlackMage.PolyglotMovementReserve = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.BlackMage.PolyglotMovementReserve, "Polyglot Reserve"),
                config.BlackMage.PolyglotMovementReserve, 0, 2,
                Loc.T(LocalizedStrings.BlackMage.PolyglotMovementReserveDesc, "Polyglot stacks to reserve for movement"), save, v => config.BlackMage.PolyglotMovementReserve = v);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.BlackMage.EnableLeyLines, "Enable Ley Lines"),
                () => config.BlackMage.EnableLeyLines,
                v => config.BlackMage.EnableLeyLines = v,
                null, save, actionId: BLMActions.LeyLines.ActionId);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawThunderSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.BlackMage.ThunderSection, "Thunder DoT"), "BLM", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.BlackMage.MaintainThunder, "Maintain Thunder"),
                () => config.BlackMage.MaintainThunder,
                v => config.BlackMage.MaintainThunder = v,
                Loc.T(LocalizedStrings.BlackMage.MaintainThunderDesc, "Keep Thunder DoT active"), save);

            config.BlackMage.ThunderRefreshThreshold = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.BlackMage.ThunderRefreshThreshold, "Thunder Refresh"),
                config.BlackMage.ThunderRefreshThreshold, 0f, 15f, "%.0f s",
                Loc.T(LocalizedStrings.BlackMage.ThunderRefreshThresholdDesc, "Seconds remaining before refreshing"), save, v => config.BlackMage.ThunderRefreshThreshold = v);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.BlackMage.UseThunderheadImmediately, "Use Thunderhead Immediately"),
                () => config.BlackMage.UseThunderheadImmediately,
                v => config.BlackMage.UseThunderheadImmediately = v,
                Loc.T(LocalizedStrings.BlackMage.UseThunderheadImmediatelyDesc, "Use Thunderhead procs immediately"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawRoleActionsSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.BlackMage.RoleActionsSection, "Role Actions"), "BLM", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.BlackMage.EnableAddle, "Enable Addle"),
                () => config.BlackMage.EnableAddle,
                v => config.BlackMage.EnableAddle = v,
                null, save,
                actionId: RoleActions.Addle.ActionId);

            ConfigUIHelpers.EndIndent();
        }
    }
}
