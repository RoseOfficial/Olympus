using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Data;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Summoner (Persephone) settings section.
/// </summary>
public sealed class SummonerSection
{
    private readonly Configuration config;
    private readonly Action save;

    public SummonerSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Summoner", "Persephone", ConfigUIHelpers.SummonerColor);

        DrawDamageSection();
        DrawPrimalSection();
        DrawDemiSection();
        DrawBurstSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Summoner.DamageSection, "Damage"), "SMN"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.EnableRuinIV, "Enable Ruin IV"),
                () => config.Summoner.EnableRuinIV,
                v => config.Summoner.EnableRuinIV = v,
                null, save, actionId: SMNActions.Ruin4.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.EnablePrimalAbilities, "Enable Primal Abilities"),
                () => config.Summoner.EnablePrimalAbilities,
                v => config.Summoner.EnablePrimalAbilities = v,
                Loc.T(LocalizedStrings.Summoner.EnablePrimalAbilitiesDesc, "Use Gemshine/Precious Brilliance"), save);

            ConfigUIHelpers.Spacing();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.EnableAoERotation, "Enable AoE Rotation"),
                () => config.Summoner.EnableAoERotation,
                v => config.Summoner.EnableAoERotation = v,
                Loc.T(LocalizedStrings.Summoner.EnableAoERotationDesc, "Switch to AoE combo at 3+ enemies."), save);

            if (config.Summoner.EnableAoERotation)
            {
                config.Summoner.AoEMinTargets = ConfigUIHelpers.IntSlider(
                    Loc.T(LocalizedStrings.Summoner.AoEMinTargets, "AoE Min Targets"),
                    config.Summoner.AoEMinTargets, 2, 8,
                    Loc.T(LocalizedStrings.Summoner.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawPrimalSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Summoner.PrimalSection, "Primal Summons"), "SMN"))
        {
            ConfigUIHelpers.BeginIndent();

            var primalOrder = config.Summoner.PrimalSummonOrder;
            if (ConfigUIHelpers.EnumCombo(Loc.T(LocalizedStrings.Summoner.PrimalOrder, "Primal Order"), ref primalOrder,
                Loc.T(LocalizedStrings.Summoner.PrimalOrderDesc, "Preferred primal summon order"), save))
            {
                config.Summoner.PrimalSummonOrder = primalOrder;
            }

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.AdaptOrderForMovement, "Adapt Order for Movement"),
                () => config.Summoner.AdaptOrderForMovement,
                v => config.Summoner.AdaptOrderForMovement = v,
                Loc.T(LocalizedStrings.Summoner.AdaptOrderForMovementDesc, "Prioritize Ifrit during movement-heavy phases"), save);

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Summoner.PrimalToggles, "Individual Primals:"));

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.EnableIfrit, "Enable Ifrit"),
                () => config.Summoner.EnableIfrit,
                v => config.Summoner.EnableIfrit = v,
                null, save, actionId: SMNActions.SummonIfrit.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.EnableTitan, "Enable Titan"),
                () => config.Summoner.EnableTitan,
                v => config.Summoner.EnableTitan = v,
                null, save, actionId: SMNActions.SummonTitan.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.EnableGaruda, "Enable Garuda"),
                () => config.Summoner.EnableGaruda,
                v => config.Summoner.EnableGaruda = v,
                null, save, actionId: SMNActions.SummonGaruda.ActionId);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDemiSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Summoner.DemiSection, "Demi-Summons"), "SMN"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.EnableBahamut, "Enable Bahamut"),
                () => config.Summoner.EnableBahamut,
                v => config.Summoner.EnableBahamut = v,
                null, save, actionId: SMNActions.SummonBahamut.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.EnablePhoenix, "Enable Phoenix"),
                () => config.Summoner.EnablePhoenix,
                v => config.Summoner.EnablePhoenix = v,
                null, save, actionId: SMNActions.SummonPhoenix.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.EnableSolarBahamut, "Enable Solar Bahamut"),
                () => config.Summoner.EnableSolarBahamut,
                v => config.Summoner.EnableSolarBahamut = v,
                null, save, actionId: SMNActions.SummonSolarBahamut.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.EnableEnkindle, "Enable Enkindle"),
                () => config.Summoner.EnableEnkindle,
                v => config.Summoner.EnableEnkindle = v,
                Loc.T(LocalizedStrings.Summoner.EnableEnkindleDesc, "Use Enkindle abilities"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Summoner.BurstSection, "Burst Windows"), "SMN", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.EnableSearingLight, "Enable Searing Light"),
                () => config.Summoner.EnableSearingLight,
                v => config.Summoner.EnableSearingLight = v,
                null, save, actionId: SMNActions.SearingLight.ActionId);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Summoner.AlignWithParty, "Align with Party"),
                () => config.Summoner.AlignSearingLightWithParty,
                v => config.Summoner.AlignSearingLightWithParty = v,
                Loc.T(LocalizedStrings.Summoner.AlignWithPartyDesc, "Coordinate Searing Light with party burst"), save);

            config.Summoner.SearingLightHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Summoner.SearingLightHoldTime, "Searing Light Hold Time"),
                config.Summoner.SearingLightHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Summoner.SearingLightHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
