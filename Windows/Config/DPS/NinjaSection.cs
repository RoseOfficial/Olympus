using System;
using Dalamud.Bindings.ImGui;
using Olympus.Config.DPS;
using Olympus.Localization;

namespace Olympus.Windows.Config.DPS;

/// <summary>
/// Renders the Ninja (Hermes) settings section.
/// </summary>
public sealed class NinjaSection
{
    private readonly Configuration config;
    private readonly Action save;

    public NinjaSection(Configuration config, Action save)
    {
        this.config = config;
        this.save = save;
    }

    public void Draw()
    {
        ConfigUIHelpers.JobHeader("Ninja", "Hermes", ConfigUIHelpers.NinjaColor);

        DrawDamageSection();
        DrawNinkiSection();
        DrawMudraSection();
        DrawBurstSection();
    }

    private void DrawDamageSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Ninja.DamageSection, "Damage"), "NIN"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableNinjutsu, "Enable Ninjutsu"),
                () => config.Ninja.EnableNinjutsu,
                v => config.Ninja.EnableNinjutsu = v,
                Loc.T(LocalizedStrings.Ninja.EnableNinjutsuDesc, "Use mudra combinations for Ninjutsu"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableRaiju, "Enable Raiju"),
                () => config.Ninja.EnableRaiju,
                v => config.Ninja.EnableRaiju = v,
                Loc.T(LocalizedStrings.Ninja.EnableRaijuDesc, "Use Forked/Fleeting Raiju procs"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnablePhantomKamaitachi, "Enable Phantom Kamaitachi"),
                () => config.Ninja.EnablePhantomKamaitachi,
                v => config.Ninja.EnablePhantomKamaitachi = v,
                Loc.T(LocalizedStrings.Ninja.EnablePhantomKamaitachiDesc, "Use Phantom Kamaitachi"), save);

            ConfigUIHelpers.Spacing();

            config.Ninja.AoEMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Ninja.AoEMinTargets, "AoE Min Targets"),
                config.Ninja.AoEMinTargets, 2, 8,
                Loc.T(LocalizedStrings.Ninja.AoEMinTargetsDesc, "Minimum enemies for AoE rotation"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawNinkiSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Ninja.NinkiSection, "Ninki Gauge"), "NIN"))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableBhavacakra, "Enable Bhavacakra"),
                () => config.Ninja.EnableBhavacakra,
                v => config.Ninja.EnableBhavacakra = v,
                Loc.T(LocalizedStrings.Ninja.EnableBhavacakraDesc, "Use Bhavacakra (single-target Ninki spender)"), save);

            config.Ninja.NinkiMinGauge = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Ninja.NinkiMinGauge, "Ninki Min Gauge"),
                config.Ninja.NinkiMinGauge, 50, 100,
                Loc.T(LocalizedStrings.Ninja.NinkiMinGaugeDesc, "Minimum Ninki to use spenders"), save);

            config.Ninja.NinkiOvercapThreshold = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Ninja.NinkiOvercapThreshold, "Ninki Overcap Threshold"),
                config.Ninja.NinkiOvercapThreshold, 50, 100,
                Loc.T(LocalizedStrings.Ninja.NinkiOvercapThresholdDesc, "Dump Ninki above this to avoid overcap"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawMudraSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Ninja.MudraSection, "Mudra Settings"), "NIN"))
        {
            ConfigUIHelpers.BeginIndent();

            var priority = config.Ninja.SingleTargetNinjutsuPriority;
            if (ConfigUIHelpers.EnumCombo(Loc.T(LocalizedStrings.Ninja.NinjutsuPriority, "Ninjutsu Priority"), ref priority,
                Loc.T(LocalizedStrings.Ninja.NinjutsuPriorityDesc, "Preferred Ninjutsu for single-target"), save))
            {
                config.Ninja.SingleTargetNinjutsuPriority = priority;
            }

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.UseDotonForAoE, "Use Doton for AoE"),
                () => config.Ninja.UseDotonForAoE,
                v => config.Ninja.UseDotonForAoE = v,
                Loc.T(LocalizedStrings.Ninja.UseDotonForAoEDesc, "Place Doton for AoE situations"), save);

            config.Ninja.DotonMinTargets = ConfigUIHelpers.IntSlider(
                Loc.T(LocalizedStrings.Ninja.DotonMinTargets, "Doton Min Targets"),
                config.Ninja.DotonMinTargets, 2, 8,
                Loc.T(LocalizedStrings.Ninja.DotonMinTargetsDesc, "Minimum enemies for Doton placement"), save);

            ConfigUIHelpers.EndIndent();
        }
    }

    private void DrawBurstSection()
    {
        if (ConfigUIHelpers.SectionHeader(Loc.T(LocalizedStrings.Ninja.BurstSection, "Burst Windows"), "NIN", false))
        {
            ConfigUIHelpers.BeginIndent();

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.EnableKunaisBane, "Enable Kunai's Bane"),
                () => config.Ninja.EnableKunaisBane,
                v => config.Ninja.EnableKunaisBane = v,
                Loc.T(LocalizedStrings.Ninja.EnableKunaisBaneDesc, "Use Kunai's Bane (formerly Trick Attack)"), save);

            ConfigUIHelpers.Toggle(
                Loc.T(LocalizedStrings.Ninja.AlignWithParty, "Align with Party"),
                () => config.Ninja.AlignKunaisBaneWithParty,
                v => config.Ninja.AlignKunaisBaneWithParty = v,
                Loc.T(LocalizedStrings.Ninja.AlignWithPartyDesc, "Coordinate Kunai's Bane with party burst"), save);

            config.Ninja.KunaisBaneHoldTime = ConfigUIHelpers.FloatSlider(
                Loc.T(LocalizedStrings.Ninja.KunaisBaneHoldTime, "Kunai's Bane Hold Time"),
                config.Ninja.KunaisBaneHoldTime, 0f, 10f, "%.1f s",
                Loc.T(LocalizedStrings.Ninja.KunaisBaneHoldTimeDesc, "Max seconds to hold waiting for party buffs"), save);

            ConfigUIHelpers.EndIndent();
        }
    }
}
