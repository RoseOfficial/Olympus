using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
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

            var enableRuinIV = config.Summoner.EnableRuinIV;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Summoner.EnableRuinIV, "Enable Ruin IV"), ref enableRuinIV,
                Loc.T(LocalizedStrings.Summoner.EnableRuinIVDesc, "Use Ruin IV procs"), save))
            {
                config.Summoner.EnableRuinIV = enableRuinIV;
            }

            var enablePrimalAbilities = config.Summoner.EnablePrimalAbilities;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Summoner.EnablePrimalAbilities, "Enable Primal Abilities"), ref enablePrimalAbilities,
                Loc.T(LocalizedStrings.Summoner.EnablePrimalAbilitiesDesc, "Use Gemshine/Precious Brilliance"), save))
            {
                config.Summoner.EnablePrimalAbilities = enablePrimalAbilities;
            }

            ConfigUIHelpers.Spacing();

            config.Summoner.AoEMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Summoner.AoEMinTargets, "AoE Min Targets"),
                config.Summoner.AoEMinTargets, 2, 8,
                Loc.T(LocalizedStrings.Summoner.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);

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

            var adaptOrderForMovement = config.Summoner.AdaptOrderForMovement;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Summoner.AdaptOrderForMovement, "Adapt Order for Movement"), ref adaptOrderForMovement,
                Loc.T(LocalizedStrings.Summoner.AdaptOrderForMovementDesc, "Prioritize Ifrit during movement-heavy phases"), save))
            {
                config.Summoner.AdaptOrderForMovement = adaptOrderForMovement;
            }

            ConfigUIHelpers.Spacing();
            ConfigUIHelpers.SectionLabel(Loc.T(LocalizedStrings.Summoner.PrimalToggles, "Individual Primals:"));

            var enableIfrit = config.Summoner.EnableIfrit;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Summoner.EnableIfrit, "Enable Ifrit"), ref enableIfrit, null, save))
            {
                config.Summoner.EnableIfrit = enableIfrit;
            }

            var enableTitan = config.Summoner.EnableTitan;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Summoner.EnableTitan, "Enable Titan"), ref enableTitan, null, save))
            {
                config.Summoner.EnableTitan = enableTitan;
            }

            var enableGaruda = config.Summoner.EnableGaruda;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Summoner.EnableGaruda, "Enable Garuda"), ref enableGaruda, null, save))
            {
                config.Summoner.EnableGaruda = enableGaruda;
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawDemiSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Summoner.DemiSection, "Demi-Summons"), "SMN"))
        {
            ConfigUIHelpers.BeginIndent();

            var enableBahamut = config.Summoner.EnableBahamut;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Summoner.EnableBahamut, "Enable Bahamut"), ref enableBahamut,
                Loc.T(LocalizedStrings.Summoner.EnableBahamutDesc, "Summon Bahamut"), save))
            {
                config.Summoner.EnableBahamut = enableBahamut;
            }

            var enablePhoenix = config.Summoner.EnablePhoenix;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Summoner.EnablePhoenix, "Enable Phoenix"), ref enablePhoenix,
                Loc.T(LocalizedStrings.Summoner.EnablePhoenixDesc, "Summon Phoenix"), save))
            {
                config.Summoner.EnablePhoenix = enablePhoenix;
            }

            var enableSolarBahamut = config.Summoner.EnableSolarBahamut;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Summoner.EnableSolarBahamut, "Enable Solar Bahamut"), ref enableSolarBahamut,
                Loc.T(LocalizedStrings.Summoner.EnableSolarBahamutDesc, "Summon Solar Bahamut"), save))
            {
                config.Summoner.EnableSolarBahamut = enableSolarBahamut;
            }

            var enableEnkindle = config.Summoner.EnableEnkindle;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Summoner.EnableEnkindle, "Enable Enkindle"), ref enableEnkindle,
                Loc.T(LocalizedStrings.Summoner.EnableEnkindleDesc, "Use Enkindle abilities"), save))
            {
                config.Summoner.EnableEnkindle = enableEnkindle;
            }

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Summoner.BurstSection, "Burst Windows"), "SMN", false))
        {
            ConfigUIHelpers.BeginIndent();

            var enableSearingLight = config.Summoner.EnableSearingLight;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Summoner.EnableSearingLight, "Enable Searing Light"), ref enableSearingLight,
                Loc.T(LocalizedStrings.Summoner.EnableSearingLightDesc, "Use Searing Light (party buff)"), save))
            {
                config.Summoner.EnableSearingLight = enableSearingLight;
            }

            var alignWithParty = config.Summoner.AlignSearingLightWithParty;
            if (ConfigUIHelpers.ToggleCheckbox(Loc.T(LocalizedStrings.Summoner.AlignWithParty, "Align with Party"), ref alignWithParty,
                Loc.T(LocalizedStrings.Summoner.AlignWithPartyDesc, "Coordinate Searing Light with party burst"), save))
            {
                config.Summoner.AlignSearingLightWithParty = alignWithParty;
            }

            config.Summoner.SearingLightHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Summoner.SearingLightHoldTime, "Searing Light Hold Time"),
                config.Summoner.SearingLightHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Summoner.SearingLightHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
